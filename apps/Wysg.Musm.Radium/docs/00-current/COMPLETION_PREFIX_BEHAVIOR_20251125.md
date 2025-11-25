# COMPLETION PREFIX BEHAVIOR CHANGE

**Date**: 2025-11-25  
**Type**: Enhancement  
**Severity**: Medium (UX Improvement)  
**Component**: Editor Completion System  

## Problem Description

Previously, the completion system determined the string of interest by finding the **full word** containing the caret, extending both left and right from the caret position.

### Example of Old Behavior
```
Text: "no, br|heart is"  (| represents caret)
String of interest: "brheart"  (full word containing caret)
Completion suggestions: Words starting with "brheart"
```

This caused unintuitive behavior when the caret was in the middle of a word - users would get completions based on the entire word, not just what they had typed so far.

## Solution

Changed the completion system to only consider the **prefix before the caret** (from the last break character to the caret), not extending beyond the caret position.

### Example of New Behavior
```
Text: "no, br|heart is"  (| represents caret)
String of interest: "br"  (only from break to caret)
Completion suggestions: Words starting with "br"
```

## Implementation Details

### 1. Added New Method to WordBoundaryHelper

**File**: `src/Wysg.Musm.Editor/Completion/WordBoundaryHelper.cs`

```csharp
/// <summary>
/// Computes only the prefix before the caret (from last break to caret).
/// For completion scenarios: "no, br|heart" returns (4, 6) for "br".
/// </summary>
public static (int startLocal, int endLocal) ComputePrefixBeforeCaret(string lineText, int caretLocal)
{
    if (string.IsNullOrEmpty(lineText)) return (0, 0);
    caretLocal = Math.Clamp(caretLocal, 0, lineText.Length);

    int left = caretLocal - 1;
    while (left >= 0 && IsWordChar(lineText[left])) left--;
    int start = left + 1;

    // Only extend to caret, not beyond
    int end = caretLocal;

    return (start, end);
}
```

**Key Difference**: The `end` position is set to `caretLocal` instead of scanning right to find the word boundary.

### 2. Updated Completion Components

Updated all completion-related code to use `ComputePrefixBeforeCaret` instead of `ComputeWordSpan`:

#### Files Modified:
- ? `src/Wysg.Musm.Editor/Completion/WordBoundaryHelper.cs` - Added new method
- ? `apps/Wysg.Musm.Radium/ViewModels/PhraseCompletionProvider.cs` - Updated GetWordBeforeCaret
- ? `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.EditorInit.cs` - Updated CompositeProvider.GetWordBeforeCaret
- ? `src/Wysg.Musm.Editor/Controls/EditorControl.Popup.cs` - Updated GetCurrentWord and OnTextEntered
- ? `src/Wysg.Musm.Editor/Completion/MusmCompletionWindow.cs` - Updated TryGetWordAtCaret and ComputeReplaceRegionFromCaret

### 3. Word Boundary Definition

Word characters are defined as:
- Letters (a-z, A-Z)
- Digits (0-9)
- Underscore (_)
- Hyphen (-)

Break characters include:
- Space
- Comma
- Period
- Any other non-word character

## Before vs After

### Scenario 1: Caret in Middle of Word
```
Before:
  Text: "no, br|heart"
  String: "brheart"
  Completions: Empty (no match)

After:
  Text: "no, br|heart"
  String: "br"
  Completions: "brain", "breast", "bronchi", etc.
```

### Scenario 2: Caret at End of Word
```
Before:
  Text: "no, br|"
  String: "br"
  Completions: "brain", "breast", etc.

After:
  Text: "no, br|"
  String: "br"
  Completions: "brain", "breast", etc.
  (Same behavior - no change)
```

### Scenario 3: Typing New Word After Space
```
Before:
  Text: "no, b|"
  String: "b"
  Completions: "brain", "breast", "bone", etc.

After:
  Text: "no, b|"
  String: "b"
  Completions: "brain", "breast", "bone", etc.
  (Same behavior - no change)
```

## User Experience Impact

### Positive Changes
? **More intuitive completion**: Users get suggestions based on what they've typed so far
? **Better mid-word editing**: Inserting text in the middle of a word shows relevant completions
? **Consistent with modern editors**: Matches behavior of VS Code, Visual Studio, and other IDEs
? **Faster phrase discovery**: Users can start typing anywhere and get relevant suggestions

### No Negative Impact
- Completion still works correctly at end of words
- No performance degradation
- All existing tests pass
- Build succeeds without errors

## Technical Details

### Replace Region Calculation

When a completion item is selected, the replace region is calculated as:
- **StartOffset**: Position of first word character after last break
- **EndOffset**: Current caret position

This means selecting a completion item will:
1. Replace text from the last break to the caret
2. Leave text after the caret unchanged

Example:
```
Text: "no, br|heart is"
Select: "brain"
Result: "no, brain|heart is"
```

### Backward Compatibility

The old `ComputeWordSpan` method is kept for backward compatibility and marked as deprecated:

```csharp
/// <summary>
/// Computes the full word span containing the caret (extending both left and right).
/// DEPRECATED: Use ComputePrefixBeforeCaret for completion scenarios.
/// </summary>
public static (int startLocal, int endLocal) ComputeWordSpan(string lineText, int caretLocal)
```

If any other components need the old behavior, they can still use this method.

## Testing

### Debug Logging

Comprehensive debug logging has been added to help diagnose the completion behavior:

**WordBoundaryHelper.ComputePrefixBeforeCaret:**
- Logs line text and caret position
- Logs each character scanned while moving left
- Logs the final prefix extracted

**EditorControl.GetCurrentWord and OnTextEntered:**
- Logs caret position and line offset
- Logs the line text being analyzed
- Logs the extracted word/prefix

**MusmCompletionWindow:**
- Logs word extraction in TryGetWordAtCaret
- Logs replace region computation in ComputeReplaceRegionFromCaret

**PhraseCompletionProvider:**
- Logs prefix extraction details
- Logs the query being sent to the phrase service

To view debug logs:
1. Run the application in Debug mode
2. Open the Output window in Visual Studio (View > Output)
3. Select "Debug" from the "Show output from" dropdown
4. Type in the editor and watch the logs to see prefix extraction in action

Example log output for "no, br|heart":
```
[EditorControl.OnTextEntered] Caret: 6, LineOffset: 0, Local: 6
[EditorControl.OnTextEntered] LineText: 'no, brheart'
[EditorControl.OnTextEntered] Text entered: 'r'
[ComputePrefixBeforeCaret] LineText: 'no, brheart', CaretLocal: 6
[ComputePrefixBeforeCaret]   Scanning left: pos=5, char='r', isWordChar=true
[ComputePrefixBeforeCaret]   Scanning left: pos=4, char='b', isWordChar=true
[ComputePrefixBeforeCaret]   Stopped at: pos=3, char=' ', isWordChar=false
[ComputePrefixBeforeCaret] Result: start=4, end=6, prefix='br'
[EditorControl.OnTextEntered] Extracted word: 'br' (start=4, startLocal=4, endLocal=6)
```

### Manual Testing Scenarios

1. **Type new word**: "no, b|" ¡æ Shows completions starting with "b"
2. **Mid-word editing**: "no, br|heart" ¡æ Shows completions starting with "br"
3. **After comma**: "heart, l|" ¡æ Shows completions starting with "l"
4. **After period**: "no finding. N|" ¡æ Shows completions starting with "N"
5. **With hyphen**: "T2-w|" ¡æ Shows completions starting with "T2-w"

### Build Status
? **Build**: Successful  
? **Compilation errors**: None  
? **Runtime tests**: Pass

## Future Considerations

### Potential Enhancements
1. **Smart completion after selection**: Automatically remove duplicate text after caret when inserting completion
2. **Fuzzy matching**: Consider fuzzy matching for completion (e.g., "brh" matches "brain heart")
3. **Context-aware completion**: Adjust completion based on surrounding text context

### Performance Monitoring
- Monitor completion trigger frequency
- Track average prefix length
- Measure completion selection rates

## Related Documentation

- **Completion Window**: `src/Wysg.Musm.Editor/Completion/MusmCompletionWindow.cs`
- **Word Boundary Helper**: `src/Wysg.Musm.Editor/Completion/WordBoundaryHelper.cs`
- **Phrase Completion**: `apps/Wysg.Musm.Radium/ViewModels/PhraseCompletionProvider.cs`

## Migration Notes

### For Developers
- Use `ComputePrefixBeforeCaret` for all new completion features
- The old `ComputeWordSpan` method should only be used for non-completion scenarios
- No API changes required - all changes are internal

### For Users
- No configuration changes needed
- Completion behavior will automatically use the new logic
- No data migration required

## Conclusion

This change makes the completion system more intuitive by only considering the text the user has typed so far (from the last break to the caret), rather than the entire word containing the caret. This aligns with modern editor behavior and improves the user experience, especially when editing in the middle of existing text.

---

**Status**: ? IMPLEMENTED  
**Build**: ? Successful  
**Impact**: Low (Internal change, no API breakage)  
**Testing**: Ready for user verification
