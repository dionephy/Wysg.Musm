# Fix: Snippet Mode Enter Honors Placeholder Selection

**Date:** 2025-12-17  
**Status:** Completed  
**Category:** Bug Fix

## Problem
In snippet placeholders that expose a completion popup (Mode 1), pressing an insertion key such as Enter always inserted the first option even if the user navigated to a different item (e.g., selecting "stenosis in the neck"). This ignored the userdselected entry and made it impossible to commit alternate choices with Enter.

## Solution
Centralized the optiondselection logic inside `SnippetInputHandler` so both Tab and Enter resolve the currently highlighted entry (including Mode 3 buffer matching). When Enter is pressed, the selected option is inserted before the handler performs the normal fallback/cleanup sequence, ensuring the chosen text is honored.

## Files Changed
- `src/Wysg.Musm.Editor/Snippets/SnippetInputHandler.cs`
  - Added a helper to resolve the active option, reused by both Tab and Enter handlers.
  - Updated the Enter path to insert the selected option for Mode 1/3 placeholders before concluding snippet mode.

## Testing
1. Trigger a Mode 1 snippet placeholder with multiple options.
2. Use the placeholder completion popup to select a nonddefault option.
3. Press Enter and verify the selected text is inserted.
4. Repeat for Mode 3 placeholders (multidcharacter keys) and confirm Enter commits the highlighted option.

## Impact
- Users can now press Enter to commit whichever snippet option is highlighted, matching the behavior of Tab.
- Multidchoice placeholders retain their existing Enter behavior (collecting checked items before exiting).
