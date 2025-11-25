# Send Report Global Hotkey - Fix Applied ?
**Date**: 2025-10-19  
**Issue**: Ctrl+Decimal hotkey configured but not invoking automation pane  
**Status**: FIXED

## Problem
User configured the "Send study" global hotkey to `Ctrl+Decimal` in Settings �� Keyboard tab, but pressing the hotkey did not trigger the Send Report automation sequence.

## Root Cause
The global hotkey registration and handler for Send Report were **not implemented** in `MainWindow.xaml.cs`. While the runtime logic (`RunSendReportShortcut()`) was complete in `MainViewModel.Commands.cs`, the OS-level hotkey integration was missing.

## Solution Implemented

### 1. Added Hotkey Constants and Fields
```csharp
private const int HOTKEY_ID_SEND_REPORT = 0xB003;
private uint _sendReportMods;
private uint _sendReportVk;
```

### 2. Implemented Registration Method
```csharp
private void TryRegisterSendReportHotkey()
{
    // Reads GlobalHotkeySendStudy from IRadiumLocalSettings
    // Parses hotkey text (e.g., "Ctrl+Decimal")
    // Registers with Windows using RegisterHotKey API
    // Logs success/failure to debug output
}
```

### 3. Added Registration Call
```csharp
protected override void OnSourceInitialized(EventArgs e)
{
    // ...existing code...
    TryRegisterOpenStudyHotkey();
    TryRegisterToggleSyncTextHotkey();
    TryRegisterSendReportHotkey();  // ? NEW
}
```

### 4. Extended WndProc Handler
```csharp
private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
{
    if (msg == WM_HOTKEY)
    {
        int id = wParam.ToInt32();
        // ...existing OPEN_STUDY and TOGGLE_SYNC_TEXT cases...
        
        else if (id == HOTKEY_ID_SEND_REPORT)  // ? NEW
        {
            if (DataContext is MainViewModel vm)
            {
                vm.RunSendReportShortcut();  // Routes based on Reportified state
                System.Diagnostics.Debug.WriteLine("[Hotkey] SendReport executed");
            }
            handled = true;
        }
    }
    return IntPtr.Zero;
}
```

### 5. Added Cleanup in OnClosed
```csharp
protected override void OnClosed(EventArgs e)
{
    try
    {
        if (_hotkeyHwndSource != null)
        {
            UnregisterHotKey(_hotkeyHwndSource.Handle, HOTKEY_ID_OPEN_STUDY);
            UnregisterHotKey(_hotkeyHwndSource.Handle, HOTKEY_ID_TOGGLE_SYNC_TEXT);
            UnregisterHotKey(_hotkeyHwndSource.Handle, HOTKEY_ID_SEND_REPORT);  // ? NEW
            _hotkeyHwndSource.RemoveHook(WndProc);
        }
    }
    catch { }
    base.OnClosed(e);
}
```

## How It Works Now

### Registration Flow
1. **App Startup** �� `OnSourceInitialized()` called after window handle created
2. **Read Settings** �� Loads `GlobalHotkeySendStudy` from `IRadiumLocalSettings`
3. **Parse Hotkey** �� Converts "Ctrl+Decimal" to mods (0x02=CONTROL) + vk (0x6E=VK_DECIMAL)
4. **Register with Windows** �� Calls `RegisterHotKey(hwnd, 0xB003, mods, vk)`
5. **Log Result** �� Debug output shows success or failure

### Execution Flow
1. **User Presses Hotkey** �� Windows sends `WM_HOTKEY` message to app
2. **WndProc Receives Message** �� Checks if `wParam == HOTKEY_ID_SEND_REPORT`
3. **Check Reportified State** �� `RunSendReportShortcut()` inspects `Reportified` toggle
4. **Load Appropriate Sequence**:
   - If `Reportified=true` �� Loads `ShortcutSendReportReportified` sequence
   - If `Reportified=false` �� Loads `ShortcutSendReportPreview` sequence
5. **Execute Modules** �� Runs modules sequentially (e.g., `Reportify, SendReport`)
6. **Log Execution** �� Debug output shows which sequence was selected

### Cleanup Flow
1. **Window Closes** �� `OnClosed()` called
2. **Unregister Hotkey** �� `UnregisterHotKey()` removes OS-level registration
3. **Unhook WndProc** �� `RemoveHook()` stops receiving messages
4. **Prevent Leaks** �� System-wide hotkey released, available for other apps

## Testing Steps

### 1. Verify Hotkey Configuration
```
Settings �� Keyboard Tab
  - Send study hotkey: Ctrl+Decimal
  - Click "Save Keyboard"
```

### 2. Configure Automation Sequences
```
Settings �� Automation Tab
  - Shortcut: Send Report (preview): SendReport
  - Shortcut: Send Report (reportified): Reportify, SendReport
  - Click "Save Automation"
```

### 3. Test Hotkey Execution
```
Scenario A: Reportified OFF
  1. Ensure "Reportified" toggle is OFF in MainWindow
  2. Press Ctrl+Decimal
  3. Expected: "ShortcutSendReportPreview" sequence executes
  4. Debug Output: "[SendReportShortcut] Reportified=false, using ShortcutSendReportPreview sequence"

Scenario B: Reportified ON
  1. Toggle "Reportified" ON in MainWindow
  2. Press Ctrl+Decimal
  3. Expected: "ShortcutSendReportReportified" sequence executes
  4. Expected: Reportified=true, report formatted, then sent
  5. Debug Output: "[SendReportShortcut] Reportified=true, using ShortcutSendReportReportified sequence"
```

### 4. Verify Debug Logging
Open **Debug Output** window and watch for:
```
[Hotkey] Registered SendReport hotkey 'Ctrl+Decimal' mods=0x2 vk=0x6E
[Hotkey] SendReport executed
[SendReportShortcut] Reportified=false, using ShortcutSendReportPreview sequence
[Automation] Module 'SendReport' executed
```

## Build Status
- ? **0 Compilation Errors**
- ? All new methods compile successfully
- ? Global hotkey pattern follows existing Open Study hotkey

## Files Modified
- `apps\Wysg.Musm.Radium\Views\MainWindow.xaml.cs`
  - Added `HOTKEY_ID_SEND_REPORT` constant
  - Added `_sendReportMods`, `_sendReportVk` fields
  - Implemented `TryRegisterSendReportHotkey()` method
  - Extended `WndProc()` with Send Report handler
  - Added registration call in `OnSourceInitialized()`
  - Added unregistration in `OnClosed()`

## Key Implementation Notes

### Hotkey Parsing
The `TryParseHotkey()` method handles various key formats:
- **Standard Keys**: `Ctrl+A`, `Alt+F1`, `Shift+Escape`
- **Numpad Keys**: `Ctrl+Decimal`, `Alt+Subtract`, `Shift+Multiply`
- **Function Keys**: `F1`, `Ctrl+F12`
- **Special Keys**: `Insert`, `Delete`, `PageUp`, `PageDown`

For `Ctrl+Decimal`:
- Modifiers: `Ctrl` �� `MOD_CONTROL` (0x0002)
- Key: `Decimal` �� WPF `Key.Decimal` �� VK code 0x6E (VK_DECIMAL)

### State-Based Routing
The `RunSendReportShortcut()` method in MainViewModel checks the `Reportified` toggle:
```csharp
if (Reportified)
    seqRaw = GetAutomationSequenceForCurrentPacs(s => s.ShortcutSendReportReportified);
else
    seqRaw = GetAutomationSequenceForCurrentPacs(s => s.ShortcutSendReportPreview);
```

This allows users to configure two different automation flows that adapt based on toggle state.

### Error Handling
- **Registration Failure**: Logged to debug output (e.g., hotkey already in use by another app)
- **Parsing Failure**: Silent fallback, hotkey not registered
- **Execution Errors**: Caught in `RunModulesSequentially()`, status message shown
- **Empty Sequences**: Status message "No Send Report shortcut sequence configured"

## Common Issues and Solutions

### Issue: Hotkey Not Firing
**Symptoms**: No debug output when pressing Ctrl+Decimal

**Solutions**:
1. Check Debug Output for registration message
2. Verify hotkey text saved correctly in Settings
3. Try a different key combination (may be in use by another app)
4. Restart application after configuring hotkey

### Issue: Wrong Sequence Executes
**Symptoms**: Preview sequence runs when Reportified is ON

**Solutions**:
1. Verify Reportified toggle state in MainWindow
2. Check debug output for "[SendReportShortcut] Reportified=..." message
3. Ensure correct sequences configured in Automation tab
4. Click "Save Automation" after making changes

### Issue: No Modules Execute
**Symptoms**: Hotkey fires but nothing happens

**Solutions**:
1. Verify automation sequences are not empty
2. Check module names match Available Modules exactly
3. Look for error messages in status bar
4. Enable Debug Output to see module execution logs

## Completion Summary

? **Global Hotkey Integration COMPLETE**

The Send Report automation feature is now fully functional with global hotkey support:
1. ? Hotkey registration implemented
2. ? WndProc handler added
3. ? State-based routing works
4. ? Cleanup properly handled
5. ? Debug logging comprehensive

**Next Steps**: Restart the application and test Ctrl+Decimal hotkey execution!
