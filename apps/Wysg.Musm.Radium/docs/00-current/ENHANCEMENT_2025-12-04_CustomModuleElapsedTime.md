# Enhancement: Custom Module Elapsed Time Tracking

**Date**: 2025-12-04  
**Type**: Enhancement  
**Component**: Custom Module Execution Status Logging  
**Status**: ? Complete  
**Build**: ? Success

---

## Summary

Added elapsed time tracking (in milliseconds) to all custom module execution status log messages. This provides visibility into how long each custom module takes to execute, helping users identify performance bottlenecks in their automation workflows.

---

## Problem Statement

Previously, custom module execution status messages did not include timing information:

```
[MyCustomModule] Done.
[GetPatientName] Current Patient Name = John Doe
[CheckCondition] Condition met.
```

This made it difficult to:
- Identify slow custom modules
- Debug performance issues in automation sequences
- Understand which procedures are taking the most time

---

## Solution

Added `Stopwatch` timing to all custom module execution paths, appending elapsed time in milliseconds to each status message:

```
[MyCustomModule] Done. (245 ms)
[GetPatientName] Current Patient Name = John Doe (512 ms)
[CheckCondition] Condition met. (178 ms)
```

---

## Implementation Details

### Files Modified

| File | Changes |
|------|---------|
| `MainViewModel.Commands.Automation.Custom.cs` | Added `Stopwatch` timing to `RunCustomModuleAsync` method |
| `MainViewModel.Commands.Automation.Core.cs` | Added `Stopwatch` timing to If/If not module handling |

### Code Changes

#### 1. MainViewModel.Commands.Automation.Custom.cs

Added timing to `RunCustomModuleAsync` method:

```csharp
private async Task RunCustomModuleAsync(CustomModule module)
{
    var sw = Stopwatch.StartNew();
    
    try
    {
        var result = await Services.ProcedureExecutor.ExecuteAsync(module.ProcedureName);
        sw.Stop();
        
        switch (module.Type)
        {
            case CustomModuleType.Run:
                SetStatus($"[{module.Name}] Done. ({sw.ElapsedMilliseconds} ms)");
                break;
                
            case CustomModuleType.AbortIf:
                if (shouldAbort)
                    SetStatus($"[{module.Name}] Aborted sequence. ({sw.ElapsedMilliseconds} ms)", true);
                else
                    SetStatus($"[{module.Name}] Condition not met, continuing. ({sw.ElapsedMilliseconds} ms)");
                break;
                
            case CustomModuleType.Set:
                SetStatus($"[{module.Name}] {module.PropertyName} = {formattedValue} ({sw.ElapsedMilliseconds} ms)");
                break;
        }
    }
    catch (Exception ex)
    {
        sw.Stop();
        SetStatus($"[{module.Name}] Error: {ex.Message} ({sw.ElapsedMilliseconds} ms)", true);
        throw;
    }
}
```

#### 2. MainViewModel.Commands.Automation.Core.cs

Added timing to If/If not module handling:

```csharp
if (customModule.Type == CustomModuleType.If || 
    customModule.Type == CustomModuleType.IfNot)
{
    var ifSw = Stopwatch.StartNew();
    
    var result = await Services.ProcedureExecutor.ExecuteAsync(customModule.ProcedureName);
    
    ifSw.Stop();
    
    // ... condition evaluation ...
    
    SetStatus($"[{customModule.Name}] {(conditionMet ? "Condition met." : "Condition not met.")} ({ifSw.ElapsedMilliseconds} ms)");
}
```

---

## Module Types Affected

| Module Type | Status Message Format |
|-------------|----------------------|
| **Run** | `[ModuleName] Done. (N ms)` |
| **Set** | `[ModuleName] PropertyName = value (N ms)` |
| **AbortIf** (abort) | `[ModuleName] Aborted sequence. (N ms)` |
| **AbortIf** (continue) | `[ModuleName] Condition not met, continuing. (N ms)` |
| **If** (met) | `[ModuleName] Condition met. (N ms)` |
| **If** (not met) | `[ModuleName] Condition not met. (N ms)` |
| **If not** (met) | `[ModuleName] Condition met. (N ms)` |
| **If not** (not met) | `[ModuleName] Condition not met. (N ms)` |
| **Error** | `[ModuleName] Error: message (N ms)` |

---

## Example Output

### Automation Sequence Log
```
[AbortIfPatientNotFound] Condition not met, continuing. (312 ms)
[GetPatientName] Current Patient Name = Smith, John (478 ms)
[GetPatientNumber] Current Patient Number = 12345678 (156 ms)
[GetStudyDateTime] Current Study Datetime = 2025-12-04 10:30:00 (203 ms)
[CheckModality] Condition met. (89 ms)
[GetFindings] Done. (1247 ms)
>> NewStudy completed successfully
```

### Error Case
```
[GetPatientData] Error: Element not found (5023 ms)
Module 'GetPatientData' failed - procedure aborted
```

---

## Performance Impact

- **Overhead**: < 1¥ìs per module (Stopwatch creation and stop)
- **Memory**: 24 bytes per Stopwatch instance (auto-collected after method returns)
- **No impact** on actual module execution time

---

## Benefits

1. **Performance Visibility**: Users can see exactly how long each module takes
2. **Bottleneck Identification**: Easy to spot slow modules in the status log
3. **Debugging**: Timing data helps diagnose timeout issues
4. **Optimization**: Users can focus optimization efforts on slowest modules
5. **Consistency**: All custom module types now report timing uniformly

---

## Related Documentation

- `ENHANCEMENT_2025-11-02_PacsModuleTimingLogs.md`: Similar timing for ProcedureExecutor
- `ENHANCEMENT_2025-12-01_ConciseAutomationStatusMessages.md`: Status message formatting

---

## Testing

### Manual Testing
1. Create custom modules of each type (Run, Set, AbortIf, If, If not)
2. Execute automation sequence containing custom modules
3. Verify status log shows elapsed time in milliseconds for each module
4. Verify error messages also include elapsed time

### Expected Results
- All custom module status messages show `(N ms)` at the end
- Timing values are reasonable (match actual execution duration)
- Format is consistent across all module types

---

## Completion Checklist

- [x] Added timing to Run module type
- [x] Added timing to Set module type
- [x] Added timing to AbortIf module type
- [x] Added timing to If module type
- [x] Added timing to If not module type
- [x] Added timing to error handling path
- [x] Build verification (0 errors)
- [x] Documentation created

---

**Status**: ? Complete  
**Build**: ? Success  
**Ready for Use**: ? Yes
