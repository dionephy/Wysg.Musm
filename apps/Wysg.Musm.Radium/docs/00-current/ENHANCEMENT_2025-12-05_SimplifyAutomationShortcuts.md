# Enhancement: Simplify Automation Shortcut Panes (2025-12-05)

## Summary
Simplified the Automation tab in Settings by removing two conditional shortcut panes and marking several built-in modules as obsolete.

## Changes

### 1. Removed UI Panes
- **Removed**: "Shortcut: Open study (add)" pane
- **Removed**: "Shortcut: Open study (after open)" pane

### 2. Renamed Pane
- **Old**: "Shortcut: Open study (new)"
- **New**: "Shortcut: Open study"

### 3. Simplified Shortcut Logic
The `RunOpenStudyShortcut()` method in `MainViewModel.Commands.Handlers.cs` has been simplified:

**Before (conditional branching based on PatientLocked/StudyOpened):**
```csharp
public void RunOpenStudyShortcut()
{
    string seqRaw;
    string sequenceName;
    if (!PatientLocked) 
    {
        seqRaw = GetAutomationSequenceForCurrentPacs(static s => s.ShortcutOpenNew);
        sequenceName = "Shortcut: Open study (new)";
    }
    else if (!StudyOpened) 
    {
        seqRaw = GetAutomationSequenceForCurrentPacs(static s => s.ShortcutOpenAdd);
        sequenceName = "Shortcut: Open study (add)";
    }
    else 
    {
        seqRaw = GetAutomationSequenceForCurrentPacs(static s => s.ShortcutOpenAfterOpen);
        sequenceName = "Shortcut: Open study (after open)";
    }
    // ... execute modules
}
```

**After (always uses ShortcutOpenNew):**
```csharp
public void RunOpenStudyShortcut()
{
    var seqRaw = GetAutomationSequenceForCurrentPacs(static s => s.ShortcutOpenNew);
    var sequenceName = "Shortcut: Open study";
    // ... execute modules
}
```

The conditional logic (whether study is locked or opened) should now be defined within the session modules themselves using `If`/`If not`/`End if` control flow.

### 4. Obsolete Modules
The following built-in modules have been marked as obsolete with "(obs)" suffix:
- `SetStudyLocked` ¡æ `SetStudyLocked(obs)`
- `SetStudyOpened` ¡æ `SetStudyOpened(obs)`
- `UnlockStudy` ¡æ `UnlockStudy(obs)`
- `SetCurrentTogglesOff` ¡æ `SetCurrentTogglesOff(obs)`

## Files Modified

| File | Change |
|------|--------|
| `Views/SettingsTabs/AutomationSettingsTab.xaml` | Removed 2 panes, renamed 1 pane |
| `Views/SettingsTabs/AutomationSettingsTab.xaml.cs` | Updated ListBox references |
| `Views/SettingsWindow.xaml.cs` | Updated InitializeAutomationListBoxes |
| `Views/AutomationWindow.Automation.cs` | Removed bindings and validation for removed panes |
| `ViewModels/SettingsViewModel.cs` | Removed collections, updated AvailableModules |
| `ViewModels/SettingsViewModel.PacsProfiles.cs` | Updated Load/Save methods, AutomationSettings |
| `ViewModels/MainViewModel.Commands.Handlers.cs` | Simplified RunOpenStudyShortcut |

## Migration Notes

### For Users with Existing Configurations
If you previously used separate sequences for:
- "Shortcut: Open study (add)"
- "Shortcut: Open study (after open)"

You should migrate that logic into the single "Shortcut: Open study" pane using custom `If`/`If not` modules to check conditions like `PatientLocked` or `StudyOpened`.

### Example Migration
**Before (3 separate panes):**
- Open (new): `NewStudy, SetStudyLocked, OpenStudy`
- Open (add): `AddPreviousStudy, OpenStudy`
- Open (after open): `OpenStudy`

**After (single pane with control flow):**
```
If not StudyLocked
  NewStudy
  SetStudyLocked
End if
If StudyLocked
  If not StudyOpened
    AddPreviousStudy
  End if
End if
OpenStudy
```

## Rationale
1. **Simplification**: Three separate panes were confusing; users often didn't understand when each would trigger
2. **Flexibility**: Using If/End if modules gives users more control over the exact conditions
3. **Consistency**: Aligns with the control flow approach used elsewhere in automation
4. **Obsolete Modules**: The legacy `SetStudyLocked`, `SetStudyOpened`, `UnlockStudy`, and `SetCurrentTogglesOff` modules are being phased out in favor of more granular custom module approaches

## Build Verification
? Build succeeded with 0 errors
