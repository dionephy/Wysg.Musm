# Radium Implementation Plan

## Recent ? SpyWindow enhancements (this iteration)
- [x] Ancestry depth > 3: materialize full children under depth-3 and below (no single target node per depth).
- [x] Custom Procedures bound to PacsService methods (no free-form names).
- [x] Custom Procedures grid UX:
  - [x] Add button to append a new operation row.
  - [x] Per-row Set (execute row, create var#) and Remove (x) actions.
  - [x] Output var1/var2/¡¦ displayed and usable in later rows.
  - [x] Argument editors switch by type (Element/String/Number/Var) with presets on operation selection.
  - [x] For GetText/Invoke, Arg2 disabled; for Split, Arg1=Var, Arg2=String, etc.
- [x] PacsService execution to call stored procedures by tag (next).

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

## This iteration
- [x] SpyWindow: Ancestry depth > 3 shows full UIA children under depth-3 element.
- [x] SpyWindow: Custom Procedures Operation combobox binds and updates Op; presets apply.
- [x] SpyWindow: Add row button; per-row Set to compute OutputVar/Preview; per-row Remove.
- [x] Update procedure vars list dynamically for Var dropdowns.
- [x] PP1: UI tree shows reliably (FlaUInspect-like) after resolve; no more single-node-per-depth.
- [x] PP2: Operation ComboBox no longer causes row disappearance or freezes; bindings hardened.

## Recent requests
- [x] PP1: Focused UI tree (single nodes for levels 1..4; expand all under level 5). Provide desktop fallback if unresolved.
- [x] PP2: Grid operation dropdowns open reliably; no ItemsSource resets; visuals refreshed only.

## Next
- [ ] Optional: user-configurable focus depth and max subtree depth.
- [ ] Add expand-on-demand for very large trees.
- [ ] More operations (RegexSplit, Replace, Substring, NormalizeWhitespace).

## Previously completed
- [x] UI shell, dark title bar.
- [x] Mapping editor improvements.
- [x] Postgres scaffold for studyname ¡ê loinc.part mapping (infra).
- [x] Dark theme/title bar and chain editor improvements.
- [x] Custom Procedures: Add/Set/Remove row, OutputVar/preview, dynamic arg editors, persistence.
- [x] Dark theme, title bar.
- [x] Chain editor and mapping UX.
