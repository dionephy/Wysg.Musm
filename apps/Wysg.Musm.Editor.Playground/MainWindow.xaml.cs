using System.Windows;
using Wysg.Musm.Editor.Controls;

namespace Wysg.Musm.Editor.Playground
{
    public partial class MainWindow : Window
    {
        private MainViewModel _vm = null!;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainViewModel(EditorCtl); // EditorCtl is the x:Name of your EditorControl in XAML

        }

        private async void OnForceGhost(object sender, RoutedEventArgs e)
        {
            await _vm.ForceServerGhostsAsync();
        }

       
    }
}
