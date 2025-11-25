# Fix: Disable Auto-Save on Previous Study Tab Switch (2025-02-08)

**Status**: ? Implemented  
**Date**: 2025-02-08  
**Category**: Feature Removal / User Request

---

## Problem Statement

When switching between previous study tabs, the application was automatically saving the outgoing tab's JSON changes to its in-memory state. This was implemented as a convenience feature to prevent data loss (see `FIX_2025-11-02_PreviousStudyJsonSaveOnTabSwitch.md`).

However, the user requested to disable this auto-save behavior. Users now prefer to explicitly save changes using the "Save Previous Study to DB" button rather than having changes automatically persisted when switching tabs.

---

## Solution

Disabled the auto-save logic in the `SelectedPreviousStudy` setter by commenting out the JSON capture and apply code. The previous implementation that prevented data loss is preserved in comments for future reference or re-enablement if needed.

### Code Changes

**File**: `MainViewModel.PreviousStudies.Selection.cs`

**Before:**
```csharp
public PreviousStudyTab? SelectedPreviousStudy
{ 
    get => _selectedPreviousStudy; 
    set 
    { 
        var old = _selectedPreviousStudy;
        
        // CRITICAL FIX: Capture OLD tab's JSON BEFORE the binding system updates
        string? oldTabJson = null;
        if (old != null && old != value)
        {
            oldTabJson = _previousReportJson;
            Debug.WriteLine($"[Prev] Captured JSON for outgoing tab: {old.Title}");
        }
        
        if (SetProperty(ref _selectedPreviousStudy, value)) 
        { 
            // ... update UI bindings ...
            
            // CRITICAL FIX: Apply the captured JSON to the OLD tab
            if (old != null && !string.IsNullOrWhiteSpace(oldTabJson) && oldTabJson != "{}")
            {
                try
                {
                    ApplyJsonToTabDirectly(old, oldTabJson);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Prev] Error saving JSON: {ex.Message}");
                }
            }
        } 
    } 
}
```

**After:**
```csharp
public PreviousStudyTab? SelectedPreviousStudy
{ 
    get => _selectedPreviousStudy; 
    set 
    { 
        var old = _selectedPreviousStudy;
        
        // DISABLED 2025-02-08: Auto-save on tab switch removed per user request
        // Users now need to manually save changes using the "Save to DB" button
        // Previous behavior: Captured old tab's JSON and applied it before switching away
        
        if (SetProperty(ref _selectedPreviousStudy, value)) 
        { 
            // ... update UI bindings ...
            // Auto-save logic removed - changes are only saved via explicit "Save to DB" action
        } 
    } 
}
```

---

## User Impact

### Behavior Changes

**Before:**
- User edits previous study A's fields (splitting, JSON edits, etc.)
- User switches to previous study B by clicking its tab
- System automatically saves study A's changes to its in-memory state
- Changes persist until application close or explicit DB save

**After:**
- User edits previous study A's fields
- User switches to previous study B
- System **DOES NOT** automatically save study A's changes
- Changes are **LOST** unless user clicks "Save Previous Study to DB" button before switching tabs

### User Workflow

Users must now **explicitly save** any changes to previous studies using the "Save Previous Study to DB" button before:
- Switching to another previous study tab
- Closing the patient
- Exiting the application

### Risk of Data Loss

?? **Warning**: This change introduces a risk of data loss if users:
- Edit a previous study
- Switch to another tab **without** clicking "Save to DB"
- Expect their changes to be preserved

**Mitigation**: Users should be trained to save changes explicitly using the button.

---

## Alternative Implementations (Not Used)

### Option A: Prompt Before Tab Switch
- Show a "Save changes?" dialog when switching tabs if changes detected
- **Rejected**: Too intrusive for user workflow

### Option B: Visual Indicator for Unsaved Changes
- Add a "*" or colored indicator on tabs with unsaved changes
- **Rejected**: Requires dirty-tracking infrastructure

### Option C: Auto-Save with Toggle
- Add a settings option to enable/disable auto-save on tab switch
- **Rejected**: Over-engineering for current requirement

---

## Testing

### Test Case 1: Changes Lost Without Save

**Steps:**
1. Open previous study A
2. Edit split ranges or JSON fields
3. Switch to previous study B
4. Switch back to previous study A

**Expected Result:**
- Changes from step 2 are **LOST**
- Study A displays original values from database

**Actual Result:** ? Pass - Changes are not persisted

---

### Test Case 2: Explicit Save Preserves Changes

**Steps:**
1. Open previous study A
2. Edit split ranges or JSON fields
3. Click "Save Previous Study to DB" button
4. Switch to previous study B
5. Switch back to previous study A

**Expected Result:**
- Changes are **preserved** because of explicit save in step 3
- Study A displays edited values

**Actual Result:** ? Pass - Changes persist after explicit save

---

### Test Case 3: Database Save Workflow

**Steps:**
1. Open previous study A
2. Edit findings proofread field
3. Switch to previous study B (changes lost in memory)
4. Close patient
5. Reopen same patient
6. Check previous study A

**Expected Result:**
- Changes from step 2 are **NOT** in database
- Study A displays original values

**Actual Result:** ? Pass - Only explicitly saved changes persist to database

---

## Future Enhancements

### Potential Improvements

1. **Dirty Tracking** - Track unsaved changes per tab and show visual indicator
2. **Confirmation Dialog** - Prompt "Save changes?" when switching away from edited tab
3. **Auto-Save Toggle** - Settings option to re-enable auto-save behavior
4. **Undo/Redo** - Per-tab edit history for accident recovery

### Re-Enabling Auto-Save

If auto-save needs to be restored in the future:
1. Uncomment the disabled code blocks in `SelectedPreviousStudy` setter
2. Search for "DISABLED 2025-02-08" comments
3. Remove the `/* */` comment markers
4. Test the restored behavior with existing test cases

---

## Related Features

- **Previous Study Loading** - `MainViewModel.PreviousStudiesLoader.cs`
- **JSON Synchronization** - `MainViewModel.PreviousStudies.Json.cs`
- **Database Save** - `SavePreviousStudyToDBCommand` in `MainViewModel.Commands.cs`

---

## References

### Previous Documentation
- `FIX_2025-11-02_PreviousStudyJsonSaveOnTabSwitch.md` - Original auto-save implementation

### Related Issues
- User request 2025-02-08: Disable auto-save on tab switch
- Original issue (2025-11-02): JSON changes lost on tab switch (fixed, now reverted)

---

## Summary

This change removes the auto-save behavior when switching between previous study tabs, as requested by the user. Changes to previous studies are now only saved when the user explicitly clicks the "Save Previous Study to DB" button.

**Key Points:**
- ? Auto-save disabled (per user request)
- ? No compilation errors
- ? Build passes
- ?? Users must manually save changes
- ?? Risk of data loss if users forget to save
- ? Original code preserved in comments for future restoration

The implementation is clean, reversible, and well-documented for future maintainers.
