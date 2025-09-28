# Implementation Plan: Radium Cumulative (Reporting Workflow + Editor + Mapping + PACS)

## Change Log Addition
- Paragraph-based conclusion numbering implemented (multi-paragraph only).
- Dereportify now normalizes '-->' to '--> ' with single space.

(Update: account_id migration + phrase snapshot)

**Branch**: `[radium-cumulative]` | **Date**: 2025-09-28 | **Spec**: Spec.md (same folder)
**Input**: Cumulative feature specification (Spec.md)

---
## Summary
Primary goal: Unify automated radiology reporting pipeline (current + previous study parsing, LOINC mapping, technique acquisition), enhanced editor assistance (completion, snippets, ghost suggestions), and reliable PACS submission with structured persistence. Plan consolidates earlier delivered UI/procedure/mapping enhancements and extends to robust, testable data / AI pipeline stages.

---
## Technical Context
**Language/Version**: .NET 9 (WPF desktop)  
**Primary Dependencies**: WPF, AvalonEdit, Npgsql, Serilog, (LLM provider abstraction) [NEEDS CLARIFICATION: actual provider], internal RuleEngine  
**Storage**: Postgres (phrase & mapping), local persistence (file or DB) [NEEDS CLARIFICATION: local DB type], in-memory caches  
**Testing**: xUnit (present in tests project) + potential golden text fixtures [NEEDS CLARIFICATION: snapshot strategy]  
**Target Platform**: Windows 10+ desktop (radiology workstation)  
**Project Type**: Single desktop solution with supporting libraries  
**Performance Goals**: <1s metadata acquisition; <300ms typical completion popup open; ghost suggestions request dispatch <100ms post idle; postprocess pipeline <5s total (LLM) [NEEDS CLARIFICATION: acceptable max]  
**Constraints**: Must function with intermittent LLM outages (graceful degradation); preserve user edits; no blocking UI thread for network I/O  
**Scale/Scope**: Single-user per workstation; concurrent sessions low (N≈1). Data volume: mappings O(10^3), phrases O(10^4)  

---
## Constitution Check
(Gates referencing internal engineering principles)  
Pending clarifications on: LLM provider contract, local persistence mechanism, logging schema. No structural violations—single project extension remains adequate.

---
## Project Structure (Affected)
- apps/Wysg.Musm.Radium (UI + orchestration)
- src/Wysg.Musm.RuleEngine (RBM steps)
- Potential new abstraction folder: `Services/ReportingPipeline` for orchestrated stages (fetch, parse, proofread, reportify, number, submit)

**Structure Decision**: Maintain existing single-project UI + supporting libs; introduce service boundary classes (interfaces) for each pipeline stage to enable isolated tests.

---
## Phase 0: Outline & Research
Unknowns / Research Tasks:
- LLM provider API latency & batching strategy.
- Failure modes (timeouts, partial outputs) handling spec.
- Local persistence format (DB vs JSON file) for final reports.
- Logging schema (fields: stage, correlation id, duration, success, error_code).
- Technique derivation from LOINC parts (rules library vs mapping table extension).
- PACS field size constraints & validation policy.
- Ghost suggestion acceptance span rule (line remainder vs dynamic token). 
Output: research.md documenting decisions (Decision, Rationale, Alternatives).

---
## Phase 1: Design & Contracts
Planned Artifacts:
1. data-model.md – Entities: Report, PrevReport, Study, MappingPart, ReportPipelineState, GhostSuggestion.
2. contracts/ – Interface definitions (pseudo-OpenAPI style) for internal service calls:
   - ICurrentStudyProvider, IPreviousStudyProvider
   - IStudynameMappingService
   - IReportPipeline (RunStagesAsync with stage enum) 
   - ILlmClient (ParseRemarks, SplitHeader Findings, ParseHeader, ProofreadBatch, GenerateConclusion)
   - IReportifier, INumberer
   - IPacsSubmissionService (ValidateAsync, SubmitAsync)
3. quickstart.md – Step-by-step: obtain study → pipeline run → edit → postprocess → PACS send.
4. Agent context update script integration (append new tech & entities).
5. Contract tests (failing) for pipeline orchestration (mock ILlmClient to force stage transitions & errors).

Design Considerations:
- Stage Pipeline pattern: Each stage returns immutable delta; aggregator composes final Report JSON.
- Retry policy: Non-idempotent stages (e.g., numbering) run only after dependent success.
- Cancellation tokens for long-running LLM calls.

---
## Phase 2: Task Planning Approach (Description Only)
The /tasks generation will:
- Derive tasks from contract interfaces (one test + implementation per interface) [P for independent ones].
- Generate model creation tasks for each entity.
- Create orchestration tests (integration style) validating FR-001..FR-020, FR-050..FR-058, FR-090..FR-097, FR-120..FR-122.
- Add resilience tasks: logging, retry/backoff, degraded UI states.
- Sequence: Tests (contracts) → Models → Services → Pipeline Orchestrator → Editor integration hooks → PACS submission enhancements → Non-functional (logging, perf) → Documentation updates.

Ordering Principles:
1. Contract tests fail first (ensure scope lock).
2. Core entities before services.
3. Services before UI binding / commands.
4. Reporting pipeline before ghost suggestion integration adjustments.
5. Persistence & PACS last (rely on stable internal representation).

Parallelization:
- Independent interface test+stub tasks flagged [P].
- UI integration tasks sequential (shared files).

Estimated Output: 45–55 tasks (broader scope than template example) with explicit file paths.

---
## Phase 3+: Future (Beyond /plan)
- Implementation per tasks.md
- Validation: golden sample reports, simulated failure injections for LLM calls, PACS submission dry-run harness.
- Performance measurement harness (timing each stage).

---
## Complexity Tracking
(No constitution violations requiring justification at present.)

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|--------------------------------------|
| (none) | | |

---
## Progress Tracking
**Phase Status**:
- [ ] Phase 0: Research complete
- [ ] Phase 1: Design complete
- [ ] Phase 2: Task planning described (this doc) – READY
- [ ] Phase 3: Tasks generated
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:
- [ ] Initial Constitution Check: PASS (pending unknowns resolution)
- [ ] Post-Design Constitution Check: PASS
- [ ] All NEEDS CLARIFICATION resolved
- [ ] Complexity deviations documented

---
## Recent Adjustments
- Central phrase service now loads per-account snapshot (AccountPhraseState) and serves editor completions from in-memory data.
- All future references use account_id (legacy tenant_id kept as alias for backward compatibility only at service boundary).
- Added non-blocking retry prefetch for phrase snapshot; empty snapshot not cached to avoid silent completion suppression.
- PhraseCache now supports Clear for future invalidation tasks.

## Recent Adjustment (Editor Idle Behavior)
- Removed legacy idle auto-close of completion popup. Ghost suggestions now appear without dismissing active completion list.

## Recent Adjustment (Completion Navigation)
- Modified popup key handling: initial Up/Down selects boundary item immediately (improves keyboard efficiency).

## Recent UI Adjustment
- Applied dark theme resource styles to PhrasesWindow (buttons, datagrid rows, headers, selection) for visual consistency.
- Added tooltip and clarified Toggle button action (Active flag inversion).

## Recent UI Adjustment (Phrases Window)
- Dark custom title bar implemented (WindowStyle=None + manual drag/minimize/close).
- Deprecated Toggle button column removed; simplified UX using Active checkbox (two-way binding triggers service toggle).

## Added Implementation (Reportified Toggle)
- View: Added ToggleButton (Reportified) in MainWindow header strip.
- ViewModel: Added HeaderText/FindingsText/ConclusionText bound to editors; added Reportified bool with ApplyReportify / ApplyDereportify methods.
- Logic: Ported simplified legacy NLPService.ProcessText (sentence purification, capitalization, numbering) + reversible restore via cached originals.
- Limitation documented: edits made while reportified are discarded on dereportify (no diff merge).

## Adjustment (Dereportify Algorithm)
- Replaced original-text caching restore with procedural inverse: regex removes numbering, indentation, trailing periods; applies dictionary-aware first-letter decap.
- Phrase list loaded once to build first-token capitalization whitelist.

## Adjustment (Drag Selection UX)
- Suppress word highlight while there is a non-empty selection to avoid masking reverse (right-to-left) drag selection visuals.

## Adjustment (Reverse Drag Selection Final)
- Added caret < wordStart guard and selection check in highlighter delegate.
- Reset _lastWordStart on mouse down to avoid negative span calculations while user begins new drag.

## Diagnostic Logging Addition
- Inserted verbose debug logging in EditorControl for selection lifecycle (mouse down, move, selection change, highlighter decisions) to isolate reverse drag selection anomaly.

## Update (Reverse Selection Highlight Suppression)
- Simplified approach: introduced _suppressHighlight boolean instead of highlighter attach/detach to avoid dependence on missing APIs.

## Iteration (Drag Selection Stabilization)
- Introduced _mouseSelecting state to gate highlight/timer logic; prevents interference with AvalonEdit selection when dragging (especially reverse).
- Extra instrumentation for mouse down/up and selection deltas.

## Enhancement (Reverse Drag Enforcement)
- Implemented anchor capture on mouse down and enforced selection span in MouseMove when dragging backwards.

## Enhancement (Selection Drag Debounce)
- Added _lastSelOffset/_lastSelLength/_lastSelLog to throttle selection changed handling during drag; only log meaningful span changes every 40ms.
- Summary emitted on mouse up.

## Enhancement (Anchored Manual Drag Selection)
- Replaced debounce-only approach with deterministic manual selection enforcement (anchor + dynamic span) for stable bi-directional selection.

## Enhancement (Drag Diagnostics & Stability)
- Added CalculateAnchorFromPoint; mouse capture to stabilize selection; throttled logging every 25ms; ignore nested drag begins.

## Adjustment (Simplified Drag Fix)
- Replaced complex anchor-from-point + capture with lightweight caret-based anchor and delayed start flag _dragStarted to avoid freeze and re-entrancy.

---
## Phrase Extraction Window (New)
- Create PhraseExtractionWindow (XAML + code-behind) bound to PhraseExtractionViewModel.
- ViewModel responsibilities: hold lines, generate candidates, manage selection/saving states.
- Generation algorithm: split selected line into tokens (space-delimited), produce all contiguous spans i..j, push into HashSet (OrdinalIgnoreCase), record incremental order.
- Query existing phrases once per generation (GetPhrasesForAccountAsync) for current tenant account id.
- Mark existing vs new; disable existing rows.
- Commands: Regenerate, SelectAllNew, Clear, SaveSelected.
- Save workflow: invoke UpsertPhraseAsync sequentially (UI acceptable small batches); on success mark Saved; on exception mark Error.
- Provide Load(header, findings, conclusion) API from window code-behind to populate lines.

## Refinement: Phrase Extraction Window
- Modify candidate generation to set Selected=true only for single-word new phrases.
- Introduce PhraseCandidate callback to ViewModel for immediate NewCount / command CanExecute recalculation.
- DataGridCheckBoxColumn uses UpdateSourceTrigger=PropertyChanged for instant updates.
- Provide dark theme resource styles (buttons, list, datagrid, text) analogous to existing dark windows.

## Reliability: PhraseService Index Ensure
- Wrap EnsureIndexAsync in retry loop (3 attempts) using IsTransient logic.
- Linear backoff (300ms * attempt) prevents hammering on network hiccups.
- Mark _indexEnsured after final failure to avoid continuous contention (index is optional if existing).

## Refinement: Candidate Ordering & Line Derivation
- Apply dereportify transform (strip numbering bullets/arrows) when collecting lines for extraction window.
- Maintain stable ordering: enabled first (IsEnabled=true), then disabled; sort secondarily by initial Order.
- Trigger reorder on any candidate state change (selection toggle, save result) and after generation.

## Refinement: Extraction Window Source & Save
- Add MainViewModel.GetDereportifiedSections to derive clean text regardless of current reportified toggle.
- Replace direct property reads in MainWindow OnExtractPhrases with dereportified tuple.
- Introduce IsBusy flag in PhraseExtractionViewModel; disable Save during operation.
- Keep persistence single-path (central DB); rely on in-memory snapshot update to reflect new phrases instantly.
