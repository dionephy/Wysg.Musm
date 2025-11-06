using System;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: Display properties for split views in Previous Studies.
    /// </summary>
    public partial class MainViewModel
    {
        // Computed display strings when PreviousReportSplitted is ON
        public string PreviousHeaderSplitView
        {
            get
            {
                if (!PreviousReportSplitted) return PreviousHeaderText;
                
                var tab = SelectedPreviousStudy;
                string hf = tab?.Findings ?? _prevHeaderAndFindingsCache ?? string.Empty;
                string fc = tab?.Conclusion ?? _prevFinalConclusionCache ?? string.Empty;
                
                int hfFrom = Clamp(tab?.HfHeaderFrom ?? 0, 0, hf.Length);
                int fcFrom = Clamp(tab?.FcHeaderFrom ?? 0, 0, fc.Length);
                
                var part1 = Sub(hf, 0, hfFrom).Trim();
                var part2 = Sub(fc, 0, fcFrom).Trim();
                
                return (part1 + Environment.NewLine + part2).Trim();
            }
        }
        
        public string PreviousFindingsSplitView
        {
            get
            {
                if (!PreviousReportSplitted) return PreviousHeaderAndFindingsText;
                
                var tab = SelectedPreviousStudy;
                string hf = tab?.Findings ?? _prevHeaderAndFindingsCache ?? string.Empty;
                string fc = tab?.Conclusion ?? _prevFinalConclusionCache ?? string.Empty;
                
                int hfTo = Clamp(tab?.HfHeaderTo ?? 0, 0, hf.Length);
                int hfFrom2 = Clamp(tab?.HfConclusionFrom ?? hf.Length, 0, hf.Length);
                int fcTo = Clamp(tab?.FcHeaderTo ?? 0, 0, fc.Length);
                int fcFrom2 = Clamp(tab?.FcFindingsFrom ?? 0, 0, fc.Length);
                
                var part1 = Sub(hf, hfTo, hfFrom2 - hfTo).Trim();
                var part2 = Sub(fc, fcTo, fcFrom2 - fcTo).Trim();
                
                return (part1 + Environment.NewLine + part2).Trim();
            }
        }
        
        public string PreviousConclusionSplitView
        {
            get
            {
                if (!PreviousReportSplitted) return PreviousFinalConclusionText;
                
                var tab = SelectedPreviousStudy;
                string hf = tab?.Findings ?? _prevHeaderAndFindingsCache ?? string.Empty;
                string fc = tab?.Conclusion ?? _prevFinalConclusionCache ?? string.Empty;
                
                int hfTo2 = Clamp(tab?.HfConclusionTo ?? hf.Length, 0, hf.Length);
                int fcTo2 = Clamp(tab?.FcFindingsTo ?? 0, 0, fc.Length);
                
                var part1 = Sub(hf, hfTo2, hf.Length - hfTo2).Trim();
                var part2 = Sub(fc, fcTo2, fc.Length - fcTo2).Trim();
                
                return (part1 + Environment.NewLine + part2).Trim();
            }
        }

        private static string Sub(string s, int start, int length)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            if (start < 0) start = 0;
            if (start > s.Length) start = s.Length;
            if (length < 0) length = 0;
            if (start + length > s.Length) length = s.Length - start;
            return length <= 0 ? string.Empty : s.Substring(start, length);
        }
    }
}
