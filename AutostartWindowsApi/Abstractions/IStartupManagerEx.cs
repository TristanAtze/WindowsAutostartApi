using System.Collections.Generic;

namespace WindowsAutostartApi.Abstractions;

/// <summary>
/// Extended startup manager interface with Result pattern for detailed error reporting.
/// </summary>
public interface IStartupManagerEx
{
    /// <summary>
    /// Lists all startup entries. Returns an empty list if no entries found or access denied.
    /// </summary>
    OperationResult<IReadOnlyList<StartupEntry>> TryListAll();

    /// <summary>
    /// Checks if a startup entry exists.
    /// </summary>
    OperationResult<bool> TryExists(string name, StartupScope scope, StartupKind kind);

    /// <summary>
    /// Adds or updates a startup entry.
    /// </summary>
    OperationResult TryAdd(StartupEntry entry);

    /// <summary>
    /// Removes a startup entry.
    /// </summary>
    OperationResult TryRemove(string name, StartupScope scope, StartupKind kind);
}