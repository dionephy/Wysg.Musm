using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Wysg.Musm.Infrastructure.ViewModels;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: Model classes for Previous Studies functionality.
    /// </summary>
    public partial class MainViewModel
    {
        /// <summary>
        /// Represents a previous study tab with report data and split configuration.
        /// </summary>
        public sealed class PreviousStudyTab : BaseViewModel
        {
            public Guid Id { get; set; }
            
            private string _title = string.Empty;
            public string Title
            {
                get => _title;
                set => SetProperty(ref _title, value ?? string.Empty);
            }
            
            private DateTime _studyDateTime;
            public DateTime StudyDateTime
            {
                get => _studyDateTime;
                set => SetProperty(ref _studyDateTime, value);
            }
            
            private string _modality = string.Empty;
            public string Modality
            {
                get => _modality;
                set => SetProperty(ref _modality, value ?? string.Empty);
            }
            
            private string _header = string.Empty;
            public string Header
            {
                get => _header;
                set
                {
                    if (SetProperty(ref _header, value))
                        Debug.WriteLine($"[PrevTab] Header changed -> {value}");
                }
            }
            
            private string _findings = string.Empty;
            public string Findings
            {
                get => _findings;
                set
                {
                    if (SetProperty(ref _findings, value))
                        Debug.WriteLine($"[PrevTab] Findings changed -> len={value?.Length}");
                }
            }
            
            private string _conclusion = string.Empty;
            public string Conclusion
            {
                get => _conclusion;
                set
                {
                    if (SetProperty(ref _conclusion, value))
                        Debug.WriteLine($"[PrevTab] Conclusion changed -> len={value?.Length}");
                }
            }
            
            public string RawJson { get; set; } = string.Empty;
            public string OriginalFindings { get; set; } = string.Empty;
            public string OriginalConclusion { get; set; } = string.Empty;
            
            private bool _isSelected;
            public bool IsSelected
            {
                get => _isSelected;
                set => SetProperty(ref _isSelected, value);
            }
            
            public ObservableCollection<PreviousReportChoice> Reports { get; } = new();
            
            private PreviousReportChoice? _selectedReport;
            public PreviousReportChoice? SelectedReport
            {
                get => _selectedReport;
                set
                {
                    if (SetProperty(ref _selectedReport, value))
                    {
                        Debug.WriteLine($"[PrevTab] Report selection changed to: {value?.Display ?? "(null)"}");
                        
                        // CRITICAL FIX: Update RawJson from the selected report's JSON
                        // Each PreviousReportChoice should contain its own JSON from the database
                        // This ensures proofread fields and split ranges are loaded correctly
                        if (value != null)
                        {
                            // Get the JSON for this specific report from the Reports collection
                            // We need to find the matching report in the database and load its JSON
                            // For now, we'll trigger the load in ApplyReportSelection
                            Debug.WriteLine($"[PrevTab] SelectedReport changed - will load proofread fields for report datetime={value.ReportDateTime:yyyy-MM-dd HH:mm:ss}");
                        }
                        
                        ApplyReportSelection(value);
                        
                        // CRITICAL: Notify that Findings and Conclusion changed so MainViewModel updates JSON and UI
                        // This is needed because ApplyReportSelection updates these properties internally
                        OnPropertyChanged(nameof(Findings));
                        OnPropertyChanged(nameof(Conclusion));
                        OnPropertyChanged(nameof(OriginalFindings));
                        OnPropertyChanged(nameof(OriginalConclusion));
                        
                        // CRITICAL FIX: Also notify proofread field changes so UI updates
                        // NOTE: All header component proofread fields removed as per user request
                        // Only Findings and Conclusion have proofread versions
                        OnPropertyChanged(nameof(FindingsProofread));
                        OnPropertyChanged(nameof(ConclusionProofread));
                        
                        // Notify split range changes
                        OnPropertyChanged(nameof(HfHeaderFrom));
                        OnPropertyChanged(nameof(HfHeaderTo));
                        OnPropertyChanged(nameof(HfConclusionFrom));
                        OnPropertyChanged(nameof(HfConclusionTo));
                        OnPropertyChanged(nameof(FcHeaderFrom));
                        OnPropertyChanged(nameof(FcHeaderTo));
                        OnPropertyChanged(nameof(FcFindingsFrom));
                        OnPropertyChanged(nameof(FcFindingsTo));
                    }
                }
            }
            
            public void ApplyReportSelection(PreviousReportChoice? rep)
            {
                if (rep == null)
                {
                    Debug.WriteLine("[PrevTab] ApplyReportSelection: null report - clearing fields");
                    
                    // CRITICAL FIX: Update RawJson first, THEN load proofread fields and split ranges BEFORE clearing other fields
                    // This ensures that when property changes fire, split ranges are already correct
                    RawJson = string.Empty;
                    LoadProofreadFieldsFromRawJson();
                    
                    OriginalFindings = string.Empty;
                    OriginalConclusion = string.Empty;
                    Findings = string.Empty;
                    Conclusion = string.Empty;
                    
                    return;
                }
                
                Debug.WriteLine($"[PrevTab] ApplyReportSelection: applying report datetime={rep.ReportDateTime:yyyy-MM-dd HH:mm:ss}, findings len={rep.Findings?.Length ?? 0}, conclusion len={rep.Conclusion?.Length ?? 0}");
                
                // CRITICAL FIX: Update RawJson FIRST, then load split ranges BEFORE setting Findings/Conclusion
                // This ensures split ranges are correct when property change events fire
                RawJson = rep.ReportJson ?? string.Empty;
                Debug.WriteLine($"[PrevTab] ApplyReportSelection: Updated RawJson length={RawJson.Length}");
                
                // CRITICAL FIX: Load proofread fields and split ranges BEFORE setting Findings/Conclusion
                // This prevents computing splits with wrong (stale) split ranges
                LoadProofreadFieldsFromRawJson();
                
                // Now set the findings and conclusion - split ranges are already correct
                OriginalFindings = rep.Findings;
                OriginalConclusion = rep.Conclusion;
                Findings = rep.Findings;
                Conclusion = rep.Conclusion;
            }
            
            /// <summary>
            /// Loads proofread fields and split ranges from RawJson.
            /// Called when a different report is selected in the ComboBox.
            /// </summary>
            private void LoadProofreadFieldsFromRawJson()
            {
                if (string.IsNullOrWhiteSpace(RawJson) || RawJson == "{}")
                {
                    Debug.WriteLine("[PrevTab] LoadProofreadFieldsFromRawJson: No RawJson available - clearing proofread fields");
                    
                    // Clear all proofread fields (all header component proofread fields removed)
                    FindingsProofread = string.Empty;
                    ConclusionProofread = string.Empty;
                    
                    // Clear split ranges
                    HfHeaderFrom = null;
                    HfHeaderTo = null;
                    HfConclusionFrom = null;
                    HfConclusionTo = null;
                    FcHeaderFrom = null;
                    FcHeaderTo = null;
                    FcFindingsFrom = null;
                    FcFindingsTo = null;
                    
                    return;
                }
                
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(RawJson);
                    var root = doc.RootElement;
                    
                    Debug.WriteLine("[PrevTab] LoadProofreadFieldsFromRawJson: Parsing RawJson");
                    
                    // Load proofread fields (all header component proofread fields removed)
                    if (root.TryGetProperty("findings_proofread", out var fpr))
                        FindingsProofread = fpr.GetString() ?? string.Empty;
                    else
                        FindingsProofread = string.Empty;
                        
                    if (root.TryGetProperty("conclusion_proofread", out var clpr))
                        ConclusionProofread = clpr.GetString() ?? string.Empty;
                    else
                        ConclusionProofread = string.Empty;
                    
                    Debug.WriteLine($"[PrevTab] LoadProofreadFieldsFromRawJson: Loaded proofread fields - findings len={FindingsProofread.Length}, conclusion len={ConclusionProofread.Length}");
                    
                    // Load split ranges from PrevReport section
                    if (root.TryGetProperty("PrevReport", out var prevReport) && prevReport.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        int? GetInt(string name)
                        {
                            if (prevReport.TryGetProperty(name, out var el) && el.ValueKind == System.Text.Json.JsonValueKind.Number && el.TryGetInt32(out var i))
                                return i;
                            return null;
                        }
                        
                        HfHeaderFrom = GetInt("header_and_findings_header_splitter_from");
                        HfHeaderTo = GetInt("header_and_findings_header_splitter_to");
                        HfConclusionFrom = GetInt("header_and_findings_conclusion_splitter_from");
                        HfConclusionTo = GetInt("header_and_findings_conclusion_splitter_to");
                        FcHeaderFrom = GetInt("final_conclusion_header_splitter_from");
                        FcHeaderTo = GetInt("final_conclusion_header_splitter_to");
                        FcFindingsFrom = GetInt("final_conclusion_findings_splitter_from");
                        FcFindingsTo = GetInt("final_conclusion_findings_splitter_to");
                        
                        Debug.WriteLine($"[PrevTab] LoadProofreadFieldsFromRawJson: Loaded split ranges - HfHeaderFrom={HfHeaderFrom}, HfHeaderTo={HfHeaderTo}");
                    }
                    else
                    {
                        Debug.WriteLine("[PrevTab] LoadProofreadFieldsFromRawJson: No PrevReport section found - clearing split ranges");
                        
                        // Clear split ranges if not found
                        HfHeaderFrom = null;
                        HfHeaderTo = null;
                        HfConclusionFrom = null;
                        HfConclusionTo = null;
                        FcHeaderFrom = null;
                        FcHeaderTo = null;
                        FcFindingsFrom = null;
                        FcFindingsTo = null;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PrevTab] LoadProofreadFieldsFromRawJson: Error parsing JSON - {ex.Message}");
                    
                    // Clear all fields on error (all header component proofread fields removed)
                    FindingsProofread = string.Empty;
                    ConclusionProofread = string.Empty;
                    
                    HfHeaderFrom = null;
                    HfHeaderTo = null;
                    HfConclusionFrom = null;
                    HfConclusionTo = null;
                    FcHeaderFrom = null;
                    FcHeaderTo = null;
                    FcFindingsFrom = null;
                    FcFindingsTo = null;
                }
            }
            
            // Splitter fields for user-defined ranges
            private int? _hfHeaderFrom;
            public int? HfHeaderFrom { get => _hfHeaderFrom; set => SetProperty(ref _hfHeaderFrom, value); }
            
            private int? _hfHeaderTo;
            public int? HfHeaderTo { get => _hfHeaderTo; set => SetProperty(ref _hfHeaderTo, value); }
            
            private int? _hfConclusionFrom;
            public int? HfConclusionFrom { get => _hfConclusionFrom; set => SetProperty(ref _hfConclusionFrom, value); }
            
            private int? _hfConclusionTo;
            public int? HfConclusionTo { get => _hfConclusionTo; set => SetProperty(ref _hfConclusionTo, value); }
            
            private int? _fcHeaderFrom;
            public int? FcHeaderFrom { get => _fcHeaderFrom; set => SetProperty(ref _fcHeaderFrom, value); }
            
            private int? _fcHeaderTo;
            public int? FcHeaderTo { get => _fcHeaderTo; set => SetProperty(ref _fcHeaderTo, value); }
            
            private int? _fcFindingsFrom;
            public int? FcFindingsFrom { get => _fcFindingsFrom; set => SetProperty(ref _fcFindingsFrom, value); }
            
            private int? _fcFindingsTo;
            public int? FcFindingsTo { get => _fcFindingsTo; set => SetProperty(ref _fcFindingsTo, value); }
            
            // Additional metadata fields for previous report
            private string _chiefComplaint = string.Empty;
            public string ChiefComplaint { get => _chiefComplaint; set => SetProperty(ref _chiefComplaint, value ?? string.Empty); }
            
            private string _patientHistory = string.Empty;
            public string PatientHistory { get => _patientHistory; set => SetProperty(ref _patientHistory, value ?? string.Empty); }
            
            private string _studyTechniques = string.Empty;
            public string StudyTechniques { get => _studyTechniques; set => SetProperty(ref _studyTechniques, value ?? string.Empty); }
            
            private string _comparison = string.Empty;
            public string Comparison { get => _comparison; set => SetProperty(ref _comparison, value ?? string.Empty); }
            
            private string _studyRemark = string.Empty;
            public string StudyRemark { get => _studyRemark; set => SetProperty(ref _studyRemark, value ?? string.Empty); }
            
            private string _patientRemark = string.Empty;
            public string PatientRemark { get => _patientRemark; set => SetProperty(ref _patientRemark, value ?? string.Empty); }
            
            // Proofread fields (root JSON)
            // NOTE: All header component proofread fields removed as per user request
            // Only Findings and Conclusion have proofread versions
            
            private string _findingsProofread = string.Empty;
            public string FindingsProofread { get => _findingsProofread; set => SetProperty(ref _findingsProofread, value ?? string.Empty); }
            
            private string _conclusionProofread = string.Empty;
            public string ConclusionProofread { get => _conclusionProofread; set => SetProperty(ref _conclusionProofread, value ?? string.Empty); }
            
            // Split outputs (root json: header_temp, findings, conclusion)
            private string _headerTemp = string.Empty;
            public string HeaderTemp { get => _headerTemp; set => SetProperty(ref _headerTemp, value ?? string.Empty); }
            
            private string _findingsOut = string.Empty;
            public string FindingsOut { get => _findingsOut; set => SetProperty(ref _findingsOut, value ?? string.Empty); }
            
            private string _conclusionOut = string.Empty;
            public string ConclusionOut { get => _conclusionOut; set => SetProperty(ref _conclusionOut, value ?? string.Empty); }
            
            public override string ToString() => Title;
        }
        
        /// <summary>
        /// Represents a previous report choice for a study.
        /// </summary>
        public sealed class PreviousReportChoice : BaseViewModel
        {
            public DateTime? ReportDateTime { get; set; }
            public string CreatedBy { get; set; } = string.Empty;
            public string Studyname { get; set; } = string.Empty;
            public string Findings { get; set; } = string.Empty;
            public string Conclusion { get; set; } = string.Empty;
            
            // CRITICAL FIX: Store the raw JSON for this specific report
            // This allows loading proofread fields and split ranges when this report is selected
            public string ReportJson { get; set; } = string.Empty;
            
            public string Display => $"{Studyname} ({StudyDateTimeFmt}) - {ReportDateTimeFmt} by {CreatedBy}";
            
            private string StudyDateTimeFmt => _studyDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "?";
            private string ReportDateTimeFmt => ReportDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "(no report dt)";
            
            internal DateTime? _studyDateTime;
            
            public override string ToString() => Display;
        }
        
        public ObservableCollection<PreviousStudyTab> PreviousStudies { get; } = new();
    }
}
