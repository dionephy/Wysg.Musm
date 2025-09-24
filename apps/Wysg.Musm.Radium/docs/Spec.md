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

## 5) Custom Procedure Operation: GetValueFromSelection
Purpose
- Allow a procedure step to extract the value of a specific column header from the currently selected row of a list (e.g., mapped SearchResultsList) reusing the Row Data parsing logic.

Definition
- Operation name: `GetValueFromSelection`.
- Arg1: Element (required) ¡æ list control (e.g., SearchResultsList mapping).
- Arg2: String (required) ¡æ header text to extract. Example: `ID`, `Accession No.`, `Status`.
- Output: value of the first column whose normalized header equals Arg2 (case-insensitive). If not exact match, the first header containing Arg2 substring is used. If still not found ¡æ null.

Normalization Rules (reuse existing):
- Accession ¡æ Accession No.
- Study Description ¡æ Study Desc
- Institution Name ¡æ Institution
- BirthDate ¡æ Birth Date
- BodyPart ¡æ Body Part

Behavior
- If no selection exists ¡æ preview "(no selection)" .
- If header not found ¡æ preview `(Arg2 not found)`.
- On success preview = extracted value; stored in `var#` for chaining.
- Default Arg2 value set to "ID" when operation selected with empty Arg2.

Implementation Notes
- Added to Operation ComboBox list in Custom Procedures grid.
- Preset rules in `OnProcOpChanged` assign Arg1=Element, Arg2=String, enable Arg2 and seed default.
- Execution implemented in both single-row Set (ExecuteSingle) and full Run (RunProcedure) flows.
- Reuses `GetHeaderTexts` + `GetRowCellValues` + `NormalizeHeader` helpers already present in SpyWindow code-behind.

## 6) Row Data & Selection Value Alignment Preservation
Change (Earlier Iteration)
- `GetRowCellValues` now always adds placeholders for blank cells (no skipping) to keep header/value alignment.

## 7) Blank Header Column Preservation (Current Iteration)
Problem
- Some lists contain a leading blank header cell (first column intentionally unlabeled). Previous logic skipped blank headers which then shifted all subsequent header/value associations (e.g. Status value appearing under ID header).

Solution
- `GetHeaderTexts` now adds an empty string entry for every header cell (even if blank after probing descendants). This preserves positional alignment with the row cell values list.
- Row Data formatter logic updated:
  - Build pairs (Header, Value) with normalized headers.
  - When both header and value are blank ¡æ pair omitted entirely.
  - When header blank but value present ¡æ output just `Value` (no colon label) preserving its sequence.
  - When header present ¡æ `Header: Value` as before.

Result
- Leading blank header no longer causes value shifts.
- Example corrected: either leading blank pair omitted (if value also blank) giving: `Status: Examined | ID: 239499 | Name: ±èº¹¸¸ | ... | Institution: BUSAN ST.MARY'S HOSPITAL` OR if value present for first blank header: `<value> | Status: Examined | ID: ...` maintaining order.

Testing Scenarios
1. Leading blank header with blank value ¡æ dropped; no shift.
2. Leading blank header with nonblank value ¡æ value emitted alone at start.
3. Internal blank header between populated headers ¡æ value-only token preserved in position.
4. `GetValueFromSelection` unaffected; indexing uses parallel arrays including blank header placeholders.

## 8) Operation: ToDateTime
Purpose
- Convert a string variable containing a date or date-time in formats `YYYY-MM-DD` or `YYYY-MM-DD HH:mm:ss` into a normalized DateTime ISO string stored in a new var.

Definition
- Operation name: `ToDateTime`.
- Arg1: Var (string content) required.
- Arg2: (unused) disabled.
- Output: if parse succeeds, stored as ISO8601 (`o`) and preview shows `yyyy-MM-dd HH:mm:ss`; else preview `(parse fail)`.

Parse Rules
- Exact match using `InvariantCulture` for the two patterns.
- `AssumeLocal` style.

## 9) New PACS Selection Methods
Added Methods (Search Results list unless noted):
- Get selected name from search results list
- Get selected sex from search results list
- Get selected birth date from search results list
- Get selected age from search results list
- Get selected studyname from search results list
- Get selected study date time from search results list
- Get selected radiologist from search results list
- Get selected study remark from search results list
- Get selected report date time from search results list
- Get selected studyname from related studies list
- Get selected study date time from related studies list
- Get selected radiologist from related studies list
- Get selected report date time from related studies list

Implementation
- Added ComboBox items with Tags matching new service method identifiers.
- `PacsService` includes one helper `GetValueFromListSelection` that resolves either `SearchResultsList` or `RelatedStudyList`, reads the selected row, aligns headers/cells preserving blanks, and returns first non-empty candidate among provided header names (exact then contains).
- For ID existing method kept but refactored to reuse header selection logic.
- Headers probed: Name, Sex, Birth Date, Age, Study Desc (aliases Study Description, Studyname), Study Date, Requesting Doctor (alias Radiologist/Doctor), Study Comments (Remark), Report approval dttm (Report Date/Time).

Notes
- All methods return raw string (no trimming beyond whitespace removal). Date/time values can be piped through `ToDateTime` in a procedure if normalization needed.

## 10) Studyname DB Diagnostics (Current Iteration)
Purpose
- Provide in-app verification that data is being read from the intended local database without relying only on connection tests.

Implementation
- Repository interface adds `GetDiagnosticsAsync` returning counts for key tables (rad_studyname, rad_study, mapping table) and connection metadata (db, host, user, port, table name, source, timestamp).
- `StudynameLoincRepository` implements the method, infers mapping table name (existing logic) and builds a diagnostics record.
- ViewModel exposes `DiagnosticsCommand` and `DiagnosticsInfo` (string) and writes to Debug output.
- UI adds a Diagnostics button and readonly TextBox (multi-line) at bottom of Studyname ¡ê LOINC Parts window.

Usage
- Press Diagnostics to see counts; if zero unexpectedly user can confirm DB empty vs. query failure (would show error message).
- Source field shows whether connection came from LocalConnectionString vs fallback.

Future
- Potentially add latency timings and last modified timestamps.

## 11) Postgres First-Chance Exception Sampling
Purpose
- Reduce noise from repeated first-chance `PostgresException` during startup while still surfacing the first unique root cause.

Implementation
- `PgDebug.Initialize()` registers a first-chance handler capturing `PostgresException` once per (SqlState|MessageText) pair and logs via Serilog at Warning level.
- Added initialization in `App.OnStartup` before splash login.
- Repository methods now log explicit PostgresException details with SqlState and server message for triage.

Usage
- Inspect log output (Serilog sinks) to identify the first logged Postgres error instead of many debugger popups.
- After resolving root cause, the flood should disappear.

## 12) PhraseService Connection Refactor
Purpose
- Remove hardcoded connection string causing failures when local DB name differs (3D000) and align with settings infrastructure.

Changes
- PhraseService now consumes `IRadiumLocalSettings.LocalConnectionString` with legacy fallback.
- Added debug logging of host/db/user when opening connections.
- Sets minimal timeouts and includes error detail.

Future
- Planned migration to use `CentralConnectionString` (central phrases) once consolidation is ready; code comments note this.
