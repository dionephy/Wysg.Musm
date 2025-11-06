using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: Split commands for Previous Studies.
    /// </summary>
    public partial class MainViewModel
    {
        // Commands for split functionality
        public ICommand? SplitHeaderCommand { get; set; }
        public ICommand? SplitConclusionCommand { get; set; }
        public ICommand? SplitHeaderTopCommand { get; set; }
        public ICommand? SplitHeaderBottomCommand { get; set; }
        public ICommand? SplitFindingsCommand { get; set; }

        private void InitializePreviousSplitCommands()
        {
            SplitHeaderTopCommand = new SimpleCommand(p => OnSplitHeaderTop(p));
            SplitConclusionCommand = new SimpleCommand(p => OnSplitConclusionTop(p));
            SplitHeaderBottomCommand = new SimpleCommand(p => OnSplitHeaderBottom(p));
            SplitFindingsCommand = new SimpleCommand(p => OnSplitFindingsBottom(p));
        }

        private static (int from, int to) GetOffsetsFromTextBox(object? param)
        {
            if (param is TextBox tb)
            {
                if (tb.SelectionLength > 0)
                {
                    int from = tb.SelectionStart;
                    int to = tb.SelectionStart + tb.SelectionLength;
                    return (from, to);
                }
                else
                {
                    int pos = tb.CaretIndex;
                    return (pos, pos);
                }
            }
            return (0, 0);
        }

        private void OnSplitHeaderTop(object? param)
        {
            var tab = SelectedPreviousStudy;
            if (tab == null)
            {
                SetStatus("Select a previous report first", true);
                return;
            }
            
            var (from, to) = GetOffsetsFromTextBox(param);
            tab.HfHeaderFrom = from;
            tab.HfHeaderTo = to;
            
            // Adjust conclusion start/end to not precede header end
            var hf = tab.Findings ?? string.Empty;
            int headerTo = Clamp(tab.HfHeaderTo ?? 0, 0, hf.Length);
            if ((tab.HfConclusionFrom ?? 0) < headerTo) tab.HfConclusionFrom = headerTo;
            if ((tab.HfConclusionTo ?? 0) < headerTo) tab.HfConclusionTo = headerTo;
            
            UpdatePreviousReportJson();
            
            // Notify UI bindings to update split views
            NotifySplitViewsChanged();
        }

        private void OnSplitConclusionTop(object? param)
        {
            var tab = SelectedPreviousStudy;
            if (tab == null)
            {
                SetStatus("Select a previous report first", true);
                return;
            }
            
            var (from, to) = GetOffsetsFromTextBox(param);
            tab.HfConclusionFrom = from;
            tab.HfConclusionTo = to;
            
            // Adjust header range to not exceed conclusion start
            var hf = tab.Findings ?? string.Empty;
            int conclFrom = Clamp(tab.HfConclusionFrom ?? 0, 0, hf.Length);
            if ((tab.HfHeaderFrom ?? 0) > conclFrom) tab.HfHeaderFrom = conclFrom;
            if ((tab.HfHeaderTo ?? 0) > conclFrom) tab.HfHeaderTo = conclFrom;
            
            UpdatePreviousReportJson();
            
            // Notify UI bindings to update split views
            NotifySplitViewsChanged();
        }

        private void OnSplitHeaderBottom(object? param)
        {
            var tab = SelectedPreviousStudy;
            if (tab == null)
            {
                SetStatus("Select a previous report first", true);
                return;
            }
            
            var (from, to) = GetOffsetsFromTextBox(param);
            tab.FcHeaderFrom = from;
            tab.FcHeaderTo = to;
            
            // Adjust findings split to not precede header end
            var fc = tab.Conclusion ?? string.Empty;
            int headerTo = Clamp(tab.FcHeaderTo ?? 0, 0, fc.Length);
            if ((tab.FcFindingsFrom ?? 0) < headerTo) tab.FcFindingsFrom = headerTo;
            if ((tab.FcFindingsTo ?? 0) < headerTo) tab.FcFindingsTo = headerTo;
            
            UpdatePreviousReportJson();
            
            // Notify UI bindings to update split views
            NotifySplitViewsChanged();
        }

        private void OnSplitFindingsBottom(object? param)
        {
            var tab = SelectedPreviousStudy;
            if (tab == null)
            {
                SetStatus("Select a previous report first", true);
                return;
            }
            
            var (from, to) = GetOffsetsFromTextBox(param);
            tab.FcFindingsFrom = from;
            tab.FcFindingsTo = to;
            
            // Adjust header split to not exceed findings start
            var fc = tab.Conclusion ?? string.Empty;
            int findingsFrom = Clamp(tab.FcFindingsFrom ?? 0, 0, fc.Length);
            if ((tab.FcHeaderFrom ?? 0) > findingsFrom) tab.FcHeaderFrom = findingsFrom;
            if ((tab.FcHeaderTo ?? 0) > findingsFrom) tab.FcHeaderTo = findingsFrom;
            
            UpdatePreviousReportJson();
            
            // Notify UI bindings to update split views
            NotifySplitViewsChanged();
        }
        
        /// <summary>
        /// Notifies all split view properties to update after a split operation.
        /// This ensures real-time UI updates in editors and textboxes.
        /// </summary>
        private void NotifySplitViewsChanged()
        {
            Debug.WriteLine("[PrevSplit] Notifying split views changed");
            
            // Notify split view computed properties
            OnPropertyChanged(nameof(PreviousHeaderSplitView));
            OnPropertyChanged(nameof(PreviousFindingsSplitView));
            OnPropertyChanged(nameof(PreviousConclusionSplitView));
            
            // Notify split output properties (these get updated in UpdatePreviousReportJson)
            OnPropertyChanged(nameof(PreviousHeaderTemp));
            OnPropertyChanged(nameof(PreviousSplitFindings));
            OnPropertyChanged(nameof(PreviousSplitConclusion));
            
            // Notify editor properties that depend on split mode
            OnPropertyChanged(nameof(PreviousFindingsEditorText));
            OnPropertyChanged(nameof(PreviousConclusionEditorText));
            
            // Notify display properties if in proofread mode
            OnPropertyChanged(nameof(PreviousFindingsDisplay));
            OnPropertyChanged(nameof(PreviousConclusionDisplay));
        }
        
        private static int Clamp(int value, int min, int max) => value < min ? min : (value > max ? max : value);
    }
}
