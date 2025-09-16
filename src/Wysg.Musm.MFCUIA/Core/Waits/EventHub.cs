using Wysg.Musm.MFCUIA.Core.Win32;

namespace Wysg.Musm.MFCUIA.Core.Waits;

public sealed class EventHub : IDisposable
{
    private readonly User32.WinEventDelegate _cb;
    private readonly IntPtr _hook;

    public event Action<IntPtr,uint,int,int,uint>? OnEvent;

    public EventHub()
    {
        _cb = (h,e,hwnd,objId,childId,tid,time) => OnEvent?.Invoke(hwnd,e,objId,childId,tid);
        _hook = User32.SetWinEventHook(User32.EVENT_MIN, User32.EVENT_MAX, IntPtr.Zero, _cb, 0, 0, User32.WINEVENT_OUTOFCONTEXT);
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero) User32.UnhookWinEvent(_hook);
    }
}
