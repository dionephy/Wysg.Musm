using System;
using System.Runtime.InteropServices;

namespace Wysg.Musm.MFCUIA.Core.Waits;

internal static class User32Events
{
    public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    [DllImport("user32.dll")] public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
    [DllImport("user32.dll")] public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    public const uint EVENT_MIN = 0x00000001;
    public const uint EVENT_MAX = 0x7FFFFFFF;
    public const uint WINEVENT_OUTOFCONTEXT = 0x0000;
}

public sealed class EventHub : IDisposable
{
    private readonly User32Events.WinEventDelegate _cb;
    private readonly IntPtr _hook;

    public event Action<IntPtr,uint,int,int,uint>? OnEvent;

    public EventHub()
    {
        _cb = (h,e,hwnd,objId,childId,tid,time) => OnEvent?.Invoke(hwnd,e,objId,childId,tid);
        _hook = User32Events.SetWinEventHook(User32Events.EVENT_MIN, User32Events.EVENT_MAX, IntPtr.Zero, _cb, 0, 0, User32Events.WINEVENT_OUTOFCONTEXT);
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero) User32Events.UnhookWinEvent(_hook);
    }
}
