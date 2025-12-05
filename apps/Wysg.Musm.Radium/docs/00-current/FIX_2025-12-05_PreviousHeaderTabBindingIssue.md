# FIX: Previous Study Header Tab Binding Issue

**Date**: 2025-12-05  
**Status**: ? Fixed  
**Build**: ? Success  
**Category**: Bug Fix

---

## Problem

When fetching previous studies and creating multiple tabs, the `EditorPreviousHeader` content remained the same as the first tab's content when switching to other tabs (except the first tab). The header only started working correctly after selecting the first tab once.

**Additional Issue (Found During Debug)**:
When `FetchPreviousStudies` module is run, the header was still showing content from a previously loaded study because the cache fields were not being cleared before loading new studies.

**Symptoms**:
- User fetches previous studies กๆ multiple tabs created
- User selects Tab B (second tab) กๆ header still shows Tab A's content (or old study's content)
- User selects Tab A กๆ header shows Tab A's content (correct)
- User selects Tab B again กๆ header now correctly shows Tab B's content
- Other elements (JSON, textboxes, findings, conclusion) worked correctly

---

## Root Causes

### Root Cause 1: Style.Triggers Binding Issue

The issue was caused by using **WPF Style.Triggers with DataTrigger** for the `EditorPreviousHeader` binding:

```xaml
<!-- OLD: Style.Triggers approach (problematic) -->
<editor:EditorControl.Style>
    <Style TargetType="editor:EditorControl">
        <Setter Property="DocumentText" Value=""/>
        <Style.Triggers>
            <DataTrigger Binding="{Binding PreviousReportSplitted}" Value="True">
                <Setter Property="DocumentText" Value="{Binding PreviousHeaderTemp, Mode=TwoWay}"/>
            </DataTrigger>
        </Style.Triggers>
    </Style>
</editor:EditorControl.Style>
```

When using `Style.Triggers` with a `DataTrigger`:
1. The binding is established when the trigger condition is first met
2. When `PreviousReportSplitted` stays `True` during tab switches, the `DataTrigger` doesn't re-evaluate
3. The AvalonEdit-based `EditorControl` may cache its internal document state

### Root Cause 2: Stale Cache Fields on FetchPreviousStudies

When `LoadPreviousStudiesForPatientAsync` was called (by `FetchPreviousStudies` module):
1. `PreviousStudies.Clear()` was called
2. BUT `_selectedPreviousStudy` still referenced the OLD tab (orphaned)
3. Cache fields (`_prevHeaderTempCache`, etc.) retained OLD data from previous patient/study
4. When new tabs were created and selected, the `PreviousHeaderEditorText` getter would sometimes return stale cache data

---

## Solution

### Fix 1: Added `PreviousHeaderEditorText` Computed Property

**File**: `MainViewModel.PreviousStudies.Properties.cs`

Created a new computed property following the same pattern as `PreviousFindingsEditorText`:

```csharp
public string PreviousHeaderEditorText
{
    get
    {
        if (!PreviousReportSplitted)
            return string.Empty;
        
        var tab = SelectedPreviousStudy;
        if (tab == null)
            return _prevHeaderTempCache ?? string.Empty;
        
        return tab.HeaderTemp ?? string.Empty;
    }
    set { /* Two-way binding support */ }
}
```

### Fix 2: Updated XAML Binding

**File**: `PreviousReportEditorPanel.xaml`

Changed from Style.Triggers binding to direct binding:

```xaml
<!-- NEW: Direct binding (fixed) -->
<editor:EditorControl x:Name="EditorPreviousHeader" 
                      DocumentText="{Binding PreviousHeaderEditorText, Mode=TwoWay}"
                      ...>
```

### Fix 3: Clear Cache Before Loading New Studies

**File**: `MainViewModel.PreviousStudiesLoader.cs`

Added cache clearing at the start of `LoadPreviousStudiesForPatientAsync`:

```csharp
private async Task LoadPreviousStudiesForPatientAsync(string patientId)
{
    if (_studyRepo == null) return;
    try
    {
        var rows = await _studyRepo.GetReportsForPatientAsync(patientId);
        
        // CRITICAL FIX: Clear selection and cache BEFORE clearing collection
        // This prevents stale cache data from being shown in the UI
        _selectedPreviousStudy = null;
        _prevHeaderTempCache = string.Empty;
        _prevHeaderAndFindingsCache = string.Empty;
        _prevFinalConclusionCache = string.Empty;
        _prevFindingsOutCache = string.Empty;
        _prevConclusionOutCache = string.Empty;
        _prevStudyRemarkCache = string.Empty;
        _prevPatientRemarkCache = string.Empty;
        _prevChiefComplaintCache = string.Empty;
        _prevPatientHistoryCache = string.Empty;
        _prevStudyTechniquesCache = string.Empty;
        _prevComparisonCache = string.Empty;
        _prevFindingsProofreadCache = string.Empty;
        _prevConclusionProofreadCache = string.Empty;
        
        PreviousStudies.Clear();
        // ... rest of method
    }
}
```

### Fix 4: Added PropertyChanged Notifications

Added `OnPropertyChanged(nameof(PreviousHeaderEditorText))` to:

1. **`MainViewModel.PreviousStudies.Selection.cs`**:
   - `SelectedPreviousStudy` setter (after tab switch)
   - `PreviousReportSplitted` setter (when split mode changes)

2. **`MainViewModel.PreviousStudies.Commands.cs`**:
   - `NotifySplitViewsChanged()` method (after split operations)

3. **`MainViewModel.PreviousStudies.Json.cs`**:
   - `OnSelectedPrevStudyPropertyChanged()` handler (when tab properties change)

---

## Technical Details

### Before Fix (Broken Flow)

```
FetchPreviousStudies module runs
    ก้
LoadPreviousStudiesForPatientAsync called
    ก้
PreviousStudies.Clear() ก็ BUT cache fields NOT cleared!
    ก้
_prevHeaderTempCache still has OLD study's data
    ก้
New tabs created and added
    ก้
SelectedPreviousStudy = PreviousStudies.First()
    ก้
PreviousHeaderEditorText returns OLD cache data ?
```

### After Fix (Correct Flow)

```
FetchPreviousStudies module runs
    ก้
LoadPreviousStudiesForPatientAsync called
    ก้
_selectedPreviousStudy = null
_prevHeaderTempCache = string.Empty (and all other caches)
    ก้
PreviousStudies.Clear()
    ก้
New tabs created and added
    ก้
SelectedPreviousStudy = PreviousStudies.First()
    ก้
UpdatePreviousReportJson() computes HeaderTemp for NEW tab
    ก้
OnPropertyChanged(PreviousHeaderEditorText) notifies UI
    ก้
EditorControl.DocumentText shows CORRECT data ?
```

---

## Files Modified

1. **`apps/Wysg.Musm.Radium/ViewModels/MainViewModel.PreviousStudies.Properties.cs`**
   - Added `PreviousHeaderEditorText` computed property with getter and setter

2. **`apps/Wysg.Musm.Radium/ViewModels/MainViewModel.PreviousStudies.Selection.cs`**
   - Added `OnPropertyChanged(nameof(PreviousHeaderEditorText))` in `SelectedPreviousStudy` setter
   - Added `OnPropertyChanged(nameof(PreviousHeaderEditorText))` in `PreviousReportSplitted` setter

3. **`apps/Wysg.Musm.Radium/ViewModels/MainViewModel.PreviousStudies.Commands.cs`**
   - Added `OnPropertyChanged(nameof(PreviousHeaderEditorText))` in `NotifySplitViewsChanged()`

4. **`apps/Wysg.Musm.Radium/ViewModels/MainViewModel.PreviousStudies.Json.cs`**
   - Added `OnPropertyChanged(nameof(PreviousHeaderEditorText))` in `OnSelectedPrevStudyPropertyChanged()`

5. **`apps/Wysg.Musm.Radium/Controls/PreviousReportEditorPanel.xaml`**
   - Changed `EditorPreviousHeader` from Style.Triggers binding to direct binding with `PreviousHeaderEditorText`

6. **`apps/Wysg.Musm.Radium/ViewModels/MainViewModel.PreviousStudiesLoader.cs`**
   - Added cache clearing at start of `LoadPreviousStudiesForPatientAsync` to prevent stale data

---

## Testing

### Test Case 1: Tab Switch with Multiple Previous Studies

**Steps**:
1. Load a patient with multiple previous studies
2. Verify Tab A's header appears in `EditorPreviousHeader`
3. Click Tab B
4. Verify Tab B's header appears immediately (without needing to click Tab A first)

**Expected**: Header updates correctly on every tab switch

### Test Case 2: FetchPreviousStudies After Loading Different Patient

**Steps**:
1. Load Patient A with previous studies
2. Note the header content for Patient A's first study
3. Load Patient B (different patient) with different previous studies
4. Verify Patient B's header content is shown (NOT Patient A's)

**Expected**: Cache is cleared, showing correct new patient's data

### Test Case 3: Split Mode Toggle

**Steps**:
1. With a previous study selected, toggle SP (split) mode off
2. Verify header editor becomes empty and read-only
3. Toggle SP mode on
4. Verify header editor shows correct content

**Expected**: Split mode toggle correctly shows/hides header content

---

## Lessons Learned

1. **Avoid Style.Triggers for complex bindings**: Use computed properties with direct binding
2. **Always clear cache when loading new data**: Prevents stale data from appearing
3. **Clear selection before clearing collections**: Prevents orphaned references
4. **Use consistent binding patterns**: All editor properties should follow the same pattern

---

**Author**: GitHub Copilot  
**Date**: 2025-12-05  
**Version**: 1.1 (Added cache clearing fix)
