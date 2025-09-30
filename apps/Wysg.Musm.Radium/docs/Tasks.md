# Tasks: Radium Cumulative (Reporting Pipeline + Editor + Mapping + PACS)

## Added
- [X] TM21 Drag selection stabilization (_mouseSelecting flag + suppression + logging).
- [X] TM22 Reverse drag selection enforcement (anchor + manual Select).
- [X] TM23 Debounce selection change handling during drag (stabilize reverse selection + reduce log noise).
- [X] TM24 Anchored manual drag selection (explicit Editor.Select span management).
- [X] TM25 Enhanced drag selection diagnostics (anchor from point, mouse capture, throttled logs).
- [X] TM26 Simplified drag selection (caret anchor, delayed start, removed capture) to resolve freeze.
- [X] TM27 Phrase Extraction Window (lines → n‑gram phrase candidates, save new phrases).
- [X] TM28 Phrase Extraction refinement (single-word auto-select, immediate Save enable) & PhraseService index retry logic.
- [X] TM29 Phrase Extraction refinement: dereportified line sourcing + enabled-first ordering.
- [X] TM30 Phrase Extraction dereportified loading + async non-blocking save (IsBusy gating).
- [X] TM31 Phrase rev stabilization: conditional trigger + app pre-select no-op short-circuit + UI load suppression.
- [X] T205 Add GetTextOCR procedure operation (SpyWindow XAML + code-behind) (FR-098).
- [X] T206 Extend PacsService with GetCurrentPatientNumberAsync heuristic (FR-099).
- [X] T207 Extend PacsService with GetCurrentStudyDateTimeAsync date/time pattern extraction (FR-099/FR-123).
- [X] T208 Update App shutdown behavior to OnMainWindowClose after main window shows (usability).
- [X] T209 Update Spec/Plan/Tasks docs with FR-098..FR-099, FR-123 (documentation sync).
- [X] T210 Add IPhraseCache.Clear() method for cache invalidation (FR-124).
- [X] T211 Implement completion cache invalidation in PhraseService.UpsertPhraseAsync() and ToggleActiveAsync() (FR-124).
- [X] T212 Fix MusmCompletionWindow keyboard navigation by updating selection guard for Down/Up keys (FR-125).
- [X] T213 Update documentation with editor completion improvements (FR-124, FR-125).
- [X] T214 Fix phrase extraction window service injection by using DI instead of manual instantiation (FR-126).
- [X] T215 Register PhraseExtractionViewModel in DI container (FR-126).
- [X] T216 Enhance completion popup Up/Down key handling with explicit selection management (FR-127).
- [X] T217 Update documentation with bug fixes (FR-126, FR-127).
- [X] T218 Prevent selection guard recursion by temporarily disabling event handlers during programmatic changes (FR-128).
- [X] T219 Update documentation with selection guard recursion fix (FR-128).
- [X] T220 Enhance selection preservation for keyboard navigation by analyzing SelectionChanged event patterns (FR-129).
- [X] T221 Update documentation with multiple event handling improvement (FR-129).
- [X] T222 Add navigation state tracking flag to ensure first Down key selects first item consistently (FR-130).
- [X] T223 Update documentation with navigation state tracking implementation (FR-130).
- [X] T224 Harden completion first navigation by resetting state on list rebuilds and when ListBox lacks focus so the initial Down/Up selects the boundary item (FR-131).
- [X] T225 Update Spec/Plan/Tasks with focus-aware first navigation guard notes (FR-131).
- [X] T226 Route Up/Down handling through the editor so ListBox never requires duplicate presses (FR-131).
- [X] T227 Document manual navigation handling in Spec/Plan/Tasks (FR-131).
- [X] T228 Ensure first navigation cannot re-trigger after focus transfer and moves immediately when already at boundary (FR-131).
- [X] T229 Update documentation with focus-independent first navigation notes (FR-131).
- [X] T230 Add guard-silent selection helper so editor-driven navigation does not trigger popup clearing (FR-131).
- [X] T231 Update documentation with guard-silent selection helper coverage (FR-131).
- [X] T232 Add recursive guard protection to prevent infinite loops during programmatic selection changes (FR-132).
- [X] T233 Update documentation with recursive guard protection implementation (FR-132).
- [X] T234 Add unit tests for completion navigation functionality (testing).
- [X] T235 Implement bounded height + single-step navigation (internal nav index + dynamic MaxHeight) (FR-133).
- [X] T236 Update Spec/Plan/Tasks with FR-133 documentation (FR-133).
- [X] T237 Implement adaptive measured height (exact height ≤8 items, clamp >8) (FR-134).
- [X] T238 Update Spec/Plan/Tasks with FR-134 documentation (FR-134).

**Input**: Spec.md & Plan.md (cumulative)  
**Prerequisites**: Plan.md completed; research & design pending for new pipeline (some legacy features done)  

NOTE: This cumulative tasks list merges legacy completed items (marked [x]/Done) and newly required tasks for extended scope. New tasks follow T### numbering continuation.

Legend:  
- [P] = Parallel-safe (different files / no dependency)  
- (FR-xxx) = Links to Functional Requirement  
- Legacy tasks already completed retained for traceability (Done)

---
## Phase 0 ? Research (New)
- [ ] T100 Establish LLM provider contract doc (timeout, error schema) (Spec Ambiguity 1,4)  
- [ ] T101 Research PACS field length constraints; document validation policy (Ambiguity 2)  
- [ ] T102 Define technique derivation rule from LOINC parts (Ambiguity 3)  
- [ ] T103 Decide local persistence mechanism (file vs table) + atomic write approach  
- [ ] T104 Define logging schema (stage, correlation id, duration, status, error_code)  
- [ ] T105 Determine ghost acceptance span rule (line remainder vs token) (Ambiguity 5)

## Phase 1 ? Design & Contracts
- [ ] T110 [P] Create data-model.md with entities & field rationale (Report, PrevReport, Study, MappingPart, GhostSuggestion)  
- [ ] T111 [P] Create contracts/ICurrentStudyProvider.md (interface & semantics)  
- [ ] T112 [P] Create contracts/IPreviousStudyProvider.md  
- [ ] T113 [P] Create contracts/IStudynameMappingService.md (mapping & cache invalidation)  
- [ ] T114 [P] Create contracts/ILlmClient.md (methods: ParseRemarks, SplitHeaderFindings, ParseHeader, ProofreadBatch, GenerateConclusion)  
- [ ] T115 [P] Create contracts/IReportifier.md & INumberer.md  
- [ ] T116 [P] Create contracts/IPacsSubmissionService.md (ValidateAsync, SubmitAsync)  
- [ ] T117 [P] Create contracts/IReportPipeline.cs (RunStageAsync, composite state)  
- [ ] T118 Quickstart.md steps (select study → pipeline stages → edit → postprocess → PACS send)  
- [ ] T119 Add agent-file updates (append new tech & FR mapping)  

## Phase 1 ? Contract Tests (Fail First)
- [ ] T120 [P] Test ICurrentStudyProvider returns metadata model (mock source)  
- [ ] T121 [P] Test IPreviousStudyProvider returns raw previous report text  
- [ ] T122 [P] Test ILlmClient.SplitHeaderFindings returns split_index invariants  
- [ ] T123 [P] Test ILlmClient.ProofreadBatch returns same count outputs preserving original  
- [ ] T124 [P] Test Reportifier enforces deterministic formatting rules (skeleton)  
- [ ] T125 [P] Test Numberer enumerates lines in conclusion_reportified  
- [ ] T126 [P] Test IPacsSubmissionService.ValidateAsync blocks on mismatched banner  
- [ ] T127 Orchestrator test: sequence stages populating FR-001..FR-017 fields (mocks)  
- [ ] T128 Failure path test: LLM failure marks only affected fields & continues (FR-122)  
- [ ] T129 OCR op test: engine unavailable path returns "(ocr unavailable)" (FR-098/FR-123)  

## Phase 2 ? Models & Infrastructure
- [ ] T130 Implement entity classes (ReportState DTO) in src/.../Reporting/Models  
- [ ] T131 Implement mapping cache with invalidation on save (FR-090, FR-094)  
- [ ] T132 Persistence adapter (file or table) decision implementation (depends T103)  
- [ ] T133 Logging schema implementation (Serilog enrichers) (depends T104)  
- [ ] T134 First-chance Postgres sampler already implemented (legacy FR-120) ? verify test coverage  

## Phase 2 ? Service Implementations
- [ ] T140 Implement CurrentStudyProvider (PACS + UI integration)  
- [ ] T141 Implement PreviousStudyProvider  
- [ ] T142 Implement LlmClient adapter (retry, timeout)  
- [ ] T143 Implement Reportifier (rule engine hook)  
- [ ] T144 Implement Numberer  
- [ ] T145 Implement PacsSubmissionService (ValidateAsync, SubmitAsync)  
- [ ] T146 Implement StudynameMappingService (playbook suggestions already legacy) unify interface  
- [ ] T147 Implement ReportPipeline orchestrator (stage enum + idempotent transitions)  
- [ ] T148 Add PacsService banner extraction tests (patient number & study datetime) (FR-099)  

## Phase 2 ? Editor Integration Enhancements
- [ ] T150 Add ghost acceptance span policy implementation (depends T105) (FR-056)  
- [ ] T151 Add import previous findings context menu → transformation call (FR-058)  
- [ ] T152 Snippet placeholder navigation validation tests (FR-057)  
- [ ] T153 Completion popup test: exact match only selection (FR-052)  
- [ ] T154 Idle closure + ghost trigger test (FR-055)  
- [X] T155 Completion cache invalidation test: verify Clear() called on phrase add/modify (FR-124)
- [X] T156 Keyboard navigation test: verify Down/Up keys change selection in completion popup (FR-125)
- [X] T157 Service injection test: verify PhraseExtractionViewModel resolves from DI with all dependencies (FR-126)
- [X] T158 Navigation reliability test: verify Up/Down keys work consistently in completion popup (FR-127)
- [X] T159 Selection guard recursion test: verify multiple selection events don't cause infinite recursion (FR-128)
- [X] T160 Multiple event handling test: verify SelectionChanged event patterns are analyzed correctly (FR-129)
- [X] T161 Navigation state tracking test: verify first Down key always selects first item (FR-130)
- [X] T162 Recursive guard protection test: verify guard flag prevents infinite loops during programmatic changes (FR-132)

## Phase 3 ? Pipeline Wiring (UI)
- [ ] T163 Hook UI command: Load Current Study → invoke pipeline stages 1–5 automatically (FR-001..FR-005)  
- [ ] T164 Integrate previous study retrieval + comparison selection UI (FR-007..FR-012)  
- [ ] T165 Wire postprocess command invoking FR-014..FR-017 sequence (LLM + RBM)  
- [ ] T166 Display proofread vs original toggles  
- [ ] T167 Error banner for individual stage failures (FR-122)  

## Phase 3 ? PACS & Persistence
- [ ] T170 Validate banner vs metadata prior to send (FR-018)  
- [ ] T171 Submit formatted sections & await acknowledgment (FR-019)  
- [ ] T172 Atomic persistence of final report JSON (FR-020)  
- [ ] T173 Field length pre-validation (depends T101, FR-018)  

## Phase 3 ? Mapping UI Consolidation
- [ ] T180 Integrate technique derivation post save (depends T102)  
- [ ] T181 Add mapping cache hot-reload on save (FR-094)  
- [ ] T182 Add optional remove/reorder buttons (legacy TODO)  

## Phase 4 ? Reliability & Observability
- [ ] T190 Implement correlation id propagation across pipeline logs  
- [ ] T191 Add duration metrics for each stage (timing decorator)  
- [ ] T192 Failure injection tests (simulate LLM timeout)  
- [ ] T193 Snapshot tests for reportifier + numberer outputs  
- [ ] T194 Performance test harness (measure idle→ghost dispatch latency)  
- [ ] T195 Performance test for completion cache refresh latency (target <50ms) (FR-124)
- [ ] T196 Performance test for keyboard navigation response time (target <50ms) (FR-127)
- [ ] T197 Performance test for selection guard event handling (target <10ms) (FR-128)
- [ ] T198 Performance test for multiple event processing (target <5ms) (FR-129)
- [ ] T199 Performance test for navigation state tracking (target <1ms) (FR-130)
- [ ] T200 Performance test for recursive guard protection (target <1ms) (FR-132)

## Phase 5 ? Documentation & Finalization
- [ ] T201 Update quickstart with actual commands & sample timeline  
- [ ] T202 Add troubleshooting section (LLM failures, PACS mismatch, OCR unavailable)  
- [ ] T203 Update agent context file with final tech + last 3 changes  
- [ ] T204 Review all FR coverage matrix (Spec vs tests)  
- [ ] T205 Final acceptance checklist sign-off  

---
## Legacy Completed (Traceability)
(From previous Tasks.md; no action required)
- Parity UI tree focus, procedure presets, value extraction ops, blank header preservation, ToDateTime op, PACS selection methods, Postgres sampler, PhraseService refactor, mapping UI behaviors, playbook import enhancements, status messaging.

---
## Dependencies
- T103 before T132; T104 before T133; T105 before T150  
- Contract tests (T120–T129) must fail before implementing services (T140–T147)  
- LLM client (T142) before pipeline orchestrator (T147)  
- Pipeline orchestrator before UI wiring (T163+)  
- Technique derivation rule (T102) before mapping derivation integration (T180)  
- OCR op tests (T129) before expanding procedure automation scenarios
- Editor completion improvements (T210-T213) complete before expanding editor test coverage (T155-T156)
- Bug fixes (T214-T217) complete before service injection tests (T157) and navigation tests (T158)
- Selection guard recursion fix (T218-T219) complete before selection guard tests (T159)
- Multiple event handling (T220-T221) complete before event processing tests (T160)
- Navigation state tracking (T222-T231) complete before navigation state tests (T161)
- Recursive guard protection (T232-T233) complete before recursive guard tests (T162)
- Unit test implementation (T234) complete before comprehensive testing phase

---
## Parallel Execution Examples
Phase 1 parallel set: T111 T112 T113 T114 T115 T116 T117  
Contract test parallel set: T120 T121 T122 T123 T124 T125 T126
Editor completion test set: T155 T156 (after T210-T213 complete)
Bug fix test set: T157 T158 T159 T160 T161 T162 (after T214-T233 complete)

---
## Validation Checklist
- [X] All FR-001..FR-020 have at least one test task
- [X] All FR-050..FR-058 have editor test or impl task
- [X] All FR-090..FR-099 mapping & procedure tasks enumerated
- [X] All FR-120..FR-132 reliability, completion, and bug fix tasks covered
- [ ] No unresolved dependency loops
- [X] Each task has concrete output (file(s) or behavior)

---
## Added / Completed (Account Migration + Completion Improvements + Bug Fixes + Selection Guard Fix + Multiple Event Handling + Navigation State Tracking + Recursive Guard Protection)
- [X] TM01 Migrate phrase service to account_id terminology (snapshot-backed)
- [X] TM02 Update PhraseCompletionProvider to use snapshot via account_id
- [X] TM03 Update Spec/Plan with migration notes
- [X] TM04 Implement phrase completion snapshot retry (avoid caching empty list)
- [X] TM05 Add PhraseCache.Clear and documentation updates
- [X] TM06 Allow completion popup + ghost suggestions coexist (remove idle auto-close)
- [X] TM07 Immediate selection on first Up/Down key in completion popup.
- [X] TM08 Dark theme styling applied to PhrasesWindow DataGrid + controls.
- [X] TM09 Clarify Toggle button (tooltip + spec note).
- [X] TM10 Dark custom title bar for PhrasesWindow.
- [X] TM11 Remove Toggle column; Active checkbox drives activation.
- [X] TM12 Add Reportified toggle (UI + binding).
- [X] TM13 Implement reportify/dereportify logic (ported from legacy NLPService) with original text caching.
- [X] TM14 Implement inverse dereportify (remove numbering, trailing periods, decap unless dictionary protected).
- [X] TM15 Paragraph-based conclusion numbering (multi-paragraph only).
- [X] TM16 Dereportify ensures '-->' followed by single space.
- [X] TM17 Improve reverse (right-to-left) drag selection visibility by disabling current word highlight during selection.
- [X] TM18 Finalize reverse drag selection fix (highlight suppression + mouse down reset).
- [X] TM19 Add verbose debug instrumentation for reverse drag selection (mouse + selection + highlighter trace).
- [X] TM20 Implement highlight suppression flag for reverse drag selection stabilization.
- [X] TM21 Drag selection stabilization (_mouseSelecting flag + suppression + logging).
- [X] TM22 Reverse drag selection enforcement (anchor + manual Select).
- [X] TM23 Debounce selection change handling during drag (stabilize reverse selection + reduce log noise).
- [X] TM24 Anchored manual drag selection (explicit Editor.Select span management).
- [X] TM25 Enhanced drag selection diagnostics (anchor from point, mouse capture, throttled logs).
- [X] TM26 Simplified drag selection (caret anchor, delayed start, removed capture) to resolve freeze.
- [X] TM27 Phrase Extraction Window (lines → n‑gram phrase candidates, save new phrases).
- [X] TM28 Phrase Extraction refinement (single-word auto-select, immediate Save enable) & PhraseService index retry logic.
- [X] TM29 Phrase Extraction refinement: dereportified line sourcing + enabled-first ordering.
- [X] TM30 Phrase Extraction dereportified loading + async non-blocking save (IsBusy gating).
- [X] TM31 Phrase rev stabilization: conditional trigger + app pre-select no-op short-circuit + UI load suppression.
- [X] T210 Add IPhraseCache.Clear() method for cache invalidation (FR-124).
- [X] T211 Implement completion cache invalidation in PhraseService.UpsertPhraseAsync() and ToggleActiveAsync() (FR-124).
- [X] T212 Fix MusmCompletionWindow keyboard navigation by updating selection guard for Down/Up keys (FR-125).
- [X] T213 Update documentation with editor completion improvements (FR-124, FR-125).
- [X] T214 Fix phrase extraction window service injection by using DI instead of manual instantiation (FR-126).
- [X] T215 Register PhraseExtractionViewModel in DI container (FR-126).
- [X] T216 Enhance completion popup Up/Down key handling with explicit selection management (FR-127).
- [X] T217 Update documentation with bug fixes (FR-126, FR-127).
- [X] T218 Prevent selection guard recursion by temporarily disabling event handlers during programmatic changes (FR-128).
- [X] T219 Update documentation with selection guard recursion fix (FR-128).
- [X] T220 Enhance selection preservation for keyboard navigation by analyzing SelectionChanged event patterns (FR-129).
- [X] T221 Update documentation with multiple event handling improvement (FR-129).
- [X] T222 Add navigation state tracking flag to ensure first Down key selects first item consistently (FR-130).
- [X] T223 Update documentation with navigation state tracking implementation (FR-130).
- [X] T232 Add recursive guard protection to prevent infinite loops during programmatic selection changes (FR-132).
- [X] T233 Update documentation with recursive guard protection implementation (FR-132).
- [X] T234 Add unit tests for completion navigation functionality (testing).
- [X] T235 Implement bounded height + single-step navigation (internal nav index + dynamic MaxHeight) (FR-133).
- [X] T236 Update Spec/Plan/Tasks with FR-133 documentation (FR-133).
- [X] T237 Implement adaptive measured height (exact height ≤8 items, clamp >8) (FR-134).
- [X] T238 Update Spec/Plan/Tasks with FR-134 documentation (FR-134).
