# Fix: Completion Window Punctuation Commit Regression

**Date:** 2025-12-19  
**Status:** Completed  
**Category:** Bug Fix

## Problem
Committing a completion with punctuation keys (`,`, `.`, `;`, `:`, `)` etc.) sometimes dropped the typed character when the selected item was not marked as a hotkey. Typing `br` then `,` produced `brain` instead of `brain,`, forcing users to retype punctuation.

## Root Cause
`OnTextEntering` only reinserted punctuation when the selected completion was flagged as `IsHotkey`. Phrase/snippet completions bypassed the reinsertion path, so the commit consumed the punctuation character without adding it to the document.

## Solution
Always reinsert non-whitespace punctuation immediately after requesting completion insertion while the popup is open. The handler now appends the typed punctuation for any completion type, preserving whitespace handling and selection deletion behavior.

## Files Changed
- `src/Wysg.Musm.Editor/Controls/EditorControl.Popup.cs`

## Testing
1. Trigger completion (e.g., type `br`).
2. With the popup item selected, press punctuation keys like `,`, `.`, `;`, `:`, `)`.
3. Verify the completion commits and the pressed punctuation remains (`brain,`, `brain.` etc.).
4. Confirm Space/Enter commits still behave as before and whitespace handling is unchanged.
