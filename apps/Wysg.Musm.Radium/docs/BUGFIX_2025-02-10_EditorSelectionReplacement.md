# Bug Fix: Editor Selection Replacement (2025-02-10)

## Overview
Fixed abnormal text editor behavior where pressing Enter or typing characters when text is selected would not delete the selected text first, causing the selected text to move downward instead of being replaced.

## Issue Description

### Problem
When text is selected in the editor and a key is pressed:
- **Enter**: Selected text moved downward (blank line added before the text) instead of being replaced with a newline
- **Space**: Nothing happened instead of replacing selected text with a space
- **Character keys**: Nothing happened instead of replacing selected text with the typed character

### Expected Behavior (Standard Text Editor)
When text is selected and a key is pressed:
- **Enter**: Delete selected text and add a newline at that position
- **Space**: Delete selected text and add a space at that position
- **Character keys**: Delete selected text and add the typed character at that position

## Root Cause

The issue was in `EditorControl.Popup.cs`:

1. **OnTextAreaPreviewKeyDown** method:
   - When Enter key was pressed without completion window, the code inserted a newline at the caret position
   - It did NOT check if there was a text selection or delete it first
   - When Space key was pressed without completion window, same issue occurred

2. **OnTextEntering** method:
   - This event fires for all text input (characters, space, etc.)
   - The code did not delete the selection before the text was inserted
   - AvalonEdit's default behavior was being prevented by completion window logic

## Solution Implemented

### 1. Fixed Enter Key Handling
Added selection deletion logic before inserting newline:

```csharp
// FIXED: Check if there's a selection and delete it before inserting newline
var selection = Editor.TextArea?.Selection;
int insertOffset = Editor.CaretOffset;

if (selection != null && !selection.IsEmpty)
{
    var segment = selection.SurroundingSegment;
    if (segment != null && segment.Length > 0)
    {
        // Delete selected text
        Editor.Document.Remove(segment.Offset, segment.Length);
        insertOffset = segment.Offset;
        // Clear selection
        Editor.TextArea.ClearSelection();
    }
}

// Insert newline at the correct position
var nl = Environment.NewLine;
Editor.Document.Insert(insertOffset, nl);
Editor.CaretOffset = insertOffset + nl.Length;
```

### 2. Fixed Space Key Handling
Added selection deletion logic before inserting space:

```csharp
// FIXED: Check if there's a selection and delete it before inserting space
var selection = Editor.TextArea?.Selection;
int insertOffset = Editor.CaretOffset;

if (selection != null && !selection.IsEmpty)
{
    var segment = selection.SurroundingSegment;
    if (segment != null && segment.Length > 0)
    {
        // Delete selected text
        Editor.Document.Remove(segment.Offset, segment.Length);
        insertOffset = segment.Offset;
        // Clear selection
        Editor.TextArea.ClearSelection();
    }
}

// Insert space at the correct position
Editor.Document.Insert(insertOffset, " ");
Editor.CaretOffset = insertOffset + 1;
```

### 3. Fixed Character Input Handling
Added selection deletion logic in `OnTextEntering` before any character is inserted:

```csharp
private void OnTextEntering(object? s, System.Windows.Input.TextCompositionEventArgs e)
{
    // FIXED: Delete selection before inserting character (normal text editor behavior)
    if (!string.IsNullOrEmpty(e.Text))
    {
        var selection = Editor.TextArea?.Selection;
        if (selection != null && !selection.IsEmpty)
        {
            var segment = selection.SurroundingSegment;
            if (segment != null && segment.Length > 0)
            {
                // Delete selected text
                Editor.Document.Remove(segment.Offset, segment.Length);
                Editor.CaretOffset = segment.Offset;
                // Clear selection
                Editor.TextArea.ClearSelection();
            }
        }
    }

    // ...existing completion window logic...
}
```

## Testing Scenarios

### Before Fix
1. Select text "chest pain"
2. Press Enter ¡æ Text moves down: `\n chest pain` ?
3. Select text "chest pain"
4. Press Space ¡æ Nothing happens ?
5. Select text "chest pain"
6. Type "a" ¡æ Nothing happens ?

### After Fix
1. Select text "chest pain"
2. Press Enter ¡æ Text deleted, newline inserted: `\n` ?
3. Select text "chest pain"
4. Press Space ¡æ Text deleted, space inserted: ` ` ?
5. Select text "chest pain"
6. Type "a" ¡æ Text deleted, "a" inserted: `a` ?

## Affected Components

### Files Modified
- `src\Wysg.Musm.Editor\Controls\EditorControl.Popup.cs`
  - Modified `OnTextEntering` method
  - Modified `OnTextAreaPreviewKeyDown` method (Enter key handling)
  - Modified `OnTextAreaPreviewKeyDown` method (Space key handling)

### Components Using EditorControl
All editor instances in the application now have proper selection replacement behavior:
- Header editor
- Findings editor
- Conclusion editor
- Previous report editors
- All other EditorControl instances

## Benefits

1. **Standard Behavior**: Editor now behaves like standard text editors (Visual Studio Code, Notepad++, etc.)
2. **Better UX**: Users can now delete text by selecting and typing, which is intuitive and expected
3. **Consistent Behavior**: All editor instances have consistent selection replacement behavior
4. **No Breaking Changes**: Existing functionality is preserved; only the selection replacement behavior is fixed

## Compatibility

- **WPF Version**: .NET 9 / WPF 9.0
- **OS**: Windows 10+
- **AvalonEdit**: Compatible with existing AvalonEdit usage

## Known Limitations

None identified. The fix implements standard text editor behavior.

## Future Enhancements (Not Implemented)

- [ ] Add unit tests for selection replacement behavior
- [ ] Add visual feedback when selection is about to be replaced
- [ ] Support undo/redo for selection replacement operations (AvalonEdit already supports this)

## Related Issues

- None (this was a new bug report)

## References

- AvalonEdit Documentation: https://github.com/icsharpcode/AvalonEdit
- WPF TextBox Selection: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/textbox-overview

---

**Status**: ? **COMPLETE**  
**Build Status**: ? **Successful**  
**Testing**: ? **Verified**
