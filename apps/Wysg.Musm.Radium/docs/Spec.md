# Radium: Feature Spec (Cumulative)

## 1) Spy UI Tree focus behavior
- Show a single chain down to level 4 (root ¡æ ... ¡æ level 4).
- Expand the children of the level-4 element; depth bounded by `FocusSubtreeMaxDepth`.
- Depths configurable via constants in `SpyWindow`.

## 2) Procedures grid argument editors
- Operation presets must set Arg1/Arg2 types and enablement, with immediate editor switch.
- Element/Var editors use dark ComboBoxes.

## 3) PACS Methods (Custom Procedures): "Get selected ID from search results list"
- Add a new method entry to SpyWindow ¡æ Custom Procedures ¡æ PACS Method list: "Get selected ID from search results list".
- Implement a PACS service method to read the currently selected row from the mapped Search Results list and return the Study ID (or Accession if labeled differently).
- The list element is resolved via UiBookmarks KnownControl = SearchResultsList.

Implemented
- PP1: Tree mirrors crawl editor logic. Chain is built to level 4 and children of that element are populated (not deeper ancestors), with caps.
- PP2: Removed commit/refresh and forced re-open from SelectionChanged to stop infinite re-open loop and row recycling/removal; preset still sets Arg types and enablement.
- `ProcArg`/`ProcOpRow` implement INotifyPropertyChanged so templates update immediately when types change.
- PP3: Added new PACS method option in SpyWindow (Custom Procedures): Tag="GetSelectedIdFromSearchResults" labeled "Get selected ID from search results list"; Implemented PacsService.GetSelectedIdFromSearchResultsAsync.

## 4) Studyname ¡ê LOINC Parts window
Goal
- Provide clear mapping UI from med.rad_studyname to LOINC Parts.

Database
- Tables: med.rad_studyname, med.rad_studyname_loinc_part; source schema loinc (loinc.part, loinc.rplaybook, loinc.loinc_term)

Studynames List
- View: bind ListBox to StudynamesView (ICollectionView) so textbox above filters by Studyname
- VM: LoadAsync calls GetStudynamesAsync; auto-select first

LOINC Parts UI
- 4x5 grid of category listboxes (equal row heights), each with its own filter and vertical-only scrollbar
- Items show only part_name (no part_number)
- Double-click on an item adds it to Mapping Preview with Sequence Order input (default "A"); duplicates allowed
- Preview panel: shows each item with an editable PartSequenceOrder TextBox; double-click removes the item
- Sequence Order input placed at the right side of the Preview header
- Common list shows most-used parts (from repo aggregation), spanning two cells

Playbook Suggestions (under Preview)
- With ¡Ã 2 distinct selected parts, suggest playbooks grouped by loinc_number (each LOINC spans multiple part rows)
  - LEFT: long_common_name (distinct by loinc_number)
  - RIGHT: the playbook parts list for the selected loinc_number (part_name + part_sequence_order)

Acceptance Criteria
- Single main window shown on run (no duplicate windows)
- Studynames textbox filters in real-time
- long_common_name suggestions appear under Preview when ¡Ã 2 parts match one or more loinc_number groups
- Selecting a long_common_name shows its constituent parts and orders
- Build succeeds

Notes / Future Enhancements
- Remove/reorder controls for preview
- Virtualization and count badges
