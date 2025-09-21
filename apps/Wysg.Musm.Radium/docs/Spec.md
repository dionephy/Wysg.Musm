# Radium: PACS Reporting Spec

## Overview
- WPF desktop app (.NET 9) with dark theme and dark title bar.
- Automates INFINITT PACS using PacsService (MFC + FlaUI; OCR available).
- Supports composing reports: header, findings, conclusion.
- Maintains previous studies as tab-like toggles.

## UI Layout
- Left pane: Current report editors (Header, Findings, Conclusion).
- Right pane:
  - Top row: PreviousStudies tab strip and Add Study (+) button in the same row; + button pinned to the right.
  - Overflow: when tabs exceed available width (DockPanel width - 2x Add width), show a "¡å" overflow button to the immediate left of +; exceeding tabs appear as dropdown items.
  - Tabs are ordered by StudyDateTime desc (newest on the left); overflow dropdown items keep this order.
  - Tabs behave like a TabControl: only one can be selected at a time; selection drives right editors. Visible toggles reflect the selected item, overflow menu item shows a check when selected is hidden.
  - Editors show SelectedPreviousStudy.Header/Findings/Conclusion and update two-way.
- Status bar (bottom): New Study, Send Report Preview, Send Report, Manage studyname buttons, plus tooling.

## Functional Requirements

### 5) Studyname ¡ê LOINC Parts Mapping
- Window: dark-themed, opens standalone or with preselected studyname.
- Data:
  - loinc.* schema exists.
  - Mapping table supported: med.rad_studyname_loinc_part (preferred) or legacy med.rad_studyname_loinc.
- Behavior:
  - Loads studyname list from med.rad_studyname.
  - Loads LOINC parts from loinc.part and current mappings from mapping table; shows grouped list and mapping preview.
  - Check selections to map parts; PartSequenceOrder editable (A/B/C/D; default A); Save persists to mapping table.
