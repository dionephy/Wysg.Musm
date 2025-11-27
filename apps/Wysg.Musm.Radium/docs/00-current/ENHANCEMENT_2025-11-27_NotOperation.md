# Enhancement: New Custom Procedure Operation - Not (2025-11-27)

## Overview
Added a new "Not" operation to the Automation window Custom Procedures section, enabling boolean NOT logic for inverting boolean variable values.

## Changes Made

### 1. AutomationWindow.OperationItems.xaml
**Path**: `apps\Wysg.Musm.Radium\Views\AutomationWindow.OperationItems.xaml`

**Changes**:
- Added `<ComboBoxItem Content="Not"/>` to operations list
- Placed after "And" for logical grouping with boolean operations

**Impact**: Users can now select "Not" from the Operation dropdown in Custom Procedures.

### 2. AutomationWindow.Procedures.Exec.cs
**Path**: `apps\Wysg.Musm.Radium\Views\AutomationWindow.Procedures.Exec.cs`

**Changes**:
Added operation configuration:
```csharp
case "Not":
    // Not: Arg1=boolean var, returns "true" if Arg1 is false, "false" if Arg1 is true
    row.Arg1.Type = nameof(ArgKind.Var); row.Arg1Enabled = true;
    row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
    row.Arg3.Type = nameof(ArgKind.String); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
    break;
```

**Impact**: Operation editor correctly sets only Arg1 as Var type when Not is selected.

### 3. OperationExecutor.cs
**Path**: `apps\Wysg.Musm.Radium\Services\OperationExecutor.cs`

**Changes**:
Added operation routing:
```csharp
case "Not":
    return ExecuteNot(resolveArg1String());
```

**Impact**: Not operations are routed to the correct implementation.

### 4. OperationExecutor.StringOps.cs
**Path**: `apps\Wysg.Musm.Radium\Services\OperationExecutor.StringOps.cs`

**Changes**:
Implemented ExecuteNot method:
```csharp
private static (string preview, string? value) ExecuteNot(string? value1)
{
    value1 ??= string.Empty;

    // Check if value is "true" (case-insensitive)
    bool isTrue = string.Equals(value1, "true", StringComparison.OrdinalIgnoreCase);

    // Invert the boolean value
    bool result = !isTrue;
    string resultStr = result ? "true" : "false";

    return ($"{resultStr} (NOT {value1})", resultStr);
}
```

**Impact**: Boolean NOT logic is evaluated and inverted result returned as "true" or "false" string.

## Feature Specifications

### Operation Signature
- **Operation Name**: Not
- **Arg1**: Var (boolean value - "true" or "false")
- **Returns**: String "true" if input is false, "false" if input is true
- **Preview**: `"true (NOT false)"` or `"false (NOT true)"`

### Boolean Logic
| Arg1 | Result |
|------|--------|
| "true" | "false" |
| "false" | "true" |
| "" | "true" |
| null | "true" |
| Any non-"true" | "true" |

### Case Insensitivity
- "true", "True", "TRUE" all treated as true
- Any other value (including "false", "False", "", null) treated as false

## Use Cases

### 1. Negate IsMatch Result
```
GetText(Field1) ?? var1
IsMatch(var1, "expected") ?? var2
Not(var2) ?? var3
Result: var3 = "true" if field does NOT match
```

### 2. Check Element Not Visible
```
IsVisible(ErrorDialog) ?? var1
Not(var1) ?? var2
Result: var2 = "true" if error dialog is NOT visible (safe to proceed)
```

### 3. Invert Safety Check
```
PatientNumberMatch ?? var1
Not(var1) ?? var2
# var2 = "true" means patient number does NOT match (warning condition)
```

### 4. Complex Logic with And
```
Condition1 ?? var1
Condition2 ?? var2
Not(var1) ?? var3
And(var2, var3) ?? var4
# var4 = "true" if Condition2 is true AND Condition1 is false
```

### 5. Double Negation
```
IsVisible(Element) ?? var1
Not(var1) ?? var2
Not(var2) ?? var3
# var3 equals var1 (double negation = original)
```

## Common Patterns

### Pattern 1: Field Does Not Match
```
Operation: GetText      Arg1: Field (Element) ?? var1
Operation: IsMatch      Arg1: var1 (Var)  Arg2: "unwanted" (String) ?? var2
Operation: Not          Arg1: var2 (Var) ?? var3
Result: var3 = "true" if field does NOT contain unwanted value
```

### Pattern 2: Element Not Present
```
Operation: IsVisible    Arg1: ErrorMessage (Element) ?? var1
Operation: Not          Arg1: var1 (Var) ?? var2
Result: var2 = "true" if no error message shown (safe to proceed)
```

### Pattern 3: De Morgan's Law (NOT (A AND B) = (NOT A) OR (NOT B))
```
Operation: Condition1 ?? var1
Operation: Condition2 ?? var2
Operation: And          Arg1: var1 (Var)  Arg2: var2 (Var) ?? var3
Operation: Not          Arg1: var3 (Var) ?? var4
Result: var4 = "true" if NOT both conditions are true
```

### Pattern 4: Exclusion Logic
```
Operation: IsMatch      Arg1: status (Var)  Arg2: "completed" (String) ?? var1
Operation: Not          Arg1: var1 (Var) ?? var2
# var2 = "true" means status is NOT completed
```

## Technical Details

### Implementation
```csharp
private static (string preview, string? value) ExecuteNot(string? value1)
{
    value1 ??= string.Empty;

    // Check if value is "true" (case-insensitive)
    bool isTrue = string.Equals(value1, "true", StringComparison.OrdinalIgnoreCase);

    // Invert the boolean value
    bool result = !isTrue;
    string resultStr = result ? "true" : "false";

    return ($"{resultStr} (NOT {value1})", resultStr);
}
```

### Behavior
- **Null Safety**: Null inputs converted to empty string (treated as false, inverted to true)
- **Case Insensitive**: "true", "True", "TRUE" all valid
- **Strict Matching**: Only exact "true" (case-insensitive) is true; everything else is false (inverted to true)
- **Preview**: Shows input and result for debugging

### Truth Table Details
```
Not("true") ¡æ "false"
Not("True") ¡æ "false"
Not("TRUE") ¡æ "false"
Not("false") ¡æ "true"
Not("") ¡æ "true"
Not(null) ¡æ "true"
Not("yes") ¡æ "true"  # Only "true" string is recognized as true
Not("1") ¡æ "true"
Not("on") ¡æ "true"
```

## Integration with Other Operations

### With IsMatch
```
IsMatch(var1, "expected") ?? var2
Not(var2) ?? var3  # var3 = "true" if NOT matching
```

### With IsVisible
```
IsVisible(Element) ?? var1
Not(var1) ?? var2  # var2 = "true" if NOT visible
```

### With And (Exclusion Logic)
```
Condition1 ?? var1
Condition2 ?? var2
Not(var1) ?? var3
And(var2, var3) ?? var4  # var4 = "true" if Condition2 AND NOT Condition1
```

### Chaining Multiple Nots (Double Negation)
```
IsMatch(var1, "test") ?? var2
Not(var2) ?? var3
Not(var3) ?? var4  # var4 equals var2 (double negation cancels)
```

## Comparison with Alternatives

| Aspect | Not Operation | Manual Inversion | Conditional Branching |
|--------|---------------|------------------|----------------------|
| Simplicity | ? One step | ? Multiple | ? Complex |
| Readability | ? Clear | ?? Verbose | ? Hidden |
| Reusability | ? Chainable | ? One-off | ?? Limited |
| Debugging | ? Preview | ? No visibility | ? Hidden |

## Best Practices

### ? Do:
```
# Good: Clear negation
IsMatch(var1, "expected") ?? var2
Not(var2) ?? var3

# Good: Meaningful variable names
IsVisible(ErrorDialog) ?? hasError
Not(hasError) ?? noError

# Good: Complex logic
Condition1 ?? result1
Not(result1) ?? notResult1
And(result2, notResult1) ?? finalCheck
```

### ? Don't:
```
# Bad: Double negation without purpose
IsMatch(var1, "test") ?? var2
Not(var2) ?? var3
Not(var3) ?? var4  # Just use var2!

# Bad: Using with non-boolean
GetText(Field) ?? var1
Not(var1)  # var1 is text, not boolean!

# Bad: Forgetting to capture result
IsMatch(var1, "expected")  # Not captured!
Not(var1)  # Wrong var!
```

## Troubleshooting

### Issue: Not Always Returns "true"
**Cause**: Input variable doesn't contain "true" string
**Fix**:
```
# Check what value is actually in variable
IsMatch(var1, "expected") ?? var2
# Check preview of var2 before Not
Not(var2) ?? var3
```

### Issue: Can't Use String Literal
**Cause**: Not operation requires Var type for argument
**Fix**:
- Use IsMatch or IsVisible to generate boolean var first
- Don't try `Not("false")` - not supported by design

### Issue: Result Unexpected
**Cause**: Misunderstanding of what values are "true"
**Debug**:
```
# Only "true" (case-insensitive) is true
IsMatch(var1, "yes") ?? var2  # var2 might be "false" even if match
Not(var2) ?? var3  # var3 = "true"
```

## Supported Input Values

### ? Recognized as True (inverted to "false")
- "true"
- "True"
- "TRUE"
- "TrUe"
- (any case variation of "true")

### ? Treated as False (inverted to "true")
- "false", "False", "FALSE"
- "" (empty string)
- null
- "yes", "YES"
- "1"
- "on", "ON"
- Any non-"true" string

## Error Handling

### Success
- Preview: `"true (NOT false)"` or `"false (NOT true)"`
- Returns: "true" or "false" string
- Always succeeds (no exceptions)

### Null/Empty Handling
- Null converted to empty string ¡æ false ¡æ inverted to "true"
- Empty string ¡æ false ¡æ inverted to "true"
- No exceptions thrown

## Performance Considerations
- **Fast**: Simple string comparison and inversion
- **No I/O**: Pure logic operation
- **Synchronous**: No delays
- **Lightweight**: Minimal memory

## Related Operations
- **And**: Combine multiple conditions
- **IsMatch**: Generate boolean from comparison
- **IsAlmostMatch**: Generate boolean from fuzzy comparison
- **IsVisible**: Generate boolean from visibility check
- **PatientNumberMatch**: Generate boolean from PACS match
- **StudyDateTimeMatch**: Generate boolean from PACS match

## Future Enhancements
- Consider adding **Or** operation (true if either true)
- Consider adding **Xor** operation (true if exactly one true)
- Consider adding **Nand** operation (NOT (A AND B))

## Example Procedures

### Example 1: Field Does Not Match Expected
```
PACS Method: ValidateFieldNotEquals

Step 1: GetText         Arg1: Field (Element) ?? var1
Step 2: IsMatch         Arg1: var1 (Var)  Arg2: "unwanted" (String) ?? var2
Step 3: Not             Arg1: var2 (Var) ?? var3
Result: var3 = "true" if field does NOT match unwanted value
```

### Example 2: Safe to Proceed (No Error)
```
PACS Method: CheckNoError

Step 1: IsVisible       Arg1: ErrorDialog (Element) ?? var1
Step 2: Not             Arg1: var1 (Var) ?? var2
# var2 = "true" means no error dialog (safe to proceed)
```

### Example 3: Exclusion Logic (A AND NOT B)
```
PACS Method: ExclusiveCondition

Step 1: IsVisible       Arg1: ElementA (Element) ?? var1
Step 2: IsVisible       Arg1: ElementB (Element) ?? var2
Step 3: Not             Arg1: var2 (Var) ?? var3
Step 4: And             Arg1: var1 (Var)  Arg2: var3 (Var) ?? var4
Result: var4 = "true" if ElementA visible AND ElementB NOT visible
```

### Example 4: Status Not Completed
```
PACS Method: CheckNotCompleted

Step 1: GetText         Arg1: StatusField (Element) ?? var1
Step 2: IsMatch         Arg1: var1 (Var)  Arg2: "completed" (String) ?? var2
Step 3: Not             Arg1: var2 (Var) ?? var3
# var3 = "true" means status is NOT completed
```

## Testing Recommendations
1. Test with "true" input ¡æ expect "false"
2. Test with "false" input ¡æ expect "true"
3. Test with empty string input ¡æ expect "true"
4. Test with null input ¡æ expect "true"
5. Test case variations ("True", "TRUE") ¡æ expect "false"
6. Test with non-"true" strings ¡æ expect "true"
7. Test double negation (cancellation)
8. Test in automation sequences
9. Test with And operation
10. Test with different PACS procedures

## Build Status
- ? Build successful
- ? No warnings
- ? No breaking changes
- ? Follows existing patterns

## Documentation Updates
- ? Enhancement document created
- ? Operation items list updated
- ? Operation configuration added
- ? OperationExecutor routing added
- ? ExecuteNot implementation added

## Conclusion
The Not operation provides boolean NOT logic for inverting boolean values, enabling exclusion logic, negative assertions, and complex conditional workflows. Essential for building complete boolean logic in automation procedures.

---

**Implementation Date**: 2025-11-27  
**Build Status**: ? Success  
**Ready for Use**: ? Yes
