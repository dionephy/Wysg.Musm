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
    public static Func<string>? GetStorePathOverride { get; set; }

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
        PatientRemark, // new: distinct bookmark for patient remark field
        TestInvoke, // new: generic target for testing Invoke operation
        Screen_MainCurrentStudyTab, // NEW: main screen current study tab area
        Screen_SubPreviousStudyTab, // NEW: sub screen previous study tab area
        ForeignTextbox, // NEW: external textbox for two-way sync with Findings editor
        // NEW: Additional KnownControl entries for new features (2025-01-16)
        WorklistOpenButton, // for InvokeOpenWorklist - button that opens worklist
        SendReportButton // for SendReport - button that sends report in PACS
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
        if (GetStorePathOverride != null) return GetStorePathOverride();
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

    /// <summary>
    /// Resolves a KnownControl with automatic retry and progressive constraint relaxation.
    /// Inspired by legacy PacsService pattern of trying multiple approaches (e.g., eViewer1 then eViewer2, AutomationId then ClassName).
    /// </summary>
    /// <param name="key">The KnownControl to resolve</param>
    /// <param name="maxAttempts">Maximum number of resolution attempts (default 3)</param>
    /// <returns>Tuple of (window handle, AutomationElement) or (IntPtr.Zero, null) if all attempts fail</returns>
    public static (IntPtr hwnd, AutomationElement? element) ResolveWithRetry(KnownControl key, int maxAttempts = 3)
    {
        var b = GetMapping(key);
        if (b == null) return (IntPtr.Zero, null);

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Attempt 1: Try exact match with all constraints
            if (attempt == 0)
            {
                var result = ResolveBookmark(b);
                if (result.element != null) return result;
            }
            // Attempt 2: Progressive relaxation - relax ControlType for all nodes
            else if (attempt == 1)
            {
                var relaxed = RelaxBookmarkControlType(b);
                var result = ResolveBookmark(relaxed);
                if (result.element != null) return result;
            }
            // Attempt 3: Further relaxation - relax ClassName (keep Name + AutomationId)
            else if (attempt == 2)
            {
                var relaxed = RelaxBookmarkClassName(b);
                var result = ResolveBookmark(relaxed);
                if (result.element != null) return result;
            }

            // Wait before next attempt (exponential backoff)
            if (attempt < maxAttempts - 1)
            {
                System.Threading.Thread.Sleep(150 * (attempt + 1)); // 150ms, 300ms
            }
        }

        // All attempts exhausted
        return (IntPtr.Zero, null);
    }

    /// <summary>
    /// Creates a copy of the bookmark with ControlType constraint relaxed on all nodes.
    /// Inspired by legacy pattern: try AutomationId first, fall back without ControlType constraint.
    /// </summary>
    private static Bookmark RelaxBookmarkControlType(Bookmark b)
    {
        return new Bookmark
        {
            Name = b.Name,
            ProcessName = b.ProcessName,
            Method = b.Method,
            DirectAutomationId = b.DirectAutomationId,
            CrawlFromRoot = b.CrawlFromRoot,
            Chain = b.Chain.Select(n => new Node
            {
                Name = n.Name,
                ClassName = n.ClassName,
                ControlTypeId = n.ControlTypeId,
                AutomationId = n.AutomationId,
                IndexAmongMatches = n.IndexAmongMatches,
                Include = n.Include,
                UseName = n.UseName,
                UseClassName = n.UseClassName,
                UseControlTypeId = false, // Relax ControlType
                UseAutomationId = n.UseAutomationId,
                UseIndex = n.UseIndex,
                Scope = n.Scope,
                Order = n.Order
            }).ToList()
        };
    }

    /// <summary>
    /// Creates a copy of the bookmark with ClassName constraint relaxed on all nodes (keeps Name + AutomationId).
    /// Most aggressive relaxation - only use when other attempts fail.
    /// </summary>
    private static Bookmark RelaxBookmarkClassName(Bookmark b)
    {
        return new Bookmark
        {
            Name = b.Name,
            ProcessName = b.ProcessName,
            Method = b.Method,
            DirectAutomationId = b.DirectAutomationId,
            CrawlFromRoot = b.CrawlFromRoot,
            Chain = b.Chain.Select(n => new Node
            {
                Name = n.Name,
                ClassName = n.ClassName,
                ControlTypeId = n.ControlTypeId,
                AutomationId = n.AutomationId,
                IndexAmongMatches = n.IndexAmongMatches,
                Include = n.Include,
                UseName = n.UseName,
                UseClassName = false, // Relax ClassName
                UseControlTypeId = false, // Also relax ControlType
                UseAutomationId = n.UseAutomationId,
                UseIndex = n.UseIndex,
                Scope = n.Scope,
                Order = n.Order
            }).ToList()
        };
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

    // FIX: Reduced retry count to minimize wasted delays (single attempt only, no retries)
    private const int StepRetryCount = 0; // total attempts per step = 1 + StepRetryCount
    private const int StepRetryDelayMs = 50; // reduced from 150ms for faster failure
    private const int ManualWalkCapChildren = 10000; // Cap for Children scope
    private const int ManualWalkCapDescendants = 5000; // Cap for Descendants scope (reduced from 100k)
    private const int ManualWalkTimeoutMs = 3000; // Max time for manual walker Descendants search
    private const int FastFailThresholdMs = 150; // Query time threshold for fast-fail (increased from 100ms)

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
            var stepSw = Stopwatch.StartNew(); // FIX: Track timing for each step
            var node = nodes[i];
            trace?.AppendLine($"Step {i}: Include={node.Include}, Scope={node.Scope}, UseName={node.UseName}('{node.Name}'), UseClassName={node.UseClassName}('{node.ClassName}'), UseAutomationId={node.UseAutomationId}('{node.AutomationId}'), UseControlTypeId={node.UseControlTypeId}({node.ControlTypeId}), UseIndex={node.UseIndex}({node.IndexAmongMatches})");

            if (!node.Include || node.LocateIndex == 0) { stepSw.Stop(); trace?.AppendLine($"Step {i}: Skipped (excluded) ({stepSw.ElapsedMilliseconds} ms)"); continue; }

            // FIX: Stricter step 0 validation - require ClassName match when provided
            if (i == 0)
            {
                bool acceptRoot = false;
                string? curName = null, curClass = null, curAuto = null; int curCt = -1;
                try { curName = current.Name; } catch { }
                try { curClass = current.ClassName; } catch { }
                try { curAuto = current.AutomationId; } catch { }
                try { curCt = (int)current.Properties.ControlType.Value; } catch { }

                // FIX: Require all enabled attributes to match (stricter than before)
                bool nameMatch = !node.UseName || string.Equals(curName, node.Name, StringComparison.Ordinal);
                bool classMatch = !node.UseClassName || string.Equals(curClass, node.ClassName, StringComparison.Ordinal);
                bool autoMatch = !node.UseAutomationId || string.Equals(curAuto, node.AutomationId, StringComparison.Ordinal);
                bool ctMatch = !node.UseControlTypeId || (node.ControlTypeId.HasValue && node.ControlTypeId.Value == curCt);

                // FIX: Accept root only if ALL enabled attributes match
                acceptRoot = nameMatch && classMatch && autoMatch && ctMatch;

                if (acceptRoot)
                {
                    stepSw.Stop(); // FIX: Stop timing for step 0
                    trace?.AppendLine($"Step 0: Accept root (Name={nameMatch}, Class={classMatch}, Auto={autoMatch}, Ct={ctMatch}) ({stepSw.ElapsedMilliseconds} ms)");
                    continue;
                }
                else
                {
                    trace?.AppendLine($"Step 0: Root mismatch (Name={nameMatch}, Class={classMatch}, Auto={autoMatch}, Ct={ctMatch}) - will attempt normal search");
                    // FIX: Don't fail immediately - attempt normal search below
                }
            }

            var cond = BuildAndCondition(node, cf);
            if (cond == null) 
            { 
                // FIX: Support pure index-based navigation (legacy pattern)
                // When no attributes are enabled but UseIndex=true, use index-based child selection
                if (node.UseIndex && node.Scope == SearchScope.Children)
                {
                    trace?.AppendLine($"Step {i}: Pure index navigation (no attributes, using index {node.IndexAmongMatches})");
                    
                    try
                    {
                        var children = current.FindAllChildren();
                        if (children.Length > node.IndexAmongMatches)
                        {
                            current = children[node.IndexAmongMatches];
                            path.Add(current);
                            stepSw.Stop();
                            trace?.AppendLine($"Step {i}: Pure index success - selected child at index {node.IndexAmongMatches} ({stepSw.ElapsedMilliseconds} ms)");
                            continue;
                        }
                        else
                        {
                            stepSw.Stop();
                            trace?.AppendLine($"Step {i}: Pure index failed - index {node.IndexAmongMatches} out of range (only {children.Length} children) ({stepSw.ElapsedMilliseconds} ms)");
                            return (null, new());
                        }
                    }
                    catch (Exception ex)
                    {
                        stepSw.Stop();
                        trace?.AppendLine($"Step {i}: Pure index failed - {ex.Message} ({stepSw.ElapsedMilliseconds} ms)");
                        return (null, new());
                    }
                }
                
                trace?.AppendLine($"Step {i}: No constraints"); 
                continue; 
            }

            AutomationElement[] matches = Array.Empty<AutomationElement>();
            // FIX: Detailed retry timing breakdown
            int totalRetries = 0;
            long queryTimeMs = 0;
            long retryDelayMs = 0;
            bool skipRetries = false; // FIX: Flag to skip retries on "not supported" errors
            bool skipManualWalker = false; // FIX: Flag to skip expensive manual walker on Descendants
            
            // Attempt primary find with small retry/backoff for transient UIA failures
            for (int attempt = 0; attempt <= StepRetryCount; attempt++)
            {
                var attemptSw = Stopwatch.StartNew();
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
                    attemptSw.Stop();
                    queryTimeMs += attemptSw.ElapsedMilliseconds;
                    trace?.AppendLine($"Step {i}: Attempt {attempt + 1}/{StepRetryCount + 1} - Query took {attemptSw.ElapsedMilliseconds} ms, found {matches.Length} matches");
                }
                catch (Exception ex)
                {
                    attemptSw.Stop();
                    queryTimeMs += attemptSw.ElapsedMilliseconds;
                    trace?.AppendLine($"Step {i}: Attempt {attempt + 1}/{StepRetryCount + 1} - Find failed after {attemptSw.ElapsedMilliseconds} ms: {ex.Message}");
                    
                    // FIX: Detect PropertyNotSupportedException and "method not supported" errors
                    // IMPORTANT: Only skip fallbacks at step 0 (process doesn't exist scenario)
                    // For child steps (i > 0), we still want to try manual walker as element may exist
                    if (ex is FlaUI.Core.Exceptions.PropertyNotSupportedException ||
                        (ex.Message != null && (ex.Message.Contains("not supported", StringComparison.OrdinalIgnoreCase) ||
                                                 ex.Message.Contains("not implemented", StringComparison.OrdinalIgnoreCase))))
                    {
                        if (i == 0)
                        {
                            // At root level - likely process doesn't exist, skip everything
                            trace?.AppendLine($"Step {i}: Detected unsupported property/method error at root level, skipping retries and fallbacks");
                            skipRetries = true;
                        }
                        else
                        {
                            // At child level - method not supported but element may exist, try manual walker
                            trace?.AppendLine($"Step {i}: Detected unsupported property/method error, will skip retries but try manual walker");
                            skipRetries = true;
                            // Don't set skipFallbacks - let manual walker run for child steps
                        }
                    }
    
                    // VS sometimes throws on Children at root; fallback to descendant scan once (only if not permanent error)
                    if (i == 0 && !skipRetries)
                    {
                        try
                        {
                            var hit = current.FindFirstDescendant(cond);
                            if (hit != null) matches = new[] { hit };
                        }
                        catch { }
                    }
    
                    // FIX: Break early if we detected a permanent error at root level
                    if (skipRetries && i == 0)
                        break;
                }

                if (matches.Length > 0)
                {
                    trace?.AppendLine($"Step {i}: Success on attempt {attempt + 1} (QueryTime={queryTimeMs} ms, Retries={totalRetries}, RetryDelay={retryDelayMs} ms)");
                    break;
                }
                
                if (attempt < StepRetryCount && !skipRetries)
                {
                    totalRetries++;
                    var delay = StepRetryDelayMs + attempt * 100;
                    retryDelayMs += delay;
                    trace?.AppendLine($"Step {i}: No match on attempt {attempt + 1}, retrying in {delay} ms...");
                    try { System.Threading.Thread.Sleep(delay); } catch { }
                }
            }

            if (matches.Length == 0)
            {
                // FIX: Skip expensive fallbacks only if permanent error at ROOT level (i=0)
                // For child steps, always try manual walker even with PropertyNotSupportedException
                bool skipFallbacks = (i == 0 && skipRetries);
                
                // FIX: For Descendants scope, skip manual walker if query failed quickly (likely doesn't exist)
                // Manual walker for Descendants is extremely expensive (can take 30+ seconds)
                // Increased threshold from 100ms to 150ms to catch more fast-failure cases
                if (!skipFallbacks && node.Scope == SearchScope.Descendants && queryTimeMs < FastFailThresholdMs)
                {
                    trace?.AppendLine($"Step {i}: Skipping manual walker for Descendants (query failed quickly in {queryTimeMs}ms < {FastFailThresholdMs}ms, likely element doesn't exist)");
                    skipManualWalker = true;
                }
                
                // Manual walk (Raw then Control) - only skip if root-level permanent error or Descendants fast-fail
                if (!skipFallbacks && !skipManualWalker)
                {
                    var manualSw = Stopwatch.StartNew();
                    matches = ManualFindMatches(current, node, node.Scope, automation, preferRaw: true, trace: trace);
                    if (matches.Length == 0)
                    {
                        var ctrl = ManualFindMatches(current, node, node.Scope, automation, preferRaw: false, trace: trace);
                        if (ctrl.Length > 0) matches = ctrl;
                    }
                    manualSw.Stop();
                    trace?.AppendLine($"Step {i}: Manual walker time: {manualSw.ElapsedMilliseconds} ms (found {matches.Length} matches)");
                }

                // Relax ControlType if requested - only if not root-level permanent error
                if (matches.Length == 0 && !skipFallbacks && node.UseControlTypeId)
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
            if (matches.Length == 0)
            {
                stepSw.Stop();
                trace?.AppendLine($"Step {i}: Failed - Total time {stepSw.ElapsedMilliseconds} ms (Query={queryTimeMs} ms, RetryDelay={retryDelayMs} ms, Attempts={totalRetries + 1})");
                return (null, new());
            }

            if (node.Scope == SearchScope.Descendants && matches.Length > 1)
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
                        if (leading != null) { current = leading; path.Add(current); stepSw.Stop(); trace?.AppendLine($"  -> Selected by look-ahead ({stepSw.ElapsedMilliseconds} ms)"); continue; }
                    }
                }
                catch (Exception ex) { trace?.AppendLine("  -> Look-ahead failed: " + ex.Message); }
            }

            int idx = (node.Scope == SearchScope.Descendants) ? 0 : (node.UseIndex ? node.IndexAmongMatches : 0);
            idx = Math.Max(0, Math.Min(idx, matches.Length - 1));
            current = matches[idx];
            path.Add(current);
            stepSw.Stop(); // FIX: Stop timing after step completes
            trace?.AppendLine($"Step {i}: Completed - Total time {stepSw.ElapsedMilliseconds} ms (Query={queryTimeMs} ms, RetryDelay={retryDelayMs} ms, Attempts={totalRetries + 1})");
        }

        return (path.LastOrDefault(), path);
    }

    // Root discovery with multiple fallbacks and optional first-node preference
    // FIX: Improved root filtering to prefer exact matches and validate against bookmark metadata
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
                System.Diagnostics.Debug.WriteLine($"Attach to '{b.ProcessName}' failed: {ex.Message} ({sw.ElapsedMilliseconds} ms)");
      
      // FIX: Early exit when process doesn't exist - skip all fallback strategies
        if (ex.Message != null && ex.Message.Contains("Unable to find process", StringComparison.OrdinalIgnoreCase))
 {
sb.AppendLine("Process not found - skipping all fallback root discovery strategies");
            attachInfo = sb.ToString().TrimEnd();
      return Array.Empty<AutomationElement>();
   }
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
                    else
                    {
                        // FIX: No process found by name - skip expensive desktop-wide scan
                        sb.AppendLine("No process found by name - skipping desktop-wide root scan");
                        attachInfo = sb.ToString().TrimEnd();
                        return Array.Empty<AutomationElement>();
                    }
                }
                catch (Exception ex) { sb.AppendLine("Name roots failed: " + ex.Message); }
            }

            if (roots.Length == 0)
            {
                try { roots = desktop.FindAllChildren(typeCond); sb.AppendLine($"Any Window/Pane roots: {roots.Length}"); }
                catch (Exception ex) { sb.AppendLine("Desktop Window/Pane failed: " + ex.Message); roots = Array.Empty<AutomationElement>(); }
            }

            // FIX: Prefer roots matching first node WITH stricter filtering
            var nodes = OrderNodes(b);
            var firstIncluded = nodes.FirstOrDefault(n => n.Include && n.LocateIndex > 0);
            if (firstIncluded != null && b.CrawlFromRoot && roots.Length > 0)
            {
                try
                {
                    // FIX: Filter current roots instead of re-scanning desktop
                    var filtered = roots.Where(r => ElementMatchesNode(r, firstIncluded)).ToArray();
                    if (filtered.Length > 0)
                    {
                        roots = filtered;
                        sb.AppendLine($"First-node filtered roots: {roots.Length} (exact match)");
                    }
                    else if (firstIncluded.UseControlTypeId)
                    {
                        // FIX: Fallback to relaxed match only if exact fails
                        var relaxed = CloneWithoutControlType(firstIncluded);
                        filtered = roots.Where(r => ElementMatchesNode(r, relaxed)).ToArray();
                        if (filtered.Length > 0)
                        {
                            roots = filtered;
                            sb.AppendLine($"First-node filtered roots: {roots.Length} (relaxed ControlType)");
                        }
                    }
                    
                    // FIX: Additional filtering by ClassName if provided and multiple matches remain
                    if (roots.Length > 1 && firstIncluded.UseClassName && !string.IsNullOrEmpty(firstIncluded.ClassName))
                    {
                        var classFiltered = roots.Where(r => string.Equals(SafeClass(r), firstIncluded.ClassName, StringComparison.Ordinal)).ToArray();
                        if (classFiltered.Length > 0)
                        {
                            roots = classFiltered;
                            sb.AppendLine($"ClassName filter applied: {roots.Length} roots remain");
                        }
                    }
                }
                catch (Exception ex) { sb.AppendLine("First-node filtering failed: " + ex.Message); }
            }
            
            // FIX: Sort roots by similarity to first node for deterministic ordering
            if (roots.Length > 1 && firstIncluded != null)
            {
                try
                {
                    roots = roots.OrderByDescending(r => CalculateNodeSimilarity(r, firstIncluded)).ToArray();
                    sb.AppendLine("Roots sorted by similarity to first node");
                }
                catch (Exception ex) { sb.AppendLine("Root sorting failed: " + ex.Message); }
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
    
    // FIX: Calculate similarity score for root prioritization
    private static int CalculateNodeSimilarity(AutomationElement e, Node node)
    {
        int score = 0;
        try
        {
            if (node.UseName && !string.IsNullOrEmpty(node.Name) && string.Equals(SafeName(e), node.Name, StringComparison.Ordinal)) score += 100;
            if (node.UseClassName && !string.IsNullOrEmpty(node.ClassName) && string.Equals(SafeClass(e), node.ClassName, StringComparison.Ordinal)) score += 50;
            if (node.UseAutomationId && !string.IsNullOrEmpty(node.AutomationId) && string.Equals(SafeAutoId(e), node.AutomationId, StringComparison.Ordinal)) score += 200;
            if (node.UseControlTypeId && node.ControlTypeId.HasValue)
            {
                try { if ((int)e.Properties.ControlType.Value == node.ControlTypeId.Value) score += 25; } catch { }
            }
        }
        catch { }
        return score;
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

    private static AutomationElement[] ManualFindMatches(AutomationElement current, Node node, SearchScope scope, UIA3Automation automation, bool preferRaw, StringBuilder? trace = null)
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
                    int visited = 0; 
                    // FIX: Use scope-specific caps - Children 10k, Descendants 5k (down from 100k)
                    int cap = scope == SearchScope.Children ? ManualWalkCapChildren : ManualWalkCapDescendants;
                    
                    // FIX: Add timeout protection for Descendants scope
                    var timeoutSw = Stopwatch.StartNew();
                    while (q.Count > 0 && visited < cap)
                    {
                        // FIX: Check timeout every 100 elements for Descendants scope
                        if (scope == SearchScope.Descendants && visited % 100 == 0)
                        {
                            if (timeoutSw.ElapsedMilliseconds > ManualWalkTimeoutMs)
                            {
                                trace?.AppendLine($"    Manual walker timeout after {timeoutSw.ElapsedMilliseconds}ms (visited {visited} elements)");
                                break;
                            }
                        }
                        
                        var el = q.Dequeue(); visited++;
                        if (Match(el)) list.Add(el);
                        foreach (var c in ChildrenOf(el)) q.Enqueue(c);
                    }
                    timeoutSw.Stop();
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