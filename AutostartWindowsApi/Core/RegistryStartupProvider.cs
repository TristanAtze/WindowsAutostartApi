using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using WindowsAutostartApi.Abstractions;
using WindowsAutostartApi.Utils;

namespace WindowsAutostartApi.Core;

[SupportedOSPlatform("windows")]
internal sealed class RegistryStartupProvider : IStartupProvider
{
    public IEnumerable<StartupEntry> ListAll()
    {
        foreach (var scope in new[] { StartupScope.CurrentUser, StartupScope.AllUsers })
        {
            foreach (var kind in new[] { StartupKind.Run, StartupKind.RunOnce })
            {
                foreach (var e in ListRegistry(scope, kind))
                    yield return e;
            }
        }
    }

    public bool Supports(StartupKind kind)
        => kind == StartupKind.Run || kind == StartupKind.RunOnce;

    public bool Exists(string name, StartupScope scope, StartupKind kind)
        => ReadRegistryValue(scope, kind, name) is not null;

    public void Add(StartupEntry entry)
    {
        if (!Supports(entry.Kind)) throw new NotSupportedException($"{nameof(RegistryStartupProvider)} does not support {entry.Kind}");

        var command = PathHelpers.QuoteIfNeeded(entry.TargetPath);
        if (!string.IsNullOrWhiteSpace(entry.Arguments))
            command += " " + entry.Arguments;

        using var baseKey = GetBaseKey(entry.Scope, writable: true);
        using var key = baseKey.CreateSubKey(GetRunKeyPath(entry.Kind))
                      ?? throw new InvalidOperationException("Failed to open or create the Run key.");
        key.SetValue(entry.Name, command, RegistryValueKind.String);
    }

    public void Remove(string name, StartupScope scope, StartupKind kind)
    {
        using var baseKey = GetBaseKey(scope, writable: true);
        using var key = baseKey.OpenSubKey(GetRunKeyPath(kind), writable: true);
        key?.DeleteValue(name, throwOnMissingValue: false);
    }

    // ----- Helpers -----

    private static IEnumerable<StartupEntry> ListRegistry(StartupScope scope, StartupKind kind)
    {
        using var baseKey = GetBaseKey(scope, writable: false);
        using var key = baseKey.OpenSubKey(GetRunKeyPath(kind), writable: false);
        if (key is null) yield break;

        foreach (var name in key.GetValueNames())
        {
            var value = key.GetValue(name) as string;
            if (string.IsNullOrWhiteSpace(value)) continue;

            SplitCommand(value, out var target, out var args);

            yield return new StartupEntry(
                Name: name,
                TargetPath: target,
                Arguments: args,
                Scope: scope,
                Kind: kind);
        }
    }

    private static string? ReadRegistryValue(StartupScope scope, StartupKind kind, string name)
    {
        using var baseKey = GetBaseKey(scope, writable: false);
        using var key = baseKey.OpenSubKey(GetRunKeyPath(kind), writable: false);
        return key?.GetValue(name) as string;
    }

    private static RegistryKey GetBaseKey(StartupScope scope, bool writable)
    {
        var view = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Default;
        return scope == StartupScope.AllUsers
            ? RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view)
            : RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view);
    }

    private static string GetRunKeyPath(StartupKind kind)
        => $@"Software\Microsoft\Windows\CurrentVersion\{(kind == StartupKind.RunOnce ? "RunOnce" : "Run")}";

    /// <summary>
    /// Splits a command value of form: "C:\path to\app.exe" arg1 arg2
    /// </summary>
    private static void SplitCommand(string command, out string target, out string? args)
    {
        command = command.Trim();

        if (command.StartsWith("\""))
        {
            // Quoted path: find closing quote
            int end = command.IndexOf('"', 1);
            if (end > 1)
            {
                target = command.Substring(1, end - 1);
                args = command.Length > end + 1 ? command.Substring(end + 1).TrimStart() : null;
                return;
            }
        }

        // Unquoted: split on first whitespace
        int idx = command.IndexOfAny(new[] { ' ', '\t' });
        if (idx < 0)
        {
            target = command;
            args = null;
        }
        else
        {
            target = command.Substring(0, idx);
            args = command.Substring(idx + 1).TrimStart();
        }
    }
}
