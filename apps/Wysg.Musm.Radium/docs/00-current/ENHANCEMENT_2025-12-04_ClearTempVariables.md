# Enhancement: ClearTempVariables Built-in Module (2025-12-04)

**Date**: 2025-12-04  
**Type**: Enhancement  
**Status**: ? Complete  
**Priority**: Normal

---

## Summary

Created a new built-in module "ClearTempVariables" that clears all temporary variables used for data exchange between custom procedures and built-in modules. This module provides a clean way to reset programmatic state without affecting UI fields or loaded studies.

## User Request

> "among the built in modules, is there code to clear preset variables? meaning 'current patient sth's, 'current study sth's. and 'previous study sth's?"
> 
> After discussion: "sure that would be better" (agreeing to create separate ClearTempVariables module instead of adding to ClearPreviousFields)

## Rationale

### Why Separate from ClearPreviousFields?

| Aspect | ClearPreviousFields | ClearTempVariables |
|--------|---------------------|-------------------|
| **Scope** | UI (visible editors) | Data (hidden variables) |
| **User Visible** | Yes (clears text boxes) | No (backend only) |
| **Purpose** | Clean up report editing UI | Data passing between modules |
| **Life Cycle** | Per-report editing session | Per-automation sequence |

### Design Benefits

1. **Separation of Concerns**: UI clearing vs data clearing are different operations
2. **Independence**: Users can clear editors without clearing temp data (or vice versa)
3. **Explicit Control**: Automation sequences can explicitly control when temp variables are cleared
4. **No Surprises**: Module names clearly indicate what they do
5. **Flexibility**: Mix and match clearing operations as needed

## Implementation

### Files Modified

#### 1. `MainViewModel.Commands.Automation.Core.cs`
**Changes**:
- Added ClearTempVariables module handler in `RunModulesSequentially` method

```csharp
else if (string.Equals(m, "ClearTempVariables", StringComparison.OrdinalIgnoreCase)) 
{ 
    await RunClearTempVariablesAsync(); 
}
```

#### 2. `MainViewModel.Commands.Automation.Database.cs`
**Changes**:
- Added `RunClearTempVariablesAsync()` implementation method

**Implementation**:
```csharp
private async Task RunClearTempVariablesAsync()
{
    try
    {
        Debug.WriteLine("[ClearTempVariables] ===== START =====");
        
        // Clear all temporary variables
        TempPreviousStudyStudyname = null;
        TempPreviousStudyDatetime = null;
        TempPreviousStudyReportDatetime = null;
        TempPreviousStudyReportReporter = null;
        TempPreviousStudyReportHeaderAndFindings = null;
        TempPreviousStudyReportConclusion = null;
        
        // Debug logging for each variable
        // Status message: "Temporary variables cleared"
        
        await Task.CompletedTask;
    }
    catch (Exception ex)
    {
        SetStatus($"ClearTempVariables error: {ex.Message}", true);
    }
}
```

#### 3. `SettingsViewModel.cs`
**Changes**:
- Added "ClearTempVariables" to `AvailableModules` list (positioned after ClearPreviousStudies)

```csharp
private ObservableCollection<string> availableModules = new(new[] { 
    ..., "ClearPreviousStudies", "ClearTempVariables", "SetCurrentStudyTechniques", ...
});
```

## Temporary Variables Cleared

The module clears **all six temporary variables** used for data exchange:

| Variable | Type | Purpose |
|----------|------|---------|
| `TempPreviousStudyStudyname` | string? | Studyname for FetchPreviousStudies/SetComparison |
| `TempPreviousStudyDatetime` | DateTime? | Study datetime for matching/comparison |
| `TempPreviousStudyReportDatetime` | DateTime? | Report datetime for specific report selection |
| `TempPreviousStudyReportReporter` | string? | Reporter name for report metadata |
| `TempPreviousStudyReportHeaderAndFindings` | string? | Report header and findings text |
| `TempPreviousStudyReportConclusion` | string? | Report conclusion text |

These variables are defined in `MainViewModel.CurrentStudy.cs`:
```csharp
public string? TempPreviousStudyStudyname { get; set; }
public DateTime? TempPreviousStudyDatetime { get; set; }
public DateTime? TempPreviousStudyReportDatetime { get; set; }
public string? TempPreviousStudyReportReporter { get; set; }
public string? TempPreviousStudyReportHeaderAndFindings { get; set; }
public string? TempPreviousStudyReportConclusion { get; set; }
```

## Use Cases

### Use Case 1: Clean State Before Fetching New Previous Study

```
Problem: Previous automation left temp variables set to old values

Automation Sequence:
1. ClearTempVariables           (clear old state)
2. Custom Procedure:
   - Set "Previous Study Studyname" = "CT Brain"
   - Set "Previous Study Datetime" = 2025-11-15
3. FetchPreviousStudies         (fetch with fresh variables)
4. SetComparison                (set comparison using fresh variables)

Result: Clean execution with no stale data interference
```

### Use Case 2: Prevent Stale Variables from Affecting Subsequent Operations

```
Scenario: First study sets temp variables, second study should not use them

Automation Sequence (First Study):
1. Custom Procedure: Set temp variables for Study A
2. FetchPreviousStudies
3. SetComparison
4. ClearTempVariables           (clean up after use)

Automation Sequence (Second Study):
1. FetchPreviousStudies          (no temp variables set - loads all studies)
2. Manual selection              (user picks study from UI)

Result: Second study not affected by first study's temp variables
```

### Use Case 3: Conditional Clearing Based on Logic

```
Automation Sequence:
1. Custom Procedure: Set temp variables
2. FetchPreviousStudies
3. Custom Procedure: Check if study found
   - If found:
     - SetComparison
     - ClearTempVariables
   - If not found:
     - Log error
     - ClearTempVariables (cleanup even on failure)
   - End if

Result: Temp variables always cleared, regardless of success/failure
```

### Use Case 4: Multiple Fetches in Same Automation

```
Scenario: Need to fetch different previous studies in sequence

Automation Sequence:
1. Custom Procedure: Set "Previous Study Studyname" = "CT Chest"
2. FetchPreviousStudies
3. Save report ID to variable
4. ClearTempVariables                    (clear for next fetch)
5. Custom Procedure: Set "Previous Study Studyname" = "MR Brain"
6. FetchPreviousStudies
7. Save report ID to variable
8. ClearTempVariables                    (cleanup)

Result: Each fetch gets clean temp variables, no cross-contamination
```

### Use Case 5: Debugging - Clear and Re-run

```
During Development:
1. Run automation (temp variables set)
2. Automation fails midway
3. ClearTempVariables                    (manual cleanup)
4. Fix procedure logic
5. Re-run automation from beginning

Result: Clean slate for debugging, no leftover state
```

## Comparison with Related Modules

| Module | What It Clears | When to Use |
|--------|---------------|-------------|
| **ClearCurrentFields** | Current report editors (findings, conclusion, etc.) | Clean current report UI |
| **ClearPreviousFields** | Previous report editors | Clean previous report UI |
| **ClearPreviousStudies** | Previous study tabs/collection | Remove all loaded previous studies |
| **ClearTempVariables** | Temp variables (data exchange) | Reset programmatic state |

### Example: Complete Cleanup

```
Automation Sequence (Full Reset):
1. ClearCurrentFields           (clear current UI)
2. ClearPreviousFields          (clear previous UI)
3. ClearPreviousStudies         (clear loaded studies)
4. ClearTempVariables           (clear temp data)

Result: Complete reset - UI, data, and state all cleared
```

### Example: Selective Cleanup

```
Automation Sequence (Keep UI, Clear Data):
1. ClearTempVariables           (clear temp data only)
2. Custom Procedure: Set new temp variables
3. SetComparison                (update comparison without reloading UI)

Result: UI unchanged, only backend state reset
```

## Behavior

### What Gets Cleared

? `TempPreviousStudyStudyname` ¡æ `null`  
? `TempPreviousStudyDatetime` ¡æ `null`  
? `TempPreviousStudyReportDatetime` ¡æ `null`  
? `TempPreviousStudyReportReporter` ¡æ `null`  
? `TempPreviousStudyReportHeaderAndFindings` ¡æ `null`  
? `TempPreviousStudyReportConclusion` ¡æ `null`  
+? `TempHeader` ¡æ `null`
@@
[ClearTempVariables] - TempPreviousStudyReportDatetime = null
[ClearTempVariables] - TempPreviousStudyReportReporter = null
[ClearTempVariables] - TempPreviousStudyReportHeaderAndFindings = null
[ClearTempVariables] - TempPreviousStudyReportConclusion = null
+[ClearTempVariables] - TempHeader = null
[ClearTempVariables] ===== END: SUCCESS =====
```

## Error Handling

The module is extremely simple and has minimal error surface. Potential errors:

### Error 1: Exception During Clearing
```csharp
catch (Exception ex)
{
    SetStatus($"ClearTempVariables error: {ex.Message}", true);
}
```

**Note**: This is highly unlikely since we're just setting properties to null. The try-catch is defensive programming.

## Performance

- **Execution Time**: <1ms (instant)
- **Memory**: Releases references (allows GC if needed)
- **Side Effects**: None (pure data clearing)
- **Thread Safety**: Safe (runs on UI thread)

## Testing Checklist

**Test Case 1: Basic Clear**
1. Set all temp variables via custom procedure
2. Verify variables are set (check debug output)
3. Run "ClearTempVariables" module
4. Verify all variables are null
5. ? Expected: All 6 variables set to null

**Test Case 2: Clear Already-Empty Variables**
1. Ensure no temp variables are set (fresh state)
2. Run "ClearTempVariables" module
3. Verify no errors
4. ? Expected: No errors, status message shown

**Test Case 3: Clear Then Set Again**
1. Set temp variables
2. Run "ClearTempVariables"
3. Set temp variables again (different values)
4. Run "FetchPreviousStudies"
5. ? Expected: Second set of values used correctly

**Test Case 4: Use in Automation Sequence**
1. Create automation: SetVars ¡æ FetchPreviousStudies ¡æ ClearTempVariables
2. Run automation
3. Verify variables cleared at end
4. Run automation again
5. ? Expected: No interference between runs

**Test Case 5: Error Recovery**
1. Set temp variables
2. FetchPreviousStudies fails (e.g., patient not in DB)
3. Run ClearTempVariables
4. ? Expected: Variables cleared even after failure

## Integration with Other Modules

### Modules That USE Temp Variables

| Module | Variables Used | Purpose |
|--------|---------------|---------|
| **FetchPreviousStudies** | Studyname, Datetime, ReportDatetime | Select specific study/report |
| **SetComparison** | Studyname, Datetime | Build comparison string |

**Best Practice**: Call `ClearTempVariables` after these modules complete to prevent state leakage.

### Modules That SET Temp Variables

- **Custom Procedures**: Use "Set var" operations to populate temp variables
- **User Code**: Can set variables programmatically

**Best Practice**: Call `ClearTempVariables` BEFORE setting new variables to ensure clean state.

## Example Automation Sequences

### Pattern 1: Fetch ¡æ Use ¡æ Clear
```
1. Custom Procedure: Set temp variables
2. FetchPreviousStudies
3. SetComparison
4. ClearTempVariables
```

### Pattern 2: Clear ¡æ Fetch ¡æ Use
```
1. ClearTempVariables        (ensure clean state)
2. Custom Procedure: Set temp variables
3. FetchPreviousStudies
4. SetComparison
```

### Pattern 3: Conditional Clear
```
1. Custom Procedure: Set temp variables
2. FetchPreviousStudies
3. Custom Procedure: Check result
   - If success: SetComparison
   - If failure: Log error
   - End if
4. ClearTempVariables        (always clear)
```

### Pattern 4: Multiple Operations
```
1. ClearTempVariables
2. Custom Procedure: Set temp vars for Study A
3. FetchPreviousStudies
4. Custom Procedure: Extract data
5. ClearTempVariables        (clear before next operation)
6. Custom Procedure: Set temp vars for Study B
7. FetchPreviousStudies
8. Custom Procedure: Compare A and B
9. ClearTempVariables        (final cleanup)
```

## Benefits

### For Users
- **Clean State**: Explicit control over when temp variables are cleared
- **No Side Effects**: Doesn't affect UI or loaded studies
- **Debugging Aid**: Can clear state manually during development
- **Predictable**: Always clears the same 6 variables

### For Developers
- **Separation of Concerns**: Data clearing independent of UI clearing
- **Reusability**: Can be called from any automation sequence
- **Testability**: Simple to test (just check variables are null)
- **Maintainability**: Centralized clearing logic

### For Automation Design
- **Clean Workflows**: Prevent state contamination between operations
- **Error Recovery**: Clear state after failures
- **Multi-Step Operations**: Reset between different fetch operations
- **Debugging**: Known clean state for testing

## Related Documentation

- **SetComparison Module**: `ENHANCEMENT_2025-12-04_SetComparison.md` (uses temp variables)
- **FetchPreviousStudies Module**: `ENHANCEMENT_2025-12-03_FetchPreviousStudies.md` (uses temp variables)
- **Temp Variables Definition**: `MainViewModel.CurrentStudy.cs` (variable declarations)

## Future Enhancements

Potential improvements:
1. Add parameter to selectively clear specific variables (e.g., clear only studyname)
2. Add return value indicating which variables were cleared (for logging)
3. Add option to backup variables before clearing (for undo functionality)
4. Add validation to warn if clearing variables that are about to be used

## Known Limitations

1. **No Undo**: Once cleared, variables cannot be restored (by design)
2. **No Validation**: Doesn't warn if clearing variables that FetchPreviousStudies needs
3. **All or Nothing**: Clears all 6 variables (no selective clearing)

## Notes

- The module is **idempotent**: Calling it multiple times has the same effect as calling it once
- Variables persist across automation runs until explicitly cleared or overwritten
- Custom procedures can read temp variables after they've been set (before clearing)
- The module does NOT affect `CurrentReportDateTime` (different lifecycle)

## Design Principles

1. **Explicit is Better Than Implicit**: User explicitly chooses when to clear
2. **Separation of Concerns**: Data clearing separate from UI clearing
3. **Single Responsibility**: Module does one thing (clear temp variables)
4. **Defensive Programming**: Try-catch even though errors are unlikely
5. **Comprehensive Logging**: Debug output shows exactly what was cleared

## Files Summary

### Created
- `apps/Wysg.Musm.Radium/docs/00-current/ENHANCEMENT_2025-12-04_ClearTempVariables.md` (this document)

### Modified
- `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.Automation.Core.cs` - Added module handler
- `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.Automation.Database.cs` - Added implementation
- `apps/Wysg.Musm.Radium/ViewModels/SettingsViewModel.cs` - Added to AvailableModules

## Build Status

? **Build Successful** - No errors, no warnings

---

**Implementation Date**: 2025-12-04  
**Build Status**: ? Success  
**Ready for Use**: ? Complete

---

*New built-in module successfully implemented. Users can now explicitly clear temporary variables to maintain clean state between automation operations.*
