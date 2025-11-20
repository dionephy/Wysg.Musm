using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FlaUI.Core.AutomationElements;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.Views
{
    public partial class SpyWindow
    {
        private static bool NeedsAsync(string? op)
        {
            return string.Equals(op, "GetTextOCR", StringComparison.OrdinalIgnoreCase)
                || string.Equals(op, "GetHTML", StringComparison.OrdinalIgnoreCase);
        }

        // Runtime element cache for storing elements from GetSelectedElement
        private readonly Dictionary<string, FlaUI.Core.AutomationElements.AutomationElement> _elementCache = new();

        private bool _handlingProcOpChange;
        private void OnProcOpChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_handlingProcOpChange) return;
            if (sender is System.Windows.Controls.ComboBox cb && cb.DataContext is ProcOpRow row)
            {
                try
                {
                    _handlingProcOpChange = true;
                    switch (row.Op)
                    {
                        case "GetText":
                        case "GetTextOCR":
                        case "GetName":
                        case "Invoke":
                        case "MouseMoveToElement":
                        case "IsVisible":
                        case "SetFocus":
                            row.Arg1.Type = nameof(ArgKind.Element); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "SetValue":
                            // SetValue: Arg1=Element (target control), Arg2=String or Var (value to set)
                            row.Arg1.Type = nameof(ArgKind.Element); row.Arg1Enabled = true;
                            row.Arg2Enabled = true; // Allow String or Var
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "SetValueWeb":
                            // SetValueWeb: Arg1=Element (target control), Arg2=String or Var (value to set)
                            row.Arg1.Type = nameof(ArgKind.Element); row.Arg1Enabled = true;
                            row.Arg2Enabled = true; // Allow String or Var
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "ClickElement":
                            // ClickElement accepts both Element (bookmark) and Var (from GetSelectedElement output)
                            // Don't reset Type if already set by user - only enable/disable args
                            row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "ClickElementAndStay":
                            // ClickElementAndStay: clicks element but leaves cursor at element (no restore)
                            // Accepts both Element (bookmark) and Var (from GetSelectedElement output)
                            row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "SetClipboard":
                            // FIX: SetClipboard accepts both String and Var types - don't force to String
                            // Only enable Arg1, keep user's Type selection
                            row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "TrimString":
                            // NEW: TrimString trims Arg2 away from Arg1 (both accept String or Var)
                            row.Arg1Enabled = true; // Source string (String or Var)
                            row.Arg2Enabled = true; // String to trim away (String or Var)
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "Delay":
                            // Delay: pauses execution for specified milliseconds
                            row.Arg1.Type = nameof(ArgKind.Number); row.Arg1Enabled = true; if (string.IsNullOrWhiteSpace(row.Arg1.Value)) row.Arg1.Value = "100";
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "SimulateTab":
                        case "SimulatePaste":
                        case "SimulateSelectAll":
                        case "SimulateDelete":
                        case "GetCurrentPatientNumber":
                        case "GetCurrentStudyDateTime":
                        case "GetCurrentHeader":
                        case "GetCurrentFindings":
                        case "GetCurrentConclusion":
                            // These operations don't require any arguments
                            row.Arg1.Type = nameof(ArgKind.String); row.Arg1Enabled = false; row.Arg1.Value = string.Empty;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "Split":
                            row.Arg1.Type = nameof(ArgKind.Var); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = true; if (string.IsNullOrWhiteSpace(row.Arg2.Value)) row.Arg2.Value = ",";
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = true; if (string.IsNullOrWhiteSpace(row.Arg3.Value)) row.Arg3.Value = "0";
                            break;
                        case "IsMatch":
                        case "IsAlmostMatch":
                            // IsMatch/IsAlmostMatch: Arg1=input (Var), Arg2=comparison value (String or Var)
                            // Don't force Arg2 type - let user choose between String literal and Var
                            row.Arg1Enabled = true; // Typically Var but could be String
                            row.Arg2Enabled = true; // Allow String or Var
                            row.Arg3.Type = nameof(ArgKind.String); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "And":
                            // And: Arg1=boolean var, Arg2=boolean var, returns "true" if both true
                            row.Arg1.Type = nameof(ArgKind.Var); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.Var); row.Arg2Enabled = true;
                            row.Arg3.Type = nameof(ArgKind.String); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "Replace":
                        case "Merge":
                            // Replace/Merge: Arg1=input (Var), Arg2=search/input2 (String or Var), Arg3=replacement/separator (String or Var)
                            // Don't force types - let user choose between String and Var
                            row.Arg1Enabled = true;
                            row.Arg2Enabled = true;
                            row.Arg3Enabled = true;
                            break;
                        case "GetHTML":
                            row.Arg1.Type = nameof(ArgKind.Var); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.String); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "MouseClick":
                            row.Arg1.Type = nameof(ArgKind.Number); row.Arg1Enabled = true; if (string.IsNullOrWhiteSpace(row.Arg1.Value)) row.Arg1.Value = "0";
                            row.Arg2.Type = nameof(ArgKind.Number); row.Arg2Enabled = true; if (string.IsNullOrWhiteSpace(row.Arg2.Value)) row.Arg2.Value = "0";
                            row.Arg3.Type = nameof(ArgKind.String); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "TakeLast":
                        case "Trim":
                        case "ToDateTime":
                            row.Arg1.Type = nameof(ArgKind.Var); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "GetValueFromSelection":
                            row.Arg1.Type = nameof(ArgKind.Element); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = true; if (string.IsNullOrWhiteSpace(row.Arg2.Value)) row.Arg2.Value = "ID";
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "GetSelectedElement":
                            row.Arg1.Type = nameof(ArgKind.Element); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        default:
                            row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                    }
                }
                finally { _handlingProcOpChange = false; }
            }
        }

        private void OnAddProcRow(object sender, RoutedEventArgs e)
        {
            if (FindName("gridProcSteps") is System.Windows.Controls.DataGrid procGrid) returnToList(procGrid, list => list.Add(new ProcOpRow()));
        }
        private void OnRemoveProcRow(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button b && b.Tag is ProcOpRow row && FindName("gridProcSteps") is System.Windows.Controls.DataGrid procGrid)
            {
                returnToList(procGrid, list => list.Remove(row));
            }
        }
        private async void OnSetProcRow(object sender, RoutedEventArgs e)
        {
            if (FindName("gridProcSteps") is not System.Windows.Controls.DataGrid procGrid) return;
            try { procGrid.CommitEdit(DataGridEditingUnit.Cell, true); procGrid.CommitEdit(DataGridEditingUnit.Row, true); } catch { }
            var list = procGrid.Items.OfType<ProcOpRow>().ToList();
            if (sender is System.Windows.Controls.Button b && b.Tag is ProcOpRow row)
            {
                var index = list.IndexOf(row); if (index < 0) return;
                var vars = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < index; i++) vars[$"var{i + 1}"] = list[i].OutputVar != null ? list[i].OutputPreview : null;
                var varName = $"var{index + 1}";
                var result = NeedsAsync(row.Op)
                    ? await ExecuteSingleAsync(row, vars)
                    : ExecuteSingle(row, vars);
                row.OutputVar = varName;
                row.OutputPreview = result.preview;
                if (!ProcedureVars.Contains(varName)) ProcedureVars.Add(varName);
                procGrid.ItemsSource = null; procGrid.ItemsSource = list;
            }
        }

        private void returnToList(System.Windows.Controls.DataGrid grid, Action<System.Collections.Generic.List<ProcOpRow>> mutator)
        {
            var list = grid.Items.OfType<ProcOpRow>().ToList();
            mutator(list);
            grid.ItemsSource = null; grid.ItemsSource = list;
            UpdateProcedureVarsFrom(list);
        }

        private static string UnescapeUserText(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            try { return Regex.Unescape(s); } catch { return s; }
        }

        private (string preview, string? value) ExecuteSingle(ProcOpRow row, Dictionary<string, string?> vars)
        {
            // Delegate to shared OperationExecutor service
            return OperationExecutor.ExecuteOperation(
                row.Op,
                resolveArg1Element: () => ResolveElement(row.Arg1, vars),
                resolveArg1String: () => ResolveString(row.Arg1, vars),
                resolveArg2String: () => ResolveString(row.Arg2, vars),
                resolveArg3String: () => ResolveString(row.Arg3, vars),
                elementCache: _elementCache
            );
        }

        private async Task<(string preview, string? value)> ExecuteSingleAsync(ProcOpRow row, Dictionary<string, string?> vars)
        {
            // Delegate to shared OperationExecutor service for async operations
            return await OperationExecutor.ExecuteOperationAsync(
                row.Op,
                resolveArg1Element: () => ResolveElement(row.Arg1, vars),
                resolveArg1String: () => ResolveString(row.Arg1, vars),
                resolveArg2String: () => ResolveString(row.Arg2, vars),
                resolveArg3String: () => ResolveString(row.Arg3, vars),
                elementCache: _elementCache
            );
        }

        private void UpdateProcedureVarsFrom(System.Collections.Generic.List<ProcOpRow> rows)
        {
            ProcedureVars.Clear();
            foreach (var r in rows)
                if (!string.IsNullOrWhiteSpace(r.OutputVar) && !ProcedureVars.Contains(r.OutputVar)) ProcedureVars.Add(r.OutputVar);
        }

        private FlaUI.Core.AutomationElements.AutomationElement? ResolveElement(ProcArg arg, Dictionary<string, string?> vars)
        {
            var type = ParseArgKind(arg.Type); 
            
            // Handle Element type (bookmark-based resolution)
            if (type == ArgKind.Element)
            {
                var tag = arg.Value ?? string.Empty; 
                if (!Enum.TryParse<UiBookmarks.KnownControl>(tag, out var key)) return null;
                var tuple = UiBookmarks.Resolve(key); 
                return tuple.element;
            }
            
            // Handle Var type (variable containing cached element reference)
            if (type == ArgKind.Var)
            {
                var varName = arg.Value ?? string.Empty;
                Debug.WriteLine($"[SpyWindow][ResolveElement][Var] Resolving variable '{varName}'");
                
                // First resolve the variable value from vars dictionary
                if (!vars.TryGetValue(varName, out var varValue) || string.IsNullOrWhiteSpace(varValue))
                {
                    Debug.WriteLine($"[SpyWindow][ResolveElement][Var] Variable '{varName}' not found in vars dictionary or is empty");
                    return null;
                }
                
                Debug.WriteLine($"[SpyWindow][ResolveElement][Var] Variable '{varName}' resolved to: '{varValue}'");
                
                // Check if this variable contains a cached element reference
                if (_elementCache.TryGetValue(varValue, out var cachedElement))
                {
                    Debug.WriteLine($"[SpyWindow][ResolveElement][Var] Found cached element for key '{varValue}'");
                    // Validate element is still alive
                    try
                    {
                        _ = cachedElement.Name; // Test if element is still accessible
                        Debug.WriteLine($"[SpyWindow][ResolveElement][Var] Cached element is still alive");
                        return cachedElement;
                    }
                    catch
                    {
                        // Element is stale, remove from cache
                        Debug.WriteLine($"[SpyWindow][ResolveElement][Var] Cached element is stale, removing from cache");
                        _elementCache.Remove(varValue);
                        return null;
                    }
                }
                else
                {
                    Debug.WriteLine($"[SpyWindow][ResolveElement][Var] No cached element found for key '{varValue}'");
                }
            }
            
            return null;
        }

        private static string? ResolveString(ProcArg arg, Dictionary<string, string?> vars)
        {
            var type = ParseArgKind(arg.Type);
            return type switch
            {
                ArgKind.Var => (arg.Value != null && vars.TryGetValue(arg.Value, out var v)) ? v : null,
                ArgKind.String => arg.Value,
                ArgKind.Number => arg.Value,
                _ => null
            };
        }

        private static ArgKind ParseArgKind(string? s)
        {
            if (Enum.TryParse<ArgKind>(s, true, out var k)) return k;
            return s?.Equals("Var", StringComparison.OrdinalIgnoreCase) == true ? ArgKind.Var : ArgKind.String;
        }

        private void OnOpComboPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.ComboBox cb && !cb.IsDropDownOpen)
                {
                    e.Handled = true;
                    var cell = FindParent<System.Windows.Controls.DataGridCell>(cb);
                    var grid = FindParent<System.Windows.Controls.DataGrid>(cell) ?? (System.Windows.Controls.DataGrid?)FindName("gridProcSteps");
                    try { grid?.BeginEdit(); } catch { }
                    cb.Focus(); cb.IsDropDownOpen = true;
                }
            }
            catch { }
        }

        private void OnOpComboPreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.ComboBox cb)
                {
                    if (e.Key == Key.F4 || (e.Key == Key.Down && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) || e.Key == Key.Space)
                    {
                        var cell = FindParent<System.Windows.Controls.DataGridCell>(cb);
                        var grid = FindParent<System.Windows.Controls.DataGrid>(cell) ?? (System.Windows.Controls.DataGrid?)FindName("gridProcSteps");
                        try { grid?.BeginEdit(); } catch { }
                        cb.Focus(); cb.IsDropDownOpen = !cb.IsDropDownOpen; e.Handled = true;
                    }
                }
            }
            catch { }
        }

        private static string GetProcPath()
        {
            try
            {
                if (UiBookmarks.GetStorePathOverride != null)
                {
                    var bookmarkPath = UiBookmarks.GetStorePathOverride.Invoke();
                    if (!string.IsNullOrWhiteSpace(bookmarkPath))
                    {
                        var baseDir = System.IO.Path.GetDirectoryName(bookmarkPath);
                        if (!string.IsNullOrEmpty(baseDir))
                        {
                            System.IO.Directory.CreateDirectory(baseDir);
                            return System.IO.Path.Combine(baseDir, "ui-procedures.json");
                        }
                    }
                }

                if (System.Windows.Application.Current is Wysg.Musm.Radium.App app)
                {
                    try
                    {
                        var tenant = (Wysg.Musm.Radium.Services.ITenantContext?)app.Services.GetService(typeof(Wysg.Musm.Radium.Services.ITenantContext));
                        var pacsKey = string.IsNullOrWhiteSpace(tenant?.CurrentPacsKey) ? "default_pacs" : tenant!.CurrentPacsKey;
                        var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wysg.Musm", "Radium", "Pacs", SanitizeFileName(pacsKey));
                        System.IO.Directory.CreateDirectory(dir);
                        return System.IO.Path.Combine(dir, "ui-procedures.json");
                    }
                    catch { }
                }
            }
            catch { }

            var legacyDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wysg.Musm", "Radium");
            System.IO.Directory.CreateDirectory(legacyDir);
            return System.IO.Path.Combine(legacyDir, "ui-procedures.json");
        }
        private static ProcStore LoadProcStore()
        {
            try
            {
                var p = GetProcPath();
                if (!System.IO.File.Exists(p)) return new ProcStore();
                return System.Text.Json.JsonSerializer.Deserialize<ProcStore>(System.IO.File.ReadAllText(p), new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web) { WriteIndented = true }) ?? new ProcStore();
            }
            catch { return new ProcStore(); }
        }
        private static void SaveProcStore(ProcStore s)
        {
            try
            {
                var p = GetProcPath();
                System.IO.File.WriteAllText(p, System.Text.Json.JsonSerializer.Serialize(s, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web) { WriteIndented = true }));
            }
            catch { }
        }
        private static void SaveProcedureForMethod(string methodTag, System.Collections.Generic.List<ProcOpRow> steps)
        { var s = LoadProcStore(); s.Methods[methodTag] = steps; SaveProcStore(s); }
        private static System.Collections.Generic.List<ProcOpRow> LoadProcedureForMethod(string methodTag)
        { var s = LoadProcStore(); return s.Methods.TryGetValue(methodTag, out var steps) ? steps : new System.Collections.Generic.List<ProcOpRow>(); }

        private void OnProcMethodChanged(object? sender, SelectionChangedEventArgs e)
        {
            var cmb = (System.Windows.Controls.ComboBox?)FindName("cmbProcMethod");
            var tag = ((cmb?.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag as string) ?? ((cmb?.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content as string);
            if (FindName("gridProcSteps") is not System.Windows.Controls.DataGrid procGrid || string.IsNullOrWhiteSpace(tag)) return;
            var steps = LoadProcedureForMethod(tag).ToList();
            procGrid.ItemsSource = steps;
            UpdateProcedureVarsFrom(steps);
        }
        private void OnSaveProcedure(object sender, RoutedEventArgs e)
        {
            var cmb = (System.Windows.Controls.ComboBox?)FindName("cmbProcMethod");
            var procGrid = (System.Windows.Controls.DataGrid?)FindName("gridProcSteps");
            var tag = ((cmb?.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag as string) ?? ((cmb?.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content as string);
            if (string.IsNullOrWhiteSpace(tag)) { txtStatus.Text = "Select PACS method"; return; }
            if (procGrid == null) { txtStatus.Text = "No steps"; return; }
            try { procGrid.CommitEdit(DataGridEditingUnit.Cell, true); procGrid.CommitEdit(DataGridEditingUnit.Row, true); } catch { }
            var steps = procGrid.Items.OfType<ProcOpRow>().Where(s => !string.IsNullOrWhiteSpace(s.Op)).ToList();
            SaveProcedureForMethod(tag, steps);
            txtStatus.Text = $"Saved procedure for {tag}";
        }
        private async void OnRunProcedure(object sender, RoutedEventArgs e)
        {
            var cmb = (System.Windows.Controls.ComboBox?)FindName("cmbProcMethod");
            var procGrid = (System.Windows.Controls.DataGrid?)FindName("gridProcSteps");
            var tag = ((cmb?.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag as string) ?? ((cmb?.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content as string);
            if (string.IsNullOrWhiteSpace(tag)) { txtStatus.Text = "Select PACS method"; return; }
            if (procGrid == null) { txtStatus.Text = "No steps"; return; }
            try { procGrid.CommitEdit(DataGridEditingUnit.Cell, true); procGrid.CommitEdit(DataGridEditingUnit.Row, true); } catch { }
            var steps = procGrid.Items.OfType<ProcOpRow>().Where(s => !string.IsNullOrWhiteSpace(s.Op)).ToList();
            var (result, annotated) = await RunProcedureAsync(steps);
            procGrid.ItemsSource = null; procGrid.ItemsSource = annotated; UpdateProcedureVarsFrom(annotated);
            txtStatus.Text = result ?? "(null)";
        }
        private async Task<(string? result, System.Collections.Generic.List<ProcOpRow> annotated)> RunProcedureAsync(System.Collections.Generic.List<ProcOpRow> steps)
        {
            // Clear element cache before running procedure
            _elementCache.Clear();
            
            var vars = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            string? last = null; var annotated = new System.Collections.Generic.List<ProcOpRow>();
            for (int i = 0; i < steps.Count; i++)
            {
                var row = steps[i]; string varName = $"var{i + 1}"; row.OutputVar = varName;
                var res = NeedsAsync(row.Op)
                    ? await ExecuteSingleAsync(row, vars)
                    : ExecuteSingle(row, vars);
                vars[varName] = res.value; if (res.value != null) last = res.value; row.OutputPreview = res.preview; annotated.Add(row);
            }
            return (last, annotated);
        }
    }
}
