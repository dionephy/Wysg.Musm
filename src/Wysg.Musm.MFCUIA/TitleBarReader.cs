using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Wysg.Musm.MFCUIA;

/// <summary>
/// Helpers to retrieve a window's visible title text via raw Win32 (GetWindowText).
/// </summary>
public static class TitleBarReader
{
    private const int GA_ROOT = 2;

    [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr hWnd, int gaFlags);
    [DllImport("user32.dll")] private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    public static string? TryGetTitleFromPaneHwnd(IntPtr paneHwnd)
    {
        if (paneHwnd == IntPtr.Zero) return null;
        var top = GetAncestor(paneHwnd, GA_ROOT);
        return TryGetTitleFromTop(top);
    }

    public static string? TryGetTitleFromTop(IntPtr top)
    {
        if (top == IntPtr.Zero) return null;
        var sb = new StringBuilder(1024);
        var len = GetWindowText(top, sb, sb.Capacity);
        return len > 0 ? sb.ToString() : null;
    }

    /// <summary>
    /// Finds any top-level window belonging to the given process and returns the first non-empty title.
    /// </summary>
    public static string? TryGetAnyProcessWindowTitle(int processId)
    {
        string? found = null;
        EnumWindows((h, l) =>
        {
            GetWindowThreadProcessId(h, out int pid);
            if (pid != processId) return true;
            var t = TryGetTitleFromTop(h);
            if (!string.IsNullOrWhiteSpace(t)) { found = t; return false; }
            return true;
        }, IntPtr.Zero);
        return found;
    }
}
