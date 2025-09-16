namespace Wysg.Musm.MFCUIA.Abstractions;

public interface ICommandInvoker
{
    bool PostToFrame(IntPtr childHwnd, int commandId);
    bool ClickButton(IntPtr hwndButton);
}
