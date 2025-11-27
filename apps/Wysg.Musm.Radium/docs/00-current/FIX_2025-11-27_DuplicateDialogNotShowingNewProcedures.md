# FIX: Duplicate Operations Dialog Not Showing Newly Created Procedures

**Date**: 2025-11-27  
**Status**: ? Fixed  
**Type**: Bug Fix

## Problem

When clicking "Duplicate to..." button in the Automation Window's Procedure tab, newly created custom procedures did not appear in the target procedure selection dialog.

## Root Cause

The `ShowProcedureSelectionDialog` method was incorrectly loading procedures from `LoadProcStore()` (which reads operation steps from `ui-procedures.json`) instead of from `PacsMethodManager` (which manages procedure metadata including all dynamic custom procedures).

### Incorrect Code
```csharp
// WRONG: Loads from ProcStore (operation steps storage)
var store = LoadProcStore();
var availableProcedures = store.Methods.Keys
    .Where(k => !string.Equals(k, sourceProcedure, StringComparison.OrdinalIgnoreCase))
    .OrderBy(k => k)
    .ToList();
```

## Solution

Changed `ShowProcedureSelectionDialog` to load procedures from `_pacsMethodManager.GetAllMethods()` which correctly includes all dynamic procedures (both built-in and user-defined).

### Fixed Code
```csharp
// CORRECT: Loads from PacsMethodManager (procedure metadata storage)
var allMethods = _pacsMethodManager.GetAllMethods();
var availableProcedures = allMethods
    .Where(m => !string.Equals(m.Tag, sourceProcedure, StringComparison.OrdinalIgnoreCase))
    .OrderBy(m => m.Name)
    .ToList();
```

## Key Changes

1. **Data Source**: Changed from `ProcStore` to `PacsMethodManager`
2. **Display**: Now shows `PacsMethod.Name` (user-friendly) instead of raw tags
3. **Return Value**: Returns `PacsMethod.Tag` for proper procedure identification
4. **ListBox Binding**: Added `DisplayMemberPath = "Name"` to show procedure names

## Understanding the Storage System

### Two Separate Storage Mechanisms

1. **PacsMethodManager** (`pacs-methods.json`)
   - Stores: Procedure metadata (Name, Tag, IsBuiltIn, Description)
   - Purpose: Procedure registry/directory
   - Location: `%AppData%\Wysg.Musm\Radium\Pacs\{pacsKey}\pacs-methods.json`
   - Example:
     ```json
     {
       "Methods": [
         {
           "Name": "Get Patient Info",
           "Tag": "GetPatientInfo",
           "IsBuiltIn": false,
           "Description": ""
         }
       ]
     }
     ```

2. **ProcStore** (`ui-procedures.json`)
   - Stores: Procedure operations/steps (Op, Arg1, Arg2, Arg3, etc.)
   - Purpose: Procedure implementation/logic
   - Location: `%AppData%\Wysg.Musm\Radium\Pacs\{pacsKey}\ui-procedures.json`
   - Example:
     ```json
     {
       "Methods": {
         "GetPatientInfo": [
           { "Op": "GetText", "Arg1": { "Type": "Element", "Value": "PatientName" } }
         ]
       }
     }
     ```

### Why Two Storages?

- **Metadata vs Implementation**: Separates "what procedures exist" from "what each procedure does"
- **Efficiency**: Can list all procedures without loading heavy operation data
- **Management**: Add/edit/delete procedures without touching their implementations
- **UI Binding**: ComboBoxes bind to metadata (Names), execution uses operations

## Impact

- ? Newly created custom procedures now immediately appear in duplicate dialog
- ? No need to restart application to see new procedures
- ? Consistent with how procedure dropdown works (already using PacsMethodManager)
- ? User-friendly names displayed instead of technical tags

## Testing

1. Create a new custom procedure via "Add procedure" button
2. Select an existing procedure with operations
3. Click "Duplicate to..." button
4. Verify new procedure appears in the target selection dialog
5. Select new procedure and confirm duplication works

## Files Modified

- `apps/Wysg.Musm.Radium/Views/AutomationWindow.Procedures.Exec.cs`
  - Fixed `ShowProcedureSelectionDialog` method

## Related Issues

**User Report**: "If i click 'Duplicate to...', in the list, there is no procedure that i recently created. does this mean the Custom Procedures are hard coded?"

**Answer**: No, Custom Procedures are **fully dynamic** and stored in local JSON files per PACS profile. The issue was a bug in the duplicate dialog that was loading from the wrong data source. This has been fixed.

## Verification

Custom Procedures are confirmed to be dynamic:
- ? Stored in `pacs-methods.json` (metadata) and `ui-procedures.json` (operations)
- ? Managed via `PacsMethodManager` class
- ? Can be added/edited/deleted at runtime
- ? Persisted per PACS profile (not hardcoded)
- ? `InitializePacsMethods()` called in AutomationWindow constructor
- ? `LoadPacsMethods()` refreshes the PacsMethods ObservableCollection
- ? Procedure dropdown correctly shows all dynamic procedures

---

**Fix Applied**: 2025-11-27  
**Build Status**: ? Success  
**User Impact**: High (critical fix for duplicate feature usability)
