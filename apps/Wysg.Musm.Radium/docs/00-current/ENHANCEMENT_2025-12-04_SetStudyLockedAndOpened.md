# Enhancement: Rename LockStudy to SetStudyLocked and Add SetStudyOpened Module (2025-12-04)

**Date**: 2025-12-04  
**Type**: Enhancement  
**Status**: ? Complete  
**Priority**: Normal

---

## Summary

Renamed the "LockStudy" built-in module to "SetStudyLocked" for consistency with the naming convention of other toggle modules, and added a new "SetStudyOpened" built-in module that toggles ON the Study Opened toggle.

## Changes

### 1. Renamed Module: LockStudy ¡æ SetStudyLocked

| Before | After |
|--------|-------|
| LockStudy | SetStudyLocked |

**Behavior**: Sets `PatientLocked = true` (locks the study, preventing edits)

**Legacy Support**: The old "LockStudy" name continues to work as a legacy alias for backward compatibility with existing automation sequences.

### 2. New Module: SetStudyOpened

**Behavior**: Sets `StudyOpened = true` (toggles ON the Study Opened toggle)

**Use Case**: Can be used in automation sequences to mark a study as opened, enabling features that depend on the StudyOpened state.

## Implementation Details

### Files Created

1. **`ISetStudyOpenedProcedure.cs`**
   ```csharp
   public interface ISetStudyOpenedProcedure
   {
       Task ExecuteAsync(MainViewModel vm);
   }
   ```

2. **`SetStudyOpenedProcedure.cs`**
   ```csharp
   public sealed class SetStudyOpenedProcedure : ISetStudyOpenedProcedure
   {
       public Task ExecuteAsync(MainViewModel vm)
       {
           if (vm == null) return Task.CompletedTask;
           vm.StudyOpened = true;
           vm.SetStatusInternal("[SetStudyOpened] Done.");
           return Task.CompletedTask;
       }
   }
   ```

### Files Modified

1. **`ILockStudyProcedure.cs`**
   - Renamed interface from `ILockStudyProcedure` to `ISetStudyLockedProcedure`

2. **`LockStudyProcedure.cs`**
   - Renamed class from `LockStudyProcedure` to `SetStudyLockedProcedure`
   - Updated status message to `"[SetStudyLocked] Done."`

3. **`App.xaml.cs`**
   - Updated DI registration from `ILockStudyProcedure` to `ISetStudyLockedProcedure`
   - Added DI registration for `ISetStudyOpenedProcedure`
   - Updated `MainViewModel` registration to inject both procedures

4. **`MainViewModel.cs`**
   - Renamed field `_lockStudyProc` to `_setStudyLockedProc`
   - Added field `_setStudyOpenedProc`
   - Updated constructor parameters and assignments

5. **`MainViewModel.Commands.Automation.Core.cs`**
   - Added handler for `SetStudyLocked` module
   - Added handler for `SetStudyOpened` module
   - Kept `LockStudy` as legacy alias pointing to `SetStudyLocked`

6. **`SettingsViewModel.cs`**
   - Updated `AvailableModules` list:
     - Replaced "LockStudy" with "SetStudyLocked"
     - Added "SetStudyOpened"

## Module Summary

| Module Name | Action | Status Message |
|-------------|--------|----------------|
| SetStudyLocked | `PatientLocked = true` | `[SetStudyLocked] Done.` |
| SetStudyOpened | `StudyOpened = true` | `[SetStudyOpened] Done.` |
| LockStudy (legacy) | Same as SetStudyLocked | `[SetStudyLocked] Done.` |

## Usage Examples

### Example 1: Open and Lock Study Sequence
```
SetStudyOpened, SetStudyLocked
```
Effect: Marks study as opened, then locks it.

### Example 2: New Study Workflow
```
ClearCurrentFields, SetStudyOpened, SetStudyLocked
```
Effect: Clears fields, opens study, then locks it.

### Example 3: Backward Compatibility
```
LockStudy
```
Effect: Still works - internally calls SetStudyLocked.

## Related Modules

- **UnlockStudy**: Sets `PatientLocked = false` and `StudyOpened = false`
- **SetCurrentTogglesOff**: Sets `ProofreadMode = false` and `Reportified = false`

## Testing Checklist

- [x] SetStudyLocked module sets PatientLocked = true
- [x] SetStudyOpened module sets StudyOpened = true
- [x] LockStudy (legacy) still works as alias
- [x] Both modules appear in Settings ¡æ Automation ¡æ Available Modules
- [x] Build succeeds with no errors

## Backward Compatibility

? **Full backward compatibility maintained**

Existing automation sequences using "LockStudy" will continue to work without changes. The legacy alias ensures no breaking changes.

---

**Build Status**: ? Success  
**Backward Compatible**: ? Yes  
**Ready for Use**: ? Complete
