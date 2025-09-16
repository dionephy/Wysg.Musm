namespace Wysg.Musm.MFCUIA.Abstractions;

public interface ISelector
{
    bool Matches(IntPtr hwnd, WindowSnapshot s);
}

public readonly record struct WindowSnapshot(string? ClassName, string? Title, System.Drawing.Rectangle Bounds);
