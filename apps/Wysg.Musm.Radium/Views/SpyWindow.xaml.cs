using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wysg.Musm.Radium.Services;
using SWA = System.Windows.Automation;

namespace Wysg.Musm.Radium.Views
{
    public partial class SpyWindow : System.Windows.Window
    {
        // P/Invoke and overlay fields
        [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT lpPoint);
        [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(POINT Point);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X; public int Y; }
        private readonly HighlightOverlay _overlay = new HighlightOverlay();

        private UiBookmarks.Bookmark? _editing;

        private System.Windows.Controls.ComboBox CmbMethod => (System.Windows.Controls.ComboBox)FindName("cmbMethod");
        private System.Windows.Controls.TextBox TxtDirectId => (System.Windows.Controls.TextBox)FindName("txtDirectId");
        private System.Windows.Controls.DataGrid GridChain => (System.Windows.Controls.DataGrid)FindName("gridChain");

        public SpyWindow()
        {
            InitializeComponent();
            LoadBookmarks();
            this.PreviewMouseDown += OnPreviewMouseDownForQuickMap;
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
            if (((System.Windows.Controls.ComboBox)FindName("cmbKnown")).SelectedItem is not System.Windows.Controls.ComboBoxItem item || item.Tag is not string keyStr)
                return;
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
                    ClassName = n.ClassName,
                    ControlTypeId = n.ControlTypeId,
                    AutomationId = n.AutomationId,
                    IndexAmongMatches = n.IndexAmongMatches
                }).ToList()
            };
            GridChain.ItemsSource = _editing.Chain;
            TxtDirectId.Text = _editing.DirectAutomationId ?? string.Empty;
            CmbMethod.SelectedIndex = _editing.Method == UiBookmarks.MapMethod.Chain ? 0 : 1;
        }

        private void SaveEditorInto(UiBookmarks.Bookmark b)
        {
            if (_editing == null) return;
            b.Method = CmbMethod.SelectedIndex == 0 ? UiBookmarks.MapMethod.Chain : UiBookmarks.MapMethod.AutomationIdOnly;
            b.DirectAutomationId = string.IsNullOrWhiteSpace(TxtDirectId.Text) ? null : TxtDirectId.Text.Trim();
            b.Chain = GridChain.Items.OfType<UiBookmarks.Node>().ToList();
        }

        private class TreeNode
        {
            public string? Name { get; set; }
            public string? ClassName { get; set; }
            public int? ControlTypeId { get; set; }
            public string? AutomationId { get; set; }
            public System.Collections.Generic.List<TreeNode> Children { get; } = new();
        }

        private void ShowAncestryTree(UiBookmarks.Bookmark b)
        {
            var root = new TreeNode { Name = "WindowRoot", ClassName = null, ControlTypeId = null, AutomationId = null };
            var cur = root;
            foreach (var n in b.Chain)
            {
                var child = new TreeNode { Name = n.Name, ClassName = n.ClassName, ControlTypeId = n.ControlTypeId, AutomationId = n.AutomationId };
                cur.Children.Add(child);
                cur = child;
            }
            tvAncestry.ItemsSource = new[] { root };
        }

        private void ShowBookmarkDetails(UiBookmarks.Bookmark b, string header)
        {
            LoadEditor(b);
            ShowAncestryTree(b);
            txtStatus.Text = header;
        }

        private async void OnPick(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtDelay.Text?.Trim(), out var delay)) delay = 600;
            txtStatus.Text = $"Pick arming... move mouse to target ({delay}ms)";
            await Task.Delay(delay);
            var (b, procName, msg) = CaptureUnderMouse(preferAutomationId: false);
            txtStatus.Text = msg;
            if (b == null) return;
            this.Tag = b;
            ShowBookmarkDetails(b, "Captured chain");
            HighlightBookmark(b);
        }

        private static string? Safe(Func<string> f) { try { return f(); } catch { return null; } }
        private static int? SafeInt(Func<int> f) { try { return f(); } catch { return null; } }

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
                while (win != null) { last = win; win = walker.GetParent(win); }
                var top = last ?? el;

                var chain = new List<SWA.AutomationElement>();
                var cur = el;
                while (cur != null && cur != top)
                {
                    chain.Add(cur);
                    cur = walker.GetParent(cur);
                }
                chain.Reverse();

                var b = new UiBookmarks.Bookmark { Name = string.Empty, ProcessName = string.IsNullOrWhiteSpace(procName) ? "Unknown" : procName };
                SWA.AutomationElement scope = top;
                foreach (var nodeEl in chain)
                {
                    string? name = null, className = null, autoId = null; int? ctId = null;
                    try { name = nodeEl.Current.Name; } catch { }
                    try { className = nodeEl.Current.ClassName; } catch { }
                    try { autoId = nodeEl.Current.AutomationId; } catch { }
                    try { ctId = nodeEl.Current.ControlType?.Id; } catch { }

                    var conds = new List<SWA.Condition>();
                    if (preferAutomationId && !string.IsNullOrEmpty(autoId)) conds.Add(new SWA.PropertyCondition(SWA.AutomationElement.AutomationIdProperty, autoId));
                    if (!string.IsNullOrEmpty(className)) conds.Add(new SWA.PropertyCondition(SWA.AutomationElement.ClassNameProperty, className));
                    if (ctId.HasValue) conds.Add(new SWA.PropertyCondition(SWA.AutomationElement.ControlTypeProperty, nodeEl.Current.ControlType));
                    if (!string.IsNullOrEmpty(name)) conds.Add(new SWA.PropertyCondition(SWA.AutomationElement.NameProperty, name));

                    SWA.Condition q = conds.Count switch { 0 => SWA.Condition.TrueCondition, 1 => conds[0], _ => new SWA.AndCondition(conds.ToArray()) };
                    var matches = scope.FindAll(SWA.TreeScope.Subtree, q);
                    int index = 0;
                    for (int i = 0; i < matches.Count; i++) if (SWA.Automation.Compare(matches[i], nodeEl)) { index = i; break; }

                    b.Chain.Add(new UiBookmarks.Node
                    {
                        Name = name,
                        ClassName = className,
                        AutomationId = preferAutomationId ? autoId : null,
                        ControlTypeId = ctId,
                        IndexAmongMatches = index,
                        Include = true,
                        UseName = !string.IsNullOrEmpty(name),
                        UseClassName = !string.IsNullOrEmpty(className),
                        UseControlTypeId = ctId.HasValue,
                        UseAutomationId = preferAutomationId && !string.IsNullOrEmpty(autoId),
                        UseIndex = true
                    });

                    scope = matches.Count > 0 ? matches[index] : nodeEl;
                }

                var cls = classNameSafe(el) ?? "?";
                var ctid = controlTypeIdSafe(el);
                return (b, procName, $"Captured element: {cls} ({ctid}) in {procName}");

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
            if (el == null) { txtStatus.Text += " | Resolve failed"; return; }
            var r = el.BoundingRectangle;
            var rect = new System.Drawing.Rectangle((int)r.Left, (int)r.Top, (int)r.Width, (int)r.Height);
            _overlay.ShowForRect(rect);
        }

        private void OnMapSelected(object sender, RoutedEventArgs e)
        {
            if (cmbKnown.SelectedItem is not System.Windows.Controls.ComboBoxItem item || item.Tag is not string keyStr)
            { txtStatus.Text = "Select a known control"; return; }

            // Capture current element
            var (b, procName, msg) = CaptureUnderMouse(preferAutomationId: true);
            txtStatus.Text = msg;
            if (b == null) return;

            // Apply UI mapping method choices
            var method = CmbMethod.SelectedIndex == 0 ? UiBookmarks.MapMethod.Chain : UiBookmarks.MapMethod.AutomationIdOnly;
            b.Method = method;
            if (method == UiBookmarks.MapMethod.AutomationIdOnly)
            {
                var directId = TxtDirectId.Text?.Trim();
                if (string.IsNullOrWhiteSpace(directId))
                {
                    // Try to take from last node
                    directId = b.Chain.LastOrDefault()?.AutomationId;
                }
                b.DirectAutomationId = string.IsNullOrWhiteSpace(directId) ? null : directId;
            }

            if (!Enum.TryParse<UiBookmarks.KnownControl>(keyStr, out var key))
            { txtStatus.Text = "Invalid known control"; return; }
            UiBookmarks.SaveMapping(key, b);
            ShowBookmarkDetails(b, $"Mapped {key}");
            HighlightBookmark(b);
            txtStatus.Text = $"Mapped {key}";
        }

        private void OnResolveSelected(object sender, RoutedEventArgs e)
        {
            if (cmbKnown.SelectedItem is not System.Windows.Controls.ComboBoxItem item || item.Tag is not string keyStr)
            { txtStatus.Text = "Select a known control"; return; }
            if (!Enum.TryParse<UiBookmarks.KnownControl>(keyStr, out var key))
            { txtStatus.Text = "Invalid known control"; return; }
            var mapping = UiBookmarks.GetMapping(key);
            if (mapping == null) { txtStatus.Text = "No mapping saved"; return; }
            ShowBookmarkDetails(mapping, $"Current mapping for {key}");
            var (hwnd, el) = UiBookmarks.Resolve(key);
            if (el == null) { txtStatus.Text += " | Resolve failed"; return; }
            var r = el.BoundingRectangle;
            var rect = new System.Drawing.Rectangle((int)r.Left, (int)r.Top, (int)r.Width, (int)r.Height);
            _overlay.ShowForRect(rect);
            txtStatus.Text = $"Resolved {key}";
        }

        private void OnValidateChain(object sender, RoutedEventArgs e)
        {
            if (_editing == null) { txtStatus.Text = "No chain to validate"; return; }
            var copy = new UiBookmarks.Bookmark
            {
                Name = _editing.Name,
                ProcessName = _editing.ProcessName,
                Method = CmbMethod.SelectedIndex == 0 ? UiBookmarks.MapMethod.Chain : UiBookmarks.MapMethod.AutomationIdOnly,
                DirectAutomationId = string.IsNullOrWhiteSpace(TxtDirectId.Text) ? null : TxtDirectId.Text.Trim(),
                Chain = GridChain.Items.OfType<UiBookmarks.Node>().ToList()
            };
            var sw = Stopwatch.StartNew();
            var (hwnd, el) = UiBookmarks.TryResolveBookmark(copy);
            sw.Stop();
            if (el == null) { txtStatus.Text = $"Validate: not found ({sw.ElapsedMilliseconds} ms)"; return; }
            var r = el.BoundingRectangle;
            var rect = new System.Drawing.Rectangle((int)r.Left, (int)r.Top, (int)r.Width, (int)r.Height);
            _overlay.ShowForRect(rect);
            txtStatus.Text = $"Validate: found and highlighted ({sw.ElapsedMilliseconds} ms)";
        }

        private void OnSaveEdited(object sender, RoutedEventArgs e)
        {
            if (cmbKnown.SelectedItem is not System.Windows.Controls.ComboBoxItem item || item.Tag is not string keyStr)
            { txtStatus.Text = "Select a known control"; return; }
            if (!Enum.TryParse<UiBookmarks.KnownControl>(keyStr, out var key))
            { txtStatus.Text = "Invalid known control"; return; }
            var mapping = UiBookmarks.GetMapping(key);
            if (mapping == null)
            {
                mapping = new UiBookmarks.Bookmark { Name = key.ToString(), ProcessName = _editing?.ProcessName ?? string.Empty };
            }
            SaveEditorInto(mapping);
            UiBookmarks.SaveMapping(key, mapping);
            txtStatus.Text = $"Saved mapping for {key}";
        }

        private void OnPreviewMouseDownForQuickMap(object sender, MouseButtonEventArgs e)
        {
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl)) return;
            if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift)) return;
            if (cmbKnown.SelectedItem is not System.Windows.Controls.ComboBoxItem item || item.Tag is not string keyStr)
            { txtStatus.Text = "Select a known control"; return; }
            var (b, procName, msg) = CaptureUnderMouse(preferAutomationId: true);
            txtStatus.Text = msg;
            if (b == null) return;
            if (!Enum.TryParse<UiBookmarks.KnownControl>(keyStr, out var key))
            { txtStatus.Text = "Invalid known control"; return; }

            // Respect UI method choices on quick map
            var method = CmbMethod.SelectedIndex == 0 ? UiBookmarks.MapMethod.Chain : UiBookmarks.MapMethod.AutomationIdOnly;
            b.Method = method;
            if (method == UiBookmarks.MapMethod.AutomationIdOnly)
            {
                var directId = TxtDirectId.Text?.Trim();
                if (string.IsNullOrWhiteSpace(directId)) directId = b.Chain.LastOrDefault()?.AutomationId;
                b.DirectAutomationId = string.IsNullOrWhiteSpace(directId) ? null : directId;
            }

            UiBookmarks.SaveMapping(key, b);
            LoadEditor(b);
            HighlightBookmark(b);
            txtStatus.Text = $"Quick-mapped {key}";
        }

        private void OnMoveUp(object sender, RoutedEventArgs e)
        {
            var items = GridChain.ItemsSource as IList<UiBookmarks.Node> ?? GridChain.Items.OfType<UiBookmarks.Node>().ToList();
            var sel = GridChain.SelectedItem as UiBookmarks.Node;
            if (sel == null) return;
            int idx = items.IndexOf(sel);
            if (idx > 0)
            {
                items.RemoveAt(idx);
                items.Insert(idx - 1, sel);
                GridChain.ItemsSource = null; GridChain.ItemsSource = items; GridChain.SelectedItem = sel;
            }
        }
        private void OnMoveDown(object sender, RoutedEventArgs e)
        {
            var items = GridChain.ItemsSource as IList<UiBookmarks.Node> ?? GridChain.Items.OfType<UiBookmarks.Node>().ToList();
            var sel = GridChain.SelectedItem as UiBookmarks.Node;
            if (sel == null) return;
            int idx = items.IndexOf(sel);
            if (idx >= 0 && idx < items.Count - 1)
            {
                items.RemoveAt(idx);
                items.Insert(idx + 1, sel);
                GridChain.ItemsSource = null; GridChain.ItemsSource = items; GridChain.SelectedItem = sel;
            }
        }
        private void OnInsertAbove(object sender, RoutedEventArgs e)
        {
            var items = GridChain.ItemsSource as IList<UiBookmarks.Node> ?? GridChain.Items.OfType<UiBookmarks.Node>().ToList();
            var sel = GridChain.SelectedItem as UiBookmarks.Node;
            int idx = sel != null ? items.IndexOf(sel) : items.Count;
            var n = new UiBookmarks.Node { Include = true };
            items.Insert(Math.Max(0, idx), n);
            GridChain.ItemsSource = null; GridChain.ItemsSource = items; GridChain.SelectedItem = n;
        }
        private void OnDeleteNode(object sender, RoutedEventArgs e)
        {
            var items = GridChain.ItemsSource as IList<UiBookmarks.Node> ?? GridChain.Items.OfType<UiBookmarks.Node>().ToList();
            var sel = GridChain.SelectedItem as UiBookmarks.Node;
            if (sel == null) return;
            items.Remove(sel);
            GridChain.ItemsSource = null; GridChain.ItemsSource = items;
        }

        private void OnAncestrySelected(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is not TreeNode n) { txtNodeProps.Text = string.Empty; return; }
            var sb = new StringBuilder();
            sb.AppendLine($"Name: {n.Name}");
            sb.AppendLine($"ClassName: {n.ClassName}");
            sb.AppendLine($"ControlTypeId: {n.ControlTypeId}");
            sb.AppendLine($"AutomationId: {n.AutomationId}");
            txtNodeProps.Text = sb.ToString();
        }
    }
}
