using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Media;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Patterns;
using FlaUI.UIA3;
using Wysg.Musm.Radium.Services;
using SWA = System.Windows.Automation;
using Wysg.Musm.MFCUIA.Session;

namespace Wysg.Musm.Radium.Views
{
    public partial class SpyWindow : System.Windows.Window
    {
        // Preferred order for output
        private static readonly string[] PreferredHeaderOrder = new[]
        {
            "Status","ID","Name","Sex","Birth Date","Body Part","Age","Accession No.",
            "Study Desc","Modality","Image","Study Date","Requesting Doctor","Location",
            "Report creator","Study Comments","Report approval dttm","Institution"
        };

        // UIA ControlType Ids
        private const int UIA_ListItem = 50007;
        private const int UIA_Header = 50034;
        private const int UIA_HeaderItem = 50035;
        private const int UIA_Text = 50020;
        private const int UIA_DataItem = 50029;

        // Spy tree configuration (user-requested configurable constants)
        private const int FocusChainDepth = 4;          // show single node per depth up to this level (1-based)
        private const int FocusSubtreeMaxDepth = 8;     // expand subtree starting from FocusChainDepth+1 down to this depth

        // P/Invoke and overlay fields
        [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT lpPoint);
        [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(POINT Point);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X; public int Y; }
        private readonly HighlightOverlay _overlay = new HighlightOverlay();
        private UiBookmarks.Bookmark? _editing;
        private AutomationElement? _lastResolved;

        private System.Windows.Controls.ComboBox CmbMethod => (System.Windows.Controls.ComboBox)FindName("cmbMethod");
        private System.Windows.Controls.DataGrid GridChain => (System.Windows.Controls.DataGrid)FindName("gridChain");
        private System.Windows.Controls.CheckBox? _chkEnableTree => (System.Windows.Controls.CheckBox?)FindName("chkEnableTree");

        // Dark title bar (DWM)
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            TryEnableDarkTitleBar();
        }
        private void TryEnableDarkTitleBar()
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd == IntPtr.Zero) return;
                int useImmersiveDarkMode = 1;
                const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
                const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
                if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useImmersiveDarkMode, sizeof(int));
                }
            }
            catch { }
        }
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        // EnsureResolved helper (used by Row Data etc.)
        private void EnsureResolved()
        {
            if (_lastResolved != null) return;
            if (!BuildBookmarkFromUi(out var copy)) return;
            var (_, el, _) = UiBookmarks.TryResolveWithTrace(copy);
            _lastResolved = el;
        }

        // ======== Custom Procedures model/persistence/executor ========
        private enum ArgKind { Element, String, Number, Var }

        private sealed class ProcArg : INotifyPropertyChanged
        {
            private string _type = nameof(ArgKind.String);
            private string? _value;
            public string Type { get => _type; set => SetField(ref _type, value); }
            public string? Value { get => _value; set => SetField(ref _value, value); }

            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
            {
                if (EqualityComparer<T>.Default.Equals(field, value)) return false;
                field = value;
                OnPropertyChanged(name);
                return true;
            }
        }

        private sealed class ProcOpRow : INotifyPropertyChanged
        {
            private string _op = string.Empty;
            private ProcArg _arg1 = new ProcArg();
            private ProcArg _arg2 = new ProcArg();
            private ProcArg _arg3 = new ProcArg();
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
            private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
            {
                if (EqualityComparer<T>.Default.Equals(field, value)) return false;
                field = value;
                OnPropertyChanged(name);
                return true;
            }
        }
        private sealed class ProcStore { public Dictionary<string, List<ProcOpRow>> Methods { get; set; } = new(); }

        // Expose known controls and currently available vars to XAML
        public List<string> KnownControlTags { get; } = Enum.GetNames(typeof(UiBookmarks.KnownControl)).ToList();
        public ObservableCollection<string> ProcedureVars { get; } = new();

        public SpyWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadBookmarks();
            this.PreviewMouseDown += OnPreviewMouseDownForQuickMap;

            // Custom procedures grid
            var procGrid = (System.Windows.Controls.DataGrid?)FindName("gridProcSteps");
            if (procGrid != null) procGrid.ItemsSource = new List<ProcOpRow>();

            var cmbMethodProc = (System.Windows.Controls.ComboBox?)FindName("cmbProcMethod");
            if (cmbMethodProc != null) cmbMethodProc.SelectionChanged += OnProcMethodChanged;
        }

        // Add a row
        private void OnAddProcRow(object sender, RoutedEventArgs e)
        {
            var procGrid = (System.Windows.Controls.DataGrid?)FindName("gridProcSteps");
            if (procGrid == null) return;
            var list = procGrid.Items.OfType<ProcOpRow>().ToList();
            list.Add(new ProcOpRow());
            procGrid.ItemsSource = null; procGrid.ItemsSource = list;
            UpdateProcedureVarsFrom(list);
        }

        // Remove row
        private void OnRemoveProcRow(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button b && b.Tag is ProcOpRow row)
            {
                var procGrid = (System.Windows.Controls.DataGrid?)FindName("gridProcSteps");
                if (procGrid == null) return;
                var list = procGrid.Items.OfType<ProcOpRow>().ToList();
                list.Remove(row);
                procGrid.ItemsSource = null; procGrid.ItemsSource = list;
                UpdateProcedureVarsFrom(list);
            }
        }

        // Set row: execute only this row with current state and produce var# (async safe for OCR)
        private async void OnSetProcRow(object sender, RoutedEventArgs e)
        {
            var procGrid = (System.Windows.Controls.DataGrid?)FindName("gridProcSteps");
            if (procGrid == null) return;
            try { procGrid.CommitEdit(DataGridEditingUnit.Cell, true); procGrid.CommitEdit(DataGridEditingUnit.Row, true); } catch { }

            var list = procGrid.Items.OfType<ProcOpRow>().ToList();
            if (sender is System.Windows.Controls.Button b && b.Tag is ProcOpRow row)
            {
                var index = list.IndexOf(row);
                if (index < 0) return;

                // Build vars from previous rows if already set
                var vars = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < index; i++) vars[$"var{i + 1}"] = list[i].OutputVar != null ? list[i].OutputPreview : null;

                var varName = $"var{index + 1}";
                (string preview, string? value) result;
                if (string.Equals(row.Op, "GetTextOCR", StringComparison.OrdinalIgnoreCase))
                    result = await ExecuteSingleAsync(row, vars);
                else
                    result = ExecuteSingle(row, vars);
                row.OutputVar = varName;
                row.OutputPreview = result.preview;

                if (!ProcedureVars.Contains(varName)) ProcedureVars.Add(varName);
                procGrid.ItemsSource = null; procGrid.ItemsSource = list; // refresh
            }
        }

        private bool _handlingProcOpChange;
        // Operation selection -> preset arg types and enable/disable
        private void OnProcOpChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_handlingProcOpChange) return;
            if (sender is System.Windows.Controls.ComboBox cb && cb.DataContext is ProcOpRow row)
            {
                try
                {
                    _handlingProcOpChange = true;
                    Debug.WriteLine($"[PP2] Op changed to: {row.Op}, FocusWithin={cb.IsKeyboardFocusWithin}, IsDropDownOpen={cb.IsDropDownOpen}");
                    switch (row.Op)
                    {
                        case "GetText":
                        case "GetTextOCR":
                        case "GetName":
                            row.Arg1.Type = nameof(ArgKind.Element); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
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
                            row.Arg1.Type = nameof(ArgKind.Var); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "Trim":
                            row.Arg1.Type = nameof(ArgKind.Var); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "GetValueFromSelection":
                            row.Arg1.Type = nameof(ArgKind.Element); row.Arg1Enabled = true;
                            row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = true; if (string.IsNullOrWhiteSpace(row.Arg2.Value)) row.Arg2.Value = "ID";
                            row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
                            break;
                        case "ToDateTime":
                            row.Arg1.Type = nameof(ArgKind.Var); row.Arg1Enabled = true;
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

        private (string preview, string? value) ExecuteSingle(ProcOpRow row, Dictionary<string, string?> vars)
        {
            string? valueToStore = null;
            string preview;
            switch (row.Op)
            {
                case "Split":
                {
                    var input = ResolveString(row.Arg1, vars);
                    var sep = ResolveString(row.Arg2, vars) ?? string.Empty;
                    var indexStr = ResolveString(row.Arg3, vars);
                    if (input == null) { preview = "(null)"; valueToStore = null; break; }
                    var parts = input.Split(new[] { sep }, StringSplitOptions.None);
                    if (!string.IsNullOrWhiteSpace(indexStr) && int.TryParse(indexStr.Trim(), out var idx))
                    {
                        if (idx >= 0 && idx < parts.Length)
                        {
                            valueToStore = parts[idx];
                            // Preview should be just the extracted part (no metadata per user request)
                            preview = valueToStore ?? string.Empty;
                        }
                        else
                        {
                            valueToStore = null; preview = $"(index out of range {parts.Length})";
                        }
                    }
                    else
                    {
                        // No index -> legacy behaviour (store all parts joined with unit separator, preview part count)
                        valueToStore = string.Join("\u001F", parts);
                        preview = $"{parts.Length} parts";
                    }
                    break;
                }
                case "GetText":
                {
                    var el = ResolveElement(row.Arg1);
                    if (el == null) { valueToStore = null; preview = "(no element)"; break; }
                    try
                    {
                        var name = el.Name;
                        var val = el.Patterns.Value.PatternOrDefault?.Value ?? string.Empty;
                        var legacy = el.Patterns.LegacyIAccessible.PatternOrDefault?.Name ?? string.Empty;
                        valueToStore = !string.IsNullOrEmpty(val) ? val : (!string.IsNullOrEmpty(name) ? name : legacy);
                        preview = valueToStore ?? "(null)";
                    }
                    catch { valueToStore = null; preview = "(error)"; }
                    break;
                }
                case "GetName":
                {
                    var el = ResolveElement(row.Arg1);
                    if (el == null) { valueToStore = null; preview = "(no element)"; break; }
                    try
                    {
                        valueToStore = el.Name;
                        preview = string.IsNullOrEmpty(valueToStore) ? "(empty)" : valueToStore;
                    }
                    catch { valueToStore = null; preview = "(error)"; }
                    break;
                }
                case "GetTextOCR":
                {
                    var el = ResolveElement(row.Arg1);
                    if (el == null) { valueToStore = null; preview = "(no element)"; break; }
                    try
                    {
                        var r = el.BoundingRectangle;
                        if (r.Width <= 0 || r.Height <= 0) { preview = "(no bounds)"; valueToStore = null; break; }
                        var hwnd = new IntPtr(el.Properties.NativeWindowHandle.Value);
                        if (hwnd == IntPtr.Zero) { preview = "(no hwnd)"; valueToStore = null; break; }
                        var (engine, text) = Wysg.Musm.MFCUIA.OcrReader.OcrTryReadRegionDetailedAsync(hwnd, new System.Drawing.Rectangle(0, 0, (int)r.Width, (int)r.Height)).ConfigureAwait(false).GetAwaiter().GetResult();
                        if (!engine) { preview = "(ocr unavailable)"; valueToStore = null; }
                        else { valueToStore = text; preview = string.IsNullOrWhiteSpace(text) ? "(empty)" : text!; }
                    }
                    catch { valueToStore = null; preview = "(error)"; }
                    break;
                }
                case "Invoke":
                {
                    var el = ResolveElement(row.Arg1);
                    if (el == null) { valueToStore = null; preview = "(no element)"; break; }
                    try
                    {
                        var inv = el.Patterns.Invoke.PatternOrDefault;
                        if (inv != null) inv.Invoke(); else el.Patterns.Toggle.PatternOrDefault?.Toggle();
                        preview = "(invoked)"; valueToStore = null;
                    }
                    catch { preview = "(error)"; valueToStore = null; }
                    break;
                }
                case "TakeLast":
                {
                    var combined = ResolveString(row.Arg1, vars) ?? string.Empty;
                    var arr = combined.Split('\u001F');
                    valueToStore = arr.Length > 0 ? arr[^1] : string.Empty;
                    preview = valueToStore ?? "(null)";
                    break;
                }
                case "Trim":
                {
                    var s = ResolveString(row.Arg1, vars);
                    valueToStore = s?.Trim();
                    preview = valueToStore ?? "(null)";
                    break;
                }
                case "GetValueFromSelection":
                {
                    var el = ResolveElement(row.Arg1);
                    var headerWanted = row.Arg2?.Value ?? "ID";
                    if (string.IsNullOrWhiteSpace(headerWanted)) headerWanted = "ID";
                    if (el == null) { preview = "(no element)"; valueToStore = null; break; }
                    try
                    {
                        var selection = el.Patterns.Selection.PatternOrDefault;
                        var selected = selection?.Selection?.Value ?? Array.Empty<AutomationElement>();
                        if (selected.Length == 0)
                        {
                            selected = el.FindAllDescendants()
                                .Where(a =>
                                {
                                    try { return a.Patterns.SelectionItem.IsSupported && a.Patterns.SelectionItem.PatternOrDefault?.IsSelected == true; }
                                    catch { return false; }
                                })
                                .ToArray();
                        }
                        if (selected.Length == 0) { preview = "(no selection)"; valueToStore = null; break; }
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
                        if (matched == null) { preview = $"({headerWanted} not found)"; valueToStore = null; }
                        else { valueToStore = matched; preview = matched; }
                    }
                    catch { preview = "(error)"; valueToStore = null; }
                    break;
                }
                case "ToDateTime":
                {
                    var s = ResolveString(row.Arg1, vars);
                    if (string.IsNullOrWhiteSpace(s)) { preview = "(null)"; valueToStore = null; break; }
                    if (TryParseYmdOrYmdHms(s.Trim(), out var dt)) { valueToStore = dt.ToString("o"); preview = dt.ToString("yyyy-MM-dd HH:mm:ss"); }
                    else { preview = "(parse fail)"; valueToStore = null; }
                    break;
                }
                default:
                    preview = "(unsupported)"; valueToStore = null; break;
            }
            return (preview, valueToStore);
        }

        // Async execution for OCR to avoid UI deadlock (FR-135)
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

        // Update ProcedureVars after full run or edits
        private void UpdateProcedureVarsFrom(List<ProcOpRow> rows)
        {
            // Only include variables that are actually produced (rows with OutputVar set)
            ProcedureVars.Clear();
            foreach (var r in rows)
            {
                if (!string.IsNullOrWhiteSpace(r.OutputVar))
                {
                    if (!ProcedureVars.Contains(r.OutputVar)) ProcedureVars.Add(r.OutputVar);
                }
            }
        }

        // ========= Ancestry data model =========
        private class TreeNode
        {
            public string? Name { get; set; }
            public string? ClassName { get; set; }
            public int? ControlTypeId { get; set; }
            public string? AutomationId { get; set; }
            public List<TreeNode> Children { get; } = new();
            public int Level { get; set; }
            public Brush? Highlight { get; set; }
        }
        private TreeNode? _ancestryRoot;

        // Rebuild and show a FlaUInspect-like subtree from the resolved root
        private void ShowAncestryTree(UiBookmarks.Bookmark b)
        {
            if (_chkEnableTree?.IsChecked != true) { tvAncestry.ItemsSource = Array.Empty<object>(); return; }
            // (Optional future: re-enable rebuild logic here)
            tvAncestry.ItemsSource = Array.Empty<object>();
        }

        private static void PopulateChildrenTree(TreeNode node, AutomationElement element, int maxDepth)
        {
            if (maxDepth <= 0) return;
            try
            {
                node.Children.Clear();
                var kids = element.FindAllChildren();
                var limit = Math.Min(kids.Length, 100);
                Debug.WriteLine($"[PP1] Populating children for {Safe(element, e=>e.ClassName)}::{Safe(element, e=>e.Name)} count={kids.Length} cap={limit} depth={maxDepth}");
                for (int i = 0; i < limit; i++)
                {
                    var k = kids[i];
                    var childNode = new TreeNode
                    {
                        Name = Safe(k, e => e.Name),
                        ClassName = Safe(k, e => e.ClassName),
                        ControlTypeId = Safe(k, e => (int?)e.Properties.ControlType.Value),
                        AutomationId = Safe(k, e => e.AutomationId),
                        Level = node.Level + 1
                    };
                    // deep levels: color only the element of interest (handled by caller who knows the path). Default no color here.
                    node.Children.Add(childNode);
                    PopulateChildrenTree(childNode, k, maxDepth - 1);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[PP1] PopulateChildrenTree error: " + ex.Message);
            }

            static T? Safe<T>(AutomationElement e, Func<AutomationElement, T?> f)
            { try { return f(e); } catch { return default; } }
        }

        // Helper to create a TreeNode from an AutomationElement and populate its children up to a depth
        private static TreeNode BuildTreeFromElement(AutomationElement element, int maxDepth)
        {
            var node = new TreeNode
            {
                Name = Safe(element, e => e.Name),
                ClassName = Safe(element, e => e.ClassName),
                ControlTypeId = Safe(element, e => (int?)e.Properties.ControlType.Value),
                AutomationId = Safe(element, e => e.AutomationId),
                Level = 1
            };
            PopulateChildrenTree(node, element, maxDepth);
            return node;

            static T? Safe<T>(AutomationElement e, Func<AutomationElement, T?> f)
            { try { return f(e); } catch { return default; } }
        }

        // ======== Persistence for procedures =========
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
        {
            var s = LoadProcStore();
            s.Methods[methodTag] = steps;
            SaveProcStore(s);
        }
        private static List<ProcOpRow> LoadProcedureForMethod(string methodTag)
        {
            var s = LoadProcStore();
            return s.Methods.TryGetValue(methodTag, out var steps) ? steps : new List<ProcOpRow>();
        }

        private void OnProcMethodChanged(object? sender, SelectionChangedEventArgs e)
        {
            var cmb = (System.Windows.Controls.ComboBox?)FindName("cmbProcMethod");
            var tag = ((cmb?.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag as string) ?? ((cmb?.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content as string);
            var procGrid = (System.Windows.Controls.DataGrid?)FindName("gridProcSteps");
            if (string.IsNullOrWhiteSpace(tag) || procGrid == null) return;
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
            procGrid.ItemsSource = null; procGrid.ItemsSource = annotated;
            UpdateProcedureVarsFrom(annotated);
            txtStatus.Text = result ?? "(null)";
        }

        private async Task<(string? result, List<ProcOpRow> annotated)> RunProcedureAsync(List<ProcOpRow> steps)
        {
            var vars = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            string? last = null;
            var annotated = new List<ProcOpRow>();

            for (int i = 0; i < steps.Count; i++)
            {
                var row = steps[i];
                string varName = $"var{i + 1}";
                row.OutputVar = varName;
                string? valueToStore = null;
                string preview;

                if (string.Equals(row.Op, "GetTextOCR", StringComparison.OrdinalIgnoreCase))
                {
                    var (p, v) = await ExecuteSingleAsync(row, vars);
                    preview = p; valueToStore = v;
                }
                else
                {
                    var (p, v) = ExecuteSingle(row, vars);
                    preview = p; valueToStore = v;
                }

                vars[varName] = valueToStore;
                if (valueToStore != null) last = valueToStore;
                row.OutputPreview = preview;
                annotated.Add(row);
            }

            return (last, annotated);
        }

        // Legacy synchronous runner retained (not used by buttons now)
        private (string? result, List<ProcOpRow> annotated) RunProcedure(List<ProcOpRow> steps)
        {
            var vars = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            string? last = null;
            var annotated = new List<ProcOpRow>();

            for (int i = 0; i < steps.Count; i++)
            {
                var row = steps[i];
                string varName = $"var{i + 1}";
                row.OutputVar = varName;
                var (preview, valueToStore) = ExecuteSingle(row, vars);
                vars[varName] = valueToStore;
                last = valueToStore ?? last;
                row.OutputPreview = preview;
                annotated.Add(row);
            }
            return (last, annotated);
        }

        private AutomationElement? ResolveElement(ProcArg arg)
        {
            var type = ParseArgKind(arg.Type);
            if (type != ArgKind.Element) return null;
            var tag = arg.Value ?? string.Empty;
            if (!Enum.TryParse<UiBookmarks.KnownControl>(tag, out var key)) return null;
            var tuple = UiBookmarks.Resolve(key);
            var el = tuple.element;
            return el;
        }
        private static string? ResolveString(ProcArg arg, Dictionary<string, string?> vars)
        {
            var type = ParseArgKind(arg.Type);
            return type switch
            {
                ArgKind.Var => (arg.Value != null && vars.TryGetValue(arg.Value, out var v)) ? v : null,
                ArgKind.String => arg.Value,
                ArgKind.Number => arg.Value,
                ArgKind.Element => null,
                _ => null
            };
        }
        private static ArgKind ParseArgKind(string? s)
        {
            if (Enum.TryParse<ArgKind>(s, true, out var k)) return k;
            return s?.Equals("Var", StringComparison.OrdinalIgnoreCase) == true ? ArgKind.Var :
                   s?.Equals("Element", StringComparison.OrdinalIgnoreCase) == true ? ArgKind.Element :
                   s?.Equals("Number", StringComparison.OrdinalIgnoreCase) == true ? ArgKind.Number : ArgKind.String;
        }

        // ========= Mapping editor (existing) =========
        private static string GetElementName(AutomationElement el)
        {
            try { if (el.Properties.Name.TryGetValue(out var n)) return n ?? string.Empty; } catch { }
            try { return el.Name; } catch { }
            return string.Empty;
        }

        private static string NormalizeHeader(string h)
        {
            h = (h ?? string.Empty).Trim();
            if (string.Equals(h, "Accession", StringComparison.OrdinalIgnoreCase)) return "Accession No.";
            if (string.Equals(h, "Study Description", StringComparison.OrdinalIgnoreCase)) return "Study Desc";
            if (string.Equals(h, "Institution Name", StringComparison.OrdinalIgnoreCase)) return "Institution";
            if (string.Equals(h, "BirthDate", StringComparison.OrdinalIgnoreCase)) return "Birth Date";
            if (string.Equals(h, "BodyPart", StringComparison.OrdinalIgnoreCase)) return "Body Part";
            return h;
        }

        private static string ReadCellText(AutomationElement cell)
        {
            try
            {
                var vp = cell.Patterns.Value.PatternOrDefault;
                if (vp != null)
                {
                    if (vp.Value.TryGetValue(out var pv) && !string.IsNullOrWhiteSpace(pv))
                        return pv;
                }
            }
            catch { }
            var name = GetElementName(cell); if (!string.IsNullOrWhiteSpace(name)) return name;
            try { var l = cell.Patterns.LegacyIAccessible.PatternOrDefault?.Name; if (!string.IsNullOrWhiteSpace(l)) return l; } catch { }
            return string.Empty;
        }

        private void LoadBookmarks()
        {
            var store = UiBookmarks.Load();
            var lb = (System.Windows.Controls.ListBox)FindName("lstBookmarks");
            if (lb != null) lb.ItemsSource = store.Bookmarks.OrderBy(b => b.Name).ToList();
        }

        private void OnReload(object sender, RoutedEventArgs e) => LoadBookmarks();

        private void OnBookmarkSelected(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var lb = (System.Windows.Controls.ListBox)FindName("lstBookmarks");
            if (lb != null && lb.SelectedItem is UiBookmarks.Bookmark b)
            {
                LoadEditor(b);
                txtStatus.Text = $"Loaded bookmark: {b.Name}";
            }
        }

        private void OnKnownSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var combo = (System.Windows.Controls.ComboBox)FindName("cmbKnown");
            var item = combo?.SelectedItem as System.Windows.Controls.ComboBoxItem;
            var keyStr = item?.Tag as string;
            if (string.IsNullOrWhiteSpace(keyStr)) return;
            if (!Enum.TryParse<UiBookmarks.KnownControl>(keyStr, out var key)) return;
            var mapping = UiBookmarks.GetMapping(key);
            if (mapping != null) LoadEditor(mapping);
        }

        private void LoadEditor(UiBookmarks.Bookmark b)
        {
            _editing = new UiBookmarks.Bookmark
            {
                Name = b.Name,
                ProcessName = b.ProcessName,
                Method = b.Method,
                DirectAutomationId = b.DirectAutomationId,
                CrawlFromRoot = b.CrawlFromRoot,
                Chain = b.Chain.Select(n => new UiBookmarks.Node
                {
                    Name = n.Name,
                    ClassName = n.ClassName,
                    ControlTypeId = n.ControlTypeId,
                    AutomationId = n.AutomationId,
                    IndexAmongMatches = n.IndexAmongMatches,
                    Include = n.Include,
                    UseName = n.UseName,
                    UseClassName = n.UseClassName,
                    UseControlTypeId = n.UseControlTypeId,
                    UseAutomationId = n.UseAutomationId,
                    UseIndex = n.UseIndex,
                    Scope = n.Scope,
                    Order = n.Order
                }).ToList()
            };
            GridChain.ItemsSource = _editing.Chain;
            CmbMethod.SelectedIndex = _editing.Method == UiBookmarks.MapMethod.Chain ? 0 : 1;
            txtProcess.Text = _editing.ProcessName ?? string.Empty;
        }

        private void SaveEditorInto(UiBookmarks.Bookmark b)
        {
            if (_editing == null) return;
            b.Method = CmbMethod.SelectedIndex == 0 ? UiBookmarks.MapMethod.Chain : UiBookmarks.MapMethod.AutomationIdOnly;
            if (b.Method == UiBookmarks.MapMethod.AutomationIdOnly)
            {
                b.DirectAutomationId = _editing.Chain.LastOrDefault()?.AutomationId;
                if (string.IsNullOrWhiteSpace(b.DirectAutomationId)) b.DirectAutomationId = null;
            }
            else
            {
                b.DirectAutomationId = null;
            }
            b.Chain = GridChain.Items.OfType<UiBookmarks.Node>().ToList();
            if (!string.IsNullOrWhiteSpace(txtProcess.Text)) b.ProcessName = txtProcess.Text.Trim();
        }

        private void ShowBookmarkDetails(UiBookmarks.Bookmark b, string header)
        {
            LoadEditor(b);
            // Tree view disabled (performance)
            txtStatus.Text = header;
        }

        private async void OnPick(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtDelay.Text?.Trim(), out var delay)) delay = 600;
            txtStatus.Text = $"Pick arming... move mouse to target ({delay}ms)";
            await Task.Delay(delay);
            var (b, procName, msg) = CaptureUnderMouse(preferAutomationId: false);
            txtStatus.Text = msg;
            if (!string.IsNullOrWhiteSpace(procName)) txtProcess.Text = procName;
            if (b == null) return;
            this.Tag = b;
            b.ProcessName = string.IsNullOrWhiteSpace(procName) ? b.ProcessName : procName;
            ShowBookmarkDetails(b, "Captured chain");
            foreach (var n in b.Chain) { n.UseIndex = false; }
            GridChain.ItemsSource = null; GridChain.ItemsSource = b.Chain; // refresh grid
        }

        private (UiBookmarks.Bookmark? bookmark, string? procName, string message) CaptureUnderMouse(bool preferAutomationId)
        {
            try
            {
                GetCursorPos(out var pt);
                var p = new System.Windows.Point(pt.X, pt.Y);
                var el = SWA.AutomationElement.FromPoint(p);
                if (el == null) return (null, null, "No UIA element under mouse");

                int pid = 0; try { pid = el.Current.ProcessId; } catch { }
                string procName = string.Empty;
                try { if (pid > 0) procName = Process.GetProcessById(pid).ProcessName; } catch { }

                var walker = SWA.TreeWalker.ControlViewWalker;
                var win = el; SWA.AutomationElement? last = null;
                int safety = 0;
                while (win != null && safety++ < 50) { last = win; win = SafeParent(walker, win); }
                var top = last ?? el;

                var chain = new System.Collections.Generic.List<SWA.AutomationElement>();
                var curEl = el;
                safety = 0;
                while (curEl != null && curEl != top && safety++ < 50)
                {
                    chain.Add(curEl);
                    curEl = SafeParent(walker, curEl);
                }
                chain.Reverse();

                var b = new UiBookmarks.Bookmark { Name = string.Empty, ProcessName = string.IsNullOrWhiteSpace(procName) ? "Unknown" : procName };

                foreach (var nodeEl in chain)
                {
                    string? name = Try(() => nodeEl.Current.Name);
                    string? className = Try(() => nodeEl.Current.ClassName);
                    string? autoId = Try(() => nodeEl.Current.AutomationId);
                    int? ctId = Try(() => nodeEl.Current.ControlType?.Id);
                    int index = 0;
                    try
                    {
                        var parent = SafeParent(walker, nodeEl);
                        if (parent != null)
                        {
                            var siblings = SafeChildren(parent);
                            for (int i = 0; i < siblings.Length; i++)
                            {
                                if (SWA.Automation.Compare(siblings[i], nodeEl)) { index = i; break; }
                            }
                        }
                    }
                    catch { }
                    b.Chain.Add(new UiBookmarks.Node
                    {
                        Name = name,
                        ClassName = className,
                        AutomationId = autoId,
                        ControlTypeId = ctId,
                        IndexAmongMatches = index,
                        Include = true,
                        UseName = !string.IsNullOrEmpty(name),
                        UseClassName = !string.IsNullOrEmpty(className),
                        UseControlTypeId = ctId.HasValue,
                        UseAutomationId = !string.IsNullOrEmpty(autoId),
                        UseIndex = true,
                        Scope = UiBookmarks.SearchScope.Children
                    });
                }
                var cls = classNameSafe(el) ?? "?";
                var ctid = controlTypeIdSafe(el);
                return (b, procName, $"Captured element: {cls} ({ctid}) in {procName}");

                static SWA.AutomationElement? SafeParent(SWA.TreeWalker walker, SWA.AutomationElement e)
                {
                    try { return walker.GetParent(e); } catch { return null; }
                }
                static SWA.AutomationElement[] SafeChildren(SWA.AutomationElement e)
                {
                    try { return e.FindAll(SWA.TreeScope.Children, SWA.Condition.TrueCondition).Cast<SWA.AutomationElement>().ToArray(); } catch { return Array.Empty<SWA.AutomationElement>(); }
                }
                static T? Try<T>(Func<T?> f) { try { return f(); } catch { return default; } }
                static string? classNameSafe(SWA.AutomationElement e) { try { return e.Current.ClassName; } catch { return null; } }
                static int? controlTypeIdSafe(SWA.AutomationElement e) { try { return e.Current.ControlType?.Id; } catch { return null; } }
            }
            catch (Exception ex)
            {
                return (null, null, $"Pick error: {ex.Message}");
            }
        }

        private void HighlightBookmark(UiBookmarks.Bookmark b)
        {
            var (hwnd, el) = UiBookmarks.TryResolveBookmark(b);
            if (el == null) {
                try {
                    var (_, _, trace) = UiBookmarks.TryResolveWithTrace(b);
                    Debug.WriteLine("[PP1][ResolveTrace]\r\n" + trace);
                } catch { }
                txtStatus.Text += " | Resolve failed"; return;
            }
            var r = el.BoundingRectangle;
            var rect = new System.Drawing.Rectangle((int)r.Left, (int)r.Top, (int)r.Width, (int)r.Height);
            _overlay.ShowForRect(rect);
        }

        private void OnMapSelected(object sender, RoutedEventArgs e)
        {
            var item = cmbKnown.SelectedItem as System.Windows.Controls.ComboBoxItem;
            var keyStr = item?.Tag as string;
            if (string.IsNullOrWhiteSpace(keyStr)) { txtStatus.Text = "Select a known control"; return; }
            if (!Enum.TryParse<UiBookmarks.KnownControl>(keyStr, out var key)) { txtStatus.Text = "Invalid known control"; return; }

            var (b, procName, msg) = CaptureUnderMouse(preferAutomationId: true);
            txtStatus.Text = msg;
            if (!string.IsNullOrWhiteSpace(procName)) txtProcess.Text = procName;
            if (b == null) return;

            var method = CmbMethod.SelectedIndex == 0 ? UiBookmarks.MapMethod.Chain : UiBookmarks.MapMethod.AutomationIdOnly;
            b.Method = method;
            if (method == UiBookmarks.MapMethod.AutomationIdOnly)
            {
                var directId = b.Chain.LastOrDefault()?.AutomationId;
                b.DirectAutomationId = string.IsNullOrWhiteSpace(directId) ? null : directId;
            }

            UiBookmarks.SaveMapping(key, b);
            ShowBookmarkDetails(b, $"Mapped {key}");
            HighlightBookmark(b);
            txtStatus.Text = $"Mapped {key}";
        }

        private void OnResolveSelected(object sender, RoutedEventArgs e)
        {
            var item = cmbKnown.SelectedItem as System.Windows.Controls.ComboBoxItem;
            var keyStr = item?.Tag as string;
            if (string.IsNullOrWhiteSpace(keyStr)) { txtStatus.Text = "Select a known control"; return; }
            if (!Enum.TryParse<UiBookmarks.KnownControl>(keyStr, out var key))
            { txtStatus.Text = "Invalid known control"; return; }
            var mapping = UiBookmarks.GetMapping(key);
            if (mapping == null) { txtStatus.Text = "No mapping saved"; return; }
            var (hwnd, el) = UiBookmarks.Resolve(key);
            if (el == null) { txtStatus.Text += " | Resolve failed"; return; }
            var r = el.BoundingRectangle;
            var rect = new System.Drawing.Rectangle((int)r.Left, (int)r.Top, (int)r.Width, (int)r.Height);
            _overlay.ShowForRect(rect);
            txtStatus.Text = $"Resolved {key}";
        }

        private void ForceCommitGridEdits()
        {
            try
            {
                gridChain.CommitEdit(DataGridEditingUnit.Cell, true);
                gridChain.CommitEdit(DataGridEditingUnit.Row, true);
            }
            catch { }
        }

        private void OnValidateChain(object sender, RoutedEventArgs e)
        {
            ForceCommitGridEdits();
            if (!BuildBookmarkFromUi(out var copy)) return;
            var diag = BuildChainDiagnostic(copy);
            var sw = Stopwatch.StartNew();
            var (_, el, trace) = UiBookmarks.TryResolveWithTrace(copy);
            sw.Stop();
            _lastResolved = el;
            if (el == null)
            {
                txtStatus.Text = diag + $"Validate: not found ({sw.ElapsedMilliseconds} ms)\r\n" + trace;
                return;
            }
            var r = el.BoundingRectangle;
            var rect = new System.Drawing.Rectangle((int)r.Left, (int)r.Top, (int)r.Width, (int)r.Height);
            _overlay.ShowForRect(rect);
            txtStatus.Text = diag + $"Validate: found and highlighted ({sw.ElapsedMilliseconds} ms)";
        }

        private void OnSaveEdited(object sender, RoutedEventArgs e)
        {
            ForceCommitGridEdits();
            UiBookmarks.Bookmark toSave;
            if (_editing == null) { txtStatus.Text = "Nothing to save"; return; }
            SaveEditorInto(_editing);
            UiBookmarks.KnownControl key;
            var knownItem = cmbKnown?.SelectedItem as System.Windows.Controls.ComboBoxItem;
            var tagStr = knownItem?.Tag as string;
            if (!string.IsNullOrWhiteSpace(tagStr) && Enum.TryParse<UiBookmarks.KnownControl>(tagStr, out key))
            {
                toSave = new UiBookmarks.Bookmark
                {
                    Name = key.ToString(),
                    ProcessName = _editing.ProcessName,
                    Method = _editing.Method,
                    DirectAutomationId = _editing.DirectAutomationId,
                    CrawlFromRoot = _editing.CrawlFromRoot,
                    Chain = _editing.Chain.ToList()
                };
                UiBookmarks.SaveMapping(key, toSave);
                txtStatus.Text = $"Saved mapping for {key}";
                return;
            }
            var store = UiBookmarks.Load();
            var name = string.IsNullOrWhiteSpace(_editing.Name) ? "Bookmark" : _editing.Name;
            var existing = store.Bookmarks.FirstOrDefault(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                existing = new UiBookmarks.Bookmark { Name = name };
                store.Bookmarks.Add(existing);
            }
            existing.ProcessName = _editing.ProcessName;
            existing.Method = _editing.Method;
            existing.DirectAutomationId = _editing.DirectAutomationId;
            existing.CrawlFromRoot = _editing.CrawlFromRoot;
            existing.Chain = _editing.Chain.ToList();
            UiBookmarks.Save(store);
            txtStatus.Text = $"Saved bookmark '{name}'";
        }

        // === Re-added helper methods lost during prior patch ===
        private static List<string> GetHeaderTexts(AutomationElement list)
        {
            var result = new List<string>();
            try
            {
                var kids = list.FindAllChildren();
                if (kids.Length > 0)
                {
                    var headerRow = kids[0];
                    var headerCells = headerRow.FindAllChildren();
                    if (headerCells.Length > 0)
                    {
                        foreach (var hc in headerCells)
                        {
                            try
                            {
                                var t = ReadCellText(hc);
                                if (string.IsNullOrWhiteSpace(t))
                                {
                                    foreach (var g in hc.FindAllChildren()) { t = ReadCellText(g); if (!string.IsNullOrWhiteSpace(t)) break; }
                                }
                                result.Add(string.IsNullOrWhiteSpace(t) ? string.Empty : t.Trim());
                            }
                            catch { result.Add(string.Empty); }
                        }
                        if (result.Count > 0) return result;
                    }
                }
                var header = kids.FirstOrDefault(c => { try { return (int)c.ControlType == UIA_Header; } catch { return false; } });
                if (header == null)
                {
                    foreach (var ch in kids)
                    {
                        try { var h = ch.FindAllChildren().FirstOrDefault(cc => (int)cc.ControlType == UIA_Header); if (h != null) { header = h; break; } } catch { }
                    }
                }
                if (header != null)
                {
                    foreach (var hi in header.FindAllChildren())
                    {
                        try
                        {
                            string txt = string.Empty;
                            if ((int)hi.ControlType == UIA_HeaderItem || (int)hi.ControlType == UIA_Text) txt = ReadCellText(hi);
                            if (string.IsNullOrWhiteSpace(txt))
                            {
                                foreach (var g in hi.FindAllChildren()) { txt = ReadCellText(g); if (!string.IsNullOrWhiteSpace(txt)) break; }
                            }
                            result.Add(string.IsNullOrWhiteSpace(txt) ? string.Empty : txt.Trim());
                        }
                        catch { result.Add(string.Empty); }
                    }
                }
            }
            catch { }
            return result;
        }

        private static List<string> GetRowCellValues(AutomationElement row)
        {
            var values = new List<string>();
            try
            {
                var children = row.FindAllChildren();
                if (children.Length > 0)
                {
                    foreach (var c in children)
                    {
                        try
                        {
                            string cellText = ReadCellText(c).Trim();
                            if (string.IsNullOrEmpty(cellText))
                            {
                                foreach (var gc in c.FindAllChildren()) { var t = ReadCellText(gc).Trim(); if (!string.IsNullOrEmpty(t)) { cellText = t; break; } }
                            }
                            values.Add(cellText);
                        }
                        catch { values.Add(string.Empty); }
                    }
                }
                else
                {
                    foreach (var d in row.FindAllDescendants())
                    {
                        try
                        {
                            if ((int)d.ControlType == UIA_Text || (int)d.ControlType == UIA_DataItem)
                            {
                                var t = ReadCellText(d).Trim();
                                values.Add(t);
                                if (values.Count >= 20) break;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
            return values;
        }

        private static bool TryParseYmdOrYmdHms(string s, out DateTime dt)
        {
            dt = default;
            if (DateTime.TryParseExact(s, new[] { "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss" }, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeLocal, out var parsed)) { dt = parsed; return true; }
            return false;
        }

        private void OnInvoke(object sender, RoutedEventArgs e)
        {
            if (_lastResolved == null)
            {
                if (!BuildBookmarkFromUi(out var copy)) return;
                var (_, el, _) = UiBookmarks.TryResolveWithTrace(copy);
                _lastResolved = el;
                if (el == null) { txtStatus.Text = "Invoke: not found"; return; }
            }
            try
            {
                var inv = _lastResolved.Patterns.Invoke.PatternOrDefault;
                if (inv != null) { inv.Invoke(); txtStatus.Text = "Invoke: done"; return; }
                var toggle = _lastResolved.Patterns.Toggle.PatternOrDefault;
                if (toggle != null) { toggle.Toggle(); txtStatus.Text = "Toggle: done"; return; }
                txtStatus.Text = "Invoke: pattern not supported";
            }
            catch (Exception ex) { txtStatus.Text = "Invoke error: " + ex.Message; }
        }

        private void OnGetText(object sender, RoutedEventArgs e)
        {
            if (_lastResolved == null)
            {
                if (!BuildBookmarkFromUi(out var copy)) return;
                var (_, el, _) = UiBookmarks.TryResolveWithTrace(copy);
                _lastResolved = el;
                if (el == null) { txtStatus.Text = "Get Text: not found"; return; }
            }
            try
            {
                var name = _lastResolved.Name;
                var value = _lastResolved.Patterns.Value.PatternOrDefault?.Value ?? string.Empty;
                var legacy = _lastResolved.Patterns.LegacyIAccessible.PatternOrDefault?.Name ?? string.Empty;
                var txt = !string.IsNullOrEmpty(value) ? value : (!string.IsNullOrEmpty(name) ? name : legacy);
                txtStatus.Text = string.IsNullOrEmpty(txt) ? "Get Text: empty" : $"Get Text: {txt}";
            }
            catch (Exception ex) { txtStatus.Text = "Get Text error: " + ex.Message; }
        }

        private void OnGetName(object sender, RoutedEventArgs e)
        {
            if (_lastResolved == null)
            {
                if (!BuildBookmarkFromUi(out var copy)) return;
                var (_, el, _) = UiBookmarks.TryResolveWithTrace(copy);
                _lastResolved = el;
                if (el == null) { txtStatus.Text = "Get Name: not found"; return; }
            }
            try
            {
                var name = _lastResolved.Name;
                txtStatus.Text = string.IsNullOrEmpty(name) ? "Get Name: empty" : $"Get Name: {name}";
            }
            catch (Exception ex) { txtStatus.Text = "Get Name error: " + ex.Message; }
        }

        private void OnOpComboPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.ComboBox cb && !cb.IsDropDownOpen)
                {
                    e.Handled = true;
                    var cell = FindParent<DataGridCell>(cb);
                    var grid = FindParent<DataGrid>(cell) ?? (DataGrid?)FindName("gridProcSteps");
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
                        var cell = FindParent<DataGridCell>(cb);
                        var grid = FindParent<DataGrid>(cell) ?? (DataGrid?)FindName("gridProcSteps");
                        try { grid?.BeginEdit(); } catch { }
                        cb.Focus(); cb.IsDropDownOpen = !cb.IsDropDownOpen; e.Handled = true;
                    }
                }
            }
            catch { }
        }

        private static TParent? FindParent<TParent>(DependencyObject? child) where TParent : DependencyObject
        {
            while (child != null)
            {
                var parent = VisualTreeHelper.GetParent(child);
                if (parent is TParent tp) return tp;
                child = parent;
            }
            return null;
        }

        private void FlushGridEdits()
        {
            try
            {
                gridChain.CommitEdit(DataGridEditingUnit.Cell, true);
                gridChain.CommitEdit(DataGridEditingUnit.Row, true);
                foreach (var item in gridChain.Items)
                {
                    if (gridChain.ItemContainerGenerator.ContainerFromItem(item) is not DataGridRow row) continue;
                    foreach (var cb in FindVisualChildren<System.Windows.Controls.CheckBox>(row))
                        cb.GetBindingExpression(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty)?.UpdateSource();
                    foreach (var tb in FindVisualChildren<System.Windows.Controls.TextBox>(row))
                        tb.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
                    foreach (var combo in FindVisualChildren<System.Windows.Controls.ComboBox>(row))
                    {
                        combo.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedIndexProperty)?.UpdateSource();
                        combo.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedValueProperty)?.UpdateSource();
                    }
                }
            }
            catch { }
        }
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject d) where T : DependencyObject
        {
            if (d == null) yield break;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
            {
                var c = VisualTreeHelper.GetChild(d, i);
                if (c is T t) yield return t;
                foreach (var cc in FindVisualChildren<T>(c)) yield return cc;
            }
        }

        private bool BuildBookmarkFromUi(out UiBookmarks.Bookmark copy)
        {
            copy = new UiBookmarks.Bookmark();
            if (_editing == null) { txtStatus.Text = "No chain to validate"; return false; }
            FlushGridEdits();
            try
            {
                gridChain.CommitEdit(DataGridEditingUnit.Cell, true);
                gridChain.CommitEdit(DataGridEditingUnit.Row, true);
            }
            catch { }

            // Take the live nodes from the grid (authoritative) instead of _editing.Chain
            var liveNodes = gridChain.Items.OfType<UiBookmarks.Node>().ToList();
            // Persist them back into the editing bookmark so later Save uses the modified values
            _editing.Chain = liveNodes;

            copy = new UiBookmarks.Bookmark
            {
                Name = _editing.Name,
                ProcessName = _editing.ProcessName,
                Method = cmbMethod.SelectedIndex == 0 ? UiBookmarks.MapMethod.Chain : UiBookmarks.MapMethod.AutomationIdOnly,
                CrawlFromRoot = _editing.CrawlFromRoot,
                DirectAutomationId = null,
                Chain = liveNodes.Select(n => new UiBookmarks.Node
                {
                    Name = n.Name,
                    ClassName = n.ClassName,
                    ControlTypeId = n.ControlTypeId,
                    AutomationId = n.AutomationId,
                    IndexAmongMatches = n.IndexAmongMatches,
                    Include = n.Include,
                    UseName = n.UseName,
                    UseClassName = n.UseClassName,
                    UseControlTypeId = n.UseControlTypeId,
                    UseAutomationId = n.UseAutomationId,
                    UseIndex = n.UseIndex,
                    Scope = n.Scope,
                    Order = n.Order
                }).ToList()
            };
            if (copy.Method == UiBookmarks.MapMethod.AutomationIdOnly)
            {
                copy.DirectAutomationId = copy.Chain.LastOrDefault()?.AutomationId;
                if (string.IsNullOrWhiteSpace(copy.DirectAutomationId)) copy.DirectAutomationId = null;
            }
            if (!string.IsNullOrWhiteSpace(txtProcess.Text)) copy.ProcessName = txtProcess.Text.Trim();
            _lastResolved = null;
            return true;
        }

        private string BuildChainDiagnostic(UiBookmarks.Bookmark b)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Idx Inc Nm Cls Ctl Auto Idx Scope | Name / Class / Ctrl / AutoId");
            for (int i = 0; i < b.Chain.Count; i++)
            {
                var n = b.Chain[i];
                sb.AppendFormat("{0,2}  {1}   {2}{3}{4}{5}{6}  {7} | {8} / {9} / {10} / {11}\r\n",
                    i,
                    n.Include ? 'Y' : 'N',
                    n.UseName ? 'N' : '-',
                    n.UseClassName ? 'C' : '-',
                    n.UseControlTypeId ? 'T' : '-',
                    n.UseAutomationId ? 'A' : '-',
                    n.UseIndex ? 'I' : '-',
                    n.Scope,
                    n.Name ?? "",
                    n.ClassName ?? "",
                    n.ControlTypeId?.ToString() ?? "",
                    n.AutomationId ?? "");
            }
            return sb.ToString();
        }

        // Restored missing handlers
        private void OnPreviewMouseDownForQuickMap(object? sender, MouseButtonEventArgs e)
        {
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl)) return;
            if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift)) return;
            var item = cmbKnown.SelectedItem as System.Windows.Controls.ComboBoxItem;
            var keyStr = item?.Tag as string;
            if (string.IsNullOrWhiteSpace(keyStr)) { txtStatus.Text = "Select a known control"; return; }
            var (b, procName, msg) = CaptureUnderMouse(preferAutomationId: true);
            txtStatus.Text = msg;
            if (!string.IsNullOrWhiteSpace(procName)) txtProcess.Text = procName;
            if (b == null) return;
            if (!Enum.TryParse<UiBookmarks.KnownControl>(keyStr, out var key)) { txtStatus.Text = "Invalid known control"; return; }
            var method = CmbMethod.SelectedIndex == 0 ? UiBookmarks.MapMethod.Chain : UiBookmarks.MapMethod.AutomationIdOnly;
            b.Method = method;
            if (method == UiBookmarks.MapMethod.AutomationIdOnly)
            {
                var directId = b.Chain.LastOrDefault()?.AutomationId;
                b.DirectAutomationId = string.IsNullOrWhiteSpace(directId) ? null : directId;
            }
            UiBookmarks.SaveMapping(key, b);
            LoadEditor(b);
            HighlightBookmark(b);
            txtStatus.Text = $"Quick-mapped {key}";
        }

        private void OnAncestrySelected(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is null) { txtNodeProps.Text = string.Empty; return; }
            if (e.NewValue is not object) { txtNodeProps.Text = string.Empty; return; }
        }

        private void OnGetSelectedRow(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureResolved();
                if (_lastResolved == null) { txtStatus.Text = "Row Data: not found"; return; }
                var list = _lastResolved;
                var selection = list.Patterns.Selection.PatternOrDefault;
                var selected = selection?.Selection?.Value ?? Array.Empty<AutomationElement>();
                if (selected.Length == 0)
                {
                    selected = list.FindAllDescendants().Where(a =>
                    {
                        try { return a.Patterns.SelectionItem.IsSupported && a.Patterns.SelectionItem.PatternOrDefault?.IsSelected == true; }
                        catch { return false; }
                    }).ToArray();
                }
                if (selected.Length == 0) { txtStatus.Text = "Row Data: no selection"; return; }
                var row = selected[0];
                var headers = GetHeaderTexts(list);
                var cellValues = GetRowCellValues(row);
                if (headers.Count < cellValues.Count)
                    for (int i = headers.Count; i < cellValues.Count; i++) headers.Add($"Col{i + 1}");
                else if (cellValues.Count < headers.Count)
                    for (int i = cellValues.Count; i < headers.Count; i++) cellValues.Add(string.Empty);
                var pairs = headers.Zip(cellValues, (h, v) => (Header: NormalizeHeader(h), Value: v)).ToList();
                var line = string.Join(" | ", pairs.Select(p => string.IsNullOrWhiteSpace(p.Header) ? p.Value : $"{p.Header}: {p.Value}"));
                txtStatus.Text = string.IsNullOrWhiteSpace(line) ? "Row Data: empty" : line;
            }
            catch (Exception ex) { txtStatus.Text = "Row Data error: " + ex.Message; }
        }

        private void OnMoveUp(object sender, RoutedEventArgs e)
        {
            var items = GridChain.ItemsSource as IList<UiBookmarks.Node> ?? GridChain.Items.OfType<UiBookmarks.Node>().ToList();
            var sel = GridChain.SelectedItem as UiBookmarks.Node; if (sel == null) return; int idx = items.IndexOf(sel); if (idx > 0) { items.RemoveAt(idx); items.Insert(idx - 1, sel); GridChain.ItemsSource = null; GridChain.ItemsSource = items; GridChain.SelectedItem = sel; }
        }
        private void OnMoveDown(object sender, RoutedEventArgs e)
        {
            var items = GridChain.ItemsSource as IList<UiBookmarks.Node> ?? GridChain.Items.OfType<UiBookmarks.Node>().ToList();
            var sel = GridChain.SelectedItem as UiBookmarks.Node; if (sel == null) return; int idx = items.IndexOf(sel); if (idx >= 0 && idx < items.Count - 1) { items.RemoveAt(idx); items.Insert(idx + 1, sel); GridChain.ItemsSource = null; GridChain.ItemsSource = items; GridChain.SelectedItem = sel; }
        }
        private void OnInsertAbove(object sender, RoutedEventArgs e)
        {
            var items = GridChain.ItemsSource as IList<UiBookmarks.Node> ?? GridChain.Items.OfType<UiBookmarks.Node>().ToList();
            var sel = GridChain.SelectedItem as UiBookmarks.Node; int idx = sel != null ? items.IndexOf(sel) : items.Count; var n = new UiBookmarks.Node { Include = true }; items.Insert(Math.Max(0, idx), n); GridChain.ItemsSource = null; GridChain.ItemsSource = items; GridChain.SelectedItem = n;
        }
        private void OnDeleteNode(object sender, RoutedEventArgs e)
        {
            var items = GridChain.ItemsSource as IList<UiBookmarks.Node> ?? GridChain.Items.OfType<UiBookmarks.Node>().ToList();
            var sel = GridChain.SelectedItem as UiBookmarks.Node; if (sel == null) return; items.Remove(sel); GridChain.ItemsSource = null; GridChain.ItemsSource = items;
        }
        private void OnTemplateCheckBoxClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.CheckBox cb)
                {
                    var cell = FindParent<DataGridCell>(cb);
                    var grid = FindParent<DataGrid>(cell) ?? gridChain;
                    grid.CommitEdit(DataGridEditingUnit.Cell, true);
                    grid.CommitEdit(DataGridEditingUnit.Row, true);
                }
            }
            catch { }
        }
        private void OnTemplateComboSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.ComboBox combo && combo.IsDropDownOpen == false)
                {
                    var cell = FindParent<DataGridCell>(combo);
                    var grid = FindParent<DataGrid>(cell) ?? gridChain;
                    grid.CommitEdit(DataGridEditingUnit.Cell, true);
                    grid.CommitEdit(DataGridEditingUnit.Row, true);
                }
            }
            catch { }
        }
        private void OnGridCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    gridChain.CommitEdit(DataGridEditingUnit.Cell, true);
                    gridChain.CommitEdit(DataGridEditingUnit.Row, true);
                }
                catch { }
            }), DispatcherPriority.Background);
        }
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                try { gridChain.CommitEdit(DataGridEditingUnit.Cell, true); gridChain.CommitEdit(DataGridEditingUnit.Row, true); } catch { }
            }
        }
    }
}
