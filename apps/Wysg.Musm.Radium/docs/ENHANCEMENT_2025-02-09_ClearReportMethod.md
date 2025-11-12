# Enhancement: New PACS Method - ClearReport (2025-02-09)

## Overview
Added a new "Clear report" PACS method to the UI Spy window Custom Procedures section, enabling automated clearing of report text fields in PACS systems.

## Changes Made

### 1. SpyWindow.PacsMethodItems.xaml
**Path**: `apps\Wysg.Musm.Radium\Views\SpyWindow.PacsMethodItems.xaml`

**Changes**:
- Added `<ComboBoxItem Tag="ClearReport">Clear report</ComboBoxItem>`
- Placed under "Report Actions" section after SendReportRetry

**Impact**: Users can now select "Clear report" from the PACS Method dropdown in Custom Procedures.

### 2. PacsService.cs
**Path**: `apps\Wysg.Musm.Radium\Services\PacsService.cs`

**Changes**:
Added new async wrapper method:
```csharp
// NEW: Clear report (runs ClearReport custom procedure)
public async Task<bool> ClearReportAsync()
{
    await ExecCustom("ClearReport");
    return true;
}
```

**Impact**: Automation code can now call this method to execute configured PACS clear report procedures.

## Feature Specifications

### Method Signature
- **PACS Method Tag**: `ClearReport`
- **Display Name**: "Clear report"
- **C# Method**: `ClearReportAsync()`
- **Returns**: `Task<bool>` (always true after execution)
- **Configuration**: Per-PACS custom procedure (no auto-seed)

## Use Cases

### 1. Clear Report Before Edit
When correcting an existing report, clear all fields first:
```
PACS Method: ClearReport

Step 1: SetFocus  Arg1: FindingsField
Step 2: SetValue  Arg1: FindingsField    Arg2: "" (String)
Step 3: SetFocus  Arg1: ConclusionField
Step 4: SetValue  Arg1: ConclusionField  Arg2: "" (String)
```

### 2. Reset Report After Error
Clear report if send operation fails:
```
Automation Sequence:
1. InvokeSendReport
2. If error detected ¡æ ClearReport
3. Restart report process
```

### 3. Clear Before Copy
Clear existing report before copying from template:
```
PACS Method: PrepareReportFromTemplate

Step 1: ClearReport
Step 2: GetText from template ¡æ var1
Step 3: SetValue to findings  Arg2: var1
```

### 4. Multiple Field Clear
Clear all report-related fields:
```
PACS Method: ClearReport

Step 1: SetValue  Arg1: FindingsField       Arg2: ""
Step 2: SetValue  Arg1: ConclusionField     Arg2: ""
Step 3: SetValue  Arg1: RecommendationField Arg2: ""
Step 4: SetValue  Arg1: AddendumField       Arg2: ""
```

## Configuration Guide

### Step-by-Step Setup in UI Spy

1. **Open UI Spy**
   - Tools ¡æ UI Spy or SpyWindow.ShowInstance()

2. **Select PACS Profile**
   - Ensure correct PACS selected in top-left dropdown

3. **Configure Clear Report Procedure**
   - Navigate to Custom Procedures section
   - Select "Clear report" from PACS Method dropdown
   - Click "Add" to add operation steps

4. **Add Clear Operations**
   ```
   Operation: SetFocus   Arg1: FindingsField (Element)
   Operation: SetValue   Arg1: FindingsField (Element)   Arg2: "" (String)
   Operation: SetFocus   Arg1: ConclusionField (Element)
   Operation: SetValue   Arg1: ConclusionField (Element) Arg2: "" (String)
   ```

5. **Save and Test**
   - Click "Save" to persist configuration
   - Click "Run" to test procedure
   - Verify fields are cleared in PACS

## Common Implementations

### Minimal Clear (Findings + Conclusion)
```
SetValue(FindingsField, "")
SetValue(ConclusionField, "")
```

### Standard Clear (with Focus)
```
SetFocus(FindingsField)
SetValue(FindingsField, "")
Delay(50)
SetFocus(ConclusionField)
SetValue(ConclusionField, "")
```

### Complete Clear (All Fields)
```
SetValue(HeaderField, "")
SetValue(FindingsField, "")
SetValue(ConclusionField, "")
SetValue(RecommendationField, "")
SetValue(AddendumField, "")
SetValue(NotesField, "")
```

### Clear with Confirmation
```
SetValue(FindingsField, "")
GetText(FindingsField) ¡æ var1
IsMatch(var1, "") ¡æ var2
# var2 = "true" if cleared successfully
```

## Integration with Automation

### In Automation Sequences
```json
// automation.json
{
  "ResetReportSequence": "ClearReport,Delay,GetStudyRemark,GetPatientRemark"
}
```

### From C# Code
```csharp
var pacs = new PacsService();

// Clear report before starting
await pacs.ClearReportAsync();

// Continue with report generation
await GenerateNewReportAsync();
```

### Error Recovery Workflow
```csharp
try
{
    await pacs.InvokeSendReportAsync();
}
catch (Exception)
{
    // Clear report and retry
    await pacs.ClearReportAsync();
    await Task.Delay(500);
    await pacs.InvokeSendReportAsync();
}
```

## Comparison with Manual Clearing

| Aspect | ClearReport Method | Manual Clearing |
|--------|-------------------|-----------------|
| Speed | ? Instant | ?? Slow |
| Reliability | ? Consistent | ?? Error-prone |
| Repeatability | ? Perfect | ? Varies |
| Documentation | ? Procedure file | ? None |
| Testing | ? Run button | ? Manual only |

## Best Practices

### ? Do:
- Include SetFocus before SetValue for reliability
- Clear all relevant fields (findings, conclusion, etc.)
- Add small delays (50-100ms) between operations
- Test procedure with actual PACS data
- Verify fields are cleared using GetText
- Document which fields are cleared

### ? Don't:
- Forget to clear hidden/secondary fields
- Use hardcoded mouse clicks (use SetValue)
- Skip verification after clearing
- Clear fields that should be preserved
- Use same procedure for different PACS systems

## Troubleshooting

### Issue: Fields not clearing
**Cause**: Element not found or read-only
**Fix**: 
- Verify bookmark resolves (use "Resolve" button)
- Check field is not read-only
- Add SetFocus before SetValue

### Issue: Some fields cleared, others not
**Cause**: Missing fields in procedure
**Fix**:
- List all report fields in PACS
- Add SetValue for each field
- Test each operation individually

### Issue: Clear succeeds but fields reappear
**Cause**: PACS validation or auto-fill
**Fix**:
- Add longer delay after clear
- Disable auto-fill if possible
- Clear again after delay

## Technical Details

### Method Implementation
```csharp
public async Task<bool> ClearReportAsync()
{
    await ExecCustom("ClearReport");
    return true;
}
```

### Execution Flow
1. User/automation calls `ClearReportAsync()`
2. PacsService calls `ExecCustom("ClearReport")`
3. ProcedureExecutor loads procedure from ui-procedures.json
4. Operations executed sequentially
5. Returns true (success based on PACS state)

### Debug Logging
```
[PacsService][ExecCustom] Executing procedure: ClearReport
[SetValue] Element resolved: Name='FindingsField'...
[SetValue] Value to set: '' (length=0)
[SetValue] SUCCESS: Value set to ''
[PacsService][ExecCustom] Result for ClearReport: ''
```

## Related Features
- **SetValue**: Used to clear fields (set to empty string)
- **SetFocus**: Prepares field before clearing
- **GetText**: Validates field is cleared
- **InvokeSendReport**: Send report after clearing and refilling
- **IsMatch**: Verify empty string after clear

## Related PACS Methods
- **SendReport**: Send report after clearing and editing
- **InvokeSendReport**: Primary send action
- **SendReportRetry**: Retry send if failed
- **GetCurrentFindings**: Read findings before clearing
- **GetCurrentConclusion**: Read conclusion before clearing

## Testing Checklist
- [ ] Procedure clears findings field
- [ ] Procedure clears conclusion field
- [ ] Procedure clears all secondary fields
- [ ] Fields remain cleared after procedure
- [ ] No validation errors from PACS
- [ ] Procedure works from Run button
- [ ] Procedure works in automation
- [ ] Debug logging shows success

## Build Status
- ? Build successful
- ? No compilation errors
- ? No warnings
- ? Backward compatible

## Documentation Updates
- ? Spec.md updated (FR-1220 through FR-1226)
- ? Enhancement document created
- ? XAML resource updated
- ? PacsService updated

## Future Enhancements
- Consider adding ClearReport automation module
- Add "Clear and Reset" combination method
- Support selective field clearing (parameters)
- Add undo/restore functionality

## Conclusion
The ClearReport PACS method provides a standardized, reliable way to clear report text fields in PACS systems. Users configure the procedure once per PACS profile and can then use it consistently in manual testing (SpyWindow) and automated workflows (MainViewModel automation).

---

**Implementation Date**: 2025-02-09  
**Build Status**: ? Success  
**Ready for Use**: ? Yes
