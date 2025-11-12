using System;
using System.Windows.Input;
using System.Diagnostics;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: ICommand initialization and UI toggle properties.
    /// </summary>
    public partial class MainViewModel
    {
        // ------------- Command properties (initialized in InitializeCommands) -------------
        public ICommand NewStudyCommand { get; private set; } = null!;
        public ICommand TestNewStudyProcedureCommand { get; private set; } = null!;
        public ICommand AddStudyCommand { get; private set; } = null!;
        public ICommand SendReportPreviewCommand { get; private set; } = null!;
        public ICommand SendReportCommand { get; private set; } = null!;
        public ICommand SelectPreviousStudyCommand { get; private set; } = null!;
        public ICommand OpenStudynameMapCommand { get; private set; } = null!;
        public ICommand GenerateFieldCommand { get; private set; } = null!;
        public ICommand EditStudyTechniqueCommand { get; private set; } = null!;
        public ICommand EditComparisonCommand { get; private set; } = null!;
        public ICommand SavePreorderCommand { get; private set; } = null!;
        public ICommand SavePreviousStudyToDBCommand { get; private set; } = null!;

        // UI mode toggles
        private bool _proofreadMode; 
        public bool ProofreadMode 
        { 
            get => _proofreadMode; 
            set 
            { 
                if (SetProperty(ref _proofreadMode, value))
                {
                    // Notify computed display properties for editors (Findings, Conclusion, and Header components)
                    OnPropertyChanged(nameof(FindingsDisplay));
                    OnPropertyChanged(nameof(ConclusionDisplay));
                    // NEW: Notify header component display properties and HeaderDisplay
                    OnPropertyChanged(nameof(ChiefComplaintDisplay));
                    OnPropertyChanged(nameof(PatientHistoryDisplay));
                    OnPropertyChanged(nameof(StudyTechniquesDisplay));
                    OnPropertyChanged(nameof(ComparisonDisplay));
                    OnPropertyChanged(nameof(HeaderDisplay));
                }
            } 
        }
        
        private bool _previousProofreadMode=true; 
        public bool PreviousProofreadMode 
        { 
            get => _previousProofreadMode; 
            set 
            { 
                if (SetProperty(ref _previousProofreadMode, value))
                {
                    Debug.WriteLine($"[PreviousProofreadMode] Changed to: {value}");
                    
                    // Notify all previous report computed display properties
                    OnPropertyChanged(nameof(PreviousChiefComplaintDisplay));
                    OnPropertyChanged(nameof(PreviousPatientHistoryDisplay));
                    OnPropertyChanged(nameof(PreviousStudyTechniquesDisplay));
                    OnPropertyChanged(nameof(PreviousComparisonDisplay));
                    OnPropertyChanged(nameof(PreviousFindingsDisplay));
                    OnPropertyChanged(nameof(PreviousConclusionDisplay));
                    
                    // CRITICAL FIX: Notify editor properties when proofread mode changes
                    // These properties must be notified so editors update in real-time
                    OnPropertyChanged(nameof(PreviousFindingsEditorText));
                    OnPropertyChanged(nameof(PreviousConclusionEditorText));
                    
                    Debug.WriteLine("[PreviousProofreadMode] All editor properties notified");
                }
            } 
        }

        // Study opened toggle (set when OpenStudy module runs)
        private bool _studyOpened; 
        public bool StudyOpened { get => _studyOpened; set => SetProperty(ref _studyOpened, value); }

        // Auto toggles for generation on current report fields
        private bool _autoChiefComplaint; 
        public bool AutoChiefComplaint { get => _autoChiefComplaint; set => SetProperty(ref _autoChiefComplaint, value); }
        private bool _autoPatientHistory; 
        public bool AutoPatientHistory { get => _autoPatientHistory; set => SetProperty(ref _autoPatientHistory, value); }
        private bool _autoConclusion; 
        public bool AutoConclusion { get => _autoConclusion; set => SetProperty(ref _autoConclusion, value); }

        // Auto toggles for previous/bottom extra fields
        private bool _autoStudyTechniques; 
        public bool AutoStudyTechniques { get => _autoStudyTechniques; set => SetProperty(ref _autoStudyTechniques, value); }
        private bool _autoComparison; 
        public bool AutoComparison { get => _autoComparison; set => SetProperty(ref _autoComparison, value); }

        // Auto toggles for proofread fields
        private bool _autoChiefComplaintProofread; 
        public bool AutoChiefComplaintProofread { get => _autoChiefComplaintProofread; set => SetProperty(ref _autoChiefComplaintProofread, value); }
        private bool _autoPatientHistoryProofread; 
        public bool AutoPatientHistoryProofread { get => _autoPatientHistoryProofread; set => SetProperty(ref _autoPatientHistoryProofread, value); }
        private bool _autoStudyTechniquesProofread; 
        public bool AutoStudyTechniquesProofread { get => _autoStudyTechniquesProofread; set => SetProperty(ref _autoStudyTechniquesProofread, value); }
        private bool _autoComparisonProofread; 
        public bool AutoComparisonProofread { get => _autoComparisonProofread; set => SetProperty(ref _autoComparisonProofread, value); }
        private bool _autoFindingsProofread; 
        public bool AutoFindingsProofread { get => _autoFindingsProofread; set => SetProperty(ref _autoFindingsProofread, value); }
        private bool _autoConclusionProofread; 
        public bool AutoConclusionProofread { get => _autoConclusionProofread; set => SetProperty(ref _autoConclusionProofread, value); }

        // Patient locked state influences several command CanExecute states
        private bool _patientLocked; 
        public bool PatientLocked
        {
            get => _patientLocked;
            set
            {
                if (SetProperty(ref _patientLocked, value))
                {
                    (AddStudyCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    (SendReportPreviewCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    (SendReportCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    (SelectPreviousStudyCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    (SavePreviousStudyToDBCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private void InitializeCommands()
        {
            NewStudyCommand = new DelegateCommand(_ => OnNewStudy());
            TestNewStudyProcedureCommand = new DelegateCommand(_ => OnRunTestAutomation());
            AddStudyCommand = new DelegateCommand(_ => OnRunAddStudyAutomation(), _ => PatientLocked);
            SendReportPreviewCommand = new DelegateCommand(_ => OnSendReportPreview(), _ => PatientLocked);
            SendReportCommand = new DelegateCommand(_ => OnSendReport(), _ => PatientLocked);
            SelectPreviousStudyCommand = new DelegateCommand(o => OnSelectPrevious(o), _ => PatientLocked);
            OpenStudynameMapCommand = new DelegateCommand(_ => Views.StudynameLoincWindow.Open());
            GenerateFieldCommand = new DelegateCommand(p => OnGenerateField(p));
            EditStudyTechniqueCommand = new DelegateCommand(_ => OnEditStudyTechnique(), _ => PatientLocked);
            EditComparisonCommand = new DelegateCommand(_ => OnEditComparison(), _ => PatientLocked);
            SavePreorderCommand = new DelegateCommand(_ => OnSavePreorder());
            SavePreviousStudyToDBCommand = new DelegateCommand(_ => OnSavePreviousStudyToDB(), _ => PatientLocked && SelectedPreviousStudy != null);
        }
    }
}
