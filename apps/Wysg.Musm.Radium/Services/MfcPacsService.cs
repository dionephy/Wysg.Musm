using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Wysg.Musm.MFCUIA.Core.Controls;
using Wysg.Musm.MFCUIA.Selectors;
using Wysg.Musm.MFCUIA.Session;

namespace Wysg.Musm.Radium.Services
{
    // Slim PACS automation using the new MFCUIA library
    public sealed class MfcPacsService
    {
        private readonly string _proc;
        public MfcPacsService(string processName = "INFINITT") => _proc = processName;

        public async Task OpenWorklistAsync()
        {
            using var mfc = MfcUi.Attach(_proc);
            // 1106 is from your legacy code (Open Worklist)
            _ = mfc.Command(1106).Invoke();
            await Task.Delay(200); // short yield; replace with event-based wait later
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
            // Heuristic: top-level belongs to target process
            using var mfc = MfcUi.Attach(_proc);
            return mfc.Process != null; // Expand with GetWindowThreadProcessId if needed
        }
    }
}
