using System.Windows;
using System.Windows.Controls;

namespace Wysg.Musm.Radium.Controls
{
    public partial class ReportInputsAndJsonPanel : UserControl
    {
        public ReportInputsAndJsonPanel()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ReverseProperty =
            DependencyProperty.Register(nameof(Reverse), typeof(bool), typeof(ReportInputsAndJsonPanel), new PropertyMetadata(false));

        public bool Reverse
        {
            get => (bool)GetValue(ReverseProperty);
            set => SetValue(ReverseProperty, value);
        }
    }
}
