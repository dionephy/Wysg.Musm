# UiBookmarks Service - File Organization

The `UiBookmarks` service has been split into multiple partial class files for better maintainability and organization.

## File Structure

### ?? UiBookmarks.Main.cs
**Public API and Core Methods**
- `Resolve(KnownControl)` - Resolve bookmark by key
- `ResolveWithRetry(KnownControl, maxAttempts)` - Retry with progressive relaxation
- `TryResolveBookmark(Bookmark)` - Direct bookmark resolution
- `ResolvePath(Bookmark)` - Get full resolution path
- `TryResolveWithTrace(Bookmark)` - Resolution with detailed trace
- `RelaxBookmarkControlType()` - Constraint relaxation helpers
- `RelaxBookmarkClassName()` - Constraint relaxation helpers

### ?? UiBookmarks.Types.cs
**Type Definitions**
- `KnownControl` enum - Predefined UI control identifiers
- `MapMethod` enum - Resolution method (Chain, AutomationIdOnly)
- `SearchScope` enum - Search strategy (Children, Descendants)
- `Bookmark` class - UI element bookmark definition
- `Node` class - Single step in bookmark chain with INotifyPropertyChanged
- `Store` class - Bookmark storage container

### ?? UiBookmarks.Storage.cs
**Persistence Operations**
- `GetStorePath()` - Get JSON storage path
- `Load()` - Load bookmarks from storage
- `Save(Store)` - Save bookmarks to storage
- `SaveMapping(KnownControl, Bookmark)` - Save control mapping
- `GetMapping(KnownControl)` - Retrieve control mapping
- `GetStorePathOverride` - Override storage location

### ?? UiBookmarks.Resolution.cs
**Core Resolution Algorithms**
- `Walk()` - Main element resolution walker with retry logic
- `DiscoverRoots()` - Multi-strategy root window discovery
- `ManualFindMatches()` - Manual tree walking (Raw/Control views)
- `StepRetryCount` - Retry configuration constant
- `StepRetryDelayMs` - Retry delay configuration

### ?? UiBookmarks.Helpers.cs
**Utility Methods**
- `OrderNodes()` - Sort bookmark chain by order
- `BuildAndCondition()` - Build FlaUI search condition from node
- `ElementMatchesNode()` - Check if element matches node criteria
- `CloneWithoutControlType()` - Create relaxed node copy
- `CalculateNodeSimilarity()` - Score root window similarity
- `SafeName()`, `SafeClass()`, `SafeAutoId()`, `SafeHandle()` - Safe property accessors

## Design Rationale

### Why Partial Classes?
- **Single Responsibility**: Each file focuses on one aspect (API, types, storage, resolution, helpers)
- **Better Navigation**: Easier to find specific functionality
- **Reduced Merge Conflicts**: Multiple developers can work on different aspects
- **Improved Readability**: Smaller files are easier to understand and maintain

### Dependencies Between Files
```
UiBookmarks.Main.cs
  戍式? UiBookmarks.Types.cs (Bookmark, Node, KnownControl)
  戍式? UiBookmarks.Storage.cs (Load, GetMapping)
  戍式? UiBookmarks.Resolution.cs (Walk, DiscoverRoots)
  戌式? UiBookmarks.Helpers.cs (OrderNodes, BuildAndCondition, Safe*)

UiBookmarks.Resolution.cs
  戍式? UiBookmarks.Types.cs (Bookmark, Node, SearchScope)
  戌式? UiBookmarks.Helpers.cs (All helper methods)

UiBookmarks.Storage.cs
  戌式? UiBookmarks.Types.cs (Store, Bookmark)
```

## Usage Example

The API remains exactly the same - no breaking changes:

```csharp
// Resolve by known control
var (hwnd, element) = UiBookmarks.Resolve(KnownControl.ReportText);

// Resolve with retry and relaxation
var (hwnd, element) = UiBookmarks.ResolveWithRetry(KnownControl.WorklistWindow, maxAttempts: 3);

// Save a bookmark
UiBookmarks.SaveMapping(KnownControl.TestInvoke, myBookmark);

// Get detailed trace
var (hwnd, element, trace) = UiBookmarks.TryResolveWithTrace(bookmark);
```

## Performance Optimizations (2025-02-02)

The Resolution.cs file contains recent fast-fail optimizations:
- `StepRetryCount = 1` - Reduced retry attempts
- Early exit on "not supported" errors
- Skip expensive fallbacks when permanent errors detected

See `PERFORMANCE_2025-02-02_UiBookmarksFastFail.md` for details.

## Future Improvements

Potential future splits if files grow too large:
- `UiBookmarks.Resolution.Walk.cs` - Walk method only
- `UiBookmarks.Resolution.Discovery.cs` - DiscoverRoots only
- `UiBookmarks.Helpers.Conditions.cs` - FlaUI condition building
- `UiBookmarks.Helpers.Matchers.cs` - Element matching logic

---

**Created**: 2025-02-02  
**Last Updated**: 2025-02-02  
**Total Lines**: ~900 (split from single 900-line file)
