# Fix: Completion Window Preserves Punctuation Keys

**Date:** 2025-12-17  
**Status:** Completed  
**Category:** Bug Fix

## Problem
When committing an item from the completion popup with punctuation keys (`,`, `.`, `;`, `:`, `)`, etc.), Radium inserted only the selected suggestion and silently dropped the key that triggered the commit. Typing `br` followed by `,` produced `brain` instead of the expected `brain,`, forcing users to retype the punctuation manually.

## Solution
Intercept non-alphanumeric text input during completion commits and explicitly reinsert the typed characters (excluding whitespace, which already has dedicated handling). After the completion entry replaces the current word, the editor now appends the punctuation at the caret position so the final text matches what the user typed (`brain,`, `brain.` and so on). As of 2025-12-17, the reinsertion logic is scoped to **hotkey** completions so snippets and phrases continue to follow their original formatting rules. The handler now reads the selected completion directly from the popup `ListBox`, ensuring the hotkey detection flag is set correctly before reinserting punctuation.

## Files Changed
- `src/Wysg.Musm.Editor/Controls/EditorControl.Popup.cs`
  - Updated `OnTextEntering` to detect punctuation-triggered commits, run `RequestInsertion`, and then insert the typed punctuation while keeping whitespace handling unchanged. The reinsertion now checks whether the selected completion is a hotkey (resolved via the ListBox selection) before adding punctuation.

## Testing
1. Open the Radium editor and trigger the completion popup (e.g., type `br`).
2. Press punctuation keys such as `,`, `.`, `;`, `:`, or `)` while a **hotkey** item is selected.
3. Verify that the completion commits and the pressed punctuation appears immediately after the inserted word (e.g., `brain,`).
4. Confirm that pressing Space/Enter or selecting snippets/phrases continues to behave as before.

## Impact
- Restores expected typing flow when using punctuation to commit hotkey completions.
- Snippet and phrase commits remain unchanged, preserving their formatting semantics.
