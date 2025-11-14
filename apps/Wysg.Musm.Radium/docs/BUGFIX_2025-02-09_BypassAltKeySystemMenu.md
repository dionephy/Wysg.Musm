# Bug Fix: Bypass Alt Key System Menu Behavior

**Date:** 2025-02-09  
**Type:** Bug Fix  
**Priority:** Medium  
**Status:** ? Complete (Fixed Alt+Arrow compatibility issue)

## Problem

When the Alt key was pressed in the main window, Windows would activate the system menu behavior:
- Focus was transferred to the window title bar
- The next key press was either ignored or triggered menu actions (e.g., Down key opening menu dropdown)
- This interrupted the normal workflow and caused user frustration

This is a default WPF/Windows behavior where Alt key activates the window's menu system.

## Solution

Added a `PreviewKeyDown` event handler that intercepts Alt key presses and marks them as handled **only when Alt is pressed completely alone** (without arrow keys or other keys).

### Implementation Details

**File Modified:**
- `apps\Wysg.Musm.Radium\Views\MainWindow.xaml.cs`

**Changes:**
1. Added `PreviewKeyDown` event handler registration in the `MainWindow` constructor
2. Created `OnPreviewKeyDownBypassAlt` method that:
   - Detects when Alt key is pressed alone (without any other keys)
   - Checks both `e.Key` and `e.SystemKey` to distinguish Alt-alone from Alt+combinations
   - Marks the event as handled **only** when Alt is truly alone
   - Allows all Alt key combinations (Alt+Arrow, Ctrl+Alt+S, etc.) to pass through normally

### Key Technical Details

**Why the SystemKey check?**
- When Alt is pressed alone, `e.SystemKey` will be `Key.None`, `Key.LeftAlt`, or `Key.RightAlt`
- When Alt+Arrow is pressed, `e.SystemKey` will be the arrow key (`Key.Up`, `Key.Down`, etc.)
- This allows us to distinguish between Alt-alone and Alt-combinations

**Fix for Alt+Arrow Issue:**
Initial implementation was too aggressive and caught Alt+Arrow combinations. The fix adds:
```csharp
// Only handle if Alt is truly alone (no arrow keys or other keys)
if (e.SystemKey == Key.None || e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt)
{
    e.Handled = true;
}
```

### Code Implementation

```csharp
public MainWindow()
{
    InitializeComponent();
    Loaded += OnLoaded;
    SizeChanged += OnWindowSizeChanged;
    Closing += OnClosing;
    
    // Initialize triple-click paragraph selection support
    InitializeTripleClickSupport();
    
    // Bypass Alt key system menu behavior
    PreviewKeyDown += OnPreviewKeyDownBypassAlt;
}

/// <summary>
/// Bypass Alt key system menu behavior to prevent title bar focus and menu dropdown.
/// When Alt is pressed alone (without other keys), it normally activates the window menu system
/// and gives focus to the title bar, causing the next key press to be ignored or trigger menu actions.
/// This handler suppresses that behavior by marking the Alt key as handled.
/// </summary>
private void OnPreviewKeyDownBypassAlt(object sender, KeyEventArgs e)
{
    // Only suppress Alt when it's pressed completely alone (not with arrow keys or other keys)
    // SystemKey is used for Alt combinations, Key is used for regular keys
    
    // Check if this is Alt being pressed alone
    bool isAltAlone = (e.Key == Key.System || e.Key == Key.LeftAlt || e.Key == Key.RightAlt) &&
                      (e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt || e.SystemKey == Key.None);
    
    // Only handle if Alt is truly alone (no arrow keys or other keys)
    if (isAltAlone && e.KeyboardDevice.Modifiers == ModifierKeys.Alt)
    {
        // Don't suppress if this is part of a combination (check if SystemKey indicates a combination)
        // When Alt+Arrow is pressed, SystemKey will be the arrow key (Up, Down, Left, Right)
        if (e.SystemKey == Key.None || e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt)
        {
            e.Handled = true;
            System.Diagnostics.Debug.WriteLine("[MainWindow] Suppressed Alt key (pressed alone)");
        }
    }
}
```

## Impact

**Positive:**
- Alt key alone no longer steals focus to the title bar
- Next key press after Alt works as expected
- Improved user experience during keyboard navigation
- **Alt+Arrow navigation works correctly** (fixed in v2)
- All other Alt-based shortcuts continue to work normally

**Neutral:**
- No breaking changes
- Existing Alt key combinations remain fully functional

**Testing:**
- Test Alt key press alone ¡æ should not activate menu system ?
- Test Alt+Up/Down/Left/Right ¡æ should work for navigation ?
- Test Ctrl+Alt+S ¡æ should still open Spy window ?
- Test global hotkeys (Ctrl+Alt+O, Ctrl+Alt+T, Ctrl+Decimal) ¡æ should continue working ?

## Related Files

- `MainWindow.xaml.cs` - Main window code-behind with Alt key bypass logic

## Technical Notes

### Why PreviewKeyDown?

- `PreviewKeyDown` is a tunneling event that fires before `KeyDown`
- It allows us to intercept and suppress the Alt key before it reaches the system menu handler
- Using the preview event ensures the handling occurs at the earliest possible stage

### System Key Handling

The Alt key is considered a "System Key" in WPF:
- When Alt is pressed alone, `e.SystemKey` is `Key.None` (or the Alt key itself)
- When Alt+Arrow is pressed, `e.SystemKey` is the arrow key (`Key.Up`, `Key.Down`, etc.)
- We check both `e.Key` and `e.SystemKey` to accurately detect Alt-alone vs Alt-combinations
- This prevents accidentally suppressing Alt+Arrow and other useful combinations

### Preserved Functionality

The following Alt-based shortcuts continue to work:
- **Alt+Arrow navigation** (for moving between text editors) - CONFIRMED WORKING
- Ctrl+Alt+S (open Spy window)
- Ctrl+Alt+O (open study - if configured)
- Ctrl+Alt+T (toggle sync text - if configured)
- Any other Alt-modifier combinations

## Version History

**v1 (Initial):** Basic Alt suppression - too aggressive, broke Alt+Arrow
**v2 (Fixed):** Added SystemKey check to distinguish Alt-alone from Alt+combinations

## Build Status

? Build successful with no errors or warnings

## Next Steps

- Monitor user feedback for any Alt key-related issues
- Consider adding similar handling to other windows if needed
- Document this pattern for future window implementations
