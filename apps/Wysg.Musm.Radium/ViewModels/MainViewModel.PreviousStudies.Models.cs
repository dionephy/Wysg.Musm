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
            public string Title { get; set; } = string.Empty;
            public DateTime StudyDateTime { get; set; }
            public string Modality { get; set; } = string.Empty;
            
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
                        ApplyReportSelection(value);
                        
                        // CRITICAL: Notify that Findings and Conclusion changed so MainViewModel updates JSON and UI
                        // This is needed because ApplyReportSelection updates these properties internally
                        OnPropertyChanged(nameof(Findings));
                        OnPropertyChanged(nameof(Conclusion));
                        OnPropertyChanged(nameof(OriginalFindings));
                        OnPropertyChanged(nameof(OriginalConclusion));
                    }
                }
            }
            
            public void ApplyReportSelection(PreviousReportChoice? rep)
            {
                if (rep == null)
                {
                    Debug.WriteLine("[PrevTab] ApplyReportSelection: null report - clearing fields");
                    OriginalFindings = string.Empty;
                    OriginalConclusion = string.Empty;
                    Findings = string.Empty;
                    Conclusion = string.Empty;
                    return;
                }
                
                Debug.WriteLine($"[PrevTab] ApplyReportSelection: applying report datetime={rep.ReportDateTime:yyyy-MM-dd HH:mm:ss}, findings len={rep.Findings?.Length ?? 0}, conclusion len={rep.Conclusion?.Length ?? 0}");
                OriginalFindings = rep.Findings;
                OriginalConclusion = rep.Conclusion;
                Findings = rep.Findings;
                Conclusion = rep.Conclusion;
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
            private string _chiefComplaintProofread = string.Empty;
            public string ChiefComplaintProofread { get => _chiefComplaintProofread; set => SetProperty(ref _chiefComplaintProofread, value ?? string.Empty); }
            
            private string _patientHistoryProofread = string.Empty;
            public string PatientHistoryProofread { get => _patientHistoryProofread; set => SetProperty(ref _patientHistoryProofread, value ?? string.Empty); }
            
            private string _studyTechniquesProofread = string.Empty;
            public string StudyTechniquesProofread { get => _studyTechniquesProofread; set => SetProperty(ref _studyTechniquesProofread, value ?? string.Empty); }
            
            private string _comparisonProofread = string.Empty;
            public string ComparisonProofread { get => _comparisonProofread; set => SetProperty(ref _comparisonProofread, value ?? string.Empty); }
            
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
            
            public string Display => $"{Studyname} ({StudyDateTimeFmt}) - {ReportDateTimeFmt} by {CreatedBy}";
            
            private string StudyDateTimeFmt => _studyDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "?";
            private string ReportDateTimeFmt => ReportDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "(no report dt)";
            
            internal DateTime? _studyDateTime;
            
            public override string ToString() => Display;
        }
        
        public ObservableCollection<PreviousStudyTab> PreviousStudies { get; } = new();
    }
}
