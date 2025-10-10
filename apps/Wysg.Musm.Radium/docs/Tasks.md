# Tasks: Radium Cumulative (Reporting Workflow + Editor + Mapping + PACS)

## Added
- [X] T493 Add "Get HTML" button to SpyWindow → Crawl Editor toolbar and wire Click to `OnGetHtml` (FR-339).
- [X] T494 Implement `OnGetHtml` to fetch URL from clipboard (http/https), reuse shared HttpClient, and output HTML or error to `txtStatus` (FR-339, aligns with Custom Procedure `GetHTML`).
- [X] T495 Update Spec/Plan/Tasks with FR-339 entries and implementation notes.
- [X] T496 Register `CodePagesEncodingProvider` at startup to enable legacy encodings (EUC-KR/CP949) (FR-340).
- [X] T497 Implement smart HTML decoding (`HttpGetHtmlSmartAsync`) with charset detection (header → meta) and CP949 fallback when UTF-8 looks corrupted; use it for both button and procedure `GetHTML` (FR-340).
- [ ] T498 Add unit tests for smart decoding using fixture pages (UTF-8 vs CP949) and corruption heuristic.
- [X] T499 Extend `ProcedureExecutor` to support `GetHTML` for background PACS execution parity; reuse the same smart decoding helper (or shared utility).
- [ ] T500 Stream-decoding optimization for very large HTML responses (optional backlog).
- [X] T486 Add "Spy" button next to "Save Automation" in Settings → Automation tab (XAML) (FR-335).
- [X] T487 Wire `OnOpenSpy` handler in `SettingsWindow.xaml.cs` to open `SpyWindow` (owned by Settings) matching Main Window behavior (FR-335).
- [X] T488 Update Spec/Plan/Tasks with FR-335 and change log entry for Settings → Automation Spy button.
- [X] T489 Add PACS method "Get current patient remark" to SpyWindow Custom Procedures combo (Tag `GetCurrentPatientRemark`) and wire PacsService wrapper `GetCurrentPatientRemarkAsync` (FR-336).
- [X] T490 Add Custom Procedure op `Replace` (Arg1 Var, Arg2 String, Arg3 String) with presets and ExecuteSingle implementation (FR-337).
- [X] T491 Add Custom Procedure op `GetHTML` (Arg1 Var URL) with async fetch via HttpClient in ExecuteSingleAsync; integrate into Set/Run flows (FR-338).
- [X] T492 Update Spec/Plan/Tasks docs with FR-336..FR-338 entries and implementation notes.
- [X] T512 Fix `ProcedureExecutor` early-return that bypassed fallback/auto-seed and caused previous-value to persist when `GetHTML` executed.
- [X] T513 Support reading/writing procedure variables by both implicit `var{i}` and custom `OutputVar` names so `GetHTML` Arg1 Type=Var can use named variables.
- [X] T514 Register encoding provider in `ProcedureExecutor` and apply basic header/meta charset handling when decoding HTML.
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
- [X] T501 Enable multi-line input for Custom Procedures Arg1/Arg2/Arg3 String editors by setting `AcceptsReturn=True` in `SpyWindow.xaml` (FR-342).
- [X] T502 Update Spec.md and Plan.md with FR-342 and add to Validation Checklist; update Tasks.md (this file) cumulatively.
- [X] T503 Add automation modules to library: `GetStudyRemark`, `GetPatientRemark` (FR-348).
- [X] T504 Wire New Study automation executor to handle `GetStudyRemark` and `GetPatientRemark` by calling PacsService and updating VM properties (FR-349, FR-350).
- [X] T505 Add VM properties `StudyRemark` and `PatientRemark` and round-trip in `CurrentReportJson` (serialize and parse) (FR-351).
- [X] T506 Add status messages and error-safe guards for remark acquisition (FR-352).
- [ ] T507 Extend Add Study automation executor to honor the modules, including remark modules (backlog).
- [X] T508 Add distinct known control `PatientRemark` in `UiBookmarks.KnownControl` and surface in SpyWindow Known controls combo (FR-353, FR-354).
- [X] T509 Document requirement for `GetCurrentPatientRemark` procedure to target Element=`PatientRemark` to avoid accidentally reading `StudyRemark` (FR-355).
- [X] T510 Auto-seed default procedure when missing: `GetCurrentPatientRemark` → single-step `GetText` on Element=`PatientRemark` (FR-356, FR-357).
- [X] T511 Auto-seed default procedure when missing: `GetCurrentStudyRemark` → single-step `GetText` on Element=`StudyRemark` (FR-358).

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

Legend:  
- [P] = Parallel-safe (different files / no dependency)  
- (FR-xxx) = Links to Functional Requirement  
- Legacy tasks already completed retained for traceability (Done)

---
## Phase 0 – Research (New)
- [ ] T100 Establish LLM provider contract doc (timeout, error schema) (Spec Ambiguity 1,4)  
- [ ] T101 Research PACS field length constraints; document validation policy (Ambiguity 2)  
- [ ] T102 Define technique derivation rule from LOINC parts (Ambiguity 3)  
- [ ] T103 Decide local persistence mechanism (file or table) + atomic write approach  
- [ ] T104 Define logging schema (stage, correlation id, duration, status, error_code)  
- [ ] T105 Determine ghost acceptance span rule (line remainder vs token) (Ambiguity 5)

## Phase 1 – Design & Contracts
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

## Phase 1 – Contract Tests (Fail First)
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

## Phase 2 – Models & Infrastructure
- [ ] T130 Implement entity classes (ReportState DTO) in src/.../Reporting/Models  
- [ ] T131 Implement mapping cache with invalidation on save (FR-090, FR-094)  
- [ ] T132 Persistence adapter (file or table) decision implementation (depends T103)  
- [ ] T133 Logging schema implementation (Serilog enrichers) (depends T104)  
- [ ] T134 First-chance Postgres sampler already implemented (legacy FR-120) – verify test coverage  

## Phase 2 – Service Implementations
- [ ] T140 Implement CurrentStudyProvider (PACS + UI integration)  
- [ ] T141 Implement PreviousStudyProvider  
- [ ] T142 Implement LlmClient adapter (retry, timeout)  
- [ ] T143 Implement Reportifier (rule engine hook)  
- [ ] T144 Implement Numberer  
- [ ] T145 Implement PacsSubmissionService (ValidateAsync, SubmitAsync)  
- [ ] T146 Implement StudynameMappingService (playbook suggestions already legacy) unify interface  
- [ ] T147 Implement ReportPipeline orchestrator (stage enum + idempotent transitions)  
- [ ] T148 Add PacsService banner extraction tests (patient number & study datetime) (FR-099)  

## Phase 2 – Editor Integration Enhancements
- [ ] T150 Add ghost acceptance span policy implementation (depends T105) (FR-056)  
- [ ] T151 Add import previous findings context menu → transformation call (FR-058)  
- [ ] T152 Snippet placeholder navigation validation tests (FR-057)  
- [ ] T153 Completion popup test: exact match only selection (FR-052)  
- [ ] T154 Idle closure + ghost trigger test (FR-055)  
- [X] T155 Completion cache invalidation test: verify Clear() called on phrase add/modify (FR-124)
- [X] T156 Keyboard navigation test: verify Down/Up keys change selection in completion popup (FR-127)
- [X] T157 Service injection test: verify PhraseExtractionViewModel resolves from DI with all dependencies (FR-126)
- [X] T158 Navigation reliability test: verify Up/Down keys work consistently in completion popup (FR-127)
- [X] T159 Selection guard recursion test: verify multiple selection events don't cause infinite recursion (FR-128)
- [X] T160 Multiple event handling test: verify SelectionChanged event patterns are analyzed correctly (FR-129)
- [X] T161 Navigation state tracking test: verify first Down key always selects first item (FR-130)
- [X] T162 Recursive guard protection test: verify guard flag prevents infinite loops during programmatic changes (FR-132)

## Phase 3 – Pipeline Wiring (UI)
- [ ] T163 Hook UI command: Load Current Study → invoke pipeline stages 1–5 automatically (FR-001..FR-005)  
- [ ] T164 Integrate previous study retrieval + comparison selection UI (FR-007..FR-012)  
- [ ] T165 Wire postprocess command invoking FR-014..FR-017 sequence (LLM + RBM)  
- [ ] T166 Display proofread vs original toggles  
- [ ] T167 Error banner for individual stage failures (FR-122)  

## Phase 3 – PACS & Persistence
- [ ] T170 Validate banner vs metadata prior to send (FR-018)  
- [ ] T171 Submit formatted sections & await acknowledgment (FR-019)  
- [ ] T172 Atomic persistence of final report JSON (FR-020)  
- [ ] T173 Field length pre-validation (depends T101, FR-018)  

## Phase 3 – Mapping UI Consolidation
- [ ] T180 Integrate technique derivation post save (depends T102)  
- [ ] T181 Add mapping cache hot-reload on save (FR-094)  
- [ ] T182 Add optional remove/reorder buttons (legacy TODO)  

## Phase 4 – Reliability & Observability
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

---
## Validation Checklist
- [X] FR-339 documented and implemented (Crawl Editor Get HTML button).
- [X] FR-340 documented and implemented (KR charset decode + CP949 fallback for Get HTML).
- [X] FR-135 documented and implemented (Split Arg3 index).
- [X] New PACS method and operations documented and implemented (FR-336, FR-337, FR-338).
- [X] FR-342 documented and implemented (Procedures grid multiline args for string types).
- [X] FR-348..FR-352 documented and implemented (Automation modules for remarks + JSON round-trip + error handling).
- [X] FR-353..FR-355 documented and implemented (Distinct PatientRemark bookmark + mapping list + procedure usage).
- [X] FR-356..FR-358 documented and implemented (Auto-seed default key procedures for remarks).
- [X] FR-360..FR-361 documented and implemented (ProcedureExecutor supports GetHTML/Replace and fixed early-return).
- [X] All FR-001..FR-020 have at least one test task
- [X] All FR-050..FR-058 have editor test or impl task
- [X] All FR-090..FR-099 mapping & procedure tasks enumerated
- [X] All FR-120..FR-132 reliability, completion, and bug fix tasks covered
- [X] All FR-258..FR-260 phrase database stability tasks implemented
- [ ] No unresolved dependency loops
- [X] Each task has concrete output (file(s) or behavior)
