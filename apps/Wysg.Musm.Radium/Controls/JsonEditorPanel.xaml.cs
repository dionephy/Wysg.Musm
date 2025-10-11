using System.Windows;
using System.Windows.Controls;

namespace Wysg.Musm.Radium.Controls
{
    public partial class JsonEditorPanel : UserControl
    {
        public JsonEditorPanel()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(JsonEditorPanel), new PropertyMetadata(string.Empty));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
    }
}
