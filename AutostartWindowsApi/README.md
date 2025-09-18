# WindowsAutostartApi

Unified .NET API to manage Windows autostart entries:
- **Registry**: `HKCU/HKLM\Software\Microsoft\Windows\CurrentVersion\Run` and `RunOnce`
- **Startup Folder**: per-user and all-users (`.lnk` shortcuts)

> Target Framework: **.NET 8 (net8.0-windows)**  
> OS: **Windows only**

## Features
- List all startup entries (Registry + Startup Folder)
- Add or update entries (idempotent)
- Remove entries
- Clean, extensible design (providers)

## Install
```powershell
dotnet add package WindowsAutostartApi
````

## Quick Start

```csharp
using WindowsAutostartApi;

var mgr = new StartupManager();

// Add HKCU\Run entry
mgr.Add(new StartupEntry(
    Name: "MyCoolApp",
    TargetPath: @"C:\Tools\MyCoolApp.exe",
    Arguments: "--minimized",
    Scope: StartupScope.CurrentUser,
    Kind: StartupKind.Run));

// Add Startup Folder shortcut
mgr.Add(new StartupEntry(
    Name: "MyCoolApp (Shortcut)",
    TargetPath: @"C:\Tools\MyCoolApp.exe",
    Arguments: "--tray",
    Scope: StartupScope.CurrentUser,
    Kind: StartupKind.StartupFolder));

// List
foreach (var e in mgr.ListAll())
    Console.WriteLine($"{e.Scope} {e.Kind}: {e.Name} -> {e.TargetPath} {e.Arguments}");

// Remove
mgr.Remove("MyCoolApp", StartupScope.CurrentUser, StartupKind.Run);
mgr.Remove("MyCoolApp (Shortcut)", StartupScope.CurrentUser, StartupKind.StartupFolder);
```

## API Surface

* `StartupEntry(string Name, string TargetPath, string? Arguments, StartupScope Scope, StartupKind Kind)`
* `StartupScope`: `CurrentUser`, `AllUsers`
* `StartupKind`: `Run`, `RunOnce`, `StartupFolder`
* `IStartupManager`:

  * `IReadOnlyList<StartupEntry> ListAll()`
  * `bool Exists(string name, StartupScope scope, StartupKind kind)`
  * `void Add(StartupEntry entry)`
  * `void Remove(string name, StartupScope scope, StartupKind kind)`

## Permissions

* **HKCU**: keine Admin-Rechte
* **HKLM (AllUsers)**: benötigt Administratorrechte (UAC Elevation)

## 32/64-bit Notes

* Registry view defaults to 64-bit on 64-bit OS (`RegistryView.Registry64`).

## Demo Project

A small console demo is included under `samples/WindowsAutostartApi.Demo`, but it is **skipped by default** during solution builds.
Build/run it explicitly with:

```bash
dotnet run --project samples/WindowsAutostartApi.Demo -p:SkipBuild=false
```

## Roadmap

* Optional **Task Scheduler** provider (Logon trigger, delay, highest privileges)
