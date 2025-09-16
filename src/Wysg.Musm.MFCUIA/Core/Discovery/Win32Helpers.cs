using System.Runtime.InteropServices;

namespace Wysg.Musm.MFCUIA.Core.Discovery;

internal static class Win32Helpers
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
}
