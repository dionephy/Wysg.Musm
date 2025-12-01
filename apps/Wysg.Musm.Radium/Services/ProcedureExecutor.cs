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
            var sw = Stopwatch.StartNew(); // Start timing
            Debug.WriteLine($"[ProcedureExecutor][ExecuteAsync] ===== START: {methodTag} =====");
            
            // Clear both element caches at the very start of each execution
            // This ensures no stale cache data from previous invocations
            _elementCache.Clear();
            _controlCache.Clear();
            Debug.WriteLine($"[ProcedureExecutor][ExecuteAsync] Element caches cleared (_elementCache and _controlCache)");
            
            try
            {
                var result = ExecuteInternal(methodTag);
                sw.Stop(); // Stop timing
                Debug.WriteLine($"[ProcedureExecutor][ExecuteAsync] ===== END: {methodTag} ===== ({sw.ElapsedMilliseconds} ms)");
                Debug.WriteLine($"[ProcedureExecutor][ExecuteAsync] Final result: '{result}'");
                Debug.WriteLine($"[ProcedureExecutor][ExecuteAsync] Result length: {result?.Length ?? 0} characters");
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop(); // Stop timing even on exception
                Debug.WriteLine($"[ProcedureExecutor][ExecuteAsync] ===== EXCEPTION in {methodTag} ===== ({sw.ElapsedMilliseconds} ms)");
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
            
            // Note: Element cache is now cleared in ExecuteAsync instead
            
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
                
                // No fallbacks - all procedures must be explicitly defined in AutomationWindow
                Debug.WriteLine($"[ProcedureExecutor][ExecuteInternal] No fallback available for '{methodTag}' - must be configured in AutomationWindow");
                throw new InvalidOperationException($"Custom procedure '{methodTag}' is not defined. Please configure it in AutomationWindow ¡æ Custom Procedures for this PACS profile.");
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
    }
}
