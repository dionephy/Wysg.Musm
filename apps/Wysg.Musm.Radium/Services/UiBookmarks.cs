using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using FlaUI.Core.Conditions;

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
        public bool Include { get; set; } = true;
        public bool UseName { get; set; } = false;
        public bool UseClassName { get; set; } = true;
        public bool UseControlTypeId { get; set; } = true;
        public bool UseAutomationId { get; set; } = true;
        public bool UseIndex { get; set; } = true;
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
            var json = File.ReadAllText(p);
            return JsonSerializer.Deserialize<Store>(json, _opts) ?? new Store();
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

    private static IEnumerable<ConditionBase> BuildConditions(Node node, ConditionFactory cf)
    {
        var list = new List<ConditionBase>();
        if (node.UseAutomationId && !string.IsNullOrEmpty(node.AutomationId)) list.Add(cf.ByAutomationId(node.AutomationId));
        if (node.UseClassName && !string.IsNullOrEmpty(node.ClassName)) list.Add(cf.ByClassName(node.ClassName));
        if (node.UseControlTypeId && node.ControlTypeId.HasValue) list.Add(cf.ByControlType((FlaUI.Core.Definitions.ControlType)node.ControlTypeId.Value));
        if (node.UseName && !string.IsNullOrEmpty(node.Name)) list.Add(cf.ByName(node.Name));
        if (list.Count == 0)
        {
            // Fallback to something to avoid matching everything. Prefer class or type if present
            if (!string.IsNullOrEmpty(node.ClassName)) list.Add(cf.ByClassName(node.ClassName));
            else if (node.ControlTypeId.HasValue) list.Add(cf.ByControlType((FlaUI.Core.Definitions.ControlType)node.ControlTypeId.Value));
        }
        // Build combined and also offer relaxed permutations by removing last terms
        for (int take = list.Count; take >= 1; take--)
        {
            ConditionBase cond = list[0];
            for (int i = 1; i < take; i++) cond = cond.And(list[i]);
            yield return cond;
        }
    }

    private static (IntPtr hwnd, AutomationElement? element) ResolveBookmark(Bookmark b)
    {
        try
        {
            using var app = Application.Attach(b.ProcessName);
            using var automation = new UIA3Automation();
            AutomationElement[] roots = app.GetAllTopLevelWindows(automation);
            if (roots == null || roots.Length == 0)
            {
                var mw = app.GetMainWindow(automation, TimeSpan.FromMilliseconds(800));
                roots = mw != null ? new AutomationElement[] { mw } : Array.Empty<AutomationElement>();
            }
            if (roots.Length == 0) return (IntPtr.Zero, null);

            var cf = new ConditionFactory(new UIA3PropertyLibrary());

            foreach (var root in roots)
            {
                AutomationElement current = root;
                if (b.Method == MapMethod.AutomationIdOnly && !string.IsNullOrEmpty(b.DirectAutomationId))
                {
                    var el = root.FindFirstDescendant(cf.ByAutomationId(b.DirectAutomationId));
                    if (el != null) return (new IntPtr(el.Properties.NativeWindowHandle.Value), el);
                    continue; // try next root
                }

                bool failed = false;
                foreach (var node in b.Chain)
                {
                    if (!node.Include) continue;
                    AutomationElement[] matches = Array.Empty<AutomationElement>();
                    foreach (var cond in BuildConditions(node, cf))
                    {
                        matches = current.FindAllDescendants(cond);
                        if (matches.Length > 0) break;
                    }
                    if (matches.Length == 0) { failed = true; break; }
                    int idx = node.UseIndex ? node.IndexAmongMatches : 0;
                    idx = Math.Max(0, Math.Min(idx, matches.Length - 1));
                    current = matches[idx];
                }
                if (!failed)
                {
                    var hwnd = new IntPtr(current.Properties.NativeWindowHandle.Value);
                    return (hwnd, current);
                }
            }
            return (IntPtr.Zero, null);
        }
        catch { return (IntPtr.Zero, null); }
    }
}