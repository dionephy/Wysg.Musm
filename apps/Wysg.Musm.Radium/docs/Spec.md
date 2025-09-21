# Radium: PACS Reporting Spec

## Overview
- WPF desktop app (.NET 9) with dark theme and dark title bar.
- Automates INFINITT PACS using PacsService (MFC + FlaUI; OCR available).
- Supports composing reports: header, findings, conclusion.
- Maintains previous studies as tab-like toggles.

## UI Layout
- Left pane: Current report editors (Header, Findings, Conclusion).
  - Header area has two rows with fixed total height (54px):
    - Upper row (32px): action controls ? `Study locked` toggle, `New Study`, `Send Report Preview`, `Send Report` buttons. No scrollbars.
    - Lower row (22px): placeholder label `Current Study` (reserved for patient/study summary in future).
- Right pane:
  - First row (32px): PreviousStudies tab strip and Add Study (+) button in the same row; + button pinned to the right. No scrollbars.
  - Second row (22px): Read-only info bar showing basic details of `SelectedPreviousStudy` (Title, StudyDateTime). No scrollbars.
  - Overflow: when tabs exceed available width, show a "¡å" overflow button; exceeding tabs appear as dropdown items.
  - Tabs are ordered by StudyDateTime desc (newest on the left); overflow dropdown items keep this order.
  - Tabs behave like a TabControl: only one can be selected at a time; selection drives right editors. Visible toggles reflect the selected item, overflow menu item shows a check when selected is hidden.
  - Editors show SelectedPreviousStudy.Header/Findings/Conclusion and update two-way.
- Status bar (bottom): Diagnostic and utility controls; core actions were moved to the Current Report header.

## Functional Requirements

### 1) New Study
- Trigger: header button "New Study".
- Behavior:
  - [ ] Fetch patient/study info from PACS via PacsService.
  - [x] Set PatientLocked = true.
  - [ ] Update window title with patient/study info.
  - [ ] Insert patient, studyname (case-sensitive unique), imaging study rows if missing; respect unique constraints.
  - [ ] Prefill header (MRI techniques and comparison if available).
  - [ ] Open current study in PACS viewer; close worklist; optional image alignment.
  - [ ] Clear current findings and conclusion.

### 2) Add Study
- Trigger: right pane + button; requires PatientLocked.
- Behavior:
  - [ ] Fetch currently selected related study from PACS.
  - [ ] Ensure patient matches current patient; otherwise ignore.
  - [ ] Temporarily save raw report pieces (var1: header+findings, conclusion).
  - [ ] Split header/findings (manual initially; later LLM).
  - [ ] Decapitalize previous header/findings/conclusion (manual initially; later phrases-based).
  - [ ] Refine and align (manual initially; later LLM).
  - [x] Create a PreviousStudies tab; tabs are single-select radio-like; JSON payload {header, findings, conclusion} saved per tab for testing.
  - [ ] Insert med.rad_report with is_created=false, report=var1 (JSON), refined_report (JSON).
  - [x] For testing: clicking + adds multiple dummy previous studies with varied StudyDateTime.

### 3) Send Report Preview
- Trigger: header button "Send Report Preview"; requires PatientLocked.
- Behavior:
  - [ ] Open worklist.
  - [ ] Save current header/findings/conclusion to var2.
  - [ ] Refine and align (manual initially; later models).

### 4) Send Report
- Trigger: header button "Send Report"; requires PatientLocked and non-empty var2.
- Behavior:
  - [ ] Verify current PACS study matches locked study via PacsService + OCR.
  - [ ] Save current header/findings/conclusion to var3.
  - [ ] Capitalize var3.
  - [ ] Send report to PACS and confirm/approve.
  - [ ] Retrieve report datetime from PACS.
  - [ ] Insert med.rad_report with is_created=true, is_mine=true, report=var2, refined_report=var3, split_index=len(var3.header).

### 5) Studyname ¡ê LOINC Parts Mapping
- Window: dark-themed, opens standalone or with preselected studyname.
- Data:
  - loinc.* schema exists (provided).
  - New table: med.studyname_loinc_part (id, studyname_id, part_number, part_sequence_order, created_at). part_number references loinc.part.part_number.
- Behavior:
  - List of studyname entries (allow add new, select one).
  - Tree of LOINC parts grouped by rad.[subcategory]; items show part_type_name and part_name.
  - Check selections to map parts; preview selection; Save persists to med.studyname_loinc_part.
