# FIX: Reportified editors stay read-only and auto-toggle off on edit attempts
**Date**: 2025-12-31  
**Status**: Completed  
**Scope**: Current report editors (findings, conclusion)

## Problem
When `Reportified` was ON, findings and conclusion editors were visually read-only but pressing **Enter** (or other typing keys) still modified the local editor buffer. Because bindings were one-way in this state, the UI changed while the view model stayed unchanged, leaving editors out of sync and breaking bindings. 

## Changes
- Added a `ReadOnlyEditAttempted` event to `EditorControl` and hardened key/text handlers so **all edit keystrokes are blocked when `IsReadOnly` is true** (navigation and copy/select remain allowed).
- Wired the findings/conclusion editors in `CurrentReportEditorPanel` to the new event; any edit attempt while `Reportified` is active automatically turns **off both Reportified and Proofread toggles**, keeping state consistent.

## Files Touched
- `src/Wysg.Musm.Editor/Controls/EditorControl.Api.cs` ? new event hook for read-only edit attempts.
- `src/Wysg.Musm.Editor/Controls/EditorControl.Popup.cs` ? guards in key/text handlers to block edits when read-only and raise the event.
- `apps/Wysg.Musm.Radium/Controls/CurrentReportEditorPanel.xaml` ? hooked findings/conclusion editors to the event.
- `apps/Wysg.Musm.Radium/Controls/CurrentReportEditorPanel.xaml.cs` ? handler to turn off Reportified + Proofread when an edit is attempted in read-only mode.

## Expected Behavior
- With `Reportified` ON, findings/conclusion editors are **strictly read-only**; typing, Enter, Backspace, Delete, Paste, etc. are blocked.
- If the user tries to edit while in this mode, `Reportified` and `Proofread` are turned **off** automatically, returning the editors to an editable state with raw text.

## Testing
- Manual: Toggle `Reportified` ON, attempt to press **Enter** or type in findings/conclusion; verify no text change occurs and both toggles flip off after the attempt.
- Regression: Navigation and copy/select still work while read-only; normal editing resumes when toggles are off.
