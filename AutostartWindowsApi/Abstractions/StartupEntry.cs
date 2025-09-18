namespace WindowsAutostartApi.Abstractions;

/// <summary>
/// Immutable description of a startup entry.
/// </summary>
public sealed record StartupEntry(
    string Name,
    string TargetPath,
    string? Arguments,
    StartupScope Scope,
    StartupKind Kind);
