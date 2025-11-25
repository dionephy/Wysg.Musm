# Enhancement: Alt+Left Copy to Caret Position

**Date**: 2025-11-11  
**Type**: UI Enhancement

---

## What Changed

Modified Alt+Left navigation from previous report editors (`editorPreviousFindings`, `editorPreviousConclusion`, `editorPreviousHeader`) to current findings editor (`editorFindings`) to paste copied text at the current caret position instead of appending at the end.

---

## Why This Matters

- **Precise Insertion** - Users can now paste previous report content exactly where they want it in the current report
- **Better Editing Workflow** - No need to manually move text after pasting
- **Consistent with Standard Editors** - Matches behavior of standard text editors (VS Code, Word, Notepad++)
- **Improved Efficiency** - Reduces manual repositioning steps

---

## Behavior Changes

### Before
```
User in editorPreviousFindings:
  1. Selects text "No acute findings"
  2. Presses Alt+Left
  3. Text is appended to END of editorFindings: "...existing text\nNo acute findings"
  4. Caret positioned at end
  5. User must manually cut and reposition text if insertion point was in middle
```

### After
```
User in editorPreviousFindings:
  1. Positions caret in editorFindings at desired location (e.g., middle of paragraph)
  2. Returns to editorPreviousFindings
  3. Selects text "No acute findings"
  4. Presses Alt+Left
  5. Text is inserted AT CARET: "...text before[No acute findings]text after..."
  6. Caret positioned after inserted text
  7. No repositioning needed ?
```

---

## Technical Implementation

### Old Implementation (Append to End)
```csharp
if (copyText && !string.IsNullOrEmpty(sourceEditor.SelectedText))
{
    // Has selection and copying enabled: copy to end of target
    var selectedText = sourceEditor.SelectedText;
    var targetText = targetEditor.Text ?? string.Empty;
    
    if (!string.IsNullOrEmpty(targetText))
    {
        targetEditor.Text = targetText + "\n" + selectedText;
    }
    else
    {
        targetEditor.Text = selectedText;
    }
    
    targetEditor.Focus();
    targetEditor.CaretOffset = targetEditor.Text.Length;
}
```

### New Implementation (Insert at Caret)
```csharp
if (copyText && !string.IsNullOrEmpty(sourceEditor.SelectedText))
{
    // Has selection and copying enabled: insert at current caret position
    var selectedText = sourceEditor.SelectedText;
    int caretOffset = targetEditor.CaretOffset;
    
    // Insert selected text at caret position
    targetEditor.Document.Insert(caretOffset, selectedText);
    
    // Move focus to target and position caret after inserted text
    targetEditor.Focus();
    targetEditor.CaretOffset = caretOffset + selectedText.Length;
}
```

---

## Affected Navigation Paths

The following Alt+Left navigation paths now paste at caret position:

1. **editorPreviousFindings �� editorFindings** (Alt+Left with selection)
   - Copy previous findings to current findings at caret

2. **editorPreviousConclusion �� editorFindings** (Alt+Left with selection)
   - Copy previous conclusion to current findings at caret

3. **editorPreviousHeader �� editorFindings** (Alt+Left with selection)
   - Copy previous header to current findings at caret

---

## Usage Examples

### Example 1: Insert Previous Findings in Middle of Current Report
```
Current Findings (caret at |):
"There is mild cerebral atrophy. |
Additional findings are as follows:"

Previous Findings (selected text):
"No acute intracranial hemorrhage or infarction."

After Alt+Left:
"There is mild cerebral atrophy. No acute intracranial hemorrhage or infarction.|
Additional findings are as follows:"
```

### Example 2: Prepend Previous Conclusion to Current Findings
```
Current Findings (caret at start |):
"|Normal exam."

Previous Conclusion (selected text):
"Comparison with prior CT shows interval improvement."

After Alt+Left:
"Comparison with prior CT shows interval improvement.|Normal exam."
```

### Example 3: Insert Previous Header Notes
```
Current Findings (caret in middle |):
"The patient presents with chest pain. |
CT angiography was performed."

Previous Header (selected text):
"History of coronary artery disease."

After Alt+Left:
"The patient presents with chest pain. History of coronary artery disease.|
CT angiography was performed."
```

---

## Non-Copyable Navigation (Unchanged)

These navigation paths remain unchanged (focus only, no text copy):

- EditorFindings �� EditorPreviousFindings (Alt+Right) - focus only
- EditorFindings �� EditorConclusion (Alt+Up/Down) - focus only with caret at end
- Previous report internal navigation (Alt+Up/Down) - focus only

---

## Key Technical Details

### AvalonEdit Document.Insert()
- Uses `TextDocument.Insert(offset, text)` method
- Offset is the character position in the document
- Insertion happens atomically (no text replacement logic needed)
- Preserves undo/redo history

### Caret Position Management
- `CaretOffset` property tracks current caret position (character offset from start)
- After insertion: `CaretOffset = originalOffset + insertedTextLength`
- Caret positioned immediately after inserted text for continued typing

### Empty Target Handling
- If target editor is empty (CaretOffset = 0), text is inserted at start
- No special handling needed - same logic works for empty and non-empty documents

---

## Edge Cases Handled

### Caret at End
```
Current: "Text here|" (caret at end, offset = 9)
Insert: "New text"
Result: "Text hereNew text|" (caret after insertion)
```

### Caret at Start
```
Current: "|Text here" (caret at start, offset = 0)
Insert: "New text"
Result: "New text|Text here" (caret after insertion)
```

### Empty Document
```
Current: "|" (empty, offset = 0)
Insert: "New text"
Result: "New text|" (caret after insertion)
```

### Selection Exists in Target (Overwrite)
- Current implementation does NOT delete existing selection
- Text is inserted at caret position (selection start)
- To overwrite selection, user should delete it first (standard editor behavior)

---

## Key File Changes

- `apps\Wysg.Musm.Radium\Controls\CenterEditingArea.xaml.cs` - Modified `HandleEditorNavigation()` method

---

## Build Status

? **Build successful** - No compilation errors

---

## Testing Checklist

- [ ] Test Alt+Left from editorPreviousFindings with selection �� inserts at caret in editorFindings
- [ ] Test Alt+Left from editorPreviousConclusion with selection �� inserts at caret in editorFindings
- [ ] Test Alt+Left from editorPreviousHeader with selection �� inserts at caret in editorFindings
- [ ] Test with caret at start of document
- [ ] Test with caret in middle of document
- [ ] Test with caret at end of document
- [ ] Test with empty target document
- [ ] Test with selection in target (insertion should happen at selection start)
- [ ] Test without selection in source (should just move focus, no text copy)
- [ ] Verify other Alt+Arrow navigation paths still work correctly

---

## Benefits

- ? Paste at precise location without manual repositioning
- ? Matches standard editor behavior (VS Code, Word, etc.)
- ? Reduces workflow friction for comparing and merging reports
- ? Better integration with existing editing patterns
- ? No breaking changes to non-copyable navigation

---

## Related Documentation

- **Navigation Map**: `QUICKREF_2025-11-11_AltArrowNavigationMap.md`
- **Navigation Directions**: `ENHANCEMENT_2025-11-11_AltArrowNavigationDirectionChanges.md`
- **Original Alt+Arrow Implementation**: `TROUBLESHOOTING_2025-01-30_AltArrowSystemKey.md`
