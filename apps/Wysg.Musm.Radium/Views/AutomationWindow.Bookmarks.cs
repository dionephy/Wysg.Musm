using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls; // WPF
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media; // Added for SolidColorBrush, Color, Colors
using FlaUI.Core.AutomationElements;
using SWA = System.Windows.Automation;
using Wysg.Musm.Radium.Services; // UiBookmarks
using WpfGrid = System.Windows.Controls.Grid; // Alias for WPF Grid
using WpfButton = System.Windows.Controls.Button; // Alias for WPF Button

namespace Wysg.Musm.Radium.Views
{
    /// <summary>
    /// Partial: Bookmark capture, editing, validation & mapping logic.
    /// </summary>
    public partial class AutomationWindow
    {
        // UIA constants needed by helper logic (kept internal to this partial)
        private const int UIA_ListItem = 50007;
        private const int UIA_Header = 50034;
        private const int UIA_HeaderItem = 50035;
        private const int UIA_Text = 50020;
        private const int UIA_DataItem = 50029;

        // P/Invoke for capture
        [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT lpPoint);
        [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(POINT Point);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X; public int Y; }

        // ------------------------------ Bookmark loading ------------------------------
        private void LoadBookmarks()
        {
            var store = UiBookmarks.Load();
            if (FindName("lstBookmarks") is System.Windows.Controls.ListBox lb)
                lb.ItemsSource = store.Bookmarks.OrderBy(b => b.Name).ToList();
        }
        private void OnReload(object sender, RoutedEventArgs e)
        {
            LoadBookmarks();
            LoadBookmarksIntoComboBox(); // Also reload ComboBox
        }
        private void OnBookmarkSelected(object sender, SelectionChangedEventArgs e)
        {
            if (FindName("lstBookmarks") is System.Windows.Controls.ListBox lb && lb.SelectedItem is UiBookmarks.Bookmark b)
            { LoadEditor(b); txtStatus.Text = $"Loaded bookmark: {b.Name}"; }
        }
        private void OnKnownSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FindName("cmbKnown") is not System.Windows.Controls.ComboBox combo) return;
            
            // Simplified: All bookmarks are now user-defined (no KnownControl enum)
            if (combo.SelectedItem is BookmarkItem item)
            {
                var store = UiBookmarks.Load();
                var bookmark = store.Bookmarks.FirstOrDefault(b => 
                    string.Equals(b.Name, item.Name, StringComparison.OrdinalIgnoreCase));
                if (bookmark != null) LoadEditor(bookmark);
            }
        }

        // ------------------------------ Editor load/save ------------------------------
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
            else b.DirectAutomationId = null;
            b.Chain = GridChain.Items.OfType<UiBookmarks.Node>().ToList();
            if (!string.IsNullOrWhiteSpace(txtProcess.Text)) b.ProcessName = txtProcess.Text.Trim();
        }
        private void OnSaveEdited(object sender, RoutedEventArgs e)
        {
            ForceCommitGridEdits();
            if (_editing == null) { txtStatus.Text = "Nothing to save"; return; }
            
            // FIX: Validate bookmark before saving to catch common robustness issues
            var validationErrors = ValidateBookmark(_editing);
            if (validationErrors.Count > 0)
            {
                txtStatus.Text = $"Validation failed: {string.Join("; ", validationErrors)}";
                return;
            }
            
            SaveEditorInto(_editing);
            
            // Simplified: All bookmarks saved to Bookmarks list (no KnownControl SaveMapping)
            if (FindName("cmbKnown") is System.Windows.Controls.ComboBox combo && 
                combo.SelectedItem is BookmarkItem item)
            {
                var store = UiBookmarks.Load();
                var existing = store.Bookmarks.FirstOrDefault(b => 
                    string.Equals(b.Name, item.Name, StringComparison.OrdinalIgnoreCase));
                
                if (existing != null)
                {
                    existing.ProcessName = _editing.ProcessName;
                    existing.Method = _editing.Method;
                    existing.DirectAutomationId = _editing.DirectAutomationId;
                    existing.CrawlFromRoot = _editing.CrawlFromRoot;
                    existing.Chain = _editing.Chain.ToList();
                }
                else
                {
                    existing = new UiBookmarks.Bookmark
                    {
                        Name = item.Name,
                        ProcessName = _editing.ProcessName,
                        Method = _editing.Method,
                        DirectAutomationId = _editing.DirectAutomationId,
                        CrawlFromRoot = _editing.CrawlFromRoot,
                        Chain = _editing.Chain.ToList()
                    };
                    store.Bookmarks.Add(existing);
                }
                
                UiBookmarks.Save(store);
                txtStatus.Text = $"Saved bookmark '{item.Name}'";
                LoadBookmarksIntoComboBox();
            }
        }
        
        // FIX: Validate bookmark to catch common robustness issues before saving
        private List<string> ValidateBookmark(UiBookmarks.Bookmark b)
        {
            var errors = new List<string>();
            
            // Check for empty process name
            if (string.IsNullOrWhiteSpace(b.ProcessName))
                errors.Add("Process name is empty");
            
            // Check for empty chain
            if (b.Chain == null || b.Chain.Count == 0)
                errors.Add("Chain is empty");
            
            // Check first node has at least one identifying attribute (changed from 2 to 1)
            var firstNode = b.Chain?.FirstOrDefault();
            if (firstNode != null && firstNode.Include)
            {
                int attrCount = 0;
                if (firstNode.UseName && !string.IsNullOrWhiteSpace(firstNode.Name)) attrCount++;
                if (firstNode.UseClassName && !string.IsNullOrWhiteSpace(firstNode.ClassName)) attrCount++;
                if (firstNode.UseAutomationId && !string.IsNullOrWhiteSpace(firstNode.AutomationId)) attrCount++;
                if (firstNode.UseControlTypeId && firstNode.ControlTypeId.HasValue) attrCount++;
                
                if (attrCount < 1)
                    errors.Add("First node has insufficient attributes (need at least 1 enabled)");
            }
            
            // Check for nodes with UseIndex=true and IndexAmongMatches=0 (potential ambiguity)
            for (int i = 1; i < (b.Chain?.Count ?? 0); i++)
            {
                var node = b.Chain[i];
                if (node.Include && node.UseIndex && node.IndexAmongMatches == 0)
                {
                    // This is okay, but warn if it's the only enabled attribute
                    int attrCount = 0;
                    if (node.UseName && !string.IsNullOrWhiteSpace(node.Name)) attrCount++;
                    if (node.UseClassName && !string.IsNullOrWhiteSpace(node.ClassName)) attrCount++;
                    if (node.UseAutomationId && !string.IsNullOrWhiteSpace(node.AutomationId)) attrCount++;
                    if (node.UseControlTypeId && node.ControlTypeId.HasValue) attrCount++;
                    
                    if (attrCount == 0)
                        errors.Add($"Node {i} relies only on index=0 (consider adding more attributes)");
                }
            }
            
            return errors;
        }

        // ------------------------------ Capture under mouse ------------------------------
        private async void OnPick(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtDelay.Text?.Trim(), out var delay)) delay = 600;
            txtStatus.Text = $"Pick arming... move mouse to target ({delay}ms)";
            await Task.Delay(delay);
            GetCursorPos(out var pt);
            var (b, procName, msg) = CaptureUnderMouse(preferAutomationId: false);
            txtStatus.Text = msg;
            if (!string.IsNullOrWhiteSpace(procName)) txtProcess.Text = procName;
            if (b == null) return;
            Tag = b; b.ProcessName = string.IsNullOrWhiteSpace(procName) ? b.ProcessName : procName;
            ShowBookmarkDetails(b, "Captured chain");
            foreach (var n in b.Chain) n.UseIndex = false; // default: disable index after capture
            GridChain.ItemsSource = null; GridChain.ItemsSource = b.Chain;
            try { if (FindName("txtPickedPoint") is System.Windows.Controls.TextBox tb) tb.Text = $"{pt.X},{pt.Y}"; } catch { }
        }

        // Pick web browser element (optimized for web stability, no auto-save)
        private async void OnPickWeb(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtDelay.Text?.Trim(), out var delay)) delay = 1500;
            txtStatus.Text = $"Pick Web arming... move mouse to web browser element ({delay}ms)";
            await Task.Delay(delay);
            GetCursorPos(out var pt);
            
            // Capture element and window information
            var (b, procName, msg) = CaptureUnderMouse(preferAutomationId: true);
            
            txtStatus.Text = msg;
            if (!string.IsNullOrWhiteSpace(procName)) txtProcess.Text = procName;
            if (b == null) return;

            // Set bookmark metadata
            b.ProcessName = string.IsNullOrWhiteSpace(procName) ? "browser" : procName;
            
            // FIX: Optimize web bookmarks for stability
            // 1. Disable Name matching for browser window nodes (titles change with tabs)
            // 2. Keep ClassName and ControlTypeId for structural matching
            // 3. Enable AutomationId when available (most stable for web elements)
            // 4. Change to Descendants scope for faster search
            
            for (int i = 0; i < b.Chain.Count; i++)
            {
                var node = b.Chain[i];
                
                // Browser window nodes (first 2-3 levels) - disable Name, keep structure
                if (i < 3)
                {
                    node.UseName = false; // Window title changes with tabs
                    node.UseClassName = !string.IsNullOrEmpty(node.ClassName); // Keep class
                    node.UseControlTypeId = node.ControlTypeId.HasValue; // Keep control type
                    node.UseAutomationId = false; // Top-level windows don't have stable AutomationId
                    node.UseIndex = false; // Disable index for stability
                }
                // Web content nodes (deeper levels) - prioritize AutomationId
                else
                {
                    node.UseName = false; // Web content names can be dynamic
                    node.UseClassName = !string.IsNullOrEmpty(node.ClassName);
                    node.UseControlTypeId = node.ControlTypeId.HasValue;
                    node.UseAutomationId = !string.IsNullOrEmpty(node.AutomationId); // Best for web elements
                    node.UseIndex = false; // Disable index unless needed
                }
                
                // Always use Descendants scope for web elements (faster search)
                if (i > 0)
                {
                    node.Scope = UiBookmarks.SearchScope.Descendants;
                }
            }

            // Display captured bookmark (DO NOT auto-save - user must select bookmark and click Save)
            Tag = b;
            ShowBookmarkDetails(b, "Captured web element (optimized for stability)");
            GridChain.ItemsSource = null; GridChain.ItemsSource = b.Chain;
            
            txtStatus.Text = $"Captured web element from '{procName}' (optimized for web stability). Select bookmark from dropdown and click Save to map.";
            try { if (FindName("txtPickedPoint") is System.Windows.Controls.TextBox tb) tb.Text = $"{pt.X},{pt.Y}"; } catch { }
        }

        // Prompt user for bookmark name with window title as context
        private string? PromptForBookmarkName(string windowTitle)
        {
            var dialog = new System.Windows.Window
            {
                Title = "Save Web Element Bookmark",
                Width = 500,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")!),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D0D0D0")!),
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow
            };

            var grid = new WpfGrid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var lblContext = new TextBlock
            {
                Text = $"Browser Window: {windowTitle}",
                Margin = new Thickness(0, 0, 0, 15),
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap
            };
            WpfGrid.SetRow(lblContext, 0);
            grid.Children.Add(lblContext);

            var lblPrompt = new TextBlock
            {
                Text = "Enter a name for this web element bookmark:",
                Margin = new Thickness(0, 0, 0, 10),
                VerticalAlignment = VerticalAlignment.Center
            };
            WpfGrid.SetRow(lblPrompt, 1);
            grid.Children.Add(lblPrompt);

            var txtName = new System.Windows.Controls.TextBox
            {
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(6, 4, 6, 4),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D30")!),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D0D0D0")!),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3C3C3C")!),
                BorderThickness = new Thickness(1),
                CaretBrush = new SolidColorBrush(Colors.White)
            };
            WpfGrid.SetRow(txtName, 2);
            grid.Children.Add(txtName);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            WpfGrid.SetRow(buttonPanel, 3);

            var btnOk = new WpfButton
            {
                Content = "Save",
                Width = 80,
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(10, 5, 10, 5),
                IsDefault = true
            };
            btnOk.Click += (s, e) => { dialog.DialogResult = true; dialog.Close(); };
            buttonPanel.Children.Add(btnOk);

            var btnCancel = new WpfButton
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

            // Focus textbox when dialog opens
            dialog.Loaded += (s, e) => txtName.Focus();

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

        private (UiBookmarks.Bookmark? bookmark, string? procName, string message) CaptureUnderMouse(bool preferAutomationId)
        {
            try
            {
                GetCursorPos(out var pt); var p = new System.Windows.Point(pt.X, pt.Y);
                var el = SWA.AutomationElement.FromPoint(p); if (el == null) return (null, null, "No UIA element under mouse");
                int pid = 0; try { pid = el.Current.ProcessId; } catch { }
                string procName = string.Empty; try { if (pid > 0) procName = System.Diagnostics.Process.GetProcessById(pid).ProcessName; } catch { }
                var walker = SWA.TreeWalker.ControlViewWalker;
                var win = el; SWA.AutomationElement? last = null; int safety = 0;
                while (win != null && safety++ < 50) { last = win; win = SafeParent(walker, win); }
                var top = last ?? el;
                var chain = new List<SWA.AutomationElement>(); var curEl = el; safety = 0;
                while (curEl != null && curEl != top && safety++ < 50) { chain.Add(curEl); curEl = SafeParent(walker, curEl); }
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
                            for (int i = 0; i < siblings.Length; i++) if (SWA.Automation.Compare(siblings[i], nodeEl)) { index = i; break; }
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
                var cls = classNameSafe(el) ?? "?"; var ctid = controlTypeIdSafe(el);
                return (b, procName, $"Captured element: {cls} ({ctid}) in {procName}");
                static SWA.AutomationElement? SafeParent(SWA.TreeWalker w, SWA.AutomationElement e) { try { return w.GetParent(e); } catch { return null; } }
                static SWA.AutomationElement[] SafeChildren(SWA.AutomationElement e) { try { return e.FindAll(SWA.TreeScope.Children, SWA.Condition.TrueCondition).Cast<SWA.AutomationElement>().ToArray(); } catch { return Array.Empty<SWA.AutomationElement>(); } }
                static T? Try<T>(Func<T?> f) { try { return f(); } catch { return default; } }
                static string? classNameSafe(SWA.AutomationElement e) { try { return e.Current.ClassName; } catch { return null; } }
                static int? controlTypeIdSafe(SWA.AutomationElement e) { try { return e.Current.ControlType?.Id; } catch { return null; } }
            }
            catch (Exception ex) { return (null, null, $"Pick error: {ex.Message}"); }
        }

        // ------------------------------ Validation & diagnostics ------------------------------
        private void OnValidateChain(object sender, RoutedEventArgs e)
        {
            ForceCommitGridEdits();
            if (!BuildBookmarkFromUi(out var copy)) return;
            var sw = Stopwatch.StartNew();
            var (_, el, trace) = UiBookmarks.TryResolveWithTrace(copy); sw.Stop(); _lastResolved = el;
            
            // FIX: Parse trace to extract per-step timing information
            var stepTimings = ParseStepTimingsFromTrace(trace);
            var diag = BuildChainDiagnosticWithTimings(copy, stepTimings);
            
            // FIX: Always show last 100 lines of trace for detailed timing analysis
            var truncatedTrace = TruncateToLastLines(trace, 100);
            
            if (el == null)
            { txtStatus.Text = diag + $"Validate: not found ({sw.ElapsedMilliseconds} ms)\r\n" + truncatedTrace; return; }
            var r = el.BoundingRectangle;
            _overlay.ShowForRect(new System.Drawing.Rectangle((int)r.Left, (int)r.Top, (int)r.Width, (int)r.Height));
            txtStatus.Text = diag + $"Validate: found and highlighted ({sw.ElapsedMilliseconds} ms)\r\n\r\n" + truncatedTrace;
        }
        
        // FIX: Truncate trace to last N lines to keep output manageable
        private string TruncateToLastLines(string text, int maxLines)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= maxLines) return text;
            
            var startIndex = lines.Length - maxLines;
            var truncatedLines = lines.Skip(startIndex).ToArray();
            return $"... (showing last {maxLines} of {lines.Length} lines)\r\n" + string.Join("\r\n", truncatedLines);
        }
        
        // FIX: Parse trace output to extract per-step timing
        private Dictionary<int, long> ParseStepTimingsFromTrace(string trace)
        {
            var timings = new Dictionary<int, long>();
            if (string.IsNullOrEmpty(trace)) return timings;
            
            // Look for patterns like "Step 0: ... (12 ms)" or "Step 1: Completed (45 ms)"
            var lines = trace.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                // Match "Step N: ... (XXX ms)"
                var match = System.Text.RegularExpressions.Regex.Match(line, @"Step (\d+):.+\((\d+) ms\)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var stepIdx) && long.TryParse(match.Groups[2].Value, out var ms))
                {
                    // Store the latest timing for each step (in case multiple lines reference same step)
                    timings[stepIdx] = ms;
                }
            }
            return timings;
        }
        
        private string BuildChainDiagnosticWithTimings(UiBookmarks.Bookmark b, Dictionary<int, long> stepTimings)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Idx Inc Nm Cls Ctl Auto Idx Scope | Name / Class / Ctrl / AutoId | Time");
            
            for (int i = 0; i < b.Chain.Count; i++)
            {
                var n = b.Chain[i];
                var timing = stepTimings.ContainsKey(i) ? $"{stepTimings[i]} ms" : "-";
                sb.AppendFormat("{0,2}  {1}   {2}{3}{4}{5}{6}  {7} | {8} / {9} / {10} / {11} | {12}\r\n",
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
                    n.AutomationId ?? "",
                    timing);
            }
            return sb.ToString();
        }

        // ------------------------------ Highlight / quick-map helpers ------------------------------
        private void HighlightBookmark(UiBookmarks.Bookmark b)
        {
            var (hwnd, el) = UiBookmarks.TryResolveBookmark(b);
            if (el == null)
            {
                try { var (_, _, trace) = UiBookmarks.TryResolveWithTrace(b); Debug.WriteLine(trace); } catch { }
                txtStatus.Text += " | Resolve failed"; return;
            }
            var r = el.BoundingRectangle;
            _overlay.ShowForRect(new System.Drawing.Rectangle((int)r.Left, (int)r.Top, (int)r.Width, (int)r.Height));
        }

        // ------------------------------ Chain grid manipulation commands ------------------------------
        private void OnMoveUp(object sender, RoutedEventArgs e)
        {
            var items = GridChain.ItemsSource as IList<UiBookmarks.Node> ?? GridChain.Items.OfType<UiBookmarks.Node>().ToList();
            var sel = GridChain.SelectedItem as UiBookmarks.Node; if (sel == null) return; int idx = items.IndexOf(sel);
            if (idx > 0) { items.RemoveAt(idx); items.Insert(idx - 1, sel); GridChain.ItemsSource = null; GridChain.ItemsSource = items; GridChain.SelectedItem = sel; }
        }
        private void OnMoveDown(object sender, RoutedEventArgs e)
        {
            var items = GridChain.ItemsSource as IList<UiBookmarks.Node> ?? GridChain.Items.OfType<UiBookmarks.Node>().ToList();
            var sel = GridChain.SelectedItem as UiBookmarks.Node; if (sel == null) return; int idx = items.IndexOf(sel);
            if (idx >= 0 && idx < items.Count - 1) { items.RemoveAt(idx); items.Insert(idx + 1, sel); GridChain.ItemsSource = null; GridChain.ItemsSource = items; GridChain.SelectedItem = sel; }
        }
        private void OnInsertAbove(object sender, RoutedEventArgs e)
        {
            var items = GridChain.ItemsSource as IList<UiBookmarks.Node> ?? GridChain.Items.OfType<UiBookmarks.Node>().ToList();
            var sel = GridChain.SelectedItem as UiBookmarks.Node; int idx = sel != null ? items.IndexOf(sel) : items.Count;
            var n = new UiBookmarks.Node { Include = true }; items.Insert(Math.Max(0, idx), n);
            GridChain.ItemsSource = null; GridChain.ItemsSource = items; GridChain.SelectedItem = n;
        }
        private void OnDeleteNode(object sender, RoutedEventArgs e)
        {
            var items = GridChain.ItemsSource as IList<UiBookmarks.Node> ?? GridChain.Items.OfType<UiBookmarks.Node>().ToList();
            var sel = GridChain.SelectedItem as UiBookmarks.Node; if (sel == null) return; items.Remove(sel);
            GridChain.ItemsSource = null; GridChain.ItemsSource = items;
        }

        // ------------------------------ Data commit helpers ------------------------------
        private void OnTemplateCheckBoxClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.CheckBox cb)
                {
                    var cell = FindParent<System.Windows.Controls.DataGridCell>(cb);
                    var grid = FindParent<System.Windows.Controls.DataGrid>(cell) ?? gridChain;
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
                    var cell = FindParent<System.Windows.Controls.DataGridCell>(combo);
                    var grid = FindParent<System.Windows.Controls.DataGrid>(cell) ?? gridChain;
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

        // ------------------------------ Build bookmark from UI state ------------------------------
        private bool BuildBookmarkFromUi(out UiBookmarks.Bookmark copy)
        {
            copy = new UiBookmarks.Bookmark();
            if (_editing == null) { txtStatus.Text = "No chain to validate"; return false; }
            try
            {
                gridChain.CommitEdit(DataGridEditingUnit.Cell, true);
                gridChain.CommitEdit(DataGridEditingUnit.Row, true);
            }
            catch { }
            var liveNodes = gridChain.Items.OfType<UiBookmarks.Node>().ToList();
            _editing.Chain = liveNodes; // persist to editing instance
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
            _lastResolved = null; return true;
        }
        private void ShowBookmarkDetails(UiBookmarks.Bookmark b, string header)
        { LoadEditor(b); txtStatus.Text = header; }

        // ------------------------------ Simple helpers ------------------------------
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
                var vp = cell.Patterns.Value.PatternOrDefault; if (vp != null && vp.Value.TryGetValue(out var pv) && !string.IsNullOrWhiteSpace(pv)) return pv;
            }
            catch { }
            var name = GetElementName(cell); if (!string.IsNullOrWhiteSpace(name)) return name;
            try { var l = cell.Patterns.LegacyIAccessible.PatternOrDefault?.Name; if (!string.IsNullOrWhiteSpace(l)) return l; } catch { }
            return string.Empty;
        }

        // Exposed to other partial for row extraction
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
                                    foreach (var g in hc.FindAllChildren()) { t = ReadCellText(g); if (!string.IsNullOrWhiteSpace(t)) break; }
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
                            if (string.IsNullOrWhiteSpace(txt)) foreach (var g in hi.FindAllChildren()) { txt = ReadCellText(g); if (!string.IsNullOrWhiteSpace(txt)) break; }
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
                                foreach (var gc in c.FindAllChildren()) { var t = ReadCellText(gc).Trim(); if (!string.IsNullOrEmpty(t)) { cellText = t; break; } }
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
                                var t = ReadCellText(d).Trim(); values.Add(t); if (values.Count >= 20) break;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
            return values;
        }

        // Date parsing used by procedure op
        private static bool TryParseYmdOrYmdHms(string s, out DateTime dt)
        {
            dt = default;
            if (DateTime.TryParseExact(s, new[] { "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss" }, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeLocal, out var parsed)) { dt = parsed; return true; }
            return false;
        }

        // Chain grid navigation use FindParent helper (defined in another partial if needed)
        
        /// <summary>
        /// Duplicate the UI map from the current bookmark to another bookmark
        /// </summary>
        private void OnDuplicateBookmark(object sender, RoutedEventArgs e)
        {
            var cmbSource = (System.Windows.Controls.ComboBox?)FindName("cmbKnown");
            
            // Get current source bookmark
            if (cmbSource?.SelectedItem is not BookmarkItem sourceItem)
            {
                txtStatus.Text = "Select a source bookmark first";
                return;
            }
            
            // Commit any pending edits to the chain grid
            ForceCommitGridEdits();
            
            // Get current bookmark data
            var store = UiBookmarks.Load();
            var sourceBookmark = store.Bookmarks.FirstOrDefault(b => 
                string.Equals(b.Name, sourceItem.Name, StringComparison.OrdinalIgnoreCase));
            
            if (sourceBookmark == null)
            {
                txtStatus.Text = $"Source bookmark '{sourceItem.Name}' not found";
                return;
            }
            
            if (sourceBookmark.Chain == null || sourceBookmark.Chain.Count == 0)
            {
                txtStatus.Text = "No UI map to duplicate (source bookmark chain is empty)";
                return;
            }
            
            // Show selection dialog for target bookmark
            var targetName = ShowBookmarkSelectionDialog(sourceItem.Name);
            
            if (string.IsNullOrWhiteSpace(targetName))
            {
                txtStatus.Text = "Duplication cancelled";
                return;
            }
            
            try
            {
                // Load target bookmark
                var targetBookmark = store.Bookmarks.FirstOrDefault(b => 
                    string.Equals(b.Name, targetName, StringComparison.OrdinalIgnoreCase));
                
                if (targetBookmark == null)
                {
                    txtStatus.Text = $"Target bookmark '{targetName}' not found";
                    return;
                }
                
                // Check if target already has a chain
                if (targetBookmark.Chain != null && targetBookmark.Chain.Count > 0)
                {
                    var result = System.Windows.MessageBox.Show(
                        $"Target bookmark '{targetName}' already has {targetBookmark.Chain.Count} node(s) in its UI map.\n\nDo you want to replace them?",
                        "Confirm Replace",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Warning);
                    
                    if (result != System.Windows.MessageBoxResult.Yes)
                    {
                        txtStatus.Text = "Duplication cancelled";
                        return;
                    }
                }
                
                // Deep copy the chain (create new Node instances to avoid reference issues)
                targetBookmark.Chain = sourceBookmark.Chain.Select(n => new UiBookmarks.Node
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
                }).ToList();
                
                // Also copy other settings
                targetBookmark.ProcessName = sourceBookmark.ProcessName;
                targetBookmark.Method = sourceBookmark.Method;
                targetBookmark.DirectAutomationId = sourceBookmark.DirectAutomationId;
                targetBookmark.CrawlFromRoot = sourceBookmark.CrawlFromRoot;
                
                // Save to storage
                UiBookmarks.Save(store);
                
                txtStatus.Text = $"Successfully duplicated UI map ({sourceBookmark.Chain.Count} node(s)) from '{sourceItem.Name}' to '{targetName}'";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"Error duplicating bookmark: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[AutomationWindow] Duplicate bookmark error: {ex}");
            }
        }
        
        /// <summary>
        /// Show a selection dialog to choose target bookmark for duplication
        /// </summary>
        /// <param name="sourceBookmarkName">Name of the source bookmark to exclude from selection</param>
        /// <returns>Selected target bookmark name, or null if cancelled</returns>
        private string? ShowBookmarkSelectionDialog(string sourceBookmarkName)
        {
            // Get all available bookmarks excluding the source
            var store = UiBookmarks.Load();
            var availableBookmarks = store.Bookmarks
                .Where(b => !string.Equals(b.Name, sourceBookmarkName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(b => b.Name)
                .ToList();
            
            if (availableBookmarks.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "No other bookmarks available.\n\nCreate a new bookmark first using '+' button.",
                    "No Target Bookmarks",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return null;
            }
            
            // Create selection dialog
            var dialog = new System.Windows.Window
            {
                Title = "Select Target Bookmark",
                Width = 500,
                Height = 400,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)),
                ResizeMode = System.Windows.ResizeMode.NoResize
            };
            
            var grid = new System.Windows.Controls.Grid { Margin = new System.Windows.Thickness(20) };
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            
            // Header
            var header = new System.Windows.Controls.TextBlock
            {
                Text = $"Select target bookmark to copy UI map from '{sourceBookmarkName}':",
                Foreground = new SolidColorBrush(System.Windows.Media.Colors.White),
                TextWrapping = System.Windows.TextWrapping.Wrap,
                Margin = new System.Windows.Thickness(0, 0, 0, 15)
            };
            System.Windows.Controls.Grid.SetRow(header, 0);
            grid.Children.Add(header);
            
            // ListBox with bookmark names
            var listBox = new System.Windows.Controls.ListBox
            {
                ItemsSource = availableBookmarks.Select(b => b.Name).ToList(),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 48)),
                Foreground = new SolidColorBrush(System.Windows.Media.Colors.White),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60)),
                Margin = new System.Windows.Thickness(0, 0, 0, 15)
            };
            System.Windows.Controls.Grid.SetRow(listBox, 1);
            grid.Children.Add(listBox);
            
            // Buttons
            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };
            System.Windows.Controls.Grid.SetRow(buttonPanel, 2);
            
            var btnOk = new System.Windows.Controls.Button
            {
                Content = "OK",
                Width = 80,
                Margin = new System.Windows.Thickness(0, 0, 10, 0),
                Padding = new System.Windows.Thickness(10, 5, 10, 5),
                IsDefault = true
            };
            btnOk.Click += (s, e) =>
            {
                if (listBox.SelectedItem != null)
                {
                    dialog.DialogResult = true;
                    dialog.Close();
                }
            };
            buttonPanel.Children.Add(btnOk);
            
            var btnCancel = new System.Windows.Controls.Button
            {
                Content = "Cancel",
                Width = 80,
                Padding = new System.Windows.Thickness(10, 5, 10, 5),
                IsCancel = true
            };
            btnCancel.Click += (s, e) =>
            {
                dialog.DialogResult = false;
                dialog.Close();
            };
            buttonPanel.Children.Add(btnCancel);
            
            grid.Children.Add(buttonPanel);
            dialog.Content = grid;
            
            // Handle double-click
            listBox.MouseDoubleClick += (s, e) =>
            {
                if (listBox.SelectedItem != null)
                {
                    dialog.DialogResult = true;
                    dialog.Close();
                }
            };
            
            var result = dialog.ShowDialog();
            
            // Return the selected bookmark name
            if (result == true && listBox.SelectedItem is string selectedName)
            {
                return selectedName;
            }
            return null;
        }
    }
}
