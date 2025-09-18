using System;
using System.IO;
using System.Linq;

namespace WindowsAutostartApi.Utils;

public static class PathHelpers
{
    /// <summary>
    /// Ensures a Windows-safe quoted path only when necessary.
    /// </summary>
    public static string QuoteIfNeeded(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        // Already quoted properly
        if (path.StartsWith("\"") && path.EndsWith("\"") && path.Length > 2)
            return path;

        // Quote if contains spaces or special characters
        if (path.Contains(' ') || path.Contains('&') || path.Contains('^'))
            return $"\"{path}\"";

        return path;
    }

    private const int MaxPathLength = 260; // MAX_PATH on Windows

    /// <summary>
    /// Enhanced path validation with security checks.
    /// </summary>
    public static bool IsValidPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        // Remove quotes for validation
        var cleanPath = path.Trim('\"');

        // Check length
        if (cleanPath.Length > MaxPathLength)
            return false;

        // Check for invalid characters
        if (cleanPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            return false;

        // Check for reserved names (CON, PRN, AUX, etc.)
        var fileName = Path.GetFileNameWithoutExtension(cleanPath)?.ToUpperInvariant();
        if (IsReservedName(fileName))
            return false;

        // Basic structure validation
        try
        {
            Path.GetFullPath(cleanPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Quick validation helper for backward compatibility.
    /// </summary>
    public static bool LooksLikeFilePath(string path)
        => IsValidPath(path);

    /// <summary>
    /// Validates startup entry name for registry safety.
    /// </summary>
    public static bool IsValidEntryName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        // Registry value names have some restrictions
        var invalidChars = new[] { '\\', '/', ':', '*', '?', '\"', '<', '>', '|' };
        return !name.Any(c => invalidChars.Contains(c)) && name.Length <= 255;
    }

    private static bool IsReservedName(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        var reservedNames = new[] {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        return reservedNames.Contains(name.ToUpperInvariant());
    }
}
