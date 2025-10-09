# Implementation Plan: Radium Cumulative (Reporting Workflow + Editor + Mapping + PACS)

## Change Log Addition (2025-01-10 - Snippets Tab in Settings)
- Added Snippets management tab in SettingsWindow mirroring Hotkeys with extra AST field and sample snippet guidance (FR-319, FR-320).
- Registered `ISnippetService` with AzureSqlSnippetService and added `SnippetsViewModel` to DI container (FR-321).
- Implemented snapshot-based snippet CRUD path (Preload, GetAllSnippetMetaAsync, Upsert, Toggle, Delete, Refresh) consistent with hotkeys/phrases (FR-315..FR-316).

## Change Log Addition (2025-01-10 - Snippet Feature with Placeholders)
- Implemented snippet expansion feature with placeholder navigation for structured text templates (FR-311).
- Added `radium.snippet` table in central database with columns: snippet_id, account_id, trigger_text, snippet_text, snippet_ast, description, is_active, lifecycle fields (FR-312).
- snippet_text stores template with placeholder syntax; snippet_ast stores pre-parsed JSON for fast runtime processing (FR-313, FR-314).
- Implemented `ISnippetService` with methods: GetActiveSnippetsAsync, UpsertSnippetAsync, ToggleActiveAsync, DeleteSnippetAsync (FR-315).
- Added snippet snapshot caching parallel to phrase/hotkey caching for performance (FR-316).
- Integrated snippet expansion into EditorControl with placeholder parsing, insertion, and tab navigation (Tab/Shift+Tab between placeholders) (FR-317).
- Added Snippets management tab in SettingsWindow with CRUD operations, AST validation/preview, and dark theme styling (FR-318).

## Change Log Addition (2025-01-10 - Auto-Build snippet_ast on Insert)
- Added `SnippetAstBuilder` to parse `snippet_text` into JSON AST (modes 0/1/2/3 with joiner/bilateral in mode 2). Integrated into `SnippetsViewModel.AddAsync` so AST is computed and persisted automatically (FR-322..FR-324).

## Change Log Addition (2025-01-09 - Hotkey Feature)
- Implemented hotkey expansion feature allowing users to define text shortcuts that expand inline during typing (FR-286).
- Added `radium.hotkey` table in central database (account_id, trigger_text, expansion_text, is_active, lifecycle fields) (FR-287).
- Implemented `IHotkeyService` with methods: GetActiveHotkeysAsync, UpsertHotkeyAsync, ToggleActiveAsync, DeleteHotkeyAsync (FR-288).
- Added hotkey snapshot caching parallel to phrase caching for performance (FR-289).
- Integrated hotkey expansion into EditorControl text input handling with prefix matching and inline replacement (FR-290).
- Added Hotkeys management tab in SettingsWindow with CRUD operations and dark theme styling (FR-291).

## Change Log Addition (2025-10-09 - Phrases tab shows account + global)
- PhrasesViewModel now loads both account-scoped and global phrases and displays them in a single list (account items first, then global). Each `PhraseRow` contains `AccountId` (null=global) and toggles call `IPhraseService.ToggleActiveAsync(AccountId?, Id)`. Add command still inserts into the current account. Completion already uses combined phrases.
- Added `IsGlobal` boolean property to `PhrasesViewModel.PhraseRow` and bound it to a new read-only `Global` DataGridCheckBoxColumn in Settings → Phrases tab (FR-284).

## Change Log Addition (2025-10-09 - Convert-to-global immediate reflection)
- GlobalPhrasesViewModel refreshes both lists after conversion and clears phrase caches for account and global. Backends (Postgres and AzureSQL) already clear caches and update snapshots during conversion; UI now awaits conversions and calls `RefreshPhrasesAsync()` and reloads AccountPhrases list, so the converted item disappears from AccountPhrases and appears in Global list immediately.
- Added `SelectAllAccountPhrasesCommand` and a "Select All" button in the Account Phrase Conversion section of the Global Phrases tab to quickly mark all rows for conversion (FR-285).

## Change Log Addition (2025-10-09 - Global Phrases UI Corrections)
- Removed manual account filter controls from Settings → Global Phrases tab: "Load phrases from account:", its textbox, and the "Load Account Phrases" button. The Account Phrases list now defaults to a cross-account view using `IPhraseService.GetAllNonGlobalPhraseMetaAsync` (FR-280).
- Added a read-only `AccountId` column to the Account Phrases grid for visibility of source account (FR-282).
- Enabled two-way checkbox editing for the `Select` column by making the Account Phrases DataGrid editable (IsReadOnly=False) while keeping data columns read-only, ensuring selection state binds to `AccountPhraseItem.IsSelected` and Convert Selected works reliably (FR-279, FR-282).
- Convert Selected to Global now operates on checked rows across mixed accounts; the VM groups by AccountId when `_sourceAccountId` is null to call `ConvertToGlobalPhrasesAsync` per account and applies duplicate purge across all accounts per FR-281.

## Change Log Addition (2025-10-09 - Global Phrase Conversion UI)
- Implemented Settings → Global Phrases tab enabling administrators to convert account-scoped phrases to global (account_id = NULL) (FR-279).
- UI features:
  - Add new global phrase input + button (Enter submits).
  - Default listing now shows ALL non-global phrases across accounts (no account id required) with ability to convert mixed-account selections (FR-280).
  - Select multiple account phrases and convert to global; duplicates removed across all accounts for the same text; exactly one global remains (FR-279, FR-281).
  - Status bar shows progress and results; Refresh button reloads global list.
  - Tab visible only when AccountId == 1 (admin guard) pending role model.
- Backend/service changes:
  - Added `GetAllNonGlobalPhraseMetaAsync(int take)` to `IPhraseService` and implemented in Postgres and Azure SQL implementations to support default cross-account listing (FR-280).
  - `ConvertToGlobalPhrasesAsync` now deletes duplicates across ALL accounts for the same text after converting one to global, or when a global already exists (FR-279, FR-281).
  - `UpsertPhraseAsync(accountId: null, text)` includes failsafe purge: delete all non-global rows with the same text across any account (FR-281).
  - Snapshots and caches updated for both account and global scopes; UI reflects refreshed state.

## Change Log Addition (2025-01-08 - Global Phrases Support)
- Implemented global phrases feature allowing phrases with NULL account_id to be shared across all accounts (FR-273).
- Added three query modes: account-only, global-only, and combined (deduped) phrase lists (FR-274).
- Applied synchronous database flow to global phrase operations ensuring consistency (FR-275).
- Modified PhraseService.UpsertPhraseAsync and ToggleActiveAsync to accept nullable account_id (FR-276).
- Implemented merge logic for combined queries with account-specific precedence (FR-277).
- Created database migration with filtered unique indexes for global and per-account uniqueness (FR-278).

## Change Log Addition (2025-01-08 - Reportified Text Change Cancellation)
- Modified MainViewModel.Editor.cs property setters for HeaderText, FindingsText, ConclusionText, and ReportFindings to cancel text changes when reportified state is ON (FR-270, FR-271, FR-272). When user attempts edit in reportified state, the change is not applied and the editor automatically toggles to dereportified state, preserving the current reportified text. User must then make their edit again in the dereportified state. Implementation uses early return after calling AutoUnreportifyOnEdit() when _reportified && !_suppressAutoToggle condition is met.

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
- Wired custom procedure execution into all PACS metadata getters (FR-137). Deprecated `GetReportConclusion`/`TryGetReportConclusion` removed from UI.
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

## Change Log Addition (2025-10-09 - Hotkey Description for Completion)
- Added radium.hotkey.description NVARCHAR(256) with migration script (backfills from first line of expansion).
- Updated AzureSqlHotkeyService and HotkeyInfo to carry Description; snapshots hydrated from DB.
- Completion display changed to "{trigger} → {description}" (fallback to first line) in CompositeProvider.

## Change Log Addition (2025-10-09 - Hotkeys UI Description)
- Added Description textbox to Hotkeys add section; persisted via Upsert with new optional parameter.
- Added Description column to Hotkeys DataGrid for visibility (read-only).

