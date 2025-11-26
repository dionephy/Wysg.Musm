# ClickElementAndStay Operation

## Overview
`ClickElementAndStay` is a UI automation operation that clicks an element and **leaves the cursor at the clicked location** (unlike `ClickElement` which restores the cursor to its original position).

## Use Cases
- **Hover effects**: When you need the cursor to remain on the element after clicking to trigger hover-dependent UI
- **Context menus**: Click to open a menu and keep cursor positioned for immediate interaction
- **Chained interactions**: Click an element and immediately interact with nearby UI without cursor jumping
- **Debugging**: Visually see where the click occurred by leaving cursor in place

## Syntax

### Operation
`ClickElementAndStay`

### Arguments
- **Arg1 (Element or Var)**: The element to click
  - **Element**: Reference to a bookmarked control (e.g., `SearchResultsList`)
  - **Var**: Variable containing a cached element from `GetSelectedElement` (e.g., `var1`)

### Output
- Preview: `(clicked and stayed at X,Y)` or `(no element)` or `(no bounds)` or `(error)`
- Value: `null` (no output variable)

## Examples

### Example 1: Click bookmarked element and stay
```
ClickElementAndStay(SearchResultsList)
```
- Clicks center of `SearchResultsList` element
- Cursor remains at clicked position

### Example 2: Click selected item and stay
```
GetSelectedElement(SearchResultsList) ¡æ var1
ClickElementAndStay(var1)
```
- Gets the currently selected item from list
- Clicks that item
- Cursor stays at clicked position (useful for context menus)

### Example 3: Sequential clicks without cursor restoration
```
GetSelectedElement(Worklist) ¡æ var1
ClickElementAndStay(var1)
ClickElementAndStay(ContextMenuItem)
```
- Click selected item ¡æ cursor stays
- Click context menu item that appeared near cursor

## Implementation Details

### Cursor Behavior
- **ClickElement**: Uses `NativeMouseHelper.ClickScreenWithRestore(x, y)`
  - Saves original cursor position
  - Moves to element center
  - Clicks
  - **Restores cursor to original position**

- **ClickElementAndStay**: Uses `NativeMouseHelper.ClickScreen(x, y)`
  - Moves to element center
  - Clicks
  - **Leaves cursor at element center**

### Element Resolution
Both operations support:
1. **Bookmark references**: Direct element lookup via `UiBookmarks.Resolve()`
2. **Variable references**: Cached elements from `GetSelectedElement` output

### Error Handling
- Returns `(no element)` if element cannot be resolved
- Returns `(no bounds)` if element has zero width/height
- Returns `(error)` on exception (with message in AutomationWindow, without in ProcedureExecutor)

## Comparison: ClickElement vs ClickElementAndStay

| Aspect | ClickElement | ClickElementAndStay |
|--------|-------------|---------------------|
| **Cursor restore** | Yes (returns to original position) | No (stays at clicked position) |
| **Use case** | Isolated clicks, automation | Hover effects, context menus |
| **Native method** | `ClickScreenWithRestore()` | `ClickScreen()` |
| **Preview** | `(clicked element center X,Y)` | `(clicked and stayed at X,Y)` |

## See Also
- [GET_SELECTED_ELEMENT.md](GET_SELECTED_ELEMENT.md) - Get selected items for clicking
- [AutomationWindow.Procedures.Exec.cs](../Views/AutomationWindow.Procedures.Exec.cs) - Operation implementation
- [ProcedureExecutor.cs](../Services/ProcedureExecutor.cs) - Runtime executor
- [NativeMouseHelper.cs](../Services/NativeMouseHelper.cs) - Native mouse operations
