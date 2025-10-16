# Change Log: Current Combination Quick Delete and All Combinations Library
**Date**: 2025-01-18
**User Request**: Add double-click delete and all combinations library features

## Problem Description

Users requested two workflow enhancements for the "Manage Studyname Techniques" window:

### User Request 1: Quick Delete
When building a technique combination, users need to remove items from the "Current Combination" list. Currently, there's no direct way to remove individual items - users must clear the entire list and start over.

**Desired Behavior**: Double-click an item in "Current Combination" to remove it immediately.

### User Request 2: Combination Library
Users want to reuse existing technique combinations as starting points for new combinations. Currently, there's no way to view or load existing combinations when building a new one.

**Desired Behavior**: Display all existing combinations in a list, and double-click one to load its techniques into "Current Combination" for modification.

## Solution Overview

### Feature 1: Double-Click to Delete
- Added `MouseDoubleClick` event handler to "Current Combination" ListBox
- Implemented `RemoveFromCurrentCombination(item)` method in ViewModel
- Removes item from ObservableCollection and notifies SaveNewCombinationCommand
- No confirmation dialog (standard delete behavior)
- Updates GroupBox header with hint text "(double-click to remove)"

### Feature 2: All Combinations Library
- Added new "All Combinations" ListBox below "Current Combination"
- Displays all technique combinations in database (not filtered by studyname)
- Implemented `GetAllCombinationsAsync()` repository method querying `v_technique_combination_display` view
- Added `LoadCombinationIntoCurrentAsync(combinationId)` method to load techniques
- Prevents duplicates when loading (checks prefix_id, tech_id, suffix_id)
- Appends loaded techniques to end of Current Combination with sequential ordering
- Updates GroupBox header with hint text "(double-click to load)"

## Technical Implementation

### 1. ViewModel Changes (StudynameTechniqueViewModel.cs)

#### New Collection
```csharp
public ObservableCollection<AllCombinationRow> AllCombinations { get; } = new();
```

#### New Model Class
```csharp
public sealed class AllCombinationRow
{
    public long CombinationId { get; set; }
    public string Display { get; set; } = string.Empty;
    public override string ToString() => Display;
}
```

#### Remove Method
```csharp
public void RemoveFromCurrentCombination(CombinationItem item)
{
    if (item == null) return;
    CurrentCombinationItems.Remove(item);
    _saveNewCombinationCommand?.RaiseCanExecuteChanged();
}
```

#### Load Method
```csharp
public async Task LoadCombinationIntoCurrentAsync(long combinationId)
{
    var items = await _repo.GetCombinationItemsAsync(combinationId);
    
    foreach (var (prefix, tech, suffix, seq) in items)
    {
        // Match against lookup lists to get IDs
        long? prefixId = Prefixes.FirstOrDefault(p => p.Text == prefix)?.Id;
        long? techId = Techs.FirstOrDefault(t => t.Text == tech)?.Id;
        long? suffixId = Suffixes.FirstOrDefault(s => s.Text == suffix)?.Id;
        
        if (techId == null) continue;
        
        // Check for duplicates
        bool isDuplicate = CurrentCombinationItems.Any(e => 
            e.PrefixId == prefixId && 
            e.TechId == techId.Value && 
            e.SuffixId == suffixId);
        
        if (!isDuplicate)
        {
            // Add to current combination with next sequence number
            var nextSeq = CurrentCombinationItems.Count + 1;
            CurrentCombinationItems.Add(new CombinationItem { ... });
        }
    }
    
    _saveNewCombinationCommand?.RaiseCanExecuteChanged();
}
```

#### Updated ReloadAsync
```csharp
public async Task ReloadAsync()
{
    // ... existing studyname combinations reload ...
    
    // NEW: Reload ALL combinations
    AllCombinations.Clear();
    var allRows = await _repo.GetAllCombinationsAsync();
    foreach (var c in allRows)
        AllCombinations.Add(new AllCombinationRow { 
            CombinationId = c.CombinationId, 
            Display = c.Display 
        });
    
    // ... existing lookups reload ...
}
```

### 2. Repository Changes

#### Interface (TechniqueRepository.cs)
```csharp
Task<IReadOnlyList<AllCombinationRow>> GetAllCombinationsAsync();

public sealed record AllCombinationRow(long CombinationId, string Display);
```

#### Implementation (TechniqueRepository.Pg.Extensions.cs)
```csharp
public async Task<IReadOnlyList<AllCombinationRow>> GetAllCombinationsAsync()
{
    var list = new List<AllCombinationRow>();
    await using var cn = Open(); 
    await PgConnectionHelper.OpenWithLocalSslFallbackAsync(cn);
    
    const string sql = @"SELECT id, COALESCE(combination_name, combination_display, '') AS display
FROM med.v_technique_combination_display
ORDER BY id DESC";
    
    await using var cmd = new NpgsqlCommand(sql, cn);
    await using var rd = await cmd.ExecuteReaderAsync();
    while (await rd.ReadAsync())
    {
        long id = rd.GetInt64(0);
        string display = rd.IsDBNull(1) ? string.Empty : rd.GetString(1);
        list.Add(new AllCombinationRow(id, display));
    }
    return list;
}
```

**Query Details**:
- Selects from `med.v_technique_combination_display` view
- Prefers combination_name over auto-generated combination_display
- Orders by ID descending (newest combinations first)
- No filtering - returns ALL combinations in database

### 3. UI Changes (StudynameTechniqueWindow.xaml.cs)

#### Layout Change
Changed left panel from 4 rows to 5 rows:
```csharp
leftPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
leftPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Builder UI
leftPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Current Combination
leftPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // All Combinations (NEW)
leftPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Save button
```

#### Current Combination ListBox
```csharp
var lstCurrentCombo = new ListBox { ... };
lstCurrentCombo.MouseDoubleClick += OnCurrentCombinationDoubleClick;
// GroupBox header: "Current Combination (double-click to remove)"
```

#### All Combinations ListBox (NEW)
```csharp
var allComboGroup = new GroupBox 
{ 
    Header = "All Combinations (double-click to load)", 
    ...
};
var lstAllCombos = new ListBox 
{ 
    Margin = new Thickness(4),
    Background = Brushes.Black,
    BorderBrush = Brushes.DimGray,
    DisplayMemberPath = "Display"
};
lstAllCombos.SetBinding(ListBox.ItemsSourceProperty, new Binding("AllCombinations"));
lstAllCombos.MouseDoubleClick += OnAllCombinationsDoubleClick;
```

#### Event Handlers
```csharp
private void OnCurrentCombinationDoubleClick(object sender, MouseButtonEventArgs e)
{
    var vm = DataContext as StudynameTechniqueViewModel;
    if (vm == null) return;

    var listBox = sender as ListBox;
    if (listBox?.SelectedItem is StudynameTechniqueViewModel.CombinationItem item)
    {
        vm.RemoveFromCurrentCombination(item);
    }
}

private async void OnAllCombinationsDoubleClick(object sender, MouseButtonEventArgs e)
{
    var vm = DataContext as StudynameTechniqueViewModel;
    if (vm == null) return;

    var listBox = sender as ListBox;
    if (listBox?.SelectedItem is StudynameTechniqueViewModel.AllCombinationRow row)
    {
        await vm.LoadCombinationIntoCurrentAsync(row.CombinationId);
    }
}
```

## User Workflow Examples

### Example 1: Quick Delete During Building
1. User adds: "axial T1", "axial T2", "coronal T1"
2. User realizes "axial T2" is wrong
3. User double-clicks "axial T2" in Current Combination
4. Item removes immediately
5. User continues building: adds "sagittal T1"
6. Final combination: "axial T1", "coronal T1", "sagittal T1"

### Example 2: Reusing Existing Combination
1. User opens window to create brain MRI combination
2. User sees "Spine MRI combo" in All Combinations list
3. User double-clicks "Spine MRI combo"
4. Techniques load: "sagittal T1", "sagittal T2", "axial T1"
5. User removes "sagittal T2" (double-click)
6. User adds "coronal FLAIR"
7. User clicks "Save as New Combination"
8. New combination saved: "sagittal T1", "axial T1", "coronal FLAIR"

### Example 3: Merging Multiple Combinations
1. User double-clicks "Basic sequences" ¡æ loads "axial T1", "axial T2"
2. User double-clicks "Contrast sequences" ¡æ loads "CE-T1" (no duplicates)
3. Current Combination now has 3 techniques
4. User saves as "Complete protocol"

## Benefits

### User Experience
- ? **Faster editing**: No need to rebuild entire combination to fix one mistake
- ? **Reusability**: Can start from existing combinations instead of building from scratch
- ? **Discoverability**: All existing combinations visible and accessible
- ? **Visual feedback**: Hint text in headers guides users on interaction
- ? **No confirmation dialogs**: Quick, fluid interaction for power users

### Technical
- ? **No breaking changes**: Existing functionality preserved
- ? **Efficient queries**: Single query to load all combinations
- ? **Duplicate prevention**: Built-in logic prevents duplicate techniques
- ? **Consistent UX**: Follows ListBox double-click conventions
- ? **Async operations**: UI remains responsive during load

## Testing Checklist

### Double-Click Delete
- [ ] V445: Double-click removes item immediately
- [ ] V446: Button disables when last item removed
- [ ] V447: Rapid double-clicks work correctly

### All Combinations Display
- [ ] V448: ListBox populates with existing combinations
- [ ] V449: Displays formatted text (grouped by prefix/suffix)
- [ ] V450: Ordered by ID descending (newest first)
- [ ] V451: Includes combinations from all studynames

### Double-Click Load
- [ ] V452: Load into empty list works
- [ ] V453: Loaded techniques show correct text
- [ ] V454: Sequence starts at 1
- [ ] V455: Append to existing list works
- [ ] V456: Same combination twice ¡æ no duplicates
- [ ] V457: Manual + load with duplicate ¡æ duplicate skipped

### Edge Cases
- [ ] V458: Null prefix/suffix handled correctly
- [ ] V459: Save button enables after load
- [ ] V460: Modify loaded combination and save creates new
- [ ] V461: Window resize ¡æ both ListBoxes resize
- [ ] V462: Large combination (10+ items) loads quickly

## Documentation Updates

All three documentation files have been updated with cumulative entries:

1. ? **Spec.md**: Added FR-1060 through FR-1074 (15 new requirements)
2. ? **Plan.md**: Added comprehensive change log with approach, test plan, and risks
3. ? **Tasks.md**: Added T1190-T1218 (29 tasks) and V440-V465 (26 verification steps)

## Build Status
? Build passes with no compilation errors

## Related Features
- Builds on FR-1025 (Save as New Combination)
- Complements FR-1024 (Duplicate prevention)
- Enhances FR-1023 (Add to Combination)
- Works with FR-1050-1054 (Button enablement)

---

**Summary**: Users can now double-click to quickly remove items from Current Combination and can reuse/modify existing combinations by loading them from the All Combinations library. Both features follow standard UI conventions and integrate seamlessly with existing duplicate prevention and save logic.
