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
                Debug.WriteLine("[GetCurrentHeader] Starting operation - getting actual editor text");
                string result = string.Empty;
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    var mainWindow = System.Windows.Application.Current?.MainWindow;
                    if (mainWindow != null)
                    {
                        // Get CenterEditingArea from MainWindow
                        var gridCenter = mainWindow.FindName("gridCenter") as Controls.CenterEditingArea;
                        if (gridCenter != null)
                        {
                            // Get EditorHeader from CenterEditingArea
                            var editorHeader = gridCenter.EditorHeader;
                            if (editorHeader != null)
                            {
                                // Get the underlying MusmEditor (TextEditor)
                                var musmEditor = editorHeader.FindName("Editor") as ICSharpCode.AvalonEdit.TextEditor;
                                if (musmEditor != null)
                                {
                                    result = musmEditor.Text ?? string.Empty;
                                    Debug.WriteLine($"[GetCurrentHeader] SUCCESS: Got text from editor, length={result.Length}");
                                }
                                else
                                {
                                    Debug.WriteLine($"[GetCurrentHeader] FAIL: MusmEditor not found in EditorHeader");
                                }
                            }
                            else
                            {
                                Debug.WriteLine($"[GetCurrentHeader] FAIL: EditorHeader not found in CenterEditingArea");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"[GetCurrentHeader] FAIL: CenterEditingArea (gridCenter) not found");
                        }
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
                Debug.WriteLine("[GetCurrentFindings] Starting operation - getting actual editor text");
                string result = string.Empty;
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    var mainWindow = System.Windows.Application.Current?.MainWindow;
                    if (mainWindow != null)
                    {
                        // Get CenterEditingArea from MainWindow
                        var gridCenter = mainWindow.FindName("gridCenter") as Controls.CenterEditingArea;
                        if (gridCenter != null)
                        {
                            // Get EditorFindings from CenterEditingArea
                            var editorFindings = gridCenter.EditorFindings;
                            if (editorFindings != null)
                            {
                                // Get the underlying MusmEditor (TextEditor)
                                var musmEditor = editorFindings.FindName("Editor") as ICSharpCode.AvalonEdit.TextEditor;
                                if (musmEditor != null)
                                {
                                    result = musmEditor.Text ?? string.Empty;
                                    Debug.WriteLine($"[GetCurrentFindings] SUCCESS: Got text from editor, length={result.Length}");
                                }
                                else
                                {
                                    Debug.WriteLine($"[GetCurrentFindings] FAIL: MusmEditor not found in EditorFindings");
                                }
                            }
                            else
                            {
                                Debug.WriteLine($"[GetCurrentFindings] FAIL: EditorFindings not found in CenterEditingArea");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"[GetCurrentFindings] FAIL: CenterEditingArea (gridCenter) not found");
                        }
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
                Debug.WriteLine("[GetCurrentConclusion] Starting operation - getting actual editor text");
                string result = string.Empty;
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    var mainWindow = System.Windows.Application.Current?.MainWindow;
                    if (mainWindow != null)
                    {
                        // Get CenterEditingArea from MainWindow
                        var gridCenter = mainWindow.FindName("gridCenter") as Controls.CenterEditingArea;
                        if (gridCenter != null)
                        {
                            // Get EditorConclusion from CenterEditingArea
                            var editorConclusion = gridCenter.EditorConclusion;
                            if (editorConclusion != null)
                            {
                                // Get the underlying MusmEditor (TextEditor)
                                var musmEditor = editorConclusion.FindName("Editor") as ICSharpCode.AvalonEdit.TextEditor;
                                if (musmEditor != null)
                                {
                                    result = musmEditor.Text ?? string.Empty;
                                    Debug.WriteLine($"[GetCurrentConclusion] SUCCESS: Got text from editor, length={result.Length}");
                                }
                                else
                                {
                                    Debug.WriteLine($"[GetCurrentConclusion] FAIL: MusmEditor not found in EditorConclusion");
                                }
                            }
                            else
                            {
                                Debug.WriteLine($"[GetCurrentConclusion] FAIL: EditorConclusion not found in CenterEditingArea");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"[GetCurrentConclusion] FAIL: CenterEditingArea (gridCenter) not found");
                        }
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
