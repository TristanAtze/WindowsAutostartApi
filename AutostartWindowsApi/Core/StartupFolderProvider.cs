using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using WindowsAutostartApi.Abstractions;
using WindowsAutostartApi.Interop;

namespace WindowsAutostartApi.Core;

[SupportedOSPlatform("windows")]
internal sealed class StartupFolderProvider : IStartupProvider
{
    public IEnumerable<StartupEntry> ListAll()
    {
        foreach (var scope in new[] { StartupScope.CurrentUser, StartupScope.AllUsers })
        {
            var dir = GetStartupFolder(scope);
            if (!Directory.Exists(dir)) continue;

            foreach (var lnk in Directory.EnumerateFiles(dir, "*.lnk"))
            {
                var name = Path.GetFileNameWithoutExtension(lnk);
                if (ShellLinkInterop.TryResolveShortcut(lnk, out var target, out var args)
                    && !string.IsNullOrWhiteSpace(target))
                {
                    yield return new StartupEntry(
                        Name: name,
                        TargetPath: target!,
                        Arguments: string.IsNullOrWhiteSpace(args) ? null : args,
                        Scope: scope,
                        Kind: StartupKind.StartupFolder);
                }
            }
        }
    }

    public bool Supports(StartupKind kind)
        => kind == StartupKind.StartupFolder;

    public bool Exists(string name, StartupScope scope, StartupKind kind)
    {
        var path = FindShortcutPath(scope, name);
        return File.Exists(path);
    }

    public void Add(StartupEntry entry)
    {
        if (!Supports(entry.Kind)) throw new NotSupportedException($"{nameof(StartupFolderProvider)} does not support {entry.Kind}");

        var folder = GetStartupFolder(entry.Scope);
        Directory.CreateDirectory(folder);

        var path = FindShortcutPath(entry.Scope, entry.Name);
        if (string.IsNullOrWhiteSpace(path))
            path = Path.Combine(folder, entry.Name + ".lnk");

        ShellLinkInterop.CreateShortcut(path, entry.TargetPath, entry.Arguments);
    }

    public void Remove(string name, StartupScope scope, StartupKind kind)
    {
        var path = FindShortcutPath(scope, name);
        if (File.Exists(path))
            File.Delete(path);
    }

    // ----- Helpers -----

    private static string GetStartupFolder(StartupScope scope)
    {
        var folder = scope == StartupScope.AllUsers
            ? Environment.SpecialFolder.CommonStartup
            : Environment.SpecialFolder.Startup;
        return Environment.GetFolderPath(folder);
    }

    private static string FindShortcutPath(StartupScope scope, string name)
    {
        var dir = GetStartupFolder(scope);
        return Path.Combine(dir, name + ".lnk");
    }
}
