# FIX: Inline Goto Entries Not Executing

**Date**: 2025-12-11  
**Type**: Automation Runtime Fix  
**Component**: MainViewModel `RunModulesSequentially`  
**Status**: ? Complete

---

## Problem
Automation sequences allow users to type `Goto LabelName:` directly into a pane without creating a matching custom module. The runtime only recognized goto instructions that originated from the module library, so inline entries (e.g., `Goto RetrySendMessage:` in the Send Report shortcut) were treated as plain text. As a result, retry loops never jumped back to their labels and the flow continued forward, producing empty report exports instead of retrying the send.

---

## Solution
Added a lightweight parser that recognizes inline goto tokens and routes them through the same execution path as stored modules. Both module-based and inline gotos now share a single helper that validates the target label, tracks hop counts, and updates the instruction pointer.

### 2025-12-11 Follow-up
Discovered that any entry ending with `:` was being treated as a label before the parser ran, so commands such as `Goto RetrySendMessage:` were swallowed as no-ops. Updated `RunModulesSequentially` to skip the label fast-path when a line starts with `Goto`, ensuring inline entries now reach the shared goto resolver.

### 2025-12-11 Follow-up #2
False `If` blocks still executed their embedded `Goto` and `If Modality with Header` modules, causing runaway hop counts and unexpected modality prompts. Added `skipExecution` guards so these modules are ignored when their parent condition fails, matching the behavior of other built-in operations.

### Files Updated
- `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.Automation.Core.cs`
  - Added `TryParseInlineGoto` and `TryResolveGotoTarget` helpers.
  - Reused the helper for existing modules to keep hop tracking consistent.

---

## Testing
1. Place a label such as `RetrySendMessage:` inside an automation sequence without adding a corresponding custom module.
2. Insert a `Goto RetrySendMessage:` line typed manually.
3. Trigger the automation and force the failure path so the goto should execute.
4. Confirm the log now shows the jump occurring and the block after the label re-runs.
5. Repeat with a library-based goto module to ensure both paths behave identically.
