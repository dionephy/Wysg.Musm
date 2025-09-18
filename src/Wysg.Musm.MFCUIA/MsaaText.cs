using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Accessibility;

namespace Wysg.Musm.MFCUIA;

public static class MsaaStringReader
{
    private const uint OBJID_CLIENT = 0xFFFFFFFC;
    private const int CHILDID_SELF = 0;

    [DllImport("oleacc.dll")]
    private static extern int AccessibleObjectFromWindow(IntPtr hwnd, uint dwObjectID, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object? ppvObject);

    public static string[] GetAllStringsFromPane(IntPtr paneHwnd)
    {
        var list = new List<string>();
        if (paneHwnd == IntPtr.Zero) return list.ToArray();

        var iid = new Guid("618736E0-3C3D-11CF-810C-00AA00389B71"); // IID_IAccessible
        if (AccessibleObjectFromWindow(paneHwnd, OBJID_CLIENT, ref iid, out var obj) != 0 || obj is not IAccessible root)
            return list.ToArray();

        var q = new Queue<IAccessible>();
        q.Enqueue(root);
        while (q.Count > 0)
        {
            var acc = q.Dequeue();
            TryCollect(acc, CHILDID_SELF, list);

            int count = 0;
            try { count = acc.accChildCount; } catch { }
            for (int i = 1; i <= count; i++)
            {
                object? child = null;
                try { child = acc.get_accChild(i); } catch { }
                if (child == null) continue;

                if (child is IAccessible accChild)
                {
                    q.Enqueue(accChild);
                }
                else if (child is int id)
                {
                    TryCollect(acc, id, list);
                }
            }
        }

        var set = new HashSet<string>(StringComparer.Ordinal);
        var outList = new List<string>();
        foreach (var s in list)
        {
            var t = (s ?? string.Empty).Trim();
            if (t.Length == 0) continue;
            if (set.Add(t)) outList.Add(t);
        }
        return outList.ToArray();
    }

    private static void TryCollect(IAccessible acc, int childId, List<string> dest)
    {
        try { var n = acc.get_accName(childId); if (!string.IsNullOrWhiteSpace(n)) dest.Add(n); } catch { }
        try { var v = acc.get_accValue(childId); if (!string.IsNullOrWhiteSpace(v)) dest.Add(v); } catch { }
        try { var d = acc.get_accDescription(childId); if (!string.IsNullOrWhiteSpace(d)) dest.Add(d); } catch { }
    }
}

public sealed class MsaaTextWatcher : IDisposable
{
    private readonly IntPtr _rootPane;
    private readonly IntPtr _rootTop;
    private readonly Action<string[]> _onChange;
    private readonly WinEventDelegate _cb;
    private readonly IntPtr _hook;
    private int _pending;
    private readonly CancellationTokenSource _cts = new();

    private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    private const uint EVENT_OBJECT_SHOW = 0x8002;
    private const uint EVENT_OBJECT_HIDE = 0x8003;
    private const uint EVENT_OBJECT_NAMECHANGE = 0x800C;
    private const uint EVENT_OBJECT_VALUECHANGE = 0x800E;

    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    private const int GA_ROOT = 2;

    [DllImport("user32.dll")] private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
    [DllImport("user32.dll")] private static extern bool UnhookWinEvent(IntPtr hWinEventHook);
    [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr hWnd, int gaFlags);

    private MsaaTextWatcher(IntPtr rootPane, Action<string[]> onChange)
    {
        _rootPane = rootPane;
        _rootTop = GetAncestor(rootPane, GA_ROOT);
        _onChange = onChange;
        _cb = OnEvent;
        _hook = SetWinEventHook(EVENT_OBJECT_SHOW, EVENT_OBJECT_VALUECHANGE, IntPtr.Zero, _cb, 0, 0, WINEVENT_OUTOFCONTEXT);
    }

    public static IDisposable Start(IntPtr rootPane, Action<string[]> onChange) => new MsaaTextWatcher(rootPane, onChange);

    private void OnEvent(IntPtr hWinEventHook, uint evt, IntPtr hwnd, int idObject, int idChild, uint tid, uint time)
    {
        if (hwnd == IntPtr.Zero) return;
        var top = GetAncestor(hwnd, GA_ROOT);
        if (top != _rootTop) return;

        if (Interlocked.Exchange(ref _pending, 1) == 1) return; // debounce flood
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(100, _cts.Token); // small debounce window
                var arr = MsaaStringReader.GetAllStringsFromPane(_rootPane);
                _onChange(arr);
            }
            catch { }
            finally { Interlocked.Exchange(ref _pending, 0); }
        }, _cts.Token);
    }

    public void Dispose()
    {
        _cts.Cancel();
        if (_hook != IntPtr.Zero) UnhookWinEvent(_hook);
    }
}
