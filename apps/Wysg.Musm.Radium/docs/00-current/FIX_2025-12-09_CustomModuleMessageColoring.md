# FIX: Custom Module "If Message" Coloring

**Date**: 2025-12-09  
**Type**: UI Fix  
**Component**: Automation Window ¡¤ Custom Module Library  
**Status**: ? Complete

---

## Problem

In the Automation tab, custom modules that execute the new `If message {procedure} is Yes` blocks only highlighted the word **If** in orange. The remaining text stayed in the default mint/gray combination, making it difficult to distinguish the control keywords from the procedure name. Designers requested a three-line treatment:

1. `If Message` (capital **M**) in orange
2. `{procedure}` on its own line in mint
3. `is Yes` on a third line in orange

Without this split, the control flow module blended in with normal procedures and users missed the conditional entry point during list scans.

---

## Solution

Updated `CustomModuleSyntaxConverter` to detect the literal pattern `If message ¡¦ is Yes` before applying generic syntax coloring. When the pattern matches, the converter now:

- Forces the heading to `If Message` and colors it with the standard keyword orange
- Places the procedure portion on its own line with the mint bookmark brush
- Adds a final `is Yes` line in orange, producing the requested stacked layout

Regular modules continue to use the existing keyword/property parsing pipeline.

### Files Modified
- `apps/Wysg.Musm.Radium/Converters/CustomModuleSyntaxConverter.cs`
  - Added a dedicated rendering path for message-if modules with the new three-line formatting.

---

## Testing
1. Open **Automation ¡æ Custom Modules** and locate an `If message ¡¦ is Yes` entry.
2. Confirm the entry now shows three lines with orange/mint/orange coloring.
3. Drag the module into an automation pane to ensure the list item uses the same formatting.
4. Verify other custom modules (Set/Run/Abort) still render with the previous syntax highlighting.

---

## Status
- ? Formatting fix implemented on 2025-12-09
- ? Ready for next Radium desktop build
