using System;
using System.Threading;
using Wysg.Musm.MFCUIA.Core.Discovery;

namespace Wysg.Musm.MFCUIA.Core.Waits;

public sealed class Wait
{
    private readonly EventHub _hub;
    public Wait(EventHub hub) { _hub = hub; }

    public bool UntilWindow(Func<WindowQuery, WindowQuery> q, int timeoutMs = 3000)
    {
        using var ev = new ManualResetEventSlim(false);
        void Handler(IntPtr hwnd, uint evId, int obj, int child, uint tid)
        {
            const uint EVENT_OBJECT_SHOW = 0x8002;
            if (evId == EVENT_OBJECT_SHOW && WindowQueryMatch(hwnd, q(new WindowQuery())))
                ev.Set();
        }
        _hub.OnEvent += Handler;
        var ok = ev.Wait(timeoutMs);
        _hub.OnEvent -= Handler;
        return ok;
    }

    private static bool WindowQueryMatch(IntPtr hwnd, WindowQuery query)
    {
        var snap = HwndLocator.Snapshot(hwnd);
        return query.Matches(hwnd, snap);
    }
}
