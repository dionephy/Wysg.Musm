# FIX: SNOMED Link Window Selection-Only Mode

**Date**: 2025-02-06  
**Issue**: "Failed to map phrase: Phrase must be account-specific for accountId" error when accessing SNOMED link window from phrase extraction window

## Problem

When opening the SNOMED link window from the phrase extraction window, users would get an error when trying to click the "Map" button. This occurred because:

1. Phrase extraction window opens SNOMED link window with `phraseId = -1` (temporary, phrase not yet saved)
2. User selects a SNOMED concept and clicks "Map" button
3. The map operation tries to call `MapPhraseAsync(_phraseId=-1, ...)` which fails because the phrase doesn't exist in the database yet

## Root Cause

The `PhraseSnomedLinkWindow` was designed for a single use case: mapping existing phrases that already have a valid database ID. It didn't account for the "concept selection" use case from phrase extraction where the phrase hasn't been saved yet.

## Solution

Introduced a **Selection-Only Mode** to the SNOMED link window that:

1. Detects when `phraseId < 0` (temporary phrase)
2. Disables the "Map" button (via `CanMap()` check)
3. Shows a visual notice explaining the selection-only mode
4. Updates scope text to indicate "Selection Mode (phrase not yet saved)"

### Implementation Details

#### PhraseSnomedLinkWindowViewModel Changes

```csharp
// NEW property to detect selection-only mode
public bool IsSelectionOnlyMode => _phraseId < 0;

// Updated CanMap() to disable Map button in selection-only mode
private bool CanMap() => SelectedConcept != null && !IsSelectionOnlyMode;

// Updated scope text in constructor
if (IsSelectionOnlyMode)
{
    ScopeText = "Selection Mode (phrase not yet saved)";
}
else
{
    ScopeText = accountId == null ? "Global" : $"Account {accountId}";
}

// Added safety check in OnMapAsync()
if (IsSelectionOnlyMode)
{
    MessageBox.Show("Cannot map in selection-only mode. Please save the phrase first.", "SNOMED", MessageBoxButton.OK, MessageBoxImage.Warning);
    return;
}
```

#### PhraseSnomedLinkWindow.xaml Changes

1. **Added BooleanToVisibilityConverter** resource for conditional visibility
2. **Added Selection Mode Notice** (visible only when `IsSelectionOnlyMode = true`):
   - Orange header: "Selection Mode"
   - Instruction text explaining the workflow
   - Styled with gray background for visibility
3. **Increased window height** from 540 to 580 to accommodate the notice
4. **Map button remains disabled** via command binding when in selection-only mode

### User Workflow

#### From Phrase Extraction Window (Selection-Only Mode)
1. User selects text in phrase extraction editor
2. Clicks "Map to SNOMED-CT" button
3. SNOMED link window opens with:
   - Orange notice explaining selection mode
   - Search pre-filled with selected text
   - "Map" button disabled (grayed out)
4. User searches and selects a SNOMED concept
5. User closes the window
6. Selected concept appears in phrase extraction window's "Temporary SNOMED-CT Mapping" panel (green)
7. User clicks "Save with SNOMED" to save phrase + mapping in one operation

#### From Phrases Management Window (Normal Mode)
1. User right-clicks existing phrase ¡æ "Link to SNOMED"
2. SNOMED link window opens normally (no orange notice)
3. User searches and selects concept
4. User clicks "Map" button (enabled)
5. Mapping saved immediately
6. Success message shown

## Files Modified

1. **apps/Wysg.Musm.Radium/Views/PhraseSnomedLinkWindow.xaml.cs**
   - Added `IsSelectionOnlyMode` property
   - Modified `CanMap()` to check mode
   - Updated constructor to set appropriate scope text
   - Added safety check in `OnMapAsync()`

2. **apps/Wysg.Musm.Radium/Views/PhraseSnomedLinkWindow.xaml**
   - Added `BooleanToVisibilityConverter` resource
   - Added selection-only mode notice with conditional visibility
   - Increased window height to 580
   - Map button automatically disabled via command binding

## Testing Checklist

- [x] Build succeeds without errors
- [ ] Open phrase extraction window ¡æ select text ¡æ click "Map to SNOMED-CT"
  - [ ] Orange notice appears at top
  - [ ] Scope shows "Selection Mode (phrase not yet saved)"
  - [ ] "Map" button is disabled (grayed out)
  - [ ] Can search and select concepts
  - [ ] Selected concept captured when window closes
- [ ] From phrases management ¡æ right-click phrase ¡æ "Link to SNOMED"
  - [ ] No orange notice (normal mode)
  - [ ] "Map" button is enabled
  - [ ] Can map successfully
- [ ] Complete workflow: extraction ¡æ select text ¡æ map to SNOMED ¡æ save with SNOMED
  - [ ] Phrase and mapping both saved successfully
  - [ ] No errors

## Related Issues

- Original issue: "Phrase must be account-specific for accountId" error
- Related enhancement: ENHANCEMENT_2025-02-06_PhraseExtractionWindowUpdate.md

## Notes

The dual-mode approach allows the same window to serve two different purposes:
1. **Selection-only**: For choosing concepts before saving (phrase extraction)
2. **Immediate mapping**: For mapping existing phrases (phrases management)

This avoids code duplication while providing a clear UX distinction between the two modes.
