# SUMMARY: AddPreviousStudy Comparison Update Feature

**Date**: 2025-02-09  
**Status**: ✅ Implemented & Fixed (2025-02-10)  
**Update**: 2025-02-10 - Changed from XR checkbox to comma-separated modalities list  
**Priority**: High - Improves workflow automation

---

## What This Feature Does

When the "AddPreviousStudy" automation module loads a new previous study:
1. **Remember** which previous study was selected BEFORE loading the new one
2. **After successfully loading** the new study, automatically fill the `Comparison` field with: `"{Modality} {Date}"` from:
   - The **previously selected study** (if one existed), OR
   - The **newly loaded study** (if no previous study was selected - ensures first load fills comparison) ✅ FIXED 2025-02-10
3. **Skip the update** if current study modality is in the "Following modalities don't send header" exclusion list (comma-separated) ✅ UPDATED 2025-02-10

---

## Key Updates (2025-02-10)

### Modality Exclusion Change

**Old Behavior:**
- Checkbox: "Do not update header in XR"
- Boolean setting (on/off)
- Only XR modality was excluded

**New Behavior:**
- Textbox: "Following modalities don't send header (separated by comma):"
- String setting (e.g., "XR,CR,DX")
- Multiple modalities can be excluded
- Case-insensitive matching
- Supports comma or semicolon separators

### Usage Examples

**Exclude XR only (original behavior):**
```
XR
```

**Exclude multiple modalities:**
```
XR,CR,DX
```

**No exclusions (allow all updates):**
```
(leave textbox empty)
```

---

## Key Fix (2025-02-10 - Original Implementation)

### Problem
The comparison string sometimes remained "N/A" even when a previous study was successfully loaded. This occurred when loading the **first** previous study because `previouslySelectedStudy` was null.

### Solution
Changed from:
```csharp
await UpdateComparisonFromPreviousStudyAsync(previouslySelectedStudy);
```

To:
```csharp
await UpdateComparisonFromPreviousStudyAsync(previouslySelectedStudy ?? newTab);
```

This ensures the comparison field is populated with:
- The previously selected study (if available) - **typical workflow**
- OR the newly loaded study (if no previous selection) - **first load scenario** ✅

### Log Evidence
**Before fix**:
```
[AddPreviousStudyModule] Previously selected study: (none)
...
[UpdateComparisonFromPreviousStudy] No previously selected study - skipping comparison update
```

**After fix**: Comparison will be filled with newly loaded study info

---

## Required Code Changes

### File: `MainViewModel.Commands.AddPreviousStudy.cs`

#### 1. Capture Previously Selected Study (Line ~33)

At the **very start** of `RunAddPreviousStudyModuleAsync()` method:

```csharp
// REMEMBER: Capture the currently selected previous study BEFORE loading new one
// This will be used to update Comparison field later
var previouslySelectedStudy = SelectedPreviousStudy;
Debug.WriteLine($"[AddPreviousStudyModule] Previously selected study: {previouslySelectedStudy?.Title ?? "(none)"}");
```

#### 2. Update Comparison After Load (Two locations) - UPDATED 2025-02-10

**Location A** - After duplicate detection (around line ~160):
```csharp
if (duplicate != null)
{
    Debug.WriteLine($"[AddPreviousStudyModule] Duplicate confirmed: {duplicate.Title}");
    SelectedPreviousStudy = duplicate;
    PreviousReportSplitted = true;
    
    // UPDATED: Use previouslySelectedStudy if available, otherwise use duplicate (newly selected)
    await UpdateComparisonFromPreviousStudyAsync(previouslySelectedStudy ?? duplicate);
    
    stopwatch.Stop();
    // ... rest of existing code ...
}
```

**Location B** - After successful new study load (around line ~240):
```csharp
if (newTab != null)
{
    SelectedPreviousStudy = newTab;
    PreviousReportSplitted = true;
    
    // UPDATED: Use previouslySelectedStudy if available, otherwise use newTab (newly loaded)
    await UpdateComparisonFromPreviousStudyAsync(previouslySelectedStudy ?? newTab);
    
    stopwatch.Stop();
    // ... rest of existing code ...
}
```

#### 3. Method: `UpdateComparisonFromPreviousStudyAsync` - UPDATED 2025-02-10

**Implementation now checks comma-separated modalities list:**

```csharp
/// <summary>
/// Updates the Comparison field from a previous study.
/// Skips update if current study modality is in the ModalitiesNoHeaderUpdate list.
/// </summary>
private async Task UpdateComparisonFromPreviousStudyAsync(PreviousStudyTab? previousStudy)
{
    // Extract current modality using LOINC mapping
    string? currentModality = await ExtractModalityAsync(StudyName);
    
    // Check ModalitiesNoHeaderUpdate setting (comma-separated list)
    var modalitiesNoHeaderUpdate = _localSettings.ModalitiesNoHeaderUpdate ?? string.Empty;
    
    if (!string.IsNullOrWhiteSpace(modalitiesNoHeaderUpdate))
    {
        // Parse comma-separated list
        var excludedModalities = modalitiesNoHeaderUpdate
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(m => m.Trim().ToUpperInvariant())
            .Where(m => !string.IsNullOrEmpty(m))
            .ToList();
        
        // Skip update if current modality is in exclusion list
        if (excludedModalities.Contains(currentModality.ToUpperInvariant()))
        {
            Debug.WriteLine($"Skipping comparison update - modality '{currentModality}' is excluded");
            SetStatus($"Comparison not updated (modality '{currentModality}' excluded by settings)");
            return;
        }
    }
    
    // Build and update comparison text
    var comparisonText = $"{previousStudy.Modality} {previousStudy.StudyDateTime:yyyy-MM-dd}";
    Comparison = comparisonText;
    SetStatus($"Comparison updated: {comparisonText}");
}
```

---

## Testing Steps

1. **First Load** (FIXED ✅):
   - Load a study (e.g., "CT Chest")
   - No previous study selected yet
   - Load first previous study "CT Chest 2025-01-15"
   - ✅ Expected: Comparison field shows "CT 2025-01-15"

2. **Second Load** (Normal Case):
   - Load a study (e.g., "CT Chest")
   - Load previous study "CT Chest 2025-01-15"
   - Load another previous study "MRI Brain 2025-02-01"
   - ✅ Expected: Comparison field shows "CT 2025-01-15" (previously selected)

3. **XR with Exclusion (UPDATED ✅)**:
   - Load "XR Chest PA" study
   - Enter "XR" in "Following modalities don't send header" textbox
   - Click "Save Automation"
   - Load previous study
   - ✅ Expected: Comparison field NOT updated, status shows skip message

4. **Multiple Modalities Excluded (NEW ✅)**:
   - Load "CR Chest" study
   - Enter "XR,CR,DX" in textbox
   - Click "Save Automation"
   - Load previous study
   - ✅ Expected: Comparison field NOT updated for CR modality

5. **CT with No Exclusion (UPDATED ✅)**:
   - Load "CT Chest" study
   - Enter "XR,CR" in textbox (doesn't include CT)
   - Click "Save Automation"
   - Load previous study
   - ✅ Expected: Comparison field IS updated

---

## Current Status

✅ **Implemented and Updated**: The null-coalescing operator ensures comparison is filled on first load, and the comma-separated modalities list provides flexible exclusion control

---

## Files Modified

- `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Commands.AddPreviousStudy.cs` - Main implementation with fix and updated exclusion logic
- `apps\Wysg.Musm.Radium\Services\IRadiumLocalSettings.cs` - Changed from DoNotUpdateHeaderInXR (bool) to ModalitiesNoHeaderUpdate (string)
- `apps\Wysg.Musm.Radium\Services\RadiumLocalSettings.cs` - Updated storage key
- `apps\Wysg.Musm.Radium\ViewModels\SettingsViewModel.cs` - Updated property type
- `apps\Wysg.Musm.Radium\ViewModels\SettingsViewModel.PacsProfiles.cs` - Updated property and save logic
- `apps\Wysg.Musm.Radium\Views\SettingsTabs\AutomationSettingsTab.xaml` - Changed checkbox to textbox
- `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Editor.cs` - Updated skip logic for multiple modalities
- `apps\Wysg.Musm.Radium\docs\ENHANCEMENT_2025-02-09_AddPreviousStudyComparisonUpdate.md` - Full specification

---

## Benefits

- ✅ Saves user time - no manual comparison entry
- ✅ Works on first load - no need to load two studies to fill comparison
- ✅ Consistent format across all reports
- ✅ **Flexible exclusions** - supports multiple modalities, not just XR ✅ NEW
- ✅ **Easy configuration** - simple comma-separated textbox ✅ NEW
- ✅ **Case insensitive** - "XR", "xr", "Xr" all work ✅ NEW
- ✅ Non-breaking - gracefully handles errors
- ✅ Well-logged for debugging

---

## Related Features

- FR-511: Add Previous Study Automation Module
- FR-514: Map Add study in Automation to '+' Button
- Comparison field editing (EditComparison window)
- Settings → "Following modalities don't send header" textbox
- ENHANCEMENT_2025-02-10_ModalitiesNoHeaderUpdate.md - Full details on UI change
