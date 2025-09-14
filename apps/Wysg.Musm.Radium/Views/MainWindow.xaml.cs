using System.Windows;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.InitializeEditor(Editor);
            }
        }

        private void OnForceGhost(object sender, RoutedEventArgs e)
        {
            // Optional: invoke a command on VM that triggers ghost fetch
        }
    }
}