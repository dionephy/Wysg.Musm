# Fix: Automation Window Drag-Drop Positioning

**Date**: 2025-11-27  
**Type**: Bug Fix  
**Status**: ? Complete  
**Project**: Wysg.Musm.Radium

---

## Problem Statement

In the Automation Window ¡æ Automation tab, users could not drag-drop modules between specific positions within panes. Modules would only drop at the very end of all modules in the pane, making it impossible to reorder modules or insert them at specific positions.

Additionally, the drag-drop experience lacked visual feedback (ghost tooltip and guidance line) that was already implemented in Settings Window ¡æ Automation tab.

---

## Solution Overview

Implemented proper insert-at-index drag-drop functionality with visual feedback, bringing the Automation Window drag-drop behavior in line with the Settings Window implementation.

### Key Changes

1. **Insert-at-Index Logic**: Calculate drop position based on mouse Y coordinate relative to existing items
2. **Visual Feedback**: Added ghost tooltip showing dragged item and orange guidance line showing drop position
3. **Reordering Support**: Proper handling of reordering within same list vs. moving between different lists
4. **Library Semantics**: Library lists remain copy-only (never remove from source)

---

## Files Modified

### 1. `AutomationWindow.Automation.cs`

**Added Fields**:
```csharp
private Border? _automationDragGhost;
private string? _automationDragItem;
private ListBox? _automationDragSource;
private int _automationDragSourceIndex = -1;
private Border? _automationDropIndicator;
```

**New Methods**:
- `OnAutomationProcDragOver()`: Handle drag-over event, position ghost and indicator
- `CreateAutomationGhost()`: Create blue semi-transparent tooltip showing dragged item
- `EnsureAutomationDropIndicator()`: Create orange horizontal line indicator
- `PositionAutomationDropIndicator()`: Calculate and position drop indicator
- `ClearAutomationDropIndicator()`: Remove drop indicator
- `GetAutomationItemInsertIndex()`: Calculate insert index based on mouse position
- `AddChildToAutomationOverlay()`: Add visual elements to canvas overlay
- `RemoveAutomationGhost()`: Remove ghost tooltip
- `ClearAutomationDragState()`: Clean up all drag state on drop/cancel

**Modified Methods**:
- `OnAutomationProcDrag()`: Store drag source information, create ghost
- `OnAutomationProcDrop()`: Use calculated insert index instead of appending to end
- `OnAutomationListDragLeave()`: Clear drop indicator when leaving list

### 2. `AutomationWindow.xaml`

**Updated All Module ListBoxes**:
Added `DragOver="OnAutomationProcDragOver"` event handler to:
- `lstNewStudy`
- `lstAddStudy`
- `lstShortcutOpenNew`
- `lstShortcutOpenAdd`
- `lstShortcutOpenAfterOpen`
- `lstSendReport`
- `lstSendReportPreview`
- `lstShortcutSendReportPreview`
- `lstShortcutSendReportReportified`
- `lstTest`
- `lstLibrary` (for ghost positioning only, no drops)
- `lstCustomModules` (for ghost positioning only, no drops)

---

## Technical Implementation

### Insert Index Calculation

```csharp
private int GetAutomationItemInsertIndex(ListBox lb, Point pos)
{
    for (int i = 0; i < lb.Items.Count; i++)
    {
        if (lb.ItemContainerGenerator.ContainerFromIndex(i) is FrameworkElement fe)
        {
            var r = fe.TransformToAncestor(lb).TransformBounds(
                new Rect(0, 0, fe.ActualWidth, fe.ActualHeight));
            if (pos.Y < r.Top + r.Height / 2)
            {
                return i; // Insert before this item
            }
        }
    }
    return lb.Items.Count; // Insert at end
}
```

**Logic**:
- Iterate through visible items
- Compare mouse Y position with item midpoint
- Return index of first item whose midpoint is below mouse
- If no item found, return count (insert at end)

### Drop Behavior by Source and Target

| Source Type | Target Type | Behavior |
|-------------|-------------|----------|
| Library/Custom | Any Ordered | Copy (duplicate allowed) |
| Ordered | Same List | Move (reorder within list) |
| Ordered | Different List | Move (remove from source, insert at target) |
| Any | Library/Custom | Copy (never modify library) |

### Visual Feedback Components

**Ghost Tooltip**:
- Blue semi-transparent border (`Color.FromArgb(180, 60, 120, 255)`)
- White text showing module name
- Follows mouse cursor (+8px offset)
- Removed on drop/cancel

**Drop Indicator**:
- Orange-red horizontal line (2px height, `Brushes.OrangeRed`)
- Positioned at calculated insert location
- Spans width of target list (minus 8px padding)
- Only shown for non-library lists
- Removed when leaving list or on drop

---

## User Experience Improvements

### Before Fix
- ? Modules always dropped at end of list
- ? No visual indication of where drop would occur
- ? No feedback during drag operation
- ? Impossible to insert module between existing modules
- ? Reordering required removing and re-adding

### After Fix
- ? Modules drop at precise mouse position
- ? Orange line shows exact drop location
- ? Blue ghost tooltip follows cursor
- ? Can insert anywhere in the list
- ? Easy reordering with drag-drop
- ? Consistent with Settings Window behavior

---

## Testing Scenarios

### Scenario 1: Insert Module Between Existing Modules
**Steps**:
1. Open Automation Window ¡æ Automation tab
2. Drag module from "Available Modules"
3. Hover between two modules in "New Study" pane
4. Observe orange guidance line between modules
5. Drop module
6. Verify module inserted at correct position

**Expected**: Module inserted at indicated position, not at end

### Scenario 2: Reorder Within Same List
**Steps**:
1. Select module in "New Study" pane (e.g., 3rd item)
2. Drag to position between 1st and 2nd items
3. Observe guidance line
4. Drop module
5. Verify module moved to new position

**Expected**: Module reordered correctly, no duplicate created

### Scenario 3: Move Between Different Lists
**Steps**:
1. Select module in "New Study" pane
2. Drag to "Add Study" pane
3. Drop between existing modules
4. Verify module removed from "New Study"
5. Verify module inserted at correct position in "Add Study"

**Expected**: Module moved (not copied) to target list at correct position

### Scenario 4: Copy from Library
**Steps**:
1. Select module in "Available Modules"
2. Drag to "New Study" pane
3. Drop between existing modules
4. Verify module still exists in "Available Modules"
5. Verify duplicate created at drop position in "New Study"

**Expected**: Module copied (not moved), inserted at correct position

### Scenario 5: Visual Feedback During Drag
**Steps**:
1. Start dragging any module
2. Observe blue ghost tooltip appears
3. Move mouse over target list
4. Observe orange guidance line appears
5. Move mouse over different positions
6. Observe guidance line moves accordingly

**Expected**: Visual feedback updates in real-time during drag

---

## Known Limitations

1. **ScrollViewer Interaction**: If target list is inside ScrollViewer, auto-scroll during drag is not implemented
2. **Touch Gestures**: Optimized for mouse; touch drag-drop not tested
3. **Keyboard Navigation**: No keyboard-only reordering support
4. **Undo/Redo**: No built-in undo for drag-drop operations

---

## Code Quality Notes

- Follows existing SettingsWindow implementation pattern
- Defensive null checks throughout
- Debug logging for troubleshooting
- Proper cleanup in `finally` block
- Idempotent state clearing methods

---

## Related Issues/PRs

- Related to Settings Window implementation (already complete)
- Addresses user feedback about drag-drop behavior inconsistency

---

## Migration Notes

**Breaking Changes**: None

**Backward Compatibility**: Fully compatible with existing saved automation configurations

**Data Migration**: Not required

---

## Future Enhancements

1. **Auto-scroll**: Implement auto-scroll when dragging near ScrollViewer edges
2. **Multi-select Drag**: Support dragging multiple modules simultaneously
3. **Keyboard Reordering**: Add Ctrl+Up/Down shortcuts for reordering
4. **Undo Support**: Implement undo/redo for drag-drop operations
5. **Animation**: Add subtle animation when modules reorder
6. **Duplicate Prevention**: Optional mode to prevent duplicates in target list

---

**Implementation Status**: ? Complete  
**Build Status**: ? Success  
**Documentation**: ? Complete
