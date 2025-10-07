# Tasks: Radium Cumulative (Reporting Workflow + Editor + Mapping + PACS)

## Added
- [X] T366 Remove global semaphore serialization in PhraseService (per-account only) (FR-261).
- [X] T367 Eliminate manual per-command CancellationTokenSource in toggle/upsert (FR-262).
- [X] T368 Tune connection string (MaxPoolSize<=50, KeepAlive=30s) & simplify retry (FR-263).

## Added (previous)
- [X] T358 Implement synchronous phrase database interaction flow (FR-258) ensuring stability under rapid clicks and network latency.
- [X] T359 Add per-account update locks to PhraseService to prevent UI state corruption during database operations (FR-259).
- [X] T360 Enhance PhrasesViewModel to display snapshot state instead of optimistic UI state (FR-260).
- [X] T361 Implement automatic consistency recovery via snapshot refresh when phrase operations fail (FR-260).
- [X] T362 Add UI toggle prevention during active database operations to ensure atomicity (FR-259).
- [X] T327 Harden SettingsWindow module removal (null DataContext guard & ItemsSource fallback) (FR-234).
- [X] T325 Add SpyWindow Crawl Editor "Get Name" button + handler (FR-231) (SpyWindow.xaml / SpyWindow.xaml.cs).
- [X] T326 Add `GetName` custom procedure operation (preset, execution switch, docs) (FR-232).
- [X] T251 Implement patient/study/studyname persistence on New Study (FR-142) via IRadStudyRepository.
- [X] T252 Optimize SpyWindow pick (limit traversal, suppress property exceptions) (FR-143).
- [X] T249 Cache resolved UIA elements in ProcedureExecutor for known controls (FR-140).
- [X] T250 Add persistence stub to insert patient/study/studyname after New Study fetch (FR-141 placeholder).
- [X] T247 Format StudyDateTime in current study label to yyyy-MM-dd HH:mm:ss (FR-138).
- [X] T248 Add placeholder previous studies load hook on New Study (FR-139) – implement once data provider method available.
- [X] T246 Remove heuristic fallbacks from PACS metadata getters (procedure-only FR-137 finalization).
- [X] T245 Wire custom ProcedureExecutor into PacsService getters (FR-137 implementation complete).
- [X] T244 Remove deprecated GetReportConclusion/TryGetReportConclusion PACS methods from UI and spec.
- [X] T243 Implement ProcedureExecutor for data-driven PACS methods (FR-137) and wire future replacement of hard-coded PacsService lookups.
- [X] T242 Implement CurrentStudyLabel population on New Study (FR-136) – add PACS metadata properties & async fetch.
- [X] T241 Refine Split operation preview to output only selected part (remove metadata) (FR-135 update).
- [X] T239 Extend Split operation with Arg3 index argument (code + XAML) (FR-135).
- [X] T240 Update Spec/Plan/Tasks docs for Split Arg3 support (FR-135).
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
- [X] T253 Align patient persistence with schema (use is_male instead of sex column) (FR-144).
- [X] T254 Temporarily disable SpyWindow ancestry tree to improve pick speed (FR-145).
- [X] T255 Persist patient birth_date via RadStudyRepository (FR-146).
- [X] T256 Add SpyWindow tree enable/disable checkbox (FR-147).
- [X] T257 Disable tree by default (FR-148).
- [X] T258 Clear UseIndex flags after Pick (FR-149).
- [X] T259 Add ReportText2 bookmark option (FR-150).
- [X] T260 Add current findings/conclusion getters (primary + variant) (FR-151).
- [X] T261 Implement previous report ingestion on AddStudy (FR-152) with partial JSON + DB persistence + UI refresh.
- [X] T262 Ensure findings/conclusion always retrieved and filter previous studies list to only studies having populated report fields (FR-152a).
- [X] T263 Reversible previous-study Reportified toggle (snapshot originals) (FR-153)
- [X] T264 Swap CurrentStudyLabel to Label (FR-154)
- [X] T265 Add modality extraction + title format `YYYY-MM-DD MOD` (FR-155)
- [X] T266 Enforce uniqueness by (StudyDateTime, Modality) (FR-156)
- [X] T267 PacsService resilient ExecCustom wrapper (FR-157)
- [X] T268 Move previous report toggle into detail bar to fix hit-test overlay (bug root cause for FR-153 usability)
- [X] T269 Replace non-selectable label with read-only selectable TextBox for CurrentStudyLabel (FR-158)
- [X] T270 Guard previous study tabs against self-toggle-off (PreviewMouseDown intercept) (FR-159)
- [X] T271 Implement rad_report UPSERT (UpsertPartialReportAsync) using unique constraint (FR-160)
- [X] T272 Auto-load existing previous studies on New Study (FetchCurrentStudyAsync) (FR-161)
- [X] T273 AddStudy patient match validation + red status message (FR-162)
- [X] T274 Default PreviousReportified ON after AddStudy / NewStudy (FR-163)
- [X] T275 Add Automation (Preview) settings tab skeleton (FR-164)
- [X] T276 Implement baseline-as-reportified logic for previous studies (FR-165)
- [X] T277 Add drag & drop skeleton for automation configuration (FR-166)
- [X] T278 Add debug logging for prev study toggle states (FR-167)
- [X] T279 Add floating drag ghost skeleton in automation tab (FR-168)
- [X] T285 Implement line-by-line dereportify with newline preservation (FR-174)
- [X] T286 Normalize patient IDs and block mismatched AddStudy (FR-175)
- [X] T287 Implement modular New Study procedure + test button (FR-176)
- [X] T288 Persist automation sequences + hook New Study (FR-177)
- [X] T289 Apply dark theme to Settings window (FR-178)
- [X] T290 Implement global retry for PACS UIA getters (FR-179)
- [X] T291 Conditional modular New Study invocation (FR-180)
- [X] T292 Brighten Settings window dark theme text (FR-181)
- [X] T293 Clear automation drop indicator on mouse up (FR-182)
- [X] T294 New Study no-op when empty automation sequence (FR-183)
- [X] T295 SaveAutomation command/button (FR-184)
- [X] T296 Library drop removal logic (FR-185)
- [X] T297 Dark title bar SettingsWindow (FR-186)
- [X] T298 Clear drop indicator + ghost on mouse up (FR-187)
- [X] T299 New Study no-op when automation sequence null/empty (FR-188)
- [X] T300 Neutral gray tab header (FR-189)
- [X] T301 Library drop relocation single-instance (FR-190)
- [X] T302 Allow duplicate automation modules (FR-191)
- [X] T303 Remove button per module instance (FR-192)
- [X] T304 Library modules copy semantics (FR-193)
- [X] T305 Live JSON preview binding (FR-194)
- [X] T306 Findings bound to ReportFindings alias (FR-195)
- [X] T307 Drop indicator clears on leave/invalid (FR-196)
- [X] T308 Library copy / ordered move semantics (FR-197)
- [X] T309 Remove button fix (FR-198)
- [X] T310 Restore Database tab (FR-199)
- [X] T311 Robust drop indicator clearing with debug (FR-200)
- [X] T312 JSON preview findings+conclusion only (FR-201)
- [X] T313 Implement LockStudy module (FR-202)
- [X] T314 Automation executes NewStudy + LockStudy sequence (FR-203)
- [X] T315 Decouple lock from NewStudy (FR-204)
- [X] T316 Auto unreportify on edit (FR-205)
- [X] T317 Raw JSON unaffected by reportify (FR-206)
- [X] T318 Simplified conclusion reportify (FR-207)
- [X] T319 Simplify reportify formatting (FR-208)
- [X] T320 Immediate JSON update on keystroke (FR-209)
- [X] T321 Two-way JSON editor + guarded parsing (FR-210)
- [X] T322 Add StudyRemark mapping (FR-211)
- [X] T323 Alphabetize Spy known controls combo (FR-212)
- [X] T324 PACS method GetCurrentStudyRemark (FR-213)
- [X] T328 Integrate PhrasesWindow into SettingsWindow as Phrases tab (FR-235).
- [X] T329 Harden ToggleActiveAsync against transient SET LOCAL failures (FR-236).
- [X] T330 Adaptive retry/backoff + pool clear for phrase toggle (FR-237).
- [X] T331 Remove SET LOCAL roundtrip in phrase toggle (FR-238).
- [X] T332 Integrate dark theme for Settings Phrases tab & remove standalone PhrasesWindow (FR-239).
- [X] T333 Integrate Spy tab into Settings (FR-240).
- [X] T334 Add DarkTheme.xaml and merge globally (FR-241).
- [X] T335 Populate PACS method ComboBox in Settings Spy tab (FR-242).
- [X] T336 Unify global ComboBox dark template (FR-243).
- [X] T337 Add full-surface click behavior to ComboBox (FR-244).
- [X] T338 Apply global ComboBox template to MainWindow and darken hover (FR-245).
- [X] T339 Apply D2Coding as global font (FR-246).
- [X] T340 Reduce ComboBox font size to 11 (FR-247).
- [X] T341 Replace CurrentStudyLabel TextBox with Label (FR-248).
- [X] T342 DDL radium.reportify_setting (FR-249).
- [X] T343 Implement IReportifySettingsService + ReportifySettingsService (FR-249).
- [X] T344 Integrate Save/Load buttons & commands into SettingsViewModel/Reportify tab (FR-250).
- [X] T345 Add spec/plan/task entries for FR-249..FR-251.
- [X] T346 Add ReportifySettingsJson to ITenantContext/TenantContext (FR-252).
- [X] T347 Auto-load reportify settings on login (SplashLoginViewModel) storing in tenant context (FR-252).
- [X] T348 Remove Load button; SettingsViewModel applies tenant JSON on construct (FR-252).
- [X] T349 Implement config-driven reportify transformations (ApplyReportifyBlock/Conclusion) (FR-253).
- [X] T350 Wire editor toggle to new reportify implementation (FR-253).
- [X] T351 Ensure Save Settings button persists JSON and affects subsequent toggles (FR-253).
- [X] T352 Add AccountIdChanged event to TenantContext (FR-255).
- [X] T353 Guard PhraseService methods for accountId<=0 (FR-254).
- [X] T354 Update PhrasesViewModel to listen for AccountIdChanged and clear/reload (FR-255).
- [X] T355 DI-resolve SettingsViewModel with tenant/reportify/phrases services (FR-256).
- [X] T356 Add IsAccountValid + Save Settings enable binding (FR-256).
- [X] T357 Compose PhrasesViewModel inside SettingsViewModel and fix bindings (FR-257).

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
- [ ] T369 Add unit test verifying first completion item auto-selected on popup open with non-exact match (FR-264).
