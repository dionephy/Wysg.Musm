using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// ProcedureExecutor: Main coordinator for UI automation procedures.
    /// This class orchestrates procedure execution, variable management, and special operations.
    /// 
    /// Architecture (split across multiple files using partial classes):
    /// - ProcedureExecutor.cs (this file): Main API, execution flow, special handlers
    /// - ProcedureExecutor.Models.cs: Data models (ProcStore, ProcOpRow, ProcArg, ArgKind)
    /// - ProcedureExecutor.Storage.cs: Persistence layer (Load/Save/GetProcPath)
    /// - ProcedureExecutor.Elements.cs: Element resolution, caching, staleness detection
    /// - ProcedureExecutor.Operations.cs: Individual operation implementations
    /// </summary>
    internal static partial class ProcedureExecutor
    {
        /// <summary>
        /// Public API: Execute a procedure by method tag asynchronously.
        /// Runs on background thread to avoid blocking UI.
        /// </summary>
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

        /// <summary>
        /// Main execution logic: resolves procedures, handles special operations, 
        /// executes steps, and manages variable scope.
        /// </summary>
        private static string? ExecuteInternal(string methodTag)
        {
            if (string.IsNullOrWhiteSpace(methodTag)) return null;
            
            // Clear element cache before executing procedure
            _elementCache.Clear();
            
            // Special handling for direct MainViewModel reads (no procedure needed)
            if (string.Equals(methodTag, "GetCurrentPatientNumber", StringComparison.OrdinalIgnoreCase))
            {
                return GetCurrentPatientNumberDirect();
            }
            
            if (string.Equals(methodTag, "GetCurrentStudyDateTime", StringComparison.OrdinalIgnoreCase))
            {
                return GetCurrentStudyDateTimeDirect();
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
            string? lastOperationResult = null;
            
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
                
                lastOperationResult = value;
            }
            
            Debug.WriteLine($"[ProcedureExecutor][ExecuteInternal] All steps completed. Last result: '{lastOperationResult}'");

            // Special handling for comparison operations
            if (string.Equals(methodTag, "PatientNumberMatch", StringComparison.OrdinalIgnoreCase))
            {
                return ComparePatientNumber(lastOperationResult);
            }
            
            if (string.Equals(methodTag, "StudyDateTimeMatch", StringComparison.OrdinalIgnoreCase))
            {
                return CompareStudyDateTime(lastOperationResult);
            }
            
            return lastOperationResult ?? string.Empty;
        }

        /// <summary>
        /// Direct read from MainViewModel for patient number.
        /// </summary>
        private static string GetCurrentPatientNumberDirect()
        {
            try
            {
                Debug.WriteLine("[ProcedureExecutor][GetCurrentPatientNumber] Starting direct read");
                
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

        /// <summary>
        /// Direct read from MainViewModel for study datetime.
        /// </summary>
        private static string GetCurrentStudyDateTimeDirect()
        {
            try
            {
                Debug.WriteLine("[ProcedureExecutor][GetCurrentStudyDateTime] Starting direct read");
                
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
                            
                            if (!string.IsNullOrWhiteSpace(rawValue) && DateTime.TryParse(rawValue, out var dt))
                            {
                                result = dt.ToString("yyyy-MM-dd HH:mm:ss");
                                Debug.WriteLine($"[ProcedureExecutor][GetCurrentStudyDateTime] SUCCESS: Formatted='{result}'");
                            }
                            else
                            {
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

        /// <summary>
        /// Compare PACS patient number with MainViewModel current patient.
        /// </summary>
        private static string ComparePatientNumber(string? pacsValue)
        {
            try
            {
                Debug.WriteLine("[ProcedureExecutor][PatientNumberMatch] Starting comparison");
                var mainWindow = System.Windows.Application.Current?.MainWindow;
                if (mainWindow != null && mainWindow.DataContext is ViewModels.MainViewModel mainVM)
                {
                    string Normalize(string? s) => string.IsNullOrWhiteSpace(s) ? string.Empty : 
                        System.Text.RegularExpressions.Regex.Replace(s, "[^A-Za-z0-9]", "").ToUpperInvariant();
                    
                    var pacsPatientNumber = Normalize(pacsValue);
                    var mainPatientNumber = Normalize(mainVM.PatientNumber);
                    Debug.WriteLine($"[ProcedureExecutor][PatientNumberMatch] PACS='{pacsPatientNumber}' Main='{mainPatientNumber}'");
                    
                    bool matches = string.Equals(pacsPatientNumber, mainPatientNumber, StringComparison.Ordinal);
                    Debug.WriteLine($"[ProcedureExecutor][PatientNumberMatch] Result: {matches}");
                    return matches ? "true" : "false";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProcedureExecutor][PatientNumberMatch] EXCEPTION: {ex.GetType().Name} - {ex.Message}");
            }
            return "false";
        }

        /// <summary>
        /// Compare PACS study datetime with MainViewModel current study datetime.
        /// </summary>
        private static string CompareStudyDateTime(string? pacsValue)
        {
            try
            {
                Debug.WriteLine("[ProcedureExecutor][StudyDateTimeMatch] Starting comparison");
                var mainWindow = System.Windows.Application.Current?.MainWindow;
                if (mainWindow != null && mainWindow.DataContext is ViewModels.MainViewModel mainVM)
                {
                    Debug.WriteLine($"[ProcedureExecutor][StudyDateTimeMatch] PACS='{pacsValue}' Main='{mainVM.StudyDateTime}'");
                    
                    if (DateTime.TryParse(pacsValue, out var pacsDateTime) &&
                        DateTime.TryParse(mainVM.StudyDateTime, out var mainDateTime))
                    {
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
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProcedureExecutor][StudyDateTimeMatch] EXCEPTION: {ex.GetType().Name} - {ex.Message}");
            }
            return "false";
        }

        /// <summary>
        /// Create fallback procedures for known method tags.
        /// </summary>
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
            
            if (string.Equals(methodTag, "InvokeOpenWorklist", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    new ProcOpRow { Op = "Invoke", Arg1 = new ProcArg { Type = nameof(ArgKind.Element), Value = UiBookmarks.KnownControl.WorklistOpenButton.ToString() }, Arg1Enabled = true, Arg2Enabled = false, Arg3Enabled = false }
                };
            }
            
            if (string.Equals(methodTag, "SetFocusSearchResultsList", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    new ProcOpRow { Op = "SetFocus", Arg1 = new ProcArg { Type = nameof(ArgKind.Element), Value = UiBookmarks.KnownControl.SearchResultsList.ToString() }, Arg1Enabled = true, Arg2Enabled = false, Arg3Enabled = false }
                };
            }
            
            if (string.Equals(methodTag, "SendReport", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    new ProcOpRow { Op = "Invoke", Arg1 = new ProcArg { Type = nameof(ArgKind.Element), Value = UiBookmarks.KnownControl.SendReportButton.ToString() }, Arg1Enabled = true, Arg2Enabled = false, Arg3Enabled = false }
                };
            }
            
            if (string.Equals(methodTag, "PatientNumberMatch", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    new ProcOpRow { Op = "GetCurrentPatientNumber", Arg1Enabled = false, Arg2Enabled = false, Arg3Enabled = false, OutputVar = "var1" }
                };
            }
            
            if (string.Equals(methodTag, "StudyDateTimeMatch", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    new ProcOpRow { Op = "GetCurrentStudyDateTime", Arg1Enabled = false, Arg2Enabled = false, Arg3Enabled = false, OutputVar = "var1" }
                };
            }
            
            return new List<ProcOpRow>();
        }
    }
}
