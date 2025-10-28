# TROUBLESHOOTING: Alt+Arrow Navigation Not Working

**Issue**: Alt+Arrow Navigation Feature Not Responding  
**Date**: 2025-01-30  
**Status**: ? Resolved

---

## Problem Description

After implementing the Alt+Arrow navigation feature for textbox navigation in `ReportInputsAndJsonPanel`, pressing Alt+Arrow key combinations did not trigger the expected navigation behavior.

---

## Root Cause

### WPF SystemKey Behavior

When the **Alt** modifier key is pressed in WPF, the framework treats subsequent key presses as **system keys**. This means:

- `KeyEventArgs.Key` returns `Key.System` (not the specific arrow key)
- The actual arrow key value is stored in `KeyEventArgs.SystemKey`

### Initial Implementation (BROKEN)

```csharp
source.PreviewKeyDown += (s, e) =>
{
    // ? WRONG: e.Key will be Key.System when Alt is pressed
    if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt && e.Key == sourceKey)
    {
        HandleAltArrowNavigation(source, target);
        e.Handled = true;
    }
};
```

**Why it failed**: When Alt+Down was pressed, `e.Key` was `Key.System`, not `Key.Down`, so the condition never matched.

---

## Solution

### Check SystemKey Property

```csharp
source.PreviewKeyDown += (s, e) =>
{
    // ? CORRECT: Check SystemKey when Key is System
    var actualKey = e.Key == Key.System ? e.SystemKey : e.Key;
    
    if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt && actualKey == sourceKey)
    {
        HandleAltArrowNavigation(source, target);
        e.Handled = true;
    }
};
```

**How it works**:
1. Check if `e.Key == Key.System` (indicates Alt is pressed)
2. If true, use `e.SystemKey` to get the actual arrow key
3. Otherwise, use `e.Key` directly (for future non-Alt scenarios)
4. Compare the actual key against the expected key

---

## Testing the Fix

### Before Fix
```
User Action: Press Alt+Down in Study Remark
Expected: Navigate to Chief Complaint
Actual: Nothing happens ?
Reason: e.Key is Key.System, not Key.Down
```

### After Fix
```
User Action: Press Alt+Down in Study Remark
Expected: Navigate to Chief Complaint
Actual: Navigates to Chief Complaint ?
Reason: e.SystemKey correctly returns Key.Down
```

---

## Additional Debugging Steps Taken

### 1. Designer File Regeneration

**Issue**: Compilation error - `txtStudyRemark` not found

**Solution**: Clean and rebuild project
```powershell
# Force regeneration of .g.cs designer files
dotnet clean
dotnet build
```

### 2. Verification Steps

- ? Checked XAML: `x:Name="txtStudyRemark"` present
- ? Checked code-behind: Method references correct controls
- ? Rebuilt project: Designer file regenerated
- ? Applied SystemKey fix: Alt+Arrow now works
- ? Tested all combinations: All navigation pairs functional

---

## Key Learnings

### WPF Key Event Behavior

| Scenario | e.Key | e.SystemKey | Notes |
|----------|-------|-------------|-------|
| Press "A" | Key.A | Key.None | Normal key press |
| Press Alt | Key.System | Key.LeftAlt | Alt key down |
| Press Alt+Down | **Key.System** | **Key.Down** | Arrow is in SystemKey! |
| Press Alt+F4 | Key.System | Key.F4 | F-keys also in SystemKey |

**Rule**: Always check `e.SystemKey` when `e.Key == Key.System` for Alt+Key combinations.

---

## General Troubleshooting Checklist

When Alt+Arrow (or any Alt+Key) combinations don't work in WPF:

1. ? **Check Event Handler**: Using `PreviewKeyDown` not `KeyDown`?
2. ? **Check Modifier**: `e.KeyboardDevice.Modifiers == ModifierKeys.Alt`?
3. ? **Check SystemKey**: Using `e.SystemKey` when `e.Key == Key.System`?
4. ? **Set Handled**: `e.Handled = true` to prevent bubbling?
5. ? **Designer Files**: Rebuild project if controls not found?
6. ? **Control Names**: All `x:Name` attributes correctly set in XAML?

---

## Code Pattern for Alt+Key Combinations

### Template for Alt+Key Detection

```csharp
control.PreviewKeyDown += (s, e) =>
{
    // Step 1: Get the actual key (handle SystemKey for Alt combinations)
    var actualKey = e.Key == Key.System ? e.SystemKey : e.Key;
    
    // Step 2: Check if Alt is pressed AND the key matches
    if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt && actualKey == Key.YourKey)
    {
 // Step 3: Handle the key combination
        YourHandlerMethod();
        
        // Step 4: Mark as handled to prevent default behavior
        e.Handled = true;
    }
};
```

### Example: Alt+S to Save

```csharp
textBox.PreviewKeyDown += (s, e) =>
{
    var actualKey = e.Key == Key.System ? e.SystemKey : e.Key;
    
if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt && actualKey == Key.S)
    {
        SaveDocument();
        e.Handled = true;
    }
};
```

---

## Related WPF Documentation

- [KeyEventArgs.Key Property](https://docs.microsoft.com/en-us/dotnet/api/system.windows.input.keyeventargs.key)
- [KeyEventArgs.SystemKey Property](https://docs.microsoft.com/en-us/dotnet/api/system.windows.input.keyeventargs.systemkey)
- [ModifierKeys Enumeration](https://docs.microsoft.com/en-us/dotnet/api/system.windows.input.modifierkeys)

---

## Summary

**Problem**: Alt+Arrow navigation didn't work  
**Root Cause**: `e.Key` returns `Key.System` when Alt is pressed, actual key is in `e.SystemKey`  
**Solution**: Check `e.SystemKey` when `e.Key == Key.System`  
**Status**: ? Fixed and tested

This is a common WPF gotcha that affects all Alt+Key combinations, not just arrow keys.
