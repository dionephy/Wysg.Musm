# Fix: Remove All Built-in Procedure Concept (2025-11-27)

**Date**: 2025-11-27  
**Type**: Bug Fix  
**Status**: ? Complete  
**Priority**: High

---

## Summary

Removed the entire "built-in procedure" concept from PacsMethodManager and AutomationWindow. **All procedures are now fully custom/dynamic** and can be edited, renamed, and deleted without restrictions.

## Problem

Users were still seeing "Cannot rename built-in procedure tags" errors when trying to edit procedures, even though all procedures should be custom and dynamic. The issue was:

1. **PacsMethodManager** was auto-seeding "built-in" methods on first load
2. **IsBuiltIn flag** was blocking rename operations
3. **IsBuiltIn flag** was blocking delete operations
4. The entire concept of "built-in" procedures was still present in the code

## Root Cause

The `PacsMethodManager.GetAllMethods()` method was checking if the storage was empty and auto-seeding with a hardcoded list of "built-in" methods:

```csharp
// OLD CODE (BUGGY)
public List<PacsMethod> GetAllMethods()
{
    var store = Load();
    
    // Auto-seed with built-in methods if empty
    if (store.Methods.Count == 0)
    {
        store.Methods = GetBuiltInMethods(); // ? Creates 40+ "built-in" methods
        Save(store);
    }

    return store.Methods;
}
```

This meant:
- First time opening AutomationWindow ¡æ 40+ procedures marked as `IsBuiltIn = true`
- Users couldn't rename or delete these procedures
- The entire premise of "custom procedures" was violated

## Solution

### Changes Made

#### 1. PacsMethodManager.cs

**Removed auto-seeding**:
```csharp
// NEW CODE (FIXED)
public List<PacsMethod> GetAllMethods()
{
    var store = Load();
    return store.Methods; // ? Just return what's stored, no auto-seeding
}
```

**Removed IsBuiltIn check from DeleteMethod**:
```csharp
// OLD CODE (BUGGY)
if (method.IsBuiltIn)
    throw new InvalidOperationException("Cannot delete built-in PACS methods");

// NEW CODE (FIXED)
// All methods are custom/dynamic and can be deleted
store.Methods.Remove(method);
```

**Removed IsBuiltIn assignment from UpdateMethod**:
```csharp
// OLD CODE
existing.IsBuiltIn = method.IsBuiltIn;

// NEW CODE (FIXED)
// IsBuiltIn flag removed - all methods are custom/dynamic
```

**Removed entire GetBuiltInMethods() method**:
```csharp
// DELETED: private static List<PacsMethod> GetBuiltInMethods() { ... }
```

**Removed ResetToBuiltIn() method**:
```csharp
// DELETED: public void ResetToBuiltIn() { ... }
```

#### 2. AutomationWindow.PacsMethods.cs

**Removed IsBuiltIn check from OnEditPacsMethod**:
```csharp
// OLD CODE (BUGGY)
if (selectedMethod.IsBuiltIn)
{
    txtStatus.Text = "Cannot edit built-in custom procedures";
    return; // ? Blocked editing
}

// Check if renaming
if (selectedMethod.IsBuiltIn && !string.Equals(oldTag, tag, ...))
{
    MessageBox.Show("Cannot rename built-in procedure tags...", ...);
    return; // ? Blocked renaming
}

// NEW CODE (FIXED)
// All procedures are custom/dynamic and fully editable (including tag rename)
var dialog = CreatePacsMethodDialog("Edit Custom Procedure", selectedMethod);
// ? No IsBuiltIn checks at all
```

**Removed IsBuiltIn check from OnDeletePacsMethod**:
```csharp
// OLD CODE (BUGGY)
if (selectedMethod.IsBuiltIn)
{
    txtStatus.Text = "Cannot delete built-in custom procedures";
    return; // ? Blocked deletion
}

// NEW CODE (FIXED)
// All methods are custom/dynamic and can be deleted
var result = MessageBox.Show(...);
// ? No IsBuiltIn check
```

## Impact

### Before (Buggy)
- ? 40+ procedures automatically created as "built-in"
- ? Users couldn't rename these procedures
- ? Users couldn't delete these procedures
- ? Error: "Cannot rename built-in procedure tags"
- ? Error: "Cannot delete built-in PACS methods"

### After (Fixed)
- ? **NO** procedures are auto-created
- ? Users start with empty procedure list
- ? Users can create any procedures they want
- ? Users can **rename** any procedure
- ? Users can **delete** any procedure
- ? Users can **edit operations** for any procedure
- ? No restrictions whatsoever

## Migration Path

### For Existing Users

If users already have procedures marked as `IsBuiltIn = true` in their `pacs-methods.json` file:

**Option 1: Manual Edit** (Recommended)
1. Close Radium
2. Navigate to: `%AppData%\Wysg.Musm\Radium\Pacs\{pacsKey}\pacs-methods.json`
3. Edit the file and set `"IsBuiltIn": false` for all methods (or just delete the file)
4. Restart Radium

**Option 2: Delete and Recreate**
1. Close Radium
2. Delete: `%AppData%\Wysg.Musm\Radium\Pacs\{pacsKey}\pacs-methods.json`
3. Restart Radium
4. Recreate procedures as needed

**Option 3: Wait for Auto-Fix** (Future Enhancement)
- We could add migration code to automatically set `IsBuiltIn = false` for all methods on load

## Testing

### Test Case 1: Empty Start
**Steps**:
1. Delete `pacs-methods.json`
2. Open AutomationWindow ¡æ Custom Procedures

**Expected**:
- ? Procedure dropdown is empty
- ? No auto-created procedures
- ? "Add procedure" button works
- ? Can create any procedure

**Result**: ? Pass

### Test Case 2: Edit Procedure Name
**Steps**:
1. Create procedure "MyProcedure"
2. Click "Edit procedure"
3. Change name to "RenamedProcedure"
4. Click OK

**Expected**:
- ? No error message
- ? Procedure renamed successfully
- ? Dropdown shows new name

**Result**: ? Pass

### Test Case 3: Delete Procedure
**Steps**:
1. Create procedure "TestProcedure"
2. Click "Delete procedure"
3. Confirm deletion

**Expected**:
- ? No error message
- ? Procedure deleted successfully
- ? Removed from dropdown

**Result**: ? Pass

### Test Case 4: Edit Operations
**Steps**:
1. Select any procedure
2. Add/modify/remove operations in grid
3. Click "Save"

**Expected**:
- ? Operations saved successfully
- ? No restrictions

**Result**: ? Pass

## Files Modified

1. **apps/Wysg.Musm.Radium/Services/PacsMethodManager.cs**
   - Removed auto-seeding in `GetAllMethods()`
   - Removed IsBuiltIn check in `DeleteMethod()`
   - Removed IsBuiltIn assignment in `UpdateMethod()`
   - Deleted `GetBuiltInMethods()` method (~120 lines)
   - Deleted `ResetToBuiltIn()` method

2. **apps/Wysg.Musm.Radium/Views/AutomationWindow.PacsMethods.cs**
   - Removed IsBuiltIn checks in `OnEditPacsMethod()`
   - Removed IsBuiltIn checks in `OnDeletePacsMethod()`
   - Set `IsBuiltIn = false` when creating new PacsMethod instances

## Related Documentation

- **FIX_2025-11-27_AllProceduresAreEditable.md** - Initial fix that allowed editing operations
- **HARDCODED_PROCEDURES_MIGRATION.md** - Migration guide for Phase 2
- **CUSTOM_PROCEDURES_PHASE2_COMPLETE.md** - Phase 2 completion document

## Backward Compatibility

?? **Breaking Change** (Minor)

Users who rely on the auto-seeded procedures will need to:
1. Manually create the procedures they need, OR
2. Keep their existing `pacs-methods.json` file

This is acceptable because:
- The auto-seeded procedures were never actually functional (no operations defined)
- Users had to configure operations anyway
- The new approach is cleaner and more flexible

## Future Enhancements

### Potential Improvements

1. **Migration Tool**
   - Automatically set `IsBuiltIn = false` for all existing methods on load
   - Show migration dialog to users

2. **Procedure Templates**
   - Provide optional template library users can import
   - Keep templates separate from "built-in" concept

3. **Procedure Sharing**
   - Export/import procedure definitions
   - Share procedures between PACS profiles

4. **Procedure Validation**
   - Warn if deleting procedure used in automation sequences
   - Show which automations use each procedure

## Build Status

? **Build Successful** - No errors, no warnings

---

**Implementation Date**: 2025-11-27  
**Build Status**: ? Success  
**Breaking Change**: ?? Minor (auto-seeding removed)  
**Ready for Use**: ? Complete

---

## Summary

The entire "built-in procedure" concept has been removed. **All procedures are now fully custom/dynamic** and can be created, edited, renamed, and deleted without any restrictions. Users start with an empty procedure list and create only what they need.

This fix completes the transition to a truly dynamic custom procedure system where users have full control over their procedures.
