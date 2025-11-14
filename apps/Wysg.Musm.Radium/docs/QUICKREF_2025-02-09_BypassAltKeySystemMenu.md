# Quick Reference: Bypass Alt Key System Menu

**Last Updated:** 2025-02-09 (v2 - Fixed Alt+Arrow)  
**Status:** ? Complete

## What It Does

Prevents the Alt key from activating the Windows system menu and stealing focus to the title bar.

**Important:** Only suppresses Alt when pressed **completely alone** - all Alt combinations work normally.

## User Impact

**Before:**
1. User presses Alt
2. Focus moves to window title bar
3. Next key press is ignored or opens menu dropdown
4. User is frustrated ??

**After:**
1. User presses Alt
2. Nothing happens (focus stays where it was)
3. Next key press works normally
4. User is happy ??

## Alt+Arrow Fix (v2)

Initial implementation blocked Alt+Arrow combinations. Now fixed:
- Alt alone ¡æ Suppressed ?
- Alt+Up ¡æ Works ?
- Alt+Down ¡æ Works ?
- Alt+Left ¡æ Works ?
- Alt+Right ¡æ Works ?

## Technical Details

- **Handler:** `OnPreviewKeyDownBypassAlt()`
- **Location:** `MainWindow.xaml.cs` constructor
- **Logic:** Uses `e.SystemKey` to distinguish Alt-alone from Alt+combinations
- **Key Check:** Only suppresses when `e.SystemKey` is None/LeftAlt/RightAlt

## How It Works

```
Alt pressed alone:
  e.SystemKey = None (or LeftAlt/RightAlt)
  ¡æ Suppress (mark as handled)

Alt+Arrow pressed:
  e.SystemKey = Up/Down/Left/Right
  ¡æ Allow (do not suppress)
```

## Preserved Shortcuts

All Alt-based keyboard shortcuts still work:
- **Alt+Arrow** (editor navigation) - CONFIRMED WORKING
- Ctrl+Alt+S (Spy window)
- Ctrl+Alt+O (Open study)
- Ctrl+Alt+T (Toggle sync text)
- Any other Alt combinations

## Testing

- [x] Build succeeds
- [x] Alt alone does not activate menu
- [x] Alt+Arrow navigation works
- [x] Ctrl+Alt+S opens Spy window
- [x] Global hotkeys work

## Version History

- v1: Initial implementation (too aggressive)
- v2: Fixed Alt+Arrow compatibility

## Files Changed

- `apps\Wysg.Musm.Radium\Views\MainWindow.xaml.cs`

## Documentation

- **Full Details:** `BUGFIX_2025-02-09_BypassAltKeySystemMenu.md`
- **Summary:** `SUMMARY_2025-02-09_BypassAltKeySystemMenu.md`
- **README:** Updated with changelog entry
