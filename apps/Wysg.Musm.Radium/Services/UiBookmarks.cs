using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Wysg.Musm.Radium.Services;

public sealed class UiBookmarks
{
    public enum KnownControl
    {
        StudyInfoBanner,
        SelectedStudyInSearch,
        SelectedStudyInRelated,
        CloseWorklistButton,
        WorklistWindow,
        WorklistToolbar,
        WorklistViewButton,
        WorklistPane,
        WorklistListsPane,
        SearchResultsList,
        ReportPane,
        StudyList,
        RelatedStudyList,
        ReportText,
        ReportText2,
        ReportInput,
        ViewerWindow,
        ViewerToolbar,
        OpenWorklistButton,
        ReportCommitButton,
        StudyRemark, // existing
        PatientRemark // new: distinct bookmark for patient remark field
    }

    public enum MapMethod { Chain = 0, AutomationIdOnly = 1 }
    public enum SearchScope { Children = 0, Descendants = 1 }

    public sealed class Bookmark
    {
        public string Name { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public List<Node> Chain { get; set; } = new();
        public MapMethod Method { get; set; } = MapMethod.Chain;
        public string? DirectAutomationId { get; set; }
        public bool CrawlFromRoot { get; set; } = true;
    }

    public sealed class Node : INotifyPropertyChanged
    {
        private string? _name; private string? _className; private int? _controlTypeId; private string? _automationId; private int _indexAmongMatches = 0; private bool _include = true; private bool _useName = false; private bool _useClassName = true; private bool _useControlTypeId = true; private bool _useAutomationId = true; private bool _useIndex = true; private SearchScope _scope = SearchScope.Children; private int? _order;

        public string? Name { get => _name; set => SetProperty(ref _name, value); }
        public string? ClassName { get => _className; set => SetProperty(ref _className, value); }
        public int? ControlTypeId { get => _controlTypeId; set => SetProperty(ref _controlTypeId, value); }
        public string? AutomationId { get => _automationId; set => SetProperty(ref _automationId, value); }
        public int IndexAmongMatches { get => _indexAmongMatches; set => SetProperty(ref _indexAmongMatches, value); }
        public bool Include { get => _include; set { if (SetProperty(ref _include, value)) OnPropertyChanged(nameof(LocateIndex)); } }
        public bool UseName { get => _useName; set => SetProperty(ref _useName, value); }
        public bool UseClassName { get => _useClassName; set => SetProperty(ref _useClassName, value); }
        public bool UseControlTypeId { get => _useControlTypeId; set => SetProperty(ref _useControlTypeId, value); }
        public bool UseAutomationId { get => _useAutomationId; set => SetProperty(ref _useAutomationId, value); }
        public bool UseIndex { get => _useIndex; set => SetProperty(ref _useIndex, value); }
        public SearchScope Scope { get => _scope; set { if (SetProperty(ref _scope, value)) { OnPropertyChanged(nameof(ScopeIndex)); OnPropertyChanged(nameof(LocateIndex)); } } }
        public int? Order { get => _order; set => SetProperty(ref _order, value); }

        [JsonIgnore]
        public int LocateIndex
        {
            get => Include ? (Scope == SearchScope.Children ? 1 : 2) : 0;
            set
            {
                if (value <= 0)
                {
                    Include = false;
                    Scope = SearchScope.Children;
                    UseName = false; UseClassName = false; UseControlTypeId = false; UseAutomationId = false; UseIndex = false;
                }
                else
                {
                    Include = true;
                    Scope = value == 1 ? SearchScope.Children : SearchScope.Descendants;
                }
                OnPropertyChanged(); OnPropertyChanged(nameof(ScopeIndex));
            }
        }

        [JsonIgnore]
        public int ScopeIndex { get => (int)Scope; set { Scope = (SearchScope)value; OnPropertyChanged(); OnPropertyChanged(nameof(LocateIndex)); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value; OnPropertyChanged(name); return true;
        }
    }

    public sealed class Store
    {
        public List<Bookmark> Bookmarks { get; set; } = new();
        public Dictionary<string, Bookmark> ControlMap { get; set; } = new();
    }

    private static readonly JsonSerializerOptions _opts = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string GetStorePath()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wysg.Musm", "Radium");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "ui-bookmarks.json");
    }

    public static Store Load()
    {
        try
        {
            var p = GetStorePath();
            if (!File.Exists(p)) return new Store();
            return JsonSerializer.Deserialize<Store>(File.ReadAllText(p), _opts) ?? new Store();
        }
        catch { return new Store(); }
    }

    public static void Save(Store s)
    {
        try
        {
            var p = GetStorePath();
            File.WriteAllText(p, JsonSerializer.Serialize(s, _opts));
        }
        catch { }
    }

    public static void SaveMapping(KnownControl key, Bookmark b)
    {
        var s = Load();
        s.ControlMap[key.ToString()] = b;
        Save(s);
    }

    public static Bookmark? GetMapping(KnownControl key)
    {
        var s = Load();
        return s.ControlMap.TryGetValue(key.ToString(), out var b) ? b : null;
    }

    public static (IntPtr hwnd, AutomationElement? element) Resolve(KnownControl key)
    {
        var b = GetMapping(key);
        return b == null ? (IntPtr.Zero, null) : ResolveBookmark(b);
    }

    public static (IntPtr hwnd, AutomationElement? element) Resolve(string name)
    {
        var s = Load();
        var b = s.Bookmarks.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        return b == null ? (IntPtr.Zero, null) : ResolveBookmark(b);
    }

    public static (IntPtr hwnd, AutomationElement? element) TryResolveBookmark(Bookmark b) => ResolveBookmark(b);

    // Returns the exact path selected by the resolver (root -> ... -> final)
    public static (AutomationElement? final, List<AutomationElement> path) ResolvePath(Bookmark b)
    {
        try
        {
            using var automation = new UIA3Automation();
            var roots = DiscoverRoots(automation, b, out _);
            var ordered = OrderNodes(b);
            foreach (var root in roots)
            {
                var (final, path) = Walk(root, b, ordered, trace: null);
                if (final != null) return (final, path);
            }
            // Desktop-wide first-node fallback
            try
            {
                var cf = automation.ConditionFactory;
                var first = ordered.FirstOrDefault(n => n.Include && n.LocateIndex > 0);
                var cond = first != null ? BuildAndCondition(first, cf) : null;
                var hit = cond != null ? automation.GetDesktop().FindFirstDescendant(cond) : null;
                if (hit != null)
                {
                    var walker = automation.TreeWalkerFactory.GetControlViewWalker();
                    var top = hit; var p = walker.GetParent(hit);
                    while (p != null) { top = p; p = walker.GetParent(top); }
                    return Walk(top, b, ordered, trace: null);
                }
            }
            catch { }
            return (null, new());
        }
        catch { return (null, new()); }
    }

    public static (IntPtr hwnd, AutomationElement? element, string trace) TryResolveWithTrace(Bookmark b)
    {
        var sb = new StringBuilder();
        try
        {
            using var automation = new UIA3Automation();
            var roots = DiscoverRoots(automation, b, out var attachInfo);
            sb.AppendLine(attachInfo);
            sb.AppendLine($"Roots count: {roots.Length}");
            var ordered = OrderNodes(b);
            foreach (var root in roots)
            {
                sb.AppendLine($"Root: Name='{SafeName(root)}', Class='{SafeClass(root)}'");
                var (final, path) = Walk(root, b, ordered, sb);
                if (final != null)
                {
                    sb.AppendLine($"Resolved path length: {path.Count}");
                    for (int i = 0; i < path.Count; i++)
                    {
                        var e = path[i];
                        string? name = null, cls = null, autoId = null; int ct = -1;
                        try { name = e.Name; } catch { }
                        try { cls = e.ClassName; } catch { }
                        try { autoId = e.AutomationId; } catch { }
                        try { ct = (int)e.Properties.ControlType.Value; } catch { }
                        sb.AppendLine($"  [{i}] Ct={ct}, Class='{cls}', Name='{name}', AutoId='{autoId}'");
                    }
                    sb.AppendLine("Resolved successfully");
                    return (new IntPtr(SafeHandle(final)), final, sb.ToString());
                }
            }
            sb.AppendLine("All roots tried, not found");
            return (IntPtr.Zero, null, sb.ToString());
        }
        catch (Exception ex)
        {
            sb.AppendLine("Trace error: " + ex.Message);
            return (IntPtr.Zero, null, sb.ToString());
        }
    }

    private static (IntPtr hwnd, AutomationElement? element) ResolveBookmark(Bookmark b)
    {
        try
        {
            using var automation = new UIA3Automation();
            var roots = DiscoverRoots(automation, b, out _);
            var ordered = OrderNodes(b);
            foreach (var root in roots)
            {
                var (final, path) = Walk(root, b, ordered, trace: null);
                if (final != null) return (new IntPtr(SafeHandle(final)), final);
            }
            return (IntPtr.Zero, null);
        }
        catch { return (IntPtr.Zero, null); }
    }

    // Core walker used by ResolvePath / TryResolveWithTrace / ResolveBookmark
    private static (AutomationElement? final, List<AutomationElement> path) Walk(AutomationElement start, Bookmark b, List<Node> nodes, StringBuilder? trace)
    {
        var automation = (UIA3Automation)start.Automation;
        var cf = automation.ConditionFactory;
        var current = start;
        var path = new List<AutomationElement> { start };

        if (b.Method == MapMethod.AutomationIdOnly && !string.IsNullOrEmpty(b.DirectAutomationId))
        {
            var el = start.FindFirstDescendant(cf.ByAutomationId(b.DirectAutomationId));
            if (el != null) { path.Add(el); return (el, path); }
            return (null, new());
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            trace?.AppendLine($"Step {i}: Include={node.Include}, Scope={node.Scope}, UseName={node.UseName}('{node.Name}'), UseClassName={node.UseClassName}('{node.ClassName}'), UseAutomationId={node.UseAutomationId}('{node.AutomationId}'), UseControlTypeId={node.UseControlTypeId}({node.ControlTypeId}), UseIndex={node.UseIndex}({node.IndexAmongMatches})");

            if (!node.Include || node.LocateIndex == 0) { trace?.AppendLine($"Step {i}: Skipped (excluded)"); continue; }

            // Special handling for step 0: accept the discovered root by relaxed match
            if (i == 0)
            {
                bool acceptRoot = false;
                string? curName = null, curAuto = null; int curCt = -1;
                try { curName = current.Name; } catch { }
                try { curAuto = current.AutomationId; } catch { }
                try { curCt = (int)current.Properties.ControlType.Value; } catch { }

                if (node.UseAutomationId && !string.IsNullOrEmpty(node.AutomationId) && string.Equals(curAuto, node.AutomationId, StringComparison.Ordinal)) acceptRoot = true;
                else if (node.UseName && !string.IsNullOrEmpty(node.Name) && string.Equals(curName, node.Name, StringComparison.Ordinal)) acceptRoot = true;
                else if (node.UseControlTypeId && node.ControlTypeId.HasValue && node.ControlTypeId.Value == curCt) acceptRoot = true;

                if (acceptRoot)
                {
                    trace?.AppendLine("Step 0: Accept root by relaxed match (ignoring ClassName)");
                    continue;
                }
            }

            var cond = BuildAndCondition(node, cf);
            if (cond == null) { trace?.AppendLine($"Step {i}: No constraints"); continue; }

            AutomationElement[] matches = Array.Empty<AutomationElement>();
            bool usedRelaxed = false;
            try
            {
                if (node.Scope == SearchScope.Children)
                {
                    matches = current.FindAllChildren(cond);
                }
                else
                {
                    if (!node.UseIndex || node.IndexAmongMatches <= 0)
                    {
                        var hit = current.FindFirstDescendant(cond);
                        matches = hit != null ? new[] { hit } : Array.Empty<AutomationElement>();
                    }
                    else
                    {
                        matches = current.FindAllDescendants(cond);
                    }
                }
            }
            catch (Exception ex)
            {
                trace?.AppendLine($"Step {i}: Find failed: {ex.Message}");
                // VS sometimes throws on Children at root; fallback to descendant scan once
                if (i == 0)
                {
                    try
                    {
                        var hit = current.FindFirstDescendant(cond);
                        if (hit != null) matches = new[] { hit };
                    }
                    catch { }
                }
            }

            if (matches.Length == 0)
            {
                // Manual walk (Raw then Control)
                matches = ManualFindMatches(current, node, node.Scope, automation, preferRaw: true);
                if (matches.Length == 0)
                {
                    var ctrl = ManualFindMatches(current, node, node.Scope, automation, preferRaw: false);
                    if (ctrl.Length > 0) matches = ctrl;
                }

                // Relax ControlType if requested
                if (matches.Length == 0 && node.UseControlTypeId)
                {
                    var relaxed = CloneWithoutControlType(node);
                    var relaxedCond = BuildAndCondition(relaxed, cf);
                    try
                    {
                        if (relaxedCond != null)
                        {
                            if (node.Scope == SearchScope.Children)
                                matches = current.FindAllChildren(relaxedCond);
                            else
                            {
                                if (!node.UseIndex || node.IndexAmongMatches <= 0)
                                {
                                    var hit = current.FindFirstDescendant(relaxedCond);
                                    matches = hit != null ? new[] { hit } : Array.Empty<AutomationElement>();
                                }
                                else matches = current.FindAllDescendants(relaxedCond);
                            }
                        }
                    }
                    catch { matches = Array.Empty<AutomationElement>(); }
                }
            }

            trace?.AppendLine($"Step {i}: Scope={node.Scope}, matches={matches.Length}");
            if (matches.Length == 0) { trace?.AppendLine($"Step {i}: Failed"); return (null, new()); }

            if (!usedRelaxed && node.Scope == SearchScope.Descendants && matches.Length > 1)
            {
                try
                {
                    FlaUI.Core.Conditions.ConditionBase? nextCond = null;
                    for (int j = i + 1; j < nodes.Count; j++)
                    {
                        var next = nodes[j];
                        if (!next.Include || next.LocateIndex == 0) continue;
                        nextCond = BuildAndCondition(next, cf);
                        if (nextCond != null) break;
                    }
                    if (nextCond != null)
                    {
                        var leading = matches.FirstOrDefault(m => { try { return m.FindFirstDescendant(nextCond) != null; } catch { return false; } });
                        if (leading != null) { current = leading; path.Add(current); trace?.AppendLine("  -> Selected by look-ahead"); continue; }
                    }
                }
                catch (Exception ex) { trace?.AppendLine("  -> Look-ahead failed: " + ex.Message); }
            }

            int idx = (node.Scope == SearchScope.Descendants) ? 0 : (node.UseIndex ? node.IndexAmongMatches : 0);
            idx = Math.Max(0, Math.Min(idx, matches.Length - 1));
            current = matches[idx];
            path.Add(current);
        }

        return (path.LastOrDefault(), path);
    }

    // Root discovery with multiple fallbacks and optional first-node preference
    private static AutomationElement[] DiscoverRoots(UIA3Automation automation, Bookmark b, out string attachInfo)
    {
        var cf = automation.ConditionFactory;
        var sb = new StringBuilder();
        AutomationElement[] roots = Array.Empty<AutomationElement>();
        int appPid = -1;

        try
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var app = Application.Attach(b.ProcessName);
                roots = app.GetAllTopLevelWindows(automation) ?? Array.Empty<AutomationElement>();
                appPid = app.ProcessId;
                sw.Stop();
                sb.AppendLine($"Attach to '{b.ProcessName}': {roots.Length} top-level windows ({sw.ElapsedMilliseconds} ms)");
            }
            catch (Exception ex)
            {
                sw.Stop();
                sb.AppendLine($"Attach to '{b.ProcessName}' failed: {ex.Message} ({sw.ElapsedMilliseconds} ms)");
            }

            var desktop = automation.GetDesktop();
            var typeCond = cf.ByControlType(FlaUI.Core.Definitions.ControlType.Window)
                               .Or(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Pane));

            if (roots.Length == 0 && appPid > 0)
            {
                try { roots = desktop.FindAllChildren(cf.ByProcessId(appPid).And(typeCond)); sb.AppendLine($"By PID roots: {roots.Length}"); }
                catch (Exception ex) { sb.AppendLine("PID roots failed: " + ex.Message); }
            }

            if (roots.Length == 0)
            {
                try
                {
                    var pids = new List<int>();
                    try { pids.AddRange(Process.GetProcessesByName(b.ProcessName).Select(p => p.Id)); } catch { }
                    if (pids.Count > 0)
                    {
                        FlaUI.Core.Conditions.ConditionBase pidCond = cf.ByProcessId(pids[0]);
                        for (int i = 1; i < pids.Count; i++) pidCond = pidCond.Or(cf.ByProcessId(pids[i]));
                        roots = desktop.FindAllChildren(pidCond.And(typeCond));
                        sb.AppendLine($"By name roots: {roots.Length}");
                    }
                }
                catch (Exception ex) { sb.AppendLine("Name roots failed: " + ex.Message); }
            }

            if (roots.Length == 0)
            {
                try { roots = desktop.FindAllChildren(typeCond); sb.AppendLine($"Any Window/Pane roots: {roots.Length}"); }
                catch (Exception ex) { sb.AppendLine("Desktop Window/Pane failed: " + ex.Message); roots = Array.Empty<AutomationElement>(); }
            }

            // Prefer roots matching first node if requested
            var nodes = OrderNodes(b);
            var firstIncluded = nodes.FirstOrDefault(n => n.Include && n.LocateIndex > 0);
            if (firstIncluded != null && b.CrawlFromRoot)
            {
                try
                {
                    var firstCond = BuildAndCondition(firstIncluded, cf);
                    if (firstCond != null)
                    {
                        var cond = firstCond.And(typeCond);
                        if (appPid > 0) cond = cf.ByProcessId(appPid).And(cond);
                        var byFirst = automation.GetDesktop().FindAllChildren(cond);
                        if (byFirst.Length == 0 && firstIncluded.UseControlTypeId)
                        {
                            var relaxed = CloneWithoutControlType(firstIncluded);
                            var relaxedCond = BuildAndCondition(relaxed, cf);
                            if (relaxedCond != null)
                            {
                                cond = relaxedCond.And(typeCond);
                                if (appPid > 0) cond = cf.ByProcessId(appPid).And(cond);
                                byFirst = automation.GetDesktop().FindAllChildren(cond);
                            }
                        }
                        if (byFirst.Length > 0) { roots = byFirst; sb.AppendLine($"First-node matched roots: {roots.Length}"); }
                    }
                }
                catch (Exception ex) { sb.AppendLine("First-node roots failed: " + ex.Message); }
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine("DiscoverRoots fatal: " + ex.Message);
            roots = Array.Empty<AutomationElement>();
        }

        attachInfo = sb.ToString().TrimEnd();
        return roots;
    }

    private static List<Node> OrderNodes(Bookmark b) => b.Chain
        .Select((n, i) => new { Node = n, Index = i })
        .OrderBy(x => x.Node.Order ?? x.Index)
        .Select(x => x.Node)
        .ToList();

    private static FlaUI.Core.Conditions.ConditionBase? BuildAndCondition(Node node, FlaUI.Core.Conditions.ConditionFactory cf)
    {
        var conds = new List<FlaUI.Core.Conditions.ConditionBase>();
        if (node.UseAutomationId && !string.IsNullOrEmpty(node.AutomationId)) conds.Add(cf.ByAutomationId(node.AutomationId));
        if (node.UseClassName && !string.IsNullOrEmpty(node.ClassName)) conds.Add(cf.ByClassName(node.ClassName));
        if (node.UseControlTypeId && node.ControlTypeId.HasValue) conds.Add(cf.ByControlType((FlaUI.Core.Definitions.ControlType)node.ControlTypeId.Value));
        if (node.UseName && !string.IsNullOrEmpty(node.Name)) conds.Add(cf.ByName(node.Name));
        if (conds.Count == 0) return null;
        var result = conds[0];
        for (int i = 1; i < conds.Count; i++) result = result.And(conds[i]);
        return result;
    }

    private static bool ElementMatchesNode(AutomationElement e, Node node)
    {
        try
        {
            if (node.UseName && !string.Equals(SafeName(e), node.Name, StringComparison.Ordinal)) return false;
            if (node.UseClassName && !string.Equals(SafeClass(e), node.ClassName, StringComparison.Ordinal)) return false;
            if (node.UseAutomationId && !string.Equals(SafeAutoId(e), node.AutomationId, StringComparison.Ordinal)) return false;
            if (node.UseControlTypeId)
            {
                int ctId; try { ctId = (int)e.Properties.ControlType.Value; } catch { return false; }
                if (!node.ControlTypeId.HasValue || ctId != node.ControlTypeId.Value) return false;
            }
            return true;
        }
        catch { return false; }
    }

    private static Node CloneWithoutControlType(Node n) => new()
    {
        Name = n.Name,
        ClassName = n.ClassName,
        ControlTypeId = n.ControlTypeId,
        AutomationId = n.AutomationId,
        IndexAmongMatches = n.IndexAmongMatches,
        Include = n.Include,
        UseName = n.UseName,
        UseClassName = n.UseClassName,
        UseControlTypeId = false,
        UseAutomationId = n.UseAutomationId,
        UseIndex = n.UseIndex,
        Scope = n.Scope,
        Order = n.Order
    };

    private static AutomationElement[] ManualFindMatches(AutomationElement current, Node node, SearchScope scope, UIA3Automation automation, bool preferRaw)
    {
        var list = new List<AutomationElement>();
        AutomationElement[] Walk(bool useRaw)
        {
            var walker = useRaw ? automation.TreeWalkerFactory.GetRawViewWalker() : automation.TreeWalkerFactory.GetControlViewWalker();
            bool Match(AutomationElement el)
            {
                try
                {
                    if (node.UseName && !string.Equals(el.Name, node.Name, StringComparison.Ordinal)) return false;
                    if (node.UseClassName && !string.Equals(el.ClassName, node.ClassName, StringComparison.Ordinal)) return false;
                    if (node.UseAutomationId && !string.IsNullOrEmpty(node.AutomationId) && !string.Equals(el.AutomationId, node.AutomationId, StringComparison.Ordinal)) return false;
                    if (node.UseControlTypeId)
                    {
                        int ct; try { ct = (int)el.Properties.ControlType.Value; } catch { return false; }
                        if (!node.ControlTypeId.HasValue || ct != node.ControlTypeId.Value) return false;
                    }
                    return true;
                }
                catch { return false; }
            }
            IEnumerable<AutomationElement> ChildrenOf(AutomationElement e)
            {
                var child = walker.GetFirstChild(e);
                while (child != null)
                {
                    yield return child;
                    AutomationElement next = null;
                    try { next = walker.GetNextSibling(child); } catch { next = null; }
                    child = next;
                }
            }
            try
            {
                if (scope == SearchScope.Children)
                {
                    foreach (var c in ChildrenOf(current)) if (Match(c)) list.Add(c);
                }
                else
                {
                    var q = new Queue<AutomationElement>();
                    foreach (var c in ChildrenOf(current)) q.Enqueue(c);
                    int visited = 0; const int cap = 100000;
                    while (q.Count > 0 && visited < cap)
                    {
                        var el = q.Dequeue(); visited++;
                        if (Match(el)) list.Add(el);
                        foreach (var c in ChildrenOf(el)) q.Enqueue(c);
                    }
                }
            }
            catch { }
            return list.ToArray();
        }
        var result = Walk(preferRaw);
        if (result.Length == 0) { list.Clear(); result = Walk(!preferRaw); }
        return result;
    }

    private static string? SafeName(AutomationElement e) { try { return e.Name; } catch { return null; } }
    private static string? SafeClass(AutomationElement e) { try { return e.ClassName; } catch { return null; } }
    private static string? SafeAutoId(AutomationElement e) { try { return e.AutomationId; } catch { return null; } }
    private static nint SafeHandle(AutomationElement e) { try { return e.Properties.NativeWindowHandle.Value; } catch { return 0; } }
}