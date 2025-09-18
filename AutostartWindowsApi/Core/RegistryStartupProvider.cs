using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Security;
using WindowsAutostartApi.Abstractions;
using WindowsAutostartApi.Utils;

namespace WindowsAutostartApi.Core;

[SupportedOSPlatform("windows")]
internal sealed class RegistryStartupProvider : IStartupProvider
{
    private static readonly object _lock = new object();

    public IEnumerable<StartupEntry> ListAll()
    {
        lock (_lock)
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
    }

    public bool Supports(StartupKind kind)
        => kind == StartupKind.Run || kind == StartupKind.RunOnce;

    public bool Exists(string name, StartupScope scope, StartupKind kind)
    {
        lock (_lock)
        {
            return ReadRegistryValue(scope, kind, name) is not null;
        }
    }

    public void Add(StartupEntry entry)
    {
        if (!Supports(entry.Kind)) throw new NotSupportedException($"{nameof(RegistryStartupProvider)} does not support {entry.Kind}");

        var command = PathHelpers.QuoteIfNeeded(entry.TargetPath);
        if (!string.IsNullOrWhiteSpace(entry.Arguments))
            command += " " + entry.Arguments;

        lock (_lock)
        {
            try
            {
                using var baseKey = GetBaseKey(entry.Scope, writable: true);
                using var key = baseKey.CreateSubKey(GetRunKeyPath(entry.Kind))
                              ?? throw new InvalidOperationException("Failed to open or create the Run key.");
                key.SetValue(entry.Name, command, RegistryValueKind.String);
            }
            catch (SecurityException ex)
            {
                throw new UnauthorizedAccessException($"Access denied when writing to {entry.Scope} registry. Administrator rights may be required.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedAccessException($"Access denied when writing to {entry.Scope} registry. Administrator rights may be required.", ex);
            }
            catch (Exception ex) when (ex is ArgumentException or ObjectDisposedException)
            {
                throw new InvalidOperationException($"Failed to add startup entry '{entry.Name}': {ex.Message}", ex);
            }
        }
    }

    public void Remove(string name, StartupScope scope, StartupKind kind)
    {
        lock (_lock)
        {
            try
            {
                using var baseKey = GetBaseKey(scope, writable: true);
                using var key = baseKey.OpenSubKey(GetRunKeyPath(kind), writable: true);
                key?.DeleteValue(name, throwOnMissingValue: false);
            }
            catch (SecurityException ex)
            {
                throw new UnauthorizedAccessException($"Access denied when removing from {scope} registry. Administrator rights may be required.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedAccessException($"Access denied when removing from {scope} registry. Administrator rights may be required.", ex);
            }
            catch (Exception ex) when (ex is ArgumentException or ObjectDisposedException)
            {
                throw new InvalidOperationException($"Failed to remove startup entry '{name}': {ex.Message}", ex);
            }
        }
    }

    // ----- Helpers -----

    private static IEnumerable<StartupEntry> ListRegistry(StartupScope scope, StartupKind kind)
    {
        RegistryKey? baseKey = null;
        RegistryKey? key = null;
        bool accessDenied = false;

        try
        {
            baseKey = GetBaseKey(scope, writable: false);
            key = baseKey.OpenSubKey(GetRunKeyPath(kind), writable: false);
            if (key is null) yield break;
        }
        catch (SecurityException)
        {
            accessDenied = true;
        }
        catch (UnauthorizedAccessException)
        {
            accessDenied = true;
        }

        if (accessDenied)
        {
            yield break;
        }

        foreach (var name in key.GetValueNames())
        {
            var value = key.GetValue(name) as string;
            if (string.IsNullOrWhiteSpace(value)) continue;

            if (TrySplitCommand(value, out var target, out var args) && !string.IsNullOrWhiteSpace(target))
            {
                yield return new StartupEntry(
                    Name: name,
                    TargetPath: target,
                    Arguments: args,
                    Scope: scope,
                    Kind: kind);
            }
        }

        key?.Dispose();
        baseKey?.Dispose();
    }

    private static string? ReadRegistryValue(StartupScope scope, StartupKind kind, string name)
    {
        try
        {
            using var baseKey = GetBaseKey(scope, writable: false);
            using var key = baseKey.OpenSubKey(GetRunKeyPath(kind), writable: false);
            return key?.GetValue(name) as string;
        }
        catch (SecurityException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
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
    /// Tries to split a command value of form: "C:\path to\app.exe" arg1 arg2
    /// </summary>
    private static bool TrySplitCommand(string command, out string? target, out string? args)
    {
        target = null;
        args = null;

        if (string.IsNullOrWhiteSpace(command))
            return false;

        command = command.Trim();

        if (command.StartsWith("\""))
        {
            // Quoted path: find closing quote
            int end = command.IndexOf('"', 1);
            if (end > 1)
            {
                target = command.Substring(1, end - 1);
                args = command.Length > end + 1 ? command.Substring(end + 1).TrimStart() : null;

                // Additional validation for quoted paths
                if (string.IsNullOrWhiteSpace(target))
                    return false;

                return true;
            }
            // Malformed quoted string
            return false;
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

            // Don't return empty args
            if (string.IsNullOrWhiteSpace(args))
                args = null;
        }

        return !string.IsNullOrWhiteSpace(target);
    }
}
