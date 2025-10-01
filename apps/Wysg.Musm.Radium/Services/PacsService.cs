using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Wysg.Musm.MFCUIA;
using Wysg.Musm.MFCUIA.Core.Controls;
using Wysg.Musm.MFCUIA.Selectors;
using Wysg.Musm.MFCUIA.Session;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Wysg.Musm.Radium.Services
{
    // PACS automation ? now fully procedure-driven for metadata getters (no fallbacks)
    public sealed class PacsService
    {
        private readonly string _proc;
        private IDisposable? _watcher;
        private IntPtr _cachedBannerHwnd = IntPtr.Zero;
        private DateTime _cachedAt;

        [DllImport("user32.dll", SetLastError = true)] private static extern bool IsWindow(IntPtr hWnd);

        public PacsService(string processName = "INFINITT") => _proc = processName;

        // Execute custom procedure only (caller must define it in ui-procedures.json)
        private static Task<string?> ExecCustom(string methodTag) => ProcedureExecutor.ExecuteAsync(methodTag);

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

        // Procedure-only metadata getters (Search Results list)
        public Task<string?> GetSelectedIdFromSearchResultsAsync() => ExecCustom("GetSelectedIdFromSearchResults");
        public Task<string?> GetSelectedNameFromSearchResultsAsync() => ExecCustom("GetSelectedNameFromSearchResults");
        public Task<string?> GetSelectedSexFromSearchResultsAsync() => ExecCustom("GetSelectedSexFromSearchResults");
        public Task<string?> GetSelectedBirthDateFromSearchResultsAsync() => ExecCustom("GetSelectedBirthDateFromSearchResults");
        public Task<string?> GetSelectedAgeFromSearchResultsAsync() => ExecCustom("GetSelectedAgeFromSearchResults");
        public Task<string?> GetSelectedStudynameFromSearchResultsAsync() => ExecCustom("GetSelectedStudynameFromSearchResults");
        public Task<string?> GetSelectedStudyDateTimeFromSearchResultsAsync() => ExecCustom("GetSelectedStudyDateTimeFromSearchResults");
        public Task<string?> GetSelectedRadiologistFromSearchResultsAsync() => ExecCustom("GetSelectedRadiologistFromSearchResults");
        public Task<string?> GetSelectedStudyRemarkFromSearchResultsAsync() => ExecCustom("GetSelectedStudyRemarkFromSearchResults");
        public Task<string?> GetSelectedReportDateTimeFromSearchResultsAsync() => ExecCustom("GetSelectedReportDateTimeFromSearchResults");

        // Procedure-only metadata getters (Related Studies list)
        public Task<string?> GetSelectedStudynameFromRelatedStudiesAsync() => ExecCustom("GetSelectedStudynameFromRelatedStudies");
        public Task<string?> GetSelectedStudyDateTimeFromRelatedStudiesAsync() => ExecCustom("GetSelectedStudyDateTimeFromRelatedStudies");
        public Task<string?> GetSelectedRadiologistFromRelatedStudiesAsync() => ExecCustom("GetSelectedRadiologistFromRelatedStudies");
        public Task<string?> GetSelectedReportDateTimeFromRelatedStudiesAsync() => ExecCustom("GetSelectedReportDateTimeFromRelatedStudies");

        // Procedure-only banner helpers
        public Task<string?> GetCurrentPatientNumberAsync() => ExecCustom("GetCurrentPatientNumber");
        public Task<string?> GetCurrentStudyDateTimeAsync() => ExecCustom("GetCurrentStudyDateTime");
        public Task<string?> GetCurrentFindingsAsync() => ExecCustom("GetCurrentFindings");
        public Task<string?> GetCurrentConclusionAsync() => ExecCustom("GetCurrentConclusion");
        public Task<string?> GetCurrentFindings2Async() => ExecCustom("GetCurrentFindings2");
        public Task<string?> GetCurrentConclusion2Async() => ExecCustom("GetCurrentConclusion2");

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

        public (IntPtr hwnd, string? text) TryAutoLocateBanner()
        {
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
            try { return hwnd != IntPtr.Zero && IsWindow(hwnd); } catch { return false; }
        }

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
