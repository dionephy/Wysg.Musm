using System;
using System.Diagnostics;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// OperationExecutor partial class: MainViewModel data access operations.
    /// Contains operations for reading current patient, study, and report data from MainViewModel.
    /// </summary>
    internal static partial class OperationExecutor
    {
        #region MainViewModel Operations

        private static (string preview, string? value) ExecuteGetCurrentPatientNumber()
        {
            try
            {
                Debug.WriteLine("[GetCurrentPatientNumber] Starting operation");
                string result = string.Empty;
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    var mainWindow = System.Windows.Application.Current?.MainWindow;
                    if (mainWindow != null && mainWindow.DataContext is ViewModels.MainViewModel mainVM)
                    {
                        result = mainVM.PatientNumber ?? string.Empty;
                        Debug.WriteLine($"[GetCurrentPatientNumber] SUCCESS: PatientNumber='{result}'");
                    }
                    else
                    {
                        Debug.WriteLine($"[GetCurrentPatientNumber] FAIL: MainViewModel not found");
                    }
                });
                return (string.IsNullOrWhiteSpace(result) ? "(empty)" : result, result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GetCurrentPatientNumber] EXCEPTION: {ex.Message}");
                return ($"(error: {ex.Message})", null);
            }
        }

        private static (string preview, string? value) ExecuteGetCurrentStudyDateTime()
        {
            try
            {
                Debug.WriteLine("[GetCurrentStudyDateTime] Starting operation");
                string result = string.Empty;
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    var mainWindow = System.Windows.Application.Current?.MainWindow;
                    if (mainWindow != null && mainWindow.DataContext is ViewModels.MainViewModel mainVM)
                    {
                        var rawValue = mainVM.StudyDateTime ?? string.Empty;
                        Debug.WriteLine($"[GetCurrentStudyDateTime] Raw value: '{rawValue}'");

                        if (!string.IsNullOrWhiteSpace(rawValue) && DateTime.TryParse(rawValue, out var studyDt))
                        {
                            result = studyDt.ToString("yyyy-MM-dd HH:mm:ss");
                            Debug.WriteLine($"[GetCurrentStudyDateTime] SUCCESS: Formatted='{result}'");
                        }
                        else
                        {
                            result = rawValue;
                            Debug.WriteLine($"[GetCurrentStudyDateTime] WARN: Failed to parse datetime");
                        }
                    }
                });
                return (string.IsNullOrWhiteSpace(result) ? "(empty)" : result, result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GetCurrentStudyDateTime] EXCEPTION: {ex.Message}");
                return ($"(error: {ex.Message})", null);
            }
        }

        private static (string preview, string? value) ExecuteGetCurrentHeader()
        {
            try
            {
                Debug.WriteLine("[GetCurrentHeader] Starting operation");
                string result = string.Empty;
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    var mainWindow = System.Windows.Application.Current?.MainWindow;
                    if (mainWindow != null && mainWindow.DataContext is ViewModels.MainViewModel mainVM)
                    {
                        result = mainVM.HeaderText ?? string.Empty;
                        Debug.WriteLine($"[GetCurrentHeader] SUCCESS: length={result.Length}");
                    }
                });
                return (string.IsNullOrWhiteSpace(result) ? "(empty)" : result, result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GetCurrentHeader] EXCEPTION: {ex.Message}");
                return ($"(error: {ex.Message})", null);
            }
        }

        private static (string preview, string? value) ExecuteGetCurrentFindings()
        {
            try
            {
                Debug.WriteLine("[GetCurrentFindings] Starting operation");
                string result = string.Empty;
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    var mainWindow = System.Windows.Application.Current?.MainWindow;
                    if (mainWindow != null && mainWindow.DataContext is ViewModels.MainViewModel mainVM)
                    {
                        result = mainVM.FindingsText ?? string.Empty;
                        Debug.WriteLine($"[GetCurrentFindings] SUCCESS: length={result.Length}");
                    }
                });
                return (string.IsNullOrWhiteSpace(result) ? "(empty)" : result, result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GetCurrentFindings] EXCEPTION: {ex.Message}");
                return ($"(error: {ex.Message})", null);
            }
        }

        private static (string preview, string? value) ExecuteGetCurrentConclusion()
        {
            try
            {
                Debug.WriteLine("[GetCurrentConclusion] Starting operation");
                string result = string.Empty;
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    var mainWindow = System.Windows.Application.Current?.MainWindow;
                    if (mainWindow != null && mainWindow.DataContext is ViewModels.MainViewModel mainVM)
                    {
                        result = mainVM.ConclusionText ?? string.Empty;
                        Debug.WriteLine($"[GetCurrentConclusion] SUCCESS: length={result.Length}");
                    }
                });
                return (string.IsNullOrWhiteSpace(result) ? "(empty)" : result, result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GetCurrentConclusion] EXCEPTION: {ex.Message}");
                return ($"(error: {ex.Message})", null);
            }
        }

        #endregion
    }
}
