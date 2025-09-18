using System.Collections.Generic;

namespace WindowsAutostartApi.Abstractions;

/// <summary>
/// Low-level provider for a specific startup mechanism (e.g., Registry or Startup Folder).
/// </summary>
public interface IStartupProvider
{
    IEnumerable<StartupEntry> ListAll();
    bool Supports(StartupKind kind);
    bool Exists(string name, StartupScope scope, StartupKind kind);
    void Add(StartupEntry entry);
    void Remove(string name, StartupScope scope, StartupKind kind);
}
