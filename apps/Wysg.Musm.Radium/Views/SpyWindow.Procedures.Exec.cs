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
                        case "ClickElement":
                            row.Arg1.Type = nameof(ArgKind.Element); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "Split":
                            row.Arg1.Type = nameof(ArgKind.Var); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = true; if (string.IsNullOrWhiteSpace(row.Arg2.Value)) row.Arg2.Value = ",";
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = true; if (string.IsNullOrWhiteSpace(row.Arg3.Value)) row.Arg3.Value = "0";
                            break;
                        case "Replace":
                            row.Arg1.Type = nameof(ArgKind.Var); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = true;
                            row.Arg3.Type = nameof(ArgKind.String); row.Arg3Enabled = true;
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
            string? valueToStore = null; string preview;
            switch (row.Op)
            {
                case "Split":
                    var input = ResolveString(row.Arg1, vars);
                    var sepRaw = ResolveString(row.Arg2, vars) ?? string.Empty;
                    var indexStr = ResolveString(row.Arg3, vars);
                    if (input == null) { preview = "(null)"; break; }
                    string[] parts;
                    if (sepRaw.StartsWith("re:", StringComparison.OrdinalIgnoreCase) || sepRaw.StartsWith("regex:", StringComparison.OrdinalIgnoreCase))
                    {
                        var pattern = sepRaw.StartsWith("re:", StringComparison.OrdinalIgnoreCase) ? sepRaw.Substring(3) : sepRaw.Substring(6);
                        if (string.IsNullOrEmpty(pattern)) { preview = "(empty pattern)"; break; }
                        try { parts = Regex.Split(input, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase); }
                        catch (Exception ex) { preview = $"(regex error: {ex.Message})"; break; }
                    }
                    else
                    {
                        var sep = UnescapeUserText(sepRaw);
                        parts = input.Split(new[] { sep }, StringSplitOptions.None);
                        if (parts.Length == 1 && sep.Contains('\n') && !sep.Contains("\r\n"))
                        {
                            var crlfSep = sep.Replace("\n", "\r\n");
                            parts = input.Split(new[] { crlfSep }, StringSplitOptions.None);
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(indexStr) && int.TryParse(indexStr.Trim(), out var idx))
                    {
                        if (idx >= 0 && idx < parts.Length)
                        { valueToStore = parts[idx]; preview = valueToStore ?? string.Empty; }
                        else { preview = $"(index out of range {parts.Length})"; }
                    }
                    else { valueToStore = string.Join("\u001F", parts); preview = $"{parts.Length} parts"; }
                    break;
                case "Replace":
                    var input2 = ResolveString(row.Arg1, vars);
                    var searchRaw = ResolveString(row.Arg2, vars) ?? string.Empty;
                    var replRaw = ResolveString(row.Arg3, vars) ?? string.Empty;
                    if (input2 == null) { preview = "(null)"; break; }
                    var search = UnescapeUserText(searchRaw);
                    var repl = UnescapeUserText(replRaw);
                    if (string.IsNullOrEmpty(search)) { valueToStore = input2; preview = input2; break; }
                    valueToStore = input2.Replace(search, repl);
                    preview = valueToStore;
                    break;
                case "GetText":
                    var el = ResolveElement(row.Arg1);
                    if (el == null) { preview = "(no element)"; break; }
                    try
                    {
                        var name = el.Name;
                        var val = el.Patterns.Value.PatternOrDefault?.Value ?? string.Empty;
                        var legacy = el.Patterns.LegacyIAccessible.PatternOrDefault?.Name ?? string.Empty;
                        var raw = !string.IsNullOrEmpty(val) ? val : (!string.IsNullOrEmpty(name) ? name : legacy);
                        valueToStore = NormalizeKoreanMojibake(raw);
                        preview = valueToStore ?? "(null)";
                    }
                    catch { preview = "(error)"; }
                    break;
                case "GetName":
                    var el2 = ResolveElement(row.Arg1);
                    if (el2 == null) { preview = "(no element)"; break; }
                    try { var raw = el2.Name; valueToStore = NormalizeKoreanMojibake(raw); preview = string.IsNullOrEmpty(valueToStore) ? "(empty)" : valueToStore; }
                    catch { preview = "(error)"; }
                    break;
                case "GetTextOCR":
                    var el3 = ResolveElement(row.Arg1);
                    if (el3 == null) { preview = "(no element)"; break; }
                    try
                    {
                        var r = el3.BoundingRectangle; if (r.Width <= 0 || r.Height <= 0) { preview = "(no bounds)"; break; }
                        var hwnd = new IntPtr(el3.Properties.NativeWindowHandle.Value); if (hwnd == IntPtr.Zero) { preview = "(no hwnd)"; break; }
                        var (engine, text) = Wysg.Musm.MFCUIA.OcrReader.OcrTryReadRegionDetailedAsync(hwnd, new System.Drawing.Rectangle(0, 0, (int)r.Width, (int)r.Height)).ConfigureAwait(false).GetAwaiter().GetResult();
                        if (!engine) preview = "(ocr unavailable)"; else { valueToStore = text; preview = string.IsNullOrWhiteSpace(text) ? "(empty)" : text!; }
                    }
                    catch { preview = "(error)"; }
                    break;
                case "Invoke":
                    var el4 = ResolveElement(row.Arg1); if (el4 == null) { preview = "(no element)"; break; }
                    try { var inv = el4.Patterns.Invoke.PatternOrDefault; if (inv != null) inv.Invoke(); else el4.Patterns.Toggle.PatternOrDefault?.Toggle(); preview = "(invoked)"; }
                    catch { preview = "(error)"; }
                    break;
                case "ClickElement":
                {
                    var elClick = ResolveElement(row.Arg1);
                    if (elClick == null) { preview = "(no element)"; break; }
                    try
                    {
                        var r = elClick.BoundingRectangle;
                        if (r.Width <= 0 || r.Height <= 0) { preview = "(no bounds)"; break; }
                        int cx = (int)(r.Left + r.Width / 2);
                        int cy = (int)(r.Top + r.Height / 2);
                        NativeMouseHelper.ClickScreenWithRestore(cx, cy);
                        preview = $"(clicked element center {cx},{cy})";
                    }
                    catch { preview = "(error)"; }
                    break;
                }
                case "TakeLast":
                    var combined = ResolveString(row.Arg1, vars) ?? string.Empty;
                    var arr = combined.Split('\u001F');
                    valueToStore = arr.Length > 0 ? arr[^1] : string.Empty;
                    preview = valueToStore ?? "(null)"; break;
                case "Trim":
                    var s = ResolveString(row.Arg1, vars);
                    valueToStore = s?.Trim();
                    preview = valueToStore ?? "(null)"; break;
                case "GetValueFromSelection":
                    var el5 = ResolveElement(row.Arg1); var headerWanted = row.Arg2?.Value ?? "ID"; if (string.IsNullOrWhiteSpace(headerWanted)) headerWanted = "ID";
                    if (el5 == null) { preview = "(no element)"; break; }
                    try
                    {
                        var selection = el5.Patterns.Selection.PatternOrDefault;
                        var selected = selection?.Selection?.Value ?? Array.Empty<AutomationElement>();
                        if (selected.Length == 0)
                        {
                            selected = el5.FindAllDescendants().Where(a =>
                            {
                                try { return a.Patterns.SelectionItem.IsSupported && a.Patterns.SelectionItem.PatternOrDefault?.IsSelected == true; }
                                catch { return false; }
                            }).ToArray();
                        }
                        if (selected.Length == 0) { preview = "(no selection)"; break; }
                        var rowEl = selected[0];
                        var headers = GetHeaderTexts(el5);
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
                        if (matched == null) { preview = $"({headerWanted} not found)"; }
                        else { valueToStore = matched; preview = matched; }
                    }
                    catch { preview = "(error)"; }
                    break;
                case "ToDateTime":
                    var s2 = ResolveString(row.Arg1, vars);
                    if (string.IsNullOrWhiteSpace(s2)) { preview = "(null)"; break; }
                    if (TryParseYmdOrYmdHms(s2.Trim(), out var dt)) { valueToStore = dt.ToString("o"); preview = dt.ToString("yyyy-MM-dd HH:mm:ss"); }
                    else { preview = "(parse fail)"; }
                    break;
                case "MouseClick":
                    var xStr = ResolveString(row.Arg1, vars);
                    var yStr = ResolveString(row.Arg2, vars);
                    if (!int.TryParse(xStr, out var px) || !int.TryParse(yStr, out var py)) { preview = "(invalid coords)"; break; }
                    try
                    {
                        NativeMouseHelper.ClickScreen(px, py);
                        preview = $"(clicked {px},{py})";
                    }
                    catch { preview = "(error)"; }
                    break;
                default: preview = "(unsupported)"; break;
            }
            return (preview, valueToStore);
        }

        private async Task<(string preview, string? value)> ExecuteSingleAsync(ProcOpRow row, Dictionary<string, string?> vars)
        {
            if (string.Equals(row.Op, "GetTextOCR", StringComparison.OrdinalIgnoreCase))
            {
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
            if (string.Equals(row.Op, "GetHTML", StringComparison.OrdinalIgnoreCase))
            {
                var url = ResolveString(row.Arg1, vars);
                if (string.IsNullOrWhiteSpace(url)) return ("(null)", null);
                try
                {
                    var html = await HttpGetHtmlSmartAsync(url);
                    return (html ?? string.Empty, html);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SpyWindow] GetHTML error: {ex.Message}");
                    return ("(error)", null);
                }
            }

            return ExecuteSingle(row, vars);
        }

        private void UpdateProcedureVarsFrom(System.Collections.Generic.List<ProcOpRow> rows)
        {
            ProcedureVars.Clear();
            foreach (var r in rows)
                if (!string.IsNullOrWhiteSpace(r.OutputVar) && !ProcedureVars.Contains(r.OutputVar)) ProcedureVars.Add(r.OutputVar);
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

        private FlaUI.Core.AutomationElements.AutomationElement? ResolveElement(ProcArg arg)
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
