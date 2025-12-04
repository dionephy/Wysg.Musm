# GetTextOnce Operation

## Date: 2025-12-04

## Summary
Added a new `GetTextOnce` operation that uses single-attempt element resolution (no retries) for faster failure when an element doesn't exist.

## Problem
The standard `GetText` operation uses 3 retry attempts for element resolution, which is safe for most cases but wastes ~2400ms when trying to read elements that don't exist (like `g3_report_viewer` in G3 procedures).

## Solution
Created a new `GetTextOnce` operation that:
- Uses `ResolveElementOnce()` instead of `ResolveElement()`
- Makes only 1 attempt to find the element (no retries)
- Fails fast when element doesn't exist
- Saves ~1600ms per call on non-existent elements

## Implementation

### Files Modified

1. **`ProcedureExecutor.Elements.cs`**
   - Reverted `ElementResolveMaxAttempts` back to 3 (for safety)
   - Added `ResolveElementOnce()` method that uses `maxAttempts: 1`
   - Refactored `ResolveElement()` to call `ResolveElementInternal()`

2. **`ProcedureExecutor.Operations.cs`**
   - Modified `ExecuteRow()` to use `ResolveElementOnce()` for `GetTextOnce` operation

3. **`OperationExecutor.ElementOps.cs`**
   - Added `ExecuteGetTextOnce()` method (delegates to `ExecuteGetText()`)

4. **`OperationExecutor.cs`**
   - Added routing for `GetTextOnce` operation in `ExecuteOperation()`

5. **`AutomationWindow.OperationItems.xaml`**
   - Added `GetTextOnce` to the operation dropdown list

## Usage

Use `GetTextOnce` instead of `GetText` when:
- You expect the element might not exist
- You want to fail fast without retrying
- The element's absence is a normal/expected condition

### Example
In `G3_GetLongerReading` procedure:
```
GetTextOnce(g3_report_viewer) -> $v1   // Fails fast if not found
GetText(g3_reading_text) -> $v2        // Normal retry behavior
GetLongerText($v1, $v2) -> $v3
```

## Performance Impact
- **Before**: 3 attempts ¡¿ 2 calls ¡¿ ~400ms = ~2400ms on missing elements
- **After**: 1 attempt ¡¿ 2 calls ¡¿ ~400ms = ~800ms on missing elements
- **Savings**: ~1600ms per procedure call with non-existent bookmarks
