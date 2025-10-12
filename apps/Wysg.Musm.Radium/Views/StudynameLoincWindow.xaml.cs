using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
            if (sender is ListBoxItem lbi && lbi.DataContext is StudynameLoincViewModel.PartItem part)
            {
                Debug.WriteLine($"[Radium][View] DoubleClick: {part.PartNumber} '{part.PartDisplay}'");
                if (DataContext is StudynameLoincViewModel vm)
                {
                    if (vm.AddPartCommand.CanExecute(part))
                        vm.AddPartCommand.Execute(part);
                }
            }
        }

        private void OnPreviewItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem lbi && lbi.DataContext is StudynameLoincViewModel.MappingPreviewItem item)
            {
                Debug.WriteLine($"[Radium][View] Remove preview item: {item.PartNumber} order={item.PartSequenceOrder}");
                if (DataContext is StudynameLoincViewModel vm)
                {
                    vm.SelectedParts.Remove(item);
                }
            }
        }

        private async void OnPlaybookMatchDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem lbi && lbi.DataContext is StudynameLoincViewModel.PlaybookItem playbook)
            {
                if (DataContext is StudynameLoincViewModel vm)
                {
                    // If a different playbook, set SelectedPlaybook (triggers async load)
                    if (vm.SelectedPlaybook?.LoincNumber != playbook.LoincNumber)
                    {
                        vm.SelectedPlaybook = playbook; // triggers async load of parts
                        // Small yield to allow async load method to populate collection
                        await Task.Delay(50);
                    }

                    // Defensive wait loop (short) until parts appear or timeout
                    int tries = 0;
                    while (vm.PlaybookParts.Count == 0 && tries < 10)
                    {
                        await Task.Delay(30);
                        tries++;
                    }

                    foreach (var part in vm.PlaybookParts.ToList())
                    {
                        // Duplicate rule: same PartNumber + same Sequence only
                        bool already = vm.SelectedParts.Any(p => p.PartNumber == part.PartNumber && p.PartSequenceOrder == (string.IsNullOrWhiteSpace(part.PartSequenceOrder) ? "A" : part.PartSequenceOrder));
                        if (!already)
                        {
                            vm.SelectedParts.Add(new StudynameLoincViewModel.MappingPreviewItem
                            {
                                PartNumber = part.PartNumber,
                                PartDisplay = part.PartName,
                                PartSequenceOrder = string.IsNullOrWhiteSpace(part.PartSequenceOrder) ? "A" : part.PartSequenceOrder
                            });
                        }
                    }
                    Debug.WriteLine($"[Radium][View] Playbook import: {playbook.LoincNumber} added; total preview={vm.SelectedParts.Count}");
                }
            }
        }

        private void OnPlaybookPartDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem lbi && lbi.DataContext is StudynameLoincViewModel.PlaybookPartItem part)
            {
                if (DataContext is StudynameLoincViewModel vm)
                {
                    var normalizedSeq = string.IsNullOrWhiteSpace(part.PartSequenceOrder) ? "A" : part.PartSequenceOrder;
                    // Allow same PartNumber with different sequence; block only if both match
                    var exists = vm.SelectedParts.Any(p => p.PartNumber == part.PartNumber && p.PartSequenceOrder == normalizedSeq);
                    if (!exists)
                    {
                        vm.SelectedParts.Add(new StudynameLoincViewModel.MappingPreviewItem
                        {
                            PartNumber = part.PartNumber,
                            PartDisplay = part.PartName,
                            PartSequenceOrder = normalizedSeq
                        });
                        Debug.WriteLine($"[Radium][View] Added playbook part: {part.PartNumber} seq={normalizedSeq}");
                    }
                }
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnManageDefaultTechniqueClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is StudynameLoincViewModel vm && vm.SelectedStudyname != null)
                {
                    StudynameTechniqueWindow.Open(vm.SelectedStudyname.Id, vm.SelectedStudyname.Studyname);
                }
                else
                {
                    StudynameTechniqueWindow.Open(null, null);
                }
            }
            catch { }
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
