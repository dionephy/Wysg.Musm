# Bugfix: IsMatch and IsAlmostMatch Argument Type Forcing (2025-02-09)

## Problem
The IsMatch and IsAlmostMatch operations were forcing both Arg1 and Arg2 to be Var type when users clicked "Set" or "Run" buttons, preventing them from using String literals for comparison values.

### Observed Behavior
1. User creates IsMatch operation with Arg2 as String: `IsMatch(var1, "expected")`
2. User clicks "Set" or "Run" button
3. Arg2.Type is forced from String to Var
4. User's String literal "expected" is now treated as a variable name
5. Operation fails or produces unexpected results

### Impact
- ? Unable to compare variables to literal strings
- ? Forced to create temporary variables for constant values
- ? Confusing user experience (argument types change unexpectedly)
- ? Breaks common validation patterns like `IsMatch(var1, "true")`

## Root Cause

### Before Fix
In `SpyWindow.Procedures.Exec.cs`, the `OnProcOpChanged` method:

```csharp
case "IsMatch":
    row.Arg1.Type = nameof(ArgKind.Var); row.Arg1Enabled = true;
    row.Arg2.Type = nameof(ArgKind.Var); row.Arg2Enabled = true;
    row.Arg3.Type = nameof(ArgKind.String); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
    break;
case "IsAlmostMatch":
    row.Arg1.Type = nameof(ArgKind.Var); row.Arg1Enabled = true;
    row.Arg2.Type = nameof(ArgKind.Var); row.Arg2Enabled = true;
    row.Arg3.Type = nameof(ArgKind.String); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
    break;
```

The code explicitly set `row.Arg1.Type = nameof(ArgKind.Var)` and `row.Arg2.Type = nameof(ArgKind.Var)`, overwriting any user-selected type.

### Why This Was Wrong
- Operations like Replace and Merge already support flexible argument types (don't force types)
- IsMatch is commonly used to compare variables to literal values: `IsMatch(var1, "expected")`
- The type forcing broke this common pattern

## Solution

### After Fix
```csharp
case "IsMatch":
case "IsAlmostMatch":
    // IsMatch/IsAlmostMatch: Arg1=input (Var), Arg2=comparison value (String or Var)
    // Don't force Arg2 type - let user choose between String literal and Var
    row.Arg1Enabled = true; // Typically Var but could be String
    row.Arg2Enabled = true; // Allow String or Var
    row.Arg3.Type = nameof(ArgKind.String); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
    break;
```

### Key Changes
1. ? Remove `row.Arg1.Type = nameof(ArgKind.Var)` - don't force Arg1 type
2. ? Remove `row.Arg2.Type = nameof(ArgKind.Var)` - don't force Arg2 type
3. ? Only enable/disable arguments, preserve user's type selection
4. ? Follows pattern used by Replace and Merge operations

## Technical Details

### Files Modified
**File**: `apps\Wysg.Musm.Radium\Views\SpyWindow.Procedures.Exec.cs`

**Method**: `OnProcOpChanged`

**Changes**: Removed type assignment for Arg1 and Arg2 in IsMatch and IsAlmostMatch cases

### Affected Operations
- **IsMatch**: Compare two values for exact equality
- **IsAlmostMatch**: Compare two values for approximate equality (fuzzy match)

### Not Affected
Other operations continue to work as designed:
- Operations requiring specific types (e.g., Split forces Arg1=Var, Arg2=String)
- Operations allowing flexible types (e.g., Replace, Merge)

## Use Cases Now Supported

### 1. Compare Variable to Literal String
```
GetText(TextField) ¡æ var1
IsMatch(var1, "expected value")
Result: "true" or "false"
```

### 2. Validate Boolean Results
```
IsVisible(Element) ¡æ var1
IsMatch(var1, "true")
Result: "true" if element visible, "false" otherwise
```

### 3. Check Empty String
```
GetText(TextField) ¡æ var1
IsMatch(var1, "")
Result: "true" if field empty
```

### 4. Compare Two Variables
```
GetText(Field1) ¡æ var1
GetText(Field2) ¡æ var2
IsMatch(var1, var2)
Result: "true" if fields match
```

### 5. Constant Validation
```
GetCurrentPatientNumber ¡æ var1
IsMatch(var1, "12345678")
Result: "true" if patient number is exactly "12345678"
```

## Testing

### Before Fix
```
Step 1: GetText(Field) ¡æ var1
Step 2: IsMatch  Arg1: var1 (Var)  Arg2: "expected" (String)
Step 3: Click "Set" button
Result: Arg2 changes from String to Var (BUG)
```

### After Fix
```
Step 1: GetText(Field) ¡æ var1
Step 2: IsMatch  Arg1: var1 (Var)  Arg2: "expected" (String)
Step 3: Click "Set" button
Result: Arg2 remains String (FIXED)
```

## Verification

### Manual Test
1. ? Create IsMatch with String literal in Arg2
2. ? Click "Set" button
3. ? Arg2 remains String type
4. ? Operation executes correctly
5. ? Result is "true" or "false" as expected

### Integration Test
1. ? IsMatch with Var to String comparison works
2. ? IsMatch with Var to Var comparison works
3. ? IsMatch with String to String comparison works
4. ? IsAlmostMatch follows same behavior
5. ? Existing procedures continue to work

## Migration Notes

### No Breaking Changes
- ? Existing procedures with IsMatch/IsAlmostMatch continue to work
- ? Saved argument types are preserved
- ? No user action required

### Improved Flexibility
- ? Can now use String literals for comparison
- ? Can still use Var to Var comparisons
- ? More intuitive user experience

## Best Practices

### ? Recommended Patterns
```
# Good: Compare to literal
IsMatch(var1, "expected")

# Good: Compare to variable
IsMatch(var1, var2)

# Good: Validate boolean
IsMatch(var1, "true")

# Good: Check empty
IsMatch(var1, "")
```

### Pattern Comparison
| Pattern | Before Fix | After Fix |
|---------|-----------|-----------|
| IsMatch(var1, "text") | ? Broken | ? Works |
| IsMatch(var1, var2) | ? Works | ? Works |
| IsAlmostMatch(var1, "text") | ? Broken | ? Works |
| IsAlmostMatch(var1, var2) | ? Works | ? Works |

## Related Operations

### Similar Pattern (Already Correct)
- **Replace**: Arg1=Var, Arg2=String/Var, Arg3=String/Var (flexible)
- **Merge**: Arg1=String/Var, Arg2=String/Var, Arg3=String/Var (flexible)
- **TrimString**: Arg1=String/Var, Arg2=String/Var (flexible)

### Different Pattern (Require Specific Types)
- **Split**: Arg1=Var (forced), Arg2=String (forced), Arg3=Number (forced)
- **GetText**: Arg1=Element (forced)
- **SetValue**: Arg1=Element (forced), Arg2=String/Var (flexible)

## Comparison with Other Fixes

This bugfix follows the pattern of the earlier SetClipboard fix:

### SetClipboard Fix (Earlier)
```csharp
case "SetClipboard":
    // FIX: SetClipboard accepts both String and Var types - don't force to String
    // Only enable Arg1, keep user's Type selection
    row.Arg1Enabled = true;
```

### IsMatch Fix (Now)
```csharp
case "IsMatch":
case "IsAlmostMatch":
    // Don't force Arg2 type - let user choose between String literal and Var
    row.Arg1Enabled = true;
    row.Arg2Enabled = true;
```

Both fixes address the same root cause: operations forcing argument types when they should be flexible.

## Future Considerations

### Review Other Operations
Consider reviewing all operations for similar issues:
- ? SetClipboard - already fixed
- ? IsMatch - fixed in this bugfix
- ? IsAlmostMatch - fixed in this bugfix
- ? Replace - already correct
- ? Merge - already correct

### Guidelines for New Operations
When adding new operations:
1. Only force argument types if absolutely required (e.g., Split needs specific types)
2. Allow flexible String/Var types whenever possible
3. Document which arguments are flexible in code comments
4. Test that "Set" and "Run" buttons preserve user's type selection

## Build Status
- ? Build successful
- ? No warnings
- ? No breaking changes
- ? Backward compatible

## Documentation Updates
- ? Spec.md updated (BUG-1260 through FIX-1265)
- ? Bugfix document created
- ? Operation behavior clarified

## Conclusion
The IsMatch and IsAlmostMatch operations now correctly preserve user-selected argument types, allowing flexible comparison between variables and literal strings. This fix resolves the confusing behavior where String arguments were unexpectedly converted to Var type, and enables common validation patterns like comparing a variable to an expected literal value.

---

**Fix Date**: 2025-02-09  
**Build Status**: ? Success  
**Breaking Changes**: ? None  
**User Action Required**: ? None (automatic fix)
