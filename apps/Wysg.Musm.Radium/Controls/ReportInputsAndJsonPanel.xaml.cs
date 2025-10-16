using System.Windows;
using System.Windows.Controls;

namespace Wysg.Musm.Radium.Controls
{
    public partial class ReportInputsAndJsonPanel : UserControl
    {
        public ReportInputsAndJsonPanel()
        {
            InitializeComponent();
            Loaded += (_, __) => ApplyReverse(Reverse);
        }

        public static readonly DependencyProperty ReverseProperty =
            DependencyProperty.Register(nameof(Reverse), typeof(bool), typeof(ReportInputsAndJsonPanel), new PropertyMetadata(false, OnReverseChanged));

        public bool Reverse
        {
            get => (bool)GetValue(ReverseProperty);
            set => SetValue(ReverseProperty, value);
        }

        private static void OnReverseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ReportInputsAndJsonPanel self)
            {
                self.ApplyReverse((bool)e.NewValue);
            }
        }

        private void ApplyReverse(bool reverse)
        {
            // True Grid layout: swap main column (0) with json column (4)
            // Note: With Grid-based rows, we need to swap all elements in column 0 and column 4
            var json = this.FindName("txtCurrentJson") as UIElement;
            if (json == null) return;

            // Get all children in column 0 (main content) and move to column 4, and vice versa
            var grid = this.Content as Grid;
            if (grid == null) return;

            if (reverse)
            {
                // json column 0, main content to column 4
                Grid.SetColumn(json, 0);
                // Swap main column content
                foreach (UIElement child in grid.Children)
                {
                    if (child == json) continue;
                    var col = Grid.GetColumn(child);
                    if (col == 0) Grid.SetColumn(child, 4);
                    else if (col == 4) Grid.SetColumn(child, 0);
                }
            }
            else
            {
                // main content column 0, json to column 4
                Grid.SetColumn(json, 4);
                // Restore main column content
                foreach (UIElement child in grid.Children)
                {
                    if (child == json) continue;
                    var col = Grid.GetColumn(child);
                    if (col == 4 && Grid.GetColumnSpan(child) == 1) Grid.SetColumn(child, 0);
                    else if (col == 0 && Grid.GetRow(child) != 0) continue; // Leave column 0 items alone
                }
            }
        }
    }
}
