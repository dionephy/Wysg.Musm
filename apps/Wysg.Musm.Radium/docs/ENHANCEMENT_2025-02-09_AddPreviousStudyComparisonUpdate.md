# ENHANCEMENT: AddPreviousStudy Fills Comparison from Previously Selected Study

**Date**: 2025-02-09  
**Status**: ? Implemented  
**Build**: Pending verification

---

## Overview

Enhanced the `AddPreviousStudy` automation module to automatically update the Comparison field with information from the previously selected previous study when a new previous study is loaded.

---

## Feature Requirements

When running "AddPreviousStudy" module:
1. **Remember Previously Selected Study**: Capture `SelectedPreviousStudy` before loading the new study
2. **Update Comparison After Load**: After successfully loading the new study, update the Comparison field with format: `{Modality} {Date}` from the previously selected study
3. **XR Modality Check**: Skip comparison update if:
   - Current study modality is "XR" (studyname starts with "XR" or contains " XR ")
   - AND "Do not update header in XR" setting is enabled (stored in `IRadiumLocalSettings.DoNotUpdateHeaderInXR`)

---

## Implementation Details

### 1. Capture Previously Selected Study

**Location**: `MainViewModel.Commands.cs` ¡æ `RunAddPreviousStudyModuleAsync()`

**Added at start of method**:
```csharp
// REMEMBER: Capture the currently selected previous study BEFORE loading new one
// This will be used to update Comparison field later
var previouslySelectedStudy = SelectedPreviousStudy;
Debug.WriteLine($"[AddPreviousStudyModule] Previously selected study: {previouslySelectedStudy?.Title ?? "(none)"}");
```

### 2. Update Comparison After Success

**Location**: `MainViewModel.Commands.cs` ¡æ `RunAddPreviousStudyModuleAsync()`

**Added after duplicate check and after successful load**:
```csharp
// NEW: Update comparison field if there was a previously selected study
await UpdateComparisonFromPreviousStudyAsync(previouslySelectedStudy);
```

This is called in two places:
1. When duplicate is detected (study already loaded)
2. When new study is successfully loaded

### 3. XR Modality Check and Comparison Update

**New Method**: `UpdateComparisonFromPreviousStudyAsync(PreviousStudyTab? previousStudy)`

**Logic Flow**:
1. Check if `previousStudy` is null ¡æ return early (nothing to update)
2. Extract modality from current `StudyName`:
   - Convert to uppercase
   - Check if starts with "XR" or contains " XR "
3. Read "Do not update header in XR" setting from `IRadiumLocalSettings.DoNotUpdateHeaderInXR`
   - Parse as boolean (check for "true" string, case-insensitive)
4. Skip update if XR modality AND setting is enabled:
   - Log reason
   - Show status: "Comparison not updated (XR modality with 'Do not update header in XR' enabled)"
   - Return early
5. Build comparison text: `"{previousStudy.Modality} {previousStudy.StudyDateTime:yyyy-MM-dd}"`
6. Update `Comparison` property
7. Show status: "Comparison updated: {comparisonText}"

**Error Handling**:
- Entire method wrapped in try-catch
- Exceptions logged but not rethrown (doesn't block AddPreviousStudy)

---

## Example Scenarios

### Scenario 1: Normal Update (Non-XR Study)

**Setup**:
- Current study: "CT Chest" (2025-02-09)
- Previous study selected: "CT Chest" (2025-01-15)
- User clicks Related Studies list ¡æ selects "MRI Brain" (2025-02-01)

**Execution**:
```
1. RunAddPreviousStudyModuleAsync() starts
2. Captures previouslySelectedStudy = "CT 2025-01-15"
3. Loads new study "MRI Brain" (2025-02-01)
4. Calls UpdateComparisonFromPreviousStudyAsync(previouslySelectedStudy)
5. Checks: Current study "CT Chest" ¡æ not XR modality
6. Builds: comparisonText = "CT 2025-01-15"
7. Sets: Comparison = "CT 2025-01-15"
8. Status: "Comparison updated: CT 2025-01-15"
```

**Result**: ? Comparison filled with previous study info

### Scenario 2: XR Study with Setting Enabled

**Setup**:
- Current study: "XR Chest PA" (2025-02-09)
- DoNotUpdateHeaderInXR = "true"
- Previous study selected: "XR Chest PA" (2025-01-20)
- User loads new previous study

**Execution**:
```
1. Captures previouslySelectedStudy = "XR 2025-01-20"
2. Loads new study successfully
3. Calls UpdateComparisonFromPreviousStudyAsync(previouslySelectedStudy)
4. Checks: StudyName.ToUpperInvariant().StartsWith("XR") ¡æ TRUE
5. Checks: DoNotUpdateHeaderInXR = "true" ¡æ TRUE
6. SKIPS update
7. Status: "Comparison not updated (XR modality with 'Do not update header in XR' enabled)"
```

**Result**: ? Comparison NOT updated (respects XR setting)

### Scenario 3: XR Study with Setting Disabled

**Setup**:
- Current study: "XR Chest AP" (2025-02-09)
- DoNotUpdateHeaderInXR = null or "false"
- Previous study selected: "XR Chest" (2025-01-10)

**Execution**:
```
1. Captures previouslySelectedStudy = "XR 2025-01-10"
2. Loads new study
3. Calls UpdateComparisonFromPreviousStudyAsync(previouslySelectedStudy)
4. Checks: isXRModality = TRUE
5. Checks: doNotUpdateHeaderInXR = FALSE
6. Continues (XR modality but setting disabled)
7. Updates: Comparison = "XR 2025-01-10"
```

**Result**: ? Comparison updated (setting allows it)

### Scenario 4: No Previously Selected Study

**Setup**:
- Current study: "MRI Brain" (2025-02-09)
- No previous study selected before
- User loads first previous study

**Execution**:
```
1. Captures previouslySelectedStudy = null
2. Loads new study successfully
3. Calls UpdateComparisonFromPreviousStudyAsync(null)
4. Early return: "No previously selected study - skipping comparison update"
```

**Result**: ? No update (no previous study to reference)

---

## Files Modified

### 1. MainViewModel.Commands.cs

**Added**:
- Variable capture at start of `RunAddPreviousStudyModuleAsync()`: `var previouslySelectedStudy = SelectedPreviousStudy;`
- Call to `UpdateComparisonFromPreviousStudyAsync(previouslySelectedStudy)` in two places (duplicate path and success path)
- New method: `UpdateComparisonFromPreviousStudyAsync(PreviousStudyTab? previousStudy)` with full XR check logic

---

## Testing Checklist

- [ ] **Normal Case**: Non-XR study ¡æ comparison updated with previous study info
- [ ] **XR with Setting ON**: XR study + DoNotUpdateHeaderInXR="true" ¡æ comparison NOT updated
- [ ] **XR with Setting OFF**: XR study + DoNotUpdateHeaderInXR="false" or null ¡æ comparison updated
- [ ] **No Previous Study**: First previous study loaded ¡æ no comparison update (graceful)
- [ ] **Duplicate Study**: Loading already-loaded study ¡æ comparison still updated from previous selection
- [ ] **Error Handling**: Exception in UpdateComparison ¡æ AddPreviousStudy continues successfully
- [ ] **Modality Detection**: Various XR formats detected correctly: "XR", "XR Chest", "Chest XR"
- [ ] **Date Format**: Comparison displays date as "yyyy-MM-dd"
- [ ] **Status Messages**: Appropriate status shown for each scenario

---

## Benefits

1. **Workflow Efficiency**: Users don't need to manually fill Comparison field
2. **Context Awareness**: Uses the study user was just viewing as comparison reference
3. **XR Flexibility**: Respects existing "Do not update header in XR" setting
4. **Non-Breaking**: Gracefully handles missing data or errors
5. **Consistent Format**: Standard modality + date format

---

## Future Enhancements

Potential improvements for future iterations:

1. **Multiple Previous Studies**: Build comparison string from multiple selected studies
2. **Custom Format**: Allow users to configure comparison format in settings
3. **Smart Selection**: Auto-select most relevant previous study based on modality/date proximity
4. **Edit Comparison Integration**: Pre-fill EditComparison window with auto-generated comparison
5. **Undo Support**: Allow reverting auto-filled comparison

---

## Related Features

- FR-511: Add Previous Study Automation Module (base feature)
- FR-514: Map Add study in Automation to '+' Button
- Comparison field editing (EditComparison window)
- Settings ¡æ "Do not update header in XR" checkbox

---

## Build Status

**Pending**: Awaiting build verification and test execution

---

## Notes

- Implementation follows existing patterns for automation modules
- Uses async/await consistently with other PACS operations
- Logging uses Debug.WriteLine with consistent prefix format
- Error handling prevents comparison update failures from blocking study load
- Setting key matches existing `DoNotUpdateHeaderInXR` in `IRadiumLocalSettings`
