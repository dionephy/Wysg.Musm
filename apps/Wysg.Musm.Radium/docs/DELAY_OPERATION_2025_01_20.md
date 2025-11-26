# Delay Operation Implementation (2025-10-20)

## Overview
Added `Delay` operation to Custom Procedures to pause execution for a specified number of milliseconds. This enables timed waits between automation steps for UI element loading, animations, or other timing-dependent scenarios.

## Implementation Details

### Operation Signature
- **Name**: `Delay`
- **Arg1**: Number (milliseconds to delay)
- **Arg2**: Disabled
- **Arg3**: Disabled

### Behavior
1. Parse milliseconds value from Arg1 (supports variables or literal numbers)
2. Validate: must be non-negative integer
3. Pause execution using `Thread.Sleep()` (AutomationWindow) or `Task.Delay()` (ProcedureExecutor)
4. Preview text: `(delayed N ms)` where N is the delay duration
5. Return value: null (operation is side-effect only)

### Error Handling
- Invalid delay (non-numeric or negative): `(invalid delay)`
- Exception during sleep: `(error: {message})`

## Use Cases

### 1. Wait for UI Element Loading
```
GetSelectedElement(SearchResultsList) ?? var1
Delay(500) ?? var2                          # Wait 500ms for element to fully render
ClickElement(var1) ?? var3
```

### 2. Wait Between Actions
```
ClickElement(OpenButton) ?? var1
Delay(1000) ?? var2                         # Wait 1 second for dialog to open
SetFocus(DialogTextField) ?? var3
```

### 3. Wait for Animation Completion
```
MouseMoveToElement(MenuButton) ?? var1
Delay(300) ?? var2                          # Wait for hover animation
ClickElement(MenuButton) ?? var3
```

### 4. Rate Limiting
```
GetValueFromSelection(ResultsList, "ID") ?? var1
Delay(200) ?? var2                          # Throttle requests to avoid overload
GetValueFromSelection(ResultsList, "Name") ?? var3
```

## Code Locations

### AutomationWindow.Procedures.Exec.cs
```csharp
case "Delay":
    // Delay: pauses execution for specified milliseconds
    row.Arg1.Type = nameof(ArgKind.Number); 
    row.Arg1Enabled = true; 
    if (string.IsNullOrWhiteSpace(row.Arg1.Value)) 
        row.Arg1.Value = "100";
    row.Arg2.Type = nameof(ArgKind.String); 
    row.Arg2Enabled = false; 
    row.Arg2.Value = string.Empty;
    row.Arg3.Type = nameof(ArgKind.Number); 
    row.Arg3Enabled = false; 
    row.Arg3.Value = string.Empty;
    break;
```

```csharp
case "Delay":
    var delayStr = ResolveString(row.Arg1, vars);
    if (!int.TryParse(delayStr, out var delayMs) || delayMs < 0) 
    { 
        preview = "(invalid delay)"; 
        break; 
    }
    try
    {
        System.Threading.Thread.Sleep(delayMs);
        preview = $"(delayed {delayMs} ms)";
    }
    catch (Exception ex) 
    { 
        preview = $"(error: {ex.Message})"; 
    }
    break;
```

### ProcedureExecutor.cs
```csharp
case "Delay":
{
    var delayStr = ResolveString(row.Arg1, vars);
    if (!int.TryParse(delayStr, out var delayMs) || delayMs < 0) 
        return ("(invalid delay)", null);
    try
    {
        Task.Delay(delayMs).Wait();
        return ($"(delayed {delayMs} ms)", null);
    }
    catch (Exception ex) 
    { 
        return ($"(error: {ex.Message})", null); 
    }
}
```

## Testing Recommendations

### AutomationWindow Interactive Testing
1. Open AutomationWindow ?? Custom Procedures tab
2. Create test procedure with Delay operations
3. Verify preview shows `(delayed N ms)` after execution
4. Test with various delay values (100ms, 500ms, 1000ms, 2000ms)
5. Test error cases: negative values, non-numeric values
6. Verify procedure execution pauses for correct duration

### Automation Sequence Testing
1. Add Delay to PACS method procedure (e.g., `SetFocusSearchResultsList`)
2. Configure delay after element interaction: `GetSelectedElement` ?? `Delay` ?? `ClickElement`
3. Run automation sequence via Settings ?? Automation
4. Verify timing: operations execute in correct order with delays respected
5. Monitor debug log for timing information

### Performance Testing
1. Test with very short delays (10ms-50ms): verify minimal overhead
2. Test with moderate delays (100ms-500ms): common use case
3. Test with long delays (1000ms-5000ms): verify responsiveness
4. Verify procedure cancellation works during delay (Escape key in AutomationWindow)

## Design Rationale

### Why Thread.Sleep vs Task.Delay?
- **AutomationWindow (interactive)**: Uses `Thread.Sleep()` - simpler synchronous execution for UI thread operations
- **ProcedureExecutor (background)**: Uses `Task.Delay().Wait()` - compatible with async execution context
- Both block the current thread appropriately for sequential operation execution

### Why Not Async/Await?
- Custom procedures execute sequentially by design
- Each operation must complete before next operation starts
- Blocking sleep ensures strict ordering and timing guarantees
- Async would introduce unnecessary complexity for simple timing delays

### Default Value: 100ms
- Chosen as reasonable default for UI element loading
- Short enough to not feel slow in most cases
- Long enough to allow basic UI rendering to complete
- User can easily adjust based on specific PACS timing requirements

## Integration with Existing Operations

### Compatible Operations
- Works with all existing operations (GetSelectedElement, ClickElement, etc.)
- Can delay before or after any operation in sequence
- Supports variable substitution for dynamic delay values

### Example Patterns

**Pattern: Retry with Backoff**
```
GetSelectedElement(SearchResultsList) ?? var1
IsVisible(var1) ?? var2
Delay(500) ?? var3                          # Wait before retry
GetSelectedElement(SearchResultsList) ?? var4
```

**Pattern: Multi-Step with Timing**
```
ClickElement(OpenButton) ?? var1
Delay(300) ?? var2                          # Wait for menu to appear
MouseMoveToElement(MenuItem) ?? var3
Delay(100) ?? var4                          # Wait for hover highlight
ClickElement(MenuItem) ?? var5
```

**Pattern: Sequential Actions**
```
SetClipboard("Patient Name") ?? var1
Delay(50) ?? var2                           # Ensure clipboard ready
SimulatePaste ?? var3
Delay(100) ?? var4                          # Wait for paste to complete
SimulateTab ?? var5
```

## Known Limitations

1. **No Cancellation**: Once delay starts, it cannot be cancelled except by closing window/app
2. **Blocks UI Thread**: In AutomationWindow, UI freezes during delay (by design for sequential execution)
3. **Max Practical Delay**: Delays over 5 seconds may appear unresponsive; use shorter delays
4. **No Adaptive Timing**: Fixed delay; does not check if condition is met (use polling pattern for that)

## Future Enhancements (Not Implemented)

1. **Cancellable Delay**: Support Escape key or Cancel button during long delays
2. **Conditional Wait**: Wait until element becomes visible (e.g., `WaitForElement` operation)
3. **Timeout with Retry**: Combine Delay with IsVisible and retry logic
4. **Progress Indicator**: Show countdown or progress bar during long delays
5. **Variable Delay**: Support range (e.g., "100-300") for randomized delays

## Specification Reference

### FR-1180: Custom Procedure Operation ? Delay (2025-10-20)
**Requirement**: Add operation `Delay` to Custom Procedures for timed pauses between automation steps.

**Operation Signature**:
- Arg1: Number (milliseconds to delay, must be non-negative)
- Arg2: Disabled
- Arg3: Disabled

**Behavior**:
1. Parse delay value from Arg1 (supports variables)
2. Validate: non-negative integer
3. Pause execution for specified milliseconds
4. Preview: `(delayed N ms)`
5. Return: null (side-effect only)

**Use Cases**:
- Wait for UI element loading after interaction
- Pause between rapid-fire actions to prevent race conditions
- Allow animations or transitions to complete
- Rate limiting for API-backed PACS systems
- Timing-dependent workflows (e.g., auto-save delays)

**Rationale**: Many PACS systems have timing dependencies where immediate actions fail due to incomplete UI rendering, ongoing animations, or async data loading. A simple delay operation allows users to insert precise timing controls without complex retry/polling logic.

## Version History
- **2025-10-20**: Initial implementation
  - Added Delay operation to AutomationWindow and ProcedureExecutor
  - Default value: 100ms
  - Supports Number arg type with variable substitution
  - Validation for non-negative integers

## Related Documentation
- [Custom Procedure Operations](CUSTOM_PROCEDURES.md)
- [GetSelectedElement Operation](GET_SELECTED_ELEMENT.md)
- [ClickElement Operation](CLICK_ELEMENT_AND_STAY.md)
- [Automation Sequences](AUTOMATION_SEQUENCES.md)

## Support Notes

### Common User Questions

**Q: How do I know what delay value to use?**
A: Start with 100-300ms for typical UI interactions. Increase if actions fail intermittently. Use AutomationWindow "Run" button to test and adjust.

**Q: Why does AutomationWindow freeze during Delay?**
A: By design - ensures strict sequential execution. Delay blocks current thread to guarantee timing. Close window if needed to cancel.

**Q: Can I use Delay with variables?**
A: Yes! Set Arg1 Type to "Var" and reference any procedure variable (e.g., `var1`). Useful for dynamic timing based on previous operations.

**Q: What if my delay is too short and actions still fail?**
A: Increase delay incrementally (e.g., 100ms ?? 300ms ?? 500ms) until reliable. Some PACS UI elements require longer rendering times.

**Q: Can I delay between GetSelectedElement and ClickElement?**
A: Yes, this is a common pattern: `GetSelectedElement ?? Delay ?? ClickElement` ensures element is fully ready before interaction.
