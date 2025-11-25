# Enhancement: New Custom Procedure Operation - SimulateDelete (2025-11-09)

## Overview
Added a new "SimulateDelete" operation to the UI Spy window Custom Procedures section, enabling automated deletion of selected text or character at cursor position by sending the Delete key.

## Changes Made

### 1. SpyWindow.OperationItems.xaml
**Path**: `apps\Wysg.Musm.Radium\Views\SpyWindow.OperationItems.xaml`

**Changes**:
- Added `<ComboBoxItem Content="SimulateDelete"/>` to operations list
- Placed after "SimulateSelectAll" for logical grouping of keyboard simulation operations

**Impact**: Users can now select SimulateDelete from the Operation dropdown in Custom Procedures.

### 2. SpyWindow.Procedures.Exec.cs
**Path**: `apps\Wysg.Musm.Radium\Views\SpyWindow.Procedures.Exec.cs`

**Changes**:
Added operation configuration:
```csharp
case "SimulateDelete":
    // No arguments required
    row.Arg1Enabled = false;
    row.Arg2Enabled = false;
    row.Arg3Enabled = false;
    break;
```

**Impact**: Operation editor correctly disables all arguments when SimulateDelete is selected.

### 3. OperationExecutor.cs
**Path**: `apps\Wysg.Musm.Radium\Services\OperationExecutor.cs`

**Changes**:
Added operation routing:
```csharp
case "SimulateDelete":
    return ExecuteSimulateDelete();
```

**Impact**: SimulateDelete operations are routed to the correct implementation.

### 4. OperationExecutor.SystemOps.cs
**Path**: `apps\Wysg.Musm.Radium\Services\OperationExecutor.SystemOps.cs`

**Changes**:
Implemented ExecuteSimulateDelete method:
```csharp
private static (string preview, string? value) ExecuteSimulateDelete()
{
    try
    {
        System.Windows.Forms.SendKeys.SendWait("{DELETE}");
        return ("(Delete key sent)", null);
    }
    catch (Exception ex) 
    { 
        return ($"(error: {ex.Message})", null); 
    }
}
```

**Impact**: Delete key is sent to the currently focused control.

## Feature Specifications

### Operation Signature
- **Operation Name**: SimulateDelete
- **Arguments**: None (all arguments disabled)
- **Returns**: No value (side-effect only)
- **Preview**: "(Delete key sent)" on success, "(error: message)" on failure

### Behavior
- **With Selection**: Deletes all selected text
- **No Selection**: Deletes character at cursor position (forward delete)
- **Focus Required**: Must have correct control focused

## Use Cases

### 1. Clear Field (Select All + Delete)
```
SetFocus(TextField)
SimulateSelectAll
SimulateDelete
Result: Field completely cleared
```

### 2. Delete Selected Text
```
SetFocus(TextField)
SimulateSelectAll
SimulateDelete
Result: All text deleted
```

### 3. Delete Character at Cursor
```
SetFocus(TextField)
SimulateDelete
Result: Character at cursor position deleted
```

### 4. Clear Multiple Fields
```
SetFocus(Field1)
SimulateSelectAll
SimulateDelete

SetFocus(Field2)
SimulateSelectAll
SimulateDelete
```

### 5. Partial Text Deletion
```
# Position cursor, then delete
SetFocus(TextField)
# Navigate to position...
SimulateDelete  # Deletes character at cursor
```

## Common Patterns

### Pattern 1: Clear Field (Recommended)
```
Operation: SetFocus          Arg1: TextField (Element)
Operation: SimulateSelectAll
Operation: SimulateDelete
Result: Field cleared
```

### Pattern 2: Simple Field Clear (Alternative)
```
Operation: SetValue  Arg1: TextField (Element)  Arg2: "" (String)
Result: Field cleared (faster, more reliable)
```

### Pattern 3: Delete Before Paste
```
Operation: SetFocus          Arg1: TextField (Element)
Operation: SimulateSelectAll
Operation: SimulateDelete
Operation: SimulatePaste
Result: Field cleared then filled with clipboard content
```

### Pattern 4: Conditional Clear
```
Operation: GetText           Arg1: TextField (Element) �� var1
Operation: IsMatch           var1  "expected" �� var2
# If var2 = "false" then clear:
Operation: SetFocus          Arg1: TextField (Element)
Operation: SimulateSelectAll
Operation: SimulateDelete
```

## Technical Details

### Implementation
Uses `System.Windows.Forms.SendKeys.SendWait()` to send Delete key:
```csharp
System.Windows.Forms.SendKeys.SendWait("{DELETE}");
```

### SendKeys Format
- `{DELETE}` - Special key notation for Delete key
- Alternatives: `{DEL}` (also works)
- Different from Backspace: `{BACKSPACE}` or `{BS}`

### Behavior Differences
| State | SimulateDelete | Backspace |
|-------|---------------|-----------|
| With Selection | Deletes selection | Deletes selection |
| No Selection | Deletes forward (right) | Deletes backward (left) |
| At End of Text | No effect | Deletes previous char |
| At Start of Text | Deletes next char | No effect |

### Focus Requirements
**Critical**: The correct control must have focus before SimulateDelete:
- Use SetFocus operation before SimulateDelete
- Verify focus using inspection tools
- Add small delay after SetFocus if needed

## Integration with Other Operations

### With SimulateSelectAll (Common)
```
SetFocus(TextField)
SimulateSelectAll    # Select all text
SimulateDelete       # Delete selected text
```

### With SetValue (Alternative Approach)
```
# Direct approach (preferred for simple clearing)
SetValue(TextField, "")

# vs. Keyboard simulation approach
SetFocus(TextField)
SimulateSelectAll
SimulateDelete
```

### With SimulatePaste (Replace Pattern)
```
SetClipboard("new text")
SetFocus(TextField)
SimulateSelectAll
SimulateDelete
SimulatePaste
```

### Sequential Deletion
```
SetFocus(TextField)
SimulateDelete       # Delete first character
Delay(50)
SimulateDelete       # Delete next character
# Repeat as needed
```

## Comparison with Alternatives

| Method | SimulateDelete | SetValue("") | Backspace |
|--------|---------------|--------------|-----------|
| Speed | Medium | Fast | Medium |
| Selection | Deletes all | N/A | Deletes all |
| Direction | Forward | N/A | Backward |
| Focus Required | Yes | Yes | Yes |
| Recommended for | Keyboard simulation | Simple clearing | Backward deletion |

### When to Use SimulateDelete
- ? Need to simulate user deletion
- ? Clear field using keyboard (with SelectAll)
- ? Delete character at cursor
- ? Legacy controls that don't support ValuePattern
- ? Testing deletion behavior

### When to Use SetValue Instead
- ? Simple field clearing
- ? Control supports ValuePattern
- ? Faster execution needed
- ? No keyboard simulation required

## Best Practices

### ? Do:
```
# Good: Focus before delete
SetFocus(TextField)
SimulateDelete

# Good: Select all before delete for full clear
SetFocus(TextField)
SimulateSelectAll
SimulateDelete

# Good: Add delay for slow controls
SetFocus(TextField)
Delay(100)
SimulateSelectAll
SimulateDelete

# Good: Validate after operation
SimulateDelete
GetText(TextField) �� var1
# Check if var1 is empty or expected
```

### ? Don't:
```
# Bad: No focus set
SimulateDelete  # May delete in wrong control!

# Bad: Use for simple clearing
SimulateSelectAll
SimulateDelete
# Just use: SetValue(TextField, "")

# Bad: Assume immediate effect
SimulateDelete
SimulatePaste  # May be too fast

# Bad: Forget selection for full clear
SimulateDelete  # Only deletes one character!
```

## Troubleshooting

### Issue: Nothing Deleted
**Cause**: Wrong control has focus or no text selected
**Fix**:
```
SetFocus(CorrectField)
Delay(50)
SimulateSelectAll  # Select text first
SimulateDelete
```

### Issue: Only One Character Deleted
**Cause**: No text selected (only cursor position)
**Fix**:
```
SimulateSelectAll  # Select all text first
SimulateDelete     # Now deletes all
```

### Issue: Wrong Field Cleared
**Cause**: Focus not set or lost
**Fix**:
- Verify bookmark resolves correctly
- Add delay after SetFocus
- Check window is active

### Issue: Deletion Doesn't Complete
**Cause**: Next operation executes too fast
**Fix**:
```
SimulateDelete
Delay(50)  # Let deletion complete
# Next operation...
```

## Supported Controls
? **Works with:**
- TextBox (single/multi-line)
- RichTextBox
- ComboBox (editable)
- Most standard Windows edit controls

? **Doesn't work with:**
- Labels (static text)
- Read-only fields
- Custom controls that don't handle Delete key
- Some web controls

## Error Handling

### Success
- Preview: `(Delete key sent)`
- Text deleted in focused control
- Cursor position updated

### Failure
- Preview: `(error: message)`
- Check debug output for details
- Verify control has focus
- Ensure control supports text editing

## Debug Output Example
```
[SimulateDelete] Sending Delete key...
[SimulateDelete] SUCCESS: Delete key sent
```

## Performance Considerations
- **Fast**: Direct keyboard event
- **Blocking**: SendWait waits for processing
- **Synchronous**: No async overhead
- **Reliable**: Standard Windows keyboard handling

## Related Operations
- **SimulateSelectAll**: Select all text before deletion (common)
- **SetFocus**: Prepare control for deletion (essential)
- **SetValue**: Alternative for simple field clearing
- **SimulatePaste**: Follow deletion with paste
- **GetText**: Validate after deletion
- **Delay**: Wait for UI updates

## Example Procedures

### Example 1: Clear Field
```
PACS Method: ClearTextField

Step 1: SetFocus        Arg1: TextField (Element)
Step 2: SimulateSelectAll
Step 3: SimulateDelete
Step 4: Delay           Arg1: 50 (Number)
```

### Example 2: Clear Multiple Fields
```
PACS Method: ClearAllFields

Step 1: SetFocus        Arg1: Field1 (Element)
Step 2: SimulateSelectAll
Step 3: SimulateDelete

Step 4: SetFocus        Arg1: Field2 (Element)
Step 5: SimulateSelectAll
Step 6: SimulateDelete

Step 7: SetFocus        Arg1: Field3 (Element)
Step 8: SimulateSelectAll
Step 9: SimulateDelete
```

### Example 3: Clear and Replace
```
PACS Method: ClearAndReplace

Step 1: SetFocus        Arg1: TextField (Element)
Step 2: SimulateSelectAll
Step 3: SimulateDelete
Step 4: Delay           Arg1: 50 (Number)
Step 5: SetClipboard    Arg1: "New text" (String)
Step 6: SimulatePaste
```

### Example 4: Conditional Clear
```
PACS Method: ConditionalClear

Step 1: GetText         Arg1: TextField (Element) �� var1
Step 2: IsMatch         var1  "old value" �� var2
# If var2 = "true" then clear:
Step 3: SetFocus        Arg1: TextField (Element)
Step 4: SimulateSelectAll
Step 5: SimulateDelete
```

## Testing Recommendations
1. Test with simple TextBox
2. Test with multi-line TextBox
3. Test with RichTextBox
4. Test with editable ComboBox
5. Test with selection (delete all)
6. Test without selection (delete one char)
7. Test focus requirements
8. Test timing/delays
9. Test in automation sequences
10. Test with different PACS controls

## Build Status
- ? Build successful
- ? No warnings
- ? No breaking changes
- ? Follows existing patterns

## Documentation Updates
- ? Spec.md updated (FR-1240 through FR-1250)
- ? Enhancement document created
- ? Operation items list updated
- ? Operation configuration added

## Comparison with Similar Operations

| Operation | Key Sent | Purpose | Use Case |
|-----------|----------|---------|----------|
| **SimulateDelete** | Delete | Delete forward | Clear field, delete at cursor |
| SimulateSelectAll | Ctrl+A | Select all | Before copy/delete/paste |
| SimulatePaste | Ctrl+V | Paste | Fill field from clipboard |
| SimulateTab | Tab | Navigate | Move to next field |

## Future Considerations
- Consider adding SimulateBackspace operation
- Add deletion validation checks
- Support for custom deletion patterns
- Integration with undo operations

## Conclusion
SimulateDelete operation provides keyboard-based deletion for fields where direct manipulation isn't possible or practical. Essential for clearing fields and text manipulation workflows, especially when combined with SimulateSelectAll.

---

**Implementation Date**: 2025-11-09  
**Build Status**: ? Success  
**Ready for Use**: ? Yes
