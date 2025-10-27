using System.Windows;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Views
{
    public partial class SnomedWordCountImporterWindow : Window
    {
        private readonly SnomedWordCountImporterViewModel _viewModel;

        public SnomedWordCountImporterWindow(SnomedWordCountImporterViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;
     
            // Save session when window closes
            Closing += OnWindowClosing;
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save current state before closing
            _viewModel.SaveSession();
        }
    }
}
