# ProcedureExecutor Refactoring

## Overview
The `ProcedureExecutor` class was refactored from a single ~1600 line file into 5 focused partial class files following the Single Responsibility Principle.

## Refactoring Date
2025-01-16

## Architecture

### File Structure

```
Services/
戍式式 ProcedureExecutor.cs              (~400 lines) - Main coordinator
戍式式 ProcedureExecutor.Models.cs       (~30 lines)  - Data models
戍式式 ProcedureExecutor.Storage.cs      (~40 lines)  - Persistence
戍式式 ProcedureExecutor.Elements.cs     (~200 lines) - Element resolution
戌式式 ProcedureExecutor.Operations.cs   (~900 lines) - Operation implementations
```

### Responsibilities by File

#### 1. **ProcedureExecutor.cs** (Main Coordinator)
- **Public API**: `ExecuteAsync(methodTag)`
- **Execution Flow**: `ExecuteInternal(methodTag)`
- **Special Handlers**: Direct MainViewModel reads (GetCurrentPatientNumber, GetCurrentStudyDateTime)
- **Comparison Operations**: PatientNumberMatch, StudyDateTimeMatch
- **Fallback Logic**: `TryCreateFallbackProcedure(methodTag)`

**Key Methods:**
- `ExecuteAsync` - Public entry point
- `ExecuteInternal` - Main execution orchestrator
- `GetCurrentPatientNumberDirect` - Direct VM read
- `GetCurrentStudyDateTimeDirect` - Direct VM read
- `ComparePatientNumber` - Comparison helper
- `CompareStudyDateTime` - Comparison helper
- `TryCreateFallbackProcedure` - Auto-seed procedures

#### 2. **ProcedureExecutor.Models.cs** (Data Models)
- **ProcStore**: Container for method procedures
  - `Dictionary<string, List<ProcOpRow>> Methods`
- **ProcOpRow**: Individual operation step
  - `Op`, `Arg1`, `Arg2`, `Arg3`
  - `Arg1Enabled`, `Arg2Enabled`, `Arg3Enabled`
  - `OutputVar`, `OutputPreview`
- **ProcArg**: Operation argument
  - `Type` (ArgKind enum value as string)
  - `Value` (string representation)
- **ArgKind** (enum): Argument type classification
  - `Element` - UI bookmark reference
  - `String` - Literal string
  - `Number` - Numeric value
  - `Var` - Variable reference

#### 3. **ProcedureExecutor.Storage.cs** (Persistence Layer)
- **File Operations**:
  - `GetProcPath()` - Resolve procedure file location
  - `Load()` - Deserialize procedures from JSON
  - `Save(ProcStore)` - Serialize procedures to JSON
- **Override Support**:
  - `SetProcPathOverride(Func<string>)` - Custom path resolver

**Storage Location:**
- Default: `%APPDATA%\Wysg.Musm\Radium\ui-procedures.json`
- Override: Via `SetProcPathOverride` callback

#### 4. **ProcedureExecutor.Elements.cs** (Element Resolution)
- **Caching**:
  - `_controlCache` - Bookmark-based element cache
  - `_elementCache` - Runtime element cache (from GetSelectedElement)
- **Resolution**:
  - `ResolveElement(ProcArg, vars)` - Main resolver with staleness detection
  - `ResolveString(ProcArg, vars)` - String value resolver
- **Validation**:
  - `IsElementAlive(element)` - Staleness check
  - Retry logic: 3 attempts with 150ms delay
- **Cache Management**:
  - `GetCached(key)` - Retrieve cached element
  - `StoreCache(key, element)` - Store element in cache
- **Helper**:
  - `ParseArgKind(string)` - Convert string to ArgKind enum

**Resolution Strategies:**
- **Element type**: Resolve via UiBookmarks with cache-first approach
- **Var type**: Lookup in variables dictionary or element cache
- **String/Number type**: Direct value return

#### 5. **ProcedureExecutor.Operations.cs** (Operation Implementations)
- **Entry Points**:
  - `ExecuteRow(row, vars)` - Main dispatcher
  - `ExecuteElemental(row, vars)` - Complex operation handler
- **HTTP Support**:
  - `_http` - Shared HttpClient instance
  - `EnsureEncodingProviders()` - Register CodePages support
- **String Operations**:
  - Split, IsMatch, TrimString, Replace, Merge, Trim, TakeLast
  - `UnescapeUserText(string)` - Regex unescape helper
- **Element Operations**:
  - GetText, GetTextOCR, Invoke, SetFocus
  - ClickElement, ClickElementAndStay, MouseMoveToElement
  - IsVisible, MouseClick
  - GetValueFromSelection, GetSelectedElement
- **Data Operations**:
  - ToDateTime, GetHTML
  - SetClipboard, SimulateTab, SimulatePaste, Delay
- **MainViewModel Integration**:
  - GetCurrentHeader, GetCurrentFindings, GetCurrentConclusion
- **Helper Class**:
  - `SpyHeaderHelpers` - Grid header/cell extraction utilities

**Special Features:**
- **SetFocus**: UI thread marshalling with retry logic
- **GetHTML**: Smart encoding detection (UTF-8 ⊥ charset ⊥ default)
- **GetTextOCR**: OCR integration via Wysg.Musm.MFCUIA
- **GetSelectedElement**: Runtime element caching for dynamic UI interactions

## Benefits of Refactoring

### 1. **Maintainability**
- Each file has a clear, single responsibility
- Easier to locate and modify specific functionality
- Reduced cognitive load when reading code

### 2. **Testability**
- Concerns are isolated, enabling focused unit testing
- Element resolution can be tested independently
- Operation implementations can be mocked

### 3. **Extensibility**
- New operations added only in Operations.cs
- Storage format changes isolated to Storage.cs
- Element resolution strategies in Elements.cs

### 4. **Code Navigation**
- Faster IDE navigation (smaller files)
- Clear boundaries between concerns
- Easier code reviews

### 5. **Team Collaboration**
- Reduced merge conflicts (changes in different files)
- Parallel development on different concerns
- Clear ownership boundaries

## Design Patterns Used

### 1. **Partial Classes**
- Allows splitting across multiple files while maintaining single class
- All parts share same namespace and access modifiers

### 2. **Strategy Pattern**
- `ResolveElement` uses different strategies based on ArgKind
- `ExecuteRow` dispatches to appropriate operation handler

### 3. **Cache-Aside Pattern**
- Element caching with cache-first, then resolve-on-miss
- Automatic staleness detection and eviction

### 4. **Retry Pattern**
- Element resolution retries on failure (3 attempts, 150ms delay)
- SetFocus operation retries with exponential backoff

### 5. **Template Method Pattern**
- `ExecuteInternal` defines execution skeleton
- `ExecuteRow` and `ExecuteElemental` implement specific steps

## Migration Notes

### Breaking Changes
**None** - The refactoring maintains 100% API compatibility. All public methods remain unchanged.

### Internal Changes
- Static fields moved to appropriate partial class files
- Helper classes (`SpyHeaderHelpers`) moved to Operations.cs
- Private methods distributed across files by responsibility

### Testing Checklist
- [x] Build succeeds without errors
- [ ] All existing tests pass (if any)
- [ ] Manual smoke test: Execute existing procedures
- [ ] Verify element caching still works
- [ ] Verify procedure persistence (Load/Save)

## Future Improvements

### 1. **Interface Extraction**
Consider extracting interfaces for:
- `IElementResolver` - Element resolution logic
- `IOperationExecutor` - Operation execution
- `IProcedureStorage` - Persistence layer

### 2. **Dependency Injection**
- Remove static class constraint
- Inject dependencies (IElementResolver, IStorage, etc.)
- Enable better unit testing

### 3. **Operation Registry**
- Replace switch statements with operation registry pattern
- Enable runtime operation registration
- Simplify adding custom operations

### 4. **Async/Await Refactoring**
- Replace `Task.Run(() => ...)` with proper async methods
- Use `ConfigureAwait(false)` consistently
- Avoid blocking calls in async methods

### 5. **Error Handling**
- Create custom exception types
- Add structured logging (instead of Debug.WriteLine)
- Implement retry policies with Polly library

## Performance Considerations

### Cache Performance
- **Bookmark cache** (`_controlCache`): Reduces UI automation overhead
- **Element cache** (`_elementCache`): Avoids repeated element searches
- **Staleness detection**: Prevents using invalid element references

### Memory Usage
- Caches cleared before each execution (`_elementCache.Clear()`)
- HttpClient reused (singleton pattern)
- No memory leaks from stale element references

### Thread Safety
- All operations run on background thread via `Task.Run`
- UI thread access via `Dispatcher.Invoke` when needed
- Static caches: Not thread-safe (single execution at a time assumed)

## Documentation References
- Original file: `ProcedureExecutor.cs` (~1600 lines)
- See commit history for detailed changes
- Related classes: `UiBookmarks`, `NativeMouseHelper`, `MainViewModel`

## Contact
For questions or issues with this refactoring, contact the Radium development team.
