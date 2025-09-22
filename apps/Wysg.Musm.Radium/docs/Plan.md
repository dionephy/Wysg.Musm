# Radium Implementation Plan

## Recent ? SpyWindow fixes (this iteration)
- [x] Fix: Procedures grid comboboxes unresponsive after selecting an operation (removed focus-driven open; commit/refresh only; no ItemsSource resets).
- [x] Fix: UI Tree focus rule updated: single node per depth for levels 1?4; subtree expansion from level 5 onward (configurable constant in code).

## Phase 1 ? UI Shell and Service Naming (Done)
- [x] Rename MfcPacsService -> PacsService, DI registration, dark title bar, top header actions.

## Phase 2 ? PACS Automation Wiring
- [ ] New Study: wire to PacsService.OpenWorklistAsync() etc.
- [ ] Add Study: related row capture.
- [ ] Send Report Preview / Send Report.

## Phase 3 ? Database Integration
- [x] Postgres repo scaffold for studyname ¡ê loinc.part mapping.
- [ ] rad_* tables and persistence wiring.

## Phase 4 ? NLP Pipeline Abstractions
- [ ] Interfaces + manual implementations.

## Phase 5 ? UX/Polish
- [x] PreviousStudies editing and selection.

## This iteration ? PP fixes
- [x] PP1: Add traversal diagnostics, cap traversal, lighten fallback while keeping focused view.
- [x] PP2: Stabilize Operation ComboBox in DataGrid (StaysOpenOnEdit, preview open, reentrancy guard, reopen after change).

## Next
- [ ] Option to set focus chainDepth, subtree depth, traversal cap.
- [ ] Lazy load ancestry nodes on expand.
