using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Wysg.Musm.EditorDataStudio.Services;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Wysg.Musm.EditorDataStudio.ViewModels;

namespace Wysg.Musm.EditorDataStudio.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnOpenSettings(object sender, RoutedEventArgs e)
        {
            // Resolve config and local settings from DI to bind into settings window
            if (Application.Current is App app)
            {
                var cfg = app.Services.GetRequiredService<IDbConfig>();
                var local = app.Services.GetRequiredService<ILocalSettings>();
                var win = new SettingsWindow(cfg, local) { Owner = this };
                _ = win.ShowDialog();
            }
        }

        private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current != null && current is not T)
            {
                current = VisualTreeHelper.GetParent(current);
            }
            return current as T;
        }

        private static SctConceptDto? GetSctFromEvent(object sender, RoutedEventArgs e)
        {
            if (sender is not DataGrid grid) return null;
            if (e is MouseButtonEventArgs mbe && mbe.OriginalSource is DependencyObject dep)
            {
                var row = FindAncestor<DataGridRow>(dep);
                return row?.Item as SctConceptDto;
            }
            return grid.SelectedItem as SctConceptDto;
        }

        private void OnSctResultsMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right) return; // Only handle right click
            var vm = DataContext as MainViewModel;
            if (vm == null) return;
            var sct = GetSctFromEvent(sender, e);
            if (sct == null) return;
            vm.RootConceptId = sct.Id;
        }

        private void OnSctResultsMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            // Do NOT update RootConceptId here
            var vm = DataContext as MainViewModel;
            if (vm == null) return;
            var sct = GetSctFromEvent(sender, e);
            if (sct == null) return;

            var id = sct.Id;
            if (string.IsNullOrWhiteSpace(vm.ExpressionCg))
                vm.ExpressionCg = id;
            else
            {
                var current = vm.ExpressionCg!.Trim();
                if (current.EndsWith(",") || current.EndsWith(", "))
                    vm.ExpressionCg = current + id;
                else
                    vm.ExpressionCg = current + ", " + id;
            }
        }

        private void SctSearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var btn = FindName("SearchSctButton") as Button;
                if (btn != null && btn.Command != null && btn.Command.CanExecute(null))
                {
                    btn.Command.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }
}
