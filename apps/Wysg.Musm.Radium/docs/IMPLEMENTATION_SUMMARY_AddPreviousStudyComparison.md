# SUMMARY: AddPreviousStudy Comparison Update Feature

**Date**: 2025-02-09  
**Status**: ? Implementation blocked by syntax errors in MainViewModel.Commands.cs  
**Priority**: High - Improves workflow automation

---

## What This Feature Does

When the "AddPreviousStudy" automation module loads a new previous study:
1. **Remember** which previous study was selected BEFORE loading the new one
2. **After successfully loading** the new study, automatically fill the `Comparison` field with: `"{Modality} {Date}"` from the previously selected study
3. **Skip the update** if current study is XR modality AND "Do not update header in XR" setting is enabled

---

## Required Code Changes

### File: `MainViewModel.Commands.cs`

#### 1. Capture Previously Selected Study (Line ~1400)

At the **very start** of `RunAddPreviousStudyModuleAsync()` method, add:

```csharp
// REMEMBER: Capture the currently selected previous study BEFORE loading new one
// This will be used to update Comparison field later
var previouslySelectedStudy = SelectedPreviousStudy;
Debug.WriteLine($"[AddPreviousStudyModule] Previously selected study: {previouslySelectedStudy?.Title ?? "(none)"}");
```

#### 2. Update Comparison After Load (Two locations)

Add this call in TWO places within `RunAddPreviousStudyModuleAsync()`:

**Location A** - After duplicate detection (around line ~1470):
```csharp
if (duplicate != null)
{
    Debug.WriteLine($"[AddPreviousStudyModule] Duplicate confirmed: {duplicate.Title}");
    SelectedPreviousStudy = duplicate;
    PreviousReportSplitted = true;
    
    // NEW: Update comparison field if there was a previously selected study
    await UpdateComparisonFromPreviousStudyAsync(previouslySelectedStudy);
    
    stopwatch.Stop();
    // ... rest of existing code ...
}
```

**Location B** - After successful new study load (around line ~1560):
```csharp
if (newTab != null)
{
    SelectedPreviousStudy = newTab;
    PreviousReportSplitted = true;
    
    // NEW: Update comparison field if there was a previously selected study
    await UpdateComparisonFromPreviousStudyAsync(previouslySelectedStudy);
    
    stopwatch.Stop();
    // ... rest of existing code ...
}
```

#### 3. New Method: `UpdateComparisonFromPreviousStudyAsync`

Add this method at the END of the MainViewModel partial class (before the closing braces):

```csharp
/// <summary>
/// Updates the Comparison field from a previously selected previous study.
/// Skips update if current study is XR and "Do not update header in XR" setting is checked.
/// </summary>
private async Task UpdateComparisonFromPreviousStudyAsync(PreviousStudyTab? previousStudy)
{
    await Task.CompletedTask; // Make method async-compatible
    
    if (previousStudy == null)
    {
        Debug.WriteLine("[UpdateComparisonFromPreviousStudy] No previously selected study - skipping comparison update");
        return;
    }
    
    try
    {
        Debug.WriteLine($"[UpdateComparisonFromPreviousStudy] Previously selected study: {previousStudy.Title}");
        
        // Check if current study is XR modality
        bool isXRModality = false;
        if (!string.IsNullOrWhiteSpace(StudyName))
        {
            // Extract modality from studyname (typically first word or first few characters)
            var studyNameUpper = StudyName.ToUpperInvariant();
            isXRModality = studyNameUpper.StartsWith("XR") || studyNameUpper.Contains(" XR ");
            Debug.WriteLine($"[UpdateComparisonFromPreviousStudy] Current study modality check: isXR={isXRModality}, StudyName='{StudyName}'");
        }
        
        // Check "Do not update header in XR" setting
        bool doNotUpdateHeaderInXR = false;
        if (_local != null)
        {
            var settingValue = _local.DoNotUpdateHeaderInXR;
            doNotUpdateHeaderInXR = string.Equals(settingValue, "true", StringComparison.OrdinalIgnoreCase);
            Debug.WriteLine($"[UpdateComparisonFromPreviousStudy] DoNotUpdateHeaderInXR setting: '{settingValue}' -> {doNotUpdateHeaderInXR}");
        }
        
        // Skip update if XR and setting is enabled
        if (isXRModality && doNotUpdateHeaderInXR)
        {
            Debug.WriteLine("[UpdateComparisonFromPreviousStudy] Skipping comparison update - XR modality and DoNotUpdateHeaderInXR is enabled");
            SetStatus("Comparison not updated (XR modality with 'Do not update header in XR' enabled)");
            return;
        }
        
        // Build comparison string from previous study
        // Format: "{Modality} {Date}"
        var comparisonText = $"{previousStudy.Modality} {previousStudy.StudyDateTime:yyyy-MM-dd}";
        Debug.WriteLine($"[UpdateComparisonFromPreviousStudy] Built comparison text: '{comparisonText}'");
        
        // Update Comparison property
        Comparison = comparisonText;
        Debug.WriteLine($"[UpdateComparisonFromPreviousStudy] Updated Comparison property: '{Comparison}'");
        SetStatus($"Comparison updated: {comparisonText}");
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"[UpdateComparisonFromPreviousStudy] ERROR: {ex.Message}");
        Debug.WriteLine($"[UpdateComparisonFromPreviousStudy] StackTrace: {ex.StackTrace}");
        // Don't throw - allow AddPreviousStudy to continue even if comparison update fails
    }
}
```

---

## Testing Steps

1. **Normal Case** (Non-XR Study):
   - Load a study (e.g., "CT Chest")
   - Load previous study "CT Chest 2025-01-15"
   - Load another previous study "MRI Brain 2025-02-01"
   - ? Expected: Comparison field shows "CT 2025-01-15"

2. **XR with Setting ON**:
   - Load "XR Chest PA" study
   - Enable "Do not update header in XR" in Settings
   - Load previous study
   - ? Expected: Comparison field NOT updated, status shows skip message

3. **XR with Setting OFF**:
   - Load "XR Chest AP" study
   - Disable "Do not update header in XR" setting
   - Load previous study
   - ? Expected: Comparison field IS updated

4. **No Previous Study**:
   - Load first previous study when none selected
   - ? Expected: No crash, status shows "skipping comparison update"

---

## Current Status

? **Blocked**: Multiple attempts to edit `MainViewModel.Commands.cs` resulted in syntax errors (CS1513: } needed)

**Root Cause**: The file is very large (~1800 lines) and has complex nested structure. Incremental edits keep breaking the structure.

**Recommended Next Steps**:
1. Fix syntax error in `MainViewModel.Commands.cs` first (missing closing brace somewhere)
2. Ensure file compiles successfully
3. Then apply the three changes listed above
4. Build and test

---

## Related Files

- `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Commands.cs` - Main implementation
- `apps\Wysg.Musm.Radium\Services\IRadiumLocalSettings.cs` - DoNotUpdateHeaderInXR setting
- `apps\Wysg.Musm.Radium\docs\ENHANCEMENT_2025-02-09_AddPreviousStudyComparisonUpdate.md` - Full specification

---

## Benefits

- ? Saves user time - no manual comparison entry
- ? Consistent format across all reports
- ? Respects existing XR header settings
- ? Non-breaking - gracefully handles errors
- ? Well-logged for debugging

---

## Alternative Implementation Approach

If syntax errors persist, consider:
1. Create a NEW partial class file: `MainViewModel.ComparisonHelpers.cs`
2. Move the `UpdateComparisonFromPreviousStudyAsync` method there
3. Modify `MainViewModel.Commands.cs` to call it
4. This reduces risk of breaking existing file structure
