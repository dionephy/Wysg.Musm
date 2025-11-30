using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: Automation screen and worklist operations.
    /// Contains methods for opening studies, managing screen layouts, and worklist interactions.
    /// </summary>
    public partial class MainViewModel
    {
        private async Task RunOpenStudyAsync()
        {
            try
            {
                await _pacs.InvokeOpenStudyAsync();
                StudyOpened = true;
                SetStatus("Open study invoked");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Automation] OpenStudy error: " + ex.Message);
                SetStatus("Open study failed", true);
            }
        }

        private async Task RunSetCurrentInMainScreenAsync()
        {
            try
            {                
                await _pacs.SetPreviousStudyInSubScreenAsync();
                await _pacs.SetCurrentStudyInMainScreenAsync();
                SetStatus("Screen layout set: current study in main, previous study in sub");
                
                // NEW: Request focus on Study Remark textbox in top grid after screen layout is complete
                // Small delay to allow PACS UI to settle before focusing our textbox
                await Task.Delay(150);
                RequestFocusStudyRemark?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Automation] SetCurrentInMainScreen error: " + ex.Message);
                SetStatus("Screen layout failed", true);
            }
        }

        private async Task RunOpenWorklistAsync()
        {
            try
            {
                await _pacs.InvokeOpenWorklistAsync();
                SetStatus("Worklist opened");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Automation] OpenWorklist error: " + ex.Message);
                SetStatus("Open worklist failed", true);
            }
        }

        private async Task RunResultsListSetFocusAsync()
        {
            try
            {
                Debug.WriteLine("[Automation] ResultsListSetFocus starting");
                
                // Execute the PACS method (which contains GetSelectedElement + ClickElementAndStay)
                await _pacs.SetFocusSearchResultsListAsync();
                
                // Add small delay to allow UI to respond to click (timing fix for automation vs manual execution)
                await Task.Delay(150);
                
                Debug.WriteLine("[Automation] ResultsListSetFocus completed successfully");
                SetStatus("Search results list focused");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Automation] ResultsListSetFocus error: {ex.Message}");
                Debug.WriteLine($"[Automation] ResultsListSetFocus stack: {ex.StackTrace}");
                SetStatus("Set focus results list failed", true);
            }
        }
    }
}
