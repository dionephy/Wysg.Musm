# Feature Specification: Radium Cumulative – Reporting Workflow, Editor Experience, PACS & Studyname→LOINC Mapping

## Update: Hotkey Text Expansion (2025-01-09)
- **FR-286** System MUST support user-defined hotkeys (text shortcuts) that expand inline when trigger text is typed in editors, enabling rapid insertion of frequently-used phrases or templates without completion popup.
- **FR-287** Hotkey storage MUST use central database table `radium.hotkey` with columns: hotkey_id (BIGINT IDENTITY PK), account_id (BIGINT FK NOT NULL), trigger_text (NVARCHAR(64) NOT NULL), expansion_text (NVARCHAR(4000) NOT NULL), is_active (BIT NOT NULL DEFAULT 1), created_at, updated_at, rev. Unique constraint on (account_id, trigger_text).
- **FR-288** Hotkey service MUST provide async methods: GetActiveHotkeysAsync(accountId), UpsertHotkeyAsync(accountId, trigger, expansion), ToggleActiveAsync(accountId, hotkeyId), DeleteHotkeyAsync(accountId, hotkeyId). All operations MUST follow synchronous database → snapshot → UI flow similar to phrase service.
- **FR-289** Hotkey service MUST implement in-memory snapshot caching per account with automatic invalidation on upsert/toggle/delete and provide GetHotkeySnapshotAsync for completion integration.
- **FR-290** Editor text input handling MUST detect hotkey triggers by matching typed word against active hotkey trigger_text (case-insensitive prefix or exact match based on configuration) and replace trigger with expansion_text inline when space or punctuation follows. Hotkey expansion MUST take precedence over phrase completion popup.
- **FR-291** Settings window MUST include Hotkeys management tab with: DataGrid listing (Id, Trigger, Expansion preview, Active, Updated, Rev); Add controls (Trigger input, Expansion multi-line input, Add button); Refresh button; Delete button for selected row; Active checkbox toggle per row; Dark theme styling consistent with Phrases tab.

## Update: Phrases tab shows account + global (2025-10-09)
- FR-283 Phrases tab MUST display both current account phrases and global phrases in a single list. Each row retains its scope via `account_id` (NULL = global). Toggling Active MUST call the appropriate scope-aware API (`ToggleActiveAsync(accountId? : null, id)`). Adding a new phrase from this tab MUST create an account-scoped phrase for the current account. Completion window MUST use combined phrases (global + account) for suggestions.
- FR-284 Phrases tab MUST include a read-only column `Global` showing a boolean derived from `account_id IS NULL` for quick scope recognition.

## Update: Global Phrase Conversion (2025-10-09)
- **FR-279** Global phrases admin MUST be able to convert selected account-specific phrases into global phrases from Settings → Global Phrases tab.
  - When converting multiple phrases with identical text from the same account, exactly one global row MUST remain (account_id = NULL) and the rest MUST be deleted.
  - When a global phrase with the same text already exists, selected account phrases with that text MUST be deleted (deduplicated) and no additional global rows created.
  - Operation MUST be synchronous and update in-memory snapshots and caches for both the account scope and the global scope on success; UI MUST reflect final snapshot state.
  - Conversion MUST be guarded by per-account lock and global lock to avoid races; UI displays converted and duplicates removed counts.
  - UI MUST allow:
    - Adding a new global phrase
    - Multi-selecting account phrases via checkboxes and invoking Convert Selected to Global
    - Toggling Active on existing global phrases
  - Visibility: Tab is visible when AccountId == 1 (temporary admin guard until role model lands).
- **FR-280** Global phrases admin tab MUST list all non-global phrases (account_id IS NOT NULL) across all accounts by default when opened; the search box is optional and when left empty the grid shows a cross-account list (latest updated first), enabling mixed-account conversion in a single action.
- **FR-281** Failsafe duplicate purge: whenever a phrase becomes or already exists as global (account_id IS NULL), all non-global duplicates with the same text across all accounts MUST be removed (DELETE), ensuring a single authoritative global row remains. This applies to both ConvertToGlobal and UpsertPhrase with account_id = NULL.
- **FR-282** Global Phrases tab MUST NOT contain manual account filter inputs; instead, the Account Phrases grid MUST include a read-only `account_id` column and an editable `Select` checkbox column bound two-way to enable conversion selection. The Convert button MUST operate on all checked rows across mixed accounts.
- **FR-285** Global Phrases tab MUST expose a `Select All` action that marks all rows in the Account Phrases grid as selected, enabling quick bulk conversion.

## Update: Global Phrases Support (2025-01-08)
- **FR-273** System MUST support global phrases (account_id IS NULL) that are available to all accounts without requiring account ownership.
- **FR-274** Phrase queries MUST support three modes: account-only, global-only, and combined (global + account-specific with deduplication).
- **FR-275** Global phrase management MUST follow the same synchronous database flow as account phrases (FR-258..FR-260) ensuring consistency.
- **FR-276** Phrase upsert and toggle operations MUST accept nullable account_id where NULL indicates a global phrase accessible to all accounts.
- **FR-277** Combined phrase queries MUST merge global and account-specific phrases with account-specific taking precedence in case of text conflicts.
- **FR-278** Database schema MUST use filtered unique indexes to enforce uniqueness separately for global phrases (account_id IS NULL) and per-account phrases (account_id IS NOT NULL).

## Update: Phrase Toggle Throughput Optimization (2025-10-07)
- **FR-261** Phrase mutation pipeline MUST avoid cross-account global serialization; only per-account locking is permitted to reduce artificial contention.
- **FR-262** Phrase toggle/upsert operations MUST NOT apply additional short client-side cancellation tokens (manual CTS) that preempt server execution; rely on CommandTimeout only for cancellation.
- **FR-263** Phrase service connection strategy MUST cap pool size (≤50) and reuse natural pooling without excessive keepalive frequency (<30s) to reduce timeouts on constrained (free tier) backends.

## Update: Phrase Database Interaction Stability (2025-10-05)
- **FR-258** Phrase change operations (toggle active, add phrase) MUST follow strict synchronous flow: user action → synchronously update database → synchronously update in-memory snapshot → display snapshot state (never display database state directly).
- **FR-259** Phrase toggle operations MUST prevent UI state corruption by blocking additional toggle requests during active toggle processing and ensuring all UI updates reflect final snapshot state.
- **FR-260** Phrase add operations MUST complete database upsert and snapshot update before displaying new phrase in UI, with automatic consistency recovery via snapshot refresh on any operation failure.

## Update: Automation list deletion robustness (2025-10-05)
- **FR-234** Settings Automation tab MUST allow removing a module instance via X button without throwing even if DataContext rebinds or control template re-applies; method MUST gracefully fallback to ListBox.ItemsSource when underlying ViewModel not yet available and ignore unknown list names.

## Update: Get Name element support (2025-10-04)
- **FR-231** SpyWindow Crawl Editor MUST provide a "Get Name" button next to "Get Text" which resolves the currently validated element and outputs only its UIA Name property (no Value/Legacy fallback).
- **FR-232** Custom Procedures MUST include a `GetName` operation (Arg1: Element, others disabled) storing the element's Name (empty when null) into the output variable; preview shows `(empty)` when Name blank and `(no element)` when target cannot be resolved.

## Update: Reportify sample + checkbox coexistence (2025-10-02)
- **FR-230** Reportify tab MUST display sample buttons and option checkboxes together inside each option group.

## Update: Reportify sample preview (2025-10-02)
- **FR-227** Reportify tab MUST provide buttons (one per option group) that when clicked populate a Sample Before textbox with a predefined raw snippet and Sample After textbox with the transformed example for that single option.
- **FR-228** Reportify tab MUST expose two multi-line text boxes `Sample Before` and `Sample After` plus existing JSON preview simultaneously (three-column layout on wide screens).
- **FR-229** Sample buttons MUST not mutate the underlying JSON settings (pure preview only).

## Update: Reportify settings enhancement (2025-10-02)
- **FR-223** Reportify settings tab MUST expose additional non-functional placeholder options: normalize arrows, normalize bullets, space after punctuation, normalize parentheses, space number-unit, collapse whitespace, number conclusion paragraphs, indent continuation lines, remove duplicate lines, preserve known tokens.
- **FR-224** Changing any Reportify settings checkbox or default text field MUST update a read-only JSON preview textbox instantly (no button required).
- **FR-225** Reportify JSON preview MUST include a nested `defaults` object for arrow, conclusion numbering, and detailing prefix; field names use snake_case.
- **FR-226** All Reportify tab checkboxes MUST be clearly visible in dark mode using light foreground (#E0E0E0 baseline).

## Update: Reportify settings skeleton (2025-10-02)
- **FR-219** Settings window MUST include a "Reportify" tab (skeleton) containing configurable placeholders for reportify behavior (non-functional initially).
- **FR-220** Reportify tab MUST list four option checkboxes: remove excessive blanks; remove excessive blank lines; capitalize first letter of sentence; add period at end of sentence.
- **FR-221** Reportify tab MUST include three text inputs with labels: Default arrow, Default conclusion numbering, Default detailing prefix (pre-filled example values). 
- **FR-222** Reportify tab MUST display a note indicating the tab is non-functional (skeleton) until implementation phase.

## Update: Previous study multi-report selection (2025-10-02)
- **FR-214** Previous study tab MUST support multiple report rows per study; a ComboBox lists all reports for the selected study ordered by report_datetime DESC (nulls last) defaulting to the most recent.
- **FR-215** Previous study report selector ComboBox item display MUST be formatted "{Studyname} ({study datetime}) - {report datetime} by {created_by}" with (no report dt) when null.
- **FR-216** Selecting a different previous report MUST immediately swap Findings & Conclusion (and reapply reportified/dereportified transformation according to toggle state) without altering other tabs.
- **FR-217** Previous report selector MUST use dark theme styling consistent with other dark controls and a compact 11px monospace font.
- **FR-218** Previous report selector MUST provide a disabled dummy first item (design-time sizing sentinel) when bound list is empty to guarantee consistent measured width; real items render after the dummy.

## Update: Previous study baseline & automation drag skeleton (2025-10-02)
- **FR-165** Previous study initial text MUST be treated as already reportified baseline; toggle OFF performs dereportify transform shown in editors; toggle ON restores original baseline text without reprocessing.
- **FR-166** Automation settings MUST present two reorderable lists (New Study, Add Study) and one library list via drag & drop skeleton; duplicates prevented per pane (preview, non-persistent).

## Update: Add Study validation & automation settings skeleton (2025-10-02)
- **FR-162** AddStudy MUST validate that selected related study patient number/id matches current patient; on mismatch no ingestion occurs and a status message in red is shown.
- **FR-163** Previous report Reportified toggle MUST default to ON when application starts a new study and after each successful AddStudy ingestion.
- **FR-164** Settings window MUST expose an "Automation (Preview)" tab with placeholder checkboxes for configuring actions on New Study and Add Study (non-functional skeleton).

## Update: Previous studies UX + Report Upsert (2025-10-02)
- **FR-153** Previous report Reportified toggle MUST apply reversible formatting using preserved original previous study text (no cumulative transformation loss).
- **FR-154** Current study label visual element changed from TextBlock toSelectable as interim (final selectable requirement may require read-only TextBox) – MUST still bind `CurrentStudyLabel`.
- **FR-155** Previous study tab title MUST be formatted `YYYY-MM-DD MOD` where MOD is derived modality (LOINC-derived when available; fallback regex heuristic on studyname).
- **FR-156** Previous study tabs MUST be unique by the composite (StudyDateTime, Modality). Duplicate combinations are ignored during load/ingest.
- **FR-157** PACS procedure metadata getter failures MUST NOT propagate exceptions (return null + debug log entry).
- **FR-158** Current study label MUST be displayed in a selectable read-only text control (read-only TextBox) instead of non-selectable label (PP2 fix).
- **FR-159** Previous study tab buttons MUST not toggle off the active tab when clicked again (except when a different overflow item is selected) – at least one tab remains visually active.
- **FR-160** Adding a previous study MUST perform an UPSERT into `med.rad_report` keyed by (study_id, report_datetime) updating existing row instead of inserting duplicate (constraint: uq_rad_report__studyid_reportdt).
- **FR-161** On New Study load completion, system MUST automatically load all existing prior studies (with reports) for the patient into previous study tabs (unique by StudyDateTime+Modality rule still applies).

## Update: Automation UX refinements (2025-10-05)
- **FR-183** New Study button MUST be a no-op when automation settings exist but contain zero modules.
- **FR-184** Settings window MUST provide dedicated Save Automation action persisting only automation sequences.
- **FR-185** Dropping module onto Available Modules pane MUST remove it from ordered panes and appear once in library.
- **FR-186** Settings window title bar MUST use dark immersive mode (when OS supports) matching main window.
- **FR-187** Automation drop indicator MUST clear on mouse up (even failed drop) and ghost removed.

## Update: Automation duplicate modules & live report JSON (2025-10-05)
- **FR-191** Automation panes MUST allow duplicate module entries (same module can appear multiple times in same or different panes).
- **FR-192** Each ordered module entry MUST expose a remove (X) control that removes only that instance from its pane.
- **FR-193** Available Modules pane MUST retain modules regardless of drag (modules are copied, not moved).
- **FR-194** Main window MUST display live JSON preview of current report (header+findings+conclusion) in txtJson updating on each edit.
- **FR-195** Findings editor MUST bind two-way to ReportFindings alias property feeding live JSON generation.
- **FR-196** Automation drag drop indicator MUST clear when cursor leaves list bounds or drag data invalid.

## Update: Automation move/copy semantics & DB tab restore (2025-10-05)
- **FR-197** Drag from library MUST copy; drag between ordered panes MUST move (reordering within same pane keeps only one instance).
- **FR-198** Remove (X) button MUST display literal 'X' and remove only the clicked instance.
- **FR-199** Database settings tab MUST remain visible alongside Automation tab.
- **FR-200** Drop indicator MUST always clear on drag leaving any list or invalid target (with debug logging for diagnostics).

## Update: Lock Study module & JSON simplification (2025-10-05)
- **FR-201** Report JSON preview MUST include only findings and conclusion (header removed from combined field).
- **FR-202** A modular `LockStudy` procedure MUST exist to only set PatientLocked and status without clearing fields.
- **FR-203** Automation sequences may include `LockStudy` alongside `NewStudy` executing both in defined order.

## Update: Reportify behavior & locking decoupling (2025-10-05)
- **FR-204** NewStudyProcedure MUST not perform locking; locking handled solely by LockStudy module.
- **FR-205** When reportified is ON, any edit to current header/findings/conclusion auto-disables reportified and restores raw text.
- **FR-206** Live JSON preview MUST always reflect raw (un-reportified) findings/conclusion regardless of reportified toggle state.
- **FR-207** Conclusion reportify MUST only capitalize first letter per line and append trailing period if alphanumeric ending (no numbering/indent injection).

## Update: Reportify formatting & immediate JSON (2025-10-05)
- **FR-208** Toggling reportified MUST not alter text with regex artifacts; only sentence capitalization + trailing period per line.
- **FR-209** Live JSON preview MUST reflect the latest keystroke (no character lag) by capturing raw text before serialization.

## Update: Two-way JSON editing (2025-10-05)
- **FR-210** JSON preview panel MUST support two-way editing; valid JSON edits update Findings/Conclusion (raw); invalid JSON ignored without breaking existing text.

## Update: Study Remark mapping & PACS getter (2025-10-05)
- **FR-211** UI Spy KnownControl list MUST include a mappable `Study remark` entry enabling bookmark capture.
- **FR-212** Known control ComboBox in SpyWindow MUST list items in case-insensitive alphabetical order by display text.
- **FR-213** PACS service MUST expose custom procedure method tag `GetCurrentStudyRemark` retrievable via `PacsService.GetCurrentStudyRemarkAsync()` and selectable in custom procedure method list.

## Update: Integrated Phrases Management Tab (2025-10-05)
- **FR-235** Settings window MUST include a "Phrases" tab consolidating phrase add/activate/refresh and listing (Id, Text, Active, Updated, Rev) replacing standalone PhrasesWindow; bindings reuse existing PhrasesViewModel commands and collections.

## Update: Phrase toggle resilience (2025-10-05)
- **FR-236** Toggling a phrase Active flag MUST tolerate transient connection/read timeouts on preliminary SET LOCAL statement_timeout without failing the toggle; the toggle retry logic (≤4 attempts) must proceed even if SET LOCAL fails.

## Update: Phrase toggle adaptive retry (2025-10-05)
- **FR-237** Phrase Active toggle MUST implement adaptive retry (≥4 attempts with exponential-ish backoff) clearing pooled connections on transient timeouts and must not surface Npgsql timeout errors caused by repeated rapid toggling; SET LOCAL failures are ignored.

## Update: Phrase toggle roundtrip removal (2025-10-05)
- **FR-238** Phrase Active toggle MUST avoid auxiliary SET LOCAL commands; a single UPDATE statement with reduced CommandTimeout (≤12s) and cancellation token (≤6s) is used to minimize stream read timeouts on rapid successive toggles.

## Update: Integrated Phrases dark theme (2025-10-05)
- **FR-239** Settings Phrases tab MUST use unified dark theme (panel #262A30, alt #2F343A, headers #30363D, selection #0E4D7A) and the standalone PhrasesWindow MUST be removed along with its launch button.

## Update: Integrated Spy tab (2025-10-05)
- **FR-240** Settings window MUST include a Spy tab exposing process pick, bookmark chain editing, minimal custom procedures grid, and quick resolve actions duplicating SpyWindow core controls for consolidated tooling.

## Update: Global Dark Theme + Spy PACS Combo (2025-10-05)
- **FR-241** Introduce global DarkTheme.xaml merged in App.xaml to centralize brushes & control styles (Window, Button, TextBox, ComboBox, TabControl, DataGrid, TreeView, GroupBox, CheckBox, ScrollBar, TextBlock). All windows (including Settings/Spy) should rely on these implicit styles instead of local duplicates.
- **FR-242** Populate PACS Method ComboBox in Settings Spy tab with same items as standalone SpyWindow (procedure authoring) including new current-context getters.
- **FR-243** Enhance implicit ComboBox style with full custom template (toggle button + popup) to ensure consistent dark styling across MainWindow and SettingsWindow; remove discrepancy where Settings combo used default light chrome.

## Update: Click-Anywhere ComboBox Drop (2025-10-05)
- **FR-244** Modify global ComboBox template to allow opening dropdown when clicking anywhere in text/content area (overlay transparent ToggleButton bound to IsDropDownOpen). Improves UX parity with custom editors.

## Update: MainWindow ComboBox + Hover Tuning (2025-10-05)
- **FR-245** Apply global click-anywhere ComboBox template to MainWindow by removing legacy DarkMiniCombo style; adjust hover to darker tone (AccentAlt border, PanelAlt background) for reduced brightness.

## Update: Reportify Settings Persistence (2025-10-05)
- **FR-249** System MUST persist per-account reportify settings JSON in central table radium.reportify_setting with columns (account_id PK/FK, settings_json jsonb, updated_at, rev).
- **FR-250** Settings window Reportify tab MUST provide Load Settings and Save Settings buttons which load existing JSON into individual option checkboxes/text fields and persist current in-memory configuration respectively.
- **FR-251** Saving reportify settings MUST perform an upsert (INSERT .. ON CONFLICT) incrementing rev and updating updated_at only when settings_json changes; client ignores transient DB errors silently.

## Update: Reportify Settings Auto-Load (2025-10-05)
- **FR-252** Application MUST automatically load per-account reportify settings JSON during successful login (silent refresh or explicit) storing it in tenant context; Reportify tab shows only Save and Close (no manual Load) and Save performs create/update upsert accordingly.

## Update: Reportify Functional Implementation (2025-10-05)
- **FR-253** Reportify toggle MUST apply all enabled transformations from persisted per-account settings (arrows, bullets, spacing, parentheses, number-unit spacing, whitespace collapse, capitalization, trailing period, paragraph numbering for conclusions, indentation of continuation lines) and reflect changes immediately upon toggling; settings JSON changes after login take effect on next toggle.

## Update: Phrase Loading Guard & Account Change Events (2025-10-05)
- **FR-254** Phrase loading MUST return empty (and never query DB) when AccountId <= 0; Add/Toggles throw or no-op until a valid AccountId is set.
- **FR-255** Application MUST raise an AccountIdChanged event (oldId,newId) on tenant context; phrase UI MUST clear on logout (newId<=0) and auto reload on first valid login (transition 0->>0).

## Update: Settings Window Composition (2025-10-05)
- **FR-256** Settings window MUST resolve SettingsViewModel via DI including tenant + reportify services; Save Settings button enabled only when AccountId > 0 and services present.
- **FR-257** Phrases tab within Settings MUST bind to shared PhrasesViewModel instance (no duplicate loads); bindings NewText/AddCommand/Items/RefreshCommand must resolve without binding errors.

## Update: Completion First Item Auto-Select (2025-10-07)
- **FR-264** Completion popup MUST auto-select the first item by default whenever it opens or when no exact prefix match is found, ensuring immediate Enter commits the first suggestion without requiring initial Down key navigation.

## Update: Side Panel Dynamic Width (2025-10-07)
- **FR-268** MainWindow `gridSide` width MUST equal (window ActualWidth - central grid ActualWidth) clamped to >=0; updates reactively on resize.

## Update: Top/Bottom Panel Dynamic Height (2025-10-07)
- **FR-269** MainWindow `gridTop` and `gridBottom` heights MUST equal (window ActualHeight - central grid ActualHeight) clamped to >=0 and auto-update on resize.

## Prior Updates
<!-- cumulative prior content retained below -->
## Update: Previous report ingestion refinements (2025-10-05)
- **FR-152a** Ensure AddStudy always populates `header_and_findings` and `conclusion` (attempt retrieval regardless of banner validity) and only display studies with at least one report row containing either field.

## Update: Tree default disabled & new PACS getters (2025-10-05)
- **FR-148** SpyWindow TreeView MUST be disabled (hidden) by default; user enables via checkbox.
- **FR-149** After a Pick capture, system MUST auto-clear (uncheck) UseIndex for all nodes in captured chain.
- **FR-150** Add new bookmark target `ReportText2` for alternate report text control mapping.
- **FR-151** Add PACS procedure method tags & service accessors: GetCurrentFindings, GetCurrentConclusion, GetCurrentFindings2, GetCurrentConclusion2.
- **FR-152** When AddStudy command invoked: gather selected related study metadata, ensure rad_study, verify banner matches patient/study date; pull findings/conclusion (choose longer variant); build partial report JSON with only `header_and_findings` and `conclusion` populated plus duplication in `findings`; insert med.rad_report row (is_created=false, is_mine=false, created_by=radiologist, report_datetime=report date); refresh PreviousStudies from DB.

## Update: Previous study tab modality & toggle reliability (2025-10-02)
- **FR-153** Previous report Reportified toggle MUST apply reversible formatting using preserved original previous study text (no cumulative transformation loss).
- **FR-154** Current study label visual element changed from TextBlock toSelectable as interim (final selectable requirement may require read-only TextBox) – MUST still bind `CurrentStudyLabel`.
- **FR-155** Previous study tab title MUST be formatted `YYYY-MM-DD MOD` where MOD is derived modality (LOINC-derived when available; fallback regex heuristic on studyname).
- **FR-156** Previous study tabs MUST be unique by the composite (StudyDateTime, Modality). Duplicate combinations are ignored during load/ingest.
- **FR-157** PACS procedure metadata getter failures MUST NOT propagate exceptions (return null + debug log entry).
- **FR-158** Current study label MUST be displayed in a selectable read-only text control (read-only TextBox) instead of non-selectable label (PP2 fix).
- **FR-159** Previous study tab buttons MUST not toggle off the active tab when clicked again (except when a different overflow item is selected) – at least one tab remains visually active.
- **FR-160** Adding a previous study MUST perform an UPSERT into `med.rad_report` keyed by (study_id, report_datetime) updating existing row instead of inserting duplicate (constraint: uq_rad_report__studyid_reportdt).
- **FR-161** On New Study load completion, system MUST automatically load all existing prior studies (with reports) for the patient into previous study tabs (unique by StudyDateTime+Modality rule still applies).
- **FR-162** AddStudy MUST validate that selected related study patient number/id matches current patient; on mismatch no ingestion occurs and a status message in red is shown.
- **FR-163** Previous report Reportified toggle MUST default to ON when application starts a new study and after each successful AddStudy ingestion.
- **FR-164** Settings window MUST expose an "Automation (Preview)" tab with placeholder checkboxes for configuring actions on New Study and Add Study (non-functional skeleton).
- **FR-165** Previous study initial text MUST be treated as already reportified baseline; toggle OFF performs dereportify transform shown in editors; toggle ON restores original baseline text without reprocessing.
- **FR-166** Automation settings MUST present two reorderable lists (New Study, Add Study) and one library list via drag & drop skeleton; duplicates prevented per pane (preview, non-persistent).
- **FR-174** Previous study dereportify (toggle OFF) MUST preserve original newline boundaries; each line independently inverse-transformed (no paragraph reflow).
- **FR-175** AddStudy MUST normalize patient identifiers (alphanumeric uppercase, stripping punctuation) prior to comparison; mismatch blocks ingestion with explicit status message.
- **FR-177** Automation module sequences (New Study, Add Study) MUST be persisted locally and drive behavior of New Study command.
- **FR-178** Settings window MUST use dark theme consistent with main window styling.

### Functional Requirements (Phrase Database Stability)
- **FR-258** Phrase change operations (toggle active, add phrase) MUST follow strict synchronous flow: user action → synchronously update database → synchronously update in-memory snapshot → display snapshot state (never display database state directly).
- **FR-259** Phrase toggle operations MUST prevent UI state corruption by blocking additional toggle requests during active toggle processing and ensuring all UI updates reflect final snapshot state.
- **FR-260** Phrase add operations MUST complete database upsert and snapshot update before displaying new phrase in UI, with automatic consistency recovery via snapshot refresh on any operation failure.

### Functional Requirements (Mapping & Procedures)
- **FR-090** Studyname→LOINC mapping UI MUST allow multi-part selection, ordering (sequence letters), and duplicate part numbers with distinct sequence order (pair uniqueness rule).
- **FR-091** System MUST surface playbook suggestions once ≥2 distinct selected parts exist.
- **FR-092** Double-clicking a playbook MUST bulk import its parts, skipping only existing (PartNumber+SequenceOrder) pairs.
- **FR-093** User MUST be able to add or remove individual parts including sequence order edits prior to save.
- **FR-094** Mapping save MUST persist all selected parts and sequence orders and update in-memory cache for immediate future study resolution.
- **FR-095** Procedure operation GetValueFromSelection MUST return target column value using normalization rules and alignment preservation.
- **FR-096** Procedure operation ToDateTime MUST parse supported date/time formats or report parse fail without throwing.
- **FR-097** PACS selection helper methods MUST retrieve structured field values (name, sex, birth date, etc.) from either Search Results or Related Study list selections using unified header logic.
- **FR-098** Procedure operation GetTextOCR MUST perform OCR on target element bounds and return extracted text or explicit status tokens ("(no element)", "(ocr unavailable)", "(empty)" etc.).
- **FR-099** PACS helper MUST expose current patient number via banner heuristic (longest digit sequence length ≥5) and current study date/time via date(+time) pattern.
- **FR-135** Custom procedure Split operation MUST support optional third argument (Arg3, Number) representing zero-based index; when provided and within bounds the operation stores only that part and preview shows part[index]; when out of range preview = (index out of range N); when omitted legacy behavior (all parts joined with \u001F) is preserved.

### Functional Requirements (Reliability & Logging)
- **FR-120** First-chance Postgres exception sampler MUST log each unique (SqlState|Message) only once per application session.
- **FR-121** PhraseService MUST use configurable LocalConnectionString (settings) with fallback; no hardcoded credentials.
- **FR-122** System MUST allow continuation of reporting if any single LLM call fails, marking only affected fields.
- **FR-123** OCR / banner extraction failures MUST NOT crash procedure execution; they return null output with diagnostic preview.

### Data & Key Entities
- **Report**: technique, chief_complaint, history_preview, chief_complaint_proofread, history, history_proofread, header_and_findings, conclusion, split_index, comparison, technique_proofread, comparison_proofread, findings_proofread, conclusion_proofread, findings, conclusion_preview.
- **PrevReport**: header_and_findings, split_index, chief_complaint, history, technique, comparison, *_proofread, findings_proofread, conclusion, conclusion_proofread.
- **Study**: identifiers, modality, studyname, study remark, patient remark, timestamps.
- **Mapping (StudynameLOINCParts)**: studyname, part list (part_number, part_name, sequence_order), derived technique link [NEEDS CLARIFICATION: technique derivation rule].
- **GhostSuggestion**: list of alternative multi-line suggestions + selected index.
- **Snippet**: template text with placeholder definitions & navigation order.
- **ProcedureOperation**: name, Arg1/Arg2 types, output variable, preview.
- **PhraseState**: in-memory snapshot tracking phrase id, text, active status, update timestamps, revision numbers per account with synchronous update semantics.

---
## Review & Acceptance Checklist
### Content Quality
- [ ] No implementation details in core sections (HOW isolated to Appendix)
- [x] Focused on user value & outcomes
- [x] Mandatory sections completed

### Requirement Completeness
- [ ] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements testable & numbered
- [x] Scope boundaries: Reporting workflow (current + previous), mapping, editor assistance, PACS submission, OCR extraction, completion improvements, bug fixes, phrase stability
- [ ] Assumptions validated (pending clarifications)

---
## Execution Status
- [x] User description parsed
- [x] Key concepts extracted
- [x] Ambiguities marked
- [x] User scenarios defined
- [x] Requirements generated
- [x] Entities identified
- [ ] Review checklist fully passed (clarifications outstanding)

---
## Ambiguities / Clarification Requests
1. Studyname mapping dismissal behavior (force, skip, mark?).
2. PACS field length overrun handling (truncate vs block?).
3. Technique derivation rule from LOINC parts (deterministic mapping or user input precedence?).
4. Acceptable latency targets for postprocess (LLM) before offering manual fallback.
5. Ghost suggestion insertion span (line remainder vs context-aware region) final policy.
6. OCR banner element reliability vs whole-window heuristic fallback thresholds.

---
## Non-Functional (Derived)
- **Performance**: Header metadata render <1s (local). Ghost suggestion request dispatch ≤100ms after idle event. LOINC mapping UI list filtering <150ms on typical dataset. OCR element operation should return in <1.2s typical (depends on Windows OCR engine). Completion cache refresh <50ms after phrase add/modify. Keyboard navigation response <50ms. Selection guard event handling <10ms. Multiple event handling <5ms. Navigation state tracking <1ms. Recursive guard handling <1ms. Phrase database operations <2s under normal network conditions. Phrase snapshot updates <100ms.
- **Reliability**: Any single external (LLM) failure must not block manual editing or PACS send if mandatory fields filled. Dependency injection must resolve all required services to prevent runtime failures. Selection guard must prevent infinite recursion. Multiple SelectionChanged events must be handled gracefully. Navigation state must be tracked accurately. Recursive event handling must be prevented. Phrase database operations must maintain consistency under network instability. Phrase UI must always display snapshot state, never optimistic state.
- **Observability**: Structured logging for each pipeline stage (event type, duration, success/failure) [NEEDS CLARIFICATION: log schema]. Phrase operation logging (database operation, snapshot update, UI update timing).
- **Local Persistence**: Final report write must be atomic (transaction or temp+rename) [detail for plan]. Phrase snapshot must maintain consistency across database operations.

---
## Risks
- Over-reliance on LLM latency impacting radiologist throughput.
- Ambiguous previous report headers producing incorrect structured fields (mitigate with confidence score + fallback).
- Mapping quality affecting downstream technique auto-fill accuracy.
- OCR variability across workstation font/render settings impacting patient number extraction accuracy.
- Completion popup navigation recursion causing UI freezes or instability.
- Phrase database timeouts causing UI inconsistency (mitigated by synchronous snapshot updates).

---
# Legacy / Implementation Detail Chronicle

## 1) Bug Fixes (2025-01-01 - Latest)
- **Navigation State Tracking**: Added _hasUserNavigated flag to EditorControl to properly track whether user has used keyboard navigation, ensuring first Down key always selects first item (index 0) regardless of existing selections from exact matching. Reset flag when creating new completion window and when closing.
- **Recursive Guard Protection**: Enhanced MusmCompletionWindow selection guard to prevent recursive event handling by adding _handlingSelectionChange flag and proper event flow control during programmatic selection changes.

## 2) Bug Fixes (2025-01-01 - Previous)
- **Multiple Event Handling**: Enhanced MusmCompletionWindow selection guard to detect and preserve selections that are added via keyboard navigation by analyzing SelectionChanged event patterns (added vs removed items). Added logic to prevent immediate clearing of new selections that don't have corresponding removals.
- **Keyboard Focus Management**: Improved EditorControl keyboard navigation to ensure ListBox gets proper focus during navigation, enhancing the keyboard focus detection in the selection guard.

## 3) Bug Fixes (2025-01-01 - Earlier)
- **Selection Guard Recursion Prevention**: Enhanced MusmCompletionWindow selection guard to prevent recursive clearing by temporarily disabling SelectionChanged event handler during programmatic selection changes. Added logic to detect and allow programmatic clearing operations.
- **Navigation Event Handling**: Improved EditorControl keyboard navigation logic with enhanced debugging and better flow control to ensure AllowSelectionByKeyboardOnce is called at the appropriate time before selection changes.

## 4) Bug Fixes (2025-01-01 - Earlier)
- **Phrase Extraction Service Injection**: Fixed PhraseExtractionWindow creation to use dependency injection (GetService<PhraseExtractionViewModel>()) instead of manual instantiation, preventing service injection errors during phrase save operations.
- **Completion Navigation Enhancement**: Improved Up/Down key handling in completion popup by explicitly managing selection state transitions and ensuring proper event handling to prevent navigation conflicts.

## 5) Editor Completion Improvements (2025-01-01)
- **Immediate Cache Refresh**: Added IPhraseCache.Clear() method and cache invalidation in PhraseService.UpsertPhraseAsync() and ToggleActiveAsync() to ensure completion list updates immediately when phrases are added/modified.
- **Fixed Down/Up Navigation**: Modified MusmCompletionWindow selection guard to allow keyboard navigation while maintaining exact-match-only behavior for non-keyboard selection events.

## 6) Phrase Database Stability Improvements (2025-10-05)
- **Synchronous Database Flow**: Implemented strict synchronous flow for phrase operations: user action → database update → snapshot update → UI display from snapshot.
- **UI State Protection**: Added toggle state protection to prevent UI corruption during database operations and ensure final display always matches snapshot state.
- **Consistency Recovery**: Enhanced error handling to automatically refresh from snapshot when any phrase operation fails, ensuring UI consistency.

## 7) Spy UI Tree focus behavior
- Show a single chain down to level 4 (root → ... → level 4).
- Expand the children of the level-4 element; depth bounded by `FocusSubtreeMaxDepth`.
- Depths configurable via constants in `SpyWindow`.

## 8) Procedures grid argument editors
- Operation presets set Arg1/Arg2 types and enablement, with immediate editor switch.
- Element/Var editors use dark ComboBoxes.

## 9) PACS Methods (Custom Procedures): "Get selected ID from search results list"
- New method Tag="GetSelectedIdFromSearchResults".
- Implements PacsService.GetSelectedIdFromSearchResultsAsync.

## 10) Studyname → LOINC Parts window
Goal
- Provide clear mapping UI from med.rad_studyname to LOINC Parts.

Database
- Tables: med.rad_studyname, med.rad_studyname_loinc_part; source schema loinc (loinc.part, loinc.rplaybook, loinc.loinc_term)

Studynames List
- View: bind ListBox to StudynamesView (ICollectionView) so textbox above filters by Studyname
- VM: LoadAsync calls GetStudynamesAsync; auto-select first

LOINC Parts UI
- 4x5 grid of category listboxes (equal row heights), each with its own filter and vertical-only scrollbar
- Items show only part_name (no part_number)
- Double-click on category/Common item adds it to Mapping Preview with Sequence Order input (default "A"); duplicates allowed
- Preview panel: shows each item with an editable PartSequenceOrder TextBox; double-click removes the item
- Sequence Order input placed at the right side of the Preview header
- Common list shows most-used parts (from repo aggregation), spanning two cells

Playbook Suggestions (under Preview)
- With ≥ 2 distinct selected parts, suggest playbooks grouped by loinc_number (each LOINC spans multiple part rows)
  - LEFT: long_common_name (distinct by loinc_number) ? vertical scrollbar present; layout changed from StackPanel to Grid so ListBox height is constrained ensuring scrollbar visibility (PP1 fix).
  - RIGHT: the playbook parts list for the selected loinc_number (part_name + part_sequence_order)
- Double-click behaviors:
  - Double-click a playbook (left list) imports all its parts with their sequence orders into Preview, skipping only duplicate (PartNumber + SequenceOrder) pairs. Uses async-safe wait loop to ensure parts have loaded before import (PP2 fix).
  - Double-click a single playbook part (right list) adds that part if the combination PartNumber + PartSequenceOrder is not already present (PP3 rule refinement).

Duplicate Rule (PP3)
- Previous logic blocked duplicates by PartNumber only. Updated rule: allow same PartNumber multiple times if SequenceOrder differs. A duplicate is only when both PartNumber and PartSequenceOrder match an existing preview item.

Acceptance Criteria
- Single main window shown on run (no duplicate windows)
- Studynames textbox filters in real-time
- long_common_name suggestions appear under Preview when ≥ 2 parts match one or more loinc_number groups
- Double-click on playbook reliably imports after async load without null/iteration errors
- Duplicate rule honors (PartNumber + SequenceOrder) uniqueness
- Build succeeds

Notes / Future Enhancements
- Remove/reorder controls for preview
- Virtualization and count badges
- Import button (double-click now handles bulk import)

## 11) Custom Procedure Operation: GetValueFromSelection
Purpose
- Allow a procedure step to extract the value of a specific column header from the currently selected row of a list (e.g., mapped SearchResultsList) reusing the Row Data parsing logic.

Definition
- Operation name: `GetValueFromSelection`.
- Arg1: Element (required) → list control (e.g., SearchResultsList mapping).
- Arg2: String (required) → header text to extract. Example: `ID`, `Accession No.`, `Status`.
- Output: value of the first column whose normalized header equals Arg2 (case-insensitive). If not exact match, the first header containing Arg2 substring is used. If still not found → null.

Normalization Rules (reuse existing):
- Accession → Accession No.
- Study Description → Study Desc
- Institution Name → Institution
- BirthDate → Birth Date
- BodyPart → Body Part

Behavior
- If no selection exists → preview "(no selection)" .
- If header not found → preview `(Arg2 not found)`.
- On success preview = extracted value; stored in `var#` for chaining.
- Default Arg2 value set to "ID" when operation selected with empty Arg2.

Implementation Notes
- Added to Operation ComboBox list in Custom Procedures grid.
- Preset rules in `OnProcOpChanged` assign Arg1=Element, Arg2=String, enable Arg2 and seed default.
- Execution implemented in both single-row Set (ExecuteSingle) and full Run (RunProcedure) flows.
- Reuses `GetHeaderTexts` + `GetRowCellValues` + `NormalizeHeader` helpers already present in SpyWindow code-behind.

## 12) Row Data & Selection Value Alignment Preservation
Change (Earlier Iteration)
- `GetRowCellValues` now always adds placeholders for blank cells (no skipping) to keep header/value alignment.

## 13) Blank Header Column Preservation (Current Iteration)
Problem
- Some lists contain a leading blank header cell (first column intentionally unlabeled). Previous logic skipped blank headers which then shifted all subsequent header/value associations (e.g. Status value appearing under ID header).

Solution
- `GetHeaderTexts` now adds an empty string entry for every header cell (even if blank after probing descendants). This preserves positional alignment with the row cell values list.
- Row Data formatter logic updated:
  - Build pairs (Header, Value) with normalized headers.
  - When both header and value are blank → pair omitted entirely.
  - When header blank but value present → output just `Value` (no colon label) preserving its sequence.
  - When header present → `Header: Value` as before.

Result
- Leading blank header no longer causes value shifts.
- Example corrected: either leading blank pair omitted (if value also blank) giving: `Status: Examined | ID: 239499 | Name: … | ... | Institution: BUSAN ST.MARY'S HOSPITAL` OR if value present for first blank header: `<value> | Status: Examined | ID: ...` maintaining order.

Testing Scenarios
1. Leading blank header with blank value → dropped; no shift.
2. Leading blank header with nonblank value → value emitted alone at start.
3. Internal blank header between populated headers → value-only token preserved in position.
4. `GetValueFromSelection` unaffected; indexing uses parallel arrays including blank header placeholders.

## 14) Operation: ToDateTime
Purpose
- Convert a string variable containing a date or date-time in formats `YYYY-MM-DD` or `YYYY-MM-DD HH:mm:ss` into a normalized DateTime ISO string stored in a new var.

Definition
- Operation name: `ToDateTime`.
- Arg1: Var (string content) required.
- Arg2: (unused) disabled.
- Output: if parse succeeds, stored as ISO8601 (`o`) and preview shows `yyyy-MM-dd HH:mm:ss`; else preview `(parse fail)`.

Parse Rules
- Exact match using `InvariantCulture` for the two patterns.
- `AssumeLocal` style.

## 15) New PACS Selection Methods
Added Methods (Search Results list unless noted):
- Get selected name from search results list
- Get selected sex from search results list
- Get selected birth date from search results list
- Get selected age from search results list
- Get selected studyname from search results list
- Get selected study date time from search results list
- Get selected radiologist from search results list
- Get selected study remark from search results list
- Get selected report date time from search results list
- Get selected studyname from related studies list
- Get selected study date time from related studies list
- Get selected radiologist from related studies list
- Get selected report date time from related studies list

Implementation
- Added ComboBox items with Tags matching new service method identifiers.
- `PacsService` includes one helper `GetValueFromListSelection` that resolves either `SearchResultsList` or `RelatedStudyList`, reads the selected row, aligns headers/cells preserving blanks, and returns first non-empty candidate among provided header names (exact then contains).
- For ID existing method kept but refactored to reuse header selection logic.
- Headers probed: Name, Sex, Birth Date, Age, Study Desc (aliases Study Description, Studyname), Study Date, Requesting Doctor (alias Radiologist/Doctor), Study Comments (Remark), Report approval dttm (Report Date/Time).

Notes
- All methods return raw string (no trimming beyond whitespace removal). Date/time values can be piped through `ToDateTime` in a procedure if normalization needed.

## 16) Postgres First-Chance Exception Sampling
- `PgDebug.Initialize()` registers a first-chance handler capturing `PostgresException` once per (SqlState|MessageText) pair and logs via Serilog at Warning level.
- Added initialization in `App.OnStartup` before splash login.
- Repository methods now log explicit PostgresException details with SqlState and server message for triage.

## 17) PhraseService Connection Refactor
Changes
- PhraseService now consumes `IRadiumLocalSettings.LocalConnectionString` with legacy fallback.
- Added debug logging of host/db/user when opening connections.
- Sets minimal timeouts and includes error detail.

Future
- Planned migration to use `CentralConnectionString` (central phrases) once consolidation is ready; code comments note this.

## Update: Reportified State Text Change Cancellation (2025-01-08)
- **FR-270** When user attempts to edit header, findings, or conclusion while reportified state is ON, the text change MUST be cancelled (not applied) and reportified state MUST be toggled OFF, requiring user to make the edit again in dereportified state.
- **FR-271** Text change cancellation MUST occur before any property update, preserving the current (reportified) text until user makes the edit in dereportified state.
- **FR-272** After cancelling a text change and dereportifying, the editor MUST display the dereportified (raw) text for the user to make their intended edit.

## Update: Completion First Item Auto-Select (2025-10-07)
- **FR-264** Completion popup MUST auto-select the first item by default whenever it opens or when no exact prefix match is found, ensuring immediate Enter commits the first suggestion without requiring initial Down key navigation.
- FR-305 Completion list first Down/Up press MUST move to the immediate next/previous item and MUST NOT wrap to last/first when no selection existed previously.
- FR-306 Completion popup MUST not auto-select exact text matches; default selection is the first item to ensure Down selects the second item consistently.
