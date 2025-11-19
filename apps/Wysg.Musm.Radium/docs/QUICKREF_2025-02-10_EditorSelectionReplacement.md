# Quick Reference: Editor Selection Replacement Fix (2025-02-10)

## Problem
Selected text in editors was not being deleted when Enter or characters were pressed.

## Solution
Added selection deletion logic in three places in `EditorControl.Popup.cs`:
1. `OnTextEntering` - for all character input
2. `OnTextAreaPreviewKeyDown` - for Enter key
3. `OnTextAreaPreviewKeyDown` - for Space key

## Behavior Change

### Before
- Select text + Enter ¡æ Text moves down ?
- Select text + Space ¡æ Nothing happens ?
- Select text + Character ¡æ Nothing happens ?

### After
- Select text + Enter ¡æ Text deleted, newline inserted ?
- Select text + Space ¡æ Text deleted, space inserted ?
- Select text + Character ¡æ Text deleted, character inserted ?

## Files Changed
- `src\Wysg.Musm.Editor\Controls\EditorControl.Popup.cs`

## Build Status
? Successful

## Documentation
See `BUGFIX_2025-02-10_EditorSelectionReplacement.md` for complete details.
