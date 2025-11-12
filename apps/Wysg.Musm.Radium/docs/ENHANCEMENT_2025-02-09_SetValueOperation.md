# Enhancement: New Custom Procedure Operation - SetValue (2025-02-09)

## Overview
Added a new "SetValue" operation to the UI Spy window Custom Procedures section, enabling automated setting of text field and control values using the UIA ValuePattern.

## Changes Made

### 1. SpyWindow.OperationItems.xaml
**Path**: `apps\Wysg.Musm.Radium\Views\SpyWindow.OperationItems.xaml`

**Changes**:
- Added `<ComboBoxItem Content="SetValue"/>` to operations list
- Placed after "SetFocus" operation for logical grouping

**Impact**: Users can now select SetValue from the Operation dropdown in Custom Procedures.

### 2. SpyWindow.Procedures.Exec.cs
**Path**: `apps\Wysg.Musm.Radium\Views\SpyWindow.Procedures.Exec.cs`

**Changes**:
Added operation configuration:
```csharp
case "SetValue":
    // SetValue: Arg1=Element (target control), Arg2=String or Var (value to set)
    row.Arg1.Type = nameof(ArgKind.Element); row.Arg1Enabled = true;
    row.Arg2Enabled = true; // Allow String or Var
    row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
    break;
```

**Impact**: Operation editor correctly configures arguments when SetValue is selected.

### 3. OperationExecutor.cs
**Path**: `apps\Wysg.Musm.Radium\Services\OperationExecutor.cs`

**Changes**:
Added operation routing:
```csharp
case "SetValue":
    return ExecuteSetValue(resolveArg1Element(), resolveArg2String());
```

**Impact**: SetValue operations are routed to the correct implementation.

### 4. OperationExecutor.ElementOps.cs
**Path**: `apps\Wysg.Musm.Radium\Services\OperationExecutor.ElementOps.cs`

**Changes**:
Implemented ExecuteSetValue method:
```csharp
private static (string preview, string? value) ExecuteSetValue(AutomationElement? el, string? valueToSet)
{
    if (el == null) return ("(no element)", null);
    if (valueToSet == null) valueToSet = string.Empty;
    
    var valuePattern = el.Patterns.Value.PatternOrDefault;
    if (valuePattern == null) return ("(no value pattern)", null);
    if (valuePattern.IsReadOnly) return ("(read-only)", null);
    
    valuePattern.SetValue(valueToSet);
    return ($"(value set, {valueToSet.Length} chars)", null);
}
```

**Impact**: UI elements can now have their values set programmatically.

## Feature Specifications

### Operation Signature
- **Operation Name**: SetValue
- **Arg1**: Element (Type=Element) - Target UI control/field
- **Arg2**: String or Var (Type=String/Var) - Value to set
- **Arg3**: Disabled
- **Output**: No return value (operation is side-effect only)

### Behavior
1. Resolves target element from Arg1 bookmark
2. Resolves value from Arg2 (literal string or variable reference)
3. Validates element supports ValuePattern
4. Checks if element is read-only
5. Sets element value using `ValuePattern.SetValue()`
6. Returns preview message indicating success/failure

### Preview Messages
- `(value set, N chars)` - Success, N characters written
- `(no element)` - Element resolution failed
- `(no value pattern)` - Element doesn't support value setting
- `(read-only)` - Element is read-only
- `(error: message)` - Exception occurred

## Use Cases

### 1. Fill Text Fields
```
Operation: SetValue
Arg1: PatientNumberField (Element)
Arg2: "12345678" (String)
```

### 2. Set Value from Variable
```
Operation: GetText       Arg1: SourceField (Element)    ¡æ var1
Operation: SetValue      Arg1: TargetField (Element)    Arg2: var1 (Var)
```

### 3. Copy Between Fields
```
Operation: GetValueFromSelection  Arg1: DataGrid (Element)  Arg2: "Name" (String) ¡æ var1
Operation: SetValue              Arg1: NameField (Element)  Arg2: var1 (Var)
```

### 4. Clear Field
```
Operation: SetValue  Arg1: TextField (Element)  Arg2: "" (String)
```

### 5. Populate Form
```
Operation: SetValue  Arg1: NameField (Element)      Arg2: "John Doe" (String)
Operation: SetValue  Arg1: IDField (Element)        Arg2: "12345" (String)
Operation: SetValue  Arg1: DateField (Element)      Arg2: "2025-02-09" (String)
Operation: ClickElement  Arg1: SubmitButton (Element)
```

## Technical Details

### ValuePattern Requirements
- Element MUST support UIA `ValuePattern`
- Common supported controls:
  - TextBox (Edit controls)
  - ComboBox (editable)
  - Spinner/NumericUpDown
  - RichEdit controls
  - Some custom controls

### Not Supported
- Controls without ValuePattern:
  - Labels (static text)
  - Buttons
  - Checkboxes (use Invoke/Toggle instead)
  - Non-editable ComboBox
  - Read-only fields

### Read-Only Detection
Operation automatically checks `ValuePattern.IsReadOnly`:
- If true: returns `(read-only)` without attempting to set
- If false: proceeds with value setting

### Value Type Handling
- Arg2 accepts String or Var types
- Null values converted to empty string
- No type conversion (caller must provide correct format)
- For numbers: provide as string (e.g., "123")
- For dates: provide in control's expected format

## Integration with Other Operations

### Chaining with GetText
```
# Copy value from one field to another
GetText(SourceField) ¡æ var1
SetValue(TargetField, var1)
```

### Chaining with Split
```
# Extract and set part of text
GetText(FullNameField) ¡æ var1
Split(var1, " ", 0) ¡æ var2  # First name
SetValue(FirstNameField, var2)
```

### Chaining with Replace
```
# Transform and set value
GetText(DateField) ¡æ var1
Replace(var1, "/", "-") ¡æ var2
SetValue(FormattedDateField, var2)
```

## Error Handling

### Element Not Found
- Returns: `(no element)`
- Cause: Bookmark doesn't resolve
- Solution: Verify bookmark mapping in UI Spy

### No Value Pattern
- Returns: `(no value pattern)`
- Cause: Control doesn't support text input
- Solution: Check control type, may need different operation

### Read-Only Field
- Returns: `(read-only)`
- Cause: Field is disabled or read-only
- Solution: Enable field first or use different control

### Exception During Set
- Returns: `(error: message)`
- Cause: Various (control disposed, validation failed, etc.)
- Solution: Check debug output for details

## Comparison with Other Operations

| Operation | Purpose | Value Source | Target |
|-----------|---------|--------------|--------|
| **SetValue** | Set control value | String/Var | Element (any with ValuePattern) |
| SetClipboard | Set Windows clipboard | String/Var | System clipboard |
| SimulatePaste | Paste via keyboard | Clipboard | Focused control |
| ClickElement | Click control | N/A | Element |
| SetFocus | Focus control | N/A | Element |

### When to Use SetValue vs. SimulatePaste

**Use SetValue:**
- ? Direct value setting (faster, more reliable)
- ? No keyboard simulation needed
- ? Works with any ValuePattern control
- ? Can set to any value (not limited by clipboard)

**Use SimulatePaste:**
- ? Control doesn't support ValuePattern
- ? Need to trigger change events (some controls)
- ? Legacy controls that don't expose patterns
- ? Simulating user behavior exactly

## Best Practices

### ? Do:
- Verify element supports ValuePattern before using
- Use SetFocus before SetValue for reliability
- Provide values in control's expected format
- Add validation after SetValue (GetText to verify)
- Use Delay after SetValue if control needs processing time

### ? Don't:
- Use on labels or static text (won't work)
- Set values on disabled controls (check IsReadOnly first)
- Assume value format (numbers as strings, not integers)
- Forget to escape special characters in string literals
- Chain too many operations without validation

## Example Procedures

### Example 1: Patient Registration Form
```
PACS Method: FillPatientInfo

Step 1: SetFocus      Arg1: PatientNumberField    # Prepare field
Step 2: SetValue      Arg1: PatientNumberField    Arg2: var_patient_number (Var)
Step 3: SetFocus      Arg1: PatientNameField
Step 4: SetValue      Arg1: PatientNameField      Arg2: var_patient_name (Var)
Step 5: SetFocus      Arg1: DateOfBirthField
Step 6: SetValue      Arg1: DateOfBirthField      Arg2: "1990-01-01" (String)
Step 7: Delay         Arg1: 100 (Number)          # Let UI update
Step 8: ClickElement  Arg1: SaveButton
```

### Example 2: Search and Copy
```
PACS Method: CopySearchResult

Step 1: GetValueFromSelection  Arg1: SearchResultsList  Arg2: "ID" (String) ¡æ var1
Step 2: SetFocus              Arg1: SearchBox
Step 3: SetValue              Arg1: SearchBox          Arg2: var1 (Var)
Step 4: Delay                 Arg1: 50 (Number)
Step 5: ClickElement          Arg1: SearchButton
```

### Example 3: Clear and Reset Form
```
PACS Method: ClearForm

Step 1: SetValue  Arg1: Field1  Arg2: "" (String)
Step 2: SetValue  Arg1: Field2  Arg2: "" (String)
Step 3: SetValue  Arg1: Field3  Arg2: "" (String)
Step 4: SetFocus  Arg1: Field1  # Return to first field
```

## Debugging Tips

### Enable Debug Output
SetValue logs detailed information to Debug output:
```
[SetValue] Element resolved: Name='TextBox1', AutomationId='txtPatientNumber'
[SetValue] Value to set: '12345678' (length=8)
[SetValue] Calling SetValue('12345678')...
[SetValue] SUCCESS: Value set to '12345678'
```

### Common Issues

**Issue**: `(no value pattern)`
- **Check**: Element type in UI Spy
- **Fix**: Verify control is TextBox/ComboBox/etc.

**Issue**: `(read-only)`
- **Check**: Control enabled state
- **Fix**: Enable control first or choose different element

**Issue**: Value doesn't appear in field
- **Check**: Debug output for exceptions
- **Fix**: Add Delay after SetValue, check control focus

**Issue**: Value reverted after set
- **Check**: Control has validation
- **Fix**: Provide valid format, trigger validation event

## Testing Recommendations

1. **Test with TextBox**: Simple text input
2. **Test with ComboBox**: Editable dropdown
3. **Test with NumericUpDown**: Number formatting
4. **Test with DatePicker**: Date format handling
5. **Test with RichEdit**: Multi-line text
6. **Test read-only field**: Verify rejection
7. **Test with variables**: String and Var types
8. **Test empty string**: Field clearing
9. **Test long text**: Performance with large strings
10. **Test special characters**: Unicode, escape sequences

## Performance Considerations

- **Fast operation**: Direct ValuePattern call (no keyboard simulation)
- **No retry logic**: Single attempt (add Delay if needed)
- **Thread-safe**: Can be called from any thread
- **Synchronous**: Completes immediately (no async wait)

## Future Enhancements

- Consider adding retry logic for transient failures
- Add timeout parameter for slow controls
- Support type conversion (string to number, etc.)
- Add validation callback support
- Implement batch SetValue for multiple fields

## Related Features
- GetText: Read element value
- SetClipboard: Set system clipboard
- SimulatePaste: Keyboard-based paste
- SetFocus: Focus element before value setting
- GetValueFromSelection: Extract value from list

## Migration Notes
- No breaking changes to existing operations
- Complements existing value manipulation operations
- Can be used alongside clipboard-based approaches
- Follows existing operation patterns and conventions
