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
            // 5-column layout: 0=textboxes, 1=splitter, 2=proofread, 3=splitter, 4=json
            var left = this.FindName("PART_InputHost") as UIElement;
            var json = this.FindName("txtCurrentJson") as UIElement;
            if (left == null || json == null) return;

            if (reverse)
            {
                // json | splitter | proofread | splitter | textboxes
                Grid.SetColumn(json, 0);
                Grid.SetColumn(left, 4);
            }
            else
            {
                // textboxes | splitter | proofread | splitter | json
                Grid.SetColumn(left, 0);
                Grid.SetColumn(json, 4);
            }
        }
    }
}
