# Quick Reference: Editor Selection Replacement Fix (2025-11-10)

**Date**: 2025-11-25  
**Type**: Quick Reference  
**Category**: User Reference  
**Status**: ? Active

---

## Summary

This guide provides quick reference information for users. For detailed implementation information, see the related plan and specification documents.

---

# Quick Reference: Editor Selection Replacement Fix (2025-11-10)

## Problem
Selected text in editors was not being deleted when Enter or characters were pressed.

## Solution
Added selection deletion logic in three places in `EditorControl.Popup.cs`:
1. `OnTextEntering` - for all character input
2. `OnTextAreaPreviewKeyDown` - for Enter key
3. `OnTextAreaPreviewKeyDown` - for Space key

## Behavior Change

### Before
- Select text + Enter → Text moves down ?
- Select text + Space → Nothing happens ?
- Select text + Character → Nothing happens ?

### After
- Select text + Enter → Text deleted, newline inserted ?
- Select text + Space → Text deleted, space inserted ?
- Select text + Character → Text deleted, character inserted ?

## Files Changed
- `src\Wysg.Musm.Editor\Controls\EditorControl.Popup.cs`

## Build Status
? Successful

## Documentation
See `BUGFIX_2025-11-10_EditorSelectionReplacement.md` for complete details.

