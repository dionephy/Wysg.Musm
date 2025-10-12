using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Microsoft.Extensions.DependencyInjection;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Views
{
    public class StudyTechniqueWindow : Window
    {
        public StudyTechniqueWindow(StudyTechniqueViewModel vm)
        {
            Title = "Edit Study Technique";
            Width = 820; Height = 560; WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = System.Windows.Media.Brushes.Black; Foreground = System.Windows.Media.Brushes.Gainsboro;
            DataContext = vm;

            var root = new Grid { Margin = new Thickness(8) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new StackPanel { Orientation = Orientation.Horizontal };
            header.Children.Add(new TextBlock { Text = "Study:", Margin = new Thickness(0, 0, 8, 0) });
            header.Children.Add(new TextBlock { DataContext = vm, Text = vm.CurrentStudyLabel, FontWeight = FontWeights.Bold });
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            var body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(body, 1);
            root.Children.Add(body);

            // Left panel for builder and preview
            var leftPanel = new StackPanel { Orientation = Orientation.Vertical };
            Grid.SetColumn(leftPanel, 0);
            body.Children.Add(leftPanel);

            // Add Technique group
            var grpAdd = new GroupBox { Header = "Add Technique", Margin = new Thickness(0, 0, 0, 8) };
            var addPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(6) };
            // Prefix
            addPanel.Children.Add(new TextBlock { Text = "Prefix", Width = 60, VerticalAlignment = VerticalAlignment.Center });
            var cbPrefix = new ComboBox { Width = 180, DisplayMemberPath = "Text", Margin = new Thickness(0, 0, 8, 0) };
            cbPrefix.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Prefixes"));
            cbPrefix.SetBinding(Selector.SelectedItemProperty, new Binding("SelectedPrefix") { Mode = BindingMode.TwoWay });
            addPanel.Children.Add(cbPrefix);
            // Tech
            addPanel.Children.Add(new TextBlock { Text = "Tech", Width = 50, VerticalAlignment = VerticalAlignment.Center });
            var cbTech = new ComboBox { Width = 180, DisplayMemberPath = "Text", Margin = new Thickness(0, 0, 8, 0) };
            cbTech.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Techs"));
            cbTech.SetBinding(Selector.SelectedItemProperty, new Binding("SelectedTech") { Mode = BindingMode.TwoWay });
            addPanel.Children.Add(cbTech);
            // Suffix
            addPanel.Children.Add(new TextBlock { Text = "Suffix", Width = 60, VerticalAlignment = VerticalAlignment.Center });
            var cbSuffix = new ComboBox { Width = 220, DisplayMemberPath = "Text", Margin = new Thickness(0, 0, 8, 0) };
            cbSuffix.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Suffixes"));
            cbSuffix.SetBinding(Selector.SelectedItemProperty, new Binding("SelectedSuffix") { Mode = BindingMode.TwoWay });
            addPanel.Children.Add(cbSuffix);
            // Add button
            var btnAddTechnique = new Button { Content = "Add", Width = 80 };
            btnAddTechnique.SetBinding(Button.CommandProperty, new Binding("AddTechniqueCommand"));
            addPanel.Children.Add(btnAddTechnique);
            grpAdd.Content = addPanel;
            leftPanel.Children.Add(grpAdd);

            // Current combination preview
            var grpPreview = new GroupBox { Header = "Current Combination", Margin = new Thickness(0, 0, 0, 8) };
            var lst = new ListBox();
            lst.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("CurrentCombinationItems"));
            var dt = new DataTemplate(typeof(StudyTechniqueViewModel.CombinationItem));
            var dock = new FrameworkElementFactory(typeof(DockPanel));
            var tbSeq = new FrameworkElementFactory(typeof(TextBlock));
            tbSeq.SetValue(FrameworkElement.WidthProperty, 40.0);
            tbSeq.SetBinding(TextBlock.TextProperty, new Binding("SequenceOrder"));
            dock.AppendChild(tbSeq);
            var tbText = new FrameworkElementFactory(typeof(TextBlock));
            tbText.SetValue(FrameworkElement.MarginProperty, new Thickness(8, 0, 0, 0));
            tbText.SetBinding(TextBlock.TextProperty, new Binding("TechniqueDisplay"));
            dock.AppendChild(tbText);
            dt.VisualTree = dock;
            lst.ItemTemplate = dt;
            grpPreview.Content = lst;
            leftPanel.Children.Add(grpPreview);

            var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnSave = new Button { Content = "Save for Study & Studyname", Margin = new Thickness(0, 0, 8, 0) };
            btnSave.SetBinding(Button.CommandProperty, new Binding("SaveForStudyAndStudynameCommand"));
            var btnClose = new Button { Content = "Close" };
            btnClose.Click += (_, __) => Close();
            footer.Children.Add(btnSave);
            footer.Children.Add(btnClose);
            Grid.SetRow(footer, 2);
            root.Children.Add(footer);

            Content = root;
        }

        public static void Open()
        {
            var app = (App)Application.Current;
            var vm = app.Services.GetRequiredService<StudyTechniqueViewModel>();
            var w = new StudyTechniqueWindow(vm);
            w.Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
            w.Show();
        }
        public static void Open(long? studynameId, string? studyname)
        {
            var app = (App)Application.Current;
            var vm = app.Services.GetRequiredService<StudyTechniqueViewModel>();
            if (studynameId.HasValue) vm.InitializeForStudyname(studynameId.Value, studyname ?? string.Empty);
            var w = new StudyTechniqueWindow(vm);
            w.Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
            w.Show();
        }
    }
}
