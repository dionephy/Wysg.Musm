using Wysg.Musm.MFCUIA.Abstractions;
using Wysg.Musm.MFCUIA.Core.Win32;

namespace Wysg.Musm.MFCUIA.Core.Commands;

public sealed class CommandInvoker : ICommandInvoker
{
    public bool PostToFrame(IntPtr childHwnd, int commandId)
    {
        var frame = User32.GetAncestor(childHwnd, User32.GA_ROOT);
        return frame != IntPtr.Zero && User32.PostMessage(frame, User32.WM_COMMAND, (IntPtr)commandId, childHwnd);
    }

    public bool ClickButton(IntPtr hwndButton)
        => User32.SendMessage(hwndButton, User32.BM_CLICK, IntPtr.Zero, IntPtr.Zero) != IntPtr.Zero;
}
