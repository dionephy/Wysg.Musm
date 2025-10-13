using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Microsoft.Extensions.DependencyInjection;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Views
{
    public partial class StudynameTechniqueWindow : Window
    {
        public StudynameTechniqueWindow(StudynameTechniqueViewModel vm)
        {
            Title = "Manage Studyname Techniques";
            Width = 780; Height = 520; WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = System.Windows.Media.Brushes.Black; Foreground = System.Windows.Media.Brushes.Gainsboro;
            DataContext = vm;

            var root = new Grid { Margin = new Thickness(8) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new TextBlock();
            header.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Header"));
            header.FontWeight = FontWeights.Bold;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            var info = new TextBlock { Text = "Add a new combination below to set default for this studyname.", Margin = new Thickness(0,4,0,8) };
            Grid.SetRow(info, 1);
            root.Children.Add(info);

            var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnAdd = new Button { Content = "Add And Set As Default", Margin = new Thickness(0, 0, 8, 0) };
            btnAdd.Click += async (_, __) =>
            {
                if (DataContext is ViewModels.StudynameTechniqueViewModel vm1 && vm1.StudynameIdPublic.HasValue)
                {
                    // Open builder and flag it to set as default after save
                    var app = (App)Application.Current;
                    var vmB = app.Services.GetRequiredService<StudyTechniqueViewModel>();
                    vmB.InitializeForStudyname(vm1.StudynameIdPublic.Value, vm1.Header.Replace("Studyname: ", string.Empty));
                    vmB.SetAsDefaultAfterSave = true;
                    var w = new StudyTechniqueWindow(vmB) { Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive) };
                    w.Show();
                    await System.Threading.Tasks.Task.Delay(200);
                    await vm1.ReloadAsync();
                }
                else
                {
                    var app = (App)Application.Current;
                    var vmB = app.Services.GetRequiredService<StudyTechniqueViewModel>();
                    vmB.SetAsDefaultAfterSave = true;
                    var w = new StudyTechniqueWindow(vmB) { Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive) };
                    w.Show();
                }
            };
            var btnClose = new Button { Content = "Close" };
            btnClose.Click += (_, __) => Close();
            footer.Children.Add(btnAdd);
            footer.Children.Add(btnClose);
            Grid.SetRow(footer, 2);
            root.Children.Add(footer);

            Content = root;
        }

        public static void Open(long studynameId, string studyname)
        {
            var app = (App)Application.Current;
            var vm = app.Services.GetRequiredService<StudynameTechniqueViewModel>();
            vm.Initialize(studynameId, studyname);
            var w = new StudynameTechniqueWindow(vm);
            w.Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
            w.Show();
        }

        public static void Open(long? studynameId, string? studyname)
        {
            var app = (App)Application.Current;
            var vm = app.Services.GetRequiredService<StudynameTechniqueViewModel>();
            if (studynameId.HasValue) vm.Initialize(studynameId.Value, studyname ?? string.Empty);
            var w = new StudynameTechniqueWindow(vm);
            w.Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
            w.Show();
        }
    }
}
