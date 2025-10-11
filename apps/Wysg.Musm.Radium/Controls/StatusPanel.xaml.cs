using System.Windows;
using System.Windows.Controls;

namespace Wysg.Musm.Radium.Controls
{
    public partial class StatusPanel : UserControl
    {
        public StatusPanel()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty TopContentProperty =
            DependencyProperty.Register(nameof(TopContent), typeof(object), typeof(StatusPanel), new PropertyMetadata(null));

        public object? TopContent
        {
            get => GetValue(TopContentProperty);
            set => SetValue(TopContentProperty, value);
        }
    }
}
