# OperationExecutor Partial Class Split

**Date**: 2025-01-16 (Phase 2)  
**Type**: Refactoring (File Organization)  
**Status**: ? Complete (Build Successful)  
**Parent**: Operation Executor Consolidation (Phase 1)

---

## Overview

After consolidating operation execution logic into a single `OperationExecutor.cs` file (Phase 1), we split the 1500-line monolithic file into 8 focused partial class files following the same pattern used in the ProcedureExecutor refactoring.

## The Problem

- **OperationExecutor.cs was 1500 lines long** - difficult to navigate and maintain
- **All operation types mixed together** - hard to find specific operations
- **Difficult code reviews** - large diffs when making changes
- **Cognitive load** - understanding the entire file at once

## The Solution

Split into 8 partial class files by logical responsibility:

```
OperationExecutor.cs (~150 lines)           - Main API, initialization, routing
OperationExecutor.StringOps.cs (~200 lines) - String manipulation
OperationExecutor.ElementOps.cs (~350 lines) - UI element interaction
OperationExecutor.SystemOps.cs (~100 lines)  - Mouse, clipboard, keyboard
OperationExecutor.MainViewModelOps.cs (~150) - MainViewModel data access
OperationExecutor.Http.cs (~250 lines)       - HTTP and web operations
OperationExecutor.Encoding.cs (~250 lines)   - Korean/UTF-8/CP949 encoding
OperationExecutor.Helpers.cs (~100 lines)    - Header parsing, element reading
```

## File Breakdown

### OperationExecutor.cs (Main - ~150 lines)
**Responsibility**: Public API and operation routing

**Contains**:
- `ExecuteOperation()` - Synchronous API
- `ExecuteOperationAsync()` - Asynchronous API
- Static initialization
- Main switch statement routing to implementations
- HTTP client setup

**Why separate**: Clean entry point, easy to understand API surface

---

### OperationExecutor.StringOps.cs (~200 lines)
**Responsibility**: String manipulation operations

**Contains**:
- `Split` - Split string by delimiter (regex support)
- `IsMatch` - Compare two strings
- `TrimString` - Remove substring from start/end
- `Replace` - Replace text in string
- `Merge` - Concatenate with separator
- `TakeLast` - Get last element from split array
- `Trim` - Trim whitespace
- `ToDateTime` - Parse and format datetime
- `UnescapeUserText` - Helper for escape sequences

**Why separate**: Pure string logic, no dependencies, easily testable

---

### OperationExecutor.ElementOps.cs (~350 lines)
**Responsibility**: UI Automation element interaction

**Contains**:
- `GetText` - Read text with mojibake repair
- `GetName` - Get element name
- `GetTextOCR` / `GetTextOCRAsync` - OCR text extraction
- `Invoke` - Click/toggle element
- `SetFocus` - Focus element with retry logic (complex)
- `ClickElement` - Click with optional cursor restore
- `MouseMoveToElement` - Move cursor to element
- `IsVisible` - Check visibility
- `GetValueFromSelection` - Get selected row cell value
- `GetSelectedElement` - Store element reference

**Why separate**: FlaUI-specific logic, largest operation group, complex retry logic

---

### OperationExecutor.SystemOps.cs (~100 lines)
**Responsibility**: System-level operations

**Contains**:
- `MouseClick` - Click at coordinates
- `SetClipboard` - Set clipboard text (STA thread)
- `SimulateTab` - Send Tab key
- `SimulatePaste` - Send Ctrl+V
- `Delay` - Sleep for milliseconds

**Why separate**: Windows API calls, simple operations, no element dependencies

---

### OperationExecutor.MainViewModelOps.cs (~150 lines)
**Responsibility**: MainViewModel data access

**Contains**:
- `GetCurrentPatientNumber` - Read patient number
- `GetCurrentStudyDateTime` - Read study datetime
- `GetCurrentHeader` - Read header text
- `GetCurrentFindings` - Read findings text
- `GetCurrentConclusion` - Read conclusion text

**Why separate**: All use Dispatcher.Invoke, MainViewModel-specific, similar pattern

---

### OperationExecutor.Http.cs (~250 lines)
**Responsibility**: HTTP operations and encoding detection

**Contains**:
- `ExecuteGetHTMLAsync` - Public async entry point
- `HttpGetHtmlSmartAsync` - Smart encoding detection pipeline
- `DecodeMixedUtf8Cp949` - Mixed encoding decoder
- `IndicatesKr` - Korean charset detection

**Why separate**: Complex HTTP logic, async operations, encoding detection pipeline

---

### OperationExecutor.Encoding.cs (~250 lines)
**Responsibility**: Korean/UTF-8/CP949 encoding helpers

**Contains**:
- `LooksLatin1Mojibake` - Detect mojibake
- `TryRepairLatin1ToUtf8` - Repair misencoded text
- `TryRepairCp1252ToUtf8` - Repair CP1252 to UTF-8
- `TryRepairLatin1ToCp949` - Repair to CP949
- `RepairLatin1Runs` - Repair Latin-1 sequences
- `NormalizeKoreanMojibake` - Main normalization entry point
- `CountReplacement` / `CountHangul` - Scoring helpers
- `ScoreText` - Score encoding quality
- `HasUtf8Bom` / `HasUtf16LeBom` / `HasUtf16BeBom` - BOM detection
- `TryResolveEncoding` - Encoding resolver
- `DecodeBest` - Choose best encoding

**Why separate**: Sophisticated encoding logic, Korean-specific, many helper methods

---

### OperationExecutor.Helpers.cs (~100 lines)
**Responsibility**: UI element reading and header parsing

**Contains**:
- `NormalizeHeader` - Normalize header names
- `GetHeaderTexts` - Extract table headers
- `GetRowCellValues` - Extract row cell values
- `TryRead` - Read text from element (Value/Name/LegacyIAccessible)

**Why separate**: UI Automation helpers used by multiple operations, table-specific logic

---

## Benefits

| Benefit | Before | After | Improvement |
|---------|--------|-------|-------------|
| **Largest file** | 1500 lines | 350 lines | **-77%** |
| **Average file size** | 1500 lines | 190 lines | **-87%** |
| **Navigation** | Scroll through 1500 lines | Jump to specific file | **Instant** |
| **Code reviews** | Large diffs | Targeted diffs | **Focused** |
| **Maintainability** | All mixed together | Clear separation | **Easier** |
| **Extensibility** | Find right region | Find right file | **Clearer** |

### 1. Better Navigation ?
- **Before**: Search through 1500 lines to find operation
- **After**: Open appropriate file (e.g., `StringOps.cs` for `Split`)
- Jump to definition works better with smaller files
- IDE file switcher more useful

### 2. Clearer Responsibilities ?
- Each file has single focus (SRP)
- String operations don't mix with HTTP logic
- Element operations separate from encoding helpers
- Easy to understand file's purpose from name

### 3. Easier Maintenance ?
- Change encoding logic without seeing element ops
- Modify string operations in isolation
- Add new HTTP features in dedicated file
- Reduced cognitive load

### 4. Better Code Reviews ?
- Changes to string ops don't affect element ops file
- Smaller diffs in pull requests
- Easier to spot related changes
- Clear context for reviewers

### 5. Follows Established Pattern ?
- Same approach as ProcedureExecutor refactoring
- Consistent across codebase
- Team already familiar with pattern
- Easy to apply to other large files

## Code Metrics

| File | Lines | Operations | Primary Dependency |
|------|-------|------------|-------------------|
| **OperationExecutor.cs** | ~150 | Routing | None (API) |
| **StringOps.cs** | ~200 | 8 | System.Text.RegularExpressions |
| **ElementOps.cs** | ~350 | 11 | FlaUI.Core.AutomationElements |
| **SystemOps.cs** | ~100 | 5 | System.Windows, NativeMouseHelper |
| **MainViewModelOps.cs** | ~150 | 5 | ViewModels.MainViewModel |
| **Http.cs** | ~250 | 1 + helpers | System.Net.Http, Ude |
| **Encoding.cs** | ~250 | 0 (helpers) | System.Text |
| **Helpers.cs** | ~100 | 0 (helpers) | FlaUI.Core.AutomationElements |
| **Total** | **~1550** | **30+** | Clear boundaries |

## Migration Guide

### For New Operations

**Before (monolithic file):**
```csharp
// In OperationExecutor.cs (1500 lines)
// Scroll to find right region
#region String Operations
// ... 200 lines of string operations ...
case "NewStringOp":
    return ExecuteNewStringOp(...);
// ... more operations ...
#endregion
```

**After (partial classes):**
```csharp
// In OperationExecutor.cs (150 lines)
case "NewStringOp":
    return ExecuteNewStringOp(...);

// In OperationExecutor.StringOps.cs (200 lines)
private static (string preview, string? value) ExecuteNewStringOp(...)
{
    // ... implementation ...
}
```

**Decision tree for new operations**:
1. **String manipulation?** ¡æ `StringOps.cs`
2. **UI element interaction?** ¡æ `ElementOps.cs`
3. **Mouse/keyboard/clipboard?** ¡æ `SystemOps.cs`
4. **MainViewModel data?** ¡æ `MainViewModelOps.cs`
5. **HTTP/web?** ¡æ `Http.cs`
6. **Encoding/mojibake?** ¡æ `Encoding.cs`
7. **Header/table helpers?** ¡æ `Helpers.cs`

### For Callers

**No changes needed** - all operations still accessed through main API:

```csharp
// Works exactly the same
OperationExecutor.ExecuteOperation(
    "Split",
    resolveArg1Element: () => null,
    resolveArg1String: () => "a,b,c",
    resolveArg2String: () => ",",
    resolveArg3String: () => "1"
);
```

## Testing

### Build Verification ?
- [x] Solution builds without errors
- [x] All partial classes recognized
- [x] No missing references
- [x] No namespace issues

### Runtime Testing ??
- [ ] All operations still work in AutomationWindow
- [ ] All operations still work in ProcedureExecutor
- [ ] No behavioral changes
- [ ] GetHTML with Korean encoding works

## Known Considerations

### 1. More Files to Manage
- **Before**: 1 file
- **After**: 8 files
- **Mitigation**: Clear naming convention, logical organization

### 2. Jump to Definition
- **Behavior**: May jump to partial class declaration first
- **Workaround**: Use "Go to Implementation" (Ctrl+F12) instead
- **Impact**: Minimal - still faster than scrolling through 1500 lines

### 3. File Hierarchy
- All files at same level in `Services/` folder
- No subfolder (keeps files together)
- Naming convention: `OperationExecutor.*.cs`

## Future Improvements

### 1. Further Split ElementOps.cs (if needed)
Currently 350 lines - could split into:
- `OperationExecutor.ElementOps.Read.cs` - GetText, GetName, GetTextOCR
- `OperationExecutor.ElementOps.Interact.cs` - Click, Focus, Move

### 2. Separate HTTP from Encoding
Currently `Http.cs` depends on `Encoding.cs` helpers
Could create `OperationExecutor.HttpEncoding.cs`

### 3. Extract Interfaces
Could extract `IStringOperations`, `IElementOperations`, etc. for testing

## Comparison with ProcedureExecutor Refactoring

| Aspect | ProcedureExecutor | OperationExecutor |
|--------|------------------|-------------------|
| **Original size** | 1600 lines | 1500 lines |
| **Number of files** | 5 | 8 |
| **Largest file after** | 900 lines | 350 lines |
| **Approach** | Split by layer (Storage, Elements, Operations) | Split by operation type |
| **Pattern** | Models, Storage, Elements, Operations | API, StringOps, ElementOps, SystemOps, etc. |
| **Reasoning** | Architectural layers | Operation categories |

Both follow **Single Responsibility Principle** but with different axes of separation.

## References

- **Phase 1**: `OPERATION_EXECUTOR_CONSOLIDATION.md` - Original consolidation
- **Inspiration**: `PROCEDUREEXECUTOR_REFACTORING.md` - Similar approach
- **Files created**:
  - `OperationExecutor.cs` (main)
  - `OperationExecutor.StringOps.cs`
  - `OperationExecutor.ElementOps.cs`
  - `OperationExecutor.SystemOps.cs`
  - `OperationExecutor.MainViewModelOps.cs`
  - `OperationExecutor.Http.cs`
  - `OperationExecutor.Encoding.cs`
  - `OperationExecutor.Helpers.cs`

---

**Status**: ? Complete (Build Successful)  
**Impact**: Medium (better organization, no functional changes)  
**Risk**: Very Low (no logic changes, only file organization)  
**Next Steps**: Runtime testing to verify no regressions

