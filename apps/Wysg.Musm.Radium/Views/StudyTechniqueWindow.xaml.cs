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
            Width = 980; Height = 640; WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = System.Windows.Media.Brushes.Black; Foreground = System.Windows.Media.Brushes.Gainsboro;
            DataContext = vm;

            var root = new Grid { Margin = new Thickness(8) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header
            var header = new StackPanel { Orientation = Orientation.Horizontal };
            header.Children.Add(new TextBlock { Text = "Study:", Margin = new Thickness(0, 0, 8, 0) });
            header.Children.Add(new TextBlock { DataContext = vm, Text = vm.CurrentStudyLabel, FontWeight = FontWeights.Bold });
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // Body: two columns with splitter; left = builder, right = studyname combos
            var body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            Grid.SetRow(body, 1);
            root.Children.Add(body);

            // Left side layout as Grid (rows: add group [Auto], preview [*])
            var leftGrid = new Grid();
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(leftGrid, 0);
            body.Children.Add(leftGrid);

            // Add Technique group (with inline add buttons)
            var grpAdd = new GroupBox { Header = "Add Technique", Margin = new Thickness(0, 0, 0, 8) };
            var addGrid = new Grid { Margin = new Thickness(8) };
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });   // label
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // combo
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });   // add btn

            addGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            addGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            addGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Prefix row
            var lblPrefix = new TextBlock { Text = "Prefix", VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(lblPrefix, 0); Grid.SetColumn(lblPrefix, 0);
            addGrid.Children.Add(lblPrefix);

            var cbPrefix = new ComboBox { MinWidth = 220, DisplayMemberPath = "Text", Margin = new Thickness(0, 0, 6, 6) };
            cbPrefix.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Prefixes"));
            cbPrefix.SetBinding(Selector.SelectedItemProperty, new Binding("SelectedPrefix") { Mode = BindingMode.TwoWay });
            Grid.SetRow(cbPrefix, 0); Grid.SetColumn(cbPrefix, 1);
            addGrid.Children.Add(cbPrefix);

            var btnAddPrefix = new Button { Content = "+", Width = 32, Margin = new Thickness(0, 0, 0, 6), ToolTip = "Add new prefix" };
            btnAddPrefix.Click += async (_, __) =>
            {
                var txt = PromptForText(this, "Add Prefix", "Enter new prefix (blank allowed):");
                if (txt is null) return; // canceled
                await vm.AddPrefixAndSelectAsync(txt);
            };
            Grid.SetRow(btnAddPrefix, 0); Grid.SetColumn(btnAddPrefix, 2);
            addGrid.Children.Add(btnAddPrefix);

            // Tech row
            var lblTech = new TextBlock { Text = "Tech", VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(lblTech, 1); Grid.SetColumn(lblTech, 0);
            addGrid.Children.Add(lblTech);

            var cbTech = new ComboBox { MinWidth = 220, DisplayMemberPath = "Text", Margin = new Thickness(0, 0, 6, 6) };
            cbTech.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Techs"));
            cbTech.SetBinding(Selector.SelectedItemProperty, new Binding("SelectedTech") { Mode = BindingMode.TwoWay });
            Grid.SetRow(cbTech, 1); Grid.SetColumn(cbTech, 1);
            addGrid.Children.Add(cbTech);

            var btnAddTech = new Button { Content = "+", Width = 32, Margin = new Thickness(0, 0, 0, 6), ToolTip = "Add new tech" };
            btnAddTech.Click += async (_, __) =>
            {
                var txt = PromptForText(this, "Add Tech", "Enter new tech (e.g., T1, T2):");
                if (txt is null || string.IsNullOrWhiteSpace(txt)) return; // required
                await vm.AddTechAndSelectAsync(txt);
            };
            Grid.SetRow(btnAddTech, 1); Grid.SetColumn(btnAddTech, 2);
            addGrid.Children.Add(btnAddTech);

            // Suffix row
            var lblSuffix = new TextBlock { Text = "Suffix", VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(lblSuffix, 2); Grid.SetColumn(lblSuffix, 0);
            addGrid.Children.Add(lblSuffix);

            var cbSuffix = new ComboBox { MinWidth = 260, DisplayMemberPath = "Text", Margin = new Thickness(0, 0, 6, 0) };
            cbSuffix.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Suffixes"));
            cbSuffix.SetBinding(Selector.SelectedItemProperty, new Binding("SelectedSuffix") { Mode = BindingMode.TwoWay });
            Grid.SetRow(cbSuffix, 2); Grid.SetColumn(cbSuffix, 1);
            addGrid.Children.Add(cbSuffix);

            var btnAddSuffix = new Button { Content = "+", Width = 32, ToolTip = "Add new suffix" };
            btnAddSuffix.Click += async (_, __) =>
            {
                var txt = PromptForText(this, "Add Suffix", "Enter new suffix (blank allowed):");
                if (txt is null) return; // canceled
                await vm.AddSuffixAndSelectAsync(txt);
            };
            Grid.SetRow(btnAddSuffix, 2); Grid.SetColumn(btnAddSuffix, 2);
            addGrid.Children.Add(btnAddSuffix);

            // Add button aligned right under rows
            var btnAddTechnique = new Button { Content = "Add Item", Width = 96, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
            btnAddTechnique.SetBinding(Button.CommandProperty, new Binding("AddTechniqueCommand"));
            addGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(btnAddTechnique, 3); Grid.SetColumnSpan(btnAddTechnique, 3);
            addGrid.Children.Add(btnAddTechnique);

            grpAdd.Content = addGrid;
            Grid.SetRow(grpAdd, 0);
            leftGrid.Children.Add(grpAdd);

            // Current Combination preview (fills remaining space)
            var grpPreview = new GroupBox { Header = "Current Combination" };
            var previewGrid = new Grid();
            previewGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            previewGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var lst = new ListBox { BorderThickness = new Thickness(0), Margin = new Thickness(0) };
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
            Grid.SetRow(lst, 0);
            previewGrid.Children.Add(lst);

            grpPreview.Content = previewGrid;
            Grid.SetRow(grpPreview, 1);
            leftGrid.Children.Add(grpPreview);

            // GridSplitter
            var splitter = new GridSplitter
            {
                Width = 6,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
                ShowsPreview = true,
                Background = System.Windows.Media.Brushes.Transparent
            };
            Grid.SetColumn(splitter, 1);
            body.Children.Add(splitter);

            // Right panel: Studyname combinations (if Studyname scope)
            var rightGrid = new Grid();
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetColumn(rightGrid, 2);
            body.Children.Add(rightGrid);

            var grpCombos = new GroupBox { Header = "Studyname Combinations" };
            var combosList = new ListBox { Margin = new Thickness(0) };
            combosList.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("StudynameCombinations"));
            combosList.SetBinding(Selector.SelectedItemProperty, new Binding("SelectedCombination") { Mode = BindingMode.TwoWay });
            // ItemTemplate: show Display and default mark
            var comboDt = new DataTemplate(typeof(StudyTechniqueViewModel.CombinationRow));
            var dock2 = new FrameworkElementFactory(typeof(DockPanel));
            var def = new FrameworkElementFactory(typeof(TextBlock));
            def.SetValue(TextBlock.TextProperty, "¡Ú ");
            def.SetValue(TextBlock.ForegroundProperty, System.Windows.Media.Brushes.Goldenrod);
            var defTrigger = new DataTrigger { Binding = new Binding("IsDefault"), Value = true };
            defTrigger.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible, null));
            var defHidden = new Setter(UIElement.VisibilityProperty, Visibility.Collapsed, null);
            // We cannot attach triggers to FrameworkElementFactory easily; keep star always visible but dim when not default
            dock2.AppendChild(def);
            var disp = new FrameworkElementFactory(typeof(TextBlock));
            disp.SetBinding(TextBlock.TextProperty, new Binding("Display"));
            dock2.AppendChild(disp);
            comboDt.VisualTree = dock2;
            combosList.ItemTemplate = comboDt;
            grpCombos.Content = combosList;
            Grid.SetRow(grpCombos, 0);
            rightGrid.Children.Add(grpCombos);

            var rightButtons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
            var btnSetDefault = new Button { Content = "Set Default", Margin = new Thickness(0, 0, 8, 0) };
            btnSetDefault.SetBinding(Button.CommandProperty, new Binding("SetDefaultForStudynameCommand"));
            rightButtons.Children.Add(btnSetDefault);
            Grid.SetRow(rightButtons, 1);
            rightGrid.Children.Add(rightButtons);

            // Footer
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

        private static string? PromptForText(Window owner, string title, string prompt)
        {
            var w = new Window
            {
                Title = title,
                Width = 420,
                Height = 160,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = System.Windows.Media.Brushes.Black,
                Foreground = System.Windows.Media.Brushes.Gainsboro,
                Owner = owner
            };
            var grid = new Grid { Margin = new Thickness(12) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var lbl = new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 8) };
            Grid.SetRow(lbl, 0); grid.Children.Add(lbl);
            var tb = new TextBox { MinWidth = 360 };
            Grid.SetRow(tb, 1); grid.Children.Add(tb);
            var sp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
            var ok = new Button { Content = "OK", Width = 72, Margin = new Thickness(0, 0, 8, 0) };
            var cancel = new Button { Content = "Cancel", Width = 72 };
            string? result = null;
            ok.Click += (_, __) => { result = tb.Text; w.DialogResult = true; };
            cancel.Click += (_, __) => { w.DialogResult = false; };
            sp.Children.Add(ok); sp.Children.Add(cancel);
            Grid.SetRow(sp, 2); grid.Children.Add(sp);
            w.Content = grid;
            var dr = w.ShowDialog();
            return dr == true ? result : null;
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
