# Quick Reference: SetValue Operation

**Date**: 2025-11-25  
**Type**: Quick Reference  
**Category**: User Reference  
**Status**: ? Active

---

## Summary

This guide provides quick reference information for users. For detailed implementation information, see the related plan and specification documents.

---

# Quick Reference: SetValue Operation

## What Is It?
SetValue is a Custom Procedure operation that sets text field and control values using UIA ValuePattern.

## Basic Usage

### In UI Spy Custom Procedures
1. Select operation: **SetValue**
2. **Arg1**: Element (select bookmark for target control)
3. **Arg2**: String or Var (value to set)
4. Click **Set** to test
5. Preview shows: `(value set, N chars)` on success

### Simple Example
```
Operation: SetValue
Arg1: PatientNumberField (Element)
Arg2: "12345678" (String)

Result: Field value set to "12345678"
```

## Argument Configuration

| Argument | Type | Required | Purpose |
|----------|------|----------|---------|
| **Arg1** | Element | Yes | Target control/field to set |
| **Arg2** | String or Var | Yes | Value to write |
| **Arg3** | - | Disabled | Not used |

## Common Patterns

### 1. Set Literal Value
```
SetValue  Arg1: TextField  Arg2: "Hello World" (String)
```

### 2. Set from Variable
```
GetText   Arg1: SourceField → var1
SetValue  Arg1: TargetField  Arg2: var1 (Var)
```

### 3. Clear Field
```
SetValue  Arg1: TextField  Arg2: "" (String)
```

### 4. Copy from List Selection
```
GetValueFromSelection  Arg1: DataGrid  Arg2: "Name" → var1
SetValue              Arg1: NameField  Arg2: var1 (Var)
```

### 5. Transform and Set
```
GetText    Arg1: DateField → var1
Replace    var1  "/"  "-" → var2
SetValue   Arg1: FormattedDate  Arg2: var2 (Var)
```

## Supported Controls
? **Works with:**
- TextBox (single/multi-line)
- ComboBox (editable)
- RichEdit controls
- Spinner/NumericUpDown
- Most input fields

? **Doesn't work with:**
- Labels (static text)
- Buttons
- Checkboxes (use Invoke)
- Read-only fields
- Disabled controls

## Preview Messages

| Message | Meaning | Action |
|---------|---------|--------|
| `(value set, N chars)` | ? Success | N characters written |
| `(no element)` | ? Element not found | Check bookmark |
| `(no value pattern)` | ? Control unsupported | Use different control |
| `(read-only)` | ? Field disabled | Enable field first |
| `(error: ...)` | ? Exception | Check debug output |

## Best Practices

### ? Do:
```
# Good: Focus before setting
SetFocus   Arg1: TextField
SetValue   Arg1: TextField  Arg2: "value"

# Good: Validate after setting
SetValue   Arg1: TextField  Arg2: "value"
GetText    Arg1: TextField → var1

# Good: Add delay for slow controls
SetValue   Arg1: TextField  Arg2: "value"
Delay      Arg1: 100
```

### ? Don't:
```
# Bad: No focus (may fail on some controls)
SetValue   Arg1: TextField  Arg2: "value"

# Bad: Assume number format
SetValue   Arg1: AgeField  Arg2: 25  # Wrong! Use "25"

# Bad: Use on labels
SetValue   Arg1: StatusLabel  Arg2: "text"  # Won't work
```

## Troubleshooting

### Problem: `(no value pattern)`
**Cause**: Control doesn't support text input
**Fix**: 
- Check control type in UI Spy
- Try SimulatePaste instead
- Use different bookmark

### Problem: `(read-only)`
**Cause**: Field is disabled or locked
**Fix**:
- Enable field first
- Check control state
- Use different field

### Problem: Value doesn't appear
**Cause**: Control needs focus or delay
**Fix**:
```
SetFocus  Arg1: TextField
Delay     Arg1: 50
SetValue  Arg1: TextField  Arg2: "value"
```

### Problem: Value reverted
**Cause**: Control has validation
**Fix**:
- Provide valid format
- Check control requirements
- Add delay after SetValue

## Complete Form Example

```
# PACS Method: FillPatientForm

# Step 1: Fill patient number
SetFocus   Arg1: PatientNumberField
SetValue   Arg1: PatientNumberField  Arg2: "12345678" (String)

# Step 2: Fill patient name  
SetFocus   Arg1: PatientNameField
SetValue   Arg1: PatientNameField    Arg2: "John Doe" (String)

# Step 3: Fill date of birth
SetFocus   Arg1: DOBField
SetValue   Arg1: DOBField            Arg2: "1990-01-01" (String)

# Step 4: Fill gender
SetFocus   Arg1: GenderField
SetValue   Arg1: GenderField         Arg2: "M" (String)

# Step 5: Wait for UI update
Delay      Arg1: 100 (Number)

# Step 6: Submit
ClickElement  Arg1: SaveButton
```

## Comparison with Other Methods

### SetValue vs. SetClipboard + SimulatePaste

**SetValue (Recommended):**
```
SetFocus  Arg1: TextField
SetValue  Arg1: TextField  Arg2: "value"
```
- ? Direct, fast, reliable
- ? No keyboard simulation
- ? Works with any ValuePattern control

**SetClipboard + SimulatePaste (Legacy):**
```
SetClipboard  Arg1: "value" (String)
SetFocus      Arg1: TextField
SimulatePaste
```
- ?? Slower (keyboard simulation)
- ?? Requires clipboard
- ?? May trigger unwanted events
- ? Works with legacy controls

### When to Use Each

| Scenario | Use SetValue | Use SimulatePaste |
|----------|--------------|-------------------|
| Modern TextBox | ? Yes | ? No |
| Legacy control | ? No | ? Yes |
| Speed critical | ? Yes | ? No |
| Simulate user | ? No | ? Yes |
| Form filling | ? Yes | ? No |

## Integration Examples

### With GetText (Copy Field)
```
GetText   Arg1: Source → var1
SetValue  Arg1: Target  Arg2: var1
```

### With Split (Extract Part)
```
GetText       Arg1: FullName → var1
Split         var1  " "  0 → var2
SetValue      Arg1: FirstName  Arg2: var2
```

### With Replace (Transform)
```
GetText    Arg1: DateField → var1
Replace    var1  "/"  "-" → var2
SetValue   Arg1: ISODate  Arg2: var2
```

### With GetValueFromSelection (Copy from List)
```
GetValueFromSelection  Arg1: List  Arg2: "ID" → var1
SetFocus              Arg1: IDField
SetValue              Arg1: IDField  Arg2: var1
```

## Tips & Tricks

### 1. Chain Multiple SetValue
```
SetValue  Arg1: Field1  Arg2: "value1"
SetValue  Arg1: Field2  Arg2: "value2"
SetValue  Arg1: Field3  Arg2: "value3"
```

### 2. Validate After Set
```
SetValue  Arg1: Field  Arg2: "123"
GetText   Arg1: Field → var1
IsMatch   var1  "123" → var2
# var2 = "true" if value was set correctly
```

### 3. Clear All Fields
```
SetValue  Arg1: Field1  Arg2: ""
SetValue  Arg1: Field2  Arg2: ""
SetValue  Arg1: Field3  Arg2: ""
```

### 4. Copy Between Screens
```
# From main screen
GetText   Arg1: MainField → var1

# To sub screen
SetFocus  Arg1: SubField
SetValue  Arg1: SubField  Arg2: var1
```

## Debug Output Example
```
[SetValue] Element resolved: Name='TextBox1', AutomationId='txtPatient'
[SetValue] Value to set: '12345678' (length=8)
[SetValue] Calling SetValue('12345678')...
[SetValue] SUCCESS: Value set to '12345678'
```

## Related Operations
- **GetText**: Read element value
- **SetFocus**: Focus element before setting
- **SetClipboard**: Set system clipboard
- **SimulatePaste**: Keyboard paste
- **GetValueFromSelection**: Extract list value
- **IsVisible**: Check if element visible
- **Delay**: Wait for UI update

## Quick Checklist
Before using SetValue, verify:
- [ ] Element supports ValuePattern (test with Get Text button)
- [ ] Element is not read-only (check in UI Spy)
- [ ] Bookmark resolves correctly (use Resolve button)
- [ ] Value format is correct for control type
- [ ] Focus set before SetValue (add SetFocus if needed)

## Related Documentation
- Full details: `ENHANCEMENT_2025-11-09_SetValueOperation.md`
- Feature specs: `Spec.md` (FR-1200 through FR-1214)
- Other operations: `SpyWindow.OperationItems.xaml`

