# Feature Specification: Radium Cumulative – Reporting Workflow, Editor Experience, PACS & Studyname→LOINC Mapping

## Update: Automation Modules for Remarks (2025-10-10)
- FR-348 Settings → Automation MUST include two modules in the Available Modules list:
  - GetStudyRemark
  - GetPatientRemark
- FR-349 When the automation sequence runs and includes GetStudyRemark, the app MUST call PACS method "Get current study remark" and set `study_remark` in the current report JSON to the fetched string (empty when null).
- FR-350 When the automation sequence runs and includes GetPatientRemark, the app MUST call PACS method "Get current patient remark" and set `patient_remark` in the current report JSON to the fetched string (empty when null).
- FR-351 `CurrentReportJson` MUST round-trip these fields. Editing `study_remark` or `patient_remark` directly in the JSON view MUST update the bound properties, and properties changes MUST serialize back to JSON.
- FR-352 Remarks capture MUST not throw; errors set status text and leave previous values unchanged.

## Update: Fix mapping for Patient Remark (2025-10-10)
- FR-353 SpyWindow MUST expose a distinct known control `PatientRemark` (separate from `StudyRemark`) so users can map the patient remark UI element explicitly.
- FR-354 Known control list in SpyWindow (Map to:) MUST include `PatientRemark`.
- FR-355 `GetCurrentPatientRemark` custom procedure MUST be able to use Arg1 Element = `PatientRemark` to extract the correct field value; previous flows that targeted `StudyRemark` will need remapping by the user.

## Update: Auto-seed key procedure for GetPatientRemark (2025-10-10)
- FR-356 If no user-defined procedure exists for the PACS method tag `GetCurrentPatientRemark`, the app MUST auto-create and persist a default procedure consisting of a single step: `GetText` on Element=`PatientRemark`.
- FR-357 The GetPatientRemark automation module MUST use `GetCurrentPatientRemark` as its key procedure entry-point (via `PacsService.GetCurrentPatientRemarkAsync`), ensuring the auto-seeded procedure is executed when user has not authored one yet.
- FR-358 Similarly, when missing, the app SHOULD auto-seed `GetCurrentStudyRemark` with a single step: `GetText` on Element=`StudyRemark`.

## Update: Enforcement – patient_remark source (2025-10-10)
- FR-359 `patient_remark` in the current report JSON MUST only be set from the result of executing the `GetCurrentPatientRemark` custom procedure (through `PacsService.GetCurrentPatientRemarkAsync()`), with no alternative source or fallback. Any prior non-procedure source MUST be removed.

## Update: ProcedureExecutor – GetHTML and Replace ops (2025-10-10)
- FR-360 Procedure execution engine MUST support operations:
  - `Replace` (uses C#-style escape decoding via Regex.Unescape for Arg2/Arg3; replaces all occurrences).
  - `GetHTML` (performs HTTP GET on URL from Arg1 Var or String; applies smart decoding using response header charset and meta charset; returns HTML string; errors produce `(error)` preview with null output as per FR-338).
- FR-361 Procedure execution MUST NOT prematurely return before attempting fallback auto-seed for known PACS tags; remove early-return that bypassed fallback creation.

## Update: Crawl Editor "Get HTML" button (2025-10-10)
- FR-339 SpyWindow Crawl Editor MUST provide a "Get HTML" button next to "Get Name".
  - Behavior: Performs an HTTP GET for a URL read from the clipboard (must start with http/https) and writes the fetched HTML into the status box.
  - Error Handling: When clipboard does not contain a valid URL, show "Get HTML: copy a URL to clipboard first.". Any fetch error shows "Get HTML error: {message}".
  - Implementation: Reuse the same HttpClient as Custom Procedure `GetHTML` (FR-338) ensuring consistent behavior.

## Update: Get HTML Korean decoding fix (2025-10-10)
- FR-340 "Get HTML" MUST decode response bytes using charset detection and support Korean encodings.
  - Charset detection order: HTTP Content-Type charset → HTML <meta> charset (first ~8KB) → UTF-8 fallback with quality check → CP949 (EUC-KR superset) fallback when UTF-8 looks corrupted.
  - The Custom Procedure operation `GetHTML` MUST use the same decoding logic.
  - The application MUST register `CodePagesEncodingProvider` at startup to enable CP949/EUC-KR.
  - Result: Korean text is readable (no "��" mojibake) for common KR news/medical sites.

## Update: Statistical charset detection fallback (2025-10-10)
- FR-341 When header/meta are inconclusive or inconsistent, the system MUST apply a statistical charset detector in addition to known fallbacks.
  - Use Ude (Mozilla Universal Charset Detector) to propose a candidate encoding.
  - Build a candidate set: header, meta, Ude, UTF-8, CP949, x-windows-949, ms949, EUC-KR, KS_C_5601-1987.
  - Choose the best decoded text by heuristic: fewest replacement characters (U+FFFD), then highest Hangul count.
  - Applies to both Crawl Editor button and Custom Procedure `GetHTML`.

## Update: Custom Procedures multiline arguments (2025-10-10)
- FR-342 Procedures grid string argument editors MUST allow multi-line input to pass multi-line parameters.
  - Scope: `SpyWindow` → Custom Procedures → DataGrid columns `Arg1`, `Arg2`, `Arg3` when their Type is `String`.
  - UI: TextBoxes used for String types MUST set `AcceptsReturn=True`. Other types (Element/Var) continue to use ComboBoxes.
  - Behavior: Enter inserts a newline inside the TextBox instead of committing the cell edit; stored value preserves `\r\n` as typed.

## Update: Split op multiline separators, escapes, and regex (2025-10-10)
- FR-343 Split Arg2 MUST support C#-style escapes (e.g., `\n`, `\r\n`, `\t`) so users can express multi-line separators without literal newlines.
- FR-344 Split Arg2 starting with `re:` or `regex:` MUST be treated as a regular expression and split using `Regex.Split` with `Singleline | IgnoreCase`. This enables patterns like `</TR>\s*</TABLE></BR>` to match across line breaks and variable whitespace.
- FR-345 Split MUST retry CRLF when only LF is provided in Arg2 but the input uses Windows `\r\n` newlines (best-effort convenience).
- FR-346 Split behavior with Arg3 index remains unchanged: when a valid index is provided, output the selected part; otherwise, join all parts with `\u001F` and preview shows `{parts} parts`.

## Update: Replace op supports escapes (2025-10-10)
- FR-347 Replace operation Arg2 (search) and Arg3 (replacement) MUST support C#-style escapes via `Regex.Unescape` so users can search/replace `\n`, `\t`, etc.

## Update: PACS Patient Remark + New Procedure Ops (2025-10-10)
- FR-336 PACS method "Get current patient remark" MUST be available via Custom Procedures.
  - Combo item Tag: `GetCurrentPatientRemark` shown in SpyWindow → Custom Procedures → PACS Method list.
  - `PacsService.GetCurrentPatientRemarkAsync()` MUST call the procedure tag with retry and return string or null on failure (no exceptions propagate).
- FR-338 Add procedure operation `GetHTML`.
  - Args: Arg1 Var (URL); Arg2/Arg3 disabled.
  - Behavior: Perform HTTP GET to fetch HTML. On success, store full HTML; preview shows HTML; errors preview `(error)` and store null. Operation executes asynchronously in Set/Run flows.

## Update: Settings Automation Spy button (2025-10-10)
- FR-335 Settings window MUST provide a "Spy" button in the Automation tab footer, positioned next to "Save Automation". Clicking it MUST open the same `SpyWindow` as the Main Window "Spy" action (non-modal, owned by Settings window).

## Update: Snippet Runtime Behavior (2025-01-10)
- **FR-325** After snippet insertion the editor enters snippet mode: all placeholders highlighted; caret locked to active placeholder; cannot move outside using arrows/home/end.
- **FR-326** Navigation and termination:
  - Tab completes current placeholder and moves to next; if none, exit and place caret at end of inserted snippet.
  - Enter on free-text placeholder inserts "[ ]" for unfilled and exits to next line; on choice placeholders behaves like Tab completion then moves to next.
  - Esc exits snippet mode and caret moves to end of inserted snippet.
- **FR-327** Placeholder behavior:
  - Free text: ${label} → user types; Tab completes; if exit before completion, replace with "[ ]".
  - Mode 1 single choice: ${1^label=a^A|b^B} → single key immediately selects and completes; if exit before selection, default to first option.
  - Mode 2 multi-choice: ${2^label^or^bilateral=a^A|b^B|3^C} → Space or letter toggles; Tab accepts joined text with joiner (", or ", " and ", "); if exit before completion, insert all options using the joiner.
  - Mode 3 single replace: ${3^label=aa^A|bb^B} → multi-char key is buffered until Tab/Enter; then replace; if exit before selection, default to first option.
- **FR-328** UI overlay must show active placeholder distinctly (highlight), others faintly.
- **FR-329** Snippet mode MUST prevent caret from moving outside current placeholder bounds using arrow keys, Home, or End; caret position is constrained to the selection range.
- **FR-330** Placeholder completion popup MUST appear for choice-based placeholders (modes 1, 2, 3) showing available options with keys; popup updates as user types keys for mode 3.
- **FR-331** Multi-choice placeholders (mode 2) MUST display visual checkmarks (✓) next to selected options in the popup window.
- **FR-332** Bilateral option in mode 2 MUST be parsed and stored in AST for future domain-specific processing (e.g., combining "left X" + "right X" → "bilateral X").
- **FR-333** PlaceholderModeManager MUST maintain global snippet mode state preventing interference from other editor features during active snippet session.
- **FR-334** EditorMutationShield MUST guard all programmatic document mutations during snippet mode to prevent cascading event handlers and document change exceptions.

## Update: Snippet AST Auto-Build on Insert (2025-01-10)
- **FR-322** When adding a snippet from Settings → Snippets tab, the system MUST automatically generate `snippet_ast` from the provided `snippet_text` according to docs/snippet_logic.md and persist it with the row.
- **FR-323** Generated `snippet_ast` MUST contain version and placeholder list capturing mode, label, tabstop (if any), options (key→text), and multi-choice joiner/bilateral hints for mode 2.
- **FR-324** The UI MUST reflect the computed AST in a read-only/preview textbox before/after insert so users can inspect the parsed structure.

## Update: Snippet Management Tab in Settings (2025-01-10)
- **FR-319** Settings window MUST include a Snippets management tab similar to Hotkeys with fields: Trigger, Template (multi-line), AST (JSON, multi-line), Description; grid listing (Id, Trigger, Description, Template preview, Active, Updated, Rev) and actions (Add, Delete, Refresh).
- **FR-320** Snippets tab MUST display a read-only "Sample Snippet" textbox with the following content for user guidance:
  - First lines:
    - ${free text}
    - ${1^single choice=a^apple|b^banana|3^pear}
    - ${2^multiple choices^or=a^cola|b^cider}
    - ${3^single replace=a^apple|b^banana|3^pear}
  - Paragraph:
    - i have ${1^fruit=a^apple|b^bannana|3^watermelon} and ${2^juices=a^cola|b^cider}, and i want to eat with ${friend}, but ${3^family=mm^mom|dd^dad} said no...
- **FR-321** Snippet service MUST be registered and used by Snippets tab using synchronous DB→snapshot→UI flow consistent with phrases/hotkeys.

## Update: Snippet Text Expansion with Placeholders (2025-01-10)
- **FR-311** System MUST support user-defined snippets with placeholder navigation for structured text templates, enabling rapid insertion of complex report sections with tab-stop navigation between editable regions.
- **FR-312** Snippet storage MUST use central database table `radium.snippet` with columns: snippet_id (BIGINT IDENTITY PK), account_id (BIGINT FK NOT NULL), trigger_text (NVARCHAR(64) NOT NULL), snippet_text (NVARCHAR(4000) NOT NULL), snippet_ast (NVARCHAR(MAX) NOT NULL), description (NVARCHAR(256) NULL), is_active (BIT NOT NULL DEFAULT 1), created_at, updated_at, rev. Unique constraint on (account_id, trigger_text).
- **FR-313** snippet_text MUST contain template with placeholder syntax (e.g., `${1^fruit=a^apple|b^banana}` for choices, `${2:default}` for text placeholders).
- **FR-314** snippet_ast MUST store pre-parsed JSON representation of placeholder structure enabling fast runtime parsing without regex overhead during editor insertion.
- **FR-315** Snippet service MUST provide async methods: GetActiveSnippetsAsync(accountId), UpsertSnippetAsync(accountId, trigger, snippetText, ast, description), ToggleActiveAsync(accountId, snippetId). All operations MUST follow synchronous database → snapshot → UI flow.
- **FR-316** Snippet service MUST implement in-memory snapshot caching per account with automatic invalidation on upsert/toggle/delete and provide GetSnippetSnapshotAsync for completion integration.
- **FR-317** Editor MUST detect snippet triggers and insert template with placeholder markers, enabling tab navigation between placeholders.
- **FR-318** Settings MUST include Snippets management tab with CRUD UI (see FR-319..FR-320).

## Update: Hotkey Text Expansion (2025-01-09)
- **FR-286** System MUST support user-defined hotkeys (text shortcuts) that expand inline when trigger text is typed in editors, enabling rapid insertion of frequently-used phrases or templates without completion popup.
- **FR-287** Hotkey storage MUST use central database table `radium.hotkey` with columns: hotkey_id (BIGINT IDENTITY PK), account_id (BIGINT FK NOT NULL), trigger_text (NVARCHAR(64) NOT NULL), expansion_text (NVARCHAR(4000) NOT NULL), is_active (BIT NOT NULL DEFAULT 1), created_at, updated_at, rev. Unique constraint on (account_id, trigger_text).
- **FR-288** Hotkey service MUST provide async methods: GetActiveHotkeysAsync(accountId), UpsertHotkeyAsync(accountId, trigger, expansion), ToggleActiveAsync(accountId, hotkeyId), DeleteHotkeyAsync(accountId, hotkeyId). All operations MUST follow synchronous database → snapshot → UI flow similar to phrase service.
- **FR-289** Hotkey service MUST implement in-memory snapshot caching per account with automatic invalidation on upsert/toggle/delete and provide GetHotkeySnapshotAsync for completion integration.
- **FR-290** Editor text input handling MUST detect hotkey triggers by matching typed word against active hotkey trigger_text (case-insensitive prefix or exact match based on configuration) and replace trigger with expansion_text inline when space or punctuation follows. Hotkey expansion MUST take precedence over phrase completion popup.
- **FR-291** Settings window MUST include Hotkeys management tab with: DataGrid listing (Id, Trigger, Expansion preview, Active, Updated, Rev); Add controls (Trigger input, Expansion multi-line input, Add button); Refresh button; Delete button for selected row; Active checkbox toggle per row; Dark theme styling consistent with Phrases tab.
- FR-307 Hotkey schema MUST add column radium.hotkey.description NVARCHAR(256) NULL used for completion display.
- FR-308 Completion window MUST display hotkeys as "{trigger} → {description}" with fallback to first line of expansion when description is null/blank.
- FR-309 Settings → Hotkeys tab MUST provide a Description input textbox when adding a hotkey.
- FR-310 Hotkeys grid MUST include a Description column showing the stored description text.

## Update: Phrases tab shows account + global (2025-10-09)
- FR-283 Phrases tab MUST display both current account phrases and global phrases in a single list. Each row retains its scope via `account_id` (NULL = global). Toggling Active MUST call the appropriate scope-aware API (`ToggleActiveAsync(accountId? : null, id)`). Adding a new phrase from this tab MUST create an account-scoped phrase for the current account. Completion window MUST use combined phrases (global + account) for suggestions.
- FR-284 Phrases tab MUST include a read-only column `Global` showing a boolean derived from `account_id IS NULL` for quick scope recognition.

## Update: Global Phrase Conversion (2025-10-09)
- **FR-279** Global phrases admin MUST be able to convert selected account-specific phrases into global phrases from Settings → Global Phrases tab.
  - When converting multiple phrases with identical text from the same account, exactly one global row MUST remain (account_id = NULL) and the rest MUST be deleted.
  - When a global phrase with the same text already exists, selected account phrases with that text MUST be deleted (deduplicated) and no additional global rows created.
  - Operation MUST be synchronous and update in-memory snapshots and caches for both the account scope and the global scope on success; UI MUST reflect final snapshot state.
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
- **FR-162** AddStudy MUST validate that selected related study patient number/id matches current patient; on mismatch no ingestion occurs with explicit status message.
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
- **FR-337** Procedure operation Replace MUST be available with Arg1 Var, Arg2 String, Arg3 String replacing all occurrences.
- **FR-338** Procedure operation GetHTML MUST fetch and output HTML from Arg1 (URL in Var), errors preview `(error)`.
- **FR-343..FR-346** Split MUST support multiline separators, escapes, regex prefix, and preserve Arg3 index behavior (see updates above).
- **FR-347** Replace MUST support C#-style escapes in Arg2/Arg3 (search/replace strings).

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
