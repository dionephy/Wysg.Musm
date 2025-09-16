namespace Wysg.Musm.MFCUIA.Abstractions;

public interface IAutomationSession : IDisposable
{
    IntPtr ProcessHandle { get; }
}
