namespace Wysg.Musm.MFCUIA.Abstractions;

public interface IWindow
{
    IntPtr Hwnd { get; }
    string? Title { get; }
}
