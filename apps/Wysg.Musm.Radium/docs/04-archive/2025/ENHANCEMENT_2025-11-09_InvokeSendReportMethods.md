# Enhancement: New PACS Method Items - InvokeSendReport and SendReportRetry (2025-11-09)

## Overview
Added two new PACS method items in the UI Spy window's Custom Procedures section to support send report automation workflows with retry capabilities.

## Changes Made

### 1. SpyWindow UI - New PACS Method Items
**File**: `apps\Wysg.Musm.Radium\Views\SpyWindow.PacsMethodItems.xaml`

Added two new ComboBoxItem entries to the PACS method dropdown:
- **InvokeSendReport**: "Invoke send report" - Primary send report action
- **SendReportRetry**: "Send report retry" - Retry mechanism for failed send operations

### 2. PacsService - Wrapper Methods
**File**: `apps\Wysg.Musm.Radium\Services\PacsService.cs`

Added corresponding async wrapper methods:
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

## User Workflow

### Configuration in UI Spy
1. Open UI Spy window (Tools �� UI Spy)
2. Select PACS profile from dropdown
3. Navigate to "Custom Procedures" section
4. Select "Invoke send report" or "Send report retry" from PACS Method dropdown
5. Configure operation steps:
   - Use bookmark resolution to identify send button/control
   - Add click operations, text field population, etc.
   - Configure retry logic (delays, validation checks)
6. Save custom procedure
7. Test using "Run" button

### Usage in Automation
These methods can be called from automation sequences:
- Part of send report workflows
- Integrated with validation/error handling modules
- Combined with patient number/study datetime matching checks

## Technical Details

### Method Signatures
- **InvokeSendReportAsync()**: Returns `Task<bool>`, always returns true after procedure execution
- **SendReportRetryAsync()**: Returns `Task<bool>`, always returns true after procedure execution

### Custom Procedure Execution
Both methods use the internal `ExecCustom()` helper which:
- Loads per-PACS procedure configuration from `%AppData%\Wysg.Musm\Radium\Pacs\{pacs_key}\ui-procedures.json`
- Executes configured operation steps sequentially
- Supports retry logic (if configured in procedure steps)
- Returns null on successful execution (result captured in operation preview)

### Error Handling
- Procedure execution errors are caught and logged in Debug output
- Methods return true even if procedure fails (caller should check PACS state)
- Custom procedures should include validation steps to detect failures

## Comparison with Existing SendReport Method

The existing `SendReportAsync(string findings, string conclusion)` method:
- Accepts findings/conclusion as parameters
- Always executes "SendReport" custom procedure
- Does not provide built-in retry logic

The new methods:
- **InvokeSendReport**: Similar to existing SendReport but distinct tag for separate configuration
- **SendReportRetry**: Explicit retry-focused method for robust report submission

Users can configure these methods independently for different PACS systems or workflows.

## Best Practices

### Recommended Procedure Structure
For **InvokeSendReport**:
1. Validate report text is visible
2. Click send button
3. Wait for confirmation dialog (if applicable)
4. Confirm send action
5. Validate success state

For **SendReportRetry**:
1. Check if previous send attempt failed
2. Close error dialogs (if any)
3. Re-invoke send button
4. Add delays for UI stabilization
5. Validate final state

### Common Operations
- `GetText`: Read UI elements to validate state
- `ClickElement`: Click buttons/controls using bookmarks
- `Delay`: Pause for UI stabilization
- `IsVisible`: Check dialog/window visibility
- `Invoke`: Trigger button actions

## Related Features
- FR-1102: Send Report PACS Method (original)
- FR-1106: SendReport Automation Module
- Custom Procedure Operations (ClickElement, Invoke, IsVisible, etc.)

## Testing Recommendations
1. Test with clean PACS state (no pending operations)
2. Test with error scenarios (network failure, invalid data)
3. Verify retry logic handles timeout conditions
4. Check procedure execution time for performance
5. Validate that procedures are PACS-profile specific

## Migration Notes
- Existing `SendReport` custom procedures remain unchanged
- No breaking changes to existing automation sequences
- Users should configure new methods explicitly per PACS profile
- Legacy procedures continue to work as before

## Future Enhancements
- Consider adding return value from procedure execution (success/failure)
- Add timeout configuration for retry operations
- Implement callback mechanism for error notification
- Support batch send report operations
