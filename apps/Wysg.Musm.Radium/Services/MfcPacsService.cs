using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Wysg.Musm.MFCUIA;
using Wysg.Musm.MFCUIA.Core.Controls;
using Wysg.Musm.MFCUIA.Selectors;
using Wysg.Musm.MFCUIA.Session;

namespace Wysg.Musm.Radium.Services
{
    // Slim PACS automation using the new MFCUIA library
    public sealed class MfcPacsService
    {
        private readonly string _proc;
        private IDisposable? _watcher;
        public MfcPacsService(string processName = "INFINITT") => _proc = processName;

        public async Task OpenWorklistAsync()
        {
            using var mfc = MfcUi.Attach(_proc);
            _ = mfc.Command(1106).Invoke();
            await Task.Delay(200); // short yield; replace with event-based wait later
            await Task.Delay(200);
        }

        public async Task<string[]> GetSelectedStudyAsync()
        {
            using var mfc = MfcUi.Attach(_proc);
            var list = mfc.Find(By.ClassNth("SysListView32", 0)).AsListView();
            var items = list.GetSelectedRow(3, 4, 7, 2, 9, 12, 15, 17, 13, 16);
            return items;
        }

        public async Task<bool> IsViewerWindowAsync(IntPtr hwnd)
        {
            using var mfc = MfcUi.Attach(_proc);
            return mfc.Process != null; // Expand with GetWindowThreadProcessId if needed
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

        public Task<string?> OcrReadTopStripAsync(IntPtr paneHwnd, int topStripPx = 50)
            => OcrReader.OcrTopStripAsync(paneHwnd, topStripPx);

        public Task<(bool engineAvailable, string? text)> OcrReadTopStripDetailedAsync(IntPtr paneHwnd, int topStripPx = 50)
            => OcrReader.OcrTryReadTopStripDetailedAsync(paneHwnd, topStripPx);

        public string[] DumpAllStrings(IntPtr paneHwnd) => MsaaStringReader.GetAllStringsFromPane(paneHwnd);
        public void StartWatchingPane(IntPtr paneHwnd, Action<string[]> onChange) { StopWatchingPane(); _watcher = MsaaTextWatcher.Start(paneHwnd, onChange); }
        public void StopWatchingPane() { _watcher?.Dispose(); _watcher = null; }
    }
}
