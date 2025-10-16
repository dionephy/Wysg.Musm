using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Views
{
    public partial class StudynameTechniqueWindow : Window
    {
        public StudynameTechniqueWindow(StudynameTechniqueViewModel vm)
        {
            Title = "Manage Studyname Techniques";
            Width = 900; Height = 600; WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = System.Windows.Media.Brushes.Black; Foreground = System.Windows.Media.Brushes.Gainsboro;
            DataContext = vm;

            var root = new Grid { Margin = new Thickness(8) };
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) }); // Left panel
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Splitter
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }); // Right panel

            // LEFT PANEL: Build new combinations
            var leftPanel = new Grid { Margin = new Thickness(0, 0, 4, 0) };
            leftPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            leftPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Builder UI
            leftPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Current combination
            leftPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // All combinations
            leftPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Save button
            Grid.SetColumn(leftPanel, 0);
            root.Children.Add(leftPanel);

            // Left header
            var leftHeader = new TextBlock { Text = "Build New Combination", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 8) };
            Grid.SetRow(leftHeader, 0);
            leftPanel.Children.Add(leftHeader);

            // Builder controls group
            var builderGroup = new GroupBox 
            { 
                Header = "Add Techniques", 
                Margin = new Thickness(0, 0, 0, 8),
                BorderBrush = System.Windows.Media.Brushes.DimGray,
                BorderThickness = new Thickness(1)
            };
            var builderStack = new StackPanel { Margin = new Thickness(4) };

            // Prefix row
            var prefixStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            prefixStack.Children.Add(new TextBlock { Text = "Prefix:", Width = 60, VerticalAlignment = VerticalAlignment.Center });
            var cmbPrefix = new ComboBox { Width = 200, Margin = new Thickness(4, 0, 4, 0) };
            cmbPrefix.SetBinding(ComboBox.ItemsSourceProperty, new Binding("Prefixes"));
            cmbPrefix.SetBinding(ComboBox.SelectedItemProperty, new Binding("SelectedPrefix") { Mode = BindingMode.TwoWay });
            prefixStack.Children.Add(cmbPrefix);
            var btnAddPrefix = new Button { Content = "+", Width = 30, Padding = new Thickness(2) };
            btnAddPrefix.Click += async (_, __) => await AddPrefixAsync();
            prefixStack.Children.Add(btnAddPrefix);
            builderStack.Children.Add(prefixStack);

            // Tech row
            var techStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            techStack.Children.Add(new TextBlock { Text = "Tech:", Width = 60, VerticalAlignment = VerticalAlignment.Center });
            var cmbTech = new ComboBox { Width = 200, Margin = new Thickness(4, 0, 4, 0) };
            cmbTech.SetBinding(ComboBox.ItemsSourceProperty, new Binding("Techs"));
            cmbTech.SetBinding(ComboBox.SelectedItemProperty, new Binding("SelectedTech") { Mode = BindingMode.TwoWay });
            techStack.Children.Add(cmbTech);
            var btnAddTech = new Button { Content = "+", Width = 30, Padding = new Thickness(2) };
            btnAddTech.Click += async (_, __) => await AddTechAsync();
            techStack.Children.Add(btnAddTech);
            builderStack.Children.Add(techStack);

            // Suffix row
            var suffixStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            suffixStack.Children.Add(new TextBlock { Text = "Suffix:", Width = 60, VerticalAlignment = VerticalAlignment.Center });
            var cmbSuffix = new ComboBox { Width = 200, Margin = new Thickness(4, 0, 4, 0) };
            cmbSuffix.SetBinding(ComboBox.ItemsSourceProperty, new Binding("Suffixes"));
            cmbSuffix.SetBinding(ComboBox.SelectedItemProperty, new Binding("SelectedSuffix") { Mode = BindingMode.TwoWay });
            suffixStack.Children.Add(cmbSuffix);
            var btnAddSuffix = new Button { Content = "+", Width = 30, Padding = new Thickness(2) };
            btnAddSuffix.Click += async (_, __) => await AddSuffixAsync();
            suffixStack.Children.Add(btnAddSuffix);
            builderStack.Children.Add(suffixStack);

            // Add button
            var btnAddToCombo = new Button { Content = "Add to Combination", Margin = new Thickness(0, 8, 0, 0), Padding = new Thickness(8, 4, 8, 4) };
            btnAddToCombo.SetBinding(Button.CommandProperty, new Binding("AddTechniqueCommand"));
            builderStack.Children.Add(btnAddToCombo);

            builderGroup.Content = builderStack;
            Grid.SetRow(builderGroup, 1);
            leftPanel.Children.Add(builderGroup);

            // Current combination list
            var comboGroup = new GroupBox 
            { 
                Header = "Current Combination (double-click to remove)", 
                Margin = new Thickness(0, 0, 0, 8),
                BorderBrush = System.Windows.Media.Brushes.DimGray,
                BorderThickness = new Thickness(1)
            };
            var lstCurrentCombo = new ListBox 
            { 
                Margin = new Thickness(4),
                Background = System.Windows.Media.Brushes.Black,
                BorderBrush = System.Windows.Media.Brushes.DimGray,
                DisplayMemberPath = "TechniqueDisplay"
            };
            lstCurrentCombo.SetBinding(ListBox.ItemsSourceProperty, new Binding("CurrentCombinationItems"));
            lstCurrentCombo.MouseDoubleClick += OnCurrentCombinationDoubleClick;
            comboGroup.Content = lstCurrentCombo;
            Grid.SetRow(comboGroup, 2);
            leftPanel.Children.Add(comboGroup);

            // All combinations list
            var allComboGroup = new GroupBox 
            { 
                Header = "All Combinations (double-click to load)", 
                Margin = new Thickness(0, 0, 0, 8),
                BorderBrush = System.Windows.Media.Brushes.DimGray,
                BorderThickness = new Thickness(1)
            };
            var lstAllCombos = new ListBox 
            { 
                Margin = new Thickness(4),
                Background = System.Windows.Media.Brushes.Black,
                BorderBrush = System.Windows.Media.Brushes.DimGray,
                DisplayMemberPath = "Display"
            };
            lstAllCombos.SetBinding(ListBox.ItemsSourceProperty, new Binding("AllCombinations"));
            lstAllCombos.MouseDoubleClick += OnAllCombinationsDoubleClick;
            allComboGroup.Content = lstAllCombos;
            Grid.SetRow(allComboGroup, 3);
            leftPanel.Children.Add(allComboGroup);

            // Save button
            var btnSaveNew = new Button 
            { 
                Content = "Save as New Combination",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(12, 4, 12, 4)
            };
            btnSaveNew.SetBinding(Button.CommandProperty, new Binding("SaveNewCombinationCommand"));
            Grid.SetRow(btnSaveNew, 4);
            leftPanel.Children.Add(btnSaveNew);

            // SPLITTER
            var splitter = new GridSplitter 
            { 
                Width = 4, 
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = System.Windows.Media.Brushes.DimGray
            };
            Grid.SetColumn(splitter, 1);
            root.Children.Add(splitter);

            // RIGHT PANEL: Existing combinations
            var rightPanel = new Grid { Margin = new Thickness(4, 0, 0, 0) };
            rightPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            rightPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Info text
            rightPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // List
            rightPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons container
            rightPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Close button
            Grid.SetColumn(rightPanel, 2);
            root.Children.Add(rightPanel);

            // Right header
            var header = new TextBlock();
            header.SetBinding(TextBlock.TextProperty, new Binding("Header"));
            header.FontWeight = FontWeights.Bold;
            Grid.SetRow(header, 0);
            rightPanel.Children.Add(header);

            // Info text
            var info = new TextBlock 
            { 
                Text = "Existing combinations (select to set default or delete):",
                Margin = new Thickness(0,4,0,4) 
            };
            Grid.SetRow(info, 1);
            rightPanel.Children.Add(info);

            // DataGrid for combinations
            var grid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                CanUserResizeRows = false,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                SelectionMode = DataGridSelectionMode.Single,
                GridLinesVisibility = DataGridGridLinesVisibility.None,
                Background = System.Windows.Media.Brushes.Black,
                RowBackground = System.Windows.Media.Brushes.Black,
                AlternatingRowBackground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 20, 20)),
                BorderBrush = System.Windows.Media.Brushes.DimGray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0),
                Foreground = System.Windows.Media.Brushes.Gainsboro // Add explicit foreground for cells
            };
            grid.SetBinding(DataGrid.ItemsSourceProperty, new Binding("Combinations"));
            grid.SetBinding(DataGrid.SelectedItemProperty, new Binding("SelectedCombination") { Mode = BindingMode.TwoWay });

            // Display column with explicit ElementStyle for Foreground
            var colDisplay = new DataGridTextColumn
            {
                Header = "Technique Combination",
                Binding = new Binding("Display"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                IsReadOnly = true
            };
            // FIX: Add ElementStyle to set Foreground explicitly
            var textBlockStyle = new Style(typeof(TextBlock));
            textBlockStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.Gainsboro));
            colDisplay.ElementStyle = textBlockStyle;
            grid.Columns.Add(colDisplay);

            // IsDefault column with checkmark
            var colDefaultTemplate = new DataGridTemplateColumn
            {
                Header = "Default",
                Width = new DataGridLength(80)
            };
            var cellTemplate = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(TextBlock));
            factory.SetValue(TextBlock.TextProperty, new Binding("IsDefault") 
            { 
                Converter = new BoolToCheckmarkConverter() 
            });
            factory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            factory.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            factory.SetValue(TextBlock.ForegroundProperty, Brushes.Gainsboro); // Add foreground to template
            cellTemplate.VisualTree = factory;
            colDefaultTemplate.CellTemplate = cellTemplate;
            grid.Columns.Add(colDefaultTemplate);

            Grid.SetRow(grid, 2);
            rightPanel.Children.Add(grid);

            // Buttons container for Set Default and Delete
            var buttonsContainer = new StackPanel 
            { 
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 8, 0, 0)
            };
            
            // Set Default button
            var btnSetDefault = new Button 
            { 
                Content = "Set Selected As Default",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 4),
                Padding = new Thickness(12, 4, 12, 4)
            };
            btnSetDefault.SetBinding(Button.CommandProperty, new Binding("SetDefaultCommand"));
            buttonsContainer.Children.Add(btnSetDefault);

            // Delete button
            var btnDelete = new Button 
            { 
                Content = "Delete Selected Combination",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(12, 4, 12, 4)
            };
            btnDelete.SetBinding(Button.CommandProperty, new Binding("DeleteCombinationCommand"));
            buttonsContainer.Children.Add(btnDelete);

            Grid.SetRow(buttonsContainer, 3);
            rightPanel.Children.Add(buttonsContainer);

            // Close button
            var btnClose = new Button 
            { 
                Content = "Close", 
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 8, 0, 0),
                Padding = new Thickness(12, 4, 12, 4) 
            };
            btnClose.Click += (_, __) => Close();
            Grid.SetRow(btnClose, 4);
            rightPanel.Children.Add(btnClose);

            Content = root;
        }

        private async System.Threading.Tasks.Task AddPrefixAsync()
        {
            var vm = DataContext as StudynameTechniqueViewModel;
            if (vm == null) return;

            var dialog = new Window 
            { 
                Title = "Add Prefix",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = System.Windows.Media.Brushes.Black,
                Foreground = System.Windows.Media.Brushes.Gainsboro
            };

            var stack = new StackPanel { Margin = new Thickness(12) };
            stack.Children.Add(new TextBlock { Text = "Prefix text:", Margin = new Thickness(0, 0, 0, 4) });
            var txt = new TextBox { Margin = new Thickness(0, 0, 0, 12) };
            stack.Children.Add(txt);

            var btnStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnOk = new Button { Content = "Add", Width = 70, Margin = new Thickness(0, 0, 8, 0) };
            btnOk.Click += (_, __) => { dialog.DialogResult = true; dialog.Close(); };
            var btnCancel = new Button { Content = "Cancel", Width = 70 };
            btnCancel.Click += (_, __) => { dialog.DialogResult = false; dialog.Close(); };
            btnStack.Children.Add(btnOk);
            btnStack.Children.Add(btnCancel);
            stack.Children.Add(btnStack);

            dialog.Content = stack;
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(txt.Text))
            {
                await vm.AddPrefixAndSelectAsync(txt.Text.Trim());
            }
        }

        private async System.Threading.Tasks.Task AddTechAsync()
        {
            var vm = DataContext as StudynameTechniqueViewModel;
            if (vm == null) return;

            var dialog = new Window 
            { 
                Title = "Add Tech",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = System.Windows.Media.Brushes.Black,
                Foreground = System.Windows.Media.Brushes.Gainsboro
            };

            var stack = new StackPanel { Margin = new Thickness(12) };
            stack.Children.Add(new TextBlock { Text = "Tech text:", Margin = new Thickness(0, 0, 0, 4) });
            var txt = new TextBox { Margin = new Thickness(0, 0, 0, 12) };
            stack.Children.Add(txt);

            var btnStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnOk = new Button { Content = "Add", Width = 70, Margin = new Thickness(0, 0, 8, 0) };
            btnOk.Click += (_, __) => { dialog.DialogResult = true; dialog.Close(); };
            var btnCancel = new Button { Content = "Cancel", Width = 70 };
            btnCancel.Click += (_, __) => { dialog.DialogResult = false; dialog.Close(); };
            btnStack.Children.Add(btnOk);
            btnStack.Children.Add(btnCancel);
            stack.Children.Add(btnStack);

            dialog.Content = stack;
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(txt.Text))
            {
                await vm.AddTechAndSelectAsync(txt.Text.Trim());
            }
        }

        private async System.Threading.Tasks.Task AddSuffixAsync()
        {
            var vm = DataContext as StudynameTechniqueViewModel;
            if (vm == null) return;

            var dialog = new Window 
            { 
                Title = "Add Suffix",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = System.Windows.Media.Brushes.Black,
                Foreground = System.Windows.Media.Brushes.Gainsboro
            };

            var stack = new StackPanel { Margin = new Thickness(12) };
            stack.Children.Add(new TextBlock { Text = "Suffix text:", Margin = new Thickness(0, 0, 0, 4) });
            var txt = new TextBox { Margin = new Thickness(0, 0, 0, 12) };
            stack.Children.Add(txt);

            var btnStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnOk = new Button { Content = "Add", Width = 70, Margin = new Thickness(0, 0, 8, 0) };
            btnOk.Click += (_, __) => { dialog.DialogResult = true; dialog.Close(); };
            var btnCancel = new Button { Content = "Cancel", Width = 70 };
            btnCancel.Click += (_, __) => { dialog.DialogResult = false; dialog.Close(); };
            btnStack.Children.Add(btnOk);
            btnStack.Children.Add(btnCancel);
            stack.Children.Add(btnStack);

            dialog.Content = stack;
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(txt.Text))
            {
                await vm.AddSuffixAndSelectAsync(txt.Text.Trim());
            }
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

        private void OnCurrentCombinationDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var vm = DataContext as StudynameTechniqueViewModel;
            if (vm == null) return;

            var listBox = sender as ListBox;
            if (listBox?.SelectedItem is StudynameTechniqueViewModel.CombinationItem item)
            {
                vm.RemoveFromCurrentCombination(item);
            }
        }

        private async void OnAllCombinationsDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var vm = DataContext as StudynameTechniqueViewModel;
            if (vm == null) return;

            var listBox = sender as ListBox;
            if (listBox?.SelectedItem is StudynameTechniqueViewModel.AllCombinationRow row)
            {
                await vm.LoadCombinationIntoCurrentAsync(row.CombinationId);
            }
        }

        // Converter for boolean to checkmark display
        private class BoolToCheckmarkConverter : System.Windows.Data.IValueConverter
        {
            public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value is bool b && b) return "?";
                return string.Empty;
            }

            public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
