using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
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

        // P/Invoke and overlay fields
        [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT lpPoint);
        [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(POINT Point);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X; public int Y; }
        private readonly HighlightOverlay _overlay = new HighlightOverlay();

        private UiBookmarks.Bookmark? _editing;
        private AutomationElement? _lastResolved;

        private System.Windows.Controls.ComboBox CmbMethod => (System.Windows.Controls.ComboBox)FindName("cmbMethod");
        private System.Windows.Controls.TextBox TxtDirectId => (System.Windows.Controls.TextBox)FindName("txtDirectId");
        private System.Windows.Controls.DataGrid GridChain => (System.Windows.Controls.DataGrid)FindName("gridChain");

        // Helper methods moved up for availability
        private void EnsureResolved()
        {
            if (_lastResolved != null) return;
            if (!BuildBookmarkFromUi(out var copy)) return;
            var (_, el, _) = UiBookmarks.TryResolveWithTrace(copy);
            _lastResolved = el;
        }

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
            TxtDirectId.Text = _editing.DirectAutomationId ?? string.Empty;
            CmbMethod.SelectedIndex = _editing.Method == UiBookmarks.MapMethod.Chain ? 0 : 1;
            // reflect process name
            txtProcess.Text = _editing.ProcessName ?? string.Empty;
        }

        private void SaveEditorInto(UiBookmarks.Bookmark b)
        {
            if (_editing == null) return;
            b.Method = CmbMethod.SelectedIndex == 0 ? UiBookmarks.MapMethod.Chain : UiBookmarks.MapMethod.AutomationIdOnly;
            b.DirectAutomationId = string.IsNullOrWhiteSpace(TxtDirectId.Text) ? null : TxtDirectId.Text.Trim();
            b.Chain = GridChain.Items.OfType<UiBookmarks.Node>().ToList();
            if (!string.IsNullOrWhiteSpace(txtProcess.Text)) b.ProcessName = txtProcess.Text.Trim();
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
            if (!string.IsNullOrWhiteSpace(procName)) txtProcess.Text = procName; // reflect detected process
            if (b == null) return;
            this.Tag = b;
            // also update bookmark's process name
            b.ProcessName = string.IsNullOrWhiteSpace(procName) ? b.ProcessName : procName;
            ShowBookmarkDetails(b, "Captured chain");
            HighlightBookmark(b);
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
                while (win != null) { last = win; win = walker.GetParent(win); }
                var top = last ?? el;

                // collect chain using parent relationships only (fast)
                var chain = new System.Collections.Generic.List<SWA.AutomationElement>();
                var curEl = el;
                while (curEl != null && curEl != top)
                {
                    chain.Add(curEl);
                    curEl = walker.GetParent(curEl);
                }
                chain.Reverse();

                var b = new UiBookmarks.Bookmark { Name = string.Empty, ProcessName = string.IsNullOrWhiteSpace(procName) ? "Unknown" : procName };
                foreach (var nodeEl in chain)
                {
                    string? name = null, className = null, autoId = null; int? ctId = null;
                    try { name = nodeEl.Current.Name; } catch { }
                    try { className = nodeEl.Current.ClassName; } catch { }
                    try { autoId = nodeEl.Current.AutomationId; } catch { }
                    try { ctId = nodeEl.Current.ControlType?.Id; } catch { }

                    // determine index among siblings quickly (children only)
                    int index = 0;
                    try
                    {
                        var parent = walker.GetParent(nodeEl);
                        if (parent != null)
                        {
                            var siblings = parent.FindAll(SWA.TreeScope.Children, SWA.Condition.TrueCondition);
                            for (int i = 0; i < siblings.Count; i++)
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
                        // Default behavior requested: check AutomationId when present; do not check Index by default
                        UseAutomationId = !string.IsNullOrEmpty(autoId),
                        UseIndex = false,
                        Scope = UiBookmarks.SearchScope.Children
                    });
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
            if (!string.IsNullOrWhiteSpace(procName)) txtProcess.Text = procName;
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
            if (!BuildBookmarkFromUi(out var copy)) return;
            var sw = Stopwatch.StartNew();
            var (hwnd, el, trace) = UiBookmarks.TryResolveWithTrace(copy);
            sw.Stop();
            _lastResolved = el;
            if (el == null)
            {
                txtStatus.Text = $"Validate: not found ({sw.ElapsedMilliseconds} ms)\r\n" + trace;
                return;
            }
            var r = el.BoundingRectangle;
            var rect = new System.Drawing.Rectangle((int)r.Left, (int)r.Top, (int)r.Width, (int)r.Height);
            _overlay.ShowForRect(rect);
            txtStatus.Text = $"Validate: found and highlighted ({sw.ElapsedMilliseconds} ms)\r\n" + trace;
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
                using var automation = new UIA3Automation();
                var inv = _lastResolved.Patterns.Invoke.PatternOrDefault;
                if (inv != null)
                {
                    inv.Invoke();
                    txtStatus.Text = "Invoke: done";
                    return;
                }
                var toggle = _lastResolved.Patterns.Toggle.PatternOrDefault;
                if (toggle != null)
                {
                    toggle.Toggle();
                    txtStatus.Text = "Toggle: done";
                    return;
                }
                txtStatus.Text = "Invoke: pattern not supported";
            }
            catch (Exception ex)
            {
                txtStatus.Text = "Invoke error: " + ex.Message;
            }
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
            catch (Exception ex)
            {
                txtStatus.Text = "Get Text error: " + ex.Message;
            }
        }

        private bool BuildBookmarkFromUi(out UiBookmarks.Bookmark copy)
        {
            copy = new UiBookmarks.Bookmark();
            if (_editing == null) { txtStatus.Text = "No chain to validate"; return false; }

            try
            {
                GridChain.CommitEdit(DataGridEditingUnit.Cell, true);
                GridChain.CommitEdit(DataGridEditingUnit.Row, true);
                FocusManager.SetFocusedElement(this, this);
                GridChain.UpdateLayout();
                CollectionViewSource.GetDefaultView(GridChain.ItemsSource)?.Refresh();
            }
            catch { }

            copy = new UiBookmarks.Bookmark
            {
                Name = _editing.Name,
                ProcessName = _editing.ProcessName,
                Method = CmbMethod.SelectedIndex == 0 ? UiBookmarks.MapMethod.Chain : UiBookmarks.MapMethod.AutomationIdOnly,
                DirectAutomationId = string.IsNullOrWhiteSpace(TxtDirectId.Text) ? null : TxtDirectId.Text.Trim(),
                Chain = (_editing.Chain ?? new List<UiBookmarks.Node>()).Select(n => new UiBookmarks.Node
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
            if (!string.IsNullOrWhiteSpace(txtProcess.Text)) copy.ProcessName = txtProcess.Text.Trim();
            _lastResolved = null; // invalidate cache on new build
            return true;
        }

        private void OnSaveEdited(object sender, RoutedEventArgs e)
        {
            UiBookmarks.Bookmark toSave;
            if (_editing == null) { txtStatus.Text = "Nothing to save"; return; }

            try
            {
                GridChain.CommitEdit(DataGridEditingUnit.Cell, true);
                GridChain.CommitEdit(DataGridEditingUnit.Row, true);
            }
            catch { }

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

        private void OnPreviewMouseDownForQuickMap(object sender, MouseButtonEventArgs e)
        {
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl)) return;
            if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift)) return;
            if (cmbKnown.SelectedItem is not System.Windows.Controls.ComboBoxItem item || item.Tag is not string keyStr)
            { txtStatus.Text = "Select a known control"; return; }
            var (b, procName, msg) = CaptureUnderMouse(preferAutomationId: true);
            txtStatus.Text = msg;
            if (!string.IsNullOrWhiteSpace(procName)) txtProcess.Text = procName;
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

        private void OnGetSelectedRow(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureResolved();
                if (_lastResolved == null) { txtStatus.Text = "Row Data: not found"; return; }

                var list = _lastResolved;
                // Get selected item(s) using Selection or fallback
                var selection = list.Patterns.Selection.PatternOrDefault;
                var selected = selection?.Selection?.Value ?? Array.Empty<AutomationElement>();
                if (selected.Length == 0)
                {
                    selected = list.FindAllDescendants()
                        .Where(a =>
                        {
                            try { return a.Patterns.SelectionItem.IsSupported && a.Patterns.SelectionItem.PatternOrDefault?.IsSelected == true; }
                            catch { return false; }
                        })
                        .ToArray();
                }
                if (selected.Length == 0) { txtStatus.Text = "Row Data: no selection"; return; }
                var row = selected[0];

                // Read headers and cell values
                var headers = GetHeaderTexts(list);
                var cellValues = GetRowCellValues(row);

                if (headers.Count == 0 && cellValues.Count == 0)
                {
                    txtStatus.Text = "Row Data: empty";
                    return;
                }

                // Make sure we can display as pairs. If headers missing or fewer than values, synthesize names.
                if (headers.Count < cellValues.Count)
                {
                    for (int i = headers.Count; i < cellValues.Count; i++) headers.Add($"Col{i + 1}");
                }
                else if (headers.Count > cellValues.Count)
                {
                    for (int i = cellValues.Count; i < headers.Count; i++) cellValues.Add(string.Empty);
                }

                var pairs = new List<(string Header, string Value)>();
                int count = Math.Max(headers.Count, cellValues.Count);
                for (int i = 0; i < count; i++)
                {
                    var h = NormalizeHeader(i < headers.Count ? headers[i] : $"Col{i + 1}");
                    var v = i < cellValues.Count ? cellValues[i] : string.Empty;
                    pairs.Add((h, v));
                }

                // One-line pairs output: Header: Value | Header: Value ...
                var line = string.Join(" | ", pairs
                    .Where(p => !string.IsNullOrWhiteSpace(p.Header) || !string.IsNullOrWhiteSpace(p.Value))
                    .Select(p => string.IsNullOrWhiteSpace(p.Header) ? p.Value : ($"{p.Header}: {p.Value}"))
                );
                txtStatus.Text = string.IsNullOrWhiteSpace(line) ? "Row Data: empty" : line;
            }
            catch (Exception ex)
            {
                txtStatus.Text = "Row Data error: " + ex.Message;
            }
        }

        private static List<string> GetHeaderTexts(AutomationElement list)
        {
            var result = new List<string>();
            try
            {
                // 1) Heuristic: header is the first child of the list control
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
                                    foreach (var g in hc.FindAllChildren())
                                    {
                                        t = ReadCellText(g);
                                        if (!string.IsNullOrWhiteSpace(t)) break;
                                    }
                                }
                                if (!string.IsNullOrWhiteSpace(t)) result.Add(t.Trim());
                            }
                            catch { }
                        }
                        if (result.Count > 0) return result;
                    }
                }

                // 2) Fallback: search explicit Header control nearby
                var header = kids.FirstOrDefault(c =>
                {
                    try { return (int)c.ControlType == UIA_Header; } catch { return false; }
                });

                if (header == null)
                {
                    foreach (var ch in kids)
                    {
                        try
                        {
                            var h = ch.FindAllChildren().FirstOrDefault(cc => (int)cc.ControlType == UIA_Header);
                            if (h != null) { header = h; break; }
                        }
                        catch { }
                    }
                }

                if (header != null)
                {
                    foreach (var hi in header.FindAllChildren())
                    {
                        try
                        {
                            string txt = string.Empty;
                            if ((int)hi.ControlType == UIA_HeaderItem || (int)hi.ControlType == UIA_Text)
                            {
                                txt = ReadCellText(hi);
                            }
                            if (string.IsNullOrWhiteSpace(txt))
                            {
                                foreach (var g in hi.FindAllChildren())
                                {
                                    txt = ReadCellText(g);
                                    if (!string.IsNullOrWhiteSpace(txt)) break;
                                }
                            }
                            if (!string.IsNullOrWhiteSpace(txt)) result.Add(txt.Trim());
                        }
                        catch { }
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
                            var txt = ReadCellText(c).Trim();
                            if (!string.IsNullOrEmpty(txt)) values.Add(txt);
                            else
                            {
                                foreach (var gc in c.FindAllChildren())
                                {
                                    var t = ReadCellText(gc).Trim();
                                    if (!string.IsNullOrEmpty(t)) { values.Add(t); break; }
                                }
                            }
                        }
                        catch { }
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
                                if (!string.IsNullOrEmpty(t)) values.Add(t);
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

        private static int TryGetSelectedRowIndex(AutomationElement list, AutomationElement[] selected)
        {
            var children = list.FindAllChildren();
            var listItems = new List<AutomationElement>();
            foreach (var ch in children)
            {
                try { if ((int)ch.ControlType == UIA_ListItem) listItems.Add(ch); } catch { }
            }

            int rowIndex = -1;
            for (int i = 0; i < listItems.Count; i++)
            {
                try { if (listItems[i].Patterns.SelectionItem.IsSupported && listItems[i].Patterns.SelectionItem.PatternOrDefault?.IsSelected == true) { rowIndex = i; break; } } catch { }
            }
            if (rowIndex < 0 && selected.Length > 0)
            {
                var rowEl = selected[0];
                for (int i = 0; i < listItems.Count; i++) { if (listItems[i].Equals(rowEl)) { rowIndex = i; break; } }
            }
            if (rowIndex < 0 && selected.Length > 0)
            {
                try { var gi = selected[0].Patterns.GridItem.PatternOrDefault; if (gi != null) rowIndex = gi.Row; } catch { }
            }
            return rowIndex;
        }

        private static Dictionary<int, string> TryGetHeaders(FlaUI.Core.Patterns.ITablePattern? table)
        {
            var headers = new Dictionary<int, string>();
            try
            {
                var columnHeaders = table?.ColumnHeaders?.Value;
                if (columnHeaders != null)
                {
                    for (int i = 0; i < columnHeaders.Length; i++) headers[i] = columnHeaders[i]?.Name ?? string.Empty;
                }
            }
            catch { }
            return headers;
        }

        private static string FormatPairs(List<(string Header, string Value)> pairs)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in pairs)
            {
                if (!string.IsNullOrWhiteSpace(p.Header) && !dict.ContainsKey(p.Header)) dict[p.Header] = p.Value;
            }
            var sb = new StringBuilder();
            foreach (var h in PreferredHeaderOrder) if (dict.TryGetValue(h, out var v)) sb.AppendLine($"{h}: {v}");
            foreach (var p in pairs) if (!string.IsNullOrWhiteSpace(p.Header) && !PreferredHeaderOrder.Contains(p.Header, StringComparer.OrdinalIgnoreCase)) sb.AppendLine($"{p.Header}: {p.Value}");
            return sb.ToString().TrimEnd();
        }
    }
}
