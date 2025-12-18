# Fix: Keep Comparison + Main Window In Sync After LOINC Mapping Changes
**Date:** 2025-12-18  
**Status:** Complete

## Problem
Radiologists often opened the Comparison dialog, jumped into **Map to LOINC Part**, added missing modality parts, and expected every open window to reflect the new modality labels. Instead:
- The comparison dialog kept stale "OT" modalities until it was reopened.
- The main window's Previous Studies list never updated even after the comparison dialog closed.

## Solution
1. **Live comparison refresh**
   - `StudynameLoincWindow` now notifies any open `EditComparisonWindow` instances after the mapping dialog closes.
   - A new `EditComparisonViewModel.RefreshAfterMappingAsync` method re-checks which studynames have mappings and recomputes the modality badge for each available study.

2. **Propagate updates back to MainWindow**
   - `OnEditComparison` now awaits `RefreshPreviousStudyModalitiesAsync` once the dialog closes (OK or Cancel).
   - This helper re-evaluates every previous study tab's modality/title using the latest LOINC definitions so the left panel immediately reflects new identifiers.
   - `PreviousStudyTab.Modality`, `Title`, and `StudyDateTime` now raise change notifications, so the UI updates without reopening tabs.

3. **Bridge helper APIs**
   - `EditComparisonWindow` exposes a lightweight `RefreshAfterMappingAsync` API so other windows can trigger the refresh without touching internals.

## Impact
- Mapping a studyname instantly updates any open comparison dialog.
- Closing the comparison dialog pushes the refreshed modality abbreviations back to the Previous Studies list and the main comparison field.
- Users no longer need to reopen dialogs or reload patients to see their mapping work.

## Files
- `ViewModels/EditComparisonViewModel.cs`
- `Views/EditComparisonWindow.xaml.cs`
- `Views/StudynameLoincWindow.xaml.cs`
- `ViewModels/MainViewModel.PreviousStudiesLoader.cs`
- `ViewModels/MainViewModel.PreviousStudies.Models.cs`
- `ViewModels/MainViewModel.Commands.Handlers.cs`
