using System.Diagnostics;
using Wysg.Musm.MFCUIA.Abstractions;
using Wysg.Musm.MFCUIA.Core.Commands;
using Wysg.Musm.MFCUIA.Core.Controls;
using Wysg.Musm.MFCUIA.Core.Discovery;
using Wysg.Musm.MFCUIA.Core.Waits;
using Wysg.Musm.MFCUIA.Selectors;

namespace Wysg.Musm.MFCUIA.Session;

public sealed class MfcUi : IDisposable
{
    private readonly HwndLocator _locator = new();
    private readonly CommandInvoker _commands = new();
    private readonly EventHub _events = new();

    private MfcUi(Process proc) { Process = proc; }

    public Process Process { get; }

    public static MfcUi Attach(string processName)
    {
        var proc = Process.GetProcessesByName(processName).FirstOrDefault()
                   ?? throw new InvalidOperationException($"Process '{processName}' not found.");
        return new MfcUi(proc);
    }

    // New: attach by discovering a top-level window matching a predicate (title/class)
    public static MfcUi AttachByTopLevel(Func<WindowSnapshot, bool> match)
    {
        var loc = new HwndLocator();
        foreach (var h in loc.EnumTopLevelWindows())
        {
            var snap = HwndLocator.Snapshot(h);
            if (!match(snap)) continue;
            Win32Helpers.GetWindowThreadProcessId(h, out int pid);
            var proc = Process.GetProcessById(pid);
            return new MfcUi(proc);
        }
        throw new InvalidOperationException("No matching window found for discovery attach.");
    }

    public IntPtr Window(Func<WindowQuery, WindowQuery> q)
        => _locator.FindTopLevel(q(new WindowQuery()));

    public ElementHandle Find(ISelector selector)
    {
        var top = Window(w => w.Any());
        var hwnd = top == IntPtr.Zero ? IntPtr.Zero : _locator.FindAnyDescendant(top, selector);
        return new ElementHandle(this, hwnd);
    }

    // New: search across any top-level window of the process
    public ElementHandle FindInAnyTop(ISelector selector)
    {
        foreach (var h in _locator.EnumTopLevelWindows())
        {
            Win32Helpers.GetWindowThreadProcessId(h, out int pid);
            if (pid != Process.Id) continue;
            var match = _locator.FindAnyDescendant(h, selector);
            if (match != IntPtr.Zero) return new ElementHandle(this, match);
        }
        return new ElementHandle(this, IntPtr.Zero);
    }

    public CommandHandle Command(int id) => new(this, id);

    public ListViewAdapter AsListView(IntPtr hwnd) => new(hwnd);

    public Wait Wait => new(_events);

    internal bool PostCommandToTop(int id)
    {
        var top = GetTopWindowOfProcess() ?? IntPtr.Zero;
        return top != IntPtr.Zero && _commands.PostToFrame(top, id);
    }

    internal IntPtr? GetTopWindowOfProcess()
    {
        foreach (var h in _locator.EnumTopLevelWindows())
        {
            try
            {
                var pid = 0;
                _ = Win32Helpers.GetWindowThreadProcessId(h, out pid);
                if (pid == Process.Id) return h;
            }
            catch { }
        }
        return null;
    }

    public void Dispose() => _events.Dispose();
}

public readonly struct CommandHandle
{
    private readonly MfcUi _ui;
    private readonly int _id;
    public CommandHandle(MfcUi ui, int id) { _ui = ui; _id = id; }
    public bool Invoke() => _ui.PostCommandToTop(_id);
}

public readonly struct ElementHandle
{
    private readonly MfcUi _ui;
    public IntPtr Hwnd { get; }
    internal ElementHandle(MfcUi ui, IntPtr hwnd) { _ui = ui; Hwnd = hwnd; }
    public ListViewAdapter AsListView() => _ui.AsListView(Hwnd);
}
