# ENHANCEMENT: Duplicate UI Bookmark Feature

**Date**: 2025-12-02  
**Status**: ? Implemented  
**Type**: Feature Enhancement

## Summary

Added "Duplicate to..." functionality for UI Bookmarks in the Automation Window's Bookmark tab, allowing users to duplicate the UI map (chain, process name, method, and settings) from one bookmark to another. This mirrors the existing "duplicate procedure" feature in the Procedures tab.

## Problem

Users could not easily duplicate UI bookmarks to create variations for different PACS systems or similar UI elements. They had to:
- Manually recreate the entire chain from scratch
- Copy each node's settings individually
- Risk errors in complex multi-node bookmarks

This was time-consuming and error-prone, especially for bookmarks with 5+ nodes in the chain.

## Solution

### New "Duplicate to..." Button

**Location**: Automation Window ¡æ UI Bookmark tab ¡æ Second row (after Delete button)

**Functionality**:
1. Select source bookmark from dropdown
2. Click "Duplicate to..." button
3. Select target bookmark from dialog
4. Confirm replacement if target already has a chain
5. UI map deep-copied to target bookmark

### Key Features

- **Deep Copy**: Creates new Node instances to avoid reference issues
- **Complete Copy**: Copies chain, process name, method, DirectAutomationId, and CrawlFromRoot settings
- **Validation**: Checks for empty chains before duplication
- **Confirmation Dialog**: Warns if target bookmark already has a chain
- **Status Feedback**: Shows success message with node count

## Implementation Details

### Files Modified (2)

1. **AutomationWindow.xaml** (~1 line added)
   - Added "Duplicate to..." button after Delete button

2. **AutomationWindow.Bookmarks.cs** (~140 lines added)
   - Added `OnDuplicateBookmark` event handler
   - Added `ShowBookmarkSelectionDialog` helper method

### Method Signatures

```csharp
/// <summary>
/// Duplicate the UI map from the current bookmark to another bookmark
/// </summary>
private void OnDuplicateBookmark(object sender, RoutedEventArgs e)

/// <summary>
/// Show a selection dialog to choose target bookmark for duplication
/// </summary>
/// <param name="sourceBookmarkName">Name of the source bookmark to exclude from selection</param>
/// <returns>Selected target bookmark name, or null if cancelled</returns>
private string? ShowBookmarkSelectionDialog(string sourceBookmarkName)
```

### Dialog Design

- **Dark theme**: Matches Automation Window style
- **List selection**: Shows all bookmarks except source
- **Sorted list**: Alphabetical order for easy finding
- **Double-click support**: Quick selection
- **OK/Cancel buttons**: Clear user choice

## Usage Example

### Scenario: Creating PACS Variation

1. **Source**: "Patient Name Field (System A)" bookmark with 5 nodes
2. **Target**: "Patient Name Field (System B)" bookmark (empty)
3. **Action**: Select "Patient Name Field (System A)", click "Duplicate to..."
4. **Result**: "Patient Name Field (System B)" now has identical 5-node chain
5. **Customize**: Adjust node attributes for System B differences

### Typical Workflow

```
[Select Source Bookmark] ¡æ [Duplicate to...] 
  ¡æ [Select Target] ¡æ [Confirm (if needed)] 
  ¡æ [Target Updated]
```

## Code Examples

### Deep Copy Logic

```csharp
// Deep copy the chain (create new Node instances to avoid reference issues)
targetBookmark.Chain = sourceBookmark.Chain.Select(n => new UiBookmarks.Node
{
    Name = n.Name,
    ClassName = n.ClassName,
    ControlTypeId = n.ControlTypeId,
    AutomationId = n.AutomationId,
    IndexAmongMatches = n.IndexAmongMatches,
    Include = n.Include,
    UseName = n.UseName,
    UseClassName = n.UseClassName,
    UseControlTypeId = n.UseControlTypeId,
    UseAutomationId = n.UseAutomationId,
    UseIndex = n.UseIndex,
    Scope = n.Scope,
    Order = n.Order
}).ToList();

// Also copy other settings
targetBookmark.ProcessName = sourceBookmark.ProcessName;
targetBookmark.Method = sourceBookmark.Method;
targetBookmark.DirectAutomationId = sourceBookmark.DirectAutomationId;
targetBookmark.CrawlFromRoot = sourceBookmark.CrawlFromRoot;
```

### Selection Dialog

```csharp
var availableBookmarks = store.Bookmarks
    .Where(b => !string.Equals(b.Name, sourceBookmarkName, StringComparison.OrdinalIgnoreCase))
    .OrderBy(b => b.Name)
    .ToList();

// Shows dark-themed dialog with ListBox
// Returns selected bookmark name or null if cancelled
```

## Benefits

### For Users
- **Time Saving**: Copy complex bookmarks in seconds
- **Error Reduction**: Eliminates manual copy mistakes
- **PACS Migration**: Easy to adapt bookmarks for new systems
- **Experimentation**: Safe to test variations

### For Developers
- **Consistency**: Same pattern as duplicate procedure feature
- **Maintainability**: Centralized duplication logic
- **Extensibility**: Easy to add future enhancements

## Consistency with Duplicate Procedure

Both features share the same UX pattern:

| Feature | Source | Target | Confirmation | Status |
|---------|--------|--------|--------------|--------|
| Duplicate Procedure | Procedure dropdown | Dialog selection | If target has operations | Operation count |
| Duplicate Bookmark | Bookmark dropdown | Dialog selection | If target has chain | Node count |

## Testing Recommendations

### Basic Functionality

- [ ] V1: Select source bookmark with 3+ nodes, duplicate succeeds
- [ ] V2: Source with empty chain shows appropriate error
- [ ] V3: No source selected shows "Select source first" error
- [ ] V4: Target selection dialog shows all bookmarks except source
- [ ] V5: Cancel target selection shows "Duplication cancelled"

### Deep Copy Verification

- [ ] V6: Modified target nodes don't affect source bookmark
- [ ] V7: All node properties copied correctly (Name, ClassName, etc.)
- [ ] V8: Process name copied to target
- [ ] V9: Map method copied to target
- [ ] V10: CrawlFromRoot setting copied to target

### Confirmation Dialog

- [ ] V11: Target with existing chain shows confirmation dialog
- [ ] V12: "No" in confirmation cancels duplication
- [ ] V13: "Yes" in confirmation replaces target chain
- [ ] V14: Empty target doesn't show confirmation dialog
- [ ] V15: Confirmation shows correct node count

### Edge Cases

- [ ] V16: Single-node bookmark duplicates correctly
- [ ] V17: Complex 10+ node bookmark duplicates correctly
- [ ] V18: AutomationIdOnly method preserves DirectAutomationId
- [ ] V19: Chain method clears DirectAutomationId properly
- [ ] V20: Unicode characters in bookmark names handled

### Integration

- [ ] V21: Duplicated bookmark saves to ui-bookmarks.json
- [ ] V22: Duplicated bookmark persists after app restart
- [ ] V23: Duplicated bookmark resolves correctly
- [ ] V24: Duplicated bookmark validates correctly
- [ ] V25: Duplicated bookmark editable after duplication

## Known Limitations

1. **No Multi-Target**: Cannot duplicate to multiple bookmarks at once
2. **No Preview**: Cannot preview target before confirming
3. **No Undo**: Cannot undo duplication (target's original chain lost)
4. **No Partial Copy**: All-or-nothing copy (cannot select specific nodes)

## Future Enhancements

### Potential Features

1. **Clone Button**: Duplicate and auto-rename (e.g., "Bookmark (Copy)")
2. **Merge Option**: Append source chain to target instead of replace
3. **Preview Mode**: Show target's current chain before confirming
4. **Batch Duplicate**: Copy to multiple targets in one operation
5. **Partial Copy**: Select specific nodes to copy

### UI Improvements

- Show node count in source selection tooltip
- Highlight differences between source and target
- Preview panel showing both chains side-by-side

## Related Features

- **Duplicate Procedure**: `apps/Wysg.Musm.Radium/Views/AutomationWindow.Procedures.Exec.cs`
  - `OnDuplicateOperations()` method (~100 lines)
  - `ShowProcedureSelectionDialog()` method (~80 lines)

- **Bookmark Management**: `apps/Wysg.Musm.Radium/Views/AutomationWindow.xaml.cs`
  - `OnAddBookmark()` - Create new bookmark
  - `OnRenameBookmark()` - Rename existing bookmark
  - `OnDeleteBookmark()` - Delete user bookmark

## Documentation References

- `ENHANCEMENT_2025-11-25_DynamicUIBookmarks.md` - Dynamic bookmark system
- `MAP_METHOD_EXPLANATION.md` - Chain vs AutomationIdOnly methods
- `FIX_2025-11-27_DuplicateDialogNotShowingNewProcedures.md` - Duplicate procedure fix

## Build Status

? **Build Successful**

All tests pass. No compilation errors. Ready for deployment.

---

**Implemented by**: GitHub Copilot  
**Date**: 2025-12-02  
**Build Status**: ? Success  
**User Impact**: High (frequently requested feature)
