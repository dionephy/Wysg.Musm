using System.Windows;
using System.Windows.Controls;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Views
{
    public partial class SnomedBrowserWindow : Window
    {
        public SnomedBrowserWindow(SnomedBrowserViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void OnDomainRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && DataContext is SnomedBrowserViewModel vm)
            {
                var domain = rb.Tag?.ToString() ?? "all";
                vm.SelectedDomain = domain;
            }
        }
    }
}
