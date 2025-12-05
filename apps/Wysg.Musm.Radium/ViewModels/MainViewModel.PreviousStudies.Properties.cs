using System.Diagnostics;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: Editor wrapper properties for Previous Studies.
    /// </summary>
    public partial class MainViewModel
    {
        // Cache fields for when no study is selected
        private string _prevHeaderCache = string.Empty;
        private string _prevHeaderAndFindingsCache = string.Empty;
        private string _prevFinalConclusionCache = string.Empty;
        private string _prevStudyRemarkCache = string.Empty;
        private string _prevPatientRemarkCache = string.Empty;
        private string _prevChiefComplaintCache = string.Empty;
        private string _prevPatientHistoryCache = string.Empty;
        private string _prevStudyTechniquesCache = string.Empty;
        private string _prevComparisonCache = string.Empty;
        private string _prevHeaderTempCache = string.Empty;
        private string _prevFindingsOutCache = string.Empty;
        private string _prevConclusionOutCache = string.Empty;
        // NOTE: All header component proofread cache fields removed as per user request
        // Only Findings and Conclusion have proofread versions
        private string _prevFindingsProofreadCache = string.Empty;
        private string _prevConclusionProofreadCache = string.Empty;
        
        public string PreviousHeaderText
        {
            get => SelectedPreviousStudy?.Header ?? _prevHeaderCache;
            set
            {
                if (SelectedPreviousStudy == null)
                {
                    if (value == _prevHeaderCache) return;
                    _prevHeaderCache = value;
                    OnPropertyChanged();
                    Debug.WriteLine($"[PrevEdit] Cache Header -> {value}");
                }
                else if (SelectedPreviousStudy.Header != value)
                {
                    SelectedPreviousStudy.Header = value;
                    Debug.WriteLine($"[PrevEdit] Tab Header -> {value}");
                }
                UpdatePreviousReportJson();
                OnPropertyChanged(nameof(PreviousReportJson));
            }
        }
        
        public string PreviousHeaderAndFindingsText
        {
            get => SelectedPreviousStudy?.Findings ?? _prevHeaderAndFindingsCache;
            set
            {
                if (SelectedPreviousStudy == null)
                {
                    if (value == _prevHeaderAndFindingsCache) return;
                    _prevHeaderAndFindingsCache = value;
                    OnPropertyChanged();
                    Debug.WriteLine($"[PrevEdit] Cache HeaderAndFindings len={value?.Length}");
                }
                else if (SelectedPreviousStudy.Findings != value)
                {
                    SelectedPreviousStudy.Findings = value;
                    Debug.WriteLine($"[PrevEdit] Tab HeaderAndFindings len={value?.Length}");
                }
                UpdatePreviousReportJson();
                OnPropertyChanged(nameof(PreviousReportJson));
            }
        }
        
        public string PreviousFinalConclusionText
        {
            get => SelectedPreviousStudy?.Conclusion ?? _prevFinalConclusionCache;
            set
            {
                if (SelectedPreviousStudy == null)
                {
                    if (value == _prevFinalConclusionCache) return;
                    _prevFinalConclusionCache = value;
                    OnPropertyChanged();
                    Debug.WriteLine($"[PrevEdit] Cache FinalConclusion len={value?.Length}");
                }
                else if (SelectedPreviousStudy.Conclusion != value)
                {
                    SelectedPreviousStudy.Conclusion = value;
                    Debug.WriteLine($"[PrevEdit] Tab FinalConclusion len={value?.Length}");
                }
                UpdatePreviousReportJson();
                OnPropertyChanged(nameof(PreviousReportJson));
                OnPropertyChanged(nameof(PreviousHeaderSplitView));
                OnPropertyChanged(nameof(PreviousFindingsSplitView));
                OnPropertyChanged(nameof(PreviousConclusionSplitView));
            }
        }
        
        // Aliases for backward compatibility (if needed elsewhere)
        public string PreviousFindingsText
        {
            get => PreviousHeaderAndFindingsText;
            set => PreviousHeaderAndFindingsText = value;
        }
        
        public string PreviousConclusionText
        {
            get => PreviousFinalConclusionText;
            set => PreviousFinalConclusionText = value;
        }

        // Root fields bound editors
        public string PreviousHeaderTemp
        {
            get => SelectedPreviousStudy?.HeaderTemp ?? _prevHeaderTempCache;
            set
            {
                if (SelectedPreviousStudy == null)
                {
                    if (value == _prevHeaderTempCache) return;
                    _prevHeaderTempCache = value;
                    OnPropertyChanged();
                }
                else if (SelectedPreviousStudy.HeaderTemp != value)
                {
                    SelectedPreviousStudy.HeaderTemp = value;
                }
                UpdatePreviousReportJson();
                OnPropertyChanged(nameof(PreviousReportJson));
            }
        }
        
        public string PreviousSplitFindings
        {
            get => SelectedPreviousStudy?.FindingsOut ?? _prevFindingsOutCache;
            set
            {
                if (SelectedPreviousStudy == null)
                {
                    if (value == _prevFindingsOutCache) return;
                    _prevFindingsOutCache = value;
                    OnPropertyChanged();
                }
                else if (SelectedPreviousStudy.FindingsOut != value)
                {
                    SelectedPreviousStudy.FindingsOut = value;
                }
                UpdatePreviousReportJson();
                OnPropertyChanged(nameof(PreviousReportJson));
            }
        }
        
        public string PreviousSplitConclusion
        {
            get => SelectedPreviousStudy?.ConclusionOut ?? _prevConclusionOutCache;
            set
            {
                if (SelectedPreviousStudy == null)
                {
                    if (value == _prevConclusionOutCache) return;
                    _prevConclusionOutCache = value;
                    OnPropertyChanged();
                }
                else if (SelectedPreviousStudy.ConclusionOut != value)
                {
                    SelectedPreviousStudy.ConclusionOut = value;
                }
                UpdatePreviousReportJson();
                OnPropertyChanged(nameof(PreviousReportJson));
            }
        }
        
        // NEW: Computed properties for previous report Findings and Conclusion editors with proofread support
        // These follow the fallback chain: proofread (with placeholders applied) ¡æ splitted (if on) ¡æ original
        // CRITICAL FIX: Apply ApplyProofreadPlaceholders() to proofread text for {arrow}, {DDx}, {bullet} conversion
        
        /// <summary>
        /// Computed property for Previous Header editor binding.
        /// Returns HeaderTemp when PreviousReportSplitted is true, empty string otherwise.
        /// This replaces the Style.Triggers binding approach which had tab-switch issues.
        /// </summary>
        public string PreviousHeaderEditorText
        {
            get
            {
                // Only show header when split mode is enabled
                if (!PreviousReportSplitted)
                {
                    return string.Empty;
                }
                
                var tab = SelectedPreviousStudy;
                if (tab == null)
                {
                    Debug.WriteLine($"[PreviousHeaderEditorText] GET: tab is null, returning cache: '{_prevHeaderTempCache?.Substring(0, Math.Min(50, _prevHeaderTempCache?.Length ?? 0))}'");
                    return _prevHeaderTempCache ?? string.Empty;
                }
                
                var result = tab.HeaderTemp ?? string.Empty;
                Debug.WriteLine($"[PreviousHeaderEditorText] GET: tab={tab.Title}, HeaderTemp.Length={result.Length}, first50='{result.Substring(0, Math.Min(50, result.Length))}'");
                return result;
            }
            set
            {
                // Two-way binding support for editing header text
                if (!PreviousReportSplitted)
                {
                    return; // Don't accept changes when not in split mode
                }
                
                if (SelectedPreviousStudy == null)
                {
                    if (value == _prevHeaderTempCache) return;
                    _prevHeaderTempCache = value;
                    OnPropertyChanged();
                }
                else if (SelectedPreviousStudy.HeaderTemp != value)
                {
                    SelectedPreviousStudy.HeaderTemp = value;
                }
                UpdatePreviousReportJson();
                OnPropertyChanged(nameof(PreviousReportJson));
            }
        }

        public string PreviousFindingsEditorText
        {
            get
            {
                var tab = SelectedPreviousStudy;
                if (tab == null) return _prevHeaderAndFindingsCache ?? string.Empty;
                
                // Proofread mode: use proofread version if available AND apply placeholder conversion
                if (PreviousProofreadMode && !string.IsNullOrWhiteSpace(tab.FindingsProofread))
                {
                    return ApplyProofreadPlaceholders(tab.FindingsProofread);
                }
                
                // Fallback: splitted mode uses split version, otherwise original
                if (PreviousReportSplitted)
                {
                    return tab.FindingsOut ?? string.Empty;
                }
                else
                {
                    return tab.Findings ?? string.Empty;
                }
            }
        }
        
        public string PreviousConclusionEditorText
        {
            get
            {
                var tab = SelectedPreviousStudy;
                if (tab == null) return _prevFinalConclusionCache ?? string.Empty;
                
                // Proofread mode: use proofread version if available AND apply placeholder conversion
                if (PreviousProofreadMode && !string.IsNullOrWhiteSpace(tab.ConclusionProofread))
                {
                    return ApplyProofreadPlaceholders(tab.ConclusionProofread);
                }
                
                // Fallback: splitted mode uses split version, otherwise original
                if (PreviousReportSplitted)
                {
                    return tab.ConclusionOut ?? string.Empty;
                }
                else
                {
                    return tab.Conclusion ?? string.Empty;
                }
            }
        }
        
        // NEW: Computed properties for previous report that switch between raw and proofread versions based on PreviousProofreadMode
        // These properties will be bound to the previous report editor DocumentText
        public string PreviousFindingsDisplay 
        {
            get
            {
                var tab = SelectedPreviousStudy;
                if (tab == null) return string.Empty;
                var text = PreviousProofreadMode && !string.IsNullOrWhiteSpace(tab.FindingsProofread) 
                    ? tab.FindingsProofread 
                    : tab.FindingsOut;
                return PreviousProofreadMode ? ApplyProofreadPlaceholders(text) : text;
            }
        }

        public string PreviousConclusionDisplay 
        {
            get
            {
                var tab = SelectedPreviousStudy;
                if (tab == null) return string.Empty;
                var text = PreviousProofreadMode && !string.IsNullOrWhiteSpace(tab.ConclusionProofread) 
                    ? tab.ConclusionProofread 
                    : tab.ConclusionOut;
                return PreviousProofreadMode ? ApplyProofreadPlaceholders(text) : text;
            }
        }
    }
}
