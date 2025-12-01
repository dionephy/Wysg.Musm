# FIX: Add Procedure Button Not Creating Procedure Entry (2025-12-01)

## Issue
When pressing "Add procedure" button in Automation window ¡æ Procedure tab, after entering a procedure name and pressing "OK", the new procedure did not appear in the Custom Procedure combobox.

## Root Cause
The `OnAddPacsMethod` method in `AutomationWindow.PacsMethods.cs` was not actually creating an empty procedure entry in the `ui-procedures.json` file. It only called `LoadPacsMethods()` which tried to load procedures from the file, but since the new procedure wasn't saved yet, it wouldn't appear.

The procedure would only be created later when the user added operations and clicked "Save" button.

## Solution
Added a new method `CreateEmptyProcedure` in `ProcedureExecutor.Storage.cs` that immediately creates an empty procedure entry in the JSON file with no operations. Updated `OnAddPacsMethod` to call this method right after the user confirms the procedure name.

## Changes Made

### 1. `apps/Wysg.Musm.Radium/Services/ProcedureExecutor.Storage.cs`
Added new public method:
```csharp
/// <summary>
/// Create a new empty procedure
/// </summary>
public static bool CreateEmptyProcedure(string procedureName)
{
    if (string.IsNullOrWhiteSpace(procedureName))
        return false;

    var store = Load();
    
    // Check if procedure already exists
    if (store.Methods.ContainsKey(procedureName))
        return false;

    // Create empty procedure with no operations
    store.Methods[procedureName] = new System.Collections.Generic.List<ProcOpRow>();
    
    Save(store);
    return true;
}
```

### 2. `apps/Wysg.Musm.Radium/Views/AutomationWindow.PacsMethods.cs`
Updated `OnAddPacsMethod` to create the empty procedure immediately:
```csharp
// Create empty procedure immediately so it appears in the list
if (!ProcedureExecutor.CreateEmptyProcedure(procedureName))
{
    MessageBox.Show($"Failed to create procedure '{procedureName}'", "Error", 
        MessageBoxButton.OK, MessageBoxImage.Error);
    return;
}

// Reload the procedure list to show the new procedure
LoadPacsMethods();
```

## Expected Behavior After Fix
1. User clicks "Add procedure" button
2. Dialog appears for entering procedure name
3. User enters valid name and clicks OK
4. **NEW**: Empty procedure is immediately saved to `ui-procedures.json`
5. Procedure list is reloaded from file
6. New procedure appears in Custom Procedure combobox
7. New procedure is automatically selected
8. User can now add operations to the procedure

## Storage Format
The new empty procedure is stored in `ui-procedures.json` as:
```json
{
  "Methods": {
    "My New Procedure": []
  }
}
```

## Testing Checklist
- [x] Build succeeds with no errors
- [ ] Open Automation Window ¡æ Procedure tab
- [ ] Click "Add procedure" button
- [ ] Enter a new procedure name (e.g., "Test Procedure")
- [ ] Click OK
- [ ] Verify procedure appears in Custom Procedure combobox
- [ ] Verify procedure is selected automatically
- [ ] Verify can add operations to the new procedure
- [ ] Click Save
- [ ] Verify procedure persists after closing and reopening window

## Related Files
- `apps/Wysg.Musm.Radium/Services/ProcedureExecutor.Storage.cs` (modified)
- `apps/Wysg.Musm.Radium/Views/AutomationWindow.PacsMethods.cs` (modified)
- `apps/Wysg.Musm.Radium/Services/ProcedureExecutor.Models.cs` (reference)

## Notes
- Empty procedures can exist in the file (with zero operations)
- Users can still edit/rename/delete procedures using existing buttons
- The fix ensures consistent behavior: procedure creation is atomic (name dialog ¡æ immediate save)
- No breaking changes to existing functionality

---

**Status**: COMPLETED  
**Date**: 2025-12-01  
**Type**: Bug Fix  
**Priority**: High (UX Issue)  
**Build Status**: ? Success
