# Implementation Plan: Radium Cumulative (Reporting Workflow + Editor + Mapping + PACS)

## Change Log Addition (2025-10-05 - Retry & Drag Indicator)
- Implement UIA retry loop for patient number (FR-169).
- Apply line-by-line dereportify for previous study toggle OFF (FR-170).
- Add drag drop insertion indicator line (FR-171).
- Enforce patient mismatch block with status message (FR-172).
- Auto-select newly added previous study tab (FR-173).

## Change Log Addition (2025-10-02 - Validation & Automation Skeleton)
- Added patient match validation before AddStudy ingestion (FR-162).
- Set default PreviousReportified true after AddStudy and intended default true logic (FR-163).
- Added Automation (Preview) tab in Settings with placeholder checkboxes (FR-164).

## Change Log Addition (2025-10-02 - UX/Upsert)
- Implement selectable current study label using read-only TextBox (FR-158, replaces earlier FR-154 interim label solution).
- Prevent deselection of active previous study tab (guard in `PreviousStudiesStrip`) (FR-159).
- Replace partial report insert with Upsert (ON CONFLICT) for rad_report (FR-160).
- Auto-load prior patient studies with reports after new study metadata fetch (FR-161).

## Change Log Addition (2025-10-02)
- Implement reversible previous-study Reportified toggle with original snapshots (FR-153).
- Replace CurrentStudyLabel TextBlock with Label (interim selectable UI) (FR-154).
- Add modality extraction heuristic and tab title format `YYYY-MM-DD MOD` (FR-155).
- Enforce uniqueness of previous study tabs by (StudyDateTime, Modality) (FR-156).
- Wrap PacsService procedure executions with resilience (try/catch + debug log) (FR-157).

## Change Log Addition (2025-10-05)
- Added UIA element caching in ProcedureExecutor (FR-140) to reduce remapping overhead.
- Added persistence stub after New Study metadata fetch (FR-141) awaiting repository integration.
- Added StudyDateTime normalization to `yyyy-MM-dd HH:mm:ss` in current study label (FR-138).
- Added placeholder hook for loading previous studies on New Study (FR-139 – pending real data service method).
- Removed heuristic fallbacks for PACS metadata getters – now procedure-only (FR-137 finalized). Returns null when no custom procedure saved.
- Wired custom procedure execution into all PACS metadata getters (FR-137 implemented) with fallback to legacy heuristics.
- Added ProcedureExecutor to enable data-driven PACS method execution from saved procedures (FR-137). Deprecated `GetReportConclusion`/`TryGetReportConclusion` removed from UI.
- Current study label metadata fetch implemented (FR-136) – New Study triggers async PACS selection read (name, id, sex, age, studyname, study datetime) stored as properties & concatenated `CurrentStudyLabel` bound to UI.
- Split operation preview refined (FR-135 update) – when Arg3 index provided preview now shows only selected part value (metadata removed). Legacy multi-join preview unchanged when Arg3 absent.
- Split operation extended with Arg3 index (FR-135) – optional numeric index selects single part; legacy multi-part join retained when Arg3 empty.
- AI orchestration skeleton added (Domain interfaces + UseCases ReportPipeline + Infrastructure NoOp skills + DI AddMusmAi extension + API registration). Implements FR-AI-001..FR-AI-008 partial (FR-AI-009/010 future enhancements).
- Adaptive completion popup height auto-sizing implemented (FR-134) – dynamic measurement of first item, exact height for ≤8 items, clamped height with scrollbar for larger sets; re-adjust on selection & rebuild.
- Completion popup bounded height + single-step navigation stabilization implemented (FR-133) – internal navigation index prevents skip-over, ListBox height dynamically constrained to 8 visible items.
- Completion popup navigation recursion fix implemented (FR-132) – added guard flag in MusmCompletionWindow to prevent infinite loops during programmatic selection changes while preserving legitimate keyboard navigation.
- Focus-aware first navigation guard implemented (FR-131) – resets navigation state whenever the completion list rebuilds so the first Down/Up selects the boundary item, and editor now handles all subsequent Up/Down keys directly using guard-silent selection updates so the very next key advances to the adjacent item without duplicate presses or guard clears.
- **2025-10-05**: Implemented patient/study/studyname upsert logic via `IRadStudyRepository` (FR-142).
- **2025-10-05**: Optimized SpyWindow pick (bounded traversal + guarded property access) reducing UIA property exceptions (FR-143).
- **2025-10-05**: Adjusted RadStudyRepository to use is_male column (schema alignment) (FR-144).
- **2025-10-05**: Disabled SpyWindow tree reconstruction to improve pick performance (FR-145).
- **2025-10-05**: Added birth_date capture and persistence if PACS provides birth date (FR-146).
- **2025-10-05**: Added SpyWindow checkbox to enable/disable UI tree (FR-147).
- **2025-10-05**: FR-148 TreeView default disabled (SpyWindow checkbox starts unchecked).
- **2025-10-05**: FR-149 Auto-clear UseIndex after Pick capture.
- **2025-10-05**: FR-150 Added ReportText2 KnownControl + XAML combo item.
- **2025-10-05**: FR-151 Added PACS getters (findings/conclusion variants) + procedure combo entries + PacsService wrappers.
- **2025-10-05**: FR-152 AddStudy now ingests previous report: ensures study, validates banner, fetches findings/conclusion variants, inserts partial rad_report, refreshes PreviousStudies via repository.
- **2025-10-05**: FR-152a Always attempt findings/conclusion retrieval and filter PreviousStudies to only those with reports containing either field.
- **2025-10-02**: Treat previous study original fields as baseline reportified content; dereportify only when toggle off (FR-165).
- **2025-10-02**: Add drag & drop skeleton lists (Library/New Study/Add Study) in settings automation tab (FR-166).
- **2025-10-02**: Add debug output for previous study toggle (FR-167).
- **2025-10-02**: Introduce floating drag ghost skeleton for automation procedure items (FR-168).
- Added line-by-line previous study dereportify preserving newline tokens (FR-174).
- Enforced normalized patient number comparison (strip non-alphanum, uppercase) blocking AddStudy on mismatch (FR-175).
- Added modular `INewStudyProcedure` with implementation `NewStudyProcedure` and temporary test button (FR-176).
- Persist automation module sequences in local settings and use for New Study invocation (FR-177).
- Dark mode styling applied to SettingsWindow (FR-178).
- Added retry wrapper for all PACS procedure getters (FR-179).
- Adjusted New Study command to check configured sequence before procedure invocation (FR-180).
- Brightened SettingsWindow foreground / dark theme unified (FR-181).
- Drop indicator now cleared on mouse up (FR-182).
- New Study no-op when automation list saved but empty (FR-183).
- Added Save Automation command/button (FR-184).
- Library drop removes from other lists and ensures single instance (FR-185).
- Dark title bar for SettingsWindow (FR-186).
- Drop indicator + ghost cleared on mouse up (FR-187).
- New Study explicit no-op when automation list null/empty (FR-188).
- Neutral gray tab header foreground applied (FR-189).
- Library drop logic enforces single-instance relocation (FR-190).
- Enabled duplicate module insertion in automation panes (FR-191).
- Added remove (X) button per module instance (FR-192).
- Library now persistent source; drag copies not moves (FR-193).
- Simplified live JSON to findings + conclusion only (FR-201).
- Implemented `ILockStudyProcedure` + `LockStudyProcedure` (FR-202).
- Updated automation to process NewStudy and LockStudy modules sequentially (FR-203).
- Removed locking from NewStudyProcedure (FR-204).
- Implemented auto-unreportify on edit (FR-205).
- Raw-only JSON generation (findings/conclusion) regardless of reportify (FR-206).
- Simplified conclusion reportify logic (FR-207).
- Simplified reportify to per-line capitalization + trailing period (FR-208).
- Raw capture moved before JSON update eliminating off-by-one lag (FR-209).
- Implement two-way JSON editing with guarded synchronization (FR-210).
- Added KnownControl.StudyRemark and SpyWindow combo alphabetized (FR-211, FR-212).
- Added PACS custom getter GetCurrentStudyRemark (FR-213).

(Update: account_id migration + phrase snapshot + OCR additions + completion improvements + bug fixes + selection guard fixes + multiple event handling + navigation state tracking + focus-aware first navigation guard + manual editor navigation handling + guard-silent selection updates + recursive guard protection)

## AI Architecture Overview (New Section)
Layer Responsibilities:
1. Domain (Wysg.Musm.Domain.AI)
   - Pure contracts & record types (provider agnostic) : ILLMClient, IModelRouter, skill interfaces, ReportState, context records.
2. UseCases (Wysg.Musm.UseCases.AI)
   - Orchestrator (ReportPipeline) composing skill interfaces; no vendor code.
3. Infrastructure (Wysg.Musm.Infrastructure.AI)
   - Adapters (future: OpenAI/Ollama), routing, telemetry, prompt templates. Currently: NoOp implementations for development.
4. API Host (Wysg.Musm.Api)
   - Registers AddMusmAi(); future Minimal API endpoints expose structured skills (e.g., POST /api/report/current-study-intake, POST /api/report/postprocess). No direct raw prompt endpoint.

Dependency Direction: UI/Clients -> API (HTTP) -> UseCases -> Domain; Infrastructure implements Domain & is registered only at host boundary.

Extensibility Path:
- Add real provider: implement ILLMClient + skill classes; adjust AddMusmAi(useNoOp:false) + config binding.
- Add new skill: define interface in Domain, implement Infrastructure adapter, extend ReportPipeline or new orchestrator.

Fallback Strategy:
- NoOp skills return empty strings/unchanged text enabling UI operation without blocking (graceful degradation while server offline).

Telemetry Plan:
- IInferenceTelemetry future implementation logs structured event {skill, model, ms, success, tokens} (Serilog sink + optional metrics).

Next Steps (AI):
- Define JSON contract validation & retry guard.
- Implement HeaderSplitter, HeaderParser, Proofreader (real) + routing heuristics.
- Add error isolation (try/catch around each stage) fulfilling FR-AI-009.
- Add configuration section: "Ai:Provider:OpenAI" etc.

# Implementation Plan: Previous Study Multi-Report Selection (FR-214..FR-218) + Reportify Settings Skeleton (FR-219..FR-222)

Branch: [radium-cumulative] | Date: 2025-10-02 | Spec: ./Spec.md

## Summary (updated)
Add support for multiple report rows per previous study. UI: ComboBox (dark, compact) bound to SelectedPreviousStudy.Reports. On selection change swap Findings/Conclusion and honor reportified toggle. Repository now returns studyname, report_datetime, created_by.

## Technical Context
Language/Version: C# 13 / .NET 9 (WPF desktop)
Primary Dependencies: WPF, Npgsql, FlaUI (unchanged)
Storage: Local Postgres (rad_report, rad_study, rad_studyname)
Testing: Manual exploratory (no automated tests yet for UI)
Target Platform: Windows 10+
Project Type: Desktop WPF client (apps/Wysg.Musm.Radium)
Performance Goals: Load previous studies < 200ms typical
Constraints: Avoid breaking existing PreviousStudies UX (tab uniqueness by StudyDateTime+Modality)
Scale/Scope: Dozens of reports per patient worst-case (small in-memory collections)

## Constitution Check
No new architectural layers added. Minor viewmodel extension + repository query widening. Pass.

## Project Structure Impact
Touched files:
- Services/RadStudyRepository.cs (query columns, DTO)
- ViewModels/MainViewModel.cs (models PreviousReportChoice, binding logic)
- Views/MainWindow.xaml (ComboBox styling & binding)
- docs/Spec.md (new FRs)
- docs/plan.md (this)
- docs/tasks.md (added below)

## Phase 0 Research
N/A (straightforward data enrichment + UI binding) – no unresolved clarifications.

## Phase 1 Design
Entities:
PreviousReportChoice { Studyname, ReportDateTime, CreatedBy, Findings, Conclusion }
Augment PatientReportRow to include Studyname, ReportDateTime, CreatedBy.
Grouping logic groups rows by Study (StudyId, StudyDateTime, Studyname) then orders reports by ReportDateTime desc.

State Transitions:
1. LoadPreviousStudiesForPatientAsync -> build groups -> create PreviousStudyTab.Reports
2. Selecting tab sets SelectedPreviousStudy
3. Selecting report sets SelectedPreviousStudy.SelectedReport -> ApplyReportSelection -> updates OriginalFindings/Conclusion + visible text (with reportified transform reapplied by toggle method)

Error Handling:
- JSON parse failures logged, skipped
- Null/empty report fields default to header_and_findings

## Phase 2 Task Generation Approach
Tasks derived (see tasks.md): repository change, VM model additions, XAML ComboBox, style, spec/docs update, manual validation.

## Complexity Tracking
None.

## Progress Tracking
Phase Status:
- [x] Phase 0: Research complete
- [x] Phase 1: Design complete
- [x] Phase 2: Task planning described
- [ ] Phase 3: Tasks executed
- [ ] Phase 4: Implementation complete (in progress)
- [ ] Phase 5: Validation

Gate Status:
- [x] Initial Constitution Check: PASS
- [x] Post-Design Constitution Check: PASS
- [x] All NEEDS CLARIFICATION resolved
- [x] No complexity deviations

## Summary (FR-219..FR-222)
Added non-functional Reportify settings skeleton tab (FR-219..FR-222) with four option checkboxes and three default value text boxes plus explanatory note. No persistence yet.

## Design Addition
Reportify tab UI only; future persistence will map to a settings model (e.g., IRadiumLocalSettings extensions) controlling reportify pipeline.

Controls:
- CheckBoxes: removeExcessiveBlanks, removeBlankLines, capitalizeSentence, ensureTrailingPeriod
- TextBoxes: defaultArrow (-->), defaultConclusionNumbering (1.), defaultDetailingPrefix (-)

## Risks
Minimal: purely declarative XAML; no code-behind changes.

## Next Steps (future implementation not in this iteration)
- Extend settings model + migration
- Bind controls to SettingsViewModel properties
- Apply options in reportify logic (MainViewModel methods SimpleReportifyBlock / ReportifyConclusion)

## Tasks Updated
Added T017-T020 (see tasks.md) for skeleton inclusion & spec/plan alignment.

