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
                
                // DISABLED 2025-02-08: Auto-save on tab switch removed per user request
                // Users now need to manually save changes using the "Save to DB" button
                // Previous behavior: Captured old tab's JSON and applied it before switching away
                /*
                // CRITICAL FIX: Capture OLD tab's JSON BEFORE the binding system updates _previousReportJson
                // This prevents the bug where Tab A gets saved with Tab B's JSON content
                string? oldTabJson = null;
                if (old != null && old != value)
                {
                    // Snapshot the current JSON text before WPF binding changes it
                    oldTabJson = _previousReportJson;
                    Debug.WriteLine($"[Prev] Captured JSON for outgoing tab: {old.Title}, len={oldTabJson?.Length}");
                }
                */
                
                // Now update the selected tab property (this may trigger WPF binding updates)
                if (SetProperty(ref _selectedPreviousStudy, value)) 
                { 
                    Debug.WriteLine($"[Prev] SelectedPreviousStudy set -> {(value==null?"<null>":value.Title)}"); 
                    foreach (var t in PreviousStudies) t.IsSelected = (value != null && t.Id == value.Id); 
                    HookPreviousStudy(old, value); 
                    EnsureSplitDefaultsIfNeeded(); 
                    
                    // DISABLED 2025-02-08: Auto-save on tab switch removed per user request
                    /*
                    // CRITICAL FIX: Apply the captured JSON to the OLD tab AFTER binding completes
                    // This ensures Tab A's changes are saved to Tab A, not to Tab B
                    if (old != null && !string.IsNullOrWhiteSpace(oldTabJson) && oldTabJson != "{}")
                    {
                        try
                        {
                            Debug.WriteLine($"[Prev] Applying captured JSON to old tab: {old.Title}");
                            ApplyJsonToTabDirectly(old, oldTabJson);
                            Debug.WriteLine($"[Prev] Successfully saved JSON for old tab: {old.Title}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[Prev] Error saving JSON for outgoing tab: {ex.Message}");
                        }
                    }
                    */
                    
                    // CRITICAL FIX (2025-02-09): Load proofread fields from selected report BEFORE calling UpdatePreviousReportJson()
                    // This prevents the "Previous findings suddenly become blank" issue when EditComparisonWindow closes and reloads studies
                    // Root cause: UpdatePreviousReportJson() was being called before the selected report's RawJson was loaded into the tab,
                    // causing split outputs (FindingsOut, ConclusionOut) to be computed with empty/stale proofread fields
                    if (value != null && value.SelectedReport != null)
                    {
                        Debug.WriteLine($"[Prev] Loading proofread fields from selected report before UpdatePreviousReportJson");
                        // Ensure RawJson is set from the selected report
                        if (!string.IsNullOrWhiteSpace(value.SelectedReport.ReportJson))
                        {
                            value.RawJson = value.SelectedReport.ReportJson;
                            Debug.WriteLine($"[Prev] Set RawJson from SelectedReport, length={value.RawJson.Length}");
                        }
                        // Now explicitly load proofread fields from RawJson
                        // This is normally done in PreviousStudyTab.ApplyReportSelection, but we need to ensure it happens
                        // before UpdatePreviousReportJson is called
                        value.SelectedReport = value.SelectedReport; // Trigger ApplyReportSelection
                    }
                    
                    // CRITICAL FIX: Call UpdatePreviousReportJson() BEFORE notifying editor properties
                    // This ensures ConclusionOut and FindingsOut are computed before bindings try to read them
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
