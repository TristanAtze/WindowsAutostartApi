using System;
using System.Runtime.Versioning;
using WindowsAutostartApi;
using WindowsAutostartApi.Abstractions;
using WindowsAutostartApi.Core;

class Program
{
    [SupportedOSPlatform("windows")]
    static void Main()
    {
        var mgr = new StartupManager();

        Console.WriteLine("=== WindowsAutostartApi Demo ===");

        // 1) Add Run (HKCU) entry
        var runEntry = new StartupEntry(
            Name: "MyCoolApp",
            TargetPath: @"C:\Windows\System32\notepad.exe", // demo target
            Arguments: null,
            Scope: StartupScope.CurrentUser,
            Kind: StartupKind.Run);

        mgr.Add(runEntry);
        Console.WriteLine("Added HKCU\\Run entry: " + runEntry.Name);

        // 2) Add StartupFolder shortcut (current user)
        var folderEntry = new StartupEntry(
            Name: "MyCoolApp (Shortcut)",
            TargetPath: @"C:\Windows\System32\notepad.exe",
            Arguments: "--started-from-startup-folder",
            Scope: StartupScope.CurrentUser,
            Kind: StartupKind.StartupFolder);

        mgr.Add(folderEntry);
        Console.WriteLine("Added StartupFolder shortcut: " + folderEntry.Name);

        // 3) List all
        Console.WriteLine();
        Console.WriteLine("=== ListAll() ===");
        foreach (var e in mgr.ListAll())
        {
            Console.WriteLine($"{e.Scope,-11} {e.Kind,-12} {e.Name} -> {e.TargetPath} {e.Arguments}");
        }

        // 4) Exists?
        Console.WriteLine();
        Console.WriteLine($"Exists(MyCoolApp, HKCU, Run)? {mgr.Exists("MyCoolApp", StartupScope.CurrentUser, StartupKind.Run)}");
        Console.WriteLine($"Exists(MyCoolApp (Shortcut), HKCU, StartupFolder)? {mgr.Exists("MyCoolApp (Shortcut)", StartupScope.CurrentUser, StartupKind.StartupFolder)}");

        // 5) Remove entries again (cleanup)
        Console.WriteLine();
        mgr.Remove("MyCoolApp", StartupScope.CurrentUser, StartupKind.Run);
        mgr.Remove("MyCoolApp (Shortcut)", StartupScope.CurrentUser, StartupKind.StartupFolder);
        Console.WriteLine("Cleanup done.");

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
