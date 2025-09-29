# Implementation Plan: Radium Cumulative (Reporting Workflow + Editor + Mapping + PACS)

## Change Log Addition
- Paragraph-based conclusion numbering implemented (multi-paragraph only).
- Dereportify now normalizes '-->' to '--> ' with single space.
- Added GetTextOCR procedure op + PACS banner helpers (current patient number, study date time).

(Update: account_id migration + phrase snapshot + OCR additions)

**Branch**: `[radium-cumulative]` | **Date**: 2025-09-28 | **Spec**: Spec.md (same folder)
**Input**: Cumulative feature specification (Spec.md)

---
## Summary
Primary goal: Unify automated radiology reporting pipeline (current + previous study parsing, LOINC mapping, technique acquisition), enhanced editor assistance (completion, snippets, ghost suggestions), reliable PACS submission with structured persistence, plus OCR-based banner extraction utilities (GetTextOCR op and patient/study banner helpers).

---
## Technical Context
**Language/Version**: .NET 9 (WPF desktop)  
**Primary Dependencies**: WPF, AvalonEdit, Npgsql, Serilog, (LLM provider abstraction) [NEEDS CLARIFICATION: actual provider], internal RuleEngine, Windows OCR API (indirect via OcrReader)  
**Storage**: Postgres (phrase & mapping), local persistence (file or DB) [NEEDS CLARIFICATION: local DB type], in-memory caches  
**Testing**: xUnit (present) + golden text fixtures + OCR op unit test with engine-availability mock (future)  
**Target Platform**: Windows 10+ desktop  
**Performance Goals**: <1s metadata acquisition; <300ms completion popup open; ghost dispatch <100ms post idle; postprocess pipeline <5s; GetTextOCR typical <1.2s; banner heuristics <150ms.  
**Constraints**: Graceful degradation if OCR engine disabled; no UI thread blocking for network / OCR heavy operations.

---
## Constitution Check
No architecture deviations: OCR op implemented in procedure executor. Banner helpers encapsulated inside PacsService. Low coupling maintained.

---
## Project Structure (Affected)
- apps/Wysg.Musm.Radium/Views/SpyWindow.* (procedure op + menu entries)
- apps/Wysg.Musm.Radium/Services/PacsService.cs (banner parsing)
- docs (Spec/Plan/Tasks updated cumulatively)

---
## Phase 0: Outline & Research
Pending clarifications extended to OCR (engine availability, fallback heuristics) and reliability markers.

---
## Phase 1: Design & Contracts
(UNCHANGED baseline) – Add note: ProcedureOp set extended with GetTextOCR (Arg1=Element, Arg2 disabled). PacsService contract implicitly extended (non-interface) with `GetCurrentPatientNumberAsync` & `GetCurrentStudyDateTimeAsync`.

---
## Phase 2: Task Planning Extension
Added incremental tasks (see Tasks.md T205..T208) covering implementation & spec alignment for FR-098..FR-099, FR-123.

---
## Phase 3+: Future
Add tests later for OCR op fallbacks (engine missing) & banner regex accuracy (digit vs mixed token collision).

---
## Complexity Tracking
No new complexity exceptions.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|--------------------------------------|
| (none) | | |

---
## Progress Tracking
GetTextOCR + banner helpers implemented (status: Done). Documentation updated.

---
## Recent Adjustments (New)
- Added GetTextOCR operation (SpyWindow) with OCR region = element bounds (naive bounding; future improvement: clamp inside visible window rect).
- Added PacsService methods extracting patient number (longest 5+ digit sequence) and study date time (YYYY-MM-DD + optional HH:mm[:ss]).
- Updated App shutdown mode to OnMainWindowClose after successful login for expected desktop behavior.

---
(Existing historical adjustments retained below)

## Recent Adjustments
- Central phrase service now loads per-account snapshot (AccountPhraseState) and serves editor completions from in-memory data.
- All future references use account_id (legacy tenant_id alias retained at boundaries).
- Added non-blocking retry prefetch for phrase snapshot.
- PhraseCache supports Clear.

## Recent Adjustment (Editor Idle Behavior)
- Removed legacy idle auto-close of completion popup.

## Recent Adjustment (Completion Navigation)
- Initial Up/Down selects boundary item immediately.

## Recent UI Adjustment
- Dark theme styling extended.

## Added Implementation (Reportified Toggle)
- Toggle handlers and reversible dereportify algorithm (see Spec) simplified by inversion logic.

... (remaining historical notes unchanged) ...
