# Summary: InvokeSendReport and SendReportRetry PACS Methods Implementation

## Date: 2025-11-09

## Objective
Add two new PACS method items "InvokeSendReport" and "SendReportRetry" to the UI Spy window Custom Procedures section, enabling configurable send report automation with retry capabilities.

## Files Modified

### 1. SpyWindow.PacsMethodItems.xaml
**Path**: `apps\Wysg.Musm.Radium\Views\SpyWindow.PacsMethodItems.xaml`

**Changes**:
- Added `<ComboBoxItem Tag="InvokeSendReport">Invoke send report</ComboBoxItem>`
- Added `<ComboBoxItem Tag="SendReportRetry">Send report retry</ComboBoxItem>`
- Placed under "NEW: Send Report Actions" section after existing SendReport item

**Impact**: Users can now select these methods from the PACS Method dropdown in SpyWindow Custom Procedures tab.

### 2. PacsService.cs
**Path**: `apps\Wysg.Musm.Radium\Services\PacsService.cs`

**Changes**:
Added two new async wrapper methods:
```csharp
// NEW: Invoke send report (runs InvokeSendReport custom procedure)
public async Task<bool> InvokeSendReportAsync()
{
    await ExecCustom("InvokeSendReport");
    return true;
}

// NEW: Send report with retry (runs SendReportRetry custom procedure)
public async Task<bool> SendReportRetryAsync()
{
    await ExecCustom("SendReportRetry");
    return true;
}
```

**Impact**: Automation code can now call these methods to execute configured PACS send report procedures.

## Documentation Created

### 1. Enhancement Document
**Path**: `apps\Wysg.Musm.Radium\docs\ENHANCEMENT_2025-11-09_InvokeSendReportMethods.md`

**Contents**:
- Overview of new feature
- Detailed implementation changes
- User workflow guide
- Technical specifications
- Best practices for procedure configuration
- Testing recommendations
- Comparison with existing SendReport method

### 2. Spec.md Update
**Path**: `apps\Wysg.Musm.Radium\docs\Spec.md`

**Changes**:
- Added FR-1190 through FR-1198
- Documented feature requirements
- Specified method signatures and behavior
- Explained rationale and user workflow
- Noted relationship with existing SendReport method

## Feature Requirements Summary

| FR ID | Description |
|-------|-------------|
| FR-1190 | Add "Invoke send report" PACS method to SpyWindow |
| FR-1191 | Add "Send report retry" PACS method to SpyWindow |
| FR-1192 | PacsService exposes InvokeSendReportAsync() wrapper |
| FR-1193 | PacsService exposes SendReportRetryAsync() wrapper |
| FR-1194 | Both methods return Task<bool>, always true after execution |
| FR-1195 | Custom procedures configured per-PACS (no auto-seed) |
| FR-1196 | Rationale: distinct configurations for initial vs. retry |
| FR-1197 | User configures using bookmarks, operations, validation |
| FR-1198 | Complements existing SendReport with parameter-free entry points |

## How to Use

### Configuration (UI Spy Window)
1. Open UI Spy: Tools �� UI Spy or SpyWindow.ShowInstance()
2. Select PACS profile from top-left dropdown
3. Scroll down to "Custom Procedures" section
4. From "PACS Method" dropdown, select:
   - "Invoke send report" for primary send action
   - "Send report retry" for retry scenarios
5. Click "Add" to add operation steps:
   - Use GetText, ClickElement, Invoke, IsVisible operations
   - Configure bookmarks for UI elements
   - Add Delay operations for UI stabilization
6. Click "Save" to persist configuration
7. Click "Run" to test procedure execution

### Integration in Automation
```csharp
// From MainViewModel or automation sequence
var pacs = new PacsService();

// Primary send
await pacs.InvokeSendReportAsync();

// Retry scenario
if (sendFailed)
{
    await pacs.SendReportRetryAsync();
}
```

## Testing Status
- ? Build successful (no compilation errors)
- ? XAML resource dictionary correctly references new items
- ? PacsService methods follow existing patterns
- ? Documentation complete and consistent

## Next Steps for Users
1. Configure InvokeSendReport procedure for each PACS profile:
   - Identify send button bookmark
   - Add click/invoke operations
   - Add validation steps
2. Configure SendReportRetry procedure:
   - Add error recovery steps
   - Configure longer delays
   - Add UI state checks
3. Integrate into automation sequences:
   - Add to SendReportSequence in automation.json
   - Test with AbortIfPatientNumberNotMatch validation
   - Verify error handling behavior

## Compatibility Notes
- ? No breaking changes to existing code
- ? Existing SendReport procedures unaffected
- ? Per-PACS configuration isolation maintained
- ? Backward compatible with all existing automation sequences

## Related Methods
- `SendReportAsync(string findings, string conclusion)` - Original method with parameters
- `InvokeOpenStudyAsync()` - Similar pattern for opening studies
- `InvokeTestAsync()` - Testing/diagnostic invoke pattern
- `CustomMouseClick1Async()`, `CustomMouseClick2Async()` - Coordinate-based actions

## Code Quality
- Consistent naming convention (Async suffix)
- Proper async/await pattern
- Follows existing ExecCustom pattern
- Debug logging inherited from ExecCustom helper
- Exception handling per custom procedure framework
