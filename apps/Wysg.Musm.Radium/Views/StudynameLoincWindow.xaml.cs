using System;
using System.Linq;
using System.Windows;
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
