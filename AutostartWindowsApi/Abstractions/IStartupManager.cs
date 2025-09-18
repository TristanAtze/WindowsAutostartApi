using System.Collections.Generic;

namespace WindowsAutostartApi.Abstractions;

/// <summary>
/// High-level manager aggregating multiple startup mechanisms.
/// </summary>
public interface IStartupManager
{
    IReadOnlyList<StartupEntry> ListAll();
    bool Exists(string name, StartupScope scope, StartupKind kind);
    void Add(StartupEntry entry);     // idempotent: updates if exists
    void Remove(string name, StartupScope scope, StartupKind kind);
}
