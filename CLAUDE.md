# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

WindowsAutostartApi is a .NET 8 library for managing Windows startup entries through multiple providers:
- **Registry Provider**: Manages `HKCU/HKLM\Software\Microsoft\Windows\CurrentVersion\Run` and `RunOnce` entries
- **Startup Folder Provider**: Manages `.lnk` shortcuts in per-user and all-users startup folders

## Architecture

The codebase follows a provider pattern:
- `IStartupManager` (main interface) → `StartupManager` (concrete implementation)
- `IStartupProvider` → `RegistryStartupProvider`, `StartupFolderProvider`
- Core models: `StartupEntry`, `StartupScope` (CurrentUser/AllUsers), `StartupKind` (Run/RunOnce/StartupFolder)

Key files:
- `AutostartWindowsApi/Core/StartupManager.cs` - Main orchestrator
- `AutostartWindowsApi/Abstractions/` - Interfaces and models
- `AutostartWindowsApi/Core/` - Provider implementations
- `AutostartWindowsApi/Interop/` - Windows Shell Link COM interop

## Development Commands

```bash
# Build the solution
dotnet build

# Build release version
dotnet build -c Release

# Run the demo project
dotnet run --project AutostartDemo

# Run unit tests
dotnet test --filter "Category!=Integration"

# Run all tests (including integration tests)
dotnet test

# Pack for NuGet (version will be taken from PackageVersion property)
dotnet pack -c Release -o ./artifacts

# Restore dependencies
dotnet restore
```

## NuGet Publishing

Automated via GitHub Actions on commits to master branch. To publish a new version, include `nuget: <version>` in commit message (e.g., "nuget: 1.2.3").

## Platform Requirements

- Target Framework: .NET 8 (net8.0-windows)
- OS: Windows only (uses Windows Registry and Shell Link COM interfaces)
- Permissions: HKCU operations work without admin rights, HKLM operations require elevation

## Testing

Comprehensive unit test suite with 114+ tests covering:
- Core functionality (StartupManager, StartupManagerEx, CachedStartupManager)
- Path validation and security (PathHelpers)
- Error handling and Result pattern (OperationResult)
- Provider behavior with mocking
- Integration tests for real system interaction

Test frameworks: xUnit, FluentAssertions, Moq

## Optional Improvements

- Code analysis tools (StyleCop, analyzers)
- `.editorconfig` for consistent formatting
- XML documentation for public APIs