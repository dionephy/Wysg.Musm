# Summary: Bypass Alt Key System Menu Behavior

**Date:** 2025-11-09  
**Status:** ? Complete (v2 - Fixed Alt+Arrow compatibility)

## Quick Summary

Fixed issue where pressing Alt key alone would steal focus to the window title bar, causing the next key press to be ignored or trigger unwanted menu actions.

**Update:** Fixed initial implementation that was blocking Alt+Arrow combinations.

## What Changed

Added event handler in `MainWindow` constructor that intercepts Alt key presses and suppresses the system menu activation **only when Alt is pressed completely alone** (not with arrow keys or other keys).

## User Impact

- Alt key alone no longer interrupts workflow by stealing focus
- Next key press after Alt works as expected  
- **Alt+Arrow navigation works correctly** (fixed in v2)
- All Alt-based shortcuts (Alt+Arrow, Ctrl+Alt+S, etc.) continue working normally

## Technical Implementation

- Added `OnPreviewKeyDownBypassAlt` event handler
- Checks for Alt key press without any other keys (including arrow keys)
- Uses `e.SystemKey` to distinguish Alt-alone from Alt+combinations
- Marks event as handled only when Alt is truly alone
- Preserves all Alt-key combinations for shortcuts

## Key Fix (v2)

Initial implementation was too aggressive and blocked Alt+Arrow. Fixed by checking `e.SystemKey`:
- When Alt+Arrow is pressed, `e.SystemKey` is the arrow key (Up, Down, etc.)
- When Alt is pressed alone, `e.SystemKey` is None/LeftAlt/RightAlt
- Only suppress when Alt is truly alone

## Files Modified

- `apps\Wysg.Musm.Radium\Views\MainWindow.xaml.cs`

## Testing Checklist

- [x] Build succeeds without errors
- [x] Alt key alone does not activate menu system
- [x] Alt+Arrow navigation works correctly (FIXED)
- [x] Ctrl+Alt+S (Spy window) still works
- [x] Global hotkeys (Ctrl+Alt+O, Ctrl+Alt+T, Ctrl+Decimal) still work

## Documentation

See full details in: `BUGFIX_2025-11-09_BypassAltKeySystemMenu.md`
