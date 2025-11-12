using System;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: User-initiated command handlers (button clicks, menu actions).
    /// </summary>
    public partial class MainViewModel
    {
        private void OnSendReportPreview() 
        { 
            var seqRaw = GetAutomationSequenceForCurrentPacs(static s => s.SendReportPreviewSequence);
            var modules = seqRaw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (modules.Length > 0)
            {
                _ = RunModulesSequentially(modules, "Send Report Preview");
            }
            else
            {
                SetStatus("No Send Report Preview sequence configured", true);
            }
        }
        
        private void OnSendReport() 
        { 
            var seqRaw = GetAutomationSequenceForCurrentPacs(static s => s.SendReportSequence);
            var modules = seqRaw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (modules.Length > 0)
            {
                _ = RunModulesSequentially(modules, "Send Report");
            }
            else
            {
                // Fallback: just unlock patient if no sequence configured
                PatientLocked = false;
            }
        }

        private void OnRunAddStudyAutomation()
        {
            var seqRaw = GetAutomationSequenceForCurrentPacs(static s => s.AddStudySequence);
            var modules = seqRaw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (modules.Length == 0) return;
            _ = RunModulesSequentially(modules, "Add Study");
        }

        private void OnRunTestAutomation()
        {
            var seqRaw = GetAutomationSequenceForCurrentPacs(static s => s.TestSequence);
            var modules = seqRaw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (modules.Length == 0) 
            {
                SetStatus("No Test sequence configured", true);
                return;
            }
            _ = RunModulesSequentially(modules, "Test");
        }

        private void OnNewStudy()
        {
            var seqRaw = GetAutomationSequenceForCurrentPacs(static s => s.NewStudySequence);
            var modules = seqRaw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (modules.Length == 0) return;
            _ = RunModulesSequentially(modules, "New Study");
        }

        // Executes configured modules for OpenStudy shortcut depending on lock/opened state
        public void RunOpenStudyShortcut()
        {
            string seqRaw;
            string sequenceName;
            if (!PatientLocked) 
            {
                seqRaw = GetAutomationSequenceForCurrentPacs(static s => s.ShortcutOpenNew);
                sequenceName = "Shortcut: Open study (new)";
            }
            else if (!StudyOpened) 
            {
                seqRaw = GetAutomationSequenceForCurrentPacs(static s => s.ShortcutOpenAdd);
                sequenceName = "Shortcut: Open study (add)";
            }
            else 
            {
                seqRaw = GetAutomationSequenceForCurrentPacs(static s => s.ShortcutOpenAfterOpen);
                sequenceName = "Shortcut: Open study (after open)";
            }

            var modules = seqRaw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (modules.Length == 0) return;
            _ = RunModulesSequentially(modules, sequenceName);
        }

        // Executes configured modules for SendReport shortcut depending on Reportified state
        public void RunSendReportShortcut()
        {
            string seqRaw;
            string sequenceName;
            if (Reportified)
            {
                seqRaw = GetAutomationSequenceForCurrentPacs(static s => s.ShortcutSendReportReportified);
                sequenceName = "Shortcut: Send report (reportified)";
                Debug.WriteLine("[SendReportShortcut] Reportified=true, using ShortcutSendReportReportified sequence");
            }
            else
            {
                seqRaw = GetAutomationSequenceForCurrentPacs(static s => s.ShortcutSendReportPreview);
                sequenceName = "Shortcut: Send report (preview)";
                Debug.WriteLine("[SendReportShortcut] Reportified=false, using ShortcutSendReportPreview sequence");
            }

            var modules = seqRaw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (modules.Length == 0) 
            {
                SetStatus("No Send Report shortcut sequence configured", true);
                return;
            }
            _ = RunModulesSequentially(modules, sequenceName);
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

        private void OnEditStudyTechnique()
        {
            try
            {
                // Open window to edit technique combination for current study
                Views.StudyTechniqueWindow.OpenForStudy(this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EditStudyTechnique] Error: {ex.Message}");
                SetStatus("Failed to open study technique editor", true);
            }
        }

        private void OnEditComparison()
        {
            try
            {
                Debug.WriteLine("[EditComparison] Opening Edit Comparison window");
                
                // Check if we have patient info and previous studies
                if (string.IsNullOrWhiteSpace(PatientNumber))
                {
                    SetStatus("No patient loaded - cannot edit comparison", true);
                    return;
                }
                
                if (PreviousStudies.Count == 0)
                {
                    SetStatus("No previous studies available for this patient", true);
                    return;
                }
                
                // Open the Edit Comparison window and get the updated comparison string
                var newComparison = Views.EditComparisonWindow.Open(
                    PatientNumber,
                    PatientName,
                    PatientSex,
                    PreviousStudies.ToList(),
                    Comparison
                );
                
                // Update comparison if user clicked OK
                if (newComparison != null)
                {
                    Comparison = newComparison;
                    SetStatus("Comparison updated");
                    Debug.WriteLine($"[EditComparison] Updated comparison: '{newComparison}'");
                }
                else
                {
                    Debug.WriteLine("[EditComparison] User cancelled");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EditComparison] Error: {ex.Message}");
                SetStatus("Failed to open comparison editor", true);
            }
        }

        private void OnSavePreorder()
        {
            try
            {
                Debug.WriteLine("[SavePreorder] Saving current findings to findings_preorder JSON field");
                
                // Get the raw findings text (unreportified)
                var findingsText = RawFindingsText;
                
                if (string.IsNullOrWhiteSpace(findingsText))
                {
                    SetStatus("No findings text available to save as preorder", true);
                    Debug.WriteLine("[SavePreorder] Findings text is empty");
                    return;
                }
                
                Debug.WriteLine($"[SavePreorder] Captured findings text: length={findingsText.Length} chars");
                
                // Save to FindingsPreorder property (which will trigger JSON update)
                FindingsPreorder = findingsText;
                
                SetStatus($"Pre-order findings saved ({findingsText.Length} chars)");
                Debug.WriteLine("[SavePreorder] Successfully saved to FindingsPreorder property");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SavePreorder] Error: {ex.Message}");
                SetStatus("Save preorder operation failed", true);
            }
        }

        private void OnSavePreviousStudyToDB()
        {
            // CRITICAL FIX: Update JSON from current tab state BEFORE saving
            // Since auto-save on tab switch was disabled (2025-02-08), we must explicitly
            // synchronize the JSON with the current UI state when user clicks Save button
            
            // Add diagnostic logging to help debug split range persistence
            var tab = SelectedPreviousStudy;
            if (tab != null)
            {
                Debug.WriteLine("[SavePrevious] BEFORE UpdatePreviousReportJson:");
                Debug.WriteLine($"[SavePrevious]   HfHeaderFrom={tab.HfHeaderFrom}, HfHeaderTo={tab.HfHeaderTo}");
                Debug.WriteLine($"[SavePrevious]   HfConclusionFrom={tab.HfConclusionFrom}, HfConclusionTo={tab.HfConclusionTo}");
                Debug.WriteLine($"[SavePrevious]   FcHeaderFrom={tab.FcHeaderFrom}, FcHeaderTo={tab.FcHeaderTo}");
                Debug.WriteLine($"[SavePrevious]   FcFindingsFrom={tab.FcFindingsFrom}, FcFindingsTo={tab.FcFindingsTo}");
            }
            
            UpdatePreviousReportJson();
            
            if (tab != null)
            {
                Debug.WriteLine("[SavePrevious] AFTER UpdatePreviousReportJson:");
                Debug.WriteLine($"[SavePrevious]   JSON length: {PreviousReportJson?.Length ?? 0}");
                Debug.WriteLine($"[SavePrevious]   JSON preview: {(PreviousReportJson?.Length > 200 ? PreviousReportJson.Substring(0, 200) + "..." : PreviousReportJson)}");
            }
            
            // Reuse the existing RunSavePreviousStudyToDBAsync implementation
            _ = RunSavePreviousStudyToDBAsync();
        }
    }
}
