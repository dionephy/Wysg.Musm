# Fix: Manage Studynames Window Preselects Current Studyname

**Date:** 2025-12-17  
**Status:** Completed  
**Category:** UX Fix

## Problem
When the **Manage Studynames** button was clicked from the main window, the Studyname°ÍLOINC manager always selected the first entry in the studynames list. Users working on an active case had to search manually to find the current studyname each time.

## Solution
Hooked the main window command to pass the current studyname (when available) into `StudynameLoincWindow.Open`. The window already supports a preselect hint via `StudynameLoincViewModel.Preselect`, so wiring that hint from the active case ensures the matching studyname is highlighted automatically.

## Files Changed
- `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.Init.cs`
  - Updated `OpenStudynameMapCommand` to trim and forward the in-progress `StudyName` so the manager loads with the matching entry selected.

## Testing
1. Load a study so that the Current Study panel shows a populated Studyname.
2. Click **Manage Studynames**.
3. Verify the studynames list focuses the current studyname instead of the first entry.
4. Clear the study context (no Studyname) and open the manager again; the list should fall back to the default first item.

## Impact
- Eliminates repeated manual search when editing mappings for the current study.
- Behavior falls back gracefully when no Studyname is present.
- Users can now press Enter to commit whichever snippet option is highlighted, matching the behavior of Tab.
- Multi-choice placeholders retain their existing Enter behavior (collecting checked items before exiting).

## Follow-up: Duplicate Studynames on Open
After wiring the preselect hint from the current study, opening the Manage Studynames dialog would create a duplicate entry whenever the requested studyname already existed. The view-model was calling `EnsureStudynameAsync`, which inserts new rows when the name is not found locally. The preselect logic was updated to:

- Track the requested studyname locally and attempt a case-insensitive match against the already-loaded list.
- Select the first existing item when the requested name is absent, matching the original default behavior.
- Avoid any repository writes during preselect to prevent unintended duplicates.

This ensures opening the dialog simply highlights the existing row without polluting the database.
