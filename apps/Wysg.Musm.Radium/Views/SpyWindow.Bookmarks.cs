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
using FlaUI.Core.AutomationElements;
using SWA = System.Windows.Automation;
using Wysg.Musm.Radium.Services; // UiBookmarks

namespace Wysg.Musm.Radium.Views
{
    /// <summary>
    /// Partial: Bookmark capture, editing, validation & mapping logic.
    /// </summary>
    public partial class SpyWindow
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
        private void OnReload(object sender, RoutedEventArgs e) => LoadBookmarks();
        private void OnBookmarkSelected(object sender, SelectionChangedEventArgs e)
        {
            if (FindName("lstBookmarks") is System.Windows.Controls.ListBox lb && lb.SelectedItem is UiBookmarks.Bookmark b)
            { LoadEditor(b); txtStatus.Text = $"Loaded bookmark: {b.Name}"; }
        }
        private void OnKnownSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FindName("cmbKnown") is not System.Windows.Controls.ComboBox combo) return;
            var item = combo.SelectedItem as System.Windows.Controls.ComboBoxItem; var keyStr = item?.Tag as string; if (string.IsNullOrWhiteSpace(keyStr)) return;
            if (!Enum.TryParse<UiBookmarks.KnownControl>(keyStr, out var key)) return;
            var mapping = UiBookmarks.GetMapping(key); if (mapping != null) LoadEditor(mapping);
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
            SaveEditorInto(_editing);
            var knownItem = cmbKnown?.SelectedItem as System.Windows.Controls.ComboBoxItem;
            var tagStr = knownItem?.Tag as string;
            if (!string.IsNullOrWhiteSpace(tagStr) && Enum.TryParse<UiBookmarks.KnownControl>(tagStr, out var key))
            {
                var toSave = new UiBookmarks.Bookmark
                {
                    Name = key.ToString(),
                    ProcessName = _editing.ProcessName,
                    Method = _editing.Method,
                    DirectAutomationId = _editing.DirectAutomationId,
                    CrawlFromRoot = _editing.CrawlFromRoot,
                    Chain = _editing.Chain.ToList()
                };
                UiBookmarks.SaveMapping(key, toSave);
                txtStatus.Text = $"Saved mapping for {key}"; return;
            }
            var store = UiBookmarks.Load();
            var name = string.IsNullOrWhiteSpace(_editing.Name) ? "Bookmark" : _editing.Name;
            var existing = store.Bookmarks.FirstOrDefault(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase));
            if (existing == null) { existing = new UiBookmarks.Bookmark { Name = name }; store.Bookmarks.Add(existing); }
            existing.ProcessName = _editing.ProcessName;
            existing.Method = _editing.Method;
            existing.DirectAutomationId = _editing.DirectAutomationId;
            existing.CrawlFromRoot = _editing.CrawlFromRoot;
            existing.Chain = _editing.Chain.ToList();
            UiBookmarks.Save(store);
            txtStatus.Text = $"Saved bookmark '{name}'";
        }

        // ------------------------------ Capture under mouse ------------------------------
        private async void OnPick(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtDelay.Text?.Trim(), out var delay)) delay = 600;
            txtStatus.Text = $"Pick arming... move mouse to target ({delay}ms)";
            await Task.Delay(delay);
            var (b, procName, msg) = CaptureUnderMouse(preferAutomationId: false);
            txtStatus.Text = msg;
            if (!string.IsNullOrWhiteSpace(procName)) txtProcess.Text = procName;
            if (b == null) return;
            Tag = b; b.ProcessName = string.IsNullOrWhiteSpace(procName) ? b.ProcessName : procName;
            ShowBookmarkDetails(b, "Captured chain");
            foreach (var n in b.Chain) n.UseIndex = false; // default: disable index after capture
            GridChain.ItemsSource = null; GridChain.ItemsSource = b.Chain;
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

        // ------------------------------ Mapping existing known control ------------------------------
        private void OnMapSelected(object sender, RoutedEventArgs e)
        {
            var item = cmbKnown.SelectedItem as System.Windows.Controls.ComboBoxItem; var keyStr = item?.Tag as string;
            if (string.IsNullOrWhiteSpace(keyStr)) { txtStatus.Text = "Select a known control"; return; }
            if (!Enum.TryParse<UiBookmarks.KnownControl>(keyStr, out var key)) { txtStatus.Text = "Invalid known control"; return; }
            var (b, procName, msg) = CaptureUnderMouse(preferAutomationId: true);
            txtStatus.Text = msg; if (!string.IsNullOrWhiteSpace(procName)) txtProcess.Text = procName; if (b == null) return;
            var method = CmbMethod.SelectedIndex == 0 ? UiBookmarks.MapMethod.Chain : UiBookmarks.MapMethod.AutomationIdOnly;
            b.Method = method;
            if (method == UiBookmarks.MapMethod.AutomationIdOnly)
            {
                var directId = b.Chain.LastOrDefault()?.AutomationId; b.DirectAutomationId = string.IsNullOrWhiteSpace(directId) ? null : directId;
            }
            UiBookmarks.SaveMapping(key, b); ShowBookmarkDetails(b, $"Mapped {key}"); HighlightBookmark(b); txtStatus.Text = $"Mapped {key}";
        }
        private void OnResolveSelected(object sender, RoutedEventArgs e)
        {
            var item = cmbKnown.SelectedItem as System.Windows.Controls.ComboBoxItem; var keyStr = item?.Tag as string;
            if (string.IsNullOrWhiteSpace(keyStr)) { txtStatus.Text = "Select a known control"; return; }
            if (!Enum.TryParse<UiBookmarks.KnownControl>(keyStr, out var key)) { txtStatus.Text = "Invalid known control"; return; }
            var mapping = UiBookmarks.GetMapping(key); if (mapping == null) { txtStatus.Text = "No mapping saved"; return; }
            var (hwnd, el) = UiBookmarks.Resolve(key); if (el == null) { txtStatus.Text += " | Resolve failed"; return; }
            var r = el.BoundingRectangle; _overlay.ShowForRect(new System.Drawing.Rectangle((int)r.Left, (int)r.Top, (int)r.Width, (int)r.Height));
            txtStatus.Text = $"Resolved {key}";
        }

        // ------------------------------ Validation & diagnostics ------------------------------
        private void OnValidateChain(object sender, RoutedEventArgs e)
        {
            ForceCommitGridEdits();
            if (!BuildBookmarkFromUi(out var copy)) return;
            var diag = BuildChainDiagnostic(copy);
            var sw = Stopwatch.StartNew();
            var (_, el, trace) = UiBookmarks.TryResolveWithTrace(copy); sw.Stop(); _lastResolved = el;
            if (el == null)
            { txtStatus.Text = diag + $"Validate: not found ({sw.ElapsedMilliseconds} ms)\r\n" + trace; return; }
            var r = el.BoundingRectangle;
            _overlay.ShowForRect(new System.Drawing.Rectangle((int)r.Left, (int)r.Top, (int)r.Width, (int)r.Height));
            txtStatus.Text = diag + $"Validate: found and highlighted ({sw.ElapsedMilliseconds} ms)";
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
        private void OnPreviewMouseDownForQuickMap(object? sender, MouseButtonEventArgs e)
        {
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl)) return;
            if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift)) return;
            var item = cmbKnown.SelectedItem as System.Windows.Controls.ComboBoxItem; var keyStr = item?.Tag as string;
            if (string.IsNullOrWhiteSpace(keyStr)) { txtStatus.Text = "Select a known control"; return; }
            var (b, procName, msg) = CaptureUnderMouse(preferAutomationId: true); txtStatus.Text = msg;
            if (!string.IsNullOrWhiteSpace(procName)) txtProcess.Text = procName; if (b == null) return;
            if (!Enum.TryParse<UiBookmarks.KnownControl>(keyStr, out var key)) { txtStatus.Text = "Invalid known control"; return; }
            var method = CmbMethod.SelectedIndex == 0 ? UiBookmarks.MapMethod.Chain : UiBookmarks.MapMethod.AutomationIdOnly;
            b.Method = method;
            if (method == UiBookmarks.MapMethod.AutomationIdOnly)
            { var directId = b.Chain.LastOrDefault()?.AutomationId; b.DirectAutomationId = string.IsNullOrWhiteSpace(directId) ? null : directId; }
            UiBookmarks.SaveMapping(key, b); LoadEditor(b); HighlightBookmark(b); txtStatus.Text = $"Quick-mapped {key}";
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
    }
}
