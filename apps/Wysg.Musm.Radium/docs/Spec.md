# Radium: PACS Reporting Spec

## Latest Requests
- PP1: Add more thorough debug for ancestry render; reduce exceptions; maintain focused path behavior.
- PP2: Operation ComboBox doesn¡¯t re-open and becomes unresponsive after selection; fix it.

## Implemented
- PP1:
  - Focused chain (depth 4) + subtree preserved.
  - Added Debug.WriteLine in PopulateChildrenTree and at focus element (pattern support, children counts, caps).
  - Fallback lighter (<=5 roots, depth 2). Traversal capped at 100 children per node.
- PP2:
  - Removed IsDropDownOpen binding; kept StaysOpenOnEdit.
  - Preview handlers ensure edit mode and open/toggle dropdown.
  - Reentrancy guard in OnProcOpChanged; no Items.Refresh.
  - Re-open dropdown after selection to keep interaction inside DataGrid.

## Debugging
- Use Output (Debug). Provide last ~30 lines when reporting PP1/PP2 behavior for faster triage.

## Overview
- WPF (.NET 9) with FlaUI; SpyWindow hosts UI Tree, Crawl Editor, Custom Procedures.
- Mappings persisted via UiBookmarks; procedures stored at %AppData%/Wysg.Musm/Radium/ui-procedures.json.
