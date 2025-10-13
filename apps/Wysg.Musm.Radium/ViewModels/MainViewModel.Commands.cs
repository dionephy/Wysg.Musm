using System;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Diagnostics;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: ICommand definitions & handlers (New Study, Add, Send, Select, Procedure execution).
    /// Keeps UI action logic compact and discoverable.
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
        public ICommand OpenEditStudyTechniqueCommand { get; private set; } = null!;

        // UI mode toggles
        private bool _proofreadMode; public bool ProofreadMode { get => _proofreadMode; set => SetProperty(ref _proofreadMode, value); }
        private bool _previousProofreadMode; public bool PreviousProofreadMode { get => _previousProofreadMode; set => SetProperty(ref _previousProofreadMode, value); }

        // Auto toggles for generation on current report fields
        private bool _autoChiefComplaint; public bool AutoChiefComplaint { get => _autoChiefComplaint; set => SetProperty(ref _autoChiefComplaint, value); }
        private bool _autoPatientHistory; public bool AutoPatientHistory { get => _autoPatientHistory; set => SetProperty(ref _autoPatientHistory, value); }
        private bool _autoConclusion; public bool AutoConclusion { get => _autoConclusion; set => SetProperty(ref _autoConclusion, value); }

        // Auto toggles for previous/bottom extra fields
        private bool _autoStudyTechniques; public bool AutoStudyTechniques { get => _autoStudyTechniques; set => SetProperty(ref _autoStudyTechniques, value); }
        private bool _autoComparison; public bool AutoComparison { get => _autoComparison; set => SetProperty(ref _autoComparison, value); }

        // Auto toggles for proofread fields
        private bool _autoChiefComplaintProofread; public bool AutoChiefComplaintProofread { get => _autoChiefComplaintProofread; set => SetProperty(ref _autoChiefComplaintProofread, value); }
        private bool _autoPatientHistoryProofread; public bool AutoPatientHistoryProofread { get => _autoPatientHistoryProofread; set => SetProperty(ref _autoPatientHistoryProofread, value); }
        private bool _autoStudyTechniquesProofread; public bool AutoStudyTechniquesProofread { get => _autoStudyTechniquesProofread; set => SetProperty(ref _autoStudyTechniquesProofread, value); }
        private bool _autoComparisonProofread; public bool AutoComparisonProofread { get => _autoComparisonProofread; set => SetProperty(ref _autoComparisonProofread, value); }
        private bool _autoFindingsProofread; public bool AutoFindingsProofread { get => _autoFindingsProofread; set => SetProperty(ref _autoFindingsProofread, value); }
        private bool _autoConclusionProofread; public bool AutoConclusionProofread { get => _autoConclusionProofread; set => SetProperty(ref _autoConclusionProofread, value); }

        // Patient locked state influences several command CanExecute states
        private bool _patientLocked; public bool PatientLocked
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
                }
            }
        }

        private void InitializeCommands()
        {
            NewStudyCommand = new DelegateCommand(_ => OnNewStudy());
            TestNewStudyProcedureCommand = new DelegateCommand(async _ => await RunNewStudyProcedureAsync());
            AddStudyCommand = new DelegateCommand(_ => OnRunAddStudyAutomation(), _ => PatientLocked);
            SendReportPreviewCommand = new DelegateCommand(_ => OnSendReportPreview(), _ => PatientLocked);
            SendReportCommand = new DelegateCommand(_ => OnSendReport(), _ => PatientLocked);
            SelectPreviousStudyCommand = new DelegateCommand(o => OnSelectPrevious(o), _ => PatientLocked);
            OpenStudynameMapCommand = new DelegateCommand(_ => Views.StudynameLoincWindow.Open());
            GenerateFieldCommand = new DelegateCommand(p => OnGenerateField(p));
            OpenEditStudyTechniqueCommand = new DelegateCommand(_ => Views.StudyTechniqueWindow.Open());
        }

        // ------------- Handlers -------------
        private void OnSendReportPreview() { /* TODO: implement preview send logic */ }
        private void OnSendReport() { PatientLocked = false; }

        private async Task RunNewStudyProcedureAsync() => await (_newStudyProc != null ? _newStudyProc.ExecuteAsync(this) : Task.Run(OnNewStudy));

        // New automation helpers (use UI thread synchronization via async void)
        private async void AcquireStudyRemarkAsync()
        {
            try
            {
                var s = await _pacs.GetCurrentStudyRemarkAsync();
                StudyRemark = s ?? string.Empty; // property triggers JSON update
                SetStatus("Study remark captured");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Automation] GetStudyRemark error: " + ex.Message);
                SetStatus("Study remark capture failed", true);
            }
        }
        private async void AcquirePatientRemarkAsync()
        {
            try
            {
                var s = await _pacs.GetCurrentPatientRemarkAsync();
                PatientRemark = s ?? string.Empty; // property triggers JSON update
                SetStatus("Patient remark captured");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Automation] GetPatientRemark error: " + ex.Message);
                SetStatus("Patient remark capture failed", true);
            }
        }

        private void OnRunAddStudyAutomation()
        {
            var seqRaw = _localSettings?.AutomationAddStudySequence ?? string.Empty;
            var modules = seqRaw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (modules.Length == 0) return;
            foreach (var m in modules)
            {
                if (string.Equals(m, "AddPreviousStudy", StringComparison.OrdinalIgnoreCase)) { _ = RunAddPreviousStudyModuleAsync(); }
                else if (string.Equals(m, "GetStudyRemark", StringComparison.OrdinalIgnoreCase)) { AcquireStudyRemarkAsync(); }
                else if (string.Equals(m, "GetPatientRemark", StringComparison.OrdinalIgnoreCase)) { AcquirePatientRemarkAsync(); }
            }
        }

        private async Task RunAddPreviousStudyModuleAsync()
        {
            try
            {
                // 1) Ensure PACS current patient equals application's current patient number
                var pacsCurrent = await _pacs.GetCurrentPatientNumberAsync();
                string Normalize(string? s) => string.IsNullOrWhiteSpace(s) ? string.Empty : Regex.Replace(s, "[^A-Za-z0-9]", "").ToUpperInvariant();
                if (Normalize(pacsCurrent) != Normalize(PatientNumber)) { SetStatus("Patient mismatch cannot add", true); return; }

                // Optional: also verify selected related study belongs to same patient
                var relatedSelected = await _pacs.GetSelectedIdFromRelatedStudiesAsync();
                if (!string.IsNullOrWhiteSpace(relatedSelected) && Normalize(relatedSelected) != Normalize(PatientNumber))
                { SetStatus("Related study not for current patient", true); return; }

                // 2) Fetch metadata from related studies list
                var studyName = await _pacs.GetSelectedStudynameFromRelatedStudiesAsync();
                var dtStr = await _pacs.GetSelectedStudyDateTimeFromRelatedStudiesAsync();
                var radiologist = await _pacs.GetSelectedRadiologistFromRelatedStudiesAsync();
                var reportDateStr = await _pacs.GetSelectedReportDateTimeFromRelatedStudiesAsync();

                // 3) Fetch findings/conclusion from current report editors using dual getters and pick longer
                var f1Task = _pacs.GetCurrentFindingsAsync();
                var f2Task = _pacs.GetCurrentFindings2Async();
                var c1Task = _pacs.GetCurrentConclusionAsync();
                var c2Task = _pacs.GetCurrentConclusion2Async();
                await Task.WhenAll(f1Task, f2Task, c1Task, c2Task);
                string PickLonger(string? a, string? b) => (b?.Length ?? 0) > (a?.Length ?? 0) ? (b ?? string.Empty) : (a ?? string.Empty);
                string findings = PickLonger(f1Task.Result, f2Task.Result);
                string conclusion = PickLonger(c1Task.Result, c2Task.Result);

                // 4) Persist as previous study via existing repo methods
                var dt = DateTime.TryParse(dtStr, out var studyDt) ? studyDt : (DateTime?)null;
                var reportDt = DateTime.TryParse(reportDateStr, out var rdt) ? rdt : (DateTime?)null;
                if (dt == null) { SetStatus("Related study datetime invalid", true); return; }
                if (_studyRepo == null) return;
                var studyId = await _studyRepo.EnsureStudyAsync(PatientNumber, PatientName, PatientSex, null, studyName, dt);
                if (studyId == null) { SetStatus("Study save failed", true); return; }

                var reportObj = new
                {
                    header_and_findings = findings,
                    final_conclusion = conclusion
                };
                string json = JsonSerializer.Serialize(reportObj);
                await _studyRepo.UpsertPartialReportAsync(studyId.Value, radiologist, reportDt, json, isMine: false, isCreated: false);
                await LoadPreviousStudiesForPatientAsync(PatientNumber);
                var newTab = PreviousStudies.FirstOrDefault(t => t.StudyDateTime == dt.Value);
                if (newTab != null) SelectedPreviousStudy = newTab;
                PreviousReportified = true;
                SetStatus("Previous study added");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[AddPreviousStudyModule] error: " + ex.Message);
                SetStatus("Add previous study module failed", true);
            }
        }

        private void OnNewStudy()
        {
            var seqRaw = _localSettings?.AutomationNewStudySequence ?? string.Empty;
            var modules = seqRaw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (modules.Length == 0) return;
            foreach (var m in modules)
            {
                if (string.Equals(m, "NewStudy", StringComparison.OrdinalIgnoreCase)) { _ = RunNewStudyProcedureAsync(); }
                else if (string.Equals(m, "LockStudy", StringComparison.OrdinalIgnoreCase) && _lockStudyProc != null) { _ = _lockStudyProc.ExecuteAsync(this); }
                else if (string.Equals(m, "GetStudyRemark", StringComparison.OrdinalIgnoreCase)) { AcquireStudyRemarkAsync(); }
                else if (string.Equals(m, "GetPatientRemark", StringComparison.OrdinalIgnoreCase)) { AcquirePatientRemarkAsync(); }
                else if (string.Equals(m, "AddPreviousStudy", StringComparison.OrdinalIgnoreCase)) { _ = RunAddPreviousStudyModuleAsync(); }
            }
        }

        private void OnSelectPrevious(object? o)
        {
            if (o is not PreviousStudyTab tab) return;
            if (SelectedPreviousStudy?.Id == tab.Id)
            {
                foreach (var t in PreviousStudies) t.IsSelected = (t.Id == tab.Id);
                return;
            }
            SelectedPreviousStudy = tab;
        }

        private void OnGenerateField(object? param)
        {
            try
            {
                var key = (param as string) ?? string.Empty;
                SetStatus(string.IsNullOrWhiteSpace(key) ? "Generate requested" : $"Generate {key} requested");
            }
            catch { }
        }

        // existing OnAddStudy retained for direct action flows if used elsewhere
        private async void OnAddStudy()
        {
            if (_studyRepo == null) return;
            try
            {
                var relatedPatientRaw = await _pacs.GetSelectedIdFromRelatedStudiesAsync();
                string Normalize(string? s) => string.IsNullOrWhiteSpace(s) ? string.Empty : Regex.Replace(s, "[^A-Za-z0-9]", "").ToUpperInvariant();
                var currentNorm = Normalize(PatientNumber);
                var relatedNorm = Normalize(relatedPatientRaw);
                if (string.IsNullOrWhiteSpace(currentNorm) || string.IsNullOrWhiteSpace(relatedNorm) || currentNorm != relatedNorm)
                { SetStatus($"Patient mismatch cannot add (current {currentNorm} vs related {relatedNorm})", true); return; }

                var studyName = await _pacs.GetSelectedStudynameFromRelatedStudiesAsync();
                var dtStr = await _pacs.GetSelectedStudyDateTimeFromRelatedStudiesAsync();
                var radiologist = await _pacs.GetSelectedRadiologistFromRelatedStudiesAsync();
                var reportDateStr = await _pacs.GetSelectedReportDateTimeFromRelatedStudiesAsync();
                if (!DateTime.TryParse(dtStr, out var studyDt)) { SetStatus("Related study datetime invalid", true); return; }
                DateTime? reportDt = DateTime.TryParse(reportDateStr, out var rdt) ? rdt : null;
                var studyId = await _studyRepo.EnsureStudyAsync(PatientNumber, PatientName, PatientSex, null, studyName, studyDt);
                if (studyId == null) { SetStatus("Study save failed", true); return; }

                // Acquire findings / conclusion (choose longer version of dual-field variants)
                var f1Task = _pacs.GetCurrentFindingsAsync();
                var f2Task = _pacs.GetCurrentFindings2Async();
                var c1Task = _pacs.GetCurrentConclusionAsync();
                var c2Task = _pacs.GetCurrentConclusion2Async();
                await Task.WhenAll(f1Task, f2Task, c1Task, c2Task);
                string PickLonger(string? a, string? b) => (b?.Length ?? 0) > (a?.Length ?? 0) ? (b ?? string.Empty) : (a ?? string.Empty);
                string findings = PickLonger(f1Task.Result, f2Task.Result);
                string conclusion = PickLonger(c1Task.Result, c2Task.Result);

                var reportObj = new
                {
                    technique = string.Empty,
                    chief_complaint = string.Empty,
                    history_preview = string.Empty,
                    chief_complaint_proofread = string.Empty,
                    history = string.Empty,
                    history_proofread = string.Empty,
                    header_and_findings = findings,
                    final_conclusion = conclusion,
                    // keep split outputs empty initially; do not misuse root 'conclusion' for main conclusion
                    split_index = 0,
                    comparison = string.Empty,
                    technique_proofread = string.Empty,
                    comparison_proofread = string.Empty,
                    findings_proofread = string.Empty,
                    conclusion_proofread = string.Empty,
                    findings,
                    conclusion_preview = string.Empty
                };
                string json = JsonSerializer.Serialize(reportObj);
                await _studyRepo.UpsertPartialReportAsync(studyId.Value, radiologist, reportDt, json, isMine: false, isCreated: false);
                await LoadPreviousStudiesForPatientAsync(PatientNumber);
                var newTab = PreviousStudies.FirstOrDefault(t => t.StudyDateTime == studyDt);
                if (newTab != null) SelectedPreviousStudy = newTab;
                PreviousReportified = true;
                SetStatus("Previous study added");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[AddStudy] error: " + ex.Message);
                SetStatus("Add study failed", true);
            }
        }

        // DelegateCommand implementation kept local for simplicity
        private sealed class DelegateCommand : ICommand
        {
            private readonly Action<object?> _exec; private readonly Predicate<object?>? _can;
            public DelegateCommand(Action<object?> exec, Predicate<object?>? can = null) { _exec = exec; _can = can; }
            public bool CanExecute(object? parameter) => _can?.Invoke(parameter) ?? true;
            public void Execute(object? parameter) => _exec(parameter);
            public event EventHandler? CanExecuteChanged; public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
