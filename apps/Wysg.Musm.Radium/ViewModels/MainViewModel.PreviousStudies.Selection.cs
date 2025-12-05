using System;
using System.Diagnostics;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: Selection and toggle logic for Previous Studies.
    /// </summary>
    public partial class MainViewModel
    {
        private PreviousStudyTab? _selectedPreviousStudy;
        
        public PreviousStudyTab? SelectedPreviousStudy
        { 
            get => _selectedPreviousStudy; 
            set 
            { 
                var old = _selectedPreviousStudy;
                
                // Now update the selected tab property (this may trigger WPF binding updates)
                if (SetProperty(ref _selectedPreviousStudy, value)) 
                { 
                    Debug.WriteLine($"[Prev] SelectedPreviousStudy set -> {(value==null?"<null>":value.Title)}"); 
                    foreach (var t in PreviousStudies) t.IsSelected = (value != null && t.Id == value.Id); 
                    
                    // CRITICAL FIX (2025-12-05): Load split ranges BEFORE HookPreviousStudy calls UpdatePreviousReportJson()
                    // Previously, HookPreviousStudy would call UpdatePreviousReportJson() before split ranges were loaded,
                    // causing HeaderTemp to be computed with wrong/stale split ranges from the previous tab.
                    // 
                    // The correct order is:
                    // 1. Load RawJson and split ranges from the new tab's SelectedReport
                    // 2. THEN call HookPreviousStudy (which calls UpdatePreviousReportJson)
                    // 3. THEN call UpdatePreviousReportJson again to ensure all properties are updated
                    if (value != null && value.SelectedReport != null)
                    {
                        Debug.WriteLine($"[Prev] Loading proofread fields and split ranges from selected report BEFORE HookPreviousStudy");
                        // Ensure RawJson is set from the selected report
                        if (!string.IsNullOrWhiteSpace(value.SelectedReport.ReportJson))
                        {
                            value.RawJson = value.SelectedReport.ReportJson;
                            Debug.WriteLine($"[Prev] Set RawJson from SelectedReport, length={value.RawJson.Length}");
                        }
                        // Now explicitly load proofread fields and split ranges from RawJson
                        // This MUST happen BEFORE UpdatePreviousReportJson is called!
                        value.ApplyReportSelection(value.SelectedReport);
                    }
                    
                    // Now hook up the property change handler and compute JSON
                    // At this point, split ranges are already loaded from the correct RawJson
                    HookPreviousStudy(old, value); 
                    EnsureSplitDefaultsIfNeeded(); 
                    
                    // CRITICAL FIX: Call UpdatePreviousReportJson() AFTER split ranges are loaded
                    // This ensures HeaderTemp, FindingsOut, ConclusionOut are computed with correct split ranges
                    UpdatePreviousReportJson();
                    
                    // Notify all wrapper properties that depend on SelectedPreviousStudy
                    OnPropertyChanged(nameof(PreviousHeaderText)); 
                    OnPropertyChanged(nameof(PreviousHeaderAndFindingsText)); 
                    OnPropertyChanged(nameof(PreviousFinalConclusionText)); 
                    
                    // CRITICAL FIX: Notify split-mode properties so editors update when switching tabs in splitted mode
                    OnPropertyChanged(nameof(PreviousHeaderTemp));
                    OnPropertyChanged(nameof(PreviousSplitFindings));
                    OnPropertyChanged(nameof(PreviousSplitConclusion));
                    OnPropertyChanged(nameof(PreviousHeaderSplitView));
                    OnPropertyChanged(nameof(PreviousFindingsSplitView));
                    OnPropertyChanged(nameof(PreviousConclusionSplitView));
                    
                    // NEW: Notify computed editor properties for proofread support
                    // These MUST be notified AFTER UpdatePreviousReportJson() completes
                    OnPropertyChanged(nameof(PreviousHeaderEditorText));  // FIX: Add notification for header editor
                    OnPropertyChanged(nameof(PreviousFindingsEditorText));
                    OnPropertyChanged(nameof(PreviousConclusionEditorText));
                    
                    // NEW: Update SavePreviousStudyToDBCommand CanExecute state when selection changes
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                } 
            } 
        }
        
        private bool _previousReportSplitted = true;
        
        public bool PreviousReportSplitted
        {
            get => _previousReportSplitted;
            set
            {
                if (SetProperty(ref _previousReportSplitted, value))
                {
                    Debug.WriteLine($"[Prev] PreviousReportSplitted -> {value}");
                    if (value) EnsureSplitDefaultsIfNeeded();
                    OnPropertyChanged(nameof(PreviousHeaderSplitView));
                    OnPropertyChanged(nameof(PreviousFindingsSplitView));
                    OnPropertyChanged(nameof(PreviousConclusionSplitView));
                    
                    // NEW: Notify editor properties when split mode changes (affects fallback)
                    OnPropertyChanged(nameof(PreviousHeaderEditorText));  // FIX: Add notification for header editor
                    OnPropertyChanged(nameof(PreviousFindingsEditorText));
                    OnPropertyChanged(nameof(PreviousConclusionEditorText));
                }
            }
        }
        
        private void EnsureSplitDefaultsIfNeeded()
        {
            var tab = SelectedPreviousStudy;
            if (tab == null) return;
            
            var hf = tab.Findings ?? string.Empty;
            var fc = tab.Conclusion ?? string.Empty;
            
            if (tab.HfHeaderFrom == null && tab.HfHeaderTo == null)
            {
                tab.HfHeaderFrom = 0;
                tab.HfHeaderTo = 0;
            }
            
            if (tab.HfConclusionFrom == null && tab.HfConclusionTo == null)
            {
                tab.HfConclusionFrom = hf.Length;
                tab.HfConclusionTo = hf.Length;
            }
            
            if (tab.FcHeaderFrom == null && tab.FcHeaderTo == null)
            {
                tab.FcHeaderFrom = 0;
                tab.FcHeaderTo = 0;
            }
            
            if (tab.FcFindingsFrom == null && tab.FcFindingsTo == null)
            {
                tab.FcFindingsFrom = 0;
                tab.FcFindingsTo = 0;
            }
        }
        
        // Placeholder properties for split functionality
        private bool _autoSplitHeader;
        public bool AutoSplitHeader { get => _autoSplitHeader; set => SetProperty(ref _autoSplitHeader, value); }
        
        private bool _autoSplitConclusion;
        public bool AutoSplitConclusion { get => _autoSplitConclusion; set => SetProperty(ref _autoSplitConclusion, value); }
        
        private bool _autoSplit;
        public bool AutoSplit { get => _autoSplit; set => SetProperty(ref _autoSplit, value); }
    }
}
