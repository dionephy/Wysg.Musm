# Implementation Plan: Radium Cumulative (Reporting Workflow + Editor + Mapping + PACS)

## Change Log Addition (2025-10-07 - Phrase Toggle Throughput Optimization)
- Added FR-261..FR-263: Removed global serialization semaphore in `PhraseService` (now per-account locking only) reducing contention on Supabase free tier. Eliminated aggressive per-command CancellationTokenSource usage causing early OperationCanceledException. Capped MaxPoolSize to 50 and relaxed KeepAlive to 30s. Toggle/upsert now single OPEN + UPDATE/INSERT with CommandTimeout=30 (default) and lightweight retry only on genuine transient timeouts. UI already strict synchronous (FR-258..260); this patch prevents self-inflicted cancellations under pool pressure.

## Change Log Addition (2025-10-05 - Phrase Database Stability)
- Implemented synchronous phrase interaction flow (FR-258, FR-259, FR-260) to ensure stability under rapid user clicks and network latency. PhraseService now uses strict synchronous flow: user action → database update → snapshot update → UI display from snapshot. Added per-account update locks to prevent UI state corruption and ensure all operations complete atomically before allowing new requests.

## Change Log Addition (2025-10-05 - Settings Automation Remove NullRef Fix)
- Hardened SettingsWindow automation list helpers against null DataContext / unexpected list names (FR-234). Added guarded GetListForListBox with ItemsSource fallback + debug logging. Prevents NullReferenceException when clicking module remove (X) before DataContext fully initialized or during transient re-template.

## Change Log Addition (2025-10-04 - Get Name element support)
- Implement SpyWindow Crawl Editor "Get Name" button (FR-231) calling new handler OnGetName to resolve last element and display only UIA Name.
- Extend Custom Procedures operations list with `GetName` (FR-232) sharing presets with GetText (Arg1=Element, others disabled) but storing only the Name property (no fallback chain) and previewing `(empty)` when blank.
- Update ExecuteSingle switch to handle `GetName` and OnProcOpChanged presets.
- Added documentation entries & tasks (T325, T326).

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

(Update: account_id migration + phrase snapshot + OCR additions + completion improvements + bug fixes + selection guard fixes + multiple event handling + navigation state tracking + focus-aware first navigation guard + manual editor navigation handling + guard-silent selection updates + recursive guard protection + phrase database stability)

## Change Log Addition (2025-10-07 - Completion First Item Auto-Select)
- Implement MusmCompletionWindow.EnsureFirstItemSelected invoked on popup show and when no exact match (FR-264). Added selection permit flag reuse to avoid guard clearing. Update tests pending (add new task T369).

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

# Implementation Plan: Previous Study Multi-Report + Reportify Settings (FR-214..FR-230)

Branch: [radium-cumulative] | Date: 2025-10-02 | Spec: ./Spec.md

## Update Summary (FR-230)
Restored option checkboxes alongside sample buttons within each Reportify group so users can preview and toggle in same context.

## Design Change
- Each Expander now contains a StackPanel with top WrapPanel (sample buttons) and second WrapPanel (checkboxes).
- JSON still updates when checkboxes or text inputs change (existing bindings unaffected).

## Risk
Minimal—pure XAML adjustment; bindings reused.

## Tasks Added
T031 added (see tasks.md).

## Change Log Addition (2025-10-05 - Integrated Phrases Tab)
- Added Phrases tab inside SettingsWindow (FR-235) migrating UI from standalone PhrasesWindow (DataGrid + Add/Activate + Refresh). Injects PhrasesViewModel lazily on tab load to avoid impacting settings load path. Standalone window can be deprecated in future cleanup.

## Change Log Addition (2025-10-05 - Phrase Toggle Resilience)
- Hardened PhraseService.ToggleActiveAsync to ignore transient failures executing SET LOCAL and added retry path (FR-236). Prevents NpgsqlException (Timeout during reading attempt) surfacing when user unchecks Active in phrases grid.

## Change Log Addition (2025-10-05 - Phrase Toggle Adaptive Retry)
- Implemented adaptive retry/backoff + pool clear for PhraseService.ToggleActiveAsync; resilient SET LOCAL handling for upsert & toggle (FR-237).

## Change Log Addition (2025-10-05 - Phrase Toggle Roundtrip Removal)
- Removed SET LOCAL pre-command in ToggleActiveAsync; rely solely on UPDATE + tighter timeouts (FR-238) to mitigate stream read timeouts on rapid toggles.

## Change Log Addition (2025-10-05 - Integrated Phrases Dark Theme)
- Applied dark styling to SettingsWindow Phrases tab (DataGrid + controls) and removed legacy standalone PhrasesWindow + Manage Phrases button (FR-239).

## Change Log Addition (2025-10-05 - Integrated Spy Tab)
- Added Spy tab to Settings consolidating UI spy functionality (process pick, mapping, chain editor, basic procedure ops) (FR-240).

## Plan Addition (2025-10-05 - FR-241 / FR-242)
- FR-241: Create Themes/DarkTheme.xaml consolidating dark palette + implicit control styles; merge into App.xaml. Remove reliance on scattered per-window styles progressively (initial application to Settings Spy tab).
- FR-242: Mirror SpyWindow PACS procedure methods in integrated Spy tab (adds 21 ComboBoxItem entries) enabling immediate procedure selection without opening standalone window.

## Plan Addition (2025-10-05 - FR-243)
- FR-243: Replace minimal implicit ComboBox style with full template (border, toggle glyph, popup) applied globally so integrated Spy tab inherits identical dark appearance without per-window style duplication.

## Plan Addition (2025-10-05 - FR-244)
- FR-244: Enhance ComboBox template by inserting full-surface transparent ToggleButton over content (excluding arrow cell) to toggle IsDropDownOpen, ensuring click on label opens list without needing small arrow target.

## Plan Addition (2025-10-05 - FR-245)
- FR-245: Retire per-window DarkMiniCombo usage; rely on global template. Modify hover visuals to be slightly darker (PanelAlt + AccentAlt border) to reduce visual noise.

## Plan Addition (2025-10-05 - FR-246 / FR-247 / FR-248)
- FR-246: Introduce Dark.MonoFont resource referencing packaged D2Coding; set as implicit Window FontFamily.
- FR-247: Adjust implicit ComboBox style (font size 11) for denser layout; rely on existing global template for click-anywhere behavior.
- FR-248: Swap TextBox -> Label for CurrentStudyLabel to avoid user confusion about editability and reduce visual chrome.

## Plan Addition (2025-10-05 - FR-249..FR-251)
- Add radium.reportify_setting table (account-scoped singleton) storing JSON & rev.
- Implement IReportifySettingsService (Get, Upsert, Delete) using central data source provider.
- Extend SettingsViewModel with Save/Load commands guarded by presence of tenant/account id & service.
- Upsert increments rev server-side via UPDATE expression; client does not diff JSON (server revision bump acceptable per write request).
- Add UI buttons to Reportify tab for Load/Save; disabled only when service unavailable.

## Plan Addition (2025-10-05 - FR-252)
- On login (silent or interactive) after EnsureAccountAsync succeeds: resolve IReportifySettingsService, fetch JSON, store in ITenantContext.ReportifySettingsJson.
- SettingsViewModel constructor checks tenant context for existing JSON and applies it (ApplyReportifyJson) removing need for manual Load.
- Reportify tab buttons reduced to Save + Close.

## Plan Addition (2025-10-05 - FR-253)
- Implement ReportifyConfig loader parsing tenant ReportifySettingsJson once per change.
- Integrate ApplyReportifyBlock / ApplyReportifyConclusion using regex passes per line with conditional steps keyed off config flags.
- Conclusion paragraph numbering executed pre line splitting; indentation applied post transformations for continuation lines.
- Editor toggle now calls ApplyReportifyBlock / ApplyReportifyConclusion replacing SimpleReportifyBlock usage.
- Config reloaded automatically when tenant.ReportifySettingsJson differs from last applied before next transformation.

## Plan Addition (2025-10-05 - FR-254, FR-255)
- Add AccountIdChanged event to ITenantContext/TenantContext (invoke when TenantId changes).
- Modify PhraseService methods to early-return empty (or throw for mutation) when accountId <= 0.
- Update PhrasesViewModel: subscribe to AccountIdChanged; clear Items on logout; trigger RefreshAsync on first valid >0 id.
- Remove legacy fallback adoption logic where possible (retain ResolveAccountIdAsync only for backward compatibility; new guards prefer tenant.AccountId).
- Update Spec/Tasks docs accordingly.

## Plan Addition (2025-10-05 - FR-256, FR-257)
- Replace parameterless SettingsWindow DataContext with DI-resolved SettingsViewModel (inject tenant, reportify svc, phrases VM).
- Add IsAccountValid property and bind Save Settings button IsEnabled.
- Inject PhrasesViewModel into SettingsViewModel (composition) to eliminate binding errors.
- Bind phrases tab root DataContext to SettingsViewModel.Phrases; fall back to service resolve if null.

## Plan Addition (2025-10-05 - FR-258, FR-259, FR-260)
- Implement synchronous phrase database interaction flow to ensure stability under rapid user clicks and network latency.
- Add per-account state update locks in PhraseService to prevent UI state corruption during database operations.
- Enhance PhrasesViewModel to wait for database completion and display snapshot state (never optimistic UI state).
- Add automatic consistency recovery via snapshot refresh when any phrase operation fails.
- Implement UI toggle prevention during active database operations to ensure atomicity and final consistency.

(Update: account_id migration + phrase snapshot + OCR additions + completion improvements + bug fixes + selection guard fixes + multiple event handling + navigation state tracking + focus-aware first navigation guard + manual editor navigation handling + guard-silent selection updates + recursive guard protection + phrase database stability)

