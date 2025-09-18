using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using WindowsAutostartApi.Abstractions;
using WindowsAutostartApi.Utils;

namespace WindowsAutostartApi.Core;

[SupportedOSPlatform("windows")]
public sealed class StartupManager : IStartupManager
{
    private readonly IStartupProvider[] _providers;

    public StartupManager()
        : this(new IStartupProvider[]
        {
            new RegistryStartupProvider(),
            new StartupFolderProvider()
        })
    { }

    internal StartupManager(IEnumerable<IStartupProvider> providers)
    {
        _providers = providers.ToArray();
    }

    public IReadOnlyList<StartupEntry> ListAll()
        => _providers.SelectMany(p => p.ListAll()).ToList();

    public bool Exists(string name, StartupScope scope, StartupKind kind)
    {
        var provider = GetProvider(kind);
        return provider.Exists(name, scope, kind);
    }

    public void Add(StartupEntry entry)
    {
        Validate(entry);
        var provider = GetProvider(entry.Kind);
        provider.Add(entry);
    }

    public void Remove(string name, StartupScope scope, StartupKind kind)
    {
        var provider = GetProvider(kind);
        provider.Remove(name, scope, kind);
    }

    private IStartupProvider GetProvider(StartupKind kind)
        => _providers.FirstOrDefault(p => p.Supports(kind))
           ?? throw new NotSupportedException($"No provider supports kind {kind}.");

    private static void Validate(StartupEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Name))
            throw new ArgumentException("Entry name cannot be empty.", nameof(entry));

        if (!PathHelpers.IsValidEntryName(entry.Name))
            throw new ArgumentException("Entry name contains invalid characters or is too long.", nameof(entry));

        if (string.IsNullOrWhiteSpace(entry.TargetPath))
            throw new ArgumentException("TargetPath cannot be empty.", nameof(entry));

        if (!PathHelpers.IsValidPath(entry.TargetPath))
            throw new ArgumentException("TargetPath is invalid or contains security risks.", nameof(entry));

        // Validate arguments if provided
        if (!string.IsNullOrWhiteSpace(entry.Arguments) && entry.Arguments.Length > 1024)
            throw new ArgumentException("Arguments string is too long (max 1024 characters).", nameof(entry));

        // Optional: you could require that the file exists
        // if (!File.Exists(Unquote(entry.TargetPath))) ...
    }
}
