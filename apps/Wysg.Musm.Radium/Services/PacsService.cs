using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Automation;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.UIA3;
using Wysg.Musm.MFCUIA;
using Wysg.Musm.MFCUIA.Core.Controls;
using Wysg.Musm.MFCUIA.Selectors;
using Wysg.Musm.MFCUIA.Session;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Linq;
using System.Collections.Generic;
using UIAElement = FlaUI.Core.AutomationElements.AutomationElement;
using System.Text.RegularExpressions;

namespace Wysg.Musm.Radium.Services
{
    // Slim PACS automation using the new MFCUIA library + FlaUI helpers
    public sealed class PacsService
    {
        private readonly string _proc;
        private IDisposable? _watcher;
        private IntPtr _cachedBannerHwnd = IntPtr.Zero;
        private DateTime _cachedAt;

        [DllImport("user32.dll", SetLastError = true)] private static extern bool IsWindow(IntPtr hWnd);

        public PacsService(string processName = "INFINITT") => _proc = processName;

        public async Task OpenWorklistAsync()
        {
            using var mfc = MfcUi.Attach(_proc);
            _ = mfc.Command(1106).Invoke();
            await Task.Delay(200);
            await Task.Delay(200);
        }

        public async Task<string[]> GetSelectedStudyAsync()
        {
            using var mfc = MfcUi.Attach(_proc);
            var list = mfc.Find(By.ClassNth("SysListView32", 0)).AsListView();
            var items = list.GetSelectedRow(3, 4, 7, 2, 9, 12, 15, 17, 13, 16);
            return items;
        }

        /// <summary>
        /// Locate the mapped SearchResultsList and return the "ID" value of the currently selected row.
        /// If header "ID" is not found, tries a header containing "ID" or "Accession".
        /// Returns null when list/selection not found or value empty.
        /// </summary>
        public Task<string?> GetSelectedIdFromSearchResultsAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    return GetValueFromListSelection(UiBookmarks.KnownControl.SearchResultsList, new[] { "ID", "Accession", "Accession No." });
                }
                catch { return (string?)null; }
            });
        }

        public Task<string?> GetSelectedNameFromSearchResultsAsync() => Task.Run(() => GetValueFromListSelection(UiBookmarks.KnownControl.SearchResultsList, new[] { "Name" }));
        public Task<string?> GetSelectedSexFromSearchResultsAsync() => Task.Run(() => GetValueFromListSelection(UiBookmarks.KnownControl.SearchResultsList, new[] { "Sex" }));
        public Task<string?> GetSelectedBirthDateFromSearchResultsAsync() => Task.Run(() => GetValueFromListSelection(UiBookmarks.KnownControl.SearchResultsList, new[] { "Birth Date", "BirthDate" }));
        public Task<string?> GetSelectedAgeFromSearchResultsAsync() => Task.Run(() => GetValueFromListSelection(UiBookmarks.KnownControl.SearchResultsList, new[] { "Age" }));
        public Task<string?> GetSelectedStudynameFromSearchResultsAsync() => Task.Run(() => GetValueFromListSelection(UiBookmarks.KnownControl.SearchResultsList, new[] { "Study Desc", "Study Description", "Studyname", "Study Name" }));
        public Task<string?> GetSelectedStudyDateTimeFromSearchResultsAsync() => Task.Run(() => GetValueFromListSelection(UiBookmarks.KnownControl.SearchResultsList, new[] { "Study Date", "Study Date Time", "Study DateTime" }));
        public Task<string?> GetSelectedRadiologistFromSearchResultsAsync() => Task.Run(() => GetValueFromListSelection(UiBookmarks.KnownControl.SearchResultsList, new[] { "Requesting Doctor", "Radiologist", "Doctor" }));
        public Task<string?> GetSelectedStudyRemarkFromSearchResultsAsync() => Task.Run(() => GetValueFromListSelection(UiBookmarks.KnownControl.SearchResultsList, new[] { "Study Comments", "Remark", "Comments" }));
        public Task<string?> GetSelectedReportDateTimeFromSearchResultsAsync() => Task.Run(() => GetValueFromListSelection(UiBookmarks.KnownControl.SearchResultsList, new[] { "Report approval dttm", "Report Date", "Report Date Time" }));

        public Task<string?> GetSelectedStudynameFromRelatedStudiesAsync() => Task.Run(() => GetValueFromListSelection(UiBookmarks.KnownControl.RelatedStudyList, new[] { "Study Desc", "Study Description", "Studyname", "Study Name" }));
        public Task<string?> GetSelectedStudyDateTimeFromRelatedStudiesAsync() => Task.Run(() => GetValueFromListSelection(UiBookmarks.KnownControl.RelatedStudyList, new[] { "Study Date", "Study Date Time", "Study DateTime" }));
        public Task<string?> GetSelectedRadiologistFromRelatedStudiesAsync() => Task.Run(() => GetValueFromListSelection(UiBookmarks.KnownControl.RelatedStudyList, new[] { "Requesting Doctor", "Radiologist", "Doctor" }));
        public Task<string?> GetSelectedReportDateTimeFromRelatedStudiesAsync() => Task.Run(() => GetValueFromListSelection(UiBookmarks.KnownControl.RelatedStudyList, new[] { "Report approval dttm", "Report Date", "Report Date Time" }));

        // NEW: Current patient number parsed from viewer banner (OCR/text) heuristics.
        public Task<string?> GetCurrentPatientNumberAsync() => Task.Run(() =>
        {
            try
            {
                var (_, banner) = TryAutoLocateBanner();
                if (string.IsNullOrWhiteSpace(banner)) return (string?)null;
                // Heuristic: patient number often first token / digits cluster before first comma
                // Extract longest digit sequence of length >=5
                var match = Regex.Matches(banner, "[0-9]{5,}").Cast<Match>().OrderByDescending(m => m.Value.Length).FirstOrDefault();
                return match?.Value;
            }
            catch { return (string?)null; }
        });

        // NEW: Current study date time parsed from banner tokens (YYYY-MM-DD or with time)
        public Task<string?> GetCurrentStudyDateTimeAsync() => Task.Run(() =>
        {
            try
            {
                var (_, banner) = TryAutoLocateBanner();
                if (string.IsNullOrWhiteSpace(banner)) return (string?)null;
                // Look for date pattern
                var dateMatch = Regex.Match(banner, @"(20[0-9]{2}[-/][01][0-9][-/.][0-3][0-9])");
                if (!dateMatch.Success) return (string?)null;
                var date = dateMatch.Groups[1].Value.Replace('/', '-').Replace('.', '-');
                // Optional time immediately after
                var timeMatch = Regex.Match(banner.Substring(dateMatch.Index + dateMatch.Length), @"\s+([0-2][0-9]:[0-5][0-9](?::[0-5][0-9])?)");
                if (timeMatch.Success)
                {
                    return date + " " + timeMatch.Groups[1].Value;
                }
                return date;
            }
            catch { return (string?)null; }
        });

        private static string? GetValueFromListSelection(UiBookmarks.KnownControl control, string[] headerCandidates)
        {
            try
            {
                var (_, list) = UiBookmarks.Resolve(control);
                if (list == null) return null;
                var selection = list.Patterns.Selection.PatternOrDefault;
                var selected = selection?.Selection?.Value ?? Array.Empty<UIAElement>();
                if (selected.Length == 0)
                {
                    selected = list.FindAllDescendants().Where(a =>
                    {
                        try { return a.Patterns.SelectionItem.IsSupported && a.Patterns.SelectionItem.PatternOrDefault?.IsSelected == true; }
                        catch { return false; }
                    }).ToArray();
                }
                if (selected.Length == 0) return null;
                var row = selected[0];
                var headers = GetHeaderTexts(list);
                var cells = GetRowCellValues(row);
                if (headers.Count < cells.Count) for (int i = headers.Count; i < cells.Count; i++) headers.Add($"Col{i + 1}");
                else if (headers.Count > cells.Count) for (int i = cells.Count; i < headers.Count; i++) cells.Add(string.Empty);
                for (int i = 0; i < headerCandidates.Length; i++)
                {
                    var wanted = headerCandidates[i];
                    int idx = FindHeaderIndex(headers, wanted);
                    if (idx >= 0 && idx < cells.Count)
                    {
                        var val = cells[idx]?.Trim();
                        if (!string.IsNullOrWhiteSpace(val)) return val;
                    }
                }
                return null;
            }
            catch { return null; }
        }

        private static int FindHeaderIndex(List<string> headers, string wanted)
        {
            for (int i = 0; i < headers.Count; i++)
            {
                var h = headers[i] ?? string.Empty;
                if (string.Equals(h, wanted, StringComparison.OrdinalIgnoreCase)) return i;
            }
            for (int i = 0; i < headers.Count; i++)
            {
                var h = headers[i] ?? string.Empty;
                if (h.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0) return i;
            }
            return -1;
        }

        private static string ReadCellText(UIAElement cell)
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
            try { var n = cell.Name; if (!string.IsNullOrWhiteSpace(n)) return n; } catch { }
            try { var l = cell.Patterns.LegacyIAccessible.PatternOrDefault?.Name; if (!string.IsNullOrWhiteSpace(l)) return l; } catch { }
            return string.Empty;
        }

        private static List<string> GetHeaderTexts(UIAElement list)
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

                // Fallback: look for any child that appears to be header-like
                foreach (var ch in list.FindAllDescendants())
                {
                    try
                    {
                        var name = ReadCellText(ch);
                        if (!string.IsNullOrWhiteSpace(name) && name.Length < 40)
                        {
                            // heuristic placeholder
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return result;
        }

        private static List<string> GetRowCellValues(UIAElement row)
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
                            if (string.IsNullOrEmpty(txt))
                            {
                                foreach (var gc in c.FindAllChildren())
                                {
                                    var t = ReadCellText(gc).Trim();
                                    if (!string.IsNullOrEmpty(t)) { txt = t; break; }
                                }
                            }
                            values.Add(txt);
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
                            var t = ReadCellText(d).Trim();
                            values.Add(t);
                            if (values.Count >= 20) break;
                        }
                        catch { }
                    }
                }
            }
            catch { }
            return values;
        }

        public async Task<bool> IsViewerWindowAsync(IntPtr hwnd)
        {
            using var mfc = MfcUi.Attach(_proc);
            return mfc.Process != null;
        }

        public string? TryReadViewerBannerFromPane(IntPtr paneHwnd)
        {
            var title = TitleBarReader.TryGetTitleFromPaneHwnd(paneHwnd);
            var scanned = ChildTextScanner.TryReadBannerFromPane(paneHwnd);
            return !string.IsNullOrWhiteSpace(scanned) ? scanned : title;
        }

        public string? TryReadFirstViewerBanner()
        {
            MfcUi mfc;
            try { mfc = MfcUi.Attach(_proc); }
            catch { mfc = MfcUi.AttachByTopLevel(s => (!string.IsNullOrEmpty(s.Title) && s.Title.Contains("INFINITT", StringComparison.OrdinalIgnoreCase)) || (!string.IsNullOrEmpty(s.ClassName) && s.ClassName.Contains("Afx", StringComparison.OrdinalIgnoreCase))); }

            using (mfc)
            {
                var el = mfc.FindInAnyTop(By.ClassNth("AfxFrameOrView140u", 0));
                if (el.Hwnd == IntPtr.Zero)
                    el = mfc.FindInAnyTop(By.ClassNth("AfxWnd140u", 0));
                if (el.Hwnd == IntPtr.Zero) return null;

                var scanned = ChildTextScanner.TryReadBannerFromPane(el.Hwnd);
                if (!string.IsNullOrWhiteSpace(scanned)) return scanned;
                return TitleBarReader.TryGetTitleFromPaneHwnd(el.Hwnd);
            }
        }

        // FlaUI-based robust discovery with caching
        public (IntPtr hwnd, string? text) TryAutoLocateBanner()
        {
            // 0) If we have a cached hwnd, verify it is still valid and belongs to the process
            if (_cachedBannerHwnd != IntPtr.Zero && IsWindow(_cachedBannerHwnd))
            {
                var txt = TryReadViewerBannerFromPane(_cachedBannerHwnd);
                if (!string.IsNullOrWhiteSpace(txt)) return (_cachedBannerHwnd, txt);
            }

            try
            {
                using var app = Application.Attach(_proc);
                using var automation = new UIA3Automation();
                var main = app.GetMainWindow(automation, TimeSpan.FromMilliseconds(500));
                if (main == null) return (IntPtr.Zero, null);

                // Search for a Pane with class AfxFrameOrView140u within top 140 px and wide enough
                var panes = main.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Pane)
                                                          .And(cf.ByClassName("AfxFrameOrView140u")));
                foreach (var p in panes)
                {
                    var r = p.BoundingRectangle;
                    if (r.Height <= 0 || r.Width < 400) continue;
                    if (r.Top - main.BoundingRectangle.Top > 200) continue;
                    var hwnd = new IntPtr(p.Properties.NativeWindowHandle.Value);
                    var txt = TryReadViewerBannerFromPane(hwnd);
                    if (!string.IsNullOrWhiteSpace(txt))
                    {
                        _cachedBannerHwnd = hwnd; _cachedAt = DateTime.UtcNow;
                        return (hwnd, txt);
                    }
                }

                // fallback: any Pane with AfxWnd140u
                var panes2 = main.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Pane)
                                                           .And(cf.ByClassName("AfxWnd140u")));
                foreach (var p in panes2)
                {
                    var r = p.BoundingRectangle;
                    if (r.Height <= 0 || r.Width < 400) continue;
                    if (r.Top - main.BoundingRectangle.Top > 200) continue;
                    var hwnd = new IntPtr(p.Properties.NativeWindowHandle.Value);
                    var txt = TryReadViewerBannerFromPane(hwnd);
                    if (!string.IsNullOrWhiteSpace(txt))
                    {
                        _cachedBannerHwnd = hwnd; _cachedAt = DateTime.UtcNow;
                        return (hwnd, txt);
                    }
                }
            }
            catch { }

            // MFC fallback
            try
            {
                using var mfc = MfcUi.AttachByTopLevel(s => (!string.IsNullOrEmpty(s.Title) && s.Title.Contains("INFINITT", StringComparison.OrdinalIgnoreCase)) || (!string.IsNullOrEmpty(s.ClassName) && s.ClassName.Contains("Afx", StringComparison.OrdinalIgnoreCase)));
                var el = mfc.FindInAnyTop(By.ClassNth("AfxFrameOrView140u", 0));
                if (el.Hwnd == IntPtr.Zero) el = mfc.FindInAnyTop(By.ClassNth("AfxWnd140u", 0));
                if (el.Hwnd != IntPtr.Zero)
                {
                    var text = TryReadViewerBannerFromPane(el.Hwnd);
                    if (!string.IsNullOrWhiteSpace(text)) { _cachedBannerHwnd = el.Hwnd; _cachedAt = DateTime.UtcNow; }
                    return (el.Hwnd, text);
                }
            }
            catch { }

            return (IntPtr.Zero, null);
        }

        private static bool IsHwndAlive(IntPtr hwnd)
        {
            try
            {
                return hwnd != IntPtr.Zero && IsWindow(hwnd);
            }
            catch { return false; }
        }

        // Presets for OCR region (relative to window top-left)
        public enum OcrRegionPreset { TopThin, TopMedium, TopWideCenter }
        public (int L, int T, int W, int H) GetOcrRegionPreset(IntPtr hwnd, OcrRegionPreset preset)
        {
            if (!OcrReader.TryGetWindowRect(hwnd, out var wr)) return (0,0,100,50);
            return preset switch
            {
                OcrRegionPreset.TopThin => (20, 8, Math.Max(200, wr.Width - 40), 50),
                OcrRegionPreset.TopMedium => (20, 12, Math.Max(200, wr.Width - 40), 90),
                OcrRegionPreset.TopWideCenter => (wr.Width/8, 10, wr.Width*3/4, 80),
                _ => (20, 10, Math.Max(200, wr.Width - 40), 70)
            };
        }

        // Quality/speed knobs
        public async Task<(bool engineAvailable, string? text)> OcrReadWithPresetAsync(IntPtr hwnd, OcrRegionPreset preset)
        {
            var (l,t,w,h) = GetOcrRegionPreset(hwnd, preset);
            return await OcrReader.OcrTryReadRegionDetailedAsync(hwnd, new Rectangle(l,t,w,h));
        }

        public Task<string?> OcrReadTopStripAsync(IntPtr paneHwnd, int topStripPx = 50)
            => OcrReader.OcrTopStripAsync(paneHwnd, topStripPx);

        public Task<(bool engineAvailable, string? text)> OcrReadTopStripDetailedAsync(IntPtr paneHwnd, int topStripPx = 50)
            => OcrReader.OcrTryReadTopStripDetailedAsync(paneHwnd, topStripPx);

        public Task<(bool engineAvailable, string? text)> OcrReadRegionAsync(IntPtr paneHwnd, Rectangle crop)
            => OcrReader.OcrTryReadRegionDetailedAsync(paneHwnd, crop);

        public string[] DumpAllStrings(IntPtr paneHwnd) => MsaaStringReader.GetAllStringsFromPane(paneHwnd);
        public void StartWatchingPane(IntPtr paneHwnd, Action<string[]> onChange) { StopWatchingPane(); _watcher = MsaaTextWatcher.Start(paneHwnd, onChange); }
        public void StopWatchingPane() { _watcher?.Dispose(); _watcher = null; }
    }
}
