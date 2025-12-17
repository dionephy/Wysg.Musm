# Fix: Suppress Completion Popup for Purely Numeric Tokens

**Date:** 2025-12-17  
**Status:** Completed  
**Category:** Bug Fix

## Problem
Typing a standalone number (for example `3`) triggered the completion popup, and committing with Space would insert the first suggestion (e.g., the hotkey `3rd`). Users had to dismiss the window manually just to enter simple numeric text.

## Solution
During `OnTextEntered`, we now inspect the word immediately before the caret. If it consists only of digits, the completion popup is closed (or never opened) so typing numbers behaves like a normal editor. As soon as the word contains any non-digit characters (e.g., `3r`), completion resumes as usual.

## Files Changed
- `src/Wysg.Musm.Editor/Controls/EditorControl.Popup.cs`
  - Added a numeric-only guard before building completion items; numeric tokens short-circuit the routine and close any existing popup.

## Testing
1. Type `3` in the editor and observe that no completion popup appears.
2. Press Space and verify the text becomes `3 ` (with no hotkey expansion).
3. Type `3r` and confirm that the completion popup opens and committing still works (`3rd`).

## Impact
- Eliminates unintended hotkey/snippet activation while entering pure numbers.
- Existing completion behavior for alphanumeric words remains unchanged.
