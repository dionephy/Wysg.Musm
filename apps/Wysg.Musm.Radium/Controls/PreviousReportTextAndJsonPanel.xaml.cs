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
            Loaded += (_, __) => ApplyReverse(Reverse);
        }

        private void ApplyReverse(bool reverse)
        {
            // Swap columns when reversed
            var scrollViewer = this.FindName("PART_LeftHost") as UIElement;
            var jsonBox = this.FindName("txtJson") as UIElement;

            if (scrollViewer != null && jsonBox != null)
            {
                if (reverse)
                {
                    Grid.SetColumn(scrollViewer, 2);
                    Grid.SetColumn(jsonBox, 0);
                }
                else
                {
                    Grid.SetColumn(scrollViewer, 0);
                    Grid.SetColumn(jsonBox, 2);
                }
            }
        }
    }
}
