using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace Wysg.Musm.Radium.Services
{
    internal static class ProcedureExecutor
    {
        private static readonly HttpClient _http = new();
        private static bool _encProviderRegistered;
        private static Func<string>? _getProcPathOverride;
        public static void SetProcPathOverride(Func<string> resolver) => _getProcPathOverride = resolver;
        private static void EnsureEncodingProviders()
        {
            if (_encProviderRegistered) return;
            try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); } catch { }
            _encProviderRegistered = true;
        }

        private static readonly Dictionary<UiBookmarks.KnownControl, AutomationElement> _controlCache = new();
        
        // Runtime element cache for storing elements from GetSelectedElement
        private static readonly Dictionary<string, AutomationElement> _elementCache = new();
        
        private static AutomationElement? GetCached(UiBookmarks.KnownControl key)
        {
            if (_controlCache.TryGetValue(key, out var el))
            {
                try { _ = el.Name; return el; } catch { _controlCache.Remove(key); }
            }
            return null;
        }
        private static void StoreCache(UiBookmarks.KnownControl key, AutomationElement el) { if (el != null) _controlCache[key] = el; }

        private sealed class ProcStore { public Dictionary<string, List<ProcOpRow>> Methods { get; set; } = new(); }
        private sealed class ProcOpRow
        {
            public string Op { get; set; } = string.Empty;
            public ProcArg Arg1 { get; set; } = new();
            public ProcArg Arg2 { get; set; } = new();
            public ProcArg Arg3 { get; set; } = new();
            public bool Arg1Enabled { get; set; } = true;
            public bool Arg2Enabled { get; set; } = true;
            public bool Arg3Enabled { get; set; } = false;
            public string? OutputVar { get; set; }
            public string? OutputPreview { get; set; }
        }
        private sealed class ProcArg { public string Type { get; set; } = "String"; public string? Value { get; set; } }

        private enum ArgKind { Element, String, Number, Var }

        private static string GetProcPath()
        {
            if (_getProcPathOverride != null)
            {
                try { return _getProcPathOverride(); } catch { }
            }
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wysg.Musm", "Radium");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "ui-procedures.json");
        }

        private static ProcStore Load()
        {
            try
            {
                var p = GetProcPath();
                if (!File.Exists(p)) return new ProcStore();
                return JsonSerializer.Deserialize<ProcStore>(File.ReadAllText(p), new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? new ProcStore();
            }
            catch { return new ProcStore(); }
        }

        private static void Save(ProcStore s)
        {
            try
            {
                var p = GetProcPath();
                File.WriteAllText(p, JsonSerializer.Serialize(s, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
            }
            catch { }
        }

        public static Task<string?> ExecuteAsync(string methodTag) => Task.Run(() => 
        {
            Debug.WriteLine($"[ProcedureExecutor][ExecuteAsync] ===== START: {methodTag} =====");
            try
            {
                var result = ExecuteInternal(methodTag);
                Debug.WriteLine($"[ProcedureExecutor][ExecuteAsync] ===== END: {methodTag} =====");
                Debug.WriteLine($"[ProcedureExecutor][ExecuteAsync] Final result: '{result}'");
                Debug.WriteLine($"[ProcedureExecutor][ExecuteAsync] Result length: {result?.Length ?? 0} characters");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProcedureExecutor][ExecuteAsync] ===== EXCEPTION in {methodTag} =====");
                Debug.WriteLine($"[ProcedureExecutor][ExecuteAsync] Exception: {ex.GetType().Name} - {ex.Message}");
                Debug.WriteLine($"[ProcedureExecutor][ExecuteAsync] StackTrace: {ex.StackTrace}");
                throw;
            }
        });

        private static string? ExecuteInternal(string methodTag)
        {
            if (string.IsNullOrWhiteSpace(methodTag)) return null;
            
            // Clear element cache before executing procedure
            _elementCache.Clear();
            
            // Special handling for direct MainViewModel reads (no procedure needed)
            if (string.Equals(methodTag, "GetCurrentPatientNumber", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    Debug.WriteLine("[ProcedureExecutor][GetCurrentPatientNumber] Starting direct read");
                    
                    // Access MainWindow on UI thread to avoid cross-thread exception
                    string result = string.Empty;
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        var mainWindow = System.Windows.Application.Current?.MainWindow;
                        if (mainWindow != null)
                        {
                            Debug.WriteLine("[ProcedureExecutor][GetCurrentPatientNumber] MainWindow found");
                            if (mainWindow.DataContext is ViewModels.MainViewModel mainVM)
                            {
                                result = mainVM.PatientNumber ?? string.Empty;
                                Debug.WriteLine($"[ProcedureExecutor][GetCurrentPatientNumber] SUCCESS: PatientNumber='{result}'");
                            }
                            else
                            {
                                Debug.WriteLine($"[ProcedureExecutor][GetCurrentPatientNumber] FAIL: MainWindow.DataContext is {mainWindow.DataContext?.GetType().Name ?? "null"}");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("[ProcedureExecutor][GetCurrentPatientNumber] FAIL: MainWindow is null");
                        }
                    });
                    return result;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ProcedureExecutor][GetCurrentPatientNumber] EXCEPTION: {ex.GetType().Name} - {ex.Message}");
                }
                return string.Empty;
            }
            
            if (string.Equals(methodTag, "GetCurrentStudyDateTime", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    Debug.WriteLine("[ProcedureExecutor][GetCurrentStudyDateTime] Starting direct read");
                    
                    // Access MainWindow on UI thread to avoid cross-thread exception
                    string result = string.Empty;
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        var mainWindow = System.Windows.Application.Current?.MainWindow;
                        if (mainWindow != null)
                        {
                            Debug.WriteLine("[ProcedureExecutor][GetCurrentStudyDateTime] MainWindow found");
                            if (mainWindow.DataContext is ViewModels.MainViewModel mainVM)
                            {
                                var rawValue = mainVM.StudyDateTime ?? string.Empty;
                                Debug.WriteLine($"[ProcedureExecutor][GetCurrentStudyDateTime] Raw value: '{rawValue}'");
                                
                                // Try to parse and format as YYYY-MM-DD HH:mm:ss
                                if (!string.IsNullOrWhiteSpace(rawValue) && DateTime.TryParse(rawValue, out var dt))
                                {
                                    result = dt.ToString("yyyy-MM-dd HH:mm:ss");
                                    Debug.WriteLine($"[ProcedureExecutor][GetCurrentStudyDateTime] SUCCESS: Formatted='{result}'");
                                }
                                else
                                {
                                    // Return raw value if parsing fails
                                    result = rawValue;
                                    Debug.WriteLine($"[ProcedureExecutor][GetCurrentStudyDateTime] WARN: Failed to parse datetime, returning raw value");
                                }
                            }
                            else
                            {
                                Debug.WriteLine($"[ProcedureExecutor][GetCurrentStudyDateTime] FAIL: MainWindow.DataContext is {mainWindow.DataContext?.GetType().Name ?? "null"}");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("[ProcedureExecutor][GetCurrentStudyDateTime] FAIL: MainWindow is null");
                        }
                    });
                    return result;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ProcedureExecutor][GetCurrentStudyDateTime] EXCEPTION: {ex.GetType().Name} - {ex.Message}");
                }
                return string.Empty;
            }
            
            var store = Load();

            if (!store.Methods.TryGetValue(methodTag, out var steps) || steps.Count == 0)
            {
                Debug.WriteLine($"[ProcedureExecutor][ExecuteInternal] Procedure '{methodTag}' not found in store");
                
                // No implicit fallback for InvokeOpenStudy; require explicit authoring
                if (string.Equals(methodTag, "InvokeOpenStudy", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine("[ProcedureExecutor][ExecuteInternal] InvokeOpenStudy requires explicit configuration");
                    throw new InvalidOperationException("Custom procedure 'InvokeOpenStudy' is not defined. Please configure it in SpyWindow for this PACS profile.");
                }

                Debug.WriteLine($"[ProcedureExecutor][ExecuteInternal] Attempting to create fallback procedure for '{methodTag}'");
                steps = TryCreateFallbackProcedure(methodTag);
                if (steps.Count > 0) 
                { 
                    Debug.WriteLine($"[ProcedureExecutor][ExecuteInternal] Created fallback with {steps.Count} steps");
                    store.Methods[methodTag] = steps; 
                    Save(store); 
                }
                else 
                {
                    Debug.WriteLine($"[ProcedureExecutor][ExecuteInternal] No fallback available for '{methodTag}'");
                    return null;
                }
            }
            else
            {
                Debug.WriteLine($"[ProcedureExecutor][ExecuteInternal] Found procedure '{methodTag}' with {steps.Count} steps");
            }

            var vars = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            string? lastOperationResult = null; // Changed: track ONLY the last operation result
            for (int i = 0; i < steps.Count; i++)
            {
                var row = steps[i];
                Debug.WriteLine($"[ProcedureExecutor][ExecuteInternal] Step {i + 1}/{steps.Count}: Op='{row.Op}'");
                var (preview, value) = ExecuteRow(row, vars);
                Debug.WriteLine($"[ProcedureExecutor][ExecuteInternal] Step {i + 1} result: preview='{preview}', value='{value}'");
                var implicitKey = $"var{i + 1}";
                vars[implicitKey] = value;
                if (!string.IsNullOrWhiteSpace(row.OutputVar)) 
                {
                    vars[row.OutputVar!] = value;
                    Debug.WriteLine($"[ProcedureExecutor][ExecuteInternal] Stored to variable '{row.OutputVar}': '{value}'");
                }
                // Changed: Always update lastOperationResult to the current operation's result
                lastOperationResult = value;
            }
            
            Debug.WriteLine($"[ProcedureExecutor][ExecuteInternal] All steps completed. Last result: '{lastOperationResult}'");

            // Special handling for PatientNumberMatch and StudyDateTimeMatch
            if (string.Equals(methodTag, "PatientNumberMatch", StringComparison.OrdinalIgnoreCase))
            {
                // Compare PACS patient number (lastOperationResult) with MainViewModel current patient
                try
                {
                    Debug.WriteLine("[ProcedureExecutor][PatientNumberMatch] Starting comparison");
                    var mainWindow = System.Windows.Application.Current?.MainWindow;
                    if (mainWindow != null)
                    {
                        if (mainWindow.DataContext is ViewModels.MainViewModel mainVM)
                        {
                            string Normalize(string? s) => string.IsNullOrWhiteSpace(s) ? string.Empty : System.Text.RegularExpressions.Regex.Replace(s, "[^A-Za-z0-9]", "").ToUpperInvariant();
                            var pacsPatientNumber = Normalize(lastOperationResult);
                            var mainPatientNumber = Normalize(mainVM.PatientNumber);
                            Debug.WriteLine($"[ProcedureExecutor][PatientNumberMatch] PACS='{pacsPatientNumber}' Main='{mainPatientNumber}'");
                            bool matches = string.Equals(pacsPatientNumber, mainPatientNumber, StringComparison.Ordinal);
                            Debug.WriteLine($"[ProcedureExecutor][PatientNumberMatch] Result: {matches}");
                            return matches ? "true" : "false";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ProcedureExecutor][PatientNumberMatch] EXCEPTION: {ex.GetType().Name} - {ex.Message}");
                }
                return "false"; // Default to false if comparison fails
            }
            
            if (string.Equals(methodTag, "StudyDateTimeMatch", StringComparison.OrdinalIgnoreCase))
            {
                // Compare PACS study datetime (lastOperationResult) with MainViewModel current study datetime
                try
                {
                    Debug.WriteLine("[ProcedureExecutor][StudyDateTimeMatch] Starting comparison");
                    var mainWindow = System.Windows.Application.Current?.MainWindow;
                    if (mainWindow != null)
                    {
                        if (mainWindow.DataContext is ViewModels.MainViewModel mainVM)
                        {
                            Debug.WriteLine($"[ProcedureExecutor][StudyDateTimeMatch] PACS='{lastOperationResult}' Main='{mainVM.StudyDateTime}'");
                            if (DateTime.TryParse(lastOperationResult, out var pacsDateTime) &&
                                DateTime.TryParse(mainVM.StudyDateTime, out var mainDateTime))
                            {
                                // Compare dates only (ignore time component)
                                bool matches = pacsDateTime.Date == mainDateTime.Date;
                                Debug.WriteLine($"[ProcedureExecutor][StudyDateTimeMatch] Result: {matches}");
                                return matches ? "true" : "false";
                            }
                            else
                            {
                                Debug.WriteLine("[ProcedureExecutor][StudyDateTimeMatch] Failed to parse datetimes");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ProcedureExecutor][StudyDateTimeMatch] EXCEPTION: {ex.GetType().Name} - {ex.Message}");
                }
                return "false"; // Default to false if comparison fails
            }
            
            // Changed: Return blank string if last operation returned null
            return lastOperationResult ?? string.Empty;
        }

        private static List<ProcOpRow> TryCreateFallbackProcedure(string methodTag)
        {
            // Keep selective fallbacks for non-critical getters; do NOT fallback for InvokeOpenStudy.
            if (string.Equals(methodTag, "GetCurrentPatientRemark", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    new ProcOpRow { Op = "GetText", Arg1 = new ProcArg { Type = nameof(ArgKind.Element), Value = UiBookmarks.KnownControl.PatientRemark.ToString() }, Arg1Enabled = true, Arg2Enabled = false, Arg3Enabled = false, OutputVar = "var1" }
                };
            }
            if (string.Equals(methodTag, "GetCurrentStudyRemark", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    new ProcOpRow { Op = "GetText", Arg1 = new ProcArg { Type = nameof(ArgKind.Element), Value = UiBookmarks.KnownControl.StudyRemark.ToString() }, Arg1Enabled = true, Arg2Enabled = false, Arg3Enabled = false, OutputVar = "var1" }
                };
            }
            // IMPORTANT: No fallback for InvokeOpenStudy (explicit procedure required). Return empty.
            if (string.Equals(methodTag, "InvokeOpenStudy", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>();
            }
            if (string.Equals(methodTag, "CustomMouseClick1", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    new ProcOpRow { Op = "MouseClick", Arg1 = new ProcArg { Type = nameof(ArgKind.Number), Value = "0" }, Arg2 = new ProcArg { Type = nameof(ArgKind.Number), Value = "0" }, Arg1Enabled = true, Arg2Enabled = true, Arg3Enabled = false }
                };
            }
            if (string.Equals(methodTag, "CustomMouseClick2", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    new ProcOpRow { Op = "MouseClick", Arg1 = new ProcArg { Type = nameof(ArgKind.Number), Value = "0" }, Arg2 = new ProcArg { Type = nameof(ArgKind.Number), Value = "0" }, Arg1Enabled = true, Arg2Enabled = true, Arg3Enabled = false }
                };
            }
            if (string.Equals(methodTag, "InvokeTest", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    new ProcOpRow { Op = "Invoke", Arg1 = new ProcArg { Type = nameof(ArgKind.Element), Value = UiBookmarks.KnownControl.TestInvoke.ToString() }, Arg1Enabled = true, Arg2Enabled = false, Arg3Enabled = false }
                };
            }
            if (string.Equals(methodTag, "SetCurrentStudyInMainScreen", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    new ProcOpRow { Op = "ClickElement", Arg1 = new ProcArg { Type = nameof(ArgKind.Element), Value = UiBookmarks.KnownControl.Screen_MainCurrentStudyTab.ToString() }, Arg1Enabled = true, Arg2Enabled = false, Arg3Enabled = false }
                };
            }
            if (string.Equals(methodTag, "SetPreviousStudyInSubScreen", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    new ProcOpRow { Op = "ClickElement", Arg1 = new ProcArg { Type = nameof(ArgKind.Element), Value = UiBookmarks.KnownControl.Screen_SubPreviousStudyTab.ToString() }, Arg1Enabled = true, Arg2Enabled = false, Arg3Enabled = false }
                };
            }
            if (string.Equals(methodTag, "WorklistIsVisible", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    new ProcOpRow { Op = "IsVisible", Arg1 = new ProcArg { Type = nameof(ArgKind.Element), Value = UiBookmarks.KnownControl.WorklistWindow.ToString() }, Arg1Enabled = true, Arg2Enabled = false, Arg3Enabled = false }
                };
            }
            // NEW: Auto-seed for InvokeOpenWorklist - invoke worklist open button
            if (string.Equals(methodTag, "InvokeOpenWorklist", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    new ProcOpRow { Op = "Invoke", Arg1 = new ProcArg { Type = nameof(ArgKind.Element), Value = UiBookmarks.KnownControl.WorklistOpenButton.ToString() }, Arg1Enabled = true, Arg2Enabled = false, Arg3Enabled = false }
                };
            }
            // NEW: Auto-seed for SetFocusSearchResultsList - use SetFocus operation on search results list
            if (string.Equals(methodTag, "SetFocusSearchResultsList", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    new ProcOpRow { Op = "SetFocus", Arg1 = new ProcArg { Type = nameof(ArgKind.Element), Value = UiBookmarks.KnownControl.SearchResultsList.ToString() }, Arg1Enabled = true, Arg2Enabled = false, Arg3Enabled = false }
                };
            }
            // NEW: Auto-seed for SendReport - placeholder for now (requires findings/conclusion parameters in future)
            if (string.Equals(methodTag, "SendReport", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    // Placeholder: Invoke send report button (user will configure specific UI element)
                    new ProcOpRow { Op = "Invoke", Arg1 = new ProcArg { Type = nameof(ArgKind.Element), Value = UiBookmarks.KnownControl.SendReportButton.ToString() }, Arg1Enabled = true, Arg2Enabled = false, Arg3Enabled = false }
                };
            }
            // NEW: Auto-seed for PatientNumberMatch - compares PACS patient number with MainViewModel current patient
            if (string.Equals(methodTag, "PatientNumberMatch", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    new ProcOpRow { Op = "GetCurrentPatientNumber", Arg1Enabled = false, Arg2Enabled = false, Arg3Enabled = false, OutputVar = "var1" }
                };
            }
            // NEW: Auto-seed for StudyDateTimeMatch - compares PACS study datetime with MainViewModel current study datetime
            if (string.Equals(methodTag, "StudyDateTimeMatch", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    new ProcOpRow { Op = "GetCurrentStudyDateTime", Arg1Enabled = false, Arg2Enabled = false, Arg3Enabled = false, OutputVar = "var1" }
                };
            }
            
            // PLACEHOLDER FALLBACKS for GetCurrentPatientNumber and GetCurrentStudyDateTime
            // These operations read from MainViewModel directly, no UI automation needed
            // Users can override these with custom procedures if needed
            if (string.Equals(methodTag, "GetCurrentPatientNumber", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    // This is a special operation that reads directly from MainViewModel.PatientNumber
                    // No procedure steps needed - handled in ExecuteInternal
                };
            }
            if (string.Equals(methodTag, "GetCurrentStudyDateTime", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    // This is a special operation that reads directly from MainViewModel.StudyDateTime
                    // No procedure steps needed - handled in ExecuteInternal
                };
            }
            
            return new List<ProcOpRow>();
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
                case "GetText":
                case "GetTextOCR":
                case "Invoke":
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
                        var el = ResolveElement(row.Arg1);
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
                        var el = ResolveElement(row.Arg1);
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
                        var el = ResolveElement(row.Arg1);
                        if (el == null) return ("(no element)", null);
                        try { var inv = el.Patterns.Invoke.PatternOrDefault; if (inv != null) inv.Invoke(); else el.Patterns.Toggle.PatternOrDefault?.Toggle(); return ("(invoked)", null); }
                        catch { return ("(error)", null); }
                    }
                    case "SetFocus":
                    {
                        var el = ResolveElement(row.Arg1);
                        if (el == null) return ("(no element)", null);
                        
                        // Retry logic for SetFocus - sometimes elements need time to be ready
                        const int maxAttempts = 3;
                        const int retryDelayMs = 150;
                        Exception? lastException = null;
                        bool success = false;
                        
                        for (int attempt = 1; attempt <= maxAttempts; attempt++)
                        {
                            try
                            {
                                // Execute Focus on STA thread to match legacy PacsService behavior
                                // UI Automation sometimes requires proper thread apartment state
                                var focusSuccess = false;
                                Exception? focusException = null;
                                
                                var thread = new System.Threading.Thread(() =>
                                {
                                    try
                                    {
                                        el.Focus();
                                        focusSuccess = true;
                                    }
                                    catch (Exception ex)
                                    {
                                        focusException = ex;
                                    }
                                });
                                
                                thread.SetApartmentState(System.Threading.ApartmentState.STA);
                                thread.Start();
                                thread.Join(1000); // Wait up to 1 second
                                
                                if (focusSuccess)
                                {
                                    var preview = attempt > 1 ? $"(focused after {attempt} attempts)" : "(focused)";
                                    success = true;
                                    return (preview, null);
                                }
                                else if (focusException != null)
                                {
                                    throw focusException;
                                }
                            }
                            catch (Exception ex)
                            {
                                lastException = ex;
                                if (attempt < maxAttempts)
                                {
                                    Task.Delay(retryDelayMs).Wait();
                                }
                            }
                        }
                        
                        if (!success)
                        {
                            return ($"(error after {maxAttempts} attempts: {lastException?.Message})", null);
                        }
                        
                        return ("(error)", null);
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
                        var el = ResolveElement(row.Arg1); var headerWanted = row.Arg2?.Value ?? "ID"; if (string.IsNullOrWhiteSpace(headerWanted)) headerWanted = "ID";
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
                            var charset = resp.Content.Headers.ContentType?.CharSet;
                            var html = DecodeHtml(bytes, charset);
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
                        var el = ResolveElement(row.Arg1);
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
                        var el = ResolveElement(row.Arg1);
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
                        var el = ResolveElement(row.Arg1);
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
                        var el = ResolveElement(row.Arg1);
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
                            Task.Delay(delayMs).Wait();
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
                        var listEl = ResolveElement(row.Arg1);
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
                }
            }
            catch { }
            return ("(unsupported)", null);
        }

        private static string DecodeHtml(byte[] bytes, string? headerCharset)
        {
            try
            {
                Encoding enc = Encoding.UTF8;
                if (!string.IsNullOrWhiteSpace(headerCharset)) { try { enc = Encoding.GetEncoding(headerCharset!); } catch { enc = Encoding.UTF8; } }
                var text = enc.GetString(bytes);
                var sample = text.Length > 8192 ? text.Substring(0, 8192) : text;
                var m = Regex.Match(sample, "charset=([^\"'>]+)", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var cs = m.Groups[1].Value.Trim().TrimEnd(';');
                    try { var e2 = Encoding.GetEncoding(cs); text = e2.GetString(bytes); } catch { }
                }
                return text;
            }
            catch { return Encoding.UTF8.GetString(bytes); }
        }

        // Element resolution with staleness detection and retry (inspired by legacy PacsService validation pattern)
        private const int ElementResolveMaxAttempts = 3;
        private const int ElementResolveRetryDelayMs = 150;

        private static AutomationElement? ResolveElement(ProcArg arg)
        {
            var type = ParseArgKind(arg.Type);
            
            // Handle Element type (bookmark-based resolution)
            if (type == ArgKind.Element)
            {
                var tag = arg.Value ?? string.Empty;
                if (!Enum.TryParse<UiBookmarks.KnownControl>(tag, out var key)) return null;

                // Strategy: Try cache first, validate it, then resolve fresh with retry on staleness
                for (int attempt = 0; attempt < ElementResolveMaxAttempts; attempt++)
                {
                    // Attempt 1: Check cache
                    var cached = GetCached(key);
                    if (cached != null)
                    {
                        if (IsElementAlive(cached))
                        {
                            return cached; // Cache hit, element valid
                        }
                        else
                        {
                            // Stale element in cache, remove it
                            _controlCache.Remove(key);
                        }
                    }

                    // Attempt 2: Resolve fresh from bookmark
                    try
                    {
                        var tuple = UiBookmarks.Resolve(key);
                        if (tuple.element != null)
                        {
                            // Validate the newly resolved element before caching
                            if (IsElementAlive(tuple.element))
                            {
                                StoreCache(key, tuple.element);
                                return tuple.element;
                            }

                            // Element resolved but not alive - retry with delay
                            Task.Delay(ElementResolveRetryDelayMs).Wait();
                        }
                    }
                    catch { }
                }
                
                // All attempts exhausted for Element type
                return null;
            }
            
            // Handle Var type (variable containing cached element reference)
            if (type == ArgKind.Var)
            {
                var varValue = ResolveString(arg, new Dictionary<string, string?>()) ?? string.Empty;
                
                // Check if this variable contains a cached element reference
                if (_elementCache.TryGetValue(varValue, out var cachedElement))
                {
                    // Validate element is still alive
                    if (IsElementAlive(cachedElement))
                    {
                        return cachedElement;
                    }
                    else
                    {
                        // Element is stale, remove from cache
                        _elementCache.Remove(varValue);
                        return null;
                    }
                }
            }
            
            return null;
        }

        private static ArgKind ParseArgKind(string s)
        {
            return s.Trim() switch
            {
                "Element" => ArgKind.Element,
                "String" => ArgKind.String,
                "Number" => ArgKind.Number,
                "Var" => ArgKind.Var,
                _ => ArgKind.String,
            };
        }

        private static bool IsElementAlive(AutomationElement el)
        {
            try
            {
                // Test if element is still accessible by checking Name property
                _ = el.Name;
                
                // Additional validation: check if element has valid bounds
                var rect = el.BoundingRectangle;
                return true; // Element is accessible
            }
            catch
            {
                return false; // Element is stale or not accessible
            }
        }

        private static string ResolveString(ProcArg arg, Dictionary<string, string?> vars)
        {
            var type = ParseArgKind(arg.Type);
            
            if (type == ArgKind.Element)
            {
                // For ArgKind.Element, resolve using the elements dictionary
                var key = arg.Value ?? string.Empty;
                return _elementCache.TryGetValue(key, out var el) && el != null ? el.Name : string.Empty;
            }
            
            if (type == ArgKind.Var)
            {
                // For ArgKind.Var, look up value in vars dictionary
                vars.TryGetValue(arg.Value ?? string.Empty, out var value);
                return value ?? string.Empty;
            }
            
            // For String and Number types, return the value directly
            return arg.Value ?? string.Empty;
        }

        private static AutomationElement ResolveElement(string key)
        {
            // Attempt to resolve an element by its key (name or automationId)
            if (_elementCache.TryGetValue(key, out var el) && el != null) return el;
            return null;
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
