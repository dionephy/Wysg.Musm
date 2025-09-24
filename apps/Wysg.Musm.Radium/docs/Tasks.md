# Radium Task Checklist (Cumulative)

## SpyWindow ? UI Tree
- [x] Parity with crawl editor via `ResolvePath` (exact chosen path)
- [x] Chain to level 4, expand from that node (bounded depth and cap 100 children)
- [x] Fallbacks: process roots ¡æ desktop-wide first-node ¡æ heuristic

## SpyWindow ? Procedures Grid
- [x] Stable presets; no loop; instant editor switching; dark ComboBoxes
- [x] Add operation: GetValueFromSelection (Arg1 Element=list, Arg2 String=header, default "ID")
- [x] Preserve blank cell placeholders in Row Data & selection extraction (alignment-safe)
- [x] Preserve blank header placeholders (leading/internal). Row Data formatting skips only header+value both blank, value-only token if header blank.
- [x] Add ToDateTime operation (parse yyyy-MM-dd / yyyy-MM-dd HH:mm:ss) storing ISO string
- [x] Add PACS selection methods (name, sex, birth date, age, studyname, study datetime, radiologist, study remark, report datetime) for search & related lists
- [x] Add Studyname DB diagnostics (repository GetDiagnosticsAsync, VM command, UI display)
- [x] Add Postgres first-chance exception sampler (PgDebug) and initial repo logging
- [x] Refactor PhraseService to use LocalConnectionString (remove hardcoded, future central migration note)

## PACS Methods ? Custom Procedures
- [x] Add "Get selected ID from search results list" to PACS Method list in SpyWindow (tag: GetSelectedIdFromSearchResults)
- [x] Implement PacsService.GetSelectedIdFromSearchResultsAsync (resolve list, read selected row, detect ID column, return value)
- [ ] Optional: Hook procedure runner to call service methods by tag and display result

## Studyname ¡ê LOINC Parts window
- [x] Repo: prefer LocalConnectionString (then fallback local) before Central; add diagnostics
- [x] VM: Categories with filters; MappingPreviewItem; SequenceOrderInput; CommonGroup; part_name-only
- [x] VM: Explicit properties bound into a 4x5 Grid
- [x] XAML: Equal-height rows; vertical-only scroll; wrap text; code-behind double-click add+delete; preview editable order; add studyname input under list
- [x] Studynames filter via ICollectionView
- [x] Playbook suggestions grouped by loinc_number; details pane shows part names + sequence order
- [x] Single MainWindow on run (removed StartupUri; splash controls flow)
- [x] Playbook threshold lowered to 2 (converter + VM)
- [x] Playbook matches ListBox vertical scrollbar added (PP1 layout fix)
- [x] Double-click playbook match imports all parts (async-safe, pair uniqueness) (PP2, PP3)
- [x] Double-click playbook part adds that part if absent (pair uniqueness)
- [x] Build passes
- [ ] Add remove/reorder controls for preview
- [ ] Virtualization and header count badges
- [ ] Repo tests with test DB
- [x] Remove Diagnostics button and textbox from XAML
- [x] Remove DiagnosticsCommand and DiagnosticsInfo from ViewModel
- [x] Verify Save button enabled when Studyname selected
- [x] Add StatusMessage property + textbox binding
- [x] Update StatusMessage on AddStudyname and Save
- [x] Add Close button Click handler to close window

## Follow-ups
- [ ] Settings UI for depths
- [ ] Rank multiple first-node fallbacks by proximity to final mapping
- [ ] Hook procedure runner (GetSelectedIdFromSearchResults) integration output area
- [ ] JSON export of Row Data preserving blanks
