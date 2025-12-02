# ENHANCEMENT: GetLongerText Operation

**Date**: 2025-12-02  
**Status**: ? Implemented  
**Type**: Feature Enhancement - New Operation

## Summary

Added new `GetLongerText` operation to the Automation Window's Custom Procedures tab. This operation compares two text strings and returns the longer one, useful for selecting the best variant from multiple PACS field sources.

## User Request

In the Automation Window ¡æ Procedures tab, create a new operation "GetLongerText" with two arguments that returns the longer of the two texts.

## Use Case

This operation directly addresses the pattern seen in `AddPreviousStudy` module where parallel fetching selects the longer variant:

```csharp
// OLD PATTERN (hardcoded in module)
var f1Task = Task.Run(async () => await _pacs.GetCurrentFindingsAsync());
var f2Task = Task.Run(async () => await _pacs.GetCurrentFindings2Async());
await Task.WhenAll(f1Task, f2Task);
var f1 = f1Task.Result ?? string.Empty;
var f2 = f2Task.Result ?? string.Empty;
findings = (f2.Length > f1.Length) ? f2 : f1; // Pick longer variant
```

Can now be implemented as a reusable custom procedure:

```
// NEW PATTERN (configurable procedure)
Operation 1: GetCurrentFindings ¡æ var1
Operation 2: GetCurrentFindings2 ¡æ var2
Operation 3: GetLongerText(var1, var2) ¡æ var3  // Returns longer text
```

## Implementation Details

### Operation Behavior

- **Name**: GetLongerText
- **Arguments**:
  - Arg1: First text (String or Var type)
  - Arg2: Second text (String or Var type)
  - Arg3: Disabled
- **Logic**: Compares string lengths, returns longer text (or first if equal)
- **Output**: The longer string value
- **Preview Format**: `{length} chars (text1: {len1}, text2: {len2})`

### Example Usage

#### Basic Comparison
```
Step 1: GetText(Field1) ¡æ var1 = "short"
Step 2: GetText(Field2) ¡æ var2 = "longer text"
Step 3: GetLongerText(var1, var2) ¡æ var3 = "longer text"
Preview: "11 chars (text1: 5, text2: 11)"
```

#### Report Fetching Pattern
```
Step 1: GetCurrentFindings ¡æ var1 (may be truncated)
Step 2: GetCurrentFindings2 ¡æ var2 (alternative source)
Step 3: GetLongerText(var1, var2) ¡æ var3 (best variant)
```

#### Empty String Handling
```
Step 1: GetText(EmptyField) ¡æ var1 = ""
Step 2: GetText(FilledField) ¡æ var2 = "content"
Step 3: GetLongerText(var1, var2) ¡æ var3 = "content"
Preview: "7 chars (text1: 0, text2: 7)"
```

## Technical Implementation

### Files Modified (4)

1. **OperationExecutor.StringOps.cs** (~15 lines added)
   ```csharp
   /// <summary>
   /// Compares two strings and returns the longer one.
   /// If both strings are equal in length, returns the first one.
   /// </summary>
   private static (string preview, string? value) ExecuteGetLongerText(string? text1, string? text2)
   {
       text1 ??= string.Empty;
       text2 ??= string.Empty;

       string longerText = text1.Length >= text2.Length ? text1 : text2;
       int len1 = text1.Length;
       int len2 = text2.Length;

       string preview = $"{longerText.Length} chars (text1: {len1}, text2: {len2})";
       return (preview, longerText);
   }
   ```

2. **OperationExecutor.cs** (1 case added)
   ```csharp
   case "GetLongerText":
       return ExecuteGetLongerText(resolveArg1String(), resolveArg2String());
   ```

3. **AutomationWindow.OperationItems.xaml** (1 line added)
   ```xml
   <ComboBoxItem Content="GetLongerText"/>
   ```

4. **AutomationWindow.Procedures.Exec.cs** (~6 lines added)
   ```csharp
   case "GetLongerText":
       // GetLongerText: Arg1=text1 (String or Var), Arg2=text2 (String or Var), returns longer text
       row.Arg1Enabled = true; // Allow String or Var
       row.Arg2Enabled = true; // Allow String or Var
       row.Arg3.Type = nameof(ArgKind.String); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
       break;
   ```

### Argument Configuration

| Argument | Type | Enabled | Description |
|----------|------|---------|-------------|
| Arg1 | String or Var | Yes | First text to compare |
| Arg2 | String or Var | Yes | Second text to compare |
| Arg3 | String | No | Unused |

### Type Flexibility

GetLongerText follows the flexible-type pattern like `IsMatch`, `Replace`, and `Merge`:
- ? Accepts both String literals and Var references
- ? No forced type conversion
- ? User-friendly for mixed scenarios

## Use Cases

### 1. Select Best Field Variant
```
GetText(Report_Findings_Main) ¡æ var1
GetText(Report_Findings_Alt) ¡æ var2
GetLongerText(var1, var2) ¡æ var_findings
```

**Benefit**: Automatically selects the most complete data source

### 2. PACS Field Redundancy
```
GetCurrentFindings ¡æ var1
GetCurrentFindings2 ¡æ var2
GetLongerText(var1, var2) ¡æ var_best_findings
```

**Benefit**: Handles PACS systems with multiple field sources

### 3. Fallback Logic
```
GetText(Primary_Field) ¡æ var1
GetText(Backup_Field) ¡æ var2
GetLongerText(var1, var2) ¡æ var_value
IsMatch(var_value, "") ¡æ var_is_empty
```

**Benefit**: Combined with conditional logic for robust data retrieval

### 4. Compare Literal to Variable
```
GetText(User_Input) ¡æ var1
GetLongerText(var1, "default text") ¡æ var2
```

**Benefit**: Ensure minimum content length

### 5. Merge Strategy
```
GetText(Field1) ¡æ var1
GetText(Field2) ¡æ var2
GetLongerText(var1, var2) ¡æ var_longer
Merge(var_longer, " (selected)") ¡æ var_output
```

**Benefit**: Annotate which source was selected

## Consistency with Existing Operations

### String Operations Family

| Operation | Arg1 | Arg2 | Arg3 | Purpose |
|-----------|------|------|------|---------|
| **GetLongerText** | String/Var | String/Var | - | Return longer text |
| IsMatch | String/Var | String/Var | - | Compare equality |
| IsAlmostMatch | String/Var | String/Var | - | Fuzzy compare |
| Replace | String/Var | String/Var | String/Var | Text replacement |
| Merge | String/Var | String/Var | String/Var | Concatenation |
| TrimString | String/Var | String/Var | - | Remove substring |

### Design Alignment

GetLongerText follows established patterns:
- ? Flexible String/Var argument types (like IsMatch)
- ? Two required arguments (like IsMatch, And)
- ? Clear preview format showing both inputs and result
- ? Null-safe implementation (treats null as empty string)

## Benefits

### For Module Authors
- ? **Reusable Logic**: Eliminate hardcoded length comparison
- ? **Testable**: Can test in Automation Window before deployment
- ? **Maintainable**: Configuration instead of code
- ? **Composable**: Combine with other operations

### For Users
- ? **Transparent**: Preview shows both lengths and result
- ? **Flexible**: Works with variables and literals
- ? **Intuitive**: Simple "return longer" semantics
- ? **Debuggable**: Clear output for troubleshooting

### For Automation
- ? **Performance**: No unnecessary string operations
- ? **Reliability**: Handles empty/null inputs gracefully
- ? **Extensibility**: Foundation for similar comparison operations

## Testing Recommendations

### Basic Functionality
- [ ] V1: GetLongerText("short", "longer text") ¡æ "longer text"
- [ ] V2: GetLongerText("same", "same") ¡æ "same" (first when equal)
- [ ] V3: GetLongerText("", "text") ¡æ "text"
- [ ] V4: GetLongerText("text", "") ¡æ "text"
- [ ] V5: GetLongerText("", "") ¡æ ""

### Variable Input
- [ ] V6: GetLongerText(var1, var2) ¡æ selects longer variable
- [ ] V7: GetLongerText(var1, "literal") ¡æ mixed types work
- [ ] V8: Preview shows correct lengths

### Integration
- [ ] V9: Combine with IsMatch for validation
- [ ] V10: Use in IF/ENDIF control flow
- [ ] V11: Chain with Merge operation
- [ ] V12: Works in headless ProcedureExecutor

### Edge Cases
- [ ] V13: Unicode characters counted correctly
- [ ] V14: Newlines/whitespace included in length
- [ ] V15: Very long texts (>10KB) handled

## Performance Characteristics

- **Time Complexity**: O(1) - simple length comparison
- **Space Complexity**: O(1) - no new allocations (returns reference)
- **Memory Impact**: Minimal - only stores length integers
- **Execution Time**: <1ms typical

## Future Enhancements (Not Implemented)

### Potential Extensions
1. **GetShorterText**: Complement operation (return shorter)
2. **GetLongerTextTrimmed**: Compare after trimming whitespace
3. **MaxLength**: Configurable maximum length parameter
4. **PreferFirst/PreferSecond**: Tiebreaker flag when equal length

### Advanced Variants
- **GetLongestText**: Accept 3+ arguments
- **GetTextByLength**: Return text matching specific length range
- **CompareLength**: Return comparison result ("longer", "shorter", "equal")

## Documentation References

- **Similar Operations**: `IsMatch`, `IsAlmostMatch` (dual-arg comparison)
- **Pattern Source**: `MainViewModel.Commands.AddPreviousStudy.cs` (original use case)
- **Type Flexibility**: Follows `BUGFIX_2025-11-09_IsMatchArgumentTypeForcing.md` pattern
- **Operation Catalog**: See `OperationExecutor.cs` for full operation list

## Related Code Pattern

### Before (Hardcoded in Module)
```csharp
// In MainViewModel.Commands.AddPreviousStudy.cs
var f1Task = Task.Run(async () => await _pacs.GetCurrentFindingsAsync());
var f2Task = Task.Run(async () => await _pacs.GetCurrentFindings2Async());
await Task.WhenAll(f1Task, f2Task);
var f1 = f1Task.Result ?? string.Empty;
var f2 = f2Task.Result ?? string.Empty;
findings = (f2.Length > f1.Length) ? f2 : f1; // HARDCODED LOGIC
```

### After (Configurable Procedure)
```
# Custom Procedure: FetchBestReportText
Step 1: GetCurrentFindings ¡æ var1
Step 2: GetCurrentFindings2 ¡æ var2
Step 3: GetLongerText(var1, var2) ¡æ var_findings
Step 4: GetCurrentConclusion ¡æ var3
Step 5: GetCurrentConclusion2 ¡æ var4
Step 6: GetLongerText(var3, var4) ¡æ var_conclusion
```

### Migration Path
1. Create "FetchBestReportText" custom procedure
2. Add GetCurrentFindings/GetCurrentFindings2 operations
3. Use GetLongerText to select best variant
4. Replace hardcoded module logic with procedure call
5. Test in Automation Window
6. Deploy to production

## Conclusion

The GetLongerText operation provides a simple, reusable way to select the longer of two text strings. This addresses a common pattern in PACS automation where multiple field sources may have different levels of completeness, and complements existing string operations like IsMatch and Merge.

The operation's flexible type system and clear preview format make it easy to use in both interactive testing (Automation Window) and automated execution (ProcedureExecutor), following the established patterns of the automation framework.

---

**Implementation Date**: 2025-12-02  
**Build Status**: ? Success  
**User Impact**: High (enables common field selection pattern)  
**Breaking Changes**: None  
**Migration Required**: None
