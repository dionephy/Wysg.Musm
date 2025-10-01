# Feature Specification: Radium Cumulative – Reporting Workflow, Editor Experience, PACS & Studyname→LOINC Mapping

## Update: Previous report ingestion refinements (2025-10-05)
- **FR-152a** Ensure AddStudy always populates `header_and_findings` and `conclusion` (attempt retrieval regardless of banner validity) and only display studies with at least one report row containing either field.

## Update: Tree default disabled & new PACS getters (2025-10-05)
- **FR-148** SpyWindow TreeView MUST be disabled (hidden) by default; user enables via checkbox.
- **FR-149** After a Pick capture, system MUST auto-clear (uncheck) UseIndex for all nodes in captured chain.
- **FR-150** Add new bookmark target `ReportText2` for alternate report text control mapping.
- **FR-151** Add PACS procedure method tags & service accessors: GetCurrentFindings, GetCurrentConclusion, GetCurrentFindings2, GetCurrentConclusion2.
- **FR-152** When AddStudy command invoked: gather selected related study metadata, ensure rad_study, verify banner matches patient/study date; pull findings/conclusion (choose longer variant); build partial report JSON with only `header_and_findings` and `conclusion` populated plus duplication in `findings`; insert med.rad_report row (is_created=false, is_mine=false, created_by=radiologist, report_datetime=report date); refresh PreviousStudies from DB.

## Prior Recent Updates
- FR-146 Birth date persistence.
- FR-147 Tree toggle user control.
- FR-144/FR-145 earlier (see history).

---

## Update: Completion Navigation Guard (2025-09-30)
- **FR-131** Completion popup MUST treat the first Down/Up navigation after the editor regains focus as selecting the natural boundary item (first/last) even if a prior selection was left behind, by recalculating navigation state whenever the list is rebuilt and when the list lacks keyboard focus, and the editor MUST adjust selection without raising guard-driven clears so the very next key advances immediately without requiring duplicate keystrokes.
- **FR-132** Completion popup selection guard MUST prevent recursive event handling and properly handle navigation by using a guard flag to prevent infinite recursion during programmatic selection changes.
- **FR-133** Completion popup MUST: (a) never skip intermediate items during sequential Up/Down navigation (one keypress → move exactly one item), and (b) cap visible item height to at most 8 items (dynamic ListBox MaxHeight) while allowing scroll for overflow.
- **FR-134** Completion popup MUST auto-size its height exactly to (items_count * measured_item_height + padding) when item_count ≤ 8, and clamp to 8-items height when item_count > 8, updating after list rebuilds and after selection-induced layout changes.

## Update: Bug Fixes (2025-01-01)
- **FR-126** Phrase extraction window MUST use proper dependency injection to prevent service injection errors during phrase save operations.
- **FR-127** Completion popup Down/Up key navigation MUST work reliably by properly handling keyboard events and selection state management.
- **FR-128** Completion popup selection guard MUST prevent recursive clearing while allowing legitimate keyboard navigation by temporarily disabling selection event handlers during programmatic changes.
- **FR-129** Completion popup MUST preserve selections that are added via keyboard navigation by detecting multiple SelectionChanged events from single user actions and allowing new selections without interference.
- **FR-130** Completion popup keyboard navigation MUST correctly handle first vs subsequent navigation by tracking user navigation state rather than relying on selection index, ensuring first Down key always selects first item (index 0).

## Update: Editor Completion Improvements (2025-01-01)
- **FR-124** Completion list MUST refresh immediately when new phrases are added via phrase extractor or phrase manager by clearing completion cache on phrase upsert/toggle.
- **FR-125** Down/Up key navigation MUST work properly in completion popup by allowing selection changes during keyboard navigation.

## Update: Phrase Extraction Window (Dereportified Source + Non-blocking Save)
- Extraction now always uses dereportified versions of current header/findings/conclusion even if UI is in reportified state (uses MainViewModel.GetDereportifiedSections()).
- Save Selected executes asynchronously with IsBusy gating; UI remains responsive (no freeze on network latency).
- Save command disabled while background save in progress; re-enabled after completion.
- Central DB upsert updates in-memory snapshot immediately (single source of truth) — no separate local DB write needed.

---
# Feature Specification: Radium Cumulative – Reporting Workflow, Editor Experience, PACS & Studyname→LOINC Mapping

(Updated: account_id replaces tenant_id; phrase completion now uses in‑memory snapshot. Added OCR element text operation and current patient/study PACS banner extraction.)

**Feature Branch**: `[radium-cumulative]`  
**Created**: 2025-09-28  
**Status**: Draft (living cumulative spec)  
**Input**: User description (evolving), prior implemented feature notes (migrated to Appendix)

---
## Execution Flow (main)
```
1. Parse cumulative scope (reporting workflow + editor + mapping + PACS helpers + OCR utilities)
2. Extract key concepts: studies, previous reports, reporting pipeline, editor assistance, mapping, PACS submission, OCR banner parsing
3. Mark ambiguities
4. Define user scenarios (radiologist daily workflow + assistant automation)
5. Generate functional requirements (testable, numbered FR-###)
6. Identify key entities (Report, PrevReport, Study, Mapping, GhostSuggestion, Snippet, ProcedureOp)
7. Run review checklist – implementation details moved to Appendix
8. Return SUCCESS (spec ready for planning updates)
```
---


## ⚡ Quick Guidelines
- Focuses on WHAT value Radium delivers to radiologist and supporting assistants
- Technical HOW (algorithms, classes, UI control specifics) deferred to plan & appendix
- Ambiguities explicitly tagged

---


## User Scenarios & Testing (mandatory)

### Primary User Story – Current Study Reporting
A radiologist selects the current study in PACS. Radium automatically extracts patient & study metadata, prompts for missing mappings (LOINC, technique), generates initial chief complaint/history preview from study & patient remarks, and prepares editable header fields. Radiologist optionally adjusts technique/comparison and then types findings assisted by completions, snippets, and AI ghost suggestions. Post‑processing produces proofread & formatted header/findings/conclusion which can be sent back to PACS in one action.

### Secondary User Story – Previous Study Reference
Radiologist requests a previous study. Radium retrieves prior report text, splits header vs findings, parses structured header fields, proofreads components, and surfaces selectable comparison content to include in the current report.

### Mapping Story – Studyname→LOINC Parts
Mapping specialist (or radiologist) opens Studyname→LOINC window, filters studynames, assembles parts via category lists and/or playbook suggestions, adjusts sequence ordering, and saves mapping so future automatic technique / standardized naming can occur without manual prompts.

### Editor Productivity Story
While entering findings, the editor provides: phrase / hotkey / snippet completions after minimal typing; optional idle ghost (AI) multi‑line suggestions without interrupting typing; snippet placeholder navigation; ability to import and transform prior study findings via contextual action. Completion list updates immediately when new phrases are added.

### PACS Submission Story
On completion, user initiates "Send to PACS". System validates banner metadata matches current study, injects formatted (reportified + numbered) sections into the respective PACS fields, confirms acknowledgment, and persists final structured report + study metadata locally.

### OCR & Live Banner Story
User or automation needs quick extraction of patient number or study date/time from currently visible viewer banner (even if not mapped in UI tree). System provides lightweight OCR procedure operation and dedicated PACS methods to retrieve these values for chaining in custom procedures.

### Acceptance Scenarios
1. Given a current study with unmapped studyname, when opened, then LOINC mapping window appears before editing proceeds (unless mapping already exists).
2. Given modality = MR and missing technique mapping, when current study loads, then technique mapping capture window appears and upon confirmation Report.technique is populated.
3. Given prior report text, when user requests previous study, then system produces structured fields (chief complaint, history, technique, comparison) and proofread variants without modifying original source text.
4. Given the user types a word of ≥ MinCharsForSuggest letters, when matches exist, then completion popup appears restricted to that word span and only inserts on explicit accept (Enter or delimiter) – never auto‑commits.
5. Given user is idle (≥ GhostIdleMs) with no completion popup open, when suggestions available, then ghost suggestions render and can be accepted with Tab.
6. Given findings text entered, when postprocess run, then conclusion preview and all proofread fields are generated exactly once per invocation and are traceable.
7. Given Send to PACS executed, when PACS acknowledgment received, then system stores final report payload locally atomically (header+findings+conclusion with metadata) or reports failure with reason.
8. Given the user adds a GetTextOCR operation to a procedure with an element target, then on run the system attempts OCR inside the element bounds and returns extracted text (or explicit reason markers when engine unavailable / empty).
9. Given the user invokes new PACS methods (Get current patient number / Get current study date time) in procedures, then values derived from banner (digits cluster / date pattern) are returned when present.
10. **Given user adds new phrases via phrase extractor or phrase manager, when user types prefix of new phrase in editor, then completion popup shows new phrases immediately without restart.**
11. **Given completion popup is open and user presses Down/Up keys, then selection changes to next/previous item respectively.**
12. **Given user clicks "Extract Phrases" button, when phrase extraction window opens, then all services are properly injected and phrase saving works without errors.**
13. **Given completion popup with multiple items is displayed and user presses Down key repeatedly, then selection moves through all items sequentially without skipping or stopping.**
14. **Given completion popup selection guard is active, when legitimate keyboard navigation occurs, then selection changes are preserved without being cleared by the guard mechanism.**
15. **Given multiple SelectionChanged events fire from a single keyboard navigation action, when selection is added via Down/Up keys, then the selection is preserved and not immediately cleared by subsequent events.**
16. **Given completion popup is displayed with 3 items and user has not yet navigated, when user presses Down key for the first time, then selection moves to first item (index 0), and subsequent Down keys navigate normally through remaining items.**
17. **Given completion popup navigation guard prevents recursive events, when programmatic selection changes occur, then the guard prevents infinite loops while preserving legitimate navigation.**

### Edge Cases
- What happens when studyname mapping window is closed without selection? → [NEEDS CLARIFICATION: fallback behavior – skip, force retry, or mark as unmapped?]
- Previous report missing clear header delimiter → Fallback: treat entire text as findings and leave structured header fields empty; still proofread findings.
- Ghost suggestions appear but user begins typing mid‑suggestion → They must instantly disappear (no flicker insertion) and idle timer resets.
- PACS field length limit exceeded → [NEEDS CLARIFICATION: truncation vs user warning]
- Network/LLM service unavailable during postprocess → System must allow manual editing; mark fields requiring AI with placeholder and non‑blocking warning.
- OCR engine unavailable (Windows OCR not enabled) → Operation returns explicit "(ocr unavailable)" preview token without throwing.
- Patient number regex finds multiple sequences → Longest sequence selected.
- Completion popup navigation causes recursive events → Selection guard prevents infinite loops while allowing legitimate navigation.

---
## Requirements (mandatory)

### Functional Requirements (Reporting Pipeline)
- **FR-001** System MUST acquire current study patient & study metadata and render in title bar within <1s of selection (local extraction only; external calls excluded).
- **FR-002** System MUST detect absent studyname→LOINC mapping and prompt user before continuing automatic parsing.
- **FR-003** System MUST (when modality=MR and technique unmapped) open technique mapping capture prior to drafting header.
- **FR-004** System MUST run Study Remark Parser (LLM) using inputs (study remark + studyname + patient info) to populate chief_complaint and history_preview.
- **FR-005** System MUST run Patient Remark Parser (LLM) using inputs (patient remark + studyname + patient info + history_preview) to populate history.
- **FR-006** System MUST allow manual override for chief_complaint, history, technique, comparison at any time before send.
- **FR-007** System MUST support retrieving previous study text and populating PrevReport.header_and_findings and PrevReport.conclusion unchanged.
- **FR-008** System MUST split prev report header vs findings using header findings splitter (LLM) returning split_index.
- **FR-009** System MUST parse header portion to structured fields (chief_complaint, history, technique, comparison) in PrevReport.
- **FR-010** System MUST produce proofread variants for each parsed / entered header field via proofreader (LLM) preserving original.
- **FR-011** System MUST proofread previous findings and conclusion independently of current report fields.
- **FR-012** System MUST let user select comparisons sourced from previous studies to populate Report.comparison.
- **FR-013** System MUST capture user-entered findings text in Report.findings without loss of formatting.
- **FR-014** System MUST generate a conclusion_preview from findings via conclusion generator (LLM).
- **FR-015** System MUST proofread findings, conclusion_preview, and header fields into *_proofread counterparts.
- **FR-016** System MUST apply rule-based reportifier to header, findings_proofread, conclusion_proofread producing *_reportified variants.
- **FR-017** System MUST apply rule-based numberer to conclusion_reportified producing numbered output.
- **FR-018** System MUST validate banner metadata vs current study prior to PACS submission and block send on mismatch with explicit reason.
- **FR-019** System MUST submit (header_reportified + findings_reportified) and (conclusion_reportified_numbered) to appropriate PACS fields and confirm acceptance.
- **FR-020** System MUST persist final report (header_findings composite, conclusion, structured JSON components) on successful PACS acknowledgment.
- **FR-021** System MUST fetch selected patient/study metadata from Search Results list and expose as properties + `CurrentStudyLabel` string on New Study.
- **FR-022** System MUST (when AddStudy command invoked) gather selected related study metadata, ensure rad_study, verify banner matches patient/study date, pull findings/conclusion (choose longer variant), build partial report JSON with only `header_and_findings` and `conclusion` populated plus duplication in `findings`, insert med.rad_report row (is_created=false, is_mine=false, created_by=radiologist, report_datetime=report date), refresh PreviousStudies from DB.

### Functional Requirements (Editor Assistance)
- **FR-050** Editor MUST open completion popup only when current contiguous letter span length ≥ configured MinCharsForSuggest and provider returns ≥1 item.
- **FR-051** Editor MUST restrict completion replacement range to the word-of-interest (letters only) at popup creation.
- **FR-052** Editor MUST not auto-select non-exact items; selection appears only on exact match or cursor navigation.
- **FR-053** Editor MUST close completion popup when caret exits replacement range, word becomes empty, or idle threshold elapses.
- **FR-054** Editor MUST reserve Tab for ghost suggestion acceptance (not snippet/completion insertion).
- **FR-055** Editor MUST trigger ghost suggestions only after idle period with no active completion popup.
- **FR-056** Accepting ghost suggestion MUST insert its full text atomically and clear ghost state.
- **FR-057** Snippet insertion MUST expand placeholders and allow Tab/Shift+Tab navigation through ordered placeholders.
- **FR-058** Import-from-previous-study command MUST allow user to insert transformed previous findings into current cursor location (LLM transform) without overwriting unrelated text.

### Functional Requirements (Editor Completion Improvements & Bug Fixes)
- **FR-124** Completion cache MUST be invalidated immediately when phrases are added or modified via UpsertPhraseAsync or ToggleActiveAsync to ensure new phrases appear in completion list without application restart.
- **FR-125** Completion popup MUST respond to Down/Up key navigation by allowing selection changes when ListBox has keyboard focus, while maintaining exact-match-only selection for non-keyboard events.
- **FR-126** Phrase extraction window MUST use dependency injection to resolve PhraseExtractionViewModel with all required services properly injected to prevent runtime errors during phrase operations.
- **FR-127** Completion popup keyboard navigation MUST handle Up/Down keys reliably by explicitly managing selection state and preventing event propagation conflicts that could interfere with navigation.
- **FR-128** Completion popup selection guard MUST handle multiple selection events from single user actions by preventing recursive clearing and temporarily disabling event handlers during programmatic selection changes.
- **FR-129** Completion popup selection preservation MUST detect and allow new selections added via keyboard navigation by analyzing SelectionChanged event patterns and preventing immediate clearing of legitimate user selections.
- **FR-130** Completion popup first navigation detection MUST track user navigation state using a dedicated flag rather than selection index to ensure first Down key always selects first item (index 0) regardless of existing selections from exact matching.
- **FR-131** Completion popup MUST recompute first-navigation state when suggestions refresh or the editor owns focus so that the first Down/Up key after typing selects the first or last item instead of skipping ahead even if the list retained a prior selection, and the editor MUST directly handle subsequent Up/Down keys so the very next press moves to the adjacent item on the first try.
- **FR-132** Completion popup selection guard MUST prevent recursive event handling by using a guard flag to stop infinite loops during programmatic changes while preserving legitimate keyboard navigation.
- **FR-133** Completion popup MUST: (a) never skip intermediate items during sequential Up/Down navigation (one keypress → move exactly one item), and (b) cap visible item height to at most 8 items (dynamic ListBox MaxHeight) while allowing scroll for overflow.
- **FR-134** Completion popup MUST auto-size its height exactly to (items_count * measured_item_height + padding) when item_count ≤ 8, and clamp to 8-items height when item_count > 8, updating after list rebuilds and after selection-induced layout changes.

### Functional Requirements (Mapping & Procedures)
- **FR-090** Studyname→LOINC mapping UI MUST allow multi-part selection, ordering (sequence letters), and duplicate part numbers with distinct sequence order (pair uniqueness rule).
- **FR-091** System MUST surface playbook suggestions once ≥2 distinct selected parts exist.
- **FR-092** Double-clicking a playbook MUST bulk import its parts, skipping only existing (PartNumber+SequenceOrder) pairs.
- **FR-093** User MUST be able to add or remove individual parts including sequence order edits prior to save.
- **FR-094** Mapping save MUST persist all selected parts and sequence orders and update in-memory cache for immediate future study resolution.
- **FR-095** Procedure operation GetValueFromSelection MUST return target column value using normalization rules and alignment preservation.
- **FR-096** Procedure operation ToDateTime MUST parse supported date/time formats or report parse fail without throwing.
- **FR-097** PACS selection helper methods MUST retrieve structured field values (name, sex, birth date, etc.) from either Search Results or Related Study list selections using unified header logic.
- **FR-098** Procedure operation GetTextOCR MUST perform OCR on target element bounds and return extracted text or explicit status tokens ("(no element)", "(ocr unavailable)", "(empty)" etc.).
- **FR-099** PACS helper MUST expose current patient number via banner heuristic (longest digit sequence length ≥5) and current study date/time via date(+time) pattern.
- **FR-135** Custom procedure Split operation MUST support optional third argument (Arg3, Number) representing zero-based index; when provided and within bounds the operation stores only that part and preview shows part[index]; when out of range preview = (index out of range N); when omitted legacy behavior (all parts joined with \u001F) is preserved.

### Functional Requirements (Reliability & Logging)
- **FR-120** First-chance Postgres exception sampler MUST log each unique (SqlState|Message) only once per application session.
- **FR-121** PhraseService MUST use configurable LocalConnectionString (settings) with fallback; no hardcoded credentials.
- **FR-122** System MUST allow continuation of reporting if any single LLM call fails, marking only affected fields.
- **FR-123** OCR / banner extraction failures MUST NOT crash procedure execution; they return null output with diagnostic preview.

### Data & Key Entities
- **Report**: technique, chief_complaint, history_preview, chief_complaint_proofread, history, history_proofread, header_and_findings, conclusion, split_index, comparison, technique_proofread, comparison_proofread, findings_proofread, conclusion_proofread, findings, conclusion_preview.
- **PrevReport**: header_and_findings, split_index, chief_complaint, history, technique, comparison, *_proofread, findings_proofread, conclusion, conclusion_proofread.
- **Study**: identifiers, modality, studyname, study remark, patient remark, timestamps.
- **Mapping (StudynameLOINCParts)**: studyname, part list (part_number, part_name, sequence_order), derived technique link [NEEDS CLARIFICATION: technique derivation rule].
- **GhostSuggestion**: list of alternative multi-line suggestions + selected index.
- **Snippet**: template text with placeholder definitions & navigation order.
- **ProcedureOperation**: name, Arg1/Arg2 types, output variable, preview.

---
## Review & Acceptance Checklist
### Content Quality
- [ ] No implementation details in core sections (HOW isolated to Appendix)
- [x] Focused on user value & outcomes
- [x] Mandatory sections completed

### Requirement Completeness
- [ ] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements testable & numbered
- [x] Scope boundaries: Reporting workflow (current + previous), mapping, editor assistance, PACS submission, OCR extraction, completion improvements, bug fixes
- [ ] Assumptions validated (pending clarifications)

---
## Execution Status
- [x] User description parsed
- [x] Key concepts extracted
- [x] Ambiguities marked
- [x] User scenarios defined
- [x] Requirements generated
- [x] Entities identified
- [ ] Review checklist fully passed (clarifications outstanding)

---
## Ambiguities / Clarification Requests
1. Studyname mapping dismissal behavior (force, skip, mark?).
2. PACS field length overrun handling (truncate vs block?).
3. Technique derivation rule from LOINC parts (deterministic mapping or user input precedence?).
4. Acceptable latency targets for postprocess (LLM) before offering manual fallback.
5. Ghost suggestion insertion span (line remainder vs context-aware region) final policy.
6. OCR banner element reliability vs whole-window heuristic fallback thresholds.

---
## Non-Functional (Derived)
- **Performance**: Header metadata render <1s (local). Ghost suggestion request dispatch ≤100ms after idle event. LOINC mapping UI list filtering <150ms on typical dataset. OCR element operation should return in <1.2s typical (depends on Windows OCR engine). Completion cache refresh <50ms after phrase add/modify. Keyboard navigation response <50ms. Selection guard event handling <10ms. Multiple event handling <5ms. Navigation state tracking <1ms. Recursive guard handling <1ms.
- **Reliability**: Any single external (LLM) failure must not block manual editing or PACS send if mandatory fields filled. Dependency injection must resolve all required services to prevent runtime failures. Selection guard must prevent infinite recursion. Multiple SelectionChanged events must be handled gracefully. Navigation state must be tracked accurately. Recursive event handling must be prevented.
- **Observability**: Structured logging for each pipeline stage (event type, duration, success/failure) [NEEDS CLARIFICATION: log schema].
- **Local Persistence**: Final report write must be atomic (transaction or temp+rename) [detail for plan].

---
## Risks
- Over-reliance on LLM latency impacting radiologist throughput.
- Ambiguous previous report headers producing incorrect structured fields (mitigate with confidence score + fallback).
- Mapping quality affecting downstream technique auto-fill accuracy.
- OCR variability across workstation font/render settings impacting patient number extraction accuracy.
- Completion popup navigation recursion causing UI freezes or instability.

---
## Appendix A: Implemented Feature Record (Traceability – Historical Notes)
(Contains prior detailed implementation notes migrated from legacy Spec.md; considered OUT OF SCOPE for core specification validation but retained for audit.)

---
# Legacy / Implementation Detail Chronicle

## 1) Bug Fixes (2025-01-01 - Latest)
- **Navigation State Tracking**: Added _hasUserNavigated flag to EditorControl to properly track whether user has used keyboard navigation, ensuring first Down key always selects first item (index 0) regardless of existing selections from exact matching. Reset flag when creating new completion window and when closing.
- **Recursive Guard Protection**: Enhanced MusmCompletionWindow selection guard to prevent recursive event handling by adding _handlingSelectionChange flag and proper event flow control during programmatic selection changes.

## 2) Bug Fixes (2025-01-01 - Previous)
- **Multiple Event Handling**: Enhanced MusmCompletionWindow selection guard to detect and preserve selections that are added via keyboard navigation by analyzing SelectionChanged event patterns (added vs removed items). Added logic to prevent immediate clearing of new selections that don't have corresponding removals.
- **Keyboard Focus Management**: Improved EditorControl keyboard navigation to ensure ListBox gets proper focus during navigation, enhancing the keyboard focus detection in the selection guard.

## 3) Bug Fixes (2025-01-01 - Earlier)
- **Selection Guard Recursion Prevention**: Enhanced MusmCompletionWindow selection guard to prevent recursive clearing by temporarily disabling SelectionChanged event handler during programmatic selection changes. Added logic to detect and allow programmatic clearing operations.
- **Navigation Event Handling**: Improved EditorControl keyboard navigation logic with enhanced debugging and better flow control to ensure AllowSelectionByKeyboardOnce is called at the appropriate time before selection changes.

## 4) Bug Fixes (2025-01-01 - Earlier)
- **Phrase Extraction Service Injection**: Fixed PhraseExtractionWindow creation to use dependency injection (GetService<PhraseExtractionViewModel>()) instead of manual instantiation, preventing service injection errors during phrase save operations.
- **Completion Navigation Enhancement**: Improved Up/Down key handling in completion popup by explicitly managing selection state transitions and ensuring proper event handling to prevent navigation conflicts.

## 5) Editor Completion Improvements (2025-01-01)
- **Immediate Cache Refresh**: Added IPhraseCache.Clear() method and cache invalidation in PhraseService.UpsertPhraseAsync() and ToggleActiveAsync() to ensure completion list updates immediately when phrases are added/modified.
- **Fixed Down/Up Navigation**: Modified MusmCompletionWindow selection guard to allow keyboard navigation while maintaining exact-match-only behavior for non-keyboard selection events.

## 6) Spy UI Tree focus behavior
- Show a single chain down to level 4 (root → ... → level 4).
- Expand the children of the level-4 element; depth bounded by `FocusSubtreeMaxDepth`.
- Depths configurable via constants in `SpyWindow`.

## 7) Procedures grid argument editors
- Operation presets set Arg1/Arg2 types and enablement, with immediate editor switch.
- Element/Var editors use dark ComboBoxes.

## 8) PACS Methods (Custom Procedures): "Get selected ID from search results list"
- New method Tag="GetSelectedIdFromSearchResults".
- Implements PacsService.GetSelectedIdFromSearchResultsAsync.

## 9) Studyname → LOINC Parts window
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
- Double-click on category/Common item adds it to Mapping Preview with Sequence Order input (default "A"); duplicates allowed
- Preview panel: shows each item with an editable PartSequenceOrder TextBox; double-click removes the item
- Sequence Order input placed at the right side of the Preview header
- Common list shows most-used parts (from repo aggregation), spanning two cells

Playbook Suggestions (under Preview)
- With ≥ 2 distinct selected parts, suggest playbooks grouped by loinc_number (each LOINC spans multiple part rows)
  - LEFT: long_common_name (distinct by loinc_number) ? vertical scrollbar present; layout changed from StackPanel to Grid so ListBox height is constrained ensuring scrollbar visibility (PP1 fix).
  - RIGHT: the playbook parts list for the selected loinc_number (part_name + part_sequence_order)
- Double-click behaviors:
  - Double-click a playbook (left list) imports all its parts with their sequence orders into Preview, skipping only duplicate (PartNumber + SequenceOrder) pairs. Uses async-safe wait loop to ensure parts have loaded before import (PP2 fix).
  - Double-click a single playbook part (right list) adds that part if the combination PartNumber + PartSequenceOrder is not already present (PP3 rule refinement).

Duplicate Rule (PP3)
- Previous logic blocked duplicates by PartNumber only. Updated rule: allow same PartNumber multiple times if SequenceOrder differs. A duplicate is only when both PartNumber and PartSequenceOrder match an existing preview item.

Acceptance Criteria
- Single main window shown on run (no duplicate windows)
- Studynames textbox filters in real-time
- long_common_name suggestions appear under Preview when ≥ 2 parts match one or more loinc_number groups
- Double-click on playbook reliably imports after async load without null/iteration errors
- Duplicate rule honors (PartNumber + SequenceOrder) uniqueness
- Build succeeds

Notes / Future Enhancements
- Remove/reorder controls for preview
- Virtualization and count badges
- Import button (double-click now handles bulk import)

## 10) Custom Procedure Operation: GetValueFromSelection
Purpose
- Allow a procedure step to extract the value of a specific column header from the currently selected row of a list (e.g., mapped SearchResultsList) reusing the Row Data parsing logic.

Definition
- Operation name: `GetValueFromSelection`.
- Arg1: Element (required) → list control (e.g., SearchResultsList mapping).
- Arg2: String (required) → header text to extract. Example: `ID`, `Accession No.`, `Status`.
- Output: value of the first column whose normalized header equals Arg2 (case-insensitive). If not exact match, the first header containing Arg2 substring is used. If still not found → null.

Normalization Rules (reuse existing):
- Accession → Accession No.
- Study Description → Study Desc
- Institution Name → Institution
- BirthDate → Birth Date
- BodyPart → Body Part

Behavior
- If no selection exists → preview "(no selection)" .
- If header not found → preview `(Arg2 not found)`.
- On success preview = extracted value; stored in `var#` for chaining.
- Default Arg2 value set to "ID" when operation selected with empty Arg2.

Implementation Notes
- Added to Operation ComboBox list in Custom Procedures grid.
- Preset rules in `OnProcOpChanged` assign Arg1=Element, Arg2=String, enable Arg2 and seed default.
- Execution implemented in both single-row Set (ExecuteSingle) and full Run (RunProcedure) flows.
- Reuses `GetHeaderTexts` + `GetRowCellValues` + `NormalizeHeader` helpers already present in SpyWindow code-behind.

## 11) Row Data & Selection Value Alignment Preservation
Change (Earlier Iteration)
- `GetRowCellValues` now always adds placeholders for blank cells (no skipping) to keep header/value alignment.

## 12) Blank Header Column Preservation (Current Iteration)
Problem
- Some lists contain a leading blank header cell (first column intentionally unlabeled). Previous logic skipped blank headers which then shifted all subsequent header/value associations (e.g. Status value appearing under ID header).

Solution
- `GetHeaderTexts` now adds an empty string entry for every header cell (even if blank after probing descendants). This preserves positional alignment with the row cell values list.
- Row Data formatter logic updated:
  - Build pairs (Header, Value) with normalized headers.
  - When both header and value are blank → pair omitted entirely.
  - When header blank but value present → output just `Value` (no colon label) preserving its sequence.
  - When header present → `Header: Value` as before.

Result
- Leading blank header no longer causes value shifts.
- Example corrected: either leading blank pair omitted (if value also blank) giving: `Status: Examined | ID: 239499 | Name: … | ... | Institution: BUSAN ST.MARY'S HOSPITAL` OR if value present for first blank header: `<value> | Status: Examined | ID: ...` maintaining order.

Testing Scenarios
1. Leading blank header with blank value → dropped; no shift.
2. Leading blank header with nonblank value → value emitted alone at start.
3. Internal blank header between populated headers → value-only token preserved in position.
4. `GetValueFromSelection` unaffected; indexing uses parallel arrays including blank header placeholders.

## 13) Operation: ToDateTime
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

## 14) New PACS Selection Methods
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

## 15) Postgres First-Chance Exception Sampling
- `PgDebug.Initialize()` registers a first-chance handler capturing `PostgresException` once per (SqlState|MessageText) pair and logs via Serilog at Warning level.
- Added initialization in `App.OnStartup` before splash login.
- Repository methods now log explicit PostgresException details with SqlState and server message for triage.

## 16) PhraseService Connection Refactor
Changes
- PhraseService now consumes `IRadiumLocalSettings.LocalConnectionString` with legacy fallback.
- Added debug logging of host/db/user when opening connections.
- Sets minimal timeouts and includes error detail.

Future
- Planned migration to use `CentralConnectionString` (central phrases) once consolidation is ready; code comments note this.

## 17) Editor Specification (Summary Reference)
For full behavioral definitions see MUSM Editor Specification included in design docs (source: spec_editor.md). Key points mapped to FR-050..FR-058.

## Update Log (2025-09-28)
- Replaced terminology: tenant_id → account_id in all phrase related requirements.
- Phrase completion (FR-050..FR-053) now explicitly sourced from snapshot in PhraseService (AccountPhraseState) instead of direct DB each keystroke.

## Update Log (2025-09-29)
- Added GetTextOCR procedure operation (FR-098).
- Added PACS banner helpers: GetCurrentPatientNumber, GetCurrentStudyDateTime (FR-099, FR-123 reliability clause).
- Application shutdown behavior updated: app exits when main window closed (usability improvement, not numbered FR; aligns with standard desktop UX).

## Update Log (2025-01-01)
- Added completion cache invalidation (FR-124) in PhraseService to clear cache when phrases are added/modified.
- Fixed completion popup keyboard navigation (FR-125) to allow Down/Up key selection changes.
- Fixed phrase extraction window service injection (FR-126) to prevent runtime errors.
- Enhanced completion popup navigation handling (FR-127) for reliable keyboard interaction.
- Prevented selection guard recursion (FR-128) to maintain proper navigation flow.
- Improved multiple event handling (FR-129) to preserve keyboard-driven selections.
- Fixed first navigation detection (FR-130) to ensure consistent Down key behavior.
- Enhanced recursive guard protection (FR-132) to prevent infinite loops during programmatic changes.
- Added precise single-step navigation and bounded height for completion popup (FR-133).
- Implemented adaptive popup height adjustment with clamp to 8-items (FR-134).
- Updated Split operation index argument handling (FR-135) to support optional third argument for part index selection.
- Added Current Study Label population from PACS on New Study (FR-136).
- Updated PACS metadata retrieval methods to execute user-defined procedure steps (FR-137).
- Added StudyDateTime formatting and previous studies retrieval placeholder (FR-138, FR-139).
- Implemented UIA caching for resolved Automation elements (FR-140).
- Added persistence placeholder for patient/study metadata (FR-141).
- Upsert implementation for patient/study and spy pick optimization (FR-142, FR-143).

---
## Feature Update (Reportified Toggle - Inverse Dereportify)
- Dereportify now performs inverse transformation: removes numeric prefixes, strips trailing single periods, collapses leading numbering indentation, and decapitalizes first token unless token appears capitalized in phrase dictionary snapshot.
- Arrow prefix spaces removed back to canonical compact form (e.g., `-->Finding` instead of `--> Finding`).

---
## Instrumentation (Reverse Selection Debug)
- Added detailed Debug.WriteLine logs for selection changes, mouse drag movement, and word highlight decisions to trace reverse (right-to-left) selection issue.

---
