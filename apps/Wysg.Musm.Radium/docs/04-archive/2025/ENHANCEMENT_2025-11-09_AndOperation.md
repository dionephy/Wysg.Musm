# Enhancement: New Custom Procedure Operation - And (2025-11-09)

## Overview
Added a new "And" operation to the UI Spy window Custom Procedures section, enabling boolean AND logic for combining two boolean variable results into a single true/false output.

## Changes Made

### 1. SpyWindow.OperationItems.xaml
**Path**: `apps\Wysg.Musm.Radium\Views\SpyWindow.OperationItems.xaml`

**Changes**:
- Added `<ComboBoxItem Content="And"/>` to operations list
- Placed after "IsAlmostMatch" for logical grouping with comparison operations

**Impact**: Users can now select "And" from the Operation dropdown in Custom Procedures.

### 2. SpyWindow.Procedures.Exec.cs
**Path**: `apps\Wysg.Musm.Radium\Views\SpyWindow.Procedures.Exec.cs`

**Changes**:
Added operation configuration:
```csharp
case "And":
    // And: Arg1=boolean var, Arg2=boolean var, returns "true" if both true
    row.Arg1.Type = nameof(ArgKind.Var); row.Arg1Enabled = true;
    row.Arg2.Type = nameof(ArgKind.Var); row.Arg2Enabled = true;
    row.Arg3.Type = nameof(ArgKind.String); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
    break;
```

**Impact**: Operation editor correctly sets both Arg1 and Arg2 as Var type when And is selected.

### 3. OperationExecutor.cs
**Path**: `apps\Wysg.Musm.Radium\Services\OperationExecutor.cs`

**Changes**:
Added operation routing:
```csharp
case "And":
    return ExecuteAnd(resolveArg1String(), resolveArg2String());
```

**Impact**: And operations are routed to the correct implementation.

### 4. OperationExecutor.StringOps.cs
**Path**: `apps\Wysg.Musm.Radium\Services\OperationExecutor.StringOps.cs`

**Changes**:
Implemented ExecuteAnd method:
```csharp
private static (string preview, string? value) ExecuteAnd(string? value1, string? value2)
{
    value1 ??= string.Empty;
    value2 ??= string.Empty;

    // Check if both values are "true" (case-insensitive)
    bool isTrue1 = string.Equals(value1, "true", StringComparison.OrdinalIgnoreCase);
    bool isTrue2 = string.Equals(value2, "true", StringComparison.OrdinalIgnoreCase);

    bool result = isTrue1 && isTrue2;
    string resultStr = result ? "true" : "false";

    return ($"{resultStr} ({value1} AND {value2})", resultStr);
}
```

**Impact**: Boolean AND logic is evaluated and result returned as "true" or "false" string.

## Feature Specifications

### Operation Signature
- **Operation Name**: And
- **Arg1**: Var (boolean value - "true" or "false")
- **Arg2**: Var (boolean value - "true" or "false")
- **Returns**: String "true" if both true, "false" otherwise
- **Preview**: `"true (true AND true)"` or `"false (true AND false)"`

### Boolean Logic
| Arg1 | Arg2 | Result |
|------|------|--------|
| "true" | "true" | "true" |
| "true" | "false" | "false" |
| "false" | "true" | "false" |
| "false" | "false" | "false" |
| Any other | Any | "false" |

### Case Insensitivity
- "true", "True", "TRUE" all treated as true
- Any other value (including "false", "False", "", null) treated as false

## Use Cases

### 1. Combine Two IsMatch Results
```
GetText(Field1) �� var1
IsMatch(var1, "expected1") �� var2

GetText(Field2) �� var3
IsMatch(var3, "expected2") �� var4

And(var2, var4) �� var5
Result: var5 = "true" only if both fields match
```

### 2. Validate Multiple Visibility Conditions
```
IsVisible(Element1) �� var1
IsVisible(Element2) �� var2
And(var1, var2) �� var3
Result: var3 = "true" only if both elements visible
```

### 3. Gate Automation Step
```
IsMatch(PatientNumber, var1) �� var2
IsMatch(StudyDateTime, var3) �� var4
And(var2, var4) �� var5
# Use var5 to decide whether to proceed
```

### 4. Multi-Condition Validation
```
WorklistIsVisible �� var1
PatientNumberMatch �� var2
And(var1, var2) �� var3
# var3 = "true" means both conditions satisfied
```

### 5. Chaining Multiple Conditions
```
Condition1 �� var1
Condition2 �� var2
And(var1, var2) �� var3

Condition3 �� var4
And(var3, var4) �� var5
# var5 = "true" means all three conditions pass
```

## Common Patterns

### Pattern 1: Dual Validation
```
Operation: IsMatch      Arg1: var1 (Var)  Arg2: "expected" (String) �� var2
Operation: IsMatch      Arg1: var3 (Var)  Arg2: "expected2" (String) �� var4
Operation: And          Arg1: var2 (Var)  Arg2: var4 (Var) �� var5
Result: var5 = "true" if both validations pass
```

### Pattern 2: Visibility Gate
```
Operation: IsVisible    Arg1: Button1 (Element) �� var1
Operation: IsVisible    Arg1: Button2 (Element) �� var2
Operation: And          Arg1: var1 (Var)  Arg2: var2 (Var) �� var3
Result: var3 = "true" if both buttons visible
```

### Pattern 3: Multi-Step Condition
```
Operation: PatientNumberMatch �� var1
Operation: StudyDateTimeMatch �� var2
Operation: And          Arg1: var1 (Var)  Arg2: var2 (Var) �� var3
# var3 = "true" means patient and study both match
```

### Pattern 4: Triple Condition (Chained And)
```
Operation: Condition1 �� var1
Operation: Condition2 �� var2
Operation: And          Arg1: var1 (Var)  Arg2: var2 (Var) �� var3
Operation: Condition3 �� var4
Operation: And          Arg1: var3 (Var)  Arg2: var4 (Var) �� var5
Result: var5 = "true" if all three conditions pass
```

## Technical Details

### Implementation
```csharp
private static (string preview, string? value) ExecuteAnd(string? value1, string? value2)
{
    value1 ??= string.Empty;
    value2 ??= string.Empty;

    // Check if both values are "true" (case-insensitive)
    bool isTrue1 = string.Equals(value1, "true", StringComparison.OrdinalIgnoreCase);
    bool isTrue2 = string.Equals(value2, "true", StringComparison.OrdinalIgnoreCase);

    bool result = isTrue1 && isTrue2;
    string resultStr = result ? "true" : "false";

    return ($"{resultStr} ({value1} AND {value2})", resultStr);
}
```

### Behavior
- **Null Safety**: Null inputs converted to empty string (treated as false)
- **Case Insensitive**: "true", "True", "TRUE" all valid
- **Strict Matching**: Only exact "true" (case-insensitive) is true; everything else is false
- **Preview**: Shows both inputs and result for debugging

### Truth Table Details
```
And("true", "true") �� "true"
And("True", "TRUE") �� "true"
And("true", "false") �� "false"
And("true", "") �� "false"
And("true", null) �� "false"
And("false", "false") �� "false"
And("", "") �� "false"
And("yes", "yes") �� "false"  # Only "true" string is recognized
```

## Integration with Other Operations

### With IsMatch (Most Common)
```
IsMatch(var1, "expected") �� var2
IsMatch(var3, "expected2") �� var4
And(var2, var4) �� var5
```

### With IsVisible
```
IsVisible(Element1) �� var1
IsVisible(Element2) �� var2
And(var1, var2) �� var3
```

### With IsAlmostMatch
```
IsAlmostMatch(var1, "fuzzy1") �� var2
IsAlmostMatch(var3, "fuzzy2") �� var4
And(var2, var4) �� var5
```

### With PACS Methods
```
PatientNumberMatch �� var1
StudyDateTimeMatch �� var2
And(var1, var2) �� var3
```

### Chaining Multiple Ands
```
Condition1 �� var1
Condition2 �� var2
And(var1, var2) �� var3

Condition3 �� var4
And(var3, var4) �� var5  # Result of first AND with third condition
```

## Comparison with Alternatives

| Aspect | And Operation | Manual Checking | Nested Procedures |
|--------|---------------|-----------------|-------------------|
| Simplicity | ? Single operation | ? Multiple steps | ? Complex |
| Readability | ? Clear intent | ?? Verbose | ? Nested |
| Reusability | ? Chainable | ? One-off | ?? Limited |
| Debugging | ? Preview shows both | ? No visibility | ? Hidden |

## Best Practices

### ? Do:
```
# Good: Clear boolean chain
IsMatch(var1, "expected") �� var2
IsMatch(var3, "expected2") �� var4
And(var2, var4) �� var5

# Good: Meaningful variable names
IsVisible(SaveButton) �� isSaveButtonVisible
IsVisible(CancelButton) �� isCancelButtonVisible
And(isSaveButtonVisible, isCancelButtonVisible) �� bothButtonsVisible

# Good: Chain multiple conditions
CheckA �� resultA
CheckB �� resultB
And(resultA, resultB) �� combinedAB
CheckC �� resultC
And(combinedAB, resultC) �� finalResult
```

### ? Don't:
```
# Bad: Using with non-boolean values
GetText(Field) �� var1  # Returns text, not boolean
And(var1, var2)        # Wrong - var1 is not boolean

# Bad: Forgetting to capture IsMatch result
IsMatch(var1, "expected")  # Result not captured!
And(var1, var2)            # var1 is original value, not match result

# Bad: Using hardcoded strings
And("true", var1)  # Can't use String literals, must use Var
```

## Troubleshooting

### Issue: And Always Returns "false"
**Cause**: Input variables don't contain "true" string
**Fix**:
```
# Check what values are actually in variables
IsMatch(var1, "expected") �� var2
GetText shows var2 value before And
And(var2, var3) �� var4
```

### Issue: Can't Use String Literal
**Cause**: And operation requires Var type for both arguments
**Fix**:
- Use IsMatch or IsVisible to generate boolean vars first
- Don't try `And("true", var1)` - not supported by design

### Issue: Result Not What Expected
**Cause**: Case or value mismatch
**Debug**:
```
# Add debug operations to see actual values
IsMatch(var1, "expected") �� var2
# Check preview of var2 before using in And
And(var2, var3) �� var4
# Preview shows: "false (true AND yes)" - var3 is "yes" not "true"!
```

## Supported Input Values

### ? Recognized as True
- "true"
- "True"
- "TRUE"
- "TrUe"
- (any case variation of "true")

### ? Treated as False
- "false", "False", "FALSE"
- "" (empty string)
- null
- "yes", "YES"
- "1"
- "on", "ON"
- Any non-"true" string

## Error Handling

### Success
- Preview: `"true (true AND true)"` or `"false (true AND false)"`
- Returns: "true" or "false" string
- Always succeeds (no exceptions)

### Null/Empty Handling
- Null converted to empty string �� false
- Empty string �� false
- No exceptions thrown

## Performance Considerations
- **Fast**: Simple string comparison
- **No I/O**: Pure logic operation
- **Synchronous**: No delays
- **Lightweight**: Minimal memory

## Related Operations
- **IsMatch**: Generate boolean from comparison
- **IsAlmostMatch**: Generate boolean from fuzzy comparison
- **IsVisible**: Generate boolean from visibility check
- **PatientNumberMatch**: Generate boolean from PACS match
- **StudyDateTimeMatch**: Generate boolean from PACS match
- **WorklistIsVisible**: Generate boolean from PACS state

## Future Enhancements
- Consider adding **Or** operation (true if either true)
- Consider adding **Not** operation (invert boolean)
- Consider adding **Xor** operation (true if exactly one true)

## Example Procedures

### Example 1: Dual Field Validation
```
PACS Method: ValidateTwoFields

Step 1: GetText         Arg1: Field1 (Element) �� var1
Step 2: IsMatch         Arg1: var1 (Var)  Arg2: "expected1" (String) �� var2
Step 3: GetText         Arg1: Field2 (Element) �� var3
Step 4: IsMatch         Arg1: var3 (Var)  Arg2: "expected2" (String) �� var4
Step 5: And             Arg1: var2 (Var)  Arg2: var4 (Var) �� var5
Result: var5 = "true" if both fields match expected values
```

### Example 2: Safety Gate for Automation
```
PACS Method: SafetyCheckBeforeSend

Step 1: PatientNumberMatch �� var1
Step 2: StudyDateTimeMatch �� var2
Step 3: And             Arg1: var1 (Var)  Arg2: var2 (Var) �� var3
# Only proceed with send if var3 = "true"
```

### Example 3: Multi-Element Visibility
```
PACS Method: CheckAllButtonsVisible

Step 1: IsVisible       Arg1: SaveButton (Element) �� var1
Step 2: IsVisible       Arg1: SendButton (Element) �� var2
Step 3: IsVisible       Arg1: CloseButton (Element) �� var3
Step 4: And             Arg1: var1 (Var)  Arg2: var2 (Var) �� var4
Step 5: And             Arg1: var4 (Var)  Arg2: var3 (Var) �� var5
Result: var5 = "true" if all three buttons visible
```

### Example 4: Complex Validation Chain
```
PACS Method: ComplexValidation

Step 1: GetText         Arg1: PatientField (Element) �� var1
Step 2: IsMatch         Arg1: var1 (Var)  Arg2: "12345678" (String) �� var2

Step 3: WorklistIsVisible �� var3

Step 4: And             Arg1: var2 (Var)  Arg2: var3 (Var) �� var4

Step 5: GetText         Arg1: StudyField (Element) �� var5
Step 6: IsMatch         Arg1: var5 (Var)  Arg2: "CT Brain" (String) �� var6

Step 7: And             Arg1: var4 (Var)  Arg2: var6 (Var) �� var7
Result: var7 = "true" if patient matches AND worklist visible AND study matches
```

## Testing Recommendations
1. Test with both true inputs �� expect "true"
2. Test with one false input �� expect "false"
3. Test with both false inputs �� expect "false"
4. Test with empty string inputs �� expect "false"
5. Test with null inputs �� expect "false"
6. Test case variations ("True", "TRUE") �� expect "true"
7. Test with non-"true" strings �� expect "false"
8. Test chaining multiple Ands
9. Test in automation sequences
10. Test with different PACS procedures

## Build Status
- ? Build successful
- ? No warnings
- ? No breaking changes
- ? Follows existing patterns

## Documentation Updates
- ? Spec.md updated (FR-1270 through FR-1279)
- ? Enhancement document created
- ? Operation items list updated
- ? Operation configuration added

## Conclusion
The And operation provides boolean AND logic for combining multiple condition results, enabling complex validation chains without manual branching or nested procedures. Essential for building robust multi-condition automation workflows.

---

**Implementation Date**: 2025-11-09  
**Build Status**: ? Success  
**Ready for Use**: ? Yes
