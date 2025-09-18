using System.IO;

namespace WindowsAutostartApi.Utils;

internal static class PathHelpers
{
    /// <summary>
    /// Ensures a Windows-safe quoted path only when necessary.
    /// </summary>
    public static string QuoteIfNeeded(string path)
        => path.Contains(' ') && !path.StartsWith("\"") ? $"\"{path}\"" : path;

    /// <summary>
    /// Quick validation helper. You may choose to not enforce existence during Add().
    /// </summary>
    public static bool LooksLikeFilePath(string path)
        => path.IndexOfAny(Path.GetInvalidPathChars()) < 0;
}
