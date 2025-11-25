# Enhancement: Collapsible JSON Panels

**Date:** 2025-11-02  
**Status:** Implemented

## Overview
Make the JSON panels in both `ReportInputsAndJsonPanel` and `PreviousReportTextAndJsonPanel` collapsible with a toggle button, and default them to collapsed state to improve screen real estate usage.

## Motivation
- JSON panels take up significant horizontal space (1/5 of the layout in 5-column grid)
- Users don't always need to see the JSON view while editing
- Collapsing the JSON panel provides more space for the main editing area
- Default collapsed state optimizes the initial layout for the most common workflow

## Implementation Details

### 1. ReportInputsAndJsonPanel.xaml
**Location:** `apps/Wysg.Musm.Radium/Controls/ReportInputsAndJsonPanel.xaml`

#### Changes:
- Add a collapse toggle button in the JSON column header
- Add a dependency property `IsJsonCollapsed` with default value `true`
- Modify the JSON column width definition to support auto-hiding:
  - When collapsed: `Width="0"`
  - When expanded: `Width="1*" MinWidth="200"`
- Update GridSplitter visibility to match collapsed state
- Bind TextBox visibility to the collapsed state

### 2. PreviousReportTextAndJsonPanel.xaml
**Location:** `apps/Wysg.Musm.Radium/Controls/PreviousReportTextAndJsonPanel.xaml`

#### Changes:
- Add a collapse toggle button in the JSON column header (Row 0)
- Add a dependency property `IsJsonCollapsed` with default value `true`
- Modify column 4 width definition similarly
- Update GridSplitter for column 3 visibility
- Bind TextBox visibility to collapsed state

### 3. Code-Behind Updates
**Locations:** 
- `apps/Wysg.Musm.Radium/Controls/ReportInputsAndJsonPanel.xaml.cs`
- `apps/Wysg.Musm.Radium/Controls/PreviousReportTextAndJsonPanel.xaml.cs`

#### Changes:
- Add `IsJsonCollapsed` dependency property with default value `true`
- Add property changed callback to update column width and element visibility
- Implement `UpdateJsonColumnVisibility()` method to handle:
  - Column width changes (0 vs 1* MinWidth 200)
  - GridSplitter visibility
  - TextBox visibility

## UI Design
- Toggle button positioned in Row 0, Column 4 (JSON column header)
- Button content: "��" when expanded (collapse arrow), "��" when collapsed (expand arrow)
- Uses existing `DarkToggleButtonStyle` for consistency
- Button remains visible even when collapsed to allow re-expansion

## Default State
- Both panels default to **collapsed** (`IsJsonCollapsed="true"`)
- Users can expand by clicking the toggle button
- State is not persisted (resets to collapsed on application restart)

## Benefits
1. **More editing space:** Default collapsed state maximizes space for text fields
2. **User control:** Toggle button provides easy access when JSON view is needed
3. **Visual clarity:** Reduce visual clutter for normal editing workflow
4. **Consistent UX:** Both JSON panels behave identically

## Testing Checklist
- [x] Toggle button appears in both panels
- [x] Clicking toggle expands/collapses JSON column
- [x] Default state is collapsed
- [x] GridSplitter visibility matches collapsed state
- [x] Layout remains responsive when toggling
- [x] Reverse layout mode still works correctly
- [x] No build errors or warnings
