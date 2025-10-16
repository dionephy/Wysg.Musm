using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace Wysg.Musm.Radium.Services
{
    internal static class ProcedureExecutor
    {
        private static readonly HttpClient _http = new HttpClient();
        private static bool _encProviderRegistered;
        private static Func<string>? _getProcPathOverride;
        public static void SetProcPathOverride(Func<string> resolver) => _getProcPathOverride = resolver;
        private static void EnsureEncodingProviders()
        {
            if (_encProviderRegistered) return;
            try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); } catch { }
            _encProviderRegistered = true;
        }

        private static readonly Dictionary<UiBookmarks.KnownControl, AutomationElement> _controlCache = new();
        private static AutomationElement? GetCached(UiBookmarks.KnownControl key)
        {
            if (_controlCache.TryGetValue(key, out var el))
            {
                try { _ = el.Name; return el; } catch { _controlCache.Remove(key); }
            }
            return null;
        }
        private static void StoreCache(UiBookmarks.KnownControl key, AutomationElement el) { if (el != null) _controlCache[key] = el; }

        private sealed class ProcStore { public Dictionary<string, List<ProcOpRow>> Methods { get; set; } = new(); }
        private sealed class ProcOpRow
        {
            public string Op { get; set; } = string.Empty;
            public ProcArg Arg1 { get; set; } = new();
            public ProcArg Arg2 { get; set; } = new();
            public ProcArg Arg3 { get; set; } = new();
            public bool Arg1Enabled { get; set; } = true;
            public bool Arg2Enabled { get; set; } = true;
            public bool Arg3Enabled { get; set; } = false;
            public string? OutputVar { get; set; }
            public string? OutputPreview { get; set; }
        }
        private sealed class ProcArg { public string Type { get; set; } = "String"; public string? Value { get; set; } }

        private enum ArgKind { Element, String, Number, Var }

        private static string GetProcPath()
        {
            if (_getProcPathOverride != null)
            {
                try { return _getProcPathOverride(); } catch { }
            }
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wysg.Musm", "Radium");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "ui-procedures.json");
        }

        private static ProcStore Load()
        {
            try
            {
                var p = GetProcPath();
                if (!File.Exists(p)) return new ProcStore();
                return JsonSerializer.Deserialize<ProcStore>(File.ReadAllText(p), new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? new ProcStore();
            }
            catch { return new ProcStore(); }
        }

        private static void Save(ProcStore s)
        {
            try
            {
                var p = GetProcPath();
                File.WriteAllText(p, JsonSerializer.Serialize(s, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
            }
            catch { }
        }

        public static Task<string?> ExecuteAsync(string methodTag) => Task.Run(() => ExecuteInternal(methodTag));

        private static string? ExecuteInternal(string methodTag)
        {
            if (string.IsNullOrWhiteSpace(methodTag)) return null;
            var store = Load();

            if (!store.Methods.TryGetValue(methodTag, out var steps) || steps.Count == 0)
            {
                // No implicit fallback for InvokeOpenStudy; require explicit authoring
                if (string.Equals(methodTag, "InvokeOpenStudy", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Custom procedure 'InvokeOpenStudy' is not defined. Please configure it in SpyWindow for this PACS profile.");

                steps = TryCreateFallbackProcedure(methodTag);
                if (steps.Count > 0) { store.Methods[methodTag] = steps; Save(store); }
                else return null;
            }

            var vars = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            string? last = null;
            for (int i = 0; i < steps.Count; i++)
            {
                var row = steps[i];
                var (preview, value) = ExecuteRow(row, vars);
                var implicitKey = $"var{i + 1}";
                vars[implicitKey] = value;
                if (!string.IsNullOrWhiteSpace(row.OutputVar)) vars[row.OutputVar!] = value;
                if (value != null) last = value;
            }
            return last;
        }

        private static List<ProcOpRow> TryCreateFallbackProcedure(string methodTag)
        {
            // Keep selective fallbacks for non-critical getters; do NOT fallback for InvokeOpenStudy.
            if (string.Equals(methodTag, "GetCurrentPatientRemark", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    new ProcOpRow { Op = "GetText", Arg1 = new ProcArg { Type = nameof(ArgKind.Element), Value = UiBookmarks.KnownControl.PatientRemark.ToString() }, Arg1Enabled = true, Arg2Enabled = false, Arg3Enabled = false, OutputVar = "var1" }
                };
            }
            if (string.Equals(methodTag, "GetCurrentStudyRemark", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    new ProcOpRow { Op = "GetText", Arg1 = new ProcArg { Type = nameof(ArgKind.Element), Value = UiBookmarks.KnownControl.StudyRemark.ToString() }, Arg1Enabled = true, Arg2Enabled = false, Arg3Enabled = false, OutputVar = "var1" }
                };
            }
            // IMPORTANT: No fallback for InvokeOpenStudy (explicit procedure required). Return empty.
            if (string.Equals(methodTag, "InvokeOpenStudy", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>();
            }
            if (string.Equals(methodTag, "CustomMouseClick1", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    new ProcOpRow { Op = "MouseClick", Arg1 = new ProcArg { Type = nameof(ArgKind.Number), Value = "0" }, Arg2 = new ProcArg { Type = nameof(ArgKind.Number), Value = "0" }, Arg1Enabled = true, Arg2Enabled = true, Arg3Enabled = false }
                };
            }
            if (string.Equals(methodTag, "CustomMouseClick2", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    new ProcOpRow { Op = "MouseClick", Arg1 = new ProcArg { Type = nameof(ArgKind.Number), Value = "0" }, Arg2 = new ProcArg { Type = nameof(ArgKind.Number), Value = "0" }, Arg1Enabled = true, Arg2Enabled = true, Arg3Enabled = false }
                };
            }
            if (string.Equals(methodTag, "InvokeTest", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    new ProcOpRow { Op = "Invoke", Arg1 = new ProcArg { Type = nameof(ArgKind.Element), Value = UiBookmarks.KnownControl.TestInvoke.ToString() }, Arg1Enabled = true, Arg2Enabled = false, Arg3Enabled = false }
                };
            }
            if (string.Equals(methodTag, "SetCurrentStudyInMainScreen", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    new ProcOpRow { Op = "ClickElement", Arg1 = new ProcArg { Type = nameof(ArgKind.Element), Value = UiBookmarks.KnownControl.Screen_MainCurrentStudyTab.ToString() }, Arg1Enabled = true, Arg2Enabled = false, Arg3Enabled = false }
                };
            }
            if (string.Equals(methodTag, "SetPreviousStudyInSubScreen", StringComparison.OrdinalIgnoreCase))
            {
                return new List<ProcOpRow>
                {
                    new ProcOpRow { Op = "ClickElement", Arg1 = new ProcArg { Type = nameof(ArgKind.Element), Value = UiBookmarks.KnownControl.Screen_SubPreviousStudyTab.ToString() }, Arg1Enabled = true, Arg2Enabled = false, Arg3Enabled = false }
                };
            }
            return new List<ProcOpRow>();
        }

        private static string UnescapeUserText(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            try { return Regex.Unescape(s); } catch { return s; }
        }

        private static (string preview, string? value) ExecuteRow(ProcOpRow row, Dictionary<string, string?> vars)
        {
            string? valueToStore = null;
            string preview;
            switch (row.Op)
            {
                case "Split":
                {
                    var input = ResolveString(row.Arg1, vars);
                    var sepRaw = ResolveString(row.Arg2, vars) ?? string.Empty;
                    var indexStr = ResolveString(row.Arg3, vars);
                    if (input == null) { return ("(null)", null); }

                    string[] parts;
                    if (sepRaw.StartsWith("re:", StringComparison.OrdinalIgnoreCase) || sepRaw.StartsWith("regex:", StringComparison.OrdinalIgnoreCase))
                    {
                        var pattern = sepRaw.StartsWith("re:", StringComparison.OrdinalIgnoreCase) ? sepRaw.Substring(3) : sepRaw.Substring(6);
                        if (string.IsNullOrEmpty(pattern)) { return ("(empty pattern)", null); }
                        try { parts = Regex.Split(input, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase); }
                        catch (Exception ex) { return ($"(regex error: {ex.Message})", null); }
                    }
                    else
                    {
                        var sep = UnescapeUserText(sepRaw);
                        parts = input.Split(new[] { sep }, StringSplitOptions.None);
                        if (parts.Length == 1 && sep.Contains('\n') && !sep.Contains("\r\n"))
                        {
                            var crlfSep = sep.Replace("\n", "\r\n");
                            parts = input.Split(new[] { crlfSep }, StringSplitOptions.None);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(indexStr) && int.TryParse(indexStr.Trim(), out var idx))
                    {
                        if (idx >= 0 && idx < parts.Length) { valueToStore = parts[idx]; preview = valueToStore ?? string.Empty; }
                        else { preview = $"(index out of range {parts.Length})"; }
                    }
                    else
                    {
                        valueToStore = string.Join("\u001F", parts);
                        preview = parts.Length + " parts";
                    }
                    return (preview, valueToStore);
                }
                case "GetText":
                case "GetTextOCR":
                case "Invoke":
                case "ClickElement":
                case "MouseClick":
                case "GetValueFromSelection":
                case "ToDateTime":
                case "TakeLast":
                case "Trim":
                case "Replace":
                case "GetHTML":
                    return ExecuteElemental(row, vars);
                default:
                    return ("(unsupported)", null);
            }
        }

        private static (string preview, string? value) ExecuteElemental(ProcOpRow row, Dictionary<string, string?> vars)
        {
            try
            {
                switch (row.Op)
                {
                    case "GetText":
                    {
                        var el = ResolveElement(row.Arg1);
                        if (el == null) return ("(no element)", null);
                        try
                        {
                            var name = el.Name;
                            var val = el.Patterns.Value.PatternOrDefault?.Value ?? string.Empty;
                            var legacy = el.Patterns.LegacyIAccessible.PatternOrDefault?.Name ?? string.Empty;
                            var txt = !string.IsNullOrEmpty(val) ? val : (!string.IsNullOrEmpty(name) ? name : legacy);
                            return (txt ?? string.Empty, txt);
                        }
                        catch { return ("(error)", null); }
                    }
                    case "GetTextOCR":
                    {
                        var el = ResolveElement(row.Arg1);
                        if (el == null) return ("(no element)", null);
                        try
                        {
                            var r = el.BoundingRectangle; if (r.Width <= 0 || r.Height <= 0) return ("(no bounds)", null);
                            var hwnd = new IntPtr(el.Properties.NativeWindowHandle.Value); if (hwnd == IntPtr.Zero) return ("(no hwnd)", null);
                            var (engine, text) = Wysg.Musm.MFCUIA.OcrReader.OcrTryReadRegionDetailedAsync(hwnd, new System.Drawing.Rectangle(0,0,(int)r.Width,(int)r.Height)).ConfigureAwait(false).GetAwaiter().GetResult();
                            if (!engine) return ("(ocr unavailable)", null);
                            return (string.IsNullOrWhiteSpace(text) ? "(empty)" : text!, text);
                        }
                        catch { return ("(error)", null); }
                    }
                    case "Invoke":
                    {
                        var el = ResolveElement(row.Arg1);
                        if (el == null) return ("(no element)", null);
                        try { var inv = el.Patterns.Invoke.PatternOrDefault; if (inv != null) inv.Invoke(); else el.Patterns.Toggle.PatternOrDefault?.Toggle(); return ("(invoked)", null); }
                        catch { return ("(error)", null); }
                    }
                    case "TakeLast":
                    {
                        var combined = ResolveString(row.Arg1, vars) ?? string.Empty;
                        var arr = combined.Split('\u001F'); var value = arr.Length > 0 ? arr[^1] : string.Empty;
                        return (value, value);
                    }
                    case "Trim":
                    {
                        var s = ResolveString(row.Arg1, vars); var v = s?.Trim(); return (v ?? "(null)", v);
                    }
                    case "Replace":
                    {
                        var input = ResolveString(row.Arg1, vars) ?? string.Empty;
                        var search = Regex.Unescape(ResolveString(row.Arg2, vars) ?? string.Empty);
                        var repl = Regex.Unescape(ResolveString(row.Arg3, vars) ?? string.Empty);
                        if (string.IsNullOrEmpty(search)) return (input, input);
                        var output = input.Replace(search, repl);
                        return (output, output);
                    }
                    case "GetValueFromSelection":
                    {
                        var el = ResolveElement(row.Arg1); var headerWanted = row.Arg2?.Value ?? "ID"; if (string.IsNullOrWhiteSpace(headerWanted)) headerWanted = "ID";
                        if (el == null) return ("(no element)", null);
                        try
                        {
                            var selection = el.Patterns.Selection.PatternOrDefault;
                            var selected = selection?.Selection?.Value ?? Array.Empty<AutomationElement>();
                            if (selected.Length == 0)
                            {
                                selected = el.FindAllDescendants().Where(a => { try { return a.Patterns.SelectionItem.IsSupported && a.Patterns.SelectionItem.PatternOrDefault?.IsSelected == true; } catch { return false; } }).ToArray();
                            }
                            if (selected.Length == 0) return ("(no selection)", null);
                            var rowEl = selected[0];
                            var headers = SpyHeaderHelpers.GetHeaderTexts(el);
                            var cells = SpyHeaderHelpers.GetRowCellValues(rowEl);
                            if (headers.Count < cells.Count) for (int j = headers.Count; j < cells.Count; j++) headers.Add($"Col{j + 1}");
                            else if (headers.Count > cells.Count) for (int j = cells.Count; j < headers.Count; j++) cells.Add(string.Empty);
                            string? matched = null;
                            for (int j = 0; j < headers.Count; j++) if (string.Equals(SpyHeaderHelpers.NormalizeHeader(headers[j]), headerWanted, StringComparison.OrdinalIgnoreCase)) { matched = cells[j]; break; }
                            if (matched == null)
                                for (int j = 0; j < headers.Count; j++) if (SpyHeaderHelpers.NormalizeHeader(headers[j]).IndexOf(headerWanted, StringComparison.OrdinalIgnoreCase) >= 0) { matched = cells[j]; break; }
                            if (matched == null) return ($"({headerWanted} not found)", null);
                            return (matched, matched);
                        }
                        catch { return ("(error)", null); }
                    }
                    case "ToDateTime":
                    {
                        var s = ResolveString(row.Arg1, vars); if (string.IsNullOrWhiteSpace(s)) return ("(null)", null);
                        if (DateTime.TryParse(s.Trim(), out var dt)) { var iso = dt.ToString("o"); return (dt.ToString("yyyy-MM-dd HH:mm:ss"), iso); }
                        return ("(parse fail)", null);
                    }
                    case "GetHTML":
                    {
                        var url = ResolveString(row.Arg1, vars);
                        if (string.IsNullOrWhiteSpace(url) || !(url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                            return ("(no url)", null);
                        try
                        {
                            EnsureEncodingProviders();
                            using var resp = _http.GetAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
                            resp.EnsureSuccessStatusCode();
                            var bytes = resp.Content.ReadAsByteArrayAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                            var charset = resp.Content.Headers.ContentType?.CharSet;
                            var html = DecodeHtml(bytes, charset);
                            return (html ?? string.Empty, html);
                        }
                        catch (Exception ex) { return ($"(error) {ex.Message}", null); }
                    }
                    case "MouseClick":
                    {
                        var xs = ResolveString(row.Arg1, vars); var ys = ResolveString(row.Arg2, vars);
                        if (!int.TryParse(xs, out var x) || !int.TryParse(ys, out var y)) return ("(invalid coords)", null);
                        try { NativeMouseHelper.ClickScreen(x, y); return ($"(clicked {x},{y})", null); }
                        catch { return ("(error)", null); }
                    }
                    case "ClickElement":
                    {
                        var el = ResolveElement(row.Arg1);
                        if (el == null) return ("(no element)", null);
                        try
                        {
                            var rect = el.BoundingRectangle;
                            if (rect.Width <= 0 || rect.Height <= 0) return ("(no bounds)", null);
                            
                            int centerX = (int)(rect.Left + rect.Width / 2);
                            int centerY = (int)(rect.Top + rect.Height / 2);
                            
                            NativeMouseHelper.ClickScreenWithRestore(centerX, centerY);
                            return ($"(clicked element center {centerX},{centerY})", null);
                        }
                        catch (Exception ex) { return ($"(error: {ex.Message})", null); }
                    }
                }
            }
            catch { }
            return ("(unsupported)", null);
        }

        private static string DecodeHtml(byte[] bytes, string? headerCharset)
        {
            try
            {
                Encoding enc = Encoding.UTF8;
                if (!string.IsNullOrWhiteSpace(headerCharset)) { try { enc = Encoding.GetEncoding(headerCharset!); } catch { enc = Encoding.UTF8; } }
                var text = enc.GetString(bytes);
                var sample = text.Length > 8192 ? text.Substring(0, 8192) : text;
                var m = Regex.Match(sample, "charset=([^\"'>]+)", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var cs = m.Groups[1].Value.Trim().TrimEnd(';');
                    try { var e2 = Encoding.GetEncoding(cs); text = e2.GetString(bytes); } catch { }
                }
                return text;
            }
            catch { return Encoding.UTF8.GetString(bytes); }
        }

        // Element resolution with staleness detection and retry (inspired by legacy PacsService validation pattern)
        private const int ElementResolveMaxAttempts = 3;
        private const int ElementResolveRetryDelayMs = 150;

        private static AutomationElement? ResolveElement(ProcArg arg)
        {
            if (ParseArgKind(arg.Type) != ArgKind.Element) return null;
            var tag = arg.Value ?? string.Empty;
            if (!Enum.TryParse<UiBookmarks.KnownControl>(tag, out var key)) return null;

            // Strategy: Try cache first, validate it, then resolve fresh with retry on staleness
            for (int attempt = 0; attempt < ElementResolveMaxAttempts; attempt++)
            {
                // Attempt 1: Check cache
                var cached = GetCached(key);
                if (cached != null)
                {
                    if (IsElementAlive(cached))
                    {
                        return cached; // Cache hit, element valid
                    }
                    else
                    {
                        // Stale element in cache, remove it
                        _controlCache.Remove(key);
                    }
                }

                // Attempt 2: Resolve fresh from bookmark
                try
                {
                    var tuple = UiBookmarks.Resolve(key);
                    if (tuple.element != null)
                    {
                        // Validate the newly resolved element before caching
                        if (IsElementAlive(tuple.element))
                        {
                            StoreCache(key, tuple.element);
                            return tuple.element;
                        }
                        // Element resolved but immediately stale (rare UI timing issue)
                    }
                }
                catch
                {
                    // Resolve failed, will retry
                }

                // If not last attempt, wait before retry (exponential backoff)
                if (attempt < ElementResolveMaxAttempts - 1)
                {
                    System.Threading.Thread.Sleep(ElementResolveRetryDelayMs * (attempt + 1));
                }
            }

            // All attempts exhausted
            return null;
        }

        /// <summary>
        /// Validates that an AutomationElement is still alive and accessible.
        /// Inspired by legacy PacsService validation pattern: try to access a property and catch exceptions.
        /// </summary>
        private static bool IsElementAlive(AutomationElement element)
        {
            try
            {
                // Attempt to access a lightweight property to validate element is still in UI tree
                _ = element.Name;
                return true;
            }
            catch
            {
                // Element is stale (UI element no longer exists or accessible)
                return false;
            }
        }

        private static string? ResolveString(ProcArg arg, Dictionary<string, string?> vars)
        {
            var type = ParseArgKind(arg.Type);
            return type switch
            {
                ArgKind.Var => (arg.Value != null && vars.TryGetValue(arg.Value, out var v)) ? v : null,
                ArgKind.String => arg.Value,
                ArgKind.Number => arg.Value,
                ArgKind.Element => null,
                _ => null
            };
        }

        private static ArgKind ParseArgKind(string? s)
        {
            if (Enum.TryParse<ArgKind>(s, true, out var k)) return k;
            return s?.Equals("Var", StringComparison.OrdinalIgnoreCase) == true ? ArgKind.Var :
                   s?.Equals("Element", StringComparison.OrdinalIgnoreCase) == true ? ArgKind.Element :
                   s?.Equals("Number", StringComparison.OrdinalIgnoreCase) == true ? ArgKind.Number : ArgKind.String;
        }

        private static class SpyHeaderHelpers
        {
            public static string NormalizeHeader(string h)
            {
                h = (h ?? string.Empty).Trim();
                if (string.Equals(h, "Accession", StringComparison.OrdinalIgnoreCase)) return "Accession No.";
                if (string.Equals(h, "Study Description", StringComparison.OrdinalIgnoreCase)) return "Study Desc";
                if (string.Equals(h, "Institution Name", StringComparison.OrdinalIgnoreCase)) return "Institution";
                if (string.Equals(h, "BirthDate", StringComparison.OrdinalIgnoreCase)) return "Birth Date";
                if (string.Equals(h, "BodyPart", StringComparison.OrdinalIgnoreCase)) return "Body Part";
                return h;
            }
            public static List<string> GetHeaderTexts(AutomationElement list)
            {
                var headers = new List<string>();
                try
                {
                    var kids = list.FindAllChildren();
                    if (kids.Length > 0)
                    {
                        var headerRow = kids[0];
                        var cells = headerRow.FindAllChildren();
                        foreach (var c in cells)
                        {
                            string txt = TryRead(c);
                            if (string.IsNullOrWhiteSpace(txt))
                            {
                                foreach (var g in c.FindAllChildren()) { txt = TryRead(g); if (!string.IsNullOrWhiteSpace(txt)) break; }
                            }
                            headers.Add(string.IsNullOrWhiteSpace(txt) ? string.Empty : txt.Trim());
                        }
                    }
                }
                catch { }
                return headers;
            }
            public static List<string> GetRowCellValues(AutomationElement row)
            {
                var vals = new List<string>();
                try
                {
                    var children = row.FindAllChildren();
                    foreach (var c in children)
                    {
                        string txt = TryRead(c).Trim();
                        if (string.IsNullOrEmpty(txt))
                        {
                            foreach (var gc in c.FindAllChildren()) { var t = TryRead(gc).Trim(); if (!string.IsNullOrEmpty(t)) { txt = t; break; } }
                        }
                        vals.Add(txt);
                    }
                }
                catch { }
                return vals;
            }
            private static string TryRead(AutomationElement el)
            {
                try
                {
                    var vp = el.Patterns.Value.PatternOrDefault; if (vp != null && vp.Value.TryGetValue(out var v) && !string.IsNullOrWhiteSpace(v)) return v;
                }
                catch { }
                try { var n = el.Name; if (!string.IsNullOrWhiteSpace(n)) return n; } catch { }
                try { var l = el.Patterns.LegacyIAccessible.PatternOrDefault?.Name; if (!string.IsNullOrWhiteSpace(l)) return l; } catch { }
                return string.Empty;
            }
        }
    }
}
