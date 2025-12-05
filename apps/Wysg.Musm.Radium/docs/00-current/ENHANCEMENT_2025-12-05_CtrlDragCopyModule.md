# Enhancement: Ctrl+Drag to Copy Module

**Date**: 2025-12-05  
**Status**: Completed  
**Category**: User Experience Enhancement

## Summary

Added the ability to copy automation modules between session panes using Ctrl+drag instead of moving them.

## Problem

Previously, when dragging a module from one session pane (e.g., "New Study") to another session pane (e.g., "Send Report"), the module was always **moved** (removed from the source and added to the target). Users had no way to duplicate a module across multiple automation sequences without manually adding it from the library each time.

## Solution

Implemented Ctrl+drag copy functionality:
- **Default behavior (no modifier)**: Drag moves the module (removes from source, adds to target)
- **Ctrl+drag**: Drag copies the module (keeps in source, adds to target)
- Visual feedback:
  - A green "+" indicator appears on the drag ghost when in copy mode
  - The mouse cursor changes to Copy cursor when Ctrl is held, Move cursor otherwise

## Implementation Details

### Modified Files

**`apps/Wysg.Musm.Radium/Views/AutomationWindow.Automation.cs`**

1. **`OnAutomationProcDragOver`**: 
   - Added `e.Effects` assignment based on Ctrl key state to control the mouse cursor
   - `DragDropEffects.Copy` when Ctrl is pressed or dragging from library
   - `DragDropEffects.Move` for normal drag operations
   - `DragDropEffects.None` when dropping onto library (not allowed)

2. **`OnAutomationProcDrop`**: Added check for `Keyboard.Modifiers & ModifierKeys.Control` to determine if copy operation should be performed instead of move.

3. **`CreateAutomationGhost`**: Modified to include a hidden "+" indicator that can be shown/hidden dynamically.

4. **`UpdateAutomationGhostCopyIndicator`**: Helper method to toggle the visibility of the copy indicator on the drag ghost.

### Behavior Matrix

| Drag From | Drag To | Ctrl Pressed | Cursor | Result |
|-----------|---------|--------------|--------|--------|
| Library | Session Pane | Any | Copy | Copy (always) |
| Session Pane | Same Pane | Any | Move | Reorder |
| Session Pane | Different Pane | No | Move | Move |
| Session Pane | Different Pane | Yes | Copy | **Copy** |
| Session Pane | Delete Zone | Any | Move | Delete |
| Any | Library | Any | None | No action |

## User Experience

- Users can now easily duplicate modules across automation sequences
- Green "+" visual indicator provides clear feedback when copy mode is active
- Mouse cursor correctly shows Copy or Move depending on operation type
- Existing move behavior is preserved as the default

## Testing Notes

1. Drag module from "New Study" to "Send Report" ¡æ Move cursor, module moves
2. Ctrl+drag module from "New Study" to "Send Report" ¡æ Copy cursor, module copies
3. Drag from Library to any pane ¡æ Copy cursor (always copy from library)
4. Ghost shows "+" when in copy mode
5. Ghost hides "+" when in move mode
