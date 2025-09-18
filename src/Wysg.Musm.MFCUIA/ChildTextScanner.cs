using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Wysg.Musm.MFCUIA;

public static class ChildTextScanner
{
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);
    internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, StringBuilder lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    private const uint WM_GETTEXT = 0x000D;
    private const uint SMTO_ABORTIFHUNG = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    public static string? TryReadBannerFromPane(IntPtr paneHwnd, int topStripPx = 120)
    {
        if (paneHwnd == IntPtr.Zero) return null;

        var paneRectOk = GetWindowRect(paneHwnd, out var paneRect);
        int paneTop = paneRectOk ? paneRect.Top : int.MinValue;

        var bestText = (score: -1, text: (string?)null);

        foreach (var ch in EnumerateDescendantsBreadthFirst(paneHwnd))
        {
            if (!GetWindowRect(ch, out var r)) continue;
            if (paneRectOk)
            {
                if (r.Top > paneTop + topStripPx) continue;
                int width = Math.Max(0, r.Right - r.Left);
                if (width < 80) continue;
            }

            var text = ReadWindowText(ch);
            if (string.IsNullOrWhiteSpace(text)) continue;

            var cls = GetClass(ch) ?? string.Empty;
            int score = ScoreCandidate(text, cls);
            if (score > bestText.score)
                bestText = (score, text.Trim());
        }

        return bestText.text;
    }

    private static IEnumerable<IntPtr> EnumerateDescendantsBreadthFirst(IntPtr root)
    {
        var q = new Queue<IntPtr>();
        q.Enqueue(root);
        while (q.Count > 0)
        {
            var p = q.Dequeue();
            var children = new List<IntPtr>();
            EnumChildWindows(p, (h, l) => { children.Add(h); return true; }, IntPtr.Zero);
            foreach (var c in children)
            {
                yield return c;
                q.Enqueue(c);
            }
        }
    }

    private static string ReadWindowText(IntPtr h)
    {
        var sb = new StringBuilder(1024);
        int len = GetWindowText(h, sb, sb.Capacity);
        if (len > 0) return sb.ToString();

        // Fallback with WM_GETTEXT using timeout to avoid hangs in owner-drawn controls
        sb.Clear(); sb.EnsureCapacity(1024);
        IntPtr res;
        _ = SendMessageTimeout(h, WM_GETTEXT, (IntPtr)sb.Capacity, sb, SMTO_ABORTIFHUNG, 50, out res);
        return sb.Length > 0 ? sb.ToString() : string.Empty;
    }

    private static string? GetClass(IntPtr h)
    {
        var sb = new StringBuilder(256);
        int len = GetClassName(h, sb, sb.Capacity);
        return len > 0 ? sb.ToString() : null;
    }

    private static int ScoreCandidate(string text, string cls)
    {
        int s = 0;
        s += Math.Min(text.Length, 200);
        if (text.Contains("MR", StringComparison.OrdinalIgnoreCase)) s += 50;
        if (text.Contains(", M", StringComparison.OrdinalIgnoreCase) || text.Contains(", F", StringComparison.OrdinalIgnoreCase)) s += 20;
        if (!string.IsNullOrEmpty(cls))
        {
            if (cls.Contains("Static", StringComparison.OrdinalIgnoreCase)) s += 15;
            if (cls.Contains("Afx", StringComparison.OrdinalIgnoreCase)) s += 10;
        }
        int commas = 0; foreach (var ch in text) if (ch == ',') commas++;
        s += Math.Min(commas * 5, 30);
        return s;
    }
}
