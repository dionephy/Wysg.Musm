using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FlaUI.Core.AutomationElements;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// OperationExecutor partial class: UI element interaction operations.
    /// Contains operations for reading, clicking, focusing, and manipulating UI automation elements.
    /// </summary>
    internal static partial class OperationExecutor
    {
        #region Element Operations

        private static (string preview, string? value) ExecuteGetText(AutomationElement? el)
        {
            if (el == null) { return ("(no element)", null); }
            
            try
            {
                // Each property access wrapped individually to prevent exception propagation delays
                string name = string.Empty;
                try { name = el.Name ?? string.Empty; } catch { }
                
                string val = string.Empty;
                try { val = el.Patterns.Value.PatternOrDefault?.Value ?? string.Empty; } catch { }
                
                string legacy = string.Empty;
                try { legacy = el.Patterns.LegacyIAccessible.PatternOrDefault?.Name ?? string.Empty; } catch { }
                
                var raw = !string.IsNullOrEmpty(val) ? val : (!string.IsNullOrEmpty(name) ? name : legacy);
                var valueToStore = NormalizeKoreanMojibake(raw);
                return (valueToStore ?? "(null)", valueToStore);
            }
            catch 
            { 
                return ("(error)", null); 
            }
        }

        /// <summary>
        /// GetTextWait: Attempts to resolve element and wait up to 5 seconds for it to become visible.
        /// This is different from GetText which fails immediately if element is not found or not visible.
        /// 
        /// Implementation: This operation needs special handling in ProcedureExecutor to pass the
        /// bookmark/element argument for retry resolution instead of using the standard pre-resolved element.
        /// </summary>
        private static (string preview, string? value) ExecuteGetTextWait(AutomationElement? el)
        {
            // Note: This implementation receives a pre-resolved element from OperationExecutor
            // For the wait functionality to work properly, we need the element resolution to be
            // part of the retry loop. See ExecuteGetTextWaitWithRetry for the proper implementation.
            
            if (el == null) { return ("(no element)", null); }
            
            try
            {
                // Wait up to 5 seconds for element to be visible
                var maxWaitMs = 5000;
                var intervalMs = 200;
                var elapsedMs = 0;
                
                while (elapsedMs < maxWaitMs)
                {
                    // Check if element is visible
                    try
                    {
                        var r = el.BoundingRectangle;
                        if (r.Width > 0 && r.Height > 0)
                        {
                            // Element is visible, proceed with GetText
                            Debug.WriteLine($"[GetTextWait] Element became visible after {elapsedMs}ms");
                            return ExecuteGetText(el);
                        }
                    }
                    catch
                    {
                        // Element might be stale or inaccessible, keep waiting
                    }
                    
                    // Wait before next check
                    System.Threading.Thread.Sleep(intervalMs);
                    elapsedMs += intervalMs;
                }
                
                // Timeout - element never became visible
                Debug.WriteLine($"[GetTextWait] Timeout after {maxWaitMs}ms - element not visible");
                return ("(timeout - not visible)", null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GetTextWait] Exception: {ex.Message}");
                return ("(error)", null);
            }
        }

        /// <summary>
        /// GetTextWaitWithRetry: Specialized version that handles element resolution with retry.
        /// This is called from OperationExecutor with a resolution function instead of a pre-resolved element.
        /// </summary>
        internal static (string preview, string? value) ExecuteGetTextWaitWithRetry(
            Func<AutomationElement?> resolveElement)
        {
            var maxWaitMs = 5000;
            var intervalMs = 200;
            var elapsedMs = 0;
            
            Debug.WriteLine($"[GetTextWaitWithRetry] Starting retry loop (max {maxWaitMs}ms, interval {intervalMs}ms)");
            
            while (elapsedMs < maxWaitMs)
            {
                try
                {
                    // Attempt to resolve element
                    Debug.WriteLine($"[GetTextWaitWithRetry] Attempt at {elapsedMs}ms - resolving element...");
                    var el = resolveElement();
                    
                    if (el != null)
                    {
                        Debug.WriteLine($"[GetTextWaitWithRetry] Element resolved at {elapsedMs}ms");
                        
                        // Check if element is visible
                        try
                        {
                            var r = el.BoundingRectangle;
                            if (r.Width > 0 && r.Height > 0)
                            {
                                // Element is visible, get text
                                Debug.WriteLine($"[GetTextWaitWithRetry] Element visible after {elapsedMs}ms, getting text");
                                var result = ExecuteGetText(el);
                                Debug.WriteLine($"[GetTextWaitWithRetry] SUCCESS: Got text '{result.value}' after {elapsedMs}ms");
                                return result;
                            }
                            else
                            {
                                Debug.WriteLine($"[GetTextWaitWithRetry] Element resolved but not visible (width={r.Width}, height={r.Height})");
                            }
                        }
                        catch (Exception visEx)
                        {
                            Debug.WriteLine($"[GetTextWaitWithRetry] Element resolved but visibility check failed: {visEx.Message}");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"[GetTextWaitWithRetry] Element resolution returned null at {elapsedMs}ms");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[GetTextWaitWithRetry] Exception during resolution attempt at {elapsedMs}ms: {ex.Message}");
                }
                
                // Wait before next attempt
                System.Threading.Thread.Sleep(intervalMs);
                elapsedMs += intervalMs;
            }
            
            // Timeout - element never became visible
            Debug.WriteLine($"[GetTextWaitWithRetry] TIMEOUT after {maxWaitMs}ms - element not found or not visible");
            return ("(timeout - not visible)", null);
        }

        private static (string preview, string? value) ExecuteGetName(AutomationElement? el)
        {
            if (el == null) { return ("(no element)", null); }
            
            try
            {
                string raw = string.Empty;
                try { raw = el.Name ?? string.Empty; } catch { }
                
                var valueToStore = NormalizeKoreanMojibake(raw);
                return (string.IsNullOrEmpty(valueToStore) ? "(empty)" : valueToStore, valueToStore);
            }
            catch 
            { 
                return ("(error)", null); 
            }
        }

        private static (string preview, string? value) ExecuteGetTextOCR(AutomationElement? el)
        {
            if (el == null) { return ("(no element)", null); }
            try
            {
                var r = el.BoundingRectangle;
                if (r.Width <= 0 || r.Height <= 0) { return ("(no bounds)", null); }
                var hwnd = new IntPtr(el.Properties.NativeWindowHandle.Value);
                if (hwnd == IntPtr.Zero) { return ("(no hwnd)", null); }
                
                // Capture only top 40 pixels of the element
                var captureHeight = Math.Min(40, (int)r.Height);
                var (engine, text) = Wysg.Musm.MFCUIA.OcrReader.OcrTryReadRegionDetailedAsync(hwnd, new System.Drawing.Rectangle(22, 0, (int)r.Width-22, captureHeight)).ConfigureAwait(false).GetAwaiter().GetResult();
                if (!engine) { return ("(ocr unavailable)", null); }
                return (string.IsNullOrWhiteSpace(text) ? "(empty)" : text!, text);
            }
            catch { return ("(error)", null); }
        }

        private static async Task<(string preview, string? value)> ExecuteGetTextOCRAsync(AutomationElement? el)
        {
            if (el == null) { return ("(no element)", null); }
            try
            {
                var r = el.BoundingRectangle;
                if (r.Width <= 0 || r.Height <= 0) { return ("(no bounds)", null); }
                var hwnd = new IntPtr(el.Properties.NativeWindowHandle.Value);
                if (hwnd == IntPtr.Zero) { return ("(no hwnd)", null); }
                
                // Capture only top 40 pixels of the element
                var captureHeight = Math.Min(40, (int)r.Height);
                var (engine, text) = await Wysg.Musm.MFCUIA.OcrReader.OcrTryReadRegionDetailedAsync(hwnd, new System.Drawing.Rectangle(22, 0, (int)r.Width-22, captureHeight));
                if (!engine) { return ("(ocr unavailable)", null); }
                return (string.IsNullOrWhiteSpace(text) ? "(empty)" : text!, text);
            }
            catch { return ("(error)", null); }
        }

        private static (string preview, string? value) ExecuteInvoke(AutomationElement? el)
        {
            if (el == null) { return ("(no element)", null); }
            try
            {
                var inv = el.Patterns.Invoke.PatternOrDefault;
                if (inv != null)
                    inv.Invoke();
                else
                    el.Patterns.Toggle.PatternOrDefault?.Toggle();
                return ("(invoked)", null);
            }
            catch { return ("(error)", null); }
        }

        private static (string preview, string? value) ExecuteSetFocus(AutomationElement? el)
        {
            if (el == null)
            {
                Debug.WriteLine("[SetFocus] FAIL: Element resolution returned null");
                return ("(no element)", null);
            }

            Debug.WriteLine($"[SetFocus] Element resolved: Name='{el.Name}', AutomationId='{el.AutomationId}'");

            // Execute SetFocus logic on UI thread asynchronously
            string resultPreview = "(error)";
            var completionSource = new System.Threading.Tasks.TaskCompletionSource<string>();

            try
            {
                Debug.WriteLine("[SetFocus] Queuing BeginInvoke on Dispatcher...");

                System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Debug.WriteLine("[SetFocus] BeginInvoke callback START - executing on UI thread");

                    try
                    {
                        // 1. Get window handle (MUST be done on UI thread)
                        Debug.WriteLine("[SetFocus] Attempting to get window handle...");
                        var hwnd = new IntPtr(el.Properties.NativeWindowHandle.Value);
                        Debug.WriteLine($"[SetFocus] Window handle: 0x{hwnd.ToInt64():X}");

                        // 2. Activate containing window first
                        if (hwnd != IntPtr.Zero)
                        {
                            Debug.WriteLine("[SetFocus] Calling SetForegroundWindow...");
                            var activated = NativeMouseHelper.SetForegroundWindow(hwnd);
                            Debug.WriteLine($"[SetFocus] SetForegroundWindow result: {activated}");

                            Debug.WriteLine("[SetFocus] Sleeping 100ms for window activation...");
                            System.Threading.Thread.Sleep(100);
                        }

                        // 3. Retry focus with delays
                        const int maxAttempts = 3;
                        const int retryDelayMs = 150;
                        Exception? lastException = null;

                        for (int attempt = 1; attempt <= maxAttempts; attempt++)
                        {
                            Debug.WriteLine($"[SetFocus] Focus attempt {attempt}/{maxAttempts}...");
                            try
                            {
                                Debug.WriteLine($"[SetFocus] Calling el.Focus() on element '{el.Name}'...");
                                el.Focus();
                                Debug.WriteLine($"[SetFocus] SUCCESS: Focus() completed on attempt {attempt}");
                                resultPreview = attempt > 1 ? $"(focused after {attempt} attempts)" : "(focused)";
                                completionSource.SetResult(resultPreview);
                                return;
                            }
                            catch (Exception ex)
                            {
                                lastException = ex;
                                Debug.WriteLine($"[SetFocus] Attempt {attempt} FAILED: {ex.GetType().Name}");
                                Debug.WriteLine($"[SetFocus] Exception message: {ex.Message}");

                                if (attempt < maxAttempts)
                                {
                                    Debug.WriteLine($"[SetFocus] Sleeping {retryDelayMs}ms before retry...");
                                    System.Threading.Thread.Sleep(retryDelayMs);
                                }
                            }
                        }

                        // All attempts failed
                        resultPreview = $"(error after {maxAttempts} attempts: {lastException?.Message})";
                        completionSource.SetResult(resultPreview);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SetFocus] Dispatcher execution EXCEPTION: {ex.GetType().Name}");
                        Debug.WriteLine($"[SetFocus] Exception message: {ex.Message}");
                        resultPreview = $"(error: {ex.Message})";
                        completionSource.SetResult(resultPreview);
                    }

                    Debug.WriteLine("[SetFocus] BeginInvoke callback END");
                }));

                Debug.WriteLine("[SetFocus] BeginInvoke queued, waiting for completion...");

                // Wait for completion with timeout
                var task = completionSource.Task;
                if (task.Wait(TimeSpan.FromSeconds(5)))
                {
                    var result = task.Result;
                    Debug.WriteLine($"[SetFocus] Task completed: preview='{result}'");
                    return (result, null);
                }
                else
                {
                    Debug.WriteLine("[SetFocus] TIMEOUT: Task did not complete within 5 seconds");
                    return ("(timeout waiting for UI thread)", null);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SetFocus] Outer try/catch EXCEPTION: {ex.GetType().Name}");
                Debug.WriteLine($"[SetFocus] Exception message: {ex.Message}");
                return ($"(dispatcher error: {ex.Message})", null);
            }
        }

        private static (string preview, string? value) ExecuteClickElement(AutomationElement? el, bool restoreCursor)
        {
            if (el == null) { return ("(no element)", null); }
            try
            {
                var r = el.BoundingRectangle;
                if (r.Width <= 0 || r.Height <= 0) { return ("(no bounds)", null); }
                int cx = (int)(r.Left + r.Width / 2);
                int cy = (int)(r.Top + r.Height / 2);

                if (restoreCursor)
                {
                    NativeMouseHelper.ClickScreenWithRestore(cx, cy);
                    return ($"(clicked element center {cx},{cy})", null);
                }
                else
                {
                    NativeMouseHelper.ClickScreen(cx, cy);
                    return ($"(clicked and stayed at {cx},{cy})", null);
                }
            }
            catch { return ("(error)", null); }
        }

        private static (string preview, string? value) ExecuteMouseMoveToElement(AutomationElement? el)
        {
            if (el == null) { return ("(no element)", null); }
            try
            {
                var r = el.BoundingRectangle;
                if (r.Width <= 0 || r.Height <= 0) { return ("(no bounds)", null); }
                int cx = (int)(r.Left + r.Width / 2);
                int cy = (int)(r.Top + r.Height / 2);
                NativeMouseHelper.SetCursorPos(cx, cy);
                return ($"(moved to element center {cx},{cy})", null);
            }
            catch { return ("(error)", null); }
        }

        private static (string preview, string? value) ExecuteIsVisible(AutomationElement? el)
        {
            if (el == null) { return ("false", "false"); }
            try
            {
                var r = el.BoundingRectangle;
                bool isVisible = r.Width > 0 && r.Height > 0;
                string result = isVisible ? "true" : "false";
                return (result, result);
            }
            catch
            {
                return ("false", "false");
            }
        }

        private static (string preview, string? value) ExecuteGetValueFromSelection(AutomationElement? el, string headerWanted)
        {
            if (string.IsNullOrWhiteSpace(headerWanted)) headerWanted = "ID";
            if (el == null) { return ("(no element)", null); }
            try
            {
                var selection = el.Patterns.Selection.PatternOrDefault;
                var selected = selection?.Selection?.Value ?? Array.Empty<AutomationElement>();
                if (selected.Length == 0)
                {
                    selected = el.FindAllDescendants().Where(a =>
                    {
                        try { return a.Patterns.SelectionItem.IsSupported && a.Patterns.SelectionItem.PatternOrDefault?.IsSelected == true; }
                        catch { return false; }
                    }).ToArray();
                }
                if (selected.Length == 0) { return ("(no selection)", null); }
                var rowEl = selected[0];
                var headers = GetHeaderTexts(el);
                var cells = GetRowCellValues(rowEl).Select(NormalizeKoreanMojibake).ToList();
                if (headers.Count < cells.Count) for (int j = headers.Count; j < cells.Count; j++) headers.Add($"Col{j + 1}");
                else if (headers.Count > cells.Count) for (int j = cells.Count; j < headers.Count; j++) cells.Add(string.Empty);
                string? matched = null;
                for (int j = 0; j < headers.Count; j++)
                {
                    var hNorm = NormalizeHeader(headers[j]);
                    if (string.Equals(hNorm, headerWanted, StringComparison.OrdinalIgnoreCase)) { matched = cells[j]; break; }
                }
                if (matched == null)
                {
                    for (int j = 0; j < headers.Count; j++)
                    {
                        var hNorm = NormalizeHeader(headers[j]);
                        if (hNorm.IndexOf(headerWanted, StringComparison.OrdinalIgnoreCase) >= 0) { matched = cells[j]; break; }
                    }
                }
                if (matched == null) { return ($"({headerWanted} not found)", null); }
                return (matched, matched);
            }
            catch { return ("(error)", null); }
        }

        private static (string preview, string? value) ExecuteGetSelectedElement(AutomationElement? listEl, Dictionary<string, AutomationElement>? elementCache)
        {
            if (listEl == null) { return ("(element not resolved)", null); }

            try
            {
                var selection = listEl.Patterns.Selection.PatternOrDefault;
                var selected = selection?.Selection?.Value ?? Array.Empty<AutomationElement>();
                if (selected.Length == 0)
                {
                    selected = listEl.FindAllDescendants().Where(a =>
                    {
                        try { return a.Patterns.SelectionItem.IsSupported && a.Patterns.SelectionItem.PatternOrDefault?.IsSelected == true; }
                        catch { return false; }
                    }).ToArray();
                }

                if (selected.Length == 0) { return ("(no selection)", null); }

                var selectedRow = selected[0];
                var elName = string.IsNullOrWhiteSpace(selectedRow.Name) ? "(no name)" : selectedRow.Name;
                var elAutoId = selectedRow.AutomationId ?? "(no automationId)";
                var preview = $"(element: {elName}, automationId: {elAutoId})";

                // Store element in cache if provided
                var cacheKey = $"SelectedElement:{selectedRow.Name}";
                if (elementCache != null)
                {
                    elementCache[cacheKey] = selectedRow;
                }

                return (preview, cacheKey);
            }
            catch (Exception ex)
            {
                return ($"(error: {ex.Message})", null);
            }
        }

        #endregion
    }
}
