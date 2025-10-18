using System.Windows;
using System.Windows.Controls;
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
    }
}
