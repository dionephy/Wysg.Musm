using System.Windows.Controls;
using Wysg.Musm.Editor.Controls;

namespace Wysg.Musm.Radium.Controls
{
    /// <summary>
    /// UserControl for editing previous report (right panel in center editing area).
    /// Contains previous studies tabs, toggles, report selector, and header/findings/conclusion editors with split mode support.
    /// </summary>
    public partial class PreviousReportEditorPanel : UserControl
    {
        public PreviousReportEditorPanel()
        {
            InitializeComponent();
        }

        // Public accessors for nested editor controls (for MainWindow.xaml.cs initialization)
        public EditorControl PreviousHeaderEditor => EditorPreviousHeader;
        public EditorControl PreviousFindingsEditor => EditorPreviousFindings;
        public EditorControl PreviousConclusionEditor => EditorPreviousConclusion;
    }
}
