# Dynamic PACS Methods Implementation Summary

**Date**: 2025-02-02  
**Feature**: Dynamic PACS Method Management  
**Status**: ? Complete  

## Overview

Implemented dynamic PACS method management system, replacing the previous hard-coded approach. Users can now add, edit, and delete PACS methods through the UI Spy window, with per-PACS profile storage.

## Changes Made

### 1. New Model Class: `PacsMethod.cs`
**Location**: `apps/Wysg.Musm.Radium/Models/PacsMethod.cs`

Represents a PACS method configuration:
- `Name` - Display name shown in UI
- `Tag` - Method identifier used in code and procedures
- `Description` - Optional description
- `IsBuiltIn` - Flag to prevent deletion of built-in methods

### 2. New Service: `PacsMethodManager.cs`
**Location**: `apps/Wysg.Musm.Radium/Services/PacsMethodManager.cs`

Manages PACS method CRUD operations:
- `GetAllMethods()` - Load all methods (built-in + custom)
- `AddMethod(method)` - Add new PACS method
- `UpdateMethod(oldTag, method)` - Update existing method
- `DeleteMethod(tag)` - Delete user-defined method
- `ResetToBuiltIn()` - Clear all custom methods

**Storage**: `%APPDATA%\Wysg.Musm\Radium\Pacs\{pacsKey}\pacs-methods.json`

### 3. New Partial File: `AutomationWindow.PacsMethods.cs`
**Location**: `apps/Wysg.Musm.Radium/Views/AutomationWindow.PacsMethods.cs`

UI logic for PACS method management:
- `InitializePacsMethods()` - Load methods for current PACS
- `LoadPacsMethods()` - Populate observable collection
- `OnAddPacsMethod()` - Add new method dialog
- `OnEditPacsMethod()` - Edit selected method dialog
- `OnDeletePacsMethod()` - Delete method with confirmation
- `CreatePacsMethodDialog()` - Create add/edit dialog

### 4. Updated Files

#### `AutomationWindow.xaml.cs`
- Added `InitializePacsMethods()` call in constructor
- Added `PacsMethods` observable collection property

#### `AutomationWindow.xaml`
- Changed ComboBox binding from static resource to dynamic collection
- Added three management buttons: "+ Method", "Edit Method", "Delete Method"
- Removed reference to `AutomationWindow.PacsMethodItems.xaml` resource

#### `AutomationWindow.Procedures.Exec.cs`
- Updated `OnProcMethodChanged()` to handle both legacy and new formats
- Updated `OnSaveProcedure()` to extract tag from new format
- Updated `OnRunProcedure()` to extract tag from new format

## Built-In PACS Methods

The system includes 43 built-in PACS methods (seeded on first load):

### Search Results List (10 methods)
- GetSelectedIdFromSearchResults
- GetSelectedNameFromSearchResults
- GetSelectedSexFromSearchResults
- GetSelectedBirthDateFromSearchResults
- GetSelectedAgeFromSearchResults
- GetSelectedStudynameFromSearchResults
- GetSelectedStudyDateTimeFromSearchResults
- GetSelectedRadiologistFromSearchResults
- GetSelectedStudyRemarkFromSearchResults
- GetSelectedReportDateTimeFromSearchResults

### Related Studies List (5 methods)
- GetSelectedIdFromRelatedStudies
- GetSelectedStudynameFromRelatedStudies
- GetSelectedStudyDateTimeFromRelatedStudies
- GetSelectedRadiologistFromRelatedStudies
- GetSelectedReportDateTimeFromRelatedStudies

### Current Study Data (9 methods)
- GetCurrentPatientNumber
- GetCurrentStudyDateTime
- GetCurrentStudyRemark
- GetCurrentPatientRemark
- GetCurrentFindings
- GetCurrentFindingsWait
- GetCurrentConclusion
- GetCurrentFindings2
- GetCurrentConclusion2

### Validation (2 methods)
- PatientNumberMatch
- StudyDateTimeMatch

### Actions (2 methods)
- InvokeOpenStudy
- InvokeTest

### Custom Mouse Clicks (2 methods)
- CustomMouseClick1
- CustomMouseClick2

### Screen Control (2 methods)
- SetCurrentStudyInMainScreen
- SetPreviousStudyInSubScreen

### Visibility Checks (2 methods)
- WorklistIsVisible
- ReportTextIsVisible

### UI Actions (3 methods)
- InvokeOpenWorklist
- SetFocusSearchResultsList
- SendReport

### Send Report Actions (2 methods)
- InvokeSendReport
- SendReportWithoutHeader

### Report Actions (1 method)
- ClearReport

## User Workflow

### Adding a New PACS Method
1. Open UI Spy window (Tools ¡æ UI Spy)
2. Navigate to "Custom Procedures" section
3. Click "+ Method" button
4. Enter Display Name (e.g., "Get Patient Address")
5. Enter Method Tag (e.g., "GetPatientAddress")
6. Click OK
7. Method appears in dropdown and is saved to current PACS profile

### Editing a PACS Method
1. Select method from "PACS Method" dropdown
2. Click "Edit Method" button
3. Modify Display Name and/or Method Tag
4. Click OK
5. Changes saved to current PACS profile

**Note**: Built-in methods cannot be edited (will show error message)

### Deleting a PACS Method
1. Select method from "PACS Method" dropdown
2. Click "Delete Method" button
3. Confirm deletion in dialog
4. Method and associated procedure steps removed

**Note**: Built-in methods cannot be deleted (will show error message)

### Configuring Procedure Steps for Custom Method
1. Select your custom method from dropdown
2. Click "Add" to add procedure steps
3. Configure operations (GetText, SetValue, etc.)
4. Click "Save" to persist procedure
5. Click "Run" to test execution

## Per-PACS Storage

PACS methods are stored per-profile in:
```
%APPDATA%\Wysg.Musm\Radium\Pacs\{pacsKey}\pacs-methods.json
```

Example for PACS key "infinitt_main":
```
C:\Users\{user}\AppData\Roaming\Wysg.Musm\Radium\Pacs\infinitt_main\pacs-methods.json
```

File format:
```json
{
  "Methods": [
    {
      "Name": "Get Patient Address",
      "Tag": "GetPatientAddress",
      "Description": "",
      "IsBuiltIn": false
    }
  ]
}
```

## Integration with Existing Systems

### PacsService Integration
Custom methods work seamlessly with existing PacsService methods:
- Built-in methods call existing `PacsService` async methods
- Custom methods execute via `ExecCustom()` pattern
- Same procedure execution logic applies to both

### Procedure Storage
Procedures remain in `ui-procedures.json` with method tag as key:
```json
{
  "Methods": {
    "GetPatientAddress": [
      {
        "Op": "GetText",
        "Arg1": { "Type": "Element", "Value": "AddressField" }
      }
    ]
  }
}
```

### Backward Compatibility
- Legacy static XAML resource still supported
- ComboBox binding handles both `ComboBoxItem` and `PacsMethod` types
- Existing procedures load correctly with new system

## Benefits

### For Users
? **Flexibility** - Add custom methods for specific PACS workflows  
? **Organization** - Better naming and categorization  
? **Per-PACS** - Different methods for different PACS systems  
? **No Code Changes** - All configuration through UI  

### For Developers
? **Extensibility** - Easy to add new built-in methods  
? **Maintainability** - Centralized method management  
? **Testability** - Methods defined in data, not code  
? **Clean Architecture** - Separation of concerns  

## Validation Rules

### Method Tag Validation
- Must start with a letter
- Can contain letters, numbers, and underscores
- Cannot be empty
- Must be unique within PACS profile

### Display Name Validation
- Cannot be empty
- Can contain any characters

### Deletion Rules
- Built-in methods cannot be deleted
- Custom methods can be deleted
- Deleting method also deletes associated procedures

## Error Handling

### Add Method Errors
- Duplicate tag ¡æ Show error message
- Invalid tag format ¡æ Show validation message
- Empty name/tag ¡æ Show validation message

### Edit Method Errors
- Editing built-in method ¡æ Show error message
- Invalid new tag ¡æ Show validation message
- Duplicate tag ¡æ Show error message

### Delete Method Errors
- Deleting built-in method ¡æ Show error message
- No method selected ¡æ Show status message

### Load Errors
- Missing file ¡æ Auto-seed with built-in methods
- Corrupt JSON ¡æ Return empty store
- All errors logged to Debug output

## Testing Checklist

? **Build Status** - Compilation successful  
? **Add Method** - New methods appear in dropdown  
? **Edit Method** - Changes persist across sessions  
? **Delete Method** - Methods removed from dropdown  
? **Built-in Protection** - Cannot edit/delete built-in methods  
? **Per-PACS Storage** - Different methods per profile  
? **Procedure Integration** - Procedures save/load correctly  
? **Validation** - Tag format validation works  
? **Backward Compatibility** - Legacy code still works  

## Future Enhancements

### Potential Improvements
1. **Import/Export** - Share method configurations between PACS
2. **Categories** - Group methods by category in dropdown
3. **Descriptions** - Show method descriptions in tooltip
4. **Method Library** - Pre-built method templates
5. **Bulk Operations** - Import multiple methods at once
6. **Method History** - Track changes to method definitions
7. **Method Search** - Filter methods by name/tag
8. **Method Validation** - Validate procedure steps before save

### API Enhancement
Consider adding `PacsServiceMethodAttribute` for auto-discovery:
```csharp
[PacsServiceMethod("GetPatientAddress", "Get banner patient address")]
public async Task<string?> GetCurrentPatientAddressAsync() 
{
    return await ExecCustom("GetCurrentPatientAddress");
}
```

## Migration Notes

### For Existing Users
- No action required - built-in methods auto-seeded
- Existing procedures continue to work
- Static XAML resource can be removed in future version

### For Developers
- New PACS methods should be added via `PacsMethodManager.GetBuiltInMethods()`
- Legacy `AutomationWindow.PacsMethodItems.xaml` can be deleted
- Custom method implementations follow same pattern

## Troubleshooting

### Methods Not Showing
**Cause**: PacsMethodManager not initialized  
**Fix**: Check Debug output for initialization errors

### Cannot Delete Method
**Cause**: Method marked as built-in  
**Fix**: Only user-defined methods can be deleted

### Method Changes Not Persisting
**Cause**: File write permission issue  
**Fix**: Check write access to `%APPDATA%\Wysg.Musm\Radium\Pacs`

### Duplicate Tag Error
**Cause**: Method with same tag already exists  
**Fix**: Choose unique tag or delete existing method

## Technical Details

### File Locations
- Model: `apps/Wysg.Musm.Radium/Models/PacsMethod.cs`
- Manager: `apps/Wysg.Musm.Radium/Services/PacsMethodManager.cs`
- UI Logic: `apps/Wysg.Musm.Radium/Views/AutomationWindow.PacsMethods.cs`
- Storage: `%APPDATA%\Wysg.Musm\Radium\Pacs\{pacsKey}\pacs-methods.json`

### Dependencies
- System.Text.Json (serialization)
- CommunityToolkit.Mvvm (INotifyPropertyChanged)
- WPF (UI components)

### Design Patterns
- **Repository Pattern** - PacsMethodManager handles data access
- **Observable Collection** - WPF data binding
- **Partial Classes** - Separation of concerns
- **MVVM** - Model-View-ViewModel architecture

## Conclusion

The dynamic PACS methods feature provides users with complete control over their PACS automation workflow, while maintaining backward compatibility and following best practices for extensibility and maintainability.

---

**Implementation Date**: 2025-02-02  
**Build Status**: ? Success  
**Ready for Use**: ? Yes  
**Breaking Changes**: ? None
