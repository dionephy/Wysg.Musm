# Fix: Completion Window No Longer Commits on '/' or '-'

**Date:** 2025-12-26  
**Status:** Completed  
**Category:** Bug Fix

## Problem
Typing `/` or `-` while the completion popup was open caused the selected suggestion to commit automatically. Users expected these characters to be inserted as-is (for tokens like `L5-S1` or `C/O`), but the popup committed and consumed the character.

## Solution
Updated the completion input handler to bypass completion commits when the typed character is `/` or `-`. These keys now flow through as normal typing without triggering popup insertion, while other punctuation commit behavior remains unchanged.

## Files Changed
- `src/Wysg.Musm.Editor/Controls/EditorControl.Popup.cs`

## Testing
1. Open the editor and trigger the completion popup (type a prefix that shows suggestions).
2. Press `/` while the popup is visible.
   - The `/` should be inserted, and the completion should not commit.
3. Press `-` while the popup is visible.
   - The `-` should be inserted, and the completion should not commit.
4. Press punctuation like `,` or `.` while the popup is visible with a selection.
   - Completion should still commit and reinsert the punctuation.
