# Fix: Digit Support in Hotkey Triggers (2025-11-04)

## Problem

Users reported two issues with hotkey completion:

1. **Hotkeys with digits not working**: Typing "zz1" would show completions for "zz", but typing the "1" would cause all completions to disappear
2. **Duplicate descriptions in display**: Hotkeys displayed as "zz1 ¡æ RLLf ¡æ RLLf" instead of "zz1 ¡æ RLLf"

## Root Cause

### Issue 1: Word Extraction Ignoring Digits

In `MainViewModel.EditorInit.cs`, the `CompositeProvider.GetWordBeforeCaret` method used `char.IsLetter` to extract word characters:

```csharp
while (i >= 0 && char.IsLetter(text[i])) i--;
```

This meant:
- Typing "zz" extracted "zz" ?
- Typing "zz1" extracted "zz" (digit ignored) ?
- The provider filtered hotkeys by "zz", found matches
- But when typing "1", the word remained "zz" (because digit was skipped)
- The completion window showed items for "zz", not "zz1"
- No matches for "zz1" ¡æ window closed

### Issue 2: Pre-formatted Display String

In the same file, hotkeys were passed to `MusmCompletionData.Hotkey` with a pre-formatted display string:

```csharp
var display = $"{hk.TriggerText} ¡æ {desc}";  // Creates "zz1 ¡æ RLLf"
yield return MusmCompletionData.Hotkey(display, hk.ExpansionText, description: null);
```

Then `MusmCompletionData.Hotkey` added its own formatting:

```csharp
var content = $"{trigger} ¡æ {expanded}";  // Adds " ¡æ RLLf" again!
```

Result: `"zz1 ¡æ RLLf ¡æ RLLf"`

## Solution

### Fix 1: Use WordBoundaryHelper for Digit Support

Updated `GetWordBeforeCaret` to use the same word boundary logic as other providers:

```csharp
private static (string word, int startOffset) GetWordBeforeCaret(ICSharpCode.AvalonEdit.TextEditor editor)
{
    int caret = editor.CaretOffset;
    var line = editor.Document.GetLineByOffset(caret);
    string lineText = editor.Document.GetText(line);
    int local = Math.Clamp(caret - line.Offset, 0, lineText.Length);
    
    // Use WordBoundaryHelper to include digits, hyphens, and underscores
    var (startLocal, endLocal) = WordBoundaryHelper.ComputeWordSpan(lineText, local);
    int start = line.Offset + startLocal;
    string word = endLocal > startLocal ? lineText.Substring(startLocal, endLocal - startLocal) : string.Empty;
    
    return (word, start);
}
```

This now correctly extracts:
- "zz" when caret after "zz"
- "zz1" when caret after "zz1"
- "zz13" when caret after "zz13"

### Fix 2: Pass Only Trigger Text

Updated hotkey provider to pass only the trigger text:

```csharp
// Pass ONLY the trigger text as first parameter
// MusmCompletionData.Hotkey will format the display as "trigger ¡æ expansion"
yield return Wysg.Musm.Editor.Snippets.MusmCompletionData.Hotkey(
    hk.TriggerText,      // ? Just "zz1"
    hk.ExpansionText,    // Expansion text
    description: hk.Description  // Optional description for tooltip
);
```

Now `MusmCompletionData.Hotkey` creates the display once: `"zz1 ¡æ RLLf"`

## Files Modified

1. **apps/Wysg.Musm.Radium/ViewModels/MainViewModel.EditorInit.cs**
   - Added `using Wysg.Musm.Editor.Completion;` for WordBoundaryHelper
   - Updated `CompositeProvider.GetWordBeforeCaret` to use WordBoundaryHelper
   - Updated hotkey yielding to pass only trigger text (removed pre-formatting)

2. **src/Wysg.Musm.Editor/Snippets/MusmCompletionData.cs**
   - Updated `Hotkey` factory method to format content as `"{trigger} ¡æ {expansion}"`
   - (This ensures single formatting, no duplication)

## Test Plan

### Manual Testing

1. **Basic digit support**:
   - Type "zz1" ¡æ verify completion window shows "zz1 ¡æ RLLf"
   - Press Tab/Space ¡æ verify expands to "RLLf"

2. **Multi-digit triggers**:
   - Type "zz13" ¡æ verify completion window shows "zz13 ¡æ BLLf"
   - Type "zz7" ¡æ verify shows "zz79 ¡æ BULf" when available

3. **Display format**:
   - Type any hotkey trigger ¡æ verify display shows "trigger ¡æ expansion" (single arrow, no duplication)
   - Verify completion list shows clean display for all hotkeys

4. **Mixed content**:
   - Type letter prefix ¡æ verify shows mix of phrases, hotkeys, and snippets
   - Type digit prefix ¡æ verify filters correctly
   - Type alphanumeric ¡æ verify includes all matching items

### Edge Cases

- Empty triggers ¡æ should not appear
- Triggers with only letters ¡æ should work as before
- Triggers with hyphens (e.g., "t2-") ¡æ should extract correctly
- Triggers with underscores (e.g., "test_1") ¡æ should extract correctly

## Performance Impact

- **Minimal**: WordBoundaryHelper.ComputeWordSpan is already used by PhraseCompletionProvider and HotkeyCompletionProvider
- No additional allocations (same span-based logic)
- No database or network calls added

## Related Issues

- FIX_2025-11-04_NumberDigitsInTriggers.md (this file)
- WordBoundaryHelper was introduced to unify word extraction logic across all providers
- PhraseCompletionProvider and HotkeyCompletionProvider already used this approach

## Verification

Build passes: ?

Manual testing shows:
- "zz1" now triggers completion window with correct items ?
- Hotkeys display as "zz1 ¡æ RLLf" (no duplication) ?
- All hotkey triggers with digits now work correctly ?

