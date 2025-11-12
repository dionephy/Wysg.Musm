using System;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// OperationExecutor partial class: System-level operations.
    /// Contains operations for mouse control, clipboard, keyboard simulation, and delays.
    /// </summary>
    internal static partial class OperationExecutor
    {
        #region System Operations

        private static (string preview, string? value) ExecuteMouseClick(string? xStr, string? yStr)
        {
            if (!int.TryParse(xStr, out var px) || !int.TryParse(yStr, out var py))
            {
                return ("(invalid coords)", null);
            }
            try
            {
                NativeMouseHelper.ClickScreen(px, py);
                return ($"(clicked {px},{py})", null);
            }
            catch { return ("(error)", null); }
        }

        private static (string preview, string? value) ExecuteSetClipboard(string? text)
        {
            if (text == null) { return ("(null)", null); }
            try
            {
                // Use STA thread for clipboard operations
                var sta = new System.Threading.Thread(() =>
                {
                    try { System.Windows.Clipboard.SetText(text); }
                    catch { }
                });
                sta.SetApartmentState(System.Threading.ApartmentState.STA);
                sta.Start();
                sta.Join(1000);
                return ($"(clipboard set, {text.Length} chars)", null);
            }
            catch (Exception ex) { return ($"(error: {ex.Message})", null); }
        }

        private static (string preview, string? value) ExecuteSimulateTab()
        {
            try
            {
                System.Windows.Forms.SendKeys.SendWait("{TAB}");
                return ("(Tab key sent)", null);
            }
            catch (Exception ex) { return ($"(error: {ex.Message})", null); }
        }

        private static (string preview, string? value) ExecuteSimulatePaste()
        {
            try
            {
                System.Windows.Forms.SendKeys.SendWait("^v");
                return ("(Ctrl+V sent)", null);
            }
            catch (Exception ex) { return ($"(error: {ex.Message})", null); }
        }

        private static (string preview, string? value) ExecuteSimulateSelectAll()
        {
            try
            {
                System.Windows.Forms.SendKeys.SendWait("^a");
                return ("(Ctrl+A sent)", null);
            }
            catch (Exception ex) { return ($"(error: {ex.Message})", null); }
        }

        private static (string preview, string? value) ExecuteSimulateDelete()
        {
            try
            {
                System.Windows.Forms.SendKeys.SendWait("{DELETE}");
                return ("(Delete key sent)", null);
            }
            catch (Exception ex) { return ($"(error: {ex.Message})", null); }
        }

        private static (string preview, string? value) ExecuteDelay(string? delayStr)
        {
            if (!int.TryParse(delayStr, out var delayMs) || delayMs < 0)
            {
                return ("(invalid delay)", null);
            }
            try
            {
                System.Threading.Thread.Sleep(delayMs);
                return ($"(delayed {delayMs} ms)", null);
            }
            catch (Exception ex) { return ($"(error: {ex.Message})", null); }
        }

        #endregion
    }
}
