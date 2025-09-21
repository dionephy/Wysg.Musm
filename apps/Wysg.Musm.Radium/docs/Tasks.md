# Radium Task Checklist

## UI/VM
- [x] PatientLocked binding on Study locked toggle.
- [x] PreviousStudies tabs (toggle buttons) with selection.
- [x] Add Study (+) button only in right dock.
- [x] Two-row headers with fixed height and no scrollbars:
  - [x] Left: action buttons in header (32px) + lower label (22px) -> total 54px.
  - [x] Right: tab strip (32px) + details bar (22px) -> total 54px.
- [x] Status bar buttons trimmed (avoid duplication with header).
- [x] PreviousStudiesStrip: tabs and + in same row; + pinned right; show "¡å" overflow dropdown when width exceeded; tabs sorted by StudyDateTime desc.
- [x] Tabs single-select behavior enforced; overflow item shows check when selected.
- [x] Bind previous editors to SelectedPreviousStudy.Header/Findings/Conclusion.
- [x] AddStudy test logic: add several dummy studies per click to exercise overflow; include JSON payload for each tab.

## PACS Wiring
- [ ] Implement New Study flow using PacsService.
- [ ] Implement Add Study flow using PacsService.
- [ ] Implement Send Report Preview and Send Report paths.

## Database
- [x] Repository for studyname ¡ê loinc.part with sequence/order (Npgsql).
- [ ] Configure remaining Postgres access for rad_* tables.
- [ ] Upsert patient/studyname and insert rad_study.
- [ ] Insert rad_report with JSON.

## NLP Pipeline
- [ ] Splitter/Decapitalizer/Refiner/Aligner/Capitalizer stubs.
- [ ] Manual pipeline for initial versions.

## Studyname ¡ê LOINC Parts Mapping
- [x] Window (XAML + VM) scaffold; grouped parts with selection; mapping preview.
- [x] Repository (Npgsql) to load loinc.part and persist mappings (with part_sequence_order).
- [ ] Command entry points from New Study / Add Study (with preselect).

## Theming
- [x] Dark title bar (DWM) on supported Windows.
- [x] Dark UI.

## QA
- [ ] Unit tests for VM logic and JSON serialization.
- [ ] Smoke test PACS flows (dry-run mode).
