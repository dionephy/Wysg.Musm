using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Wysg.Musm.BooHill;

/// <summary>
/// Window for testing bulk import parsing without actually modifying the database.
/// </summary>
public sealed class BulkImportWindow : Window
{
    private TextBox? _inputBox;
    private TextBlock? _summaryText;
    private ListView? _housesList;
    private ListView? _itemsList;
    private TextBox? _logBox;
    private ListView? _duplicatesList;
    private ListView? _relatedList;
    private ListView? _similarList;
    private BulkParsedHouse? _selectedHouse;
    private HouseView? _selectedSimilar;
    private readonly ObservableCollection<HouseView> SimilarHouses = new();
    private List<HouseView> _existingHouses = new();
    private int _duplicatesImported;
    private int _mergedCount;
    private int _newInserted;
    private readonly string _today = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public ObservableCollection<BulkParsedHouse> ParsedHouses { get; } = new();
    public ObservableCollection<BulkParsedItem> SelectedItems { get; } = new();
    public ObservableCollection<BulkParsedHouse> DuplicateHouses { get; } = new();
    public ObservableCollection<BulkParsedHouse> RelatedDuplicates { get; } = new();

    public BulkImportWindow()
    {
        BuildLayout();
    }

    private void BuildLayout()
    {
        Title = "Bulk Import Simulator";

        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            },
            Padding = new Thickness(16)
        };

        // Header with input and parse button
        var headerPanel = new StackPanel { Spacing = 12 };

        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        titleRow.Children.Add(new TextBlock
        {
            Text = "Bulk Import Simulator",
            FontSize = 24,
            FontWeight = FontWeights.SemiBold
        });

        var parseButton = new Button { Content = "Parse & Simulate" };
        parseButton.Click += ParseButton_Click;
        titleRow.Children.Add(parseButton);

        var importDuplicatesButton = new Button { Content = "Import duplicates" };
        importDuplicatesButton.Click += ImportDuplicates_Click;
        titleRow.Children.Add(importDuplicatesButton);

        var finalizeButton = new Button { Content = "Finalize new" };
        finalizeButton.Click += FinalizeNew_Click;
        titleRow.Children.Add(finalizeButton);

        var clearButton = new Button { Content = "Clear" };
        clearButton.Click += ClearButton_Click;
        titleRow.Children.Add(clearButton);

        _summaryText = new TextBlock
        {
            Text = "Paste text below and click 'Parse & Simulate'",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0)
        };
        titleRow.Children.Add(_summaryText);

        headerPanel.Children.Add(titleRow);

        _inputBox = new TextBox
        {
            PlaceholderText = "Paste bulk listing text here...",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 200,
            VerticalAlignment = VerticalAlignment.Top
        };
        headerPanel.Children.Add(_inputBox);

        _logBox = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 140,
            VerticalAlignment = VerticalAlignment.Top,
            FontSize = 12,
            Header = "Debug log"
        };
        headerPanel.Children.Add(_logBox);

        Grid.SetRow(headerPanel, 0);
        root.Children.Add(headerPanel);

        // Houses list
        var housesSection = new Grid
        {
            Background = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(0, 12, 0, 0),
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            }
        };

        var housesHeader = new StackPanel
        {
            Padding = new Thickness(12),
            Background = (Brush)Application.Current.Resources["LayerFillColorAltBrush"]
        };
        housesHeader.Children.Add(new TextBlock
        {
            Text = "Parsed Houses (click to view items)",
            FontWeight = FontWeights.SemiBold
        });
        housesSection.Children.Add(housesHeader);

        _housesList = new ListView
        {
            SelectionMode = ListViewSelectionMode.Single,
            ItemsSource = ParsedHouses
        };
        _housesList.SelectionChanged += HousesList_SelectionChanged;

        var housesScroll = new ScrollViewer { Content = _housesList };
        Grid.SetRow(housesScroll, 1);
        housesSection.Children.Add(housesScroll);

        Grid.SetRow(housesSection, 1);
        root.Children.Add(housesSection);

        // Items list
        var itemsSection = new Grid
        {
            Background = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(0, 12, 0, 0),
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            }
        };

        var itemsHeader = new StackPanel
        {
            Padding = new Thickness(12),
            Background = (Brush)Application.Current.Resources["LayerFillColorAltBrush"]
        };
        itemsHeader.Children.Add(new TextBlock
        {
            Text = "Items for Selected House",
            FontWeight = FontWeights.SemiBold
        });

        // Column headers for items
        var itemColumns = new Grid { ColumnSpacing = 8, Margin = new Thickness(0, 8, 0, 0) };
        itemColumns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
        itemColumns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        itemColumns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
        itemColumns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        itemColumns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        itemColumns.Children.Add(new TextBlock { Text = "Type", FontWeight = FontWeights.SemiBold });
        var priceHeader = new TextBlock { Text = "Price", FontWeight = FontWeights.SemiBold };
        Grid.SetColumn(priceHeader, 1);
        itemColumns.Children.Add(priceHeader);
        var officeHeader = new TextBlock { Text = "Office", FontWeight = FontWeights.SemiBold };
        Grid.SetColumn(officeHeader, 2);
        itemColumns.Children.Add(officeHeader);
        var dateHeader = new TextBlock { Text = "Updated", FontWeight = FontWeights.SemiBold };
        Grid.SetColumn(dateHeader, 3);
        itemColumns.Children.Add(dateHeader);
        var remarkHeader = new TextBlock { Text = "Remark", FontWeight = FontWeights.SemiBold };
        Grid.SetColumn(remarkHeader, 4);
        itemColumns.Children.Add(remarkHeader);

        itemsHeader.Children.Add(itemColumns);
        itemsSection.Children.Add(itemsHeader);

        _itemsList = new ListView
        {
            SelectionMode = ListViewSelectionMode.None,
            ItemsSource = SelectedItems,
            ItemTemplate = CreateItemTemplate()
        };

        var itemsScroll = new ScrollViewer { Content = _itemsList };
        Grid.SetRow(itemsScroll, 1);
        itemsSection.Children.Add(itemsScroll);

        var relatedHeader = new TextBlock
        {
            Text = "Existing duplicates (added automatically)",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(12, 8, 12, 0)
        };
        Grid.SetRow(relatedHeader, 2);
        itemsSection.Children.Add(relatedHeader);

        _relatedList = new ListView
        {
            SelectionMode = ListViewSelectionMode.None,
            ItemsSource = RelatedDuplicates,
            ItemTemplate = CreateDuplicateTemplate(),
            Margin = new Thickness(12, 0, 12, 12)
        };
        var relatedScroll = new ScrollViewer { Content = _relatedList };
        Grid.SetRow(relatedScroll, 3);
        itemsSection.Children.Add(relatedScroll);

        var similarHeader = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(12, 8, 12, 0)
        };
        similarHeader.Children.Add(new TextBlock
        {
            Text = "Similar houses (select to merge)",
            FontWeight = FontWeights.SemiBold
        });
        var mergeButton = new Button { Content = "Merge", Margin = new Thickness(12, 0, 0, 0) };
        mergeButton.Click += MergeSelected_Click;
        similarHeader.Children.Add(mergeButton);
        Grid.SetRow(similarHeader, 4);
        itemsSection.Children.Add(similarHeader);

        _similarList = new ListView
        {
            SelectionMode = ListViewSelectionMode.Single,
            ItemsSource = SimilarHouses,
            Margin = new Thickness(12, 0, 12, 12)
        };
        _similarList.SelectionChanged += SimilarList_SelectionChanged;
        var similarScroll = new ScrollViewer { Content = _similarList };
        Grid.SetRow(similarScroll, 5);
        itemsSection.Children.Add(similarScroll);

        Grid.SetRow(itemsSection, 2);
        root.Children.Add(itemsSection);

        // Duplicates list
        var dupSection = new Grid
        {
            Background = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(0, 12, 0, 0),
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            }
        };

        var dupHeader = new StackPanel
        {
            Padding = new Thickness(12),
            Background = (Brush)Application.Current.Resources["LayerFillColorAltBrush"]
        };
        dupHeader.Children.Add(new TextBlock
        {
            Text = "Duplicates (skipped)",
            FontWeight = FontWeights.SemiBold
        });
        dupSection.Children.Add(dupHeader);

        _duplicatesList = new ListView
        {
            SelectionMode = ListViewSelectionMode.None,
            ItemsSource = DuplicateHouses,
            ItemTemplate = CreateDuplicateTemplate()
        };

        var dupScroll = new ScrollViewer { Content = _duplicatesList };
        Grid.SetRow(dupScroll, 1);
        dupSection.Children.Add(dupScroll);

        Grid.SetRow(dupSection, 3);
        root.Children.Add(dupSection);

        Content = root;
    }

    private async Task ApplyDatabaseDuplicateCheckAsync(BulkImportResult result)
    {
        var repo = await BooHillRepository.CreateAsync().ConfigureAwait(false);

        var buildingNumbers = result.Houses
            .Select(h => h.BuildingNumber)
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var areas = result.Houses
            .Select(h => h.Area)
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // We use the dominant area filter if there is exactly one; otherwise no area filter.
        string? areaFilter = areas.Count == 1 ? areas[0] : null;

        _existingHouses = (await repo.GetHousesWithItemsAsync(buildingNumbers, areaFilter).ConfigureAwait(false)).ToList();

        foreach (var parsed in result.Houses)
        {
            var matchingExisting = _existingHouses.Where(e => HouseIdentityMatches(parsed, e)).ToList();

            var shared = matchingExisting.FirstOrDefault(e => HasSharedItem(parsed.Items, e.Items));
            if (shared != null)
            {
                parsed.IsDuplicate = true;
                parsed.DuplicateReason = "Matches existing DB house with shared item";
                parsed.MatchedHouseId = shared.HouseId;
                DuplicateHouses.Add(parsed);
                result.Logs.Add($"DB duplicate: {parsed.Display} matches house_id={shared.HouseId}");
            }
            else
            {
                ParsedHouses.Add(parsed);
            }
        }
    }

    private static bool HouseIdentityMatches(BulkParsedHouse parsed, HouseView existing)
    {
        var unitParsed = NormalizeUnit(parsed.UnitNumber);
        var unitExisting = NormalizeUnit(existing.UnitNumber);

        return string.Equals(parsed.BuildingNumber?.Trim(), existing.BuildingNumber?.Trim(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(parsed.Area?.Trim(), existing.Area?.Trim(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(unitParsed, unitExisting, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasSharedItem(IEnumerable<BulkParsedItem> parsedItems, IEnumerable<ItemRecord> existingItems)
    {
        var existingKeys = new HashSet<string>(existingItems.Select(ItemKey), StringComparer.OrdinalIgnoreCase);
        return parsedItems.Any(pi => existingKeys.Contains(ItemKey(pi)));
    }

    private static string NormalizeUnit(string? unit)
    {
        return string.IsNullOrWhiteSpace(unit) ? string.Empty : unit.Trim();
    }

    private static string ItemKey(BulkParsedItem item)
    {
        var price = item.Price?.ToString("G17", CultureInfo.InvariantCulture) ?? "<null>";
        var office = item.Office?.Trim() ?? "<null>";
        var remark = item.Remark?.Trim() ?? "<null>";
        return $"{price}|{office}|{remark}";
    }

    private static string ItemKey(ItemRecord item)
    {
        var price = item.Price?.ToString("G17", CultureInfo.InvariantCulture) ?? "<null>";
        var office = item.Office?.Trim() ?? "<null>";
        var remark = item.Remark?.Trim() ?? "<null>";
        return $"{price}|{office}|{remark}";
    }

    private static DataTemplate CreateItemTemplate()
    {
        var xaml = @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
    <Grid Padding='8' ColumnSpacing='8'>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width='60'/>
            <ColumnDefinition Width='100'/>
            <ColumnDefinition Width='200'/>
            <ColumnDefinition Width='100'/>
            <ColumnDefinition Width='*'/>
        </Grid.ColumnDefinitions>
        <TextBlock Text='{Binding TransactionType}' />
        <TextBlock Grid.Column='1' Text='{Binding PriceDisplay}' />
        <TextBlock Grid.Column='2' Text='{Binding Office}' TextTrimming='CharacterEllipsis' />
        <TextBlock Grid.Column='3' Text='{Binding LastUpdatedDate}' />
        <TextBlock Grid.Column='4' Text='{Binding Remark}' TextWrapping='Wrap' />
    </Grid>
</DataTemplate>";
        return (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
    }

    private static DataTemplate CreateDuplicateTemplate()
    {
        var xaml = @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
    <TextBlock Text='{Binding Display}' Foreground='Red' Padding='8' />
</DataTemplate>";
        return (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
    }

    private async void ImportDuplicates_Click(object sender, RoutedEventArgs e)
    {
        if (DuplicateHouses.Count == 0)
        {
            return;
        }

        var repo = await BooHillRepository.CreateAsync().ConfigureAwait(false);
        var added = 0;
        foreach (var dup in DuplicateHouses)
        {
            if (dup.MatchedHouseId == null)
            {
                continue;
            }

            added += await repo.AddItemsAsync(dup.MatchedHouseId.Value, dup.Items, _today).ConfigureAwait(false);
        }

        _duplicatesImported += added;
        DispatcherQueue?.TryEnqueue(UpdateSummaryText);
    }

    private async void MergeSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedHouse == null || _selectedSimilar == null)
        {
            return;
        }

        var repo = await BooHillRepository.CreateAsync().ConfigureAwait(false);
        var added = await repo.AddItemsAsync(_selectedSimilar.HouseId, _selectedHouse.Items, _today).ConfigureAwait(false);
        _mergedCount += added > 0 ? 1 : 0;

        ParsedHouses.Remove(_selectedHouse);
        SimilarHouses.Clear();
        _selectedSimilar = null;
        _selectedHouse = null;
        SelectedItems.Clear();

        DispatcherQueue?.TryEnqueue(UpdateSummaryText);
    }

    private async void FinalizeNew_Click(object sender, RoutedEventArgs e)
    {
        if (ParsedHouses.Count == 0)
        {
            return;
        }

        var repo = await BooHillRepository.CreateAsync().ConfigureAwait(false);
        var insertedHouses = 0;
        var insertedItems = 0;

        foreach (var house in ParsedHouses.ToList())
        {
            var newId = await repo.InsertHouseWithItemsAsync(house, _today).ConfigureAwait(false);
            insertedHouses++;
            insertedItems += house.Items.Count;
        }

        ParsedHouses.Clear();
        SelectedItems.Clear();
        _newInserted += insertedHouses;
        DispatcherQueue?.TryEnqueue(UpdateSummaryText);
    }

    private void UpdateSummaryText()
    {
        if (_summaryText == null)
        {
            return;
        }

        var addableHouses = ParsedHouses.Count;
        var addableItems = ParsedHouses.Sum(h => h.Items.Count);
        _summaryText.Text = $"Remaining: {addableHouses} houses / {addableItems} items | Imported dup items: {_duplicatesImported}, Merged: {_mergedCount}, New houses: {_newInserted}";
    }

    private async void ParseButton_Click(object sender, RoutedEventArgs e)
    {
        var rawText = _inputBox?.Text ?? string.Empty;
        var result = BulkImportParser.Parse(rawText);

        ParsedHouses.Clear();
        SelectedItems.Clear();
        DuplicateHouses.Clear();
        RelatedDuplicates.Clear();
        SimilarHouses.Clear();
        _existingHouses.Clear();
        _duplicatesImported = 0;
        _mergedCount = 0;
        _newInserted = 0;
        await ApplyDatabaseDuplicateCheckAsync(result);

        if (_summaryText != null)
        {
            var addableHouses = ParsedHouses.Count;
            var addableItems = ParsedHouses.Sum(h => h.Items.Count);
            _summaryText.Text = $"Would add: {addableHouses} houses, {addableItems} items (duplicates: {DuplicateHouses.Count})";
        }

        if (_logBox != null)
        {
            _logBox.Text = string.Join(Environment.NewLine, result.Logs);
        }

        // Select first house if any
        if (ParsedHouses.Count > 0 && _housesList != null)
        {
            _housesList.SelectedIndex = 0;
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        if (_inputBox != null)
        {
            _inputBox.Text = string.Empty;
        }

        ParsedHouses.Clear();
        SelectedItems.Clear();
        DuplicateHouses.Clear();
        RelatedDuplicates.Clear();
        SimilarHouses.Clear();
        _existingHouses.Clear();
        _duplicatesImported = 0;
        _mergedCount = 0;
        _newInserted = 0;

        if (_summaryText != null)
        {
            _summaryText.Text = "Paste text below and click 'Parse & Simulate'";
        }

        if (_logBox != null)
        {
            _logBox.Text = string.Empty;
        }
    }

    private void HousesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedItems.Clear();

        if (_housesList?.SelectedItem is BulkParsedHouse house)
        {
            _selectedHouse = house;
            foreach (var item in house.Items)
            {
                SelectedItems.Add(item);
            }

            RelatedDuplicates.Clear();
            foreach (var dup in DuplicateHouses.Where(d => d.Key == house.Key))
            {
                RelatedDuplicates.Add(dup);
            }

            SimilarHouses.Clear();
            _selectedSimilar = null;
            foreach (var similar in _existingHouses.Where(e => HouseIdentityMatches(house, e)))
            {
                SimilarHouses.Add(similar);
            }
        }
        else
        {
            _selectedHouse = null;
            RelatedDuplicates.Clear();
            SimilarHouses.Clear();
            _selectedSimilar = null;
        }
    }

    private void SimilarList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_similarList?.SelectedItem is HouseView hv)
        {
            _selectedSimilar = hv;
        }
        else
        {
            _selectedSimilar = null;
        }
    }
}
