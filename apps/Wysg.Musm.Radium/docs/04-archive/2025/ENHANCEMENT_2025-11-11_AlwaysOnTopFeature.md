# Enhancement: Always on Top Feature

**Date**: 2025-11-11  
**Component**: MainWindow / StatusActionsBar  
**Type**: UI Enhancement  
**Status**: ? Implemented & Fixed

---

## Overview

Added a checkbox in the StatusActionsBar that allows users to toggle the main window "always on top" state. The setting is persisted across sessions using the existing `IRadiumLocalSettings` encrypted storage.

---

## Bug Fixes (2025-11-11)

### Issues Fixed

1. **Window Not Staying on Top**
   - **Problem**: Checking the "Always on Top" checkbox did not make the window stay on top
   - **Root Cause**: Event handlers (`OnAlwaysOnTopChecked` and `OnAlwaysOnTopUnchecked`) were missing from MainWindow.xaml.cs
   - **Solution**: Added the two event handlers that set `Window.Topmost` property and save to settings

2. **Setting Not Persisted**
   - **Problem**: Checkbox state was not saved, so it was unchecked when application restarted
   - **Root Cause**: Missing initialization code in `OnLoaded` method
   - **Solution**: Added initialization code that reads `AlwaysOnTop` from settings and sets both `Window.Topmost` and checkbox state

### Files Modified (Bug Fix)
- `apps\Wysg.Musm.Radium\Views\MainWindow.xaml.cs`
  - Added `OnAlwaysOnTopChecked` method (sets Topmost and saves setting)
  - Added `OnAlwaysOnTopUnchecked` method (clears Topmost and saves setting)
  - Added initialization in `OnLoaded` to restore state from settings

---

## User Story

**As a** radiologist using the Radium application  
**I want** the ability to keep the application window always on top of other windows  
**So that** I can easily reference report data while working with PACS or other applications side-by-side

---

## Components Modified

### 1. IRadiumLocalSettings Interface

**File**: `apps\Wysg.Musm.Radium\Services\IRadiumLocalSettings.cs`

**Changes**:
- Added `bool AlwaysOnTop { get; set; }` property

```csharp
// NEW: Always on Top setting
/// <summary>Keep the main window always on top of other windows.</summary>
bool AlwaysOnTop { get; set; }
```

### 2. RadiumLocalSettings Implementation

**File**: `apps\Wysg.Musm.Radium\Services\RadiumLocalSettings.cs`

**Changes**:
- Implemented `AlwaysOnTop` property using existing encrypted storage mechanism

```csharp
// NEW: Always on Top setting
public bool AlwaysOnTop { get => ReadBool("always_on_top"); set => WriteBool("always_on_top", value); }
```

**Storage**:
- Key: `"always_on_top"`
- Format: Boolean stored as "1" (true) or "0" (false)
- Encryption: DPAPI CurrentUser scope (same as other settings)
- Location: `%LocalAppData%\Wysg.Musm\Radium\settings.dat`

### 3. StatusActionsBar XAML

**File**: `apps\Wysg.Musm.Radium\Controls\StatusActionsBar.xaml`

**Changes**:
- Added checkbox control in the right-aligned StackPanel
- Positioned between PACS display and Log out button

```xaml
<!-- Always on Top Checkbox -->
<CheckBox x:Name="chkAlwaysOnTop" Content="Always on Top" 
          VerticalAlignment="Center" Margin="0,0,12,0"
          Foreground="#C8C8C8" FontSize="11"
          Checked="OnAlwaysOnTop_Checked" Unchecked="OnAlwaysOnTop_Unchecked"/>
```

**Styling**:
- Font Size: 11 (matches User/PACS display text)
- Foreground: `#C8C8C8` (light gray, consistent with status text)
- Margin: `0,0,12,0` (proper spacing before Log out button)

### 4. StatusActionsBar Code-Behind

**File**: `apps\Wysg.Musm.Radium\Controls\StatusActionsBar.xaml.cs`

**Changes**:
- Added `using Microsoft.Extensions.DependencyInjection;` for GetService extension
- Added `InitializeAlwaysOnTopCheckbox()` method called in `OnLoaded`
- Added event handler delegates to bubble events to MainWindow

```csharp
private void InitializeAlwaysOnTopCheckbox()
{
    try
    {
        var app = (App)Application.Current;
        var local = app.Services.GetService<Services.IRadiumLocalSettings>();
        if (local != null)
        {
            chkAlwaysOnTop.IsChecked = local.AlwaysOnTop;
        }
    }
    catch
    {
        // Silently fail - checkbox will remain unchecked
    }
}
```

###  5. MainWindow Code-Behind

**File**: `apps\Wysg.Musm.Radium\Views\MainWindow.xaml.cs`

**Changes**:
- Added initialization logic in `OnLoaded` to restore "Always on Top" state from settings
- Added two event handlers: `OnAlwaysOnTopChecked` and `OnAlwaysOnTopUnchecked`

**Initialization** (in `OnLoaded`):
```csharp
// NEW: Initialize Always on Top from settings
try
{
    var app = (App)Application.Current;
    var localSettings = app.Services.GetService<IRadiumLocalSettings>();
    if (localSettings != null && localSettings.AlwaysOnTop)
    {
        this.Topmost = true;
        System.Diagnostics.Debug.WriteLine("[MainWindow] Always on Top enabled from settings");
    }
}
catch (Exception ex)
{
    System.Diagnostics.Debug.WriteLine($"[MainWindow] Failed to initialize Always on Top: {ex.Message}");
}
```

**Event Handlers**:
```csharp
private void OnAlwaysOnTopChecked(object sender, RoutedEventArgs e)
{
    try
    {
        this.Topmost = true;
        
        // Save setting
        var app = (App)Application.Current;
        var localSettings = app.Services.GetService<IRadiumLocalSettings>();
        if (localSettings != null)
        {
            localSettings.AlwaysOnTop = true;
        }
        
        System.Diagnostics.Debug.WriteLine("[MainWindow] Always on Top enabled");
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[MainWindow] Failed to enable Always on Top: {ex.Message}");
    }
}

private void OnAlwaysOnTopUnchecked(object sender, RoutedEventArgs e)
{
    try
    {
        this.Topmost = false;
        
        // Save setting
        var app = (App)Application.Current;
        var localSettings = app.Services.GetService<IRadiumLocalSettings>();
        if (localSettings != null)
        {
            localSettings.AlwaysOnTop = false;
        }
        
        System.Diagnostics.Debug.WriteLine("[MainWindow] Always on Top disabled");
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[MainWindow] Failed to disable Always on Top: {ex.Message}");
    }
}
```

---

## Technical Design

### Architecture Pattern

**Event Bubbling Pattern**:
1. User clicks checkbox in `StatusActionsBar`
2. `StatusActionsBar` raises event to parent `MainWindow` using reflection
3. `MainWindow` handles event and updates window state + persists setting

This follows the existing pattern used for other StatusActionsBar buttons (Settings, Spy, Logout, etc.).

### State Management Flow

```
[Checkbox Click] 
    ��
[StatusActionsBar Event Handler]
    ��
[RaiseEventToWindow("OnAlwaysOnTopChecked/Unchecked")]
    ��
[MainWindow Event Handler]
    ��
[Update Window.Topmost Property]
    ��
[Persist to IRadiumLocalSettings]
    ��
[Encrypt and Save to disk]
```

### Initialization Flow

```
[Application Startup]
    ��
[MainWindow.OnLoaded]
    ��
[Load IRadiumLocalSettings.AlwaysOnTop]
    ��
[If true, set Window.Topmost = true]
    ��
[StatusActionsBar.OnLoaded]
    ��
[InitializeAlwaysOnTopCheckbox]
    ��
[Set checkbox.IsChecked from settings]
```

---

## Behavior

### User Experience

1. **Initial State**: Checkbox reflects saved setting from previous session
2. **Click Behavior**: Immediate toggle of window's always-on-top state
3. **Persistence**: Setting automatically saved on every change
4. **Session Restore**: Setting restored when application reopens

### Window Behavior

- **When Checked**: Window stays on top of all other windows (even when not focused)
- **When Unchecked**: Window follows normal Z-order behavior
- **Compatibility**: Works with Windows 10/11 window management

---

## Testing Checklist

- [x] Build succeeds without errors
- [x] Checkbox appears in StatusActionsBar (right side, before Log out button)
- [x] Clicking checkbox toggles window always-on-top state immediately
- [x] Setting persists across application restarts
- [x] Setting initializes correctly on startup
- [ ] Works with multiple monitors
- [ ] No conflicts with other window management features (minimize, maximize, restore)
- [x] Debug logging works correctly

---

## Edge Cases Handled

1. **Settings Load Failure**: If settings can't be loaded, checkbox defaults to unchecked (fail-safe)
2. **Service Not Available**: Try-catch blocks prevent crashes if DI services unavailable
3. **Race Conditions**: StatusActionsBar initializes after MainWindow, ensuring proper state sync

---

## Known Issues (Resolved)

### ~~Issue 1: Window Not Staying on Top~~ ? FIXED
- **Status**: Fixed on 2025-11-11
- **Symptom**: Checking the checkbox didn't make window stay on top
- **Cause**: Missing event handlers in MainWindow.xaml.cs
- **Fix**: Added `OnAlwaysOnTopChecked` and `OnAlwaysOnTopUnchecked` methods

### ~~Issue 2: Setting Not Persisted~~ ? FIXED
- **Status**: Fixed on 2025-11-11
- **Symptom**: Checkbox unchecked on restart even after checking it
- **Cause**: Missing initialization code in `OnLoaded`
- **Fix**: Added initialization that reads from settings and sets both window and checkbox state

---

## Future Enhancements

- [ ] Add keyboard shortcut for toggling always-on-top (e.g., Ctrl+T)
- [ ] Add visual indication when window is in always-on-top mode (title bar highlight?)
- [ ] Consider per-PACS profile setting (currently global per-user)

---

## Related Code Patterns

This feature follows existing patterns established in:
- **Editor Autofocus** (`EditorAutofocusEnabled` setting)
- **Window Placement** (`MainWindowPlacement` setting)
- **Auto Toggles** (`AutoChiefComplaint`, `AutoConclusion`, etc.)

---

## Documentation Updates

- [x] This enhancement document created
- [x] Bug fixes documented
- [ ] Update user manual with Always on Top feature
- [ ] Update settings reference documentation
- [ ] Add to release notes

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-11-11 | Initial implementation |
| 1.1 | 2025-11-11 | Bug fix: Added missing event handlers and initialization |

---

## References

- **Similar Feature**: LlmDataBuilder project already has Always on Top checkbox (see `apps\Wysg.Musm.LlmDataBuilder\MainWindow.xaml.cs`)
- **Windows API**: Uses WPF `Window.Topmost` property (built on Win32 `SetWindowPos` with `HWND_TOPMOST`)
