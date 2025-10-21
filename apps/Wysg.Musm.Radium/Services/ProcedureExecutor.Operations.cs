using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using FlaUI.Core.AutomationElements;

namespace Wysg.Musm.Radium.Services
{
    internal static partial class ProcedureExecutor
    {
        private static readonly HttpClient _http = new();
        private static bool _encProviderRegistered;

        private static void EnsureEncodingProviders()
        {
            if (_encProviderRegistered) return;
            try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); } catch { }
            _encProviderRegistered = true;
        }

        private static string UnescapeUserText(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            try { return Regex.Unescape(s); } catch { return s; }
        }

        private static (string preview, string? value) ExecuteRow(ProcOpRow row, Dictionary<string, string?> vars)
        {
            string? valueToStore = null;
            string preview;
            switch (row.Op)
            {
                case "Split":
                {
                    var input = ResolveString(row.Arg1, vars);
                    var sepRaw = ResolveString(row.Arg2, vars) ?? string.Empty;
                    var indexStr = ResolveString(row.Arg3, vars);
                    
                    // DIAGNOSTIC: Log separator details
                    Debug.WriteLine($"[Split] Input length: {input?.Length ?? 0}");
                    Debug.WriteLine($"[Split] SepRaw: '{sepRaw}' (length: {sepRaw.Length}, bytes: {string.Join(" ", System.Text.Encoding.UTF8.GetBytes(sepRaw).Select(b => b.ToString("X2")))})");
                    Debug.WriteLine($"[Split] Input contains separator: {input?.Contains(sepRaw) ?? false}");
                    
                    if (input == null) { return ("(null)", null); }

                    string[] parts;
                    if (sepRaw.StartsWith("re:", StringComparison.OrdinalIgnoreCase) || sepRaw.StartsWith("regex:", StringComparison.OrdinalIgnoreCase))
                    {
                        var pattern = sepRaw.StartsWith("re:", StringComparison.OrdinalIgnoreCase) ? sepRaw.Substring(3) : sepRaw.Substring(6);
                        if (string.IsNullOrEmpty(pattern)) { return ("(empty pattern)", null); }
                        try { parts = Regex.Split(input, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase); }
                        catch (Exception ex) { return ($"(regex error: {ex.Message})", null); }
                    }
                    else
                    {
                        var sep = UnescapeUserText(sepRaw);
                        Debug.WriteLine($"[Split] After unescape: '{sep}' (length: {sep.Length}, bytes: {string.Join(" ", System.Text.Encoding.UTF8.GetBytes(sep).Select(b => b.ToString("X2")))})");
                        Debug.WriteLine($"[Split] Input contains unescaped separator: {input.Contains(sep)}");
                        
                        parts = input.Split(new[] { sep }, StringSplitOptions.None);
                        Debug.WriteLine($"[Split] Split result: {parts.Length} parts");
                        
                        if (parts.Length == 1 && sep.Contains('\n') && !sep.Contains("\r\n"))
                        {
                            var crlfSep = sep.Replace("\n", "\r\n");
                            parts = input.Split(new[] { crlfSep }, StringSplitOptions.None);
                            Debug.WriteLine($"[Split] After CRLF retry: {parts.Length} parts");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(indexStr) && int.TryParse(indexStr.Trim(), out var idx))
                    {
                        if (idx >= 0 && idx < parts.Length) { valueToStore = parts[idx]; preview = valueToStore ?? string.Empty; }
                        else { preview = $"(index out of range {parts.Length})"; }
                    }
                    else
                    {
                        valueToStore = string.Join("\u001F", parts);
                        preview = parts.Length + " parts";
                    }
                    return (preview, valueToStore);
                }
                case "IsMatch":
                {
                    var value1 = ResolveString(row.Arg1, vars) ?? string.Empty;
                    var value2 = ResolveString(row.Arg2, vars) ?? string.Empty;
                    
                    bool match = string.Equals(value1, value2, StringComparison.Ordinal);
                    string result = match ? "true" : "false";
                    
                    preview = $"{result} ('{value1}' vs '{value2}')";
                    return (preview, result);
                }
                case "TrimString":
                {
                    var sourceString = ResolveString(row.Arg1, vars) ?? string.Empty;
                    var trimString = ResolveString(row.Arg2, vars) ?? string.Empty;
                    
                    if (string.IsNullOrEmpty(trimString))
                    {
                        // No trim string specified - return source as-is
                        valueToStore = sourceString;
                        preview = valueToStore;
                    }
                    else
                    {
                        // Trim string from start and end only (after standard whitespace trim)
                        // Example: " I am me " with trim "I" -> First trim whitespace: "I am me"
                        // Then remove "I" from start: " am me" -> Then trim whitespace again: "am me"
                        
                        var result = sourceString;
                        
                        // Trim from start
                        while (result.StartsWith(trimString, StringComparison.Ordinal))
                        {
                            result = result.Substring(trimString.Length);
                        }
                        
                        // Trim from end
                        while (result.EndsWith(trimString, StringComparison.Ordinal))
                        {
                            result = result.Substring(0, result.Length - trimString.Length);
                        }
                        
                        valueToStore = result;
                        preview = valueToStore;
                    }
                    return (preview, valueToStore);
                }
                case "GetText":
                case "GetTextOCR":
                case "Invoke":
                case "SetFocus":
                case "ClickElement":
                case "ClickElementAndStay":
                case "MouseMoveToElement":
                case "IsVisible":
                case "MouseClick":
                case "GetValueFromSelection":
                case "GetSelectedElement":
                case "ToDateTime":
                case "TakeLast":
                case "Trim":
                case "Replace":
                case "GetHTML":
                case "SetClipboard":
                case "SimulateTab":
                case "SimulatePaste":
                case "Delay":
                case "GetCurrentPatientNumber":
                case "GetCurrentStudyDateTime":
                case "GetCurrentHeader":
                case "GetCurrentFindings":
                case "GetCurrentConclusion":
                case "Merge":
                    return ExecuteElemental(row, vars);
                default:
                    return ("(unsupported)", null);
            }
        }

        private static (string preview, string? value) ExecuteElemental(ProcOpRow row, Dictionary<string, string?> vars)
        {
            try
            {
                switch (row.Op)
                {
                    case "GetText":
                    {
                        var el = ResolveElement(row.Arg1, vars);
                        if (el == null) return ("(no element)", null);
                        try
                        {
                            var name = el.Name;
                            var val = el.Patterns.Value.PatternOrDefault?.Value ?? string.Empty;
                            var legacy = el.Patterns.LegacyIAccessible.PatternOrDefault?.Name ?? string.Empty;
                            var txt = !string.IsNullOrEmpty(val) ? val : (!string.IsNullOrEmpty(name) ? name : legacy);
                            return (txt ?? string.Empty, txt);
                        }
                        catch { return ("(error)", null); }
                    }
                    case "GetTextOCR":
                    {
                        var el = ResolveElement(row.Arg1, vars);
                        if (el == null) return ("(no element)", null);
                        try
                        {
                            var r = el.BoundingRectangle; if (r.Width <= 0 || r.Height <= 0) return ("(no bounds)", null);
                            var hwnd = new IntPtr(el.Properties.NativeWindowHandle.Value); if (hwnd == IntPtr.Zero) return ("(no hwnd)", null);
                            var (engine, text) = Wysg.Musm.MFCUIA.OcrReader.OcrTryReadRegionDetailedAsync(hwnd, new System.Drawing.Rectangle(0,0,(int)r.Width,(int)r.Height)).ConfigureAwait(false).GetAwaiter().GetResult();
                            if (!engine) return ("(ocr unavailable)", null);
                            return (string.IsNullOrWhiteSpace(text) ? "(empty)" : text!, text);
                        }
                        catch { return ("(error)", null); }
                    }
                    case "Invoke":
                    {
                        var el = ResolveElement(row.Arg1, vars);
                        if (el == null) return ("(no element)", null);
                        try { var inv = el.Patterns.Invoke.PatternOrDefault; if (inv != null) inv.Invoke(); else el.Patterns.Toggle.PatternOrDefault?.Toggle(); return ("(invoked)", null); }
                        catch { return ("(error)", null); }
                    }
                    case "SetFocus":
                    {
                        var el = ResolveElement(row.Arg1, vars);
                        if (el == null)
                        {
                            Debug.WriteLine("[SetFocus] FAIL: Element resolution returned null");
                            return ("(no element)", null);
                        }
                        
                        Debug.WriteLine($"[SetFocus] Element resolved: Name='{el.Name}', AutomationId='{el.AutomationId}'");
                        
                        // CRITICAL FIX: Move ALL FlaUI operations including window handle access into Dispatcher
                        // Accessing el.Properties from background thread can cause PropertyNotSupportedException
                        string resultPreview = "(error)";
                        bool success = false;
                        var completionSource = new System.Threading.Tasks.TaskCompletionSource<(string preview, bool success)>();
                        
                        try
                        {
                            Debug.WriteLine("[SetFocus] Queuing BeginInvoke on Dispatcher...");
                            
                            // Execute SetFocus logic on UI thread asynchronously
                            // IMPORTANT: Move window handle retrieval into Dispatcher to avoid cross-thread issues
                            System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                Debug.WriteLine("[SetFocus] BeginInvoke callback START - executing on UI thread");
                                Debug.WriteLine($"[SetFocus] Current thread ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                                Debug.WriteLine($"[SetFocus] Is UI thread: {System.Windows.Application.Current?.Dispatcher.CheckAccess()}");
                                
                                try
                                {
                                    // 1. Get window handle (MUST be done on UI thread to avoid PropertyNotSupportedException)
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
                                        Debug.WriteLine("[SetFocus] Sleep completed");
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
                                            success = true;
                                            completionSource.SetResult((resultPreview, success));
                                            Debug.WriteLine("[SetFocus] TaskCompletionSource.SetResult called with success");
                                            return; // Success - exit retry loop
                                        }
                                        catch (Exception ex)
                                        {
                                            lastException = ex;
                                            Debug.WriteLine($"[SetFocus] Attempt {attempt} FAILED: {ex.GetType().Name}");
                                            Debug.WriteLine($"[SetFocus] Exception message: {ex.Message}");
                                            Debug.WriteLine($"[SetFocus] Exception stack: {ex.StackTrace}");
                                            
                                            if (attempt < maxAttempts)
                                            {
                                                Debug.WriteLine($"[SetFocus] Sleeping {retryDelayMs}ms before retry...");
                                                System.Threading.Thread.Sleep(retryDelayMs);
                                            }
                                        }
                                    }
                                    
                                    // All attempts failed
                                    if (!success && lastException != null)
                                    {
                                        resultPreview = $"(error after {maxAttempts} attempts: {lastException.Message})";
                                        Debug.WriteLine($"[SetFocus] All attempts exhausted: {resultPreview}");
                                    }
                                    completionSource.SetResult((resultPreview, success));
                                    Debug.WriteLine("[SetFocus] TaskCompletionSource.SetResult called with failure");
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"[SetFocus] Dispatcher execution EXCEPTION: {ex.GetType().Name}");
                                    Debug.WriteLine($"[SetFocus] Exception message: {ex.Message}");
                                    Debug.WriteLine($"[SetFocus] Exception stack: {ex.StackTrace}");
                                    resultPreview = $"(error: {ex.Message})";
                                    completionSource.SetResult((resultPreview, false));
                                }
                                
                                Debug.WriteLine("[SetFocus] BeginInvoke callback END");
                            }));
                            
                            Debug.WriteLine("[SetFocus] BeginInvoke queued, waiting for completion...");
                            
                            // Wait for completion with timeout
                            var task = completionSource.Task;
                            Debug.WriteLine("[SetFocus] Calling Task.Wait with 5 second timeout...");
                            
                            if (task.Wait(TimeSpan.FromSeconds(5)))
                            {
                                var result = task.Result;
                                Debug.WriteLine($"[SetFocus] Task completed: preview='{result.preview}', success={result.success}");
                                return (result.preview, null);
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
                            Debug.WriteLine($"[SetFocus] Exception stack: {ex.StackTrace}");
                            return ($"(dispatcher error: {ex.Message})", null);
                        }
                    }
                    case "TakeLast":
                    {
                        var combined = ResolveString(row.Arg1, vars) ?? string.Empty;
                        var arr = combined.Split('\u001F'); var value = arr.Length > 0 ? arr[^1] : string.Empty;
                        return (value, value);
                    }
                    case "Trim":
                    {
                        var s = ResolveString(row.Arg1, vars); var v = s?.Trim(); return (v ?? "(null)", v);
                    }
                    case "Replace":
                    {
                        var input = ResolveString(row.Arg1, vars) ?? string.Empty;
                        var search = Regex.Unescape(ResolveString(row.Arg2, vars) ?? string.Empty);
                        var repl = Regex.Unescape(ResolveString(row.Arg3, vars) ?? string.Empty);
                        if (string.IsNullOrEmpty(search)) return (input, input);
                        var output = input.Replace(search, repl);
                        return (output, output);
                    }
                    case "GetValueFromSelection":
                    {
                        var el = ResolveElement(row.Arg1, vars); 
                        var headerWanted = row.Arg2?.Value ?? "ID"; 
                        if (string.IsNullOrWhiteSpace(headerWanted)) headerWanted = "ID";
                        if (el == null) return ("(no element)", null);
                        try
                        {
                            var selection = el.Patterns.Selection.PatternOrDefault;
                            var selected = selection?.Selection?.Value ?? Array.Empty<AutomationElement>();
                            if (selected.Length == 0)
                            {
                                selected = el.FindAllDescendants().Where(a => { try { return a.Patterns.SelectionItem.IsSupported && a.Patterns.SelectionItem.PatternOrDefault?.IsSelected == true; } catch { return false; } }).ToArray();
                            }
                            if (selected.Length == 0) return ("(no selection)", null);
                            var rowEl = selected[0];
                            var headers = SpyHeaderHelpers.GetHeaderTexts(el);
                            var cells = SpyHeaderHelpers.GetRowCellValues(rowEl);
                            if (headers.Count < cells.Count) for (int j = headers.Count; j < cells.Count; j++) headers.Add($"Col{j + 1}");
                            else if (headers.Count > cells.Count) for (int j = cells.Count; j < headers.Count; j++) cells.Add(string.Empty);
                            string? matched = null;
                            for (int j = 0; j < headers.Count; j++) if (string.Equals(SpyHeaderHelpers.NormalizeHeader(headers[j]), headerWanted, StringComparison.OrdinalIgnoreCase)) { matched = cells[j]; break; }
                            if (matched == null)
                                for (int j = 0; j < headers.Count; j++) if (SpyHeaderHelpers.NormalizeHeader(headers[j]).IndexOf(headerWanted, StringComparison.OrdinalIgnoreCase) >= 0) { matched = cells[j]; break; }
                            if (matched == null) return ($"({headerWanted} not found)", null);
                            return (matched, matched);
                        }
                        catch { return ("(error)", null); }
                    }
                    case "ToDateTime":
                    {
                        var s = ResolveString(row.Arg1, vars); if (string.IsNullOrWhiteSpace(s)) return ("(null)", null);
                        if (DateTime.TryParse(s.Trim(), out var dt)) { var iso = dt.ToString("o"); return (dt.ToString("yyyy-MM-dd HH:mm:ss"), iso); }
                        return ("(parse fail)", null);
                    }
                    case "GetHTML":
                    {
                        var url = ResolveString(row.Arg1, vars);
                        if (string.IsNullOrWhiteSpace(url) || !(url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                            return ("(no url)", null);
                        try
                        {
                            EnsureEncodingProviders();
                            using var resp = _http.GetAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
                            resp.EnsureSuccessStatusCode();
                            var bytes = resp.Content.ReadAsByteArrayAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                            
                            // Simple decoding: try UTF-8 first, then fallback to charset from header
                            var charset = resp.Content.Headers.ContentType?.CharSet;
                            string html;
                            try
                            {
                                // First try UTF-8
                                html = Encoding.UTF8.GetString(bytes);
                            }
                            catch
                            {
                                // Fallback to charset from header if available
                                if (!string.IsNullOrWhiteSpace(charset))
                                {
                                    try
                                    {
                                        var enc = Encoding.GetEncoding(charset);
                                        html = enc.GetString(bytes);
                                    }
                                    catch
                                    {
                                        // Last resort: use default encoding
                                        html = Encoding.Default.GetString(bytes);
                                    }
                                }
                                else
                                {
                                    html = Encoding.Default.GetString(bytes);
                                }
                            }
                            
                            return (html ?? string.Empty, html);
                        }
                        catch (Exception ex) { return ($"(error) {ex.Message}", null); }
                    }
                    case "MouseClick":
                    {
                        var xs = ResolveString(row.Arg1, vars); var ys = ResolveString(row.Arg2, vars);
                        if (!int.TryParse(xs, out var x) || !int.TryParse(ys, out var y)) return ("(invalid coords)", null);
                        try { NativeMouseHelper.ClickScreen(x, y); return ($"(clicked {x},{y})", null); }
                        catch { return ("(error)", null); }
                    }
                    case "ClickElement":
                    {
                        var el = ResolveElement(row.Arg1, vars);
                        if (el == null) return ("(no element)", null);
                        try
                        {
                            var rect = el.BoundingRectangle;
                            if (rect.Width <= 0 || rect.Height <= 0) return ("(no bounds)", null);
                            
                            int centerX = (int)(rect.Left + rect.Width / 2);
                            int centerY = (int)(rect.Top + rect.Height / 2);
                            
                            NativeMouseHelper.ClickScreenWithRestore(centerX, centerY);
                            return ($"(clicked element center {centerX},{centerY})", null);
                        }
                        catch (Exception ex) { return ($"(error: {ex.Message})", null); }
                    }
                    case "ClickElementAndStay":
                    {
                        var el = ResolveElement(row.Arg1, vars);
                        if (el == null) return ("(no element)", null);
                        try
                        {
                            var rect = el.BoundingRectangle;
                            if (rect.Width <= 0 || rect.Height <= 0) return ("(no bounds)", null);
                            
                            int centerX = (int)(rect.Left + rect.Width / 2);
                            int centerY = (int)(rect.Top + rect.Height / 2);
                            
                            NativeMouseHelper.ClickScreen(centerX, centerY); // No restore - cursor stays
                            return ($"(clicked and stayed at {centerX},{centerY})", null);
                        }
                        catch (Exception ex) { return ($"(error: {ex.Message})", null); }
                    }
                    case "MouseMoveToElement":
                    {
                        var el = ResolveElement(row.Arg1, vars);
                        if (el == null) return ("(no element)", null);
                        try
                        {
                            var rect = el.BoundingRectangle;
                            if (rect.Width <= 0 || rect.Height <= 0) return ("(no bounds)", null);
                            
                            int centerX = (int)(rect.Left + rect.Width / 2);
                            int centerY = (int)(rect.Top + rect.Height / 2);
                            
                            NativeMouseHelper.SetCursorPos(centerX, centerY);
                            return ($"(moved to element center {centerX},{centerY})", null);
                        }
                        catch (Exception ex) { return ($"(error: {ex.Message})", null); }
                    }
                    case "IsVisible":
                    {
                        var el = ResolveElement(row.Arg1, vars);
                        if (el == null) return ("false", "false");
                        try
                        {
                            // Check if element is reachable and has valid bounds
                            var rect = el.BoundingRectangle;
                            bool isVisible = rect.Width > 0 && rect.Height > 0;
                            string result = isVisible ? "true" : "false";
                            return (result, result);
                        }
                        catch
                        {
                            // Element exists but not accessible - consider it not visible
                            return ("false", "false");
                        }
                    }
                    case "SetClipboard":
                    {
                        var text = ResolveString(row.Arg1, vars);
                        if (text == null) return ("(null)", null);
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
                            sta.Join(1000); // Wait up to 1 second
                            return ($"(clipboard set, {text.Length} chars)", null);
                        }
                        catch (Exception ex) { return ($"(error: {ex.Message})", null); }
                    }
                    case "SimulateTab":
                    {
                        try
                        {
                            System.Windows.Forms.SendKeys.SendWait("{TAB}");
                            return ("(Tab key sent)", null);
                        }
                        catch (Exception ex) { return ($"(error: {ex.Message})", null); }
                    }
                    case "Delay":
                    {
                        var delayStr = ResolveString(row.Arg1, vars);
                        if (!int.TryParse(delayStr, out var delayMs) || delayMs < 0) 
                            return ("(invalid delay)", null);
                        try
                        {
                            System.Threading.Tasks.Task.Delay(delayMs).Wait();
                            return ($"(delayed {delayMs} ms)", null);
                        }
                        catch (Exception ex) { return ($"(error: {ex.Message})", null); }
                    }
                    case "SimulatePaste":
                    {
                        try
                        {
                            System.Windows.Forms.SendKeys.SendWait("^v");
                            return ("(Ctrl+V sent)", null);
                        }
                        catch (Exception ex) { return ($"(error: {ex.Message})", null); }
                    }
                    case "GetSelectedElement":
                    {
                        // Resolve parent element from Arg1
                        var listEl = ResolveElement(row.Arg1, vars);
                        if (listEl == null)
                            return ("(element not resolved)", null);

                        try
                        {
                            // Get selected item
                            var selection = listEl.Patterns.Selection.PatternOrDefault;
                            var selected = selection?.Selection?.Value ?? Array.Empty<AutomationElement>();
                            if (selected.Length == 0)
                            {
                                // Fallback: scan descendants
                                selected = listEl.FindAllDescendants().Where(a =>
                                {
                                    try { return a.Patterns.SelectionItem.IsSupported && a.Patterns.SelectionItem.PatternOrDefault?.IsSelected == true; }
                                    catch { return false; }
                                }).ToArray();
                            }

                            if (selected.Length == 0)
                                return ("(no selection)", null);

                            var selectedRow = selected[0];
                            var elName = string.IsNullOrWhiteSpace(selectedRow.Name) ? "(no name)" : selectedRow.Name;
                            var elAutoId = selectedRow.AutomationId ?? "(no automationId)";
                            
                            // Store element in cache for later use by ClickElement, etc.
                            var cacheKey = $"SelectedElement:{selectedRow.Name}";
                            _elementCache[cacheKey] = selectedRow;
                            
                            // Return element identifier (name)
                            return ($"(element: {elName}, automationId: {elAutoId})", cacheKey);
                        }
                        catch (Exception ex)
                        {
                            return ($"(error: {ex.Message})", null);
                        }
                    }
                    case "GetCurrentHeader":
                    {
                        // Get header text from MainViewModel (UI thread access required)
                        try
                        {
                            Debug.WriteLine("[ProcedureExecutor][GetCurrentHeader] Starting operation");
                            string result = string.Empty;
                            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                            {
                                var mainWindow = System.Windows.Application.Current?.MainWindow;
                                if (mainWindow != null && mainWindow.DataContext is ViewModels.MainViewModel mainVM)
                                {
                                    result = mainVM.HeaderText ?? string.Empty;
                                    Debug.WriteLine($"[ProcedureExecutor][GetCurrentHeader] SUCCESS: length={result.Length}");
                                }
                            });
                            return (string.IsNullOrWhiteSpace(result) ? "(empty)" : result, result);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[ProcedureExecutor][GetCurrentHeader] EXCEPTION: {ex.Message}");
                            return ($"(error: {ex.Message})", null);
                        }
                    }
                    case "GetCurrentFindings":
                    {
                        // Get findings text from MainViewModel (UI thread access required)
                        try
                        {
                            Debug.WriteLine("[ProcedureExecutor][GetCurrentFindings] Starting operation");
                            string result = string.Empty;
                            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                            {
                                var mainWindow = System.Windows.Application.Current?.MainWindow;
                                if (mainWindow != null && mainWindow.DataContext is ViewModels.MainViewModel mainVM)
                                {
                                    result = mainVM.FindingsText ?? string.Empty;
                                    Debug.WriteLine($"[ProcedureExecutor][GetCurrentFindings] SUCCESS: length={result.Length}");
                                }
                            });
                            return (string.IsNullOrWhiteSpace(result) ? "(empty)" : result, result);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[ProcedureExecutor][GetCurrentFindings] EXCEPTION: {ex.Message}");
                            return ($"(error: {ex.Message})", null);
                        }
                    }
                    case "GetCurrentConclusion":
                    {
                        // Get conclusion text from MainViewModel (UI thread access required)
                        try
                        {
                            Debug.WriteLine("[ProcedureExecutor][GetCurrentConclusion] Starting operation");
                            string result = string.Empty;
                            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                            {
                                var mainWindow = System.Windows.Application.Current?.MainWindow;
                                if (mainWindow != null && mainWindow.DataContext is ViewModels.MainViewModel mainVM)
                                {
                                    result = mainVM.ConclusionText ?? string.Empty;
                                    Debug.WriteLine($"[ProcedureExecutor][GetCurrentConclusion] SUCCESS: length={result.Length}");
                                }
                            });
                            return (string.IsNullOrWhiteSpace(result) ? "(empty)" : result, result);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[ProcedureExecutor][GetCurrentConclusion] EXCEPTION: {ex.Message}");
                            return ($"(error: {ex.Message})", null);
                        }
                    }
                    case "Merge":
                    {
                        var input1 = ResolveString(row.Arg1, vars) ?? string.Empty;
                        var input2 = ResolveString(row.Arg2, vars) ?? string.Empty;
                        var separator = ResolveString(row.Arg3, vars) ?? string.Empty;
                        
                        // Merge the two strings with optional separator
                        string merged;
                        if (string.IsNullOrEmpty(separator))
                        {
                            merged = input1 + input2;
                        }
                        else
                        {
                            merged = input1 + separator + input2;
                        }
                        return (merged, merged);
                    }
                }
            }
            catch { }
            return ("(unsupported)", null);
        }

        private static class SpyHeaderHelpers
        {
            public static string NormalizeHeader(string h)
            {
                h = (h ?? string.Empty).Trim();
                if (string.Equals(h, "Accession", StringComparison.OrdinalIgnoreCase)) return "Accession No.";
                if (string.Equals(h, "Study Description", StringComparison.OrdinalIgnoreCase)) return "Study Desc";
                if (string.Equals(h, "Institution Name", StringComparison.OrdinalIgnoreCase)) return "Institution";
                if (string.Equals(h, "BirthDate", StringComparison.OrdinalIgnoreCase)) return "Birth Date";
                if (string.Equals(h, "BodyPart", StringComparison.OrdinalIgnoreCase)) return "Body Part";
                return h;
            }
            
            public static List<string> GetHeaderTexts(AutomationElement list)
            {
                var headers = new List<string>();
                try
                {
                    var kids = list.FindAllChildren();
                    if (kids.Length > 0)
                    {
                        var headerRow = kids[0];
                        var cells = headerRow.FindAllChildren();
                        foreach (var c in cells)
                        {
                            string txt = TryRead(c);
                            if (string.IsNullOrWhiteSpace(txt))
                            {
                                foreach (var g in c.FindAllChildren()) { txt = TryRead(g); if (!string.IsNullOrWhiteSpace(txt)) break; }
                            }
                            headers.Add(string.IsNullOrWhiteSpace(txt) ? string.Empty : txt.Trim());
                        }
                    }
                }
                catch { }
                return headers;
            }
            
            public static List<string> GetRowCellValues(AutomationElement row)
            {
                var vals = new List<string>();
                try
                {
                    var children = row.FindAllChildren();
                    foreach (var c in children)
                    {
                        string txt = TryRead(c).Trim();
                        if (string.IsNullOrEmpty(txt))
                        {
                            foreach (var gc in c.FindAllChildren()) { var t = TryRead(gc).Trim(); if (!string.IsNullOrEmpty(t)) { txt = t; break; } }
                        }
                        vals.Add(txt);
                    }
                }
                catch { }
                return vals;
            }
            
            private static string TryRead(AutomationElement el)
            {
                try
                {
                    var vp = el.Patterns.Value.PatternOrDefault; if (vp != null && vp.Value.TryGetValue(out var v) && !string.IsNullOrWhiteSpace(v)) return v;
                }
                catch { }
                try { var n = el.Name; if (!string.IsNullOrWhiteSpace(n)) return n; } catch { }
                try { var l = el.Patterns.LegacyIAccessible.PatternOrDefault?.Name; if (!string.IsNullOrWhiteSpace(l)) return l; } catch { }
                return string.Empty;
            }
        }
    }
}
