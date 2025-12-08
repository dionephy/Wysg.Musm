# Fix: Editor Autofocus Single Key Consistency

**Date:** 2025-12-08  
**Status:** Completed  
**Category:** Bug Fix

## Problem

When focused at PACS viewer and pressing a key (e.g., "a") that triggers editor autofocus:
- Sometimes only "a" was inserted
- Sometimes "aa" was inserted

The behavior was inconsistent due to a race condition in the keyboard hook.

## Root Cause

The original implementation in `EditorAutofocusService.HookCallback` had a race condition:

1. Caught the keypress in a low-level keyboard hook
2. Used `Dispatcher.Invoke` (blocking) to focus editor and send key
3. Returned `(IntPtr)1` to consume the original key

The issue: `Dispatcher.Invoke` blocks the hook callback, but the OS keyboard hook has a timeout. If the invoke takes too long, the original key may slip through before being consumed.

## Solution

Changed the implementation to:

1. Use `Dispatcher.BeginInvoke` (non-blocking) instead of `Invoke`
2. Add a small delay (10ms) after focus transfer to ensure it completes
3. Consume the original key immediately by returning `(IntPtr)1`

```csharp
// Non-blocking dispatch - allows immediate return to consume key
System.Windows.Application.Current?.Dispatcher.BeginInvoke(
    System.Windows.Threading.DispatcherPriority.Send,
    new Action(() =>
    {
        _focusEditorCallback();
        System.Threading.Thread.Sleep(10); // Ensure focus completes
        System.Windows.Forms.SendKeys.SendWait(keyString);
    }));

// Immediately consume original key
return (IntPtr)1;
```

This ensures:
1. Original key is consumed before it can reach any window
2. Focus transfer happens asynchronously
3. Sent key arrives at the now-focused editor
4. Result: Exactly one character ("a") is inserted consistently

## Files Changed

- `apps/Wysg.Musm.Radium/Services/EditorAutofocusService.cs`
  - Modified `HookCallback` method to use non-blocking dispatch and immediate key consumption

## Testing

1. Configure editor autofocus with:
   - Target bookmark (e.g., PACS viewer element)
   - Window title filter
   - Key types: Alphabet
2. Focus on PACS viewer
3. Press "a" key multiple times
4. Verify exactly one "a" is inserted each time (not "aa" or missed keys)

## Impact

- **Autofocus behavior:** Consistently inserts single character when triggering from external app
- **Normal typing:** Unaffected (autofocus only triggers when PACS has focus)
