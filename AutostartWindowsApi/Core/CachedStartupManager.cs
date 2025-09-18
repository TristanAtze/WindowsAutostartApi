using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using WindowsAutostartApi.Abstractions;

namespace WindowsAutostartApi.Core;

/// <summary>
/// Performance-optimized startup manager with caching and lazy loading.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class CachedStartupManager : IStartupManager, IDisposable
{
    private readonly IStartupProvider[] _providers;
    private readonly TimeSpan _cacheExpiry;
    private readonly ReaderWriterLockSlim _cacheLock = new();

    private List<StartupEntry>? _cachedEntries;
    private DateTime _lastCacheUpdate = DateTime.MinValue;
    private bool _disposed;

    public CachedStartupManager(TimeSpan? cacheExpiry = null)
        : this(new IStartupProvider[]
        {
            new RegistryStartupProvider(),
            new StartupFolderProvider()
        }, cacheExpiry)
    { }

    internal CachedStartupManager(IEnumerable<IStartupProvider> providers, TimeSpan? cacheExpiry = null)
    {
        _providers = providers.ToArray();
        _cacheExpiry = cacheExpiry ?? TimeSpan.FromMinutes(5);
    }

    public IReadOnlyList<StartupEntry> ListAll()
    {
        ThrowIfDisposed();

        _cacheLock.EnterReadLock();
        try
        {
            if (_cachedEntries != null && DateTime.UtcNow - _lastCacheUpdate < _cacheExpiry)
            {
                return _cachedEntries.AsReadOnly();
            }
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }

        return RefreshCache();
    }

    public bool Exists(string name, StartupScope scope, StartupKind kind)
    {
        ThrowIfDisposed();

        var provider = GetProvider(kind);
        return provider.Exists(name, scope, kind);
    }

    public void Add(StartupEntry entry)
    {
        ThrowIfDisposed();

        Validate(entry);
        var provider = GetProvider(entry.Kind);
        provider.Add(entry);

        InvalidateCache();
    }

    public void Remove(string name, StartupScope scope, StartupKind kind)
    {
        ThrowIfDisposed();

        var provider = GetProvider(kind);
        provider.Remove(name, scope, kind);

        InvalidateCache();
    }

    /// <summary>
    /// Forces a cache refresh and returns the latest entries.
    /// </summary>
    public IReadOnlyList<StartupEntry> RefreshCache()
    {
        ThrowIfDisposed();

        _cacheLock.EnterWriteLock();
        try
        {
            var entries = new List<StartupEntry>();

            // Use parallel loading for better performance
            var providerTasks = _providers.AsParallel().Select(provider =>
            {
                try
                {
                    return provider.ListAll().ToList();
                }
                catch
                {
                    // Skip failed providers
                    return new List<StartupEntry>();
                }
            }).ToArray();

            foreach (var providerEntries in providerTasks)
            {
                entries.AddRange(providerEntries);
            }

            _cachedEntries = entries;
            _lastCacheUpdate = DateTime.UtcNow;

            return entries.AsReadOnly();
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Clears the cache, forcing the next ListAll() to reload from providers.
    /// </summary>
    public void InvalidateCache()
    {
        if (_disposed) return;

        _cacheLock.EnterWriteLock();
        try
        {
            _cachedEntries = null;
            _lastCacheUpdate = DateTime.MinValue;
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }
    }

    private IStartupProvider GetProvider(StartupKind kind)
        => _providers.FirstOrDefault(p => p.Supports(kind))
           ?? throw new NotSupportedException($"No provider supports kind {kind}.");

    private static void Validate(StartupEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Name))
            throw new ArgumentException("Entry name cannot be empty.", nameof(entry));

        if (string.IsNullOrWhiteSpace(entry.TargetPath))
            throw new ArgumentException("TargetPath cannot be empty.", nameof(entry));

        // Additional validation can be added here
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CachedStartupManager));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cacheLock.Dispose();
            _disposed = true;
        }
    }
}