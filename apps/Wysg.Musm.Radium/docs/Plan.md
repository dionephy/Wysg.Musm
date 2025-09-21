# Radium Implementation Plan

## Phase 1 ? UI Shell and Service Naming (Done)
- [x] Rename MfcPacsService -> PacsService (keep MFC + FlaUI + OCR helpers).
- [x] DI registration updated; build verified.
- [x] Dark title bar using DwmSetWindowAttribute.
- [x] PatientLocked binding; PreviousStudies tabs and selection.
- [x] Commands: New Study, Add Study, Send Report Preview, Send Report, Manage studyname.
- [x] PreviousStudiesStrip control: tabs + Add on same row, overflow dropdown ("¡å") when width exceeded; + pinned right. Tabs sorted by StudyDateTime desc.
- [x] Tabs behave single-select; overflow item shows check when selected.
- [x] AddStudy test logic adds multiple dummy studies with varied datetimes and JSON payload.
- [x] Two-row headers with fixed height and no scrollbars:
  - [x] Left: Upper row (32px) with Study locked + New/Preview/Send; lower row (22px) placeholder label.
  - [x] Right: Row 0 (32px) tab strip; Row 1 (22px) details bar for SelectedPreviousStudy.
- [x] Status bar trimmed (moved core actions to top header).

## Phase 2 ? PACS Automation Wiring
- [ ] New Study button:
  - [ ] Wire to PacsService.OpenWorklistAsync(), list selection, viewer open.
  - [ ] Populate window title and current header (techniques, comparison).
  - [ ] Close worklist and (optional) alignment.
- [ ] Add Study (+) button:
  - [ ] Read related study row; validate same patient.
  - [ ] Snapshot var1 (raw JSON) and create refined JSON placeholder.
  - [ ] Push PreviousStudyTab; bind editors to selection.
- [ ] Send Report Preview:
  - [ ] Snapshot var2, run simple refine/align.
- [ ] Send Report:
  - [ ] Snapshot var3, capitalize, send via PacsService, confirm/approve, get datetime.

## Phase 3 ? Database Integration
- [ ] Postgres connection and repositories for med.* objects.
- [ ] Upsert patient by patient_number.
- [ ] Upsert studyname with case-sensitive uniqueness.
- [ ] Insert rad_study with composite unique (on conflict do nothing; return id).
- [ ] Insert rad_report with proper JSONB payloads and flags.
- [x] Create med.studyname_loinc_part mapping table SQL (id, studyname_id, part_number, part_sequence_order, created_at).

## Phase 4 ? NLP Pipeline Abstractions
- [ ] ISplitter, IDecapitalizer, IRefiner, IAligner, ICapitalizer interfaces.
- [ ] Manual implementations; later LLM-backed implementations.
- [ ] Plug pipeline into Add Study, Preview, Send paths.

## Phase 5 ? UX/Polish
- [x] Show selected PreviousStudyTab content in right editors.
- [ ] Tab close buttons (optional) and keyboard navigation.
- [x] Add Manage studyname button.

## Phase 6 ? Studyname ¡ê LOINC Parts Mapping
- [x] Window (XAML + VM) scaffold; grouped parts with selection; mapping preview.
- [ ] Simple repository to load loinc.part and persist mappings (with part_sequence_order).
- [ ] Command entry points from New Study / Add Study (with preselect).
