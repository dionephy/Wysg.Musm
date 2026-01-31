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
        Title = "한꺼번에 가져오기";

        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            },
            Padding = new Thickness(16)
        };

        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        titleRow.Children.Add(new TextBlock
        {
            Text = "한꺼번에 가져오기",
            FontSize = 24,
            FontWeight = FontWeights.SemiBold
        });

        var parseButton = new Button { Content = "(1) 시뮬레이션" };
        parseButton.Click += ParseButton_Click;
        titleRow.Children.Add(parseButton);

        var importDuplicatesButton = new Button { Content = "(3) 기존 매물 가져오기" };
        importDuplicatesButton.Click += ImportDuplicates_Click;
        titleRow.Children.Add(importDuplicatesButton);

        var finalizeButton = new Button { Content = "(4) 새 매물 가져오기" };
        finalizeButton.Click += FinalizeNew_Click;
        titleRow.Children.Add(finalizeButton);

        var clearButton = new Button { Content = "초기화" };
        clearButton.Click += ClearButton_Click;
        titleRow.Children.Add(clearButton);

        _summaryText = new TextBlock
        {
            Text = "",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0)
        };
        titleRow.Children.Add(_summaryText);

        Grid.SetRow(titleRow, 0);
        root.Children.Add(titleRow);

        var contentGrid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = new GridLength(260) },
                new RowDefinition { Height = new GridLength(260) },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            },
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }
            },
            ColumnSpacing = 12,
            RowSpacing = 12
        };
        Grid.SetRow(contentGrid, 1);
        root.Children.Add(contentGrid);

        var inputSection = new Grid
        {
            Background = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            }
        };
        var inputHeader = new StackPanel { Padding = new Thickness(12), Background = (Brush)Application.Current.Resources["LayerFillColorAltBrush"] };
        inputHeader.Children.Add(new TextBlock { Text = "붙여넣기", FontWeight = FontWeights.SemiBold });
        inputSection.Children.Add(inputHeader);

        _inputBox = new TextBox
        {
            PlaceholderText = "여기에 붙여넣기 하세요",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 200,
            VerticalAlignment = VerticalAlignment.Top
        };
        var inputScroll = new ScrollViewer { Content = _inputBox, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Grid.SetRow(inputScroll, 1);
        inputSection.Children.Add(inputScroll);
        Grid.SetRow(inputSection, 0);
        Grid.SetColumn(inputSection, 0);
        contentGrid.Children.Add(inputSection);

        var logSection = new Grid
        {
            Background = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            }
        };
        var logHeader = new StackPanel { Padding = new Thickness(12), Background = (Brush)Application.Current.Resources["LayerFillColorAltBrush"] };
        logHeader.Children.Add(new TextBlock { Text = "Debug log", FontWeight = FontWeights.SemiBold });
        logSection.Children.Add(logHeader);

        _logBox = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 200,
            VerticalAlignment = VerticalAlignment.Top,
            FontSize = 12
        };
        var logScroll = new ScrollViewer { Content = _logBox, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Grid.SetRow(logScroll, 1);
        logSection.Children.Add(logScroll);
        Grid.SetRow(logSection, 0);
        Grid.SetColumn(logSection, 2);
        contentGrid.Children.Add(logSection);

        // Houses list
        var housesSection = new Grid
        {
            Background = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
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
            Text = "새 주택 후보",
            FontWeight = FontWeights.SemiBold
        });
        housesSection.Children.Add(housesHeader);

        _housesList = new ListView
        {
            SelectionMode = ListViewSelectionMode.Single,
            ItemsSource = ParsedHouses
        };
        _housesList.SelectionChanged += HousesList_SelectionChanged;

        var housesScroll = new ScrollViewer { Content = _housesList, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Grid.SetRow(housesScroll, 1);
        housesSection.Children.Add(housesScroll);

        Grid.SetRow(housesSection, 1);
        Grid.SetColumn(housesSection, 0);
        contentGrid.Children.Add(housesSection);

        // Items list
        var itemsSection = new Grid
        {
            Background = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            RowDefinitions =
            {
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
            Text = "새 주택 후보 매물",
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

        var itemsScroll = new ScrollViewer { Content = _itemsList, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Grid.SetRow(itemsScroll, 1);
        itemsSection.Children.Add(itemsScroll);

        Grid.SetRow(itemsSection, 1);
        Grid.SetColumn(itemsSection, 1);
        contentGrid.Children.Add(itemsSection);

        // Duplicates list (skipped)
        var dupSection = new Grid
        {
            Background = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
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
            Text = "기존 주택",
            FontWeight = FontWeights.SemiBold
        });
        dupSection.Children.Add(dupHeader);

        _duplicatesList = new ListView
        {
            SelectionMode = ListViewSelectionMode.None,
            ItemsSource = DuplicateHouses,
            ItemTemplate = CreateDuplicateTemplate()
        };

        var dupScroll = new ScrollViewer { Content = _duplicatesList, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Grid.SetRow(dupScroll, 1);
        dupSection.Children.Add(dupScroll);

        Grid.SetRow(dupSection, 0);
        Grid.SetColumn(dupSection, 1);
        contentGrid.Children.Add(dupSection);

        // Existing duplicates (matched by key)
        var relatedSection = new Grid
        {
            Background = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            }
        };

        var relatedHeader = new StackPanel
        {
            Padding = new Thickness(12),
            Background = (Brush)Application.Current.Resources["LayerFillColorAltBrush"]
        };
        relatedHeader.Children.Add(new TextBlock
        {
            Text = "Existing duplicates",
            FontWeight = FontWeights.SemiBold
        });
        relatedSection.Children.Add(relatedHeader);

        _relatedList = new ListView
        {
            SelectionMode = ListViewSelectionMode.None,
            ItemsSource = RelatedDuplicates,
            ItemTemplate = CreateDuplicateTemplate()
        };
        var relatedScroll = new ScrollViewer { Content = _relatedList, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Grid.SetRow(relatedScroll, 1);
        relatedSection.Children.Add(relatedScroll);

        Grid.SetRow(relatedSection, 1);
        Grid.SetColumn(relatedSection, 2);
        contentGrid.Children.Add(relatedSection);

        // Similar houses
        var similarSection = new Grid
        {
            Background = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            }
        };

        var similarHeader = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Padding = new Thickness(12),
            Background = (Brush)Application.Current.Resources["LayerFillColorAltBrush"]
        };
        similarHeader.Children.Add(new TextBlock
        {
            Text = "(2) 새 주택 후보가 기존 주택에 있는지 확인 (중복이면 합치기)",
            FontWeight = FontWeights.SemiBold
        });
        var mergeButton = new Button { Content = "합치기", Margin = new Thickness(12, 0, 0, 0) };
        mergeButton.Click += MergeSelected_Click;
        similarHeader.Children.Add(mergeButton);
        similarSection.Children.Add(similarHeader);

        _similarList = new ListView
        {
            SelectionMode = ListViewSelectionMode.Single,
            ItemsSource = SimilarHouses
        };
        _similarList.SelectionChanged += SimilarList_SelectionChanged;
        var similarScroll = new ScrollViewer { Content = _similarList, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Grid.SetRow(similarScroll, 1);
        similarSection.Children.Add(similarScroll);

        Grid.SetRow(similarSection, 2);
        Grid.SetColumn(similarSection, 0);
        Grid.SetColumnSpan(similarSection, 3);
        contentGrid.Children.Add(similarSection);

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
