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
    /// BookmarkItem represents either a KnownControl or a user-defined bookmark
    /// </summary>
    public class BookmarkItem : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        public string Name 
        { 
            get => _name; 
            set 
            { 
                if (_name != value) 
                { 
                    _name = value; 
                    OnPropertyChanged(); 
                } 
            } 
        }
        
        public string? Tag { get; set; } // For KnownControl, this is the enum name
        public bool IsKnownControl { get; set; }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

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
        
        // NEW: Dynamic bookmarks collection
        public ObservableCollection<BookmarkItem> BookmarkItems { get; } = new();

        // Convenient accessors to XAML controls
        private System.Windows.Controls.ComboBox CmbMethod => (System.Windows.Controls.ComboBox)FindName("cmbMethod");
        private System.Windows.Controls.DataGrid GridChain => (System.Windows.Controls.DataGrid)FindName("gridChain");

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
            LoadBookmarksIntoComboBox(); // NEW: Load dynamic bookmarks
            LoadBookmarks(); // (implemented in Bookmarks partial)

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
        // NEW: Bookmark management methods
        // ------------------------------------------------------------------
        
        /// <summary>
        /// Load all bookmarks (KnownControls + user bookmarks) into ComboBox
        /// </summary>
        private void LoadBookmarksIntoComboBox()
        {
            BookmarkItems.Clear();
            
            // Add KnownControls
            foreach (UiBookmarks.KnownControl knownCtrl in Enum.GetValues(typeof(UiBookmarks.KnownControl)))
            {
                var name = knownCtrl.ToString();
                // Format name: convert "StudyInfoBanner" to "Study Info Banner"
                var displayName = System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
                displayName = System.Text.RegularExpressions.Regex.Replace(displayName, "([A-Z]+)([A-Z][a-z])", "$1 $2");
                
                BookmarkItems.Add(new BookmarkItem
                {
                    Name = displayName.ToLowerInvariant(),
                    Tag = name,
                    IsKnownControl = true
                });
            }
            
            // Add user bookmarks
            var store = UiBookmarks.Load();
            foreach (var bookmark in store.Bookmarks.OrderBy(b => b.Name))
            {
                BookmarkItems.Add(new BookmarkItem
                {
                    Name = bookmark.Name,
                    Tag = null,
                    IsKnownControl = false
                });
            }
        }
        
        /// <summary>
        /// Add a new user-defined bookmark
        /// </summary>
        private void OnAddBookmark(object sender, RoutedEventArgs e)
        {
            var name = PromptForBookmarkName("New Bookmark");
            if (string.IsNullOrWhiteSpace(name)) return;
            
            // Check if name already exists
            var store = UiBookmarks.Load();
            if (store.Bookmarks.Any(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                txtStatus.Text = $"Bookmark '{name}' already exists";
                return;
            }
            
            // Create new bookmark with current chain if available
            var newBookmark = new UiBookmarks.Bookmark
            {
                Name = name,
                ProcessName = txtProcess.Text?.Trim() ?? string.Empty,
                Method = CmbMethod.SelectedIndex == 0 ? UiBookmarks.MapMethod.Chain : UiBookmarks.MapMethod.AutomationIdOnly,
                Chain = _editing?.Chain.ToList() ?? new List<UiBookmarks.Node>()
            };
            
            store.Bookmarks.Add(newBookmark);
            UiBookmarks.Save(store);
            
            LoadBookmarksIntoComboBox();
            
            // Select the new bookmark
            var newItem = BookmarkItems.FirstOrDefault(b => b.Name == name && !b.IsKnownControl);
            if (FindName("cmbKnown") is System.Windows.Controls.ComboBox combo && newItem != null)
            {
                combo.SelectedItem = newItem;
            }
            
            txtStatus.Text = $"Added bookmark '{name}'";
        }
        
        /// <summary>
        /// Delete the selected user-defined bookmark
        /// </summary>
        private void OnDeleteBookmark(object sender, RoutedEventArgs e)
        {
            if (FindName("cmbKnown") is not System.Windows.Controls.ComboBox combo) return;
            if (combo.SelectedItem is not BookmarkItem item) return;
            
            if (item.IsKnownControl)
            {
                txtStatus.Text = "Cannot delete built-in bookmarks";
                return;
            }
            
            var result = MessageBox.Show(
                $"Delete bookmark '{item.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result != MessageBoxResult.Yes) return;
            
            var store = UiBookmarks.Load();
            var toRemove = store.Bookmarks.FirstOrDefault(b => string.Equals(b.Name, item.Name, StringComparison.OrdinalIgnoreCase));
            if (toRemove != null)
            {
                store.Bookmarks.Remove(toRemove);
                UiBookmarks.Save(store);
                LoadBookmarksIntoComboBox();
                txtStatus.Text = $"Deleted bookmark '{item.Name}'";
            }
        }
        
        /// <summary>
        /// Rename the selected user-defined bookmark
        /// </summary>
        private void OnRenameBookmark(object sender, RoutedEventArgs e)
        {
            if (FindName("cmbKnown") is not System.Windows.Controls.ComboBox combo) return;
            if (combo.SelectedItem is not BookmarkItem item) return;
            
            if (item.IsKnownControl)
            {
                txtStatus.Text = "Cannot rename built-in bookmarks";
                return;
            }
            
            var newName = PromptForBookmarkName("Rename Bookmark", item.Name);
            if (string.IsNullOrWhiteSpace(newName) || newName == item.Name) return;
            
            var store = UiBookmarks.Load();
            
            // Check if new name already exists
            if (store.Bookmarks.Any(b => string.Equals(b.Name, newName, StringComparison.OrdinalIgnoreCase)))
            {
                txtStatus.Text = $"Bookmark '{newName}' already exists";
                return;
            }
            
            var toRename = store.Bookmarks.FirstOrDefault(b => string.Equals(b.Name, item.Name, StringComparison.OrdinalIgnoreCase));
            if (toRename != null)
            {
                toRename.Name = newName;
                UiBookmarks.Save(store);
                LoadBookmarksIntoComboBox();
                
                // Select the renamed bookmark
                var renamedItem = BookmarkItems.FirstOrDefault(b => b.Name == newName && !b.IsKnownControl);
                if (renamedItem != null) combo.SelectedItem = renamedItem;
                
                txtStatus.Text = $"Renamed bookmark to '{newName}'";
            }
        }
        
        /// <summary>
        /// Prompt user for bookmark name
        /// </summary>
        private string? PromptForBookmarkName(string title, string defaultName = "")
        {
            var dialog = new System.Windows.Window
            {
                Title = title,
                Width = 400,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")!),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D0D0D0")!),
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow
            };

            var grid = new System.Windows.Controls.Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var lblPrompt = new TextBlock
            {
                Text = "Bookmark name:",
                Margin = new Thickness(0, 0, 0, 10)
            };
            System.Windows.Controls.Grid.SetRow(lblPrompt, 0);
            grid.Children.Add(lblPrompt);

            var txtName = new System.Windows.Controls.TextBox
            {
                Text = defaultName,
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(6, 4, 6, 4),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")!),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D0D0D0")!),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3C3C3C")!),
                BorderThickness = new Thickness(1),
                CaretBrush = new SolidColorBrush(Colors.White)
            };
            System.Windows.Controls.Grid.SetRow(txtName, 1);
            grid.Children.Add(txtName);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            System.Windows.Controls.Grid.SetRow(buttonPanel, 2);

            var btnOk = new System.Windows.Controls.Button
            {
                Content = "OK",
                Width = 80,
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(10, 5, 10, 5),
                IsDefault = true
            };
            btnOk.Click += (s, e) => { dialog.DialogResult = true; dialog.Close(); };
            buttonPanel.Children.Add(btnOk);

            var btnCancel = new System.Windows.Controls.Button
            {
                Content = "Cancel",
                Width = 80,
                Padding = new Thickness(10, 5, 10, 5),
                IsCancel = true
            };
            btnCancel.Click += (s, e) => { dialog.DialogResult = false; dialog.Close(); };
            buttonPanel.Children.Add(btnCancel);

            grid.Children.Add(buttonPanel);
            dialog.Content = grid;

            // Focus textbox and select all when dialog opens
            dialog.Loaded += (s, e) => 
            { 
                txtName.Focus(); 
                txtName.SelectAll();
            };

            // Handle Enter key
            txtName.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    dialog.DialogResult = true;
                    dialog.Close();
                }
            };

            var result = dialog.ShowDialog();
            return result == true ? txtName.Text?.Trim() : null;
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
        
        private void OnSetFocus(object sender, RoutedEventArgs e)
        {
            if (_lastResolved == null)
            {
                if (!BuildBookmarkFromUi(out var copy)) return;
                var (_, el, _) = UiBookmarks.TryResolveWithTrace(copy);
                _lastResolved = el;
                if (el == null) { txtStatus.Text = "SetFocus: not found"; return; }
            }
            
            // NEW: Activate containing window before setting focus (required for complex controls like RichEdit)
            try
            {
                var hwnd = new IntPtr(_lastResolved.Properties.NativeWindowHandle.Value);
                if (hwnd != IntPtr.Zero)
                {
                    // Bring window to foreground using Win32 API
                    NativeMouseHelper.SetForegroundWindow(hwnd);
                    System.Threading.Thread.Sleep(100); // Brief delay for window activation
                }
            }
            catch { } // Activation failure shouldn't block focus attempt
            
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
                            _lastResolved.Focus();
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
                        var statusMsg = attempt > 1 ? $"SetFocus: focused after {attempt} attempts" : "SetFocus: focused";
                        txtStatus.Text = statusMsg;
                        success = true;
                        return;
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
                        System.Threading.Thread.Sleep(retryDelayMs);
                    }
                }
            }
            
            if (!success)
            {
                txtStatus.Text = $"SetFocus: error after {maxAttempts} attempts: {lastException?.Message}";
            }
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
