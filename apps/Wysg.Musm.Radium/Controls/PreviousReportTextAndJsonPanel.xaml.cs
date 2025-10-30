using System.Windows;
using System.Windows.Controls;

namespace Wysg.Musm.Radium.Controls
{
    /// <summary>
    /// Reusable panel displaying Previous Report's header_and_findings text alongside JSON view.
    /// Used in both side (landscape) and bottom (portrait) panels to reduce duplication.
    /// </summary>
    public partial class PreviousReportTextAndJsonPanel : UserControl
    {
        // Dependency Property for Header and Findings Text
        public static readonly DependencyProperty HeaderAndFindingsTextProperty =
            DependencyProperty.Register(
                nameof(HeaderAndFindingsText),
                typeof(string),
                typeof(PreviousReportTextAndJsonPanel),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string HeaderAndFindingsText
        {
            get => (string)GetValue(HeaderAndFindingsTextProperty);
            set => SetValue(HeaderAndFindingsTextProperty, value);
        }

        // Dependency Property for Final Conclusion Text
        public static readonly DependencyProperty FinalConclusionTextProperty =
            DependencyProperty.Register(
                nameof(FinalConclusionText),
                typeof(string),
                typeof(PreviousReportTextAndJsonPanel),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string FinalConclusionText
        {
            get => (string)GetValue(FinalConclusionTextProperty);
            set => SetValue(FinalConclusionTextProperty, value);
        }

        // Dependency Property for JSON Text
        public static readonly DependencyProperty JsonTextProperty =
            DependencyProperty.Register(
                nameof(JsonText),
                typeof(string),
                typeof(PreviousReportTextAndJsonPanel),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string JsonText
        {
            get => (string)GetValue(JsonTextProperty);
            set => SetValue(JsonTextProperty, value);
        }

        // Split outputs and header temp
        public static readonly DependencyProperty HeaderTempProperty =
            DependencyProperty.Register(
                nameof(HeaderTemp),
                typeof(string),
                typeof(PreviousReportTextAndJsonPanel),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public string HeaderTemp { get => (string)GetValue(HeaderTempProperty); set => SetValue(HeaderTempProperty, value); }

        public static readonly DependencyProperty SplitFindingsProperty =
            DependencyProperty.Register(
                nameof(SplitFindings),
                typeof(string),
                typeof(PreviousReportTextAndJsonPanel),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public string SplitFindings { get => (string)GetValue(SplitFindingsProperty); set => SetValue(SplitFindingsProperty, value); }

        public static readonly DependencyProperty SplitConclusionProperty =
            DependencyProperty.Register(
                nameof(SplitConclusion),
                typeof(string),
                typeof(PreviousReportTextAndJsonPanel),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public string SplitConclusion { get => (string)GetValue(SplitConclusionProperty); set => SetValue(SplitConclusionProperty, value); }

        // Proofread fields
        public static readonly DependencyProperty FindingsProofreadProperty =
            DependencyProperty.Register(
                nameof(FindingsProofread),
                typeof(string),
                typeof(PreviousReportTextAndJsonPanel),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public string FindingsProofread { get => (string)GetValue(FindingsProofreadProperty); set => SetValue(FindingsProofreadProperty, value); }

        public static readonly DependencyProperty ConclusionProofreadProperty =
            DependencyProperty.Register(
                nameof(ConclusionProofread),
                typeof(string),
                typeof(PreviousReportTextAndJsonPanel),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public string ConclusionProofread { get => (string)GetValue(ConclusionProofreadProperty); set => SetValue(ConclusionProofreadProperty, value); }

        // NEW: IsJsonCollapsed dependency property with default value true
        public static readonly DependencyProperty IsJsonCollapsedProperty =
            DependencyProperty.Register(
                nameof(IsJsonCollapsed),
                typeof(bool),
                typeof(PreviousReportTextAndJsonPanel),
                new PropertyMetadata(true, OnIsJsonCollapsedChanged));

        public bool IsJsonCollapsed
        {
            get => (bool)GetValue(IsJsonCollapsedProperty);
            set => SetValue(IsJsonCollapsedProperty, value);
        }

        private static void OnIsJsonCollapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PreviousReportTextAndJsonPanel self && e.NewValue is bool collapsed)
            {
                self.UpdateJsonColumnVisibility(collapsed);
            }
        }

        private void UpdateJsonColumnVisibility(bool collapsed)
        {
            if (JsonSplitter == null || txtJson == null || btnToggleJson == null)
                return;

            if (collapsed)
            {
                // Collapsed: just hide the TextBox and splitter, leave column structure intact
                JsonSplitter.Visibility = Visibility.Collapsed;
                txtJson.Visibility = Visibility.Collapsed;
                btnToggleJson.Content = "\u25B6"; // Right arrow (expand)
            }
            else
            {
                // Expanded: show the TextBox and splitter
                JsonSplitter.Visibility = Visibility.Visible;
                txtJson.Visibility = Visibility.Visible;
                btnToggleJson.Content = "\u25C0"; // Left arrow (collapse)
            }
        }

        // Dependency Property for Reverse layout (optional, for future use)
        public static readonly DependencyProperty ReverseProperty =
            DependencyProperty.Register(
                nameof(Reverse),
                typeof(bool),
                typeof(PreviousReportTextAndJsonPanel),
                new PropertyMetadata(false, OnReverseChanged));

        public bool Reverse
        {
            get => (bool)GetValue(ReverseProperty);
            set => SetValue(ReverseProperty, value);
        }

        private static void OnReverseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PreviousReportTextAndJsonPanel panel)
            {
                panel.ApplyReverse((bool)e.NewValue);
            }
        }

        public PreviousReportTextAndJsonPanel()
        {
            InitializeComponent();
            Loaded += (_, __) => 
            {
                ApplyReverse(Reverse);
                // Apply default collapsed state after control is loaded
                UpdateJsonColumnVisibility(IsJsonCollapsed);
            };
        }

        private void ApplyReverse(bool reverse)
        {
            // 5-column layout: 0=textboxes, 1=splitter, 2=proofread, 3=splitter, 4=json
            var scrollViewer = this.FindName("PART_LeftHost") as UIElement; // left textboxes panel
            var jsonBox = this.FindName("txtJson") as UIElement;            // right json panel

            if (scrollViewer == null || jsonBox == null) return;

            if (reverse)
            {
                // json | splitter | proofread | splitter | textboxes
                Grid.SetColumn(jsonBox, 0);
                Grid.SetColumn(scrollViewer, 4);
            }
            else
            {
                // textboxes | splitter | proofread | splitter | json
                Grid.SetColumn(scrollViewer, 0);
                Grid.SetColumn(jsonBox, 4);
            }
        }
    }
}
