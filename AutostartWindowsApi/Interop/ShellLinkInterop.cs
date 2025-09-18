using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowsAutostartApi.Interop;

/// <summary>
/// Minimal COM interop for creating and resolving .lnk shortcuts via IShellLinkW.
/// </summary>
internal static class ShellLinkInterop
{
    private const int MAX_PATH = 260;

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink { }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int iIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0000010b-0000-0000-C000-000000000046")]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        void IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }

    /// <summary>
    /// Create or update a .lnk pointing to target with optional args.
    /// </summary>
    public static void CreateShortcut(string lnkPath, string target, string? args)
    {
        var link = (IShellLinkW)new ShellLink();
        link.SetPath(target);
        link.SetArguments(args ?? string.Empty);

        var workingDir = System.IO.Path.GetDirectoryName(target);
        if (!string.IsNullOrWhiteSpace(workingDir))
            link.SetWorkingDirectory(workingDir!);

        var pf = (IPersistFile)link;
        pf.Save(lnkPath, true);
    }

    /// <summary>
    /// Try to resolve a .lnk to target path and arguments.
    /// </summary>
    public static bool TryResolveShortcut(string lnkPath, out string? target, out string? args)
    {
        target = null;
        args = null;

        var link = (IShellLinkW)new ShellLink();
        var pf = (IPersistFile)link;
        pf.Load(lnkPath, 0);

        var sbPath = new StringBuilder(MAX_PATH);
        link.GetPath(sbPath, sbPath.Capacity, IntPtr.Zero, 0);
        var path = sbPath.ToString();

        var sbArgs = new StringBuilder(MAX_PATH);
        link.GetArguments(sbArgs, sbArgs.Capacity);
        var a = sbArgs.ToString();

        if (!string.IsNullOrWhiteSpace(path))
        {
            target = path;
            args = string.IsNullOrWhiteSpace(a) ? null : a;
            return true;
        }
        return false;
    }
}
