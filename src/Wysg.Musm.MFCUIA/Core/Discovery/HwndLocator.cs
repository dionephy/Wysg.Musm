using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Wysg.Musm.MFCUIA.Abstractions;
using Wysg.Musm.MFCUIA.Core.Win32;

namespace Wysg.Musm.MFCUIA.Core.Discovery;

public sealed class HwndLocator
{
    public IntPtr[] EnumTopLevelWindows()
    {
        var list = new List<IntPtr>();
        EnumWindows((h, p) => { list.Add(h); return true; }, IntPtr.Zero);
        return list.ToArray();
    }

    public IntPtr FindTopLevel(WindowQuery q)
    {
        foreach (var h in EnumTopLevelWindows())
        {
            var snap = Snapshot(h);
            if (q.Matches(h, snap)) return h;
        }
        return IntPtr.Zero;
    }

    public IntPtr FindChild(IntPtr parent, ISelector sel)
    {
        IntPtr match = IntPtr.Zero;
        EnumChildWindows(parent, (h, p) =>
        {
            if (sel.Matches(h, Snapshot(h))) { match = h; return false; }
            return true;
        }, IntPtr.Zero);
        return match;
    }

    public IntPtr FindAnyDescendant(IntPtr parent, ISelector sel)
    {
        IntPtr match = IntPtr.Zero;
        EnumChildWindows(parent, (h, p) =>
        {
            if (sel.Matches(h, Snapshot(h))) { match = h; return false; }
            // recurse
            var child = FindChild(h, sel);
            if (child != IntPtr.Zero) { match = child; return false; }
            return true;
        }, IntPtr.Zero);
        return match;
    }

    public static WindowSnapshot Snapshot(IntPtr hwnd)
    {
        var cls = new StringBuilder(256);
        _ = User32.GetClassName(hwnd, cls, cls.Capacity);
        var title = new StringBuilder(512);
        _ = User32.GetWindowText(hwnd, title, title.Capacity);
        _ = User32.GetWindowRect(hwnd, out var r);
        return new WindowSnapshot(cls.ToString(), title.ToString(), new System.Drawing.Rectangle(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top));
    }

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);
}

public sealed class WindowQuery
{
    private readonly List<Func<IntPtr, WindowSnapshot, bool>> _preds = new();

    public WindowQuery TitleContains(string part)
    {
        _preds.Add((h,s) => !string.IsNullOrEmpty(s.Title) && s.Title!.Contains(part, StringComparison.OrdinalIgnoreCase));
        return this;
    }
    public WindowQuery Class(string cls)
    {
        _preds.Add((h,s) => string.Equals(s.ClassName, cls, StringComparison.OrdinalIgnoreCase));
        return this;
    }
    public WindowQuery Any() { _preds.Add((h,s) => true); return this; }

    internal bool Matches(IntPtr hwnd, WindowSnapshot s) => _preds.All(p => p(hwnd, s));
}
