using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Views
{
    public partial class StudynameLoincWindow : Window
    {
        public StudynameLoincWindow(StudynameLoincViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void OnPartItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Ensure we have a ListBoxItem and its DataContext is PartItem
            if (sender is ListBoxItem lbi && lbi.DataContext is StudynameLoincViewModel.PartItem part)
            {
                Debug.WriteLine($"[Radium][View] DoubleClick: {part.PartNumber} '{part.PartDisplay}'");
                if (DataContext is StudynameLoincViewModel vm)
                {
                    // Forward to VM command for consistency
                    if (vm.AddPartCommand.CanExecute(part))
                        vm.AddPartCommand.Execute(part);
                }
            }
        }

        private void OnPreviewItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Ensure we have a ListBoxItem and its DataContext is MappingPreviewItem
            if (sender is ListBoxItem lbi && lbi.DataContext is StudynameLoincViewModel.MappingPreviewItem item)
            {
                Debug.WriteLine($"[Radium][View] Remove preview item: {item.PartNumber} order={item.PartSequenceOrder}");
                if (DataContext is StudynameLoincViewModel vm)
                {
                    // Remove the item from the SelectedParts collection
                    vm.SelectedParts.Remove(item);
                }
            }
        }

        public static void Open(string? preselectStudyname = null)
        {
            var app = (App)Application.Current;
            var vm = app.Services.GetRequiredService<StudynameLoincViewModel>();
            var w = new StudynameLoincWindow(vm);
            if (!string.IsNullOrWhiteSpace(preselectStudyname))
                vm.Preselect(preselectStudyname!);
            w.Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
            w.Show();
        }
    }
}
