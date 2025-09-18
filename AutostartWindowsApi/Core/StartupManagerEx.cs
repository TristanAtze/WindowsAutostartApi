using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using WindowsAutostartApi.Abstractions;
using WindowsAutostartApi.Utils;

namespace WindowsAutostartApi.Core;

/// <summary>
/// Extended startup manager with Result pattern for detailed error reporting.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class StartupManagerEx : IStartupManagerEx
{
    private readonly IStartupProvider[] _providers;

    public StartupManagerEx()
        : this(new IStartupProvider[]
        {
            new RegistryStartupProvider(),
            new StartupFolderProvider()
        })
    { }

    public StartupManagerEx(IEnumerable<IStartupProvider> providers)
    {
        _providers = providers.ToArray();
    }

    public OperationResult<IReadOnlyList<StartupEntry>> TryListAll()
    {
        try
        {
            var entries = new List<StartupEntry>();

            foreach (var provider in _providers)
            {
                try
                {
                    entries.AddRange(provider.ListAll());
                }
                catch (Exception ex)
                {
                    // Log provider failures but continue with other providers
                    // In a real implementation, you might want to use a logging framework
                    System.Diagnostics.Debug.WriteLine($"Provider {provider.GetType().Name} failed: {ex.Message}");
                }
            }

            return OperationResult<IReadOnlyList<StartupEntry>>.Success(entries);
        }
        catch (Exception ex)
        {
            return OperationResult<IReadOnlyList<StartupEntry>>.Failure("Failed to list startup entries", ex);
        }
    }

    public OperationResult<bool> TryExists(string name, StartupScope scope, StartupKind kind)
    {
        try
        {
            var provider = GetProvider(kind);
            if (provider == null)
                return OperationResult<bool>.Failure($"No provider supports kind {kind}");

            var exists = provider.Exists(name, scope, kind);
            return OperationResult<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.Failure($"Failed to check if startup entry '{name}' exists", ex);
        }
    }

    public OperationResult TryAdd(StartupEntry entry)
    {
        try
        {
            var validationResult = ValidateEntry(entry);
            if (!validationResult.IsSuccess)
                return validationResult;

            var provider = GetProvider(entry.Kind);
            if (provider == null)
                return OperationResult.Failure($"No provider supports kind {entry.Kind}");

            provider.Add(entry);
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"Failed to add startup entry '{entry.Name}'", ex);
        }
    }

    public OperationResult TryRemove(string name, StartupScope scope, StartupKind kind)
    {
        try
        {
            var provider = GetProvider(kind);
            if (provider == null)
                return OperationResult.Failure($"No provider supports kind {kind}");

            provider.Remove(name, scope, kind);
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"Failed to remove startup entry '{name}'", ex);
        }
    }

    private IStartupProvider? GetProvider(StartupKind kind)
        => _providers.FirstOrDefault(p => p.Supports(kind));

    private static OperationResult ValidateEntry(StartupEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Name))
            return OperationResult.Failure("Entry name cannot be empty.");

        if (!PathHelpers.IsValidEntryName(entry.Name))
            return OperationResult.Failure("Entry name contains invalid characters or is too long.");

        if (string.IsNullOrWhiteSpace(entry.TargetPath))
            return OperationResult.Failure("TargetPath cannot be empty.");

        if (!PathHelpers.IsValidPath(entry.TargetPath))
            return OperationResult.Failure("TargetPath is invalid or contains security risks.");

        if (!string.IsNullOrWhiteSpace(entry.Arguments) && entry.Arguments.Length > 1024)
            return OperationResult.Failure("Arguments string is too long (max 1024 characters).");

        return OperationResult.Success();
    }
}