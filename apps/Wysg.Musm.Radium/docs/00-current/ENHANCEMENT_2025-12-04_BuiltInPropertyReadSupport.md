# Enhancement: Built-in Property Read Support in Procedure Operations

**Date**: 2025-12-04  
**Status**: ? Completed  
**Build**: ? Success

---

## Summary

Added support for reading built-in properties directly within procedure operations using the `Var` type. This allows procedures to access MainViewModel properties without requiring custom operations.

---

## Features Added

### 1. New Toggle Properties (Read/Write)

The following toggle properties have been added to `CustomModuleProperties`:

| Property Name | MainViewModel Property | Value Type |
|--------------|----------------------|------------|
| `Study Locked` | `PatientLocked` | "true"/"false" |
| `Study Opened` | `StudyOpened` | "true"/"false" |
| `Current Reportified` | `Reportified` | "true"/"false" |
| `Current Proofread` | `ProofreadMode` | "true"/"false" |
| `Previous Report Splitted` | `PreviousReportSplitted` | "true"/"false" |

### 2. New Read-Only Editor Text Properties

These properties allow reading the current editor text directly from procedures:

| Property Name | MainViewModel Property | Description |
|--------------|----------------------|-------------|
| `Current Header` | `HeaderText` | Current header editor content |
| `Current Findings` | `FindingsText` | Current findings editor content |
| `Current Conclusion` | `ConclusionText` | Current conclusion editor content |

### 3. Built-in Property Resolution in Procedures

When using `Var` type in procedure operations, the system now checks if the variable name matches a built-in property name. If it does, the value is read directly from MainViewModel.

**Example Usage:**
```
Operation: IsMatch
Arg1 Type: Var
Arg1 Value: Current Reportified
Arg2 Type: String
Arg2 Value: true

Result: Returns "true" if Reportified toggle is ON
```

---

## Implementation Details

### Files Modified

1. **`apps/Wysg.Musm.Radium/Models/CustomModule.cs`**
   - Added new toggle property constants
   - Added read-only editor text property constants
   - Added `AllReadableProperties` array (includes all readable properties)
   - Added `IsBuiltInProperty()` helper method

2. **`apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.Automation.Custom.cs`**
   - Updated `SetPropertyValue()` to handle new toggle properties
   - Added `GetPropertyValue()` method for reading properties

3. **`apps/Wysg.Musm.Radium/Services/ProcedureExecutor.Elements.cs`**
   - Updated `ResolveString()` to check for built-in properties
   - Added `GetBuiltInPropertyValue()` helper method

4. **`apps/Wysg.Musm.Radium/Views/AutomationWindow.Procedures.Exec.cs`**
   - Updated `ResolveString()` to check for built-in properties
   - Added `GetBuiltInPropertyValue()` helper method

5. **`apps/Wysg.Musm.Radium/Services/OperationExecutor.MainViewModelOps.cs`**
   - Simplified `GetCurrentHeader`, `GetCurrentFindings`, `GetCurrentConclusion` to use `GetPropertyValue()`

---

## Property Lists

### All Settable Properties (Set Custom Module)

```csharp
CustomModuleProperties.AllProperties = [
    // Patient/study
    "Current Patient Name",
    "Current Patient Number",
    "Current Patient Age",
    "Current Patient Sex",
    "Current Study Studyname",
    "Current Study Datetime",
    "Current Study Remark",
    "Current Patient Remark",
    // Previous study
    "Previous Study Studyname",
    "Previous Study Datetime",
    "Previous Study Report Datetime",
    "Previous Study Report Reporter",
    "Previous Study Report Header and Findings",
    "Previous Study Report Conclusion",
    // Toggles (writable)
    "Study Locked",
    "Study Opened",
    "Current Reportified",
    "Current Proofread",
    "Previous Report Splitted"
];
```

### All Readable Properties (Var Type in Procedures)

```csharp
CustomModuleProperties.AllReadableProperties = [
    // All settable properties +
    // Read-only editor text
    "Current Header",
    "Current Findings",
    "Current Conclusion"
];
```

---

## Usage Examples

### Example 1: Check if Study is Locked

```
Procedure: CheckStudyLocked
Operation: IsMatch
  Arg1 Type: Var, Value: Study Locked
  Arg2 Type: String, Value: true
  Output: var1

Result: var1 = "true" if study is locked, "false" otherwise
```

### Example 2: Check if Reportified Toggle is ON

```
Procedure: CheckReportified
Operation: IsMatch
  Arg1 Type: Var, Value: Current Reportified
  Arg2 Type: String, Value: true
  Output: var1

Operation: Not
  Arg1 Type: Var, Value: var1
  Output: var2

Result: var2 = "true" if NOT reportified
```

### Example 3: Get Current Findings Text

```
Procedure: GetFindingsLength
Operation: IsBlank
  Arg1 Type: Var, Value: Current Findings
  Output: var1

Result: var1 = "true" if findings editor is empty
```

---

## Architecture

### Property Resolution Flow

```
ResolveString(arg, vars)
    ¦¢
    ¦§¦¡ ArgKind.Element ¡æ _elementCache lookup
    ¦¢
    ¦§¦¡ ArgKind.Var
    ¦¢   ¦§¦¡ IsBuiltInProperty(varName)?
    ¦¢   ¦¢   ¦¦¦¡ YES ¡æ GetBuiltInPropertyValue(varName) ¡æ MainViewModel.GetPropertyValue()
    ¦¢   ¦¢
    ¦¢   ¦¦¦¡ NO ¡æ vars dictionary lookup
    ¦¢
    ¦¦¦¡ ArgKind.String/Number ¡æ return value directly
```

### GetBuiltInPropertyValue Flow

```
GetBuiltInPropertyValue(propertyName)
    ¦¢
    ¦¦¦¡ Dispatcher.Invoke()
        ¦¢
        ¦¦¦¡ MainWindow.DataContext ¡æ MainViewModel
            ¦¢
            ¦¦¦¡ mainVM.GetPropertyValue(propertyName)
                ¦¢
                ¦§¦¡ String properties ¡æ return value
                ¦¢
                ¦¦¦¡ Boolean properties ¡æ return "true"/"false"
```

---

## Backward Compatibility

- ? All existing procedures continue to work unchanged
- ? Existing custom modules are unaffected
- ? Variable lookup still works as before (built-in properties are checked first)

---

## Testing

### Test Cases

1. **Read toggle property**
   - Use `Study Locked` as Var value
   - Verify returns "true" when locked, "false" when unlocked

2. **Read editor text property**
   - Use `Current Findings` as Var value
   - Verify returns current editor content

3. **Write toggle property**
   - Create Set custom module with `Current Reportified`
   - Verify toggle state changes

4. **Mixed usage**
   - Create procedure using both built-in properties and regular variables
   - Verify both resolve correctly

---

## Future Enhancements

1. **Unified If/If not** - Allow selecting built-in boolean properties directly in If/If not modules
2. **Property dropdown in AutomationWindow** - Show categorized dropdown for Var type including built-in properties
3. **Additional properties** - Add more MainViewModel properties as needed

---

## Related Documentation

- `ENHANCEMENT_2025-12-04_SessionCacheUI.md` - Session-based caching configuration
- `ENHANCEMENT_2025-12-04_GetTextOnceOperation.md` - GetTextOnce operation for fast failure
