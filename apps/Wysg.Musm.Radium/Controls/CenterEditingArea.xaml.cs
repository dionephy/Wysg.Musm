using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wysg.Musm.Editor.Controls;

namespace Wysg.Musm.Radium.Controls
{
    /// <summary>
    /// UserControl combining current and previous report editing panels.
    /// This is the central editing area in MainWindow (replaces the former gridCenter).
    /// </summary>
    public partial class CenterEditingArea : UserControl
    {
        public CenterEditingArea()
        {
            InitializeComponent();
            
            // Wire up ExtractPhrases event from CurrentReportPanel to bubble to MainWindow
            CurrentReportPanel.ExtractPhrasesClick += OnExtractPhrasesClick;
            
            // Setup Alt+Arrow navigation between editors
            Loaded += (_, __) => SetupEditorNavigation();
        }

        /// <summary>
        /// Event raised when Extract Phrases button is clicked.
        /// Bubbles from CurrentReportPanel to MainWindow.
        /// </summary>
        public event RoutedEventHandler? ExtractPhrasesClick;

        private void OnExtractPhrasesClick(object? sender, RoutedEventArgs e)
        {
            // Bubble event to parent (MainWindow)
            ExtractPhrasesClick?.Invoke(this, e);
        }

        // Public accessors for nested editor controls (for MainWindow.xaml.cs access)
        public EditorControl EditorHeader => CurrentReportPanel.HeaderEditor;
        public EditorControl EditorFindings => CurrentReportPanel.FindingsEditor;
        public EditorControl EditorConclusion => CurrentReportPanel.ConclusionEditor;
        public EditorControl EditorPreviousHeader => PreviousReportPanel.PreviousHeaderEditor;
        public EditorControl EditorPreviousFindings => PreviousReportPanel.PreviousFindingsEditor;
        public EditorControl EditorPreviousConclusion => PreviousReportPanel.PreviousConclusionEditor;

        private void SetupEditorNavigation()
        {
            // Current report vertical navigation (with copy)
            SetupEditorPair(EditorFindings, EditorConclusion, Key.Down, Key.Up, copyText: true);
            
            // Current <-> Previous horizontal navigation
            SetupEditorPair(EditorFindings, EditorPreviousFindings, Key.Right, Key.Left, copyText: false);
            SetupOneWayEditor(EditorPreviousHeader, EditorFindings, Key.Left, copyText: true);
            SetupOneWayEditor(EditorPreviousConclusion, EditorFindings, Key.Left, copyText: true);
     
            // Previous report vertical navigation (no copy)
            SetupOneWayEditor(EditorPreviousHeader, EditorPreviousFindings, Key.Down, copyText: false);
            SetupOneWayEditor(EditorPreviousFindings, EditorPreviousHeader, Key.Up, copyText: false);
            SetupOneWayEditor(EditorPreviousFindings, EditorPreviousConclusion, Key.Down, copyText: false);
            SetupOneWayEditor(EditorPreviousConclusion, EditorPreviousFindings, Key.Up, copyText: false);
        }

        private void SetupEditorPair(EditorControl source, EditorControl target, Key sourceKey, Key targetKey, bool copyText)
        {
            SetupOneWayEditor(source, target, sourceKey, copyText);
            SetupOneWayEditor(target, source, targetKey, copyText);
        }

        private void SetupOneWayEditor(EditorControl source, EditorControl target, Key key, bool copyText)
        {
            // Find the underlying MusmEditor (AvalonEdit TextEditor)
            var sourceEditor = source.FindName("Editor") as ICSharpCode.AvalonEdit.TextEditor;
            if (sourceEditor == null)
            {
                System.Diagnostics.Debug.WriteLine($"[CenterEditingArea] Could not find Editor in source control");
                return;
            }

            sourceEditor.PreviewKeyDown += (s, e) =>
            {
                var actualKey = e.Key == Key.System ? e.SystemKey : e.Key;
  
                if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt && actualKey == key)
                {
                    HandleEditorNavigation(source, target, copyText);
                    e.Handled = true;
                }
            };
        }

        private void HandleEditorNavigation(EditorControl source, EditorControl target, bool copyText)
        {
            var sourceEditor = source.FindName("Editor") as ICSharpCode.AvalonEdit.TextEditor;
            var targetEditor = target.FindName("Editor") as ICSharpCode.AvalonEdit.TextEditor;
            
            if (sourceEditor == null || targetEditor == null) return;

            if (copyText && !string.IsNullOrEmpty(sourceEditor.SelectedText))
            {
                // Has selection and copying enabled: copy to end of target
                var selectedText = sourceEditor.SelectedText;
                var targetText = targetEditor.Text ?? string.Empty;
        
                if (!string.IsNullOrEmpty(targetText))
                {
                    targetEditor.Text = targetText + "\n" + selectedText;
                }
                else
                {
                    targetEditor.Text = selectedText;
                }
   
                targetEditor.Focus();
                targetEditor.CaretOffset = targetEditor.Text.Length;
            }
            else
            {
                // No selection or copying disabled: just move focus
                targetEditor.Focus();
                targetEditor.CaretOffset = targetEditor.Text?.Length ?? 0;
            }
        }
    }
}
