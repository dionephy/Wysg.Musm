using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wysg.Musm.BooHill
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private BooHillRepository? _repository;
        private FilterOptions _currentFilters = new();

        private Grid? _rootGrid;
        private TextBlock? _statusText;
        private ListView? _houseList;
        private ComboBox? _clusterCombo;
        private TextBox? _buildingBox;
        private TextBox? _unitBox;
        private TextBox? _areaBox;
        private CheckBox? _showSoldCheck;
        private TextBox? _minValueBox;
        private TextBox? _maxValueBox;
        private TextBox? _minRankBox;
        private TextBox? _maxRankBox;

        public ObservableCollection<HouseView> Houses { get; } = new();
        public ObservableCollection<ClusterRecord> Clusters { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                WireUpControls();
                _repository = await BooHillRepository.CreateAsync();
                await LoadClustersAsync();
                await LoadHousesAsync(_currentFilters);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Failed to initialize", ex.Message);
            }
        }

        private async Task LoadClustersAsync()
        {
            if (_repository == null)
            {
                return;
            }

            Clusters.Clear();
            var clusters = await _repository.GetClustersAsync();
            foreach (var cluster in clusters)
            {
                Clusters.Add(cluster);
            }
        }

        private async Task LoadHousesAsync(FilterOptions filters)
        {
            if (_repository == null)
            {
                return;
            }

            if (_statusText != null)
            {
                _statusText.Text = "Loading...";
            }

            if (_houseList != null)
            {
                _houseList.IsEnabled = false;
            }

            try
            {
                var houses = await _repository.GetHousesAsync(filters);
                Houses.Clear();
                foreach (var house in houses)
                {
                    Houses.Add(house);
                }

                // Preload items so status/office group counts are available even when rows are collapsed.
                await PopulateHouseItemsAsync(Houses);

                if (_statusText != null)
                {
                    _statusText.Text = Houses.Count == 0 ? "No listings found." : $"Showing {Houses.Count} listings.";
                }
            }
            catch (Exception ex)
            {
                if (_statusText != null)
                {
                    _statusText.Text = "Failed to load listings.";
                }
                await ShowErrorAsync("Load failed", ex.Message);
            }
            finally
            {
                if (_houseList != null)
                {
                    _houseList.IsEnabled = true;
                }
            }
        }

        private async Task ShowErrorAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "Close",
                XamlRoot = (Content as FrameworkElement)?.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private async void ApplyFilters_Click(object sender, RoutedEventArgs e)
        {
            _currentFilters = ReadFiltersFromUi();
            await LoadHousesAsync(_currentFilters);
        }

        private async void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            if (_clusterCombo != null)
            {
                _clusterCombo.SelectedItem = null;
            }

            if (_buildingBox != null) _buildingBox.Text = string.Empty;
            if (_unitBox != null) _unitBox.Text = string.Empty;
            if (_areaBox != null) _areaBox.Text = string.Empty;
            if (_showSoldCheck != null) _showSoldCheck.IsChecked = false;
            if (_minValueBox != null) _minValueBox.Text = string.Empty;
            if (_maxValueBox != null) _maxValueBox.Text = string.Empty;
            if (_minRankBox != null) _minRankBox.Text = string.Empty;
            if (_maxRankBox != null) _maxRankBox.Text = string.Empty;

            _currentFilters = new FilterOptions();
            await LoadHousesAsync(_currentFilters);
        }

        private void OpenAdmin_Click(object sender, RoutedEventArgs e)
        {
            var window = new AdminWindow();
            window.Activate();
        }

        private void OpenBulkImport_Click(object sender, RoutedEventArgs e)
        {
            var window = new BulkImportWindow();
            window.Activate();
        }

        private FilterOptions ReadFiltersFromUi()
        {
            int? selectedClusterId = null;
            if (_clusterCombo?.SelectedValue is int selectedValue)
            {
                selectedClusterId = selectedValue;
            }
            else if (_clusterCombo?.SelectedItem is ClusterRecord record)
            {
                selectedClusterId = record.ClusterId;
            }

            return new FilterOptions
            {
                ClusterId = selectedClusterId,
                BuildingNumber = NormalizeInput(_buildingBox?.Text),
                UnitNumber = NormalizeInput(_unitBox?.Text),
                Area = NormalizeInput(_areaBox?.Text),
                ShowSold = _showSoldCheck?.IsChecked == true,
                MinValue = TryParseDouble(NormalizeInput(_minValueBox?.Text)),
                MaxValue = TryParseDouble(NormalizeInput(_maxValueBox?.Text)),
                MinRank = TryParseDouble(NormalizeInput(_minRankBox?.Text)),
                MaxRank = TryParseDouble(NormalizeInput(_maxRankBox?.Text)),
                SortField = _currentFilters.SortField,
                SortDirection = _currentFilters.SortDirection
            };
        }

        private static string NormalizeInput(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static double? TryParseDouble(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            return double.TryParse(text, out var value) ? value : null;
        }

        private async void Favorite_Click(object sender, RoutedEventArgs e)
        {
            if (_repository == null)
            {
                return;
            }

            if (sender is Button button && button.Tag is long houseId)
            {
                try
                {
                    await _repository.ToggleFavoriteAsync(houseId);
                    await LoadHousesAsync(_currentFilters);
                }
                catch (Exception ex)
                {
                    await ShowErrorAsync("Failed to toggle favorite", ex.Message);
                }
            }
        }

        private async void SortBuilding_Click(object sender, RoutedEventArgs e)
        {
            ToggleSort(SortField.Building);
            _currentFilters = ReadFiltersFromUi();
            _currentFilters.SortField = SortField.Building;
            await LoadHousesAsync(_currentFilters);
        }

        private async void SortPrice_Click(object sender, RoutedEventArgs e)
        {
            ToggleSort(SortField.PriceRange);
            _currentFilters = ReadFiltersFromUi();
            _currentFilters.SortField = SortField.PriceRange;
            await LoadHousesAsync(_currentFilters);
        }

        private void ToggleSort(SortField field)
        {
            if (_currentFilters.SortField == field)
            {
                _currentFilters.SortDirection = _currentFilters.SortDirection == SortDirection.Ascending
                    ? SortDirection.Descending
                    : SortDirection.Ascending;
            }
            else
            {
                _currentFilters.SortField = field;
                _currentFilters.SortDirection = SortDirection.Ascending;
            }
        }

        private void WireUpControls()
        {
            _rootGrid = FindControl<Grid>("RootGrid");
            _statusText = FindControl<TextBlock>("StatusText");
            _houseList = FindControl<ListView>("HouseList");
            _clusterCombo = FindControl<ComboBox>("ClusterCombo");
            _buildingBox = FindControl<TextBox>("BuildingBox");
            _unitBox = FindControl<TextBox>("UnitBox");
            _areaBox = FindControl<TextBox>("AreaBox");
            _showSoldCheck = FindControl<CheckBox>("ShowSoldCheck");
            _minValueBox = FindControl<TextBox>("MinValueBox");
            _maxValueBox = FindControl<TextBox>("MaxValueBox");
            _minRankBox = FindControl<TextBox>("MinRankBox");
            _maxRankBox = FindControl<TextBox>("MaxRankBox");

            if (_rootGrid != null)
            {
                _rootGrid.DataContext = this;
            }
        }

        private T? FindControl<T>(string name) where T : class
        {
            return (Content as FrameworkElement)?.FindName(name) as T;
        }

        private async void HouseExpander_Expanding(Expander sender, ExpanderExpandingEventArgs args)
        {
            if (_repository == null)
            {
                return;
            }

            if (sender.DataContext is not HouseView house)
            {
                return;
            }

            if (house.Items.Count > 0)
            {
                return;
            }

            var items = await _repository.GetItemsForHouseAsync(house.HouseId);
            foreach (var item in items)
            {
                house.Items.Add(item);
            }
        }

        private async Task PopulateHouseItemsAsync(IEnumerable<HouseView> houses)
        {
            if (_repository == null)
            {
                return;
            }

            foreach (var house in houses)
            {
                if (house.Items.Count > 0)
                {
                    continue;
                }

                var items = await _repository.GetItemsForHouseAsync(house.HouseId);
                foreach (var item in items)
                {
                    house.Items.Add(item);
                }
            }
        }

        private void HouseExpander_Collapsed(Expander sender, ExpanderCollapsedEventArgs args)
        {
            // Keep items loaded so status/office-group counts stay visible when collapsed.
        }
    }
}
