using System;
using System.Windows;
using System.Windows.Controls;
using Wysg.Musm.Editor.Controls;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Controls
{
    /// <summary>
    /// UserControl for editing current report (left panel in center editing area).
    /// Contains toolbar, header, foreign/findings (with shared scrollbar), and conclusion editors.
    /// </summary>
    public partial class CurrentReportEditorPanel : UserControl
    {
        public CurrentReportEditorPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Event raised when Extract Phrases button is clicked.
        /// Bubbles to MainWindow which has the actual handler.
        /// </summary>
        public event RoutedEventHandler? ExtractPhrasesClick;

        private void OnExtractPhrasesClick(object sender, RoutedEventArgs e)
        {
            // Bubble event to parent (MainWindow)
            ExtractPhrasesClick?.Invoke(this, e);
        }

        // Public accessors for nested editor controls (for MainWindow.xaml.cs initialization)
        public EditorControl HeaderEditor => EditorHeader;
        public EditorControl FindingsEditor => EditorFindings;
        public EditorControl ConclusionEditor => EditorConclusion;

        private void OnReadOnlyEditAttempted(object? sender, EventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.Reportified)
            {
                vm.Reportified = false;
                vm.ProofreadMode = false;
            }
        }
    }
}
