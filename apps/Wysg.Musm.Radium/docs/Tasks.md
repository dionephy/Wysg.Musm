# Tasks: Radium Cumulative (Reporting Workflow + Editor + Mapping + PACS)

## Added
- [X] T404 Merge account and global phrases in Phrases tab (VM loads both lists; rows keep AccountId for scope-aware toggles) (FR-283).
- [X] T405 Ensure Convert-to-global immediately updates UI lists and caches (GlobalPhrasesViewModel refresh + backend snapshot/caches) (FR-279, FR-281).
- [X] T406 Add Select All button to Account Phrases grid (Global Phrases tab) with command `SelectAllAccountPhrasesCommand` (FR-285).
- [X] T407 Add `Global` boolean column in Settings → Phrases tab bound to `PhraseRow.IsGlobal` (FR-284).
- [X] T393 Add Global Phrases admin tab UI (SettingsWindow) with add, load-by-account, convert-selected, toggle active (FR-279).
- [X] T394 Implement GlobalPhrasesViewModel: AccountPhrases list, SearchAccountPhrasesCommand, ConvertSelectedCommand wired to IPhraseService.ConvertToGlobalPhrasesAsync (FR-279).
- [X] T395 Wire SettingsWindow AccountId binding and restrict Global Phrases tab visibility to AccountId==1 (temporary admin gate) (FR-279).
- [X] T396 Ensure PhraseService snapshots and caches are updated after conversion; clear both account and global caches (FR-279).
- [ ] T397 Update PhraseCompletionProvider to prefer combined phrases (global + account) by default and add tests.
- [ ] T398 Add unit/integration tests for ConvertToGlobalPhrasesAsync covering: existing global dedupe path and create-global-then-delete-duplicates path.
- [X] T399 Default non-global listing on Global Phrases tab (cross-account) using IPhraseService.GetAllNonGlobalPhraseMetaAsync (FR-280).
- [X] T400 Enable Convert Selected button by default; handler validates selection at runtime (UI) (FR-279).
- [X] T401 Implement failsafe duplicate purge when creating/upserting global: delete all non-global duplicates with same text across accounts (FR-281).
- [X] T402 Remove manual account filter inputs from Global Phrases tab and add read-only AccountId column to Account Phrases grid (FR-282).
- [X] T403 Fix selection checkbox binding in Account Phrases grid by making grid editable for that column (two-way IsChecked), ensuring Convert Selected works (FR-279, FR-282).

## Added (previous)
- [X] T366 Remove global semaphore serialization in PhraseService (per-account only) (FR-261).
- [X] T367 Eliminate manual per-command CancellationTokenSource in toggle/upsert (FR-262).
- [X] T368 Tune connection string (MaxPoolSize<=50, KeepAlive=30s) & simplify retry (FR-263).

## Added (previous legacy)
- [X] T358 Implement synchronous phrase database interaction flow (FR-258) ensuring stability under rapid clicks and network latency.
- [X] T359 Add per-account update locks to PhraseService to prevent UI state corruption during database operations (FR-259).
- [X] T360 Enhance PhrasesViewModel to display snapshot state instead of optimistic UI state (FR-260).
- [X] T361 Implement automatic consistency recovery via snapshot refresh when phrase operations fail (FR-260).
- [X] T362 Add UI toggle prevention during active database operations to ensure atomicity (FR-259).

**Input**: Spec.md & Plan.md (cumulative)  
**Prerequisites**: Plan.md completed; research & design pending for new pipeline (some legacy features done)  

NOTE: This cumulative tasks list merges legacy completed items and the new FR-279 work. New tasks follow T### numbering continuation.

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
- [ ] T114 [P] Create contracts/ILlmClient.md (methods: ParseRemarks, SplitHeader Findings, ParseHeader, ProofreadBatch, GenerateConclusion)  
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
- [ ] T363 Performance test for phrase database operations (target <2s normal network) (FR-258)
- [ ] T364 Performance test for phrase snapshot updates (target <100ms) (FR-258)
- [ ] T365 Stress test for rapid phrase toggles under network latency (FR-259)

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
- Phrase database stability (T358-T362) complete before phrase stress testing (T363-T365)

---
## Parallel Execution Examples
Phase 1 parallel set: T111 T112 T113 T114 T115 T116 T117  
Contract test parallel set: T120 T121 T122 T123 T124 T125 T126
Editor completion test set: T155 T156 (after T210-T213 complete)
Bug fix test set: T157 T158 T159 T160 T161 T162 (after T214-T233 complete)
Phrase stability test set: T363 T364 T365 (after T358-T362 complete)

---
## Validation Checklist
- [X] FR-135 documented and implemented (Split Arg3 index).
- [X] All FR-001..FR-020 have at least one test task
- [X] All FR-050..FR-058 have editor test or impl task
- [X] All FR-090..FR-099 mapping & procedure tasks enumerated
- [X] All FR-120..FR-132 reliability, completion, and bug fix tasks covered
- [X] All FR-258..FR-260 phrase database stability tasks implemented
- [ ] No unresolved dependency loops
- [X] Each task has concrete output (file(s) or behavior)

---
## Added / Completed (Account Migration + Completion Improvements + Bug Fixes + Selection Guard Fix + Multiple Event Handling + Navigation State Tracking + Recursive Guard Protection + Phrase Database Stability)
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
- [X] TM21 Drag selection stabilization (_mouse Selecting flag + suppression + logging).
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
- [X] T358 Implement synchronous phrase database interaction flow (FR-258) ensuring stability under rapid clicks and network latency.
- [X] T359 Add per-account update locks to PhraseService to prevent UI state corruption during database operations (FR-259).
- [X] T360 Enhance PhrasesViewModel to display snapshot state instead of optimistic UI state (FR-260).
- [X] T361 Implement automatic consistency recovery via snapshot refresh when phrase operations fail (FR-260).
- [X] T362 Add UI toggle prevention during active database operations to ensure atomicity (FR-259).
- [X] T376 Modify radium.phrase schema to allow NULL account_id for global phrases (FR-273).
- [X] T377 Create filtered unique indexes for global and per-account phrase uniqueness (FR-278).
- [X] T378 Update IPhraseService interface to support nullable account_id and add global/combined query methods (FR-274).
- [X] T379 Update PhraseInfo record to use nullable AccountId (FR-273).
- [X] T380 Implement PhraseService global phrase queries (GetGlobalPhrasesAsync, GetGlobalPhrasesByPrefixAsync) (FR-274).
- [X] T381 Implement combined phrase queries with deduplication logic (GetCombinedPhrasesAsync, GetCombinedPhrasesByPrefixAsync) (FR-277).
- [X] T382 Modify UpsertPhraseInternalAsync to handle NULL account_id in SQL queries (FR-276).
- [X] T383 Modify ToggleActiveInternalAsync to handle NULL account_id in SQL queries (FR-276).
- [X] T384 Create database migration script with rollback procedure (FR-273, FR-278).
- [X] T389 Update Spec/Plan/Tasks documentation with global phrases feature details.
- [X] T390 Create comprehensive implementation guide and summary documents.
- [X] T391 Update AzureSqlPhraseService to implement all new global phrases methods (FR-273..FR-278).
- [X] T392 Fix compilation errors in AzureSqlPhraseService implementation.
- [ ] T385 Update completion provider to use combined phrases (global + account) by default.
- [X] T386 Add global phrases management UI in Settings window.
- [ ] T387 Add unit tests for global phrase operations (insert, toggle, query).
- [ ] T388 Add integration tests for combined phrase queries with precedence rules.

## Added (Hotkey Feature - 2025-01-09)
- [ ] T408 Create `radium.hotkey` table in central database with all constraints, indexes, and triggers (FR-287).
- [ ] T409 Define `IHotkeyService` interface with methods: GetActiveHotkeysAsync, UpsertHotkeyAsync, ToggleActiveAsync, DeleteHotkeyAsync (FR-288).
- [ ] T410 Implement `HotkeyService` using CentralDataSourceProvider with synchronous database → snapshot flow (FR-288).
- [ ] T411 Implement hotkey snapshot caching mechanism parallel to phrase caching (per-account Dictionary with rev tracking) (FR-289).
- [ ] T412 Add hotkey cache invalidation in UpsertHotkeyAsync, ToggleActiveAsync, DeleteHotkeyAsync (FR-289).
- [ ] T413 Integrate hotkey expansion into EditorControl OnTextEntered handler with trigger detection logic (FR-290).
- [ ] T414 Implement inline text replacement when hotkey trigger followed by space/punctuation (FR-290).
- [ ] T415 Add Hotkeys tab to SettingsWindow with DataGrid and CRUD controls (FR-291).
- [ ] T416 Create HotkeysViewModel with observable collections and commands (Add, Delete, Refresh, Toggle) (FR-291).
- [ ] T417 Wire HotkeysViewModel to SettingsWindow with lazy instantiation on tab load (FR-291).
- [ ] T418 Apply dark theme styling to Hotkeys tab (DataGrid, TextBox, Button, CheckBox) (FR-291).
- [ ] T419 Register IHotkeyService and HotkeysViewModel in App.xaml.cs DI container.
- [ ] T420 Update Spec/Plan/Tasks documentation with hotkey feature details and SQL migration script.
- [X] T421 Inject IHotkeyService into MainViewModel and preload snapshot during editor init (FR-294).
- [X] T422 Implement composite ISnippetProvider that merges phrases and hotkeys, listing hotkeys as "{trigger}→{expansion}" (FR-292, FR-295, FR-296).
- [X] T423 Ensure completion selection replaces current word with expansion and caret advances (FR-293).
- [X] T426 Add spaces around arrow in hotkey completion display (FR-297).
- [X] T427 Truncate multi-line hotkey expansion to first line with ellipsis for display (FR-298).
- [X] T428 Suppress completion item tooltips by setting Description to null (FR-299).
- [ ] T424 Add unit tests for hotkey completion rendering and insertion behavior (prefix cases, arrow display-only).
- [ ] T425 Add snapshot refresh tests: add/toggle/delete hotkey reflects in GetActiveHotkeysAsync and completion items.
- [X] T429 Seed editor completion cache with combined (global + account) phrases on init (FR-301).
- [X] T430 Composite provider to fetch combined phrases and prefetch when cache empty (FR-300).
- [X] T431 Fix completion first-press navigation: Down/Up moves relative to current selection without extra press (FR-302).
- [X] T432 Ensure Home/End move editor caret (line start/end) and never change completion list selection; close popup when outside selection range (FR-303).
- [X] T433 Intercept Home/End at completion window level; forward only to editor and suppress ListBox handling (FR-304).
- [X] T434 Prevent first Down from selecting the last item by handling Up/Down in ListBox PreviewKeyDown (FR-305).
- [X] T435 Disable exact-match auto-selection so first Down always moves to second item (FR-306).
