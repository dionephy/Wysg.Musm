# Fix: All Procedures Are Editable (2025-11-27)

**Date**: 2025-11-27  
**Type**: Bug Fix  
**Status**: ? Complete  
**Priority**: High

---

## Summary

Fixed incorrect `IsBuiltIn` check that prevented users from editing procedures for built-in methods like "GetSelectedIdFromSearchResults". All procedures (operation steps) are user-created and editable.

## Problem

Users reported that some custom procedures were not editable, with an error message "Cannot edit built-in custom procedures". This was caused by confusion between:

1. **PacsMethod** (dropdown entries) - Method names like "GetSelectedIdFromSearchResults" with `IsBuiltIn = true`
2. **Procedures** (operation steps in `ui-procedures.json`) - The actual automation steps which are ALL user-created

## Root Cause

The `OnEditPacsMethod` method in `AutomationWindow.PacsMethods.cs` was checking `selectedMethod.IsBuiltIn` and blocking editing. However:

- **IsBuiltIn applies to METHOD NAMES** in the dropdown (can't delete/rename built-in names)
- **Procedures (operations) are ALWAYS user-created** and should be editable

### Before (Buggy Code)
```csharp
if (selectedMethod.IsBuiltIn)
{
    txtStatus.Text = "Cannot edit built-in custom procedures";
    return; // ? Blocked editing of ALL procedures for built-in methods
}
```

### After (Fixed Code)
```csharp
// NOTE: IsBuiltIn only applies to the PacsMethod dropdown entries (method names).
// The actual procedures (operation steps) are ALL user-created and editable.
// Users can edit the procedures for built-in methods (like GetSelectedIdFromSearchResults).
// They just can't rename or delete the method name itself from the dropdown.

// REMOVED: IsBuiltIn check - procedures are always editable

// Later in the method: Prevent renaming the method tag for built-in entries
if (selectedMethod.IsBuiltIn && !string.Equals(selectedMethod.Tag, tag, StringComparison.OrdinalIgnoreCase))
{
    MessageBox.Show("Cannot rename built-in procedure tags.\n\nYou can edit the operation steps, but the procedure name must remain the same.", 
        "Cannot Rename", MessageBoxButton.OK, MessageBoxImage.Warning);
    return;
}
```

## What Changed

### File Modified: `apps/Wysg.Musm.Radium/Views/AutomationWindow.PacsMethods.cs`

**Changes**:
1. ? **Removed** early IsBuiltIn check that blocked editing
2. ? **Added** clarifying comments explaining PacsMethod vs Procedures
3. ? **Added** tag rename protection for built-in methods (you can edit operations but not rename the method)
4. ? **Preserved** IsBuiltIn flag when updating to maintain dropdown integrity

## Architecture Clarification

### PacsMethod (Dropdown Names) - `pacs-methods.json`
```json
{
  "Methods": [
    {
      "Name": "Get selected ID from search results list",
      "Tag": "GetSelectedIdFromSearchResults",
      "IsBuiltIn": true,  // ¡ç This means: can't delete/rename this dropdown entry
      "Description": ""
    }
  ]
}
```

**Purpose**: Defines what appears in the procedure dropdown list
**IsBuiltIn**: If true, prevents deletion/renaming of the dropdown entry
**Editability**: Display name can be changed, but not the Tag

### Procedures (Operation Steps) - `ui-procedures.json`
```json
{
  "Methods": {
    "GetSelectedIdFromSearchResults": [  // ¡ç Method tag (matches PacsMethod.Tag)
      {
        "Op": "GetValueFromSelection",
        "Arg1": { "Type": "Element", "Value": "SearchResultsList" },
        "Arg2": { "Type": "String", "Value": "ID" },
        // ...operation details...
      }
    ]
  }
}
```

**Purpose**: Stores the actual automation steps for each procedure
**IsBuiltIn**: **Does NOT exist** - all procedures are user-created
**Editability**: **ALWAYS editable** - users define all operations

## User Impact

### Before (Bug)
- ? Users could NOT edit procedures for built-in method names
- ? Error message: "Cannot edit built-in custom procedures"
- ? No way to create/modify operations for methods like GetSelectedIdFromSearchResults

### After (Fixed)
- ? Users CAN edit ALL procedures (operations are always user-defined)
- ? Users CAN add/remove/modify operations for any method
- ? Users CANNOT rename built-in method tags (dropdown integrity protected)
- ? Users CANNOT delete built-in method names from dropdown

## What Users Can Do Now

### ? Allowed Operations

1. **Edit procedure operations** for ANY method (including built-in like GetSelectedIdFromSearchResults)
   - Add/remove/modify operation steps
   - Change arguments, bookmarks, variables
   - Test and save changes

2. **Create new procedures** for built-in method names
   - Select "GetSelectedIdFromSearchResults" from dropdown
   - Add custom operations
   - Save procedure

3. **Edit display names** for built-in methods
   - Change "Get selected ID from search results list" to anything
   - Tag "GetSelectedIdFromSearchResults" remains unchanged

### ? Protected Operations

1. **Cannot rename** built-in method tags
   - Tag "GetSelectedIdFromSearchResults" is fixed
   - Ensures automation sequences don't break

2. **Cannot delete** built-in method names from dropdown
   - Dropdown always contains core methods
   - Protects system integrity

## Testing Checklist

- [x] Open AutomationWindow ¡æ Custom Procedures tab
- [x] Select built-in method (e.g., "GetSelectedIdFromSearchResults")
- [x] Click "Edit procedure" button
- [x] Verify operations grid is editable
- [x] Add/modify operations
- [x] Save procedure
- [x] Verify procedure loads correctly after save
- [x] Try renaming method tag (should show warning)
- [x] Try deleting built-in method (should show error)
- [x] Build successful

## Related Files

- `apps/Wysg.Musm.Radium/Views/AutomationWindow.PacsMethods.cs` - Fixed
- `apps/Wysg.Musm.Radium/Services/PacsMethodManager.cs` - No changes (correct behavior)
- `apps/Wysg.Musm.Radium/Services/ProcedureExecutor.cs` - No changes (correct behavior)

## Documentation Updates

Created this document to clarify:
1. Difference between PacsMethod and Procedures
2. IsBuiltIn flag scope and purpose
3. What operations are protected vs editable
4. User capabilities and limitations

---

**Status**: ? COMPLETE  
**Build**: ? SUCCESS  
**Impact**: HIGH (unblocks all procedure editing)  
**Backward Compatibility**: ? FULL

---

*Issue Fixed: 2025-11-27*  
*Root Cause: Incorrect IsBuiltIn check scope*  
*Solution: Remove early check, add tag rename protection*

