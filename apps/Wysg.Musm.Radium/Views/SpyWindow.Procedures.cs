using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls; // WPF controls
using System.Windows.Input;
using FlaUI.Core.AutomationElements;
using Wysg.Musm.Radium.Services; // UiBookmarks

namespace Wysg.Musm.Radium.Views
{
    /// <summary>
    /// Partial: Custom procedure authoring & execution for SpyWindow.
    /// Provides a lightweight DSL that operates over resolved UI elements or string variables.
    /// </summary>
    public partial class SpyWindow
    {
        // ------------------------------------------------------------------
        // Procedure model
        // ------------------------------------------------------------------
        private enum ArgKind { Element, String, Number, Var }

        /// <summary>Single argument for an operation (typed: Element | String | Number | Var)</summary>
        private sealed class ProcArg : INotifyPropertyChanged
        {
            private string _type = nameof(ArgKind.String);
            private string? _value;
            public string Type { get => _type; set => SetField(ref _type, value); }
            public string? Value { get => _value; set => SetField(ref _value, value); }
            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
            private bool SetField<T>(ref T f, T v, [CallerMemberName] string? n = null)
            { if (EqualityComparer<T>.Default.Equals(f, v)) return false; f = v; OnPropertyChanged(n); return true; }
        }

        /// <summary>Row == single operation invocation with up to 3 arguments and an output variable.</summary>
        private sealed class ProcOpRow : INotifyPropertyChanged
        {
            private string _op = string.Empty;
            private ProcArg _arg1 = new();
            private ProcArg _arg2 = new();
            private ProcArg _arg3 = new();
            private bool _arg1Enabled = true;
            private bool _arg2Enabled = true;
            private bool _arg3Enabled = false;
            private string? _outputVar;
            private string? _outputPreview;
            public string Op { get => _op; set => SetField(ref _op, value); }
            public ProcArg Arg1 { get => _arg1; set => SetField(ref _arg1, value); }
            public ProcArg Arg2 { get => _arg2; set => SetField(ref _arg2, value); }
            public ProcArg Arg3 { get => _arg3; set => SetField(ref _arg3, value); }
            public bool Arg1Enabled { get => _arg1Enabled; set => SetField(ref _arg1Enabled, value); }
            public bool Arg2Enabled { get => _arg2Enabled; set => SetField(ref _arg2Enabled, value); }
            public bool Arg3Enabled { get => _arg3Enabled; set => SetField(ref _arg3Enabled, value); }
            public string? OutputVar { get => _outputVar; set => SetField(ref _outputVar, value); }
            public string? OutputPreview { get => _outputPreview; set => SetField(ref _outputPreview, value); }
            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
            private bool SetField<T>(ref T f, T v, [CallerMemberName] string? n = null)
            { if (EqualityComparer<T>.Default.Equals(f, v)) return false; f = v; OnPropertyChanged(n); return true; }
        }

        /// <summary>Persistence store for procedures by method tag.</summary>
        private sealed class ProcStore { public Dictionary<string, List<ProcOpRow>> Methods { get; set; } = new(); }

        // ------------------------------------------------------------------
        // Procedure grid event handlers
        // ------------------------------------------------------------------
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
                (string preview, string? value) result = string.Equals(row.Op, "GetTextOCR", StringComparison.OrdinalIgnoreCase)
                    ? await ExecuteSingleAsync(row, vars)
                    : ExecuteSingle(row, vars);
                row.OutputVar = varName;
                row.OutputPreview = result.preview;
                if (!ProcedureVars.Contains(varName)) ProcedureVars.Add(varName);
                procGrid.ItemsSource = null; procGrid.ItemsSource = list; // refresh
            }
        }

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
                            row.Arg1.Type = nameof(ArgKind.Element); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "Split":
                            row.Arg1.Type = nameof(ArgKind.Var); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = true; if (string.IsNullOrWhiteSpace(row.Arg2.Value)) row.Arg2.Value = ",";
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = true; if (string.IsNullOrWhiteSpace(row.Arg3.Value)) row.Arg3.Value = "0";
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
                        default:
                            row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                    }
                }
                finally { _handlingProcOpChange = false; }
            }
        }

        // Helper local function to manipulate list & refresh grid
        private void returnToList(System.Windows.Controls.DataGrid grid, Action<List<ProcOpRow>> mutator)
        {
            var list = grid.Items.OfType<ProcOpRow>().ToList();
            mutator(list);
            grid.ItemsSource = null; grid.ItemsSource = list;
            UpdateProcedureVarsFrom(list);
        }

        // ------------------------------------------------------------------
        // Execution (unchanged logic)
        // ------------------------------------------------------------------
        private (string preview, string? value) ExecuteSingle(ProcOpRow row, Dictionary<string, string?> vars)
        {
            string? valueToStore = null; string preview;
            switch (row.Op)
            {
                case "Split":
                {
                    var input = ResolveString(row.Arg1, vars);
                    var sep = ResolveString(row.Arg2, vars) ?? string.Empty;
                    var indexStr = ResolveString(row.Arg3, vars);
                    if (input == null) { preview = "(null)"; break; }
                    var parts = input.Split(new[] { sep }, StringSplitOptions.None);
                    if (!string.IsNullOrWhiteSpace(indexStr) && int.TryParse(indexStr.Trim(), out var idx))
                    {
                        if (idx >= 0 && idx < parts.Length)
                        { valueToStore = parts[idx]; preview = valueToStore ?? string.Empty; }
                        else { preview = $"(index out of range {parts.Length})"; }
                    }
                    else { valueToStore = string.Join("\u001F", parts); preview = $"{parts.Length} parts"; }
                    break;
                }
                case "GetText":
                {
                    var el = ResolveElement(row.Arg1);
                    if (el == null) { preview = "(no element)"; break; }
                    try
                    {
                        var name = el.Name;
                        var val = el.Patterns.Value.PatternOrDefault?.Value ?? string.Empty;
                        var legacy = el.Patterns.LegacyIAccessible.PatternOrDefault?.Name ?? string.Empty;
                        valueToStore = !string.IsNullOrEmpty(val) ? val : (!string.IsNullOrEmpty(name) ? name : legacy);
                        preview = valueToStore ?? "(null)";
                    }
                    catch { preview = "(error)"; }
                    break;
                }
                case "GetName":
                {
                    var el = ResolveElement(row.Arg1);
                    if (el == null) { preview = "(no element)"; break; }
                    try { valueToStore = el.Name; preview = string.IsNullOrEmpty(valueToStore) ? "(empty)" : valueToStore; }
                    catch { preview = "(error)"; }
                    break;
                }
                case "GetTextOCR":
                {
                    var el = ResolveElement(row.Arg1);
                    if (el == null) { preview = "(no element)"; break; }
                    try
                    {
                        var r = el.BoundingRectangle; if (r.Width <= 0 || r.Height <= 0) { preview = "(no bounds)"; break; }
                        var hwnd = new IntPtr(el.Properties.NativeWindowHandle.Value); if (hwnd == IntPtr.Zero) { preview = "(no hwnd)"; break; }
                        var (engine, text) = Wysg.Musm.MFCUIA.OcrReader.OcrTryReadRegionDetailedAsync(hwnd, new System.Drawing.Rectangle(0, 0, (int)r.Width, (int)r.Height)).ConfigureAwait(false).GetAwaiter().GetResult();
                        if (!engine) preview = "(ocr unavailable)"; else { valueToStore = text; preview = string.IsNullOrWhiteSpace(text) ? "(empty)" : text!; }
                    }
                    catch { preview = "(error)"; }
                    break;
                }
                case "Invoke":
                {
                    var el = ResolveElement(row.Arg1); if (el == null) { preview = "(no element)"; break; }
                    try { var inv = el.Patterns.Invoke.PatternOrDefault; if (inv != null) inv.Invoke(); else el.Patterns.Toggle.PatternOrDefault?.Toggle(); preview = "(invoked)"; }
                    catch { preview = "(error)"; }
                    break;
                }
                case "TakeLast":
                {
                    var combined = ResolveString(row.Arg1, vars) ?? string.Empty;
                    var arr = combined.Split('\u001F');
                    valueToStore = arr.Length > 0 ? arr[^1] : string.Empty;
                    preview = valueToStore ?? "(null)"; break;
                }
                case "Trim":
                {
                    var s = ResolveString(row.Arg1, vars);
                    valueToStore = s?.Trim();
                    preview = valueToStore ?? "(null)"; break;
                }
                case "GetValueFromSelection":
                {
                    var el = ResolveElement(row.Arg1); var headerWanted = row.Arg2?.Value ?? "ID"; if (string.IsNullOrWhiteSpace(headerWanted)) headerWanted = "ID";
                    if (el == null) { preview = "(no element)"; break; }
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
                        if (selected.Length == 0) { preview = "(no selection)"; break; }
                        var rowEl = selected[0];
                        var headers = GetHeaderTexts(el);
                        var cells = GetRowCellValues(rowEl);
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
                        if (matched == null) { preview = $"({headerWanted} not found)"; }
                        else { valueToStore = matched; preview = matched; }
                    }
                    catch { preview = "(error)"; }
                    break;
                }
                case "ToDateTime":
                {
                    var s = ResolveString(row.Arg1, vars);
                    if (string.IsNullOrWhiteSpace(s)) { preview = "(null)"; break; }
                    if (TryParseYmdOrYmdHms(s.Trim(), out var dt)) { valueToStore = dt.ToString("o"); preview = dt.ToString("yyyy-MM-dd HH:mm:ss"); }
                    else { preview = "(parse fail)"; }
                    break;
                }
                default: preview = "(unsupported)"; break;
            }
            return (preview, valueToStore);
        }
        private async Task<(string preview, string? value)> ExecuteSingleAsync(ProcOpRow row, Dictionary<string, string?> vars)
        {
            if (!string.Equals(row.Op, "GetTextOCR", StringComparison.OrdinalIgnoreCase))
                return ExecuteSingle(row, vars);
            var el = ResolveElement(row.Arg1);
            if (el == null) return ("(no element)", null);
            try
            {
                var r = el.BoundingRectangle;
                if (r.Width <= 0 || r.Height <= 0) return ("(no bounds)", null);
                var hwnd = new IntPtr(el.Properties.NativeWindowHandle.Value);
                if (hwnd == IntPtr.Zero) return ("(no hwnd)", null);
                var (engine, text) = await Wysg.Musm.MFCUIA.OcrReader.OcrTryReadRegionDetailedAsync(hwnd, new System.Drawing.Rectangle(0, 0, (int)r.Width, (int)r.Height));
                if (!engine) return ("(ocr unavailable)", null);
                return (string.IsNullOrWhiteSpace(text) ? "(empty)" : text!, text);
            }
            catch { return ("(error)", null); }
        }

        private void UpdateProcedureVarsFrom(List<ProcOpRow> rows)
        {
            ProcedureVars.Clear();
            foreach (var r in rows)
                if (!string.IsNullOrWhiteSpace(r.OutputVar) && !ProcedureVars.Contains(r.OutputVar)) ProcedureVars.Add(r.OutputVar);
        }

        // ------------------------------------------------------------------
        // Procedure persistence
        // ------------------------------------------------------------------
        private static string GetProcPath()
        {
            var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wysg.Musm", "Radium");
            System.IO.Directory.CreateDirectory(dir);
            return System.IO.Path.Combine(dir, "ui-procedures.json");
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
        private static void SaveProcedureForMethod(string methodTag, List<ProcOpRow> steps)
        { var s = LoadProcStore(); s.Methods[methodTag] = steps; SaveProcStore(s); }
        private static List<ProcOpRow> LoadProcedureForMethod(string methodTag)
        { var s = LoadProcStore(); return s.Methods.TryGetValue(methodTag, out var steps) ? steps : new List<ProcOpRow>(); }

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
        private async Task<(string? result, List<ProcOpRow> annotated)> RunProcedureAsync(List<ProcOpRow> steps)
        {
            var vars = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            string? last = null; var annotated = new List<ProcOpRow>();
            for (int i = 0; i < steps.Count; i++)
            {
                var row = steps[i]; string varName = $"var{i + 1}"; row.OutputVar = varName;
                (string preview, string? value) res = string.Equals(row.Op, "GetTextOCR", StringComparison.OrdinalIgnoreCase)
                    ? await ExecuteSingleAsync(row, vars)
                    : ExecuteSingle(row, vars);
                vars[varName] = res.value; if (res.value != null) last = res.value; row.OutputPreview = res.preview; annotated.Add(row);
            }
            return (last, annotated);
        }

        // ------------------------------------------------------------------
        // Argument resolution helpers
        // ------------------------------------------------------------------
        private AutomationElement? ResolveElement(ProcArg arg)
        {
            var type = ParseArgKind(arg.Type); if (type != ArgKind.Element) return null;
            var tag = arg.Value ?? string.Empty; if (!Enum.TryParse<UiBookmarks.KnownControl>(tag, out var key)) return null;
            var tuple = UiBookmarks.Resolve(key); return tuple.element;
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

        // ------------------------------------------------------------------
        // UI helpers
        // ------------------------------------------------------------------
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
    }
}
