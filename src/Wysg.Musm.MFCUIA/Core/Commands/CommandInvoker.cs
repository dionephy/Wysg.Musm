using Wysg.Musm.MFCUIA.Abstractions;
using System;

namespace Wysg.Musm.MFCUIA.Core.Commands;

// Minimal Win32 interop required (previous reference to missing Core.Win32 namespace)
internal static class User32
{
    public const int GA_ROOT = 2;
    public const int WM_COMMAND = 0x0111;
    public const int BM_CLICK = 0x00F5;

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetAncestor(IntPtr hwnd, int flags);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    public static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}

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
