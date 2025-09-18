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
    private static readonly object _lock = new object();
    public IEnumerable<StartupEntry> ListAll()
    {
        lock (_lock)
        {
            var entries = new List<StartupEntry>();

            foreach (var scope in new[] { StartupScope.CurrentUser, StartupScope.AllUsers })
            {
                var dir = GetStartupFolder(scope);
                if (!Directory.Exists(dir)) continue;

                try
                {
                    // Get all .lnk files at once for better performance
                    var lnkFiles = Directory.GetFiles(dir, "*.lnk", SearchOption.TopDirectoryOnly);

                    foreach (var lnk in lnkFiles)
                    {
                        var name = Path.GetFileNameWithoutExtension(lnk);
                        if (ShellLinkInterop.TryResolveShortcut(lnk, out var target, out var args)
                            && !string.IsNullOrWhiteSpace(target))
                        {
                            entries.Add(new StartupEntry(
                                Name: name,
                                TargetPath: target!,
                                Arguments: string.IsNullOrWhiteSpace(args) ? null : args,
                                Scope: scope,
                                Kind: StartupKind.StartupFolder));
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip directories we can't access
                    continue;
                }
                catch (DirectoryNotFoundException)
                {
                    // Skip non-existent directories
                    continue;
                }
            }

            return entries;
        }
    }

    public bool Supports(StartupKind kind)
        => kind == StartupKind.StartupFolder;

    public bool Exists(string name, StartupScope scope, StartupKind kind)
    {
        lock (_lock)
        {
            var path = FindShortcutPath(scope, name);
            return File.Exists(path);
        }
    }

    public void Add(StartupEntry entry)
    {
        if (!Supports(entry.Kind)) throw new NotSupportedException($"{nameof(StartupFolderProvider)} does not support {entry.Kind}");

        lock (_lock)
        {
            try
            {
                var folder = GetStartupFolder(entry.Scope);
                Directory.CreateDirectory(folder);

                var path = FindShortcutPath(entry.Scope, entry.Name);
                if (string.IsNullOrWhiteSpace(path))
                    path = Path.Combine(folder, entry.Name + ".lnk");

                ShellLinkInterop.CreateShortcut(path, entry.TargetPath, entry.Arguments);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedAccessException($"Access denied when creating shortcut in {entry.Scope} startup folder. Administrator rights may be required.", ex);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"Failed to create startup shortcut '{entry.Name}': {ex.Message}", ex);
            }
        }
    }

    public void Remove(string name, StartupScope scope, StartupKind kind)
    {
        lock (_lock)
        {
            try
            {
                var path = FindShortcutPath(scope, name);
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedAccessException($"Access denied when removing shortcut from {scope} startup folder. Administrator rights may be required.", ex);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"Failed to remove startup shortcut '{name}': {ex.Message}", ex);
            }
        }
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
