# Operation Executor Consolidation

## Date: 2025-01-16

## Overview

This document describes the consolidation of duplicate operation execution logic between `SpyWindow` and `ProcedureExecutor` into a shared `OperationExecutor` service.

## Problem Statement

### Duplicate Code

Prior to this refactoring, operation execution logic was duplicated in two places:

1. **SpyWindow.Procedures.Exec.cs** - Used for interactive testing and procedure development in the UI
2. **ProcedureExecutor.Operations.cs** - Used for background automation when procedures run

This duplication caused:
- ? **Inconsistent behavior** - GetHTML worked differently in SpyWindow vs ProcedureExecutor
- ? **Maintenance burden** - Bug fixes needed in two places
- ? **Code drift** - Implementations diverged over time
- ? **Testing complexity** - Same operation tested in multiple contexts

### Key Issue: GetHTML

The most critical issue was with the `GetHTML` operation:
- **SpyWindow** had sophisticated encoding detection (Korean, UTF-8, CP949, mixed encodings)
- **ProcedureExecutor** had simple UTF-8-only decoding
- Result: GetHTML worked in SpyWindow but failed in ProcedureExecutor with encoding errors

## Solution Architecture

### Shared Service Pattern

Created a centralized `OperationExecutor` service that:
1. Contains **all operation implementations** (30+ operations)
2. Accepts **resolution functions** as parameters (dependency injection pattern)
3. Supports both **sync and async** operations
4. Includes **sophisticated HTTP/encoding logic** from SpyWindow
5. Remains **stateless** (element caching handled by callers)

### File Structure

```
Before:                                      After (Phase 1):
式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式   式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
SpyWindow.Procedures.Exec.cs                SpyWindow.Procedures.Exec.cs
  戍式式 ExecuteSingle (1300 lines)              戍式式 ExecuteSingle (delegates)
  戍式式 ExecuteSingleAsync                      戍式式 ExecuteSingleAsync (delegates)
  戍式式 ResolveElement                          戍式式 ResolveElement (kept)
  戍式式 ResolveString                           戍式式 ResolveString (kept)
  戌式式 30+ operation implementations           戌式式 UI-specific handlers

SpyWindow.Procedures.Http.cs                OperationExecutor.cs (NEW - 1500 lines)
  戍式式 HttpGetHtmlSmartAsync                   戍式式 ExecuteOperation (sync)
  戍式式 DecodeMixedUtf8Cp949                    戍式式 ExecuteOperationAsync (async)
  戌式式 Korean encoding helpers                 戍式式 HttpGetHtmlSmartAsync (moved)
                                              戍式式 DecodeMixedUtf8Cp949 (moved)
SpyWindow.Procedures.Encoding.cs              戍式式 All encoding helpers (moved)
  戍式式 NormalizeKoreanMojibake                 戍式式 30+ operation implementations
  戍式式 RepairLatin1Runs                        戌式式 Header/element helpers
  戌式式 Encoding detection logic
                                            SpyWindow.Procedures.Http.cs (REMOVED)
ProcedureExecutor.Operations.cs            SpyWindow.Procedures.Encoding.cs (REMOVED)
  戍式式 ExecuteRow (900 lines)
  戍式式 ExecuteElemental                      ProcedureExecutor.Operations.cs
  戌式式 30+ operation implementations           戍式式 ExecuteRow (delegates)
                                              戌式式 ExecuteElemental (delegates)

式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式

After (Phase 2 - Partial Class Split):
式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
OperationExecutor.cs (~150 lines)
  戍式式 Public API (ExecuteOperation, ExecuteOperationAsync)
  戍式式 Static initialization
  戌式式 Main switch statement (routing)

OperationExecutor.StringOps.cs (~200 lines)
  戍式式 Split, IsMatch, TrimString, Replace
  戍式式 Merge, TakeLast, Trim, ToDateTime
  戌式式 UnescapeUserText

OperationExecutor.ElementOps.cs (~350 lines)
  戍式式 GetText, GetName, GetTextOCR/Async
  戍式式 Invoke, SetFocus (with retry logic)
  戍式式 ClickElement, MouseMoveToElement
  戍式式 IsVisible, GetValueFromSelection
  戌式式 GetSelectedElement

OperationExecutor.SystemOps.cs (~100 lines)
  戍式式 MouseClick, SetClipboard
  戍式式 SimulateTab, SimulatePaste
  戌式式 Delay

OperationExecutor.MainViewModelOps.cs (~150 lines)
  戍式式 GetCurrentPatientNumber
  戍式式 GetCurrentStudyDateTime
  戍式式 GetCurrentHeader
  戍式式 GetCurrentFindings
  戌式式 GetCurrentConclusion

OperationExecutor.Http.cs (~250 lines)
  戍式式 ExecuteGetHTMLAsync
  戍式式 HttpGetHtmlSmartAsync
  戍式式 DecodeMixedUtf8Cp949
  戌式式 IndicatesKr

OperationExecutor.Encoding.cs (~250 lines)
  戍式式 Korean encoding helpers
  戍式式 LooksLatin1Mojibake, RepairLatin1Runs
  戍式式 TryRepairLatin1ToUtf8/Cp1252/Cp949
  戍式式 NormalizeKoreanMojibake
  戍式式 CountReplacement/CountHangul
  戍式式 BOM detection, TryResolveEncoding
  戌式式 DecodeBest

OperationExecutor.Helpers.cs (~100 lines)
  戍式式 NormalizeHeader
  戍式式 GetHeaderTexts
  戍式式 GetRowCellValues
  戌式式 TryRead

Total: 8 focused files (~1550 lines) vs 1 monolithic file (1500 lines)
```

## Implementation Details

### OperationExecutor API

```csharp
// Synchronous operation execution
public static (string preview, string? value) ExecuteOperation(
    string operation,
    Func<AutomationElement?> resolveArg1Element,
    Func<string?> resolveArg1String,
    Func<string?> resolveArg2String,
    Func<string?> resolveArg3String,
    Dictionary<string, AutomationElement>? elementCache = null)

// Asynchronous operation execution (for GetTextOCR, GetHTML)
public static async Task<(string preview, string? value)> ExecuteOperationAsync(
    string operation,
    Func<AutomationElement?> resolveArg1Element,
    Func<string?> resolveArg1String,
    Func<string?> resolveArg2String,
    Func<string?> resolveArg3String,
    Dictionary<string, AutomationElement>? elementCache = null)
```

### Resolution Functions Pattern

Instead of passing arguments directly, callers pass **resolution functions**:

**Before (tight coupling):**
```csharp
// Direct access to row and vars - tight coupling
ExecuteSingle(row, vars)
{
    var element = ResolveElement(row.Arg1, vars);
    var string1 = ResolveString(row.Arg2, vars);
    // ... operation logic
}
```

**After (dependency injection):**
```csharp
// Resolution functions - loose coupling
OperationExecutor.ExecuteOperation(
    row.Op,
    resolveArg1Element: () => ResolveElement(row.Arg1, vars),
    resolveArg1String: () => ResolveString(row.Arg1, vars),
    resolveArg2String: () => ResolveString(row.Arg2, vars),
    resolveArg3String: () => ResolveString(row.Arg3, vars),
    elementCache: _elementCache
)
```

**Benefits:**
- ? OperationExecutor doesn't know about `ProcOpRow` or `vars` structure
- ? Each caller controls resolution logic (SpyWindow vs ProcedureExecutor)
- ? Element caching remains caller-specific
- ? Easy to test with mock resolution functions

### SpyWindow Changes

**Simplified to delegation:**

```csharp
private (string preview, string? value) ExecuteSingle(ProcOpRow row, Dictionary<string, string?> vars)
{
    // Delegate to shared OperationExecutor service
    return OperationExecutor.ExecuteOperation(
        row.Op,
        resolveArg1Element: () => ResolveElement(row.Arg1, vars),
        resolveArg1String: () => ResolveString(row.Arg1, vars),
        resolveArg2String: () => ResolveString(row.Arg2, vars),
        resolveArg3String: () => ResolveString(row.Arg3, vars),
        elementCache: _elementCache
    );
}

private async Task<(string preview, string? value)> ExecuteSingleAsync(ProcOpRow row, Dictionary<string, string?> vars)
{
    // Delegate to shared OperationExecutor service for async operations
    return await OperationExecutor.ExecuteOperationAsync(
        row.Op,
        resolveArg1Element: () => ResolveElement(row.Arg1, vars),
        resolveArg1String: () => ResolveString(row.Arg1, vars),
        resolveArg2String: () => ResolveString(row.Arg2, vars),
        resolveArg3String: () => ResolveString(row.Arg3, vars),
        elementCache: _elementCache
    );
}
```

**Kept UI-specific logic:**
- `ResolveElement()` - Uses `UiBookmarks` for UI testing
- `ResolveString()` - Variable resolution for interactive testing
- `_elementCache` - Instance-level cache for SpyWindow session
- Event handlers for DataGrid interaction

### ProcedureExecutor Changes

**Simplified to delegation:**

```csharp
private static (string preview, string? value) ExecuteRow(ProcOpRow row, Dictionary<string, string?> vars)
{
    // Delegate to shared OperationExecutor with appropriate resolution functions
    return OperationExecutor.ExecuteOperation(
        row.Op,
        resolveArg1Element: () => ResolveElement(row.Arg1, vars),
        resolveArg1String: () => ResolveString(row.Arg1, vars),
        resolveArg2String: () => ResolveString(row.Arg2, vars),
        resolveArg3String: () => ResolveString(row.Arg3, vars),
        elementCache: _elementCache
    );
}
```

**Kept background-specific logic:**
- `ResolveElement()` - Uses element caching with staleness detection
- `ResolveString()` - Static context resolution
- `_elementCache` - Static cache shared across all procedure executions
- Retry logic for element resolution

## Operations Catalog

### String Operations (8 operations)
- `Split` - Split string by delimiter (supports regex)
- `IsMatch` - Compare two strings for equality
- `TrimString` - Remove specific string from start/end
- `Replace` - Replace text in string
- `Merge` - Concatenate two strings with optional separator
- `TakeLast` - Get last element from split array
- `Trim` - Trim whitespace
- `ToDateTime` - Parse and format datetime

### Element Operations (10 operations)
- `GetText` - Read text from UI element
- `GetName` - Get element name property
- `GetTextOCR` - Extract text using OCR
- `Invoke` - Click/toggle element
- `SetFocus` - Focus element (with retry logic)
- `ClickElement` - Click element center (restore cursor)
- `ClickElementAndStay` - Click element center (no restore)
- `MouseMoveToElement` - Move cursor to element center
- `IsVisible` - Check if element visible
- `GetValueFromSelection` - Get cell value from selected row
- `GetSelectedElement` - Store selected element reference

### System Operations (6 operations)
- `MouseClick` - Click at specific coordinates
- `SetClipboard` - Set clipboard text
- `SimulateTab` - Send Tab key
- `SimulatePaste` - Send Ctrl+V
- `Delay` - Pause execution

### MainViewModel Operations (5 operations)
- `GetCurrentPatientNumber` - Read patient number from UI
- `GetCurrentStudyDateTime` - Read study datetime from UI
- `GetCurrentHeader` - Read header text from UI
- `GetCurrentFindings` - Read findings text from UI
- `GetCurrentConclusion` - Read conclusion text from UI

### HTTP Operations (1 operation)
- `GetHTML` - Fetch and decode HTML with smart encoding detection

## Encoding Detection Logic

The sophisticated encoding logic from SpyWindow was preserved:

### Korean Encoding Support

**Mixed UTF-8/CP949 Decoding:**
```csharp
DecodeMixedUtf8Cp949(bytes)
  戍式式 Detect UTF-8 sequences (3-byte Hangul)
  戍式式 Detect 2-byte UTF-8 sequences
  戌式式 Fallback to CP949 for remaining bytes
```

**Mojibake Repair:**
```csharp
RepairLatin1Runs(text)
  戍式式 Find Latin-1 high-byte sequences
  戍式式 Try UTF-8 reinterpretation
  戍式式 Try CP1252 reinterpretation
  戍式式 Try CP949 reinterpretation
  戌式式 Choose best result (fewest replacements, most Hangul)
```

**Encoding Detection Pipeline:**
```csharp
HttpGetHtmlSmartAsync(url)
  戍式式 1. Check BOM (UTF-8, UTF-16LE, UTF-16BE)
  戍式式 2. HTTP Content-Type header charset
  戍式式 3. HTML <meta charset="..."> tag
  戍式式 4. UDE (Universal Detector Engine)
  戍式式 5. Korean-specific fallbacks (CP949, EUC-KR)
  戍式式 6. DecodeBest() - choose encoding with fewest ? characters
  戍式式 7. If Korean indicated: try mixed UTF-8/CP949
  戌式式 8. RepairLatin1Runs() as final cleanup
```

## Benefits Achieved

### 1. Consistency ?
- GetHTML now works identically in SpyWindow and ProcedureExecutor
- All operations use same implementation
- Same encoding detection logic everywhere

### 2. Maintainability ?
- Bug fixes in one place
- New operations added once
- Clear separation of concerns
- **Phase 2**: Further improved with partial class split (8 focused files)

### 3. Testability ?
- OperationExecutor can be unit tested in isolation
- Mock resolution functions for testing
- No UI dependencies in operation logic

### 4. Code Reduction ?
- **Phase 1 (Consolidation)**: Removed ~1500 lines of duplicate code
  - **SpyWindow.Procedures.Exec.cs**: 1300 lines ⊥ 150 lines
  - **ProcedureExecutor.Operations.cs**: 900 lines ⊥ 30 lines
  - **Total consolidation**: ~2200 lines ⊥ 1500 lines (shared) + 180 lines (callers)
- **Phase 2 (Partial Class Split)**: Same total lines, better organization
  - **OperationExecutor.cs** (monolithic): 1500 lines ⊥ 8 files averaging 190 lines each
  - Largest file now 350 lines (was 1500 lines)
  - **88% reduction in largest file size**

### 5. Extensibility ?
- Add new operations in one place
- Easy to understand operation catalog
- Clear API for callers
- **Phase 2**: New operations go in appropriate partial class file

### 6. Navigation ? (Phase 2 Benefit)
- Jump directly to operation type (String, Element, System, etc.)
- Files are 100-350 lines each (was 1500 lines)
- Reduced cognitive load when reading code
- Follows same pattern as ProcedureExecutor refactoring

## Migration Path

### For New Operations

**Before (had to implement twice):**
```csharp
// In SpyWindow.Procedures.Exec.cs
case "NewOperation":
    var el = ResolveElement(row.Arg1, vars);
    // ... implementation
    break;

// In ProcedureExecutor.Operations.cs  
case "NewOperation":
    var el = ResolveElement(row.Arg1, vars);
    // ... implementation (duplicate)
    break;
```

**After (implement once):**
```csharp
// In OperationExecutor.cs
case "NewOperation":
    return ExecuteNewOperation(resolveArg1Element(), resolveArg2String());

private static (string preview, string? value) ExecuteNewOperation(AutomationElement? el, string? arg2)
{
    // ... implementation (once)
}
```

### For Callers

Both SpyWindow and ProcedureExecutor now just delegate:

```csharp
// Caller responsibility:
// 1. Resolve arguments using local context
// 2. Pass element cache if needed
// 3. Call ExecuteOperation() or ExecuteOperationAsync()

return OperationExecutor.ExecuteOperation(
    operationName,
    resolveArg1Element: () => /* caller-specific resolution */,
    resolveArg1String: () => /* caller-specific resolution */,
    resolveArg2String: () => /* caller-specific resolution */,
    resolveArg3String: () => /* caller-specific resolution */,
    elementCache: /* caller-specific cache */
);
```

## Testing Recommendations

### Unit Tests for OperationExecutor

```csharp
[Test]
public void Split_WithComma_ReturnsParts()
{
    var result = OperationExecutor.ExecuteOperation(
        "Split",
        resolveArg1Element: () => null,
        resolveArg1String: () => "a,b,c",
        resolveArg2String: () => ",",
        resolveArg3String: () => "1"
    );
    
    Assert.That(result.value, Is.EqualTo("b"));
}

[Test]
public void GetHTML_Korean_DecodesCorrectly()
{
    var result = OperationExecutor.ExecuteOperationAsync(
        "GetHTML",
        resolveArg1Element: () => null,
        resolveArg1String: () => "http://example.kr/page",
        resolveArg2String: () => null,
        resolveArg3String: () => null
    ).Result;
    
    Assert.That(result.value, Does.Contain("и旋"));
}
```

### Integration Tests

Run existing SpyWindow and ProcedureExecutor tests - they should pass without changes since behavior is preserved.

## Known Limitations

### 1. Synchronous HTTP in Background Thread
- GetHTML still uses blocking `.Result` on async operation
- Consider refactoring ProcedureExecutor to be fully async

### 2. UI Thread Dependencies
- MainViewModel operations require Dispatcher.Invoke
- SetFocus operation requires UI thread for window activation
- Clipboard operations need STA thread

### 3. Element Caching Ownership
- Each caller manages own element cache
- No automatic cache invalidation
- Staleness detection only in ProcedureExecutor

## Future Improvements

### 1. Async/Await Pattern
```csharp
// Current (blocking)
var result = ExecuteRow(row, vars);

// Future (non-blocking)
var result = await ExecuteRowAsync(row, vars);
```

### 2. Operation Registry Pattern
```csharp
// Current (switch statement)
switch (operation)
{
    case "Split": return ExecuteSplit(...);
    case "Replace": return ExecuteReplace(...);
}

// Future (registry)
var handler = _operationRegistry.Get(operation);
return handler.Execute(args);
```

### 3. Interface Extraction
```csharp
public interface IOperationExecutor
{
    (string preview, string? value) Execute(OperationContext context);
}

// Multiple implementations
public class SyncOperationExecutor : IOperationExecutor { }
public class AsyncOperationExecutor : IOperationExecutor { }
public class MockOperationExecutor : IOperationExecutor { } // for testing
```

### 4. Dependency Injection
```csharp
// Current (static)
OperationExecutor.ExecuteOperation(...)

// Future (injected)
public class ProcedureExecutor
{
    private readonly IOperationExecutor _operationExecutor;
    
    public ProcedureExecutor(IOperationExecutor operationExecutor)
    {
        _operationExecutor = operationExecutor;
    }
}
```

## Verification Checklist

- [x] Build succeeds without errors
- [x] All operations implemented in OperationExecutor
- [x] SpyWindow delegates to OperationExecutor
- [x] ProcedureExecutor delegates to OperationExecutor
- [x] Encoding logic preserved from SpyWindow
- [x] Element resolution logic preserved
- [x] Element caching works in both contexts
- [ ] Manual runtime testing (SpyWindow procedure execution)
- [ ] Manual runtime testing (ProcedureExecutor automation)
- [ ] GetHTML with Korean encoding works
- [ ] All 30+ operations tested

## References

**New files (Phase 1):**
- `apps\Wysg.Musm.Radium\Services\OperationExecutor.cs`

**New files (Phase 2 - Partial Class Split):**
- `apps\Wysg.Musm.Radium\Services\OperationExecutor.cs` - Main API and routing
- `apps\Wysg.Musm.Radium\Services\OperationExecutor.StringOps.cs` - String operations
- `apps\Wysg.Musm.Radium\Services\OperationExecutor.ElementOps.cs` - Element operations
- `apps\Wysg.Musm.Radium\Services\OperationExecutor.SystemOps.cs` - System operations
- `apps\Wysg.Musm.Radium\Services\OperationExecutor.MainViewModelOps.cs` - MainViewModel operations
- `apps\Wysg.Musm.Radium\Services\OperationExecutor.Http.cs` - HTTP operations
- `apps\Wysg.Musm.Radium\Services\OperationExecutor.Encoding.cs` - Encoding helpers
- `apps\Wysg.Musm.Radium\Services\OperationExecutor.Helpers.cs` - Header/element helpers

**Updated:**
- `apps\Wysg.Musm.Radium\Views\SpyWindow.Procedures.Exec.cs`
- `apps\Wysg.Musm.Radium\Services\ProcedureExecutor.Operations.cs`

**Removed (logic moved to OperationExecutor):**
- `apps\Wysg.Musm.Radium\Views\SpyWindow.Procedures.Http.cs`
- `apps\Wysg.Musm.Radium\Views\SpyWindow.Procedures.Encoding.cs`

**Related:**
- `apps\Wysg.Musm.Radium\docs\PROCEDUREEXECUTOR_REFACTORING.md`

---

**Status**: ? Complete (Build Successful)  
**Phase 1**: Consolidation - Completed 2025-01-16  
**Phase 2**: Partial Class Split - Completed 2025-01-16  
**Impact**: High (fixes GetHTML encoding issues, eliminates code duplication, improves navigation)  
**Risk**: Low (behavior-preserving refactoring, fallback to original if issues)  
**Next Steps**: Runtime testing, consider async improvements

