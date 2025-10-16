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
    /// <summary>
    /// Core partial of SpyWindow. Heavy logic has been split into multiple partial class files:
    ///  - SpyWindow.Procedures.cs : Custom procedure model + execution
    ///  - SpyWindow.Bookmarks.cs  : Bookmark capture / edit / mapping logic
    ///  - SpyWindow.Tree.cs       : (Disabled) ancestry/tree view helpers
    ///  - SpyWindow.UIAHelpers.cs : UIA helper & parsing utilities
    /// This core file now contains only bootstrapping, shared fields, and simple command handlers that
    /// delegate to logic in the other partials.
    /// </summary>
    public partial class SpyWindow : System.Windows.Window
    {
        // ------------------------------------------------------------------
        // Single instance management
        // ------------------------------------------------------------------
        private static SpyWindow? _instance;
        
        public static void ShowInstance()
        {
            if (_instance == null || !_instance.IsLoaded)
            {
                _instance = new SpyWindow();
                _instance.Closed += (s, e) => _instance = null;
            }
            
            _instance.Show();
            _instance.Activate();
        }
        
        // ------------------------------------------------------------------
        // Shared fields & objects
        // ------------------------------------------------------------------

        private readonly HighlightOverlay _overlay = new HighlightOverlay();
        private UiBookmarks.Bookmark? _editing;          // Active bookmark being edited
        private AutomationElement? _lastResolved;        // Cached resolved element (for quick actions)

        // Expose known controls and procedure vars (populated in procedures partial)
        public List<string> KnownControlTags { get; } = Enum.GetNames(typeof(UiBookmarks.KnownControl)).ToList();
        public ObservableCollection<string> ProcedureVars { get; } = new();

        // Convenient accessors to XAML controls
        private System.Windows.Controls.ComboBox CmbMethod => (System.Windows.Controls.ComboBox)FindName("cmbMethod");
        private System.Windows.Controls.DataGrid GridChain => (System.Windows.Controls.DataGrid)FindName("gridChain");
        private System.Windows.Controls.CheckBox? _chkEnableTree => (System.Windows.Controls.CheckBox?)FindName("chkEnableTree");

        // ------------------------------------------------------------------
        // DWM (dark title bar) setup
        // ------------------------------------------------------------------
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
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        // ------------------------------------------------------------------
        // Constructor
        // ------------------------------------------------------------------
        public SpyWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadBookmarks(); // (implemented in Bookmarks partial)
            PreviewMouseDown += OnPreviewMouseDownForQuickMap; // quick-map hotkey handler (Bookmarks partial)

            // Custom procedures grid wiring (handlers in Procedures partial)
            if (FindName("gridProcSteps") is System.Windows.Controls.DataGrid procGrid) procGrid.ItemsSource = new List<ProcOpRow>();
            if (FindName("cmbProcMethod") is System.Windows.Controls.ComboBox cmbMethodProc) cmbMethodProc.SelectionChanged += OnProcMethodChanged;

            // Use PACS from tenant context (set in settings). Show and listen for changes.
            try
            {
                if (Application.Current is App app)
                {
                    var tenant = app.Services.GetService(typeof(Wysg.Musm.Radium.Services.ITenantContext)) as Wysg.Musm.Radium.Services.ITenantContext;
                    if (tenant != null)
                    {
                        txtPacsKey.Text = string.IsNullOrWhiteSpace(tenant.CurrentPacsKey) ? "default_pacs" : tenant.CurrentPacsKey;
                        tenant.PacsKeyChanged += (oldKey, newKey) =>
                        {
                            Dispatcher.Invoke(() => txtPacsKey.Text = string.IsNullOrWhiteSpace(newKey) ? "default_pacs" : newKey);
                        };
                    }
                }
            }
            catch { }
        }

        // ------------------------------------------------------------------
        // Utility: ensure we have a resolved element (used by quick commands)
        // ------------------------------------------------------------------
        private void EnsureResolved()
        {
            if (_lastResolved != null) return;
            if (!BuildBookmarkFromUi(out var copy)) return; // BuildBookmarkFromUi in Bookmarks partial
            var (_, el, _) = UiBookmarks.TryResolveWithTrace(copy);
            _lastResolved = el;
        }

        // ------------------------------------------------------------------
        // Quick action buttons (Invoke / GetText / GetName / Selected row data)
        // These rely on helpers & bookmark logic implemented in other partials.
        // ------------------------------------------------------------------
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
                var headers = GetHeaderTexts(list);          // from UIAHelpers partial
                var cellValues = GetRowCellValues(row);      // from UIAHelpers partial
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

        // New: Get HTML (clipboard URL) ? mirrors Custom Procedure GetHTML op with smart decoding
        private async void OnGetHtml(object sender, RoutedEventArgs e)
        {
            try
            {
                string clip = string.Empty;
                try { clip = Clipboard.ContainsText() ? Clipboard.GetText().Trim() : string.Empty; } catch { clip = string.Empty; }
                if (string.IsNullOrWhiteSpace(clip) || !(clip.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || clip.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                {
                    txtStatus.Text = "Get HTML: copy a URL to clipboard first.";
                    return;
                }

                txtStatus.Text = "Get HTML: fetching...";
                var html = await HttpGetHtmlSmartAsync(clip);
                txtStatus.Text = html ?? "(null)";
            }
            catch (Exception ex)
            {
                txtStatus.Text = "Get HTML error: " + ex.Message;
            }
        }

        // ------------------------------------------------------------------
        // Grid commit helper (used across bookmark editing logic)
        // ------------------------------------------------------------------
        private void ForceCommitGridEdits()
        {
            try
            {
                gridChain.CommitEdit(DataGridEditingUnit.Cell, true);
                gridChain.CommitEdit(DataGridEditingUnit.Row, true);
            }
            catch { }
        }

        // ------------------------------------------------------------------
        // Keyboard commit for chain grid
        // ------------------------------------------------------------------
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
