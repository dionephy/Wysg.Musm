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
        ReportInput,
        ViewerWindow,
        ViewerToolbar,
        OpenWorklistButton,
        ReportCommitButton
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

    public sealed class Node
    {
        public string? Name { get; set; }
        public string? ClassName { get; set; }
        public int? ControlTypeId { get; set; }
        public string? AutomationId { get; set; }
        public int IndexAmongMatches { get; set; } = 0;
        public bool Include { get; set; } = true; // kept for compatibility
        public bool UseName { get; set; } = false;
        public bool UseClassName { get; set; } = true;
        public bool UseControlTypeId { get; set; } = true;
        public bool UseAutomationId { get; set; } = true;
        public bool UseIndex { get; set; } = true;
        public SearchScope Scope { get; set; } = SearchScope.Children; // kept for compatibility
        public int? Order { get; set; } // optional explicit order

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
                    // When excluded, ensure all feature flags are unchecked
                    UseName = false;
                    UseClassName = false;
                    UseControlTypeId = false;
                    UseAutomationId = false;
                    UseIndex = false;
                }
                else
                {
                    Include = true;
                    Scope = value == 1 ? SearchScope.Children : SearchScope.Descendants;
                }
            }
        }

        [JsonIgnore]
        public int ScopeIndex { get => (int)Scope; set => Scope = (SearchScope)value; }
    }

    public sealed class Store
    {
        public List<Bookmark> Bookmarks { get; set; } = new();
        public Dictionary<string, Bookmark> ControlMap { get; set; } = new();
    }

    private static readonly System.Text.Json.JsonSerializerOptions _opts = new(System.Text.Json.JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
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
            var json = File.ReadAllText(p);
            return System.Text.Json.JsonSerializer.Deserialize<Store>(json, _opts) ?? new Store();
        }
        catch { return new Store(); }
    }

    public static void Save(Store s)
    {
        try
        {
            var p = GetStorePath();
            File.WriteAllText(p, System.Text.Json.JsonSerializer.Serialize(s, _opts));
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
        if (b == null) return (IntPtr.Zero, null);
        return ResolveBookmark(b);
    }

    public static (IntPtr hwnd, AutomationElement? element) Resolve(string name)
    {
        var s = Load();
        var b = s.Bookmarks.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        if (b == null) return (IntPtr.Zero, null);
        return ResolveBookmark(b);
    }

    public static (IntPtr hwnd, AutomationElement? element) TryResolveBookmark(Bookmark b) => ResolveBookmark(b);

    public static (IntPtr hwnd, AutomationElement? element, string trace) TryResolveWithTrace(Bookmark b)
    {
        var sb = new StringBuilder();
        try
        {
            using var automation = new UIA3Automation();
            var cf = automation.ConditionFactory;

            AutomationElement[] roots = Array.Empty<AutomationElement>();
            Application? app = null;
            int appPid = -1;
            var attachSw = Stopwatch.StartNew();
            try
            {
                app = Application.Attach(b.ProcessName);
                roots = app.GetAllTopLevelWindows(automation);
                appPid = app.ProcessId;
                attachSw.Stop();
                sb.AppendLine($"Attach to '{b.ProcessName}': {roots.Length} top-level windows ({attachSw.ElapsedMilliseconds} ms)");
            }
            catch
            {
                attachSw.Stop();
                sb.AppendLine($"Attach to '{b.ProcessName}' failed. ({attachSw.ElapsedMilliseconds} ms)");
            }

            var discoverSw = Stopwatch.StartNew();
            try
            {
                var desktop = automation.GetDesktop();
                var typeCond = cf.ByControlType(FlaUI.Core.Definitions.ControlType.Window)
                                  .Or(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Pane));

                if (roots.Length == 0 && appPid > 0)
                {
                    roots = desktop.FindAllChildren(cf.ByProcessId(appPid).And(typeCond));
                    sb.AppendLine($"Desktop fallback (by PID + Window/Pane): {roots.Length}");
                }

                if (roots.Length == 0)
                {
                    var pids = new List<int>();
                    try { pids.AddRange(Process.GetProcessesByName(b.ProcessName).Select(p => p.Id)); } catch { }
                    if (pids.Count > 0)
                    {
                        FlaUI.Core.Conditions.ConditionBase pidCond = cf.ByProcessId(pids[0]);
                        for (int i = 1; i < pids.Count; i++) pidCond = pidCond.Or(cf.ByProcessId(pids[i]));
                        roots = desktop.FindAllChildren(pidCond.And(typeCond));
                        sb.AppendLine($"Desktop fallback (by name + Window/Pane): {roots.Length}");
                    }
                }

                if (roots.Length == 0)
                {
                    roots = desktop.FindAllChildren(typeCond);
                    sb.AppendLine($"Desktop fallback (any Window/Pane): {roots.Length}");
                }

                // If user wants to crawl from root (first node), prefer roots that match first node
                var nodesOrderedTemp = b.Chain
                    .Select((n, i) => new { Node = n, Index = i })
                    .OrderBy(x => x.Node.Order ?? x.Index)
                    .Select(x => x.Node)
                    .ToList();
                var firstIncluded = nodesOrderedTemp.FirstOrDefault(n => n.Include && n.LocateIndex > 0);
                if (firstIncluded != null && b.CrawlFromRoot)
                {
                    var firstCond = BuildAndCondition(firstIncluded, cf);
                    if (firstCond != null)
                    {
                        var cond = firstCond.And(typeCond);
                        if (appPid > 0) cond = cf.ByProcessId(appPid).And(cond);
                        var byFirst = automation.GetDesktop().FindAllChildren(cond);
                        if (byFirst.Length == 0 && firstIncluded.UseControlTypeId)
                        {
                            // Relax ControlType for root discovery too
                            var relaxed = CloneWithoutControlType(firstIncluded);
                            var relaxedCond = BuildAndCondition(relaxed, cf);
                            if (relaxedCond != null)
                            {
                                var cond2 = relaxedCond.And(typeCond);
                                if (appPid > 0) cond2 = cf.ByProcessId(appPid).And(cond2);
                                byFirst = automation.GetDesktop().FindAllChildren(cond2);
                            }
                        }
                        if (byFirst.Length > 0)
                        {
                            roots = byFirst;
                            sb.AppendLine($"Root by first node match: {roots.Length}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Desktop fallback failed: {ex.Message}");
            }
            finally
            {
                discoverSw.Stop();
                sb.AppendLine($"Root discovery elapsed: {discoverSw.ElapsedMilliseconds} ms");
            }

            if (roots.Length == 0) return (IntPtr.Zero, null, sb.ToString());

            var nodesOrdered = b.Chain
                .Select((n, i) => new { Node = n, Index = i })
                .OrderBy(x => x.Node.Order ?? x.Index)
                .Select(x => x.Node)
                .ToList();

            foreach (var root in roots)
            {
                sb.AppendLine($"Root: Name='{SafeName(root)}', Class='{SafeClass(root)}'");
                AutomationElement current = root;

                if (b.Method == MapMethod.AutomationIdOnly && !string.IsNullOrEmpty(b.DirectAutomationId))
                {
                    var directSw = Stopwatch.StartNew();
                    AutomationElement? el = null;
                    try { el = root.FindFirstDescendant(cf.ByAutomationId(b.DirectAutomationId)); }
                    catch (Exception ex) { sb.AppendLine($"Direct AutomationId search failed: {ex.Message}"); }
                    directSw.Stop();
                    if (el != null) return (new IntPtr(SafeHandle(el)), el, sb.AppendLine($"Direct AutomationId hit ({directSw.ElapsedMilliseconds} ms)").ToString());
                    sb.AppendLine($"Direct AutomationId not found under this root ({directSw.ElapsedMilliseconds} ms)");
                    continue;
                }

                bool failed = false;
                for (int i = 0; i < nodesOrdered.Count; i++)
                {
                    var stepSw = Stopwatch.StartNew();
                    var node = nodesOrdered[i];
                    sb.AppendLine($"Step {i}: Include={node.Include}, Scope={node.Scope}, UseName={node.UseName}('{node.Name}'), UseClassName={node.UseClassName}('{node.ClassName}'), UseAutomationId={node.UseAutomationId}('{node.AutomationId}'), UseControlTypeId={node.UseControlTypeId}({node.ControlTypeId}), UseIndex={node.UseIndex}({node.IndexAmongMatches})");

                    if (!node.Include || node.LocateIndex == 0) { stepSw.Stop(); sb.AppendLine($"Step {i}: Skipped (excluded) [{stepSw.ElapsedMilliseconds} ms]"); continue; }

                    if (i == 0 && ElementMatchesNode(current, node))
                    {
                        stepSw.Stop();
                        sb.AppendLine($"Step {i}: Current root matches by AND props -> select self [{stepSw.ElapsedMilliseconds} ms]");
                        continue;
                    }

                    var cond = BuildAndCondition(node, cf);
                    if (cond == null) { stepSw.Stop(); sb.AppendLine($"Step {i}: No constraints -> noop [{stepSw.ElapsedMilliseconds} ms]"); continue; }

                    if (i == 0)
                    {
                        try
                        {
                            var rootHit = roots.FirstOrDefault(r => ElementMatchesNode(r, node));
                            if (rootHit != null)
                            {
                                current = rootHit;
                                stepSw.Stop();
                                sb.AppendLine($"Step {i}: Matched root by AND props [{stepSw.ElapsedMilliseconds} ms]");
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            sb.AppendLine($"Step {i}: Root match check failed: {ex.Message}");
                        }
                    }

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
                            // Fast path: first descendant only when index is not explicitly used
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
                        sb.AppendLine($"Step {i}: FindAll failed: {ex.Message}");
                    }

                    if (matches.Length == 0)
                    {
                        // Try manual walk first (Raw then Control)
                        matches = ManualFindMatches(current, node, node.Scope, automation, preferRaw: true);
                        if (matches.Length == 0)
                        {
                            var ctrlMatches = ManualFindMatches(current, node, node.Scope, automation, preferRaw: false);
                            if (ctrlMatches.Length > 0) matches = ctrlMatches;
                        }

                        // If still none and ControlType filter is on, try a relaxed search without ControlType
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
                                        else
                                        {
                                            matches = current.FindAllDescendants(relaxedCond);
                                        }
                                    }
                                }
                            }
                            catch { matches = Array.Empty<AutomationElement>(); }

                            if (matches.Length == 0)
                            {
                                matches = ManualFindMatches(current, relaxed, node.Scope, automation, preferRaw: true);
                                if (matches.Length == 0)
                                {
                                    var ctrlRelax = ManualFindMatches(current, relaxed, node.Scope, automation, preferRaw: false);
                                    if (ctrlRelax.Length > 0) matches = ctrlRelax;
                                }
                            }
                            if (matches.Length > 0) { usedRelaxed = true; sb.AppendLine($"Step {i}: Relaxed ControlType filter -> {matches.Length} matches"); }
                            else
                            {
                                var diag = ManualFindMatches(current, relaxed, node.Scope, automation, preferRaw: true);
                                if (diag.Length == 0) diag = ManualFindMatches(current, relaxed, node.Scope, automation, preferRaw: false);
                                if (diag.Length > 0)
                                {
                                    sb.AppendLine($"Step {i}: Candidates ignoring ControlType: {Math.Min(5, diag.Length)}");
                                    for (int d = 0; d < Math.Min(5, diag.Length); d++)
                                    {
                                        var e = diag[d];
                                        int ctVal = -1; string ctName = "?";
                                        try { ctVal = (int)e.Properties.ControlType.Value; ctName = e.Properties.ControlType.Value.ToString(); } catch { }
                                        sb.AppendLine($"    -> Ct={ctVal}({ctName}), Name='{SafeName(e)}', Class='{SafeClass(e)}', AutoId='{SafeAutoId(e)}'");
                                    }
                                }
                            }
                        }

                        if (matches.Length == 0)
                        {
                            int rawChildren = CountChildren(current, automation, useRaw: true);
                            int ctrlChildren = CountChildren(current, automation, useRaw: false);
                            sb.AppendLine($"Step {i}: ChildCounts Raw={rawChildren}, Control={ctrlChildren}");
                        }
                    }

                    sb.AppendLine($"Step {i}: Scope={node.Scope}, matches={matches.Length}");
                    for (int k = 0; k < Math.Min(5, matches.Length); k++)
                    {
                        var m = matches[k];
                        sb.AppendLine($"  - [{k}] Name='{SafeName(m)}', Class='{SafeClass(m)}', AutoId='{SafeAutoId(m)}'");
                    }

                    if (matches.Length == 0) { failed = true; stepSw.Stop(); sb.AppendLine($"Step {i}: Failed [{stepSw.ElapsedMilliseconds} ms]"); break; }

                    if (!usedRelaxed && node.Scope == SearchScope.Descendants && matches.Length > 1)
                    {
                        try
                        {
                            FlaUI.Core.Conditions.ConditionBase? nextCond = null;
                            for (int j = i + 1; j < nodesOrdered.Count; j++)
                            {
                                var next = nodesOrdered[j];
                                if (!next.Include || next.LocateIndex == 0) continue;
                                nextCond = BuildAndCondition(next, cf);
                                if (nextCond != null) break;
                            }
                            if (nextCond != null)
                            {
                                var leading = matches.FirstOrDefault(m =>
                                {
                                    try { return m.FindFirstDescendant(nextCond) != null; }
                                    catch { return false; }
                                });
                                if (leading != null)
                                {
                                    current = leading;
                                    sb.AppendLine("  -> Selected by look-ahead");
                                    stepSw.Stop();
                                    continue;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            sb.AppendLine($"  -> Look-ahead failed: {ex.Message}");
                        }
                    }

                    int idx = (node.Scope == SearchScope.Descendants) ? 0 : (node.UseIndex ? node.IndexAmongMatches : 0);
                    idx = Math.Max(0, Math.Min(idx, matches.Length - 1));
                    current = matches[idx];
                    stepSw.Stop();
                    sb.AppendLine($"  -> Selected index {idx} [{stepSw.ElapsedMilliseconds} ms]");
                }
                if (!failed)
                {
                    var hwnd = new IntPtr(SafeHandle(current));
                    sb.AppendLine("Resolved successfully");
                    return (hwnd, current, sb.ToString());
                }
            }
            sb.AppendLine("All roots tried, not found");
            return (IntPtr.Zero, null, sb.ToString());
        }
        catch (Exception ex)
        {
            return (IntPtr.Zero, null, $"Trace error: {ex.Message}");
        }

        static string? SafeName(AutomationElement e) { try { return e.Name; } catch { return null; } }
        static string? SafeClass(AutomationElement e) { try { return e.ClassName; } catch { return null; } }
        static string? SafeAutoId(AutomationElement e) { try { return e.AutomationId; } catch { return null; } }
        static nint SafeHandle(AutomationElement e) { try { return e.Properties.NativeWindowHandle.Value; } catch { return 0; } }
        static int CountChildren(AutomationElement e, UIA3Automation automation, bool useRaw)
        {
            try
            {
                var walker = useRaw ? automation.TreeWalkerFactory.GetRawViewWalker() : automation.TreeWalkerFactory.GetControlViewWalker();
                int count = 0;
                var c = walker.GetFirstChild(e);
                while (c != null && count < 10000)
                {
                    count++;
                    c = walker.GetNextSibling(c);
                }
                return count;
            }
            catch { return -1; }
        }
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
        UseControlTypeId = false, // relaxed
        UseAutomationId = n.UseAutomationId,
        UseIndex = n.UseIndex,
        Scope = n.Scope,
        Order = n.Order
    };

    private static (IntPtr hwnd, AutomationElement? element) ResolveBookmark(Bookmark b)
    {
        try
        {
            using var automation = new UIA3Automation();
            var cf = automation.ConditionFactory;
            AutomationElement[] roots = Array.Empty<AutomationElement>();

            try
            {
                using var app = Application.Attach(b.ProcessName);
                roots = app.GetAllTopLevelWindows(automation);
                if (roots == null || roots.Length == 0)
                {
                    var mw = app.GetMainWindow(automation, TimeSpan.FromMilliseconds(800));
                    roots = mw != null ? new AutomationElement[] { mw } : Array.Empty<AutomationElement>();
                }
                if (roots == null) roots = Array.Empty<AutomationElement>();

                if (roots.Length == 0)
                {
                    var desktop = automation.GetDesktop();
                    var typeCond = cf.ByControlType(FlaUI.Core.Definitions.ControlType.Window)
                                      .Or(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Pane));
                    roots = desktop.FindAllChildren(cf.ByProcessId(app.ProcessId).And(typeCond));
                }
            }
            catch
            {
                var desktop = automation.GetDesktop();
                var typeCond = cf.ByControlType(FlaUI.Core.Definitions.ControlType.Window)
                                  .Or(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Pane));
                var pids = new List<int>();
                try { pids.AddRange(Process.GetProcessesByName(b.ProcessName).Select(p => p.Id)); } catch { }
                if (pids.Count > 0)
                {
                    FlaUI.Core.Conditions.ConditionBase pidCond = cf.ByProcessId(pids[0]);
                    for (int i = 1; i < pids.Count; i++) pidCond = pidCond.Or(cf.ByProcessId(pids[i]));
                    roots = desktop.FindAllChildren(pidCond.And(typeCond));
                }
                if (roots.Length == 0)
                {
                    roots = desktop.FindAllChildren(typeCond);
                }
            }

            // Prefer roots that match first node if requested
            var nodesOrdered = b.Chain
                .Select((n, i) => new { Node = n, Index = i })
                .OrderBy(x => x.Node.Order ?? x.Index)
                .Select(x => x.Node)
                .ToList();
            var firstIncluded = nodesOrdered.FirstOrDefault(n => n.Include && n.LocateIndex > 0);
            if (firstIncluded != null && b.CrawlFromRoot)
            {
                var firstCond = BuildAndCondition(firstIncluded, cf);
                if (firstCond != null)
                {
                    var typeCond = cf.ByControlType(FlaUI.Core.Definitions.ControlType.Window)
                                      .Or(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Pane));
                    var cond = firstCond.And(typeCond);
                    var desktop = automation.GetDesktop();
                    var byFirst = desktop.FindAllChildren(cond);
                    if (byFirst.Length == 0 && firstIncluded.UseControlTypeId)
                    {
                        var relaxed = CloneWithoutControlType(firstIncluded);
                        var relaxedCond = BuildAndCondition(relaxed, cf);
                        if (relaxedCond != null) byFirst = desktop.FindAllChildren(relaxedCond.And(typeCond));
                    }
                    if (byFirst.Length > 0) roots = byFirst;
                }
            }

            if (roots.Length == 0) return (IntPtr.Zero, null);

            foreach (var root in roots)
            {
                AutomationElement current = root;

                if (b.Method == MapMethod.AutomationIdOnly && !string.IsNullOrEmpty(b.DirectAutomationId))
                {
                    var el = root.FindFirstDescendant(cf.ByAutomationId(b.DirectAutomationId));
                    if (el != null) return (new IntPtr(SafeHandle(el)), el);
                    continue; // try next root
                }

                bool failed = false;

                for (int i = 0; i < nodesOrdered.Count; i++)
                {
                    var node = nodesOrdered[i];
                    if (!node.Include || node.LocateIndex == 0) continue; // excluded

                    if (i == 0 && ElementMatchesNode(current, node))
                    {
                        continue;
                    }

                    var cond = BuildAndCondition(node, cf);
                    if (cond == null) continue; // no constraints selected => noop

                    if (i == 0)
                    {
                        var rootHit = roots.FirstOrDefault(r => ElementMatchesNode(r, node));
                        if (rootHit != null)
                        {
                            current = rootHit;
                            continue;
                        }
                    }

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
                    catch
                    {
                        // ignore
                    }

                    if (matches.Length == 0)
                    {
                        matches = ManualFindMatches(current, node, node.Scope, automation, preferRaw: true);
                        if (matches.Length == 0)
                        {
                            var ctrl = ManualFindMatches(current, node, node.Scope, automation, preferRaw: false);
                            if (ctrl.Length > 0) matches = ctrl;
                        }
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
                                        else
                                        {
                                            matches = current.FindAllDescendants(relaxedCond);
                                        }
                                    }
                                }
                            }
                            catch { matches = Array.Empty<AutomationElement>(); }
                            if (matches.Length == 0)
                            {
                                var ctrl2 = ManualFindMatches(current, relaxed, node.Scope, automation, preferRaw: false);
                                if (ctrl2.Length > 0) matches = ctrl2;
                            }
                            if (matches.Length > 0) usedRelaxed = true;
                        }
                    }

                    if (matches.Length == 0) { failed = true; break; }

                    if (!usedRelaxed && node.Scope == SearchScope.Descendants && matches.Length > 1)
                    {
                        FlaUI.Core.Conditions.ConditionBase? nextCond = null;
                        for (int j = i + 1; j < nodesOrdered.Count; j++)
                        {
                            var next = nodesOrdered[j];
                            if (!next.Include || next.LocateIndex == 0) continue;
                            nextCond = BuildAndCondition(next, cf);
                            if (nextCond != null) break;
                        }
                        if (nextCond != null)
                        {
                            var leading = matches.FirstOrDefault(m =>
                            {
                                try { return m.FindFirstDescendant(nextCond) != null; }
                                catch { return false; }
                            });
                            if (leading != null)
                            {
                                current = leading;
                                continue;
                            }
                        }
                    }

                    int idx = (node.Scope == SearchScope.Descendants) ? 0 : (node.UseIndex ? node.IndexAmongMatches : 0);
                    idx = Math.Max(0, Math.Min(idx, matches.Length - 1));
                    current = matches[idx];
                }
                if (!failed)
                {
                    var hwnd = new IntPtr(SafeHandle(current));
                    return (hwnd, current);
                }
            }
            return (IntPtr.Zero, null);
        }
        catch { return (IntPtr.Zero, null); }

        static nint SafeHandle(AutomationElement e) { try { return e.Properties.NativeWindowHandle.Value; } catch { return 0; } }
    }

    private static FlaUI.Core.Conditions.ConditionBase? BuildAndCondition(Node node, FlaUI.Core.Conditions.ConditionFactory cf)
    {
        var conds = new System.Collections.Generic.List<FlaUI.Core.Conditions.ConditionBase>();
        if (node.UseAutomationId && !string.IsNullOrEmpty(node.AutomationId)) conds.Add(cf.ByAutomationId(node.AutomationId));
        if (node.UseClassName && !string.IsNullOrEmpty(node.ClassName)) conds.Add(cf.ByClassName(node.ClassName));
        if (node.UseControlTypeId && node.ControlTypeId.HasValue)
        {
            conds.Add(cf.ByControlType((FlaUI.Core.Definitions.ControlType)node.ControlTypeId.Value));
        }
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
                int ctId;
                try { ctId = (int)e.Properties.ControlType.Value; } catch { return false; }
                if (!node.ControlTypeId.HasValue || ctId != node.ControlTypeId.Value) return false;
            }
            return true;
        }
        catch { return false; }

        static string? SafeName(AutomationElement el) { try { return el.Name; } catch { return null; } }
        static string? SafeClass(AutomationElement el) { try { return el.ClassName; } catch { return null; } }
        static string? SafeAutoId(AutomationElement el) { try { return el.AutomationId; } catch { return null; } }
    }

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

        // Try preferred view first, then fallback to the other
        var result = Walk(preferRaw);
        if (result.Length == 0)
        {
            list.Clear();
            result = Walk(!preferRaw);
        }
        return result;
    }
}