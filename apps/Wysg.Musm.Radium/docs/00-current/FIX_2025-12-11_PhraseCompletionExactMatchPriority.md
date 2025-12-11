# Fix: Phrase Completion Exact Match Priority

**Date:** 2025-12-11  
**Status:** Completed  
**Category:** Bug Fix

## Problem
While typing short tokens such as "no" the completion popup listed snippets and hotkeys ahead of the phrase database entry that exactly matched the typed word. Committing the first suggestion (via Space/Enter) therefore triggered the hotkey/snippet instead of the intended phrase, even though the phrase text matched perfectly.

## Solution
Boost the priority that the completion list assigns to phrase items when their text exactly matches the current word. Exact phrase matches now receive a large priority bonus before sorting, ensuring they always float to the top of the popup, ahead of snippets and hotkeys.

```csharp
if (!IsSnippet && !IsHotkey && matchKey.Equals(input, StringComparison.OrdinalIgnoreCase))
{
    Priority += ExactPhrasePriorityBoost;
    return;
}
```

## Files Changed
- `src/Wysg.Musm.Editor/Snippets/MusmCompletionData.cs`
  - Added an `ExactPhrasePriorityBoost` constant and applied it when a phrase exactly matches the user input.

## Testing
1. Launch Radium and focus the report editor.
2. Type a short phrase token (e.g., `no`).
3. Observe that the matching phrase now appears first in the completion popup even if snippets/hotkeys share the same letters.
4. Press Space to commit and verify the phrase text inserts instead of the hotkey/snippet.

## Impact
- Exact phrase matches now take precedence, preventing unintended hotkey/snippet expansion when the user expects phrase insertion.
- Non-exact matches retain existing ordering; no changes to snippet or hotkey configuration are required.
