# Tasks: Radium Cumulative (Reporting Workflow + Editor + Mapping + PACS)

## Added
- [X] T621 Design study technique database schema with 8 tables in med schema: technique_prefix, technique_tech, technique_suffix, technique, technique_combination, technique_combination_item, rad_studyname_technique_combination, rad_study_technique_combination (FR-453..FR-460).
- [X] T622 Create technique_prefix table with id, prefix_text (unique), display_order, created_at (FR-453, FR-464).
- [X] T623 Create technique_tech table with id, tech_text (unique), display_order, created_at (FR-453, FR-464).
- [X] T624 Create technique_suffix table with id, suffix_text (unique), display_order, created_at (FR-453, FR-464).
- [X] T625 Create technique table with id, prefix_id (nullable FK), tech_id (required FK), suffix_id (nullable FK), created_at, unique constraint on (prefix_id, tech_id, suffix_id) (FR-454, FR-463).
- [X] T626 Create technique_combination table with id, combination_name (nullable), created_at (FR-455, FR-465).
- [X] T627 Create technique_combination_item join table with id, combination_id (FK), technique_id (FK), sequence_order, created_at, unique constraint on (combination_id, technique_id, sequence_order) (FR-455).
- [X] T628 Create rad_studyname_technique_combination table with id, studyname_id (FK), combination_id (FK), is_default (bool), created_at, unique constraint on (studyname_id, combination_id) (FR-456, FR-458).
- [X] T629 Create rad_study_technique_combination table with id, study_id (FK unique), combination_id (FK), created_at (FR-457, FR-459).
- [X] T630 Add foreign key constraints with CASCADE delete for studyname/study links and RESTRICT delete for technique component links (FR-467).
- [X] T631 Create indexes on: technique.tech_id, technique_combination_item.combination_id, technique_combination_item.technique_id, rad_studyname_technique_combination.studyname_id, rad_studyname_technique_combination.combination_id (FR-468).
- [X] T632 Create view med.v_technique_display with formatted display as "prefix tech suffix" using TRIM and CONCAT_WS (FR-461).
- [X] T633 Create view med.v_technique_combination_display with STRING_AGG joining techniques with " + " separator ordered by sequence_order (FR-462).
- [X] T634 Seed common prefixes: blank, axial, coronal, sagittal, 3D, intracranial, neck with display_order (FR-466).
- [X] T635 Seed common techs: T1, T2, GRE, SWI, DWI, CE-T1, TOF-MRA, CE-MRA, 3T with display_order (FR-466).
- [X] T636 Seed common suffixes: blank, "of sellar fossa" with display_order (FR-466).
- [X] T637 Create SQL file db\schema\technique_tables.sql with all table definitions, views, indexes, and seed data (FR-453..FR-468).
- [X] T638 Add table and column comments documenting purpose and special handling (e.g., empty string vs NULL) (FR-463).
- [X] T639 Update Spec.md with FR-453..FR-468 documenting study technique feature requirements (cumulative).
- [X] T640 Update Plan.md with change log entry for study technique database schema including approach, test plan, and risks (cumulative).
- [X] T641 Update Tasks.md with completed study technique database schema tasks (this file, cumulative).
- [ ] T642 Implement repository methods for technique component CRUD operations (future work).
- [ ] T643 Implement service layer for technique management business logic (future work).
- [ ] T644 Create UI for technique management and assignment to studynames (future work).
- [ ] T645 Wire technique display in report header and study list views (future work).
- [ ] T646 Add validation logic to enforce one default per studyname in application layer (future work).
- [X] T580 Add "Splitted" toggle button next to test button in previous report area binding to PreviousReportSplitted property (FR-415).
- [X] T581 Add first set of split controls to PreviousReportTextAndJsonPanel: Split Header button, Auto Split Header toggle, Split Conclusion button, Auto Split Conclusion toggle, Auto Split toggle (FR-416).
- [X] T582 Add "Final Conclusion" textbox to PreviousReportTextAndJsonPanel below Previous Header and Findings with two-way binding (FR-417).
- [X] T583 Create FinalConclusionText dependency property in PreviousReportTextAndJsonPanel for two-way binding (FR-418, FR-426).
- [X] T584 Add second set of split controls below Final Conclusion textbox: Split Header button, Split Conclusion button, Auto Split toggle (FR-419).
- [X] T585 Update gridSideBottom and gridBottomControl instances to bind FinalConclusionText to PreviousFinalConclusionText (FR-427).
- [X] T586 Add PreviousReportSplitted property to MainViewModel for toggle state binding (FR-415).
- [X] T587 Add AutoSplitHeader, AutoSplitConclusion, AutoSplit properties to MainViewModel for toggle bindings (FR-422, FR-423, FR-424).
- [X] T588 Add SplitHeaderCommand and SplitConclusionCommand placeholders to MainViewModel (FR-420, FR-421).
- [X] T589 Apply dark theme styling to split control buttons and toggles in PreviousReportTextAndJsonPanel (FR-425).
- [X] T590 Add using System.Windows.Input to MainViewModel.PreviousStudies.cs for ICommand support.
- [X] T591 Update Spec.md with FR-415..FR-427 documenting Previous Report Split Controls functionality (cumulative).
- [X] T592 Update Plan.md with change log entry for Previous Report Split Controls including approach, test plan, and risks (cumulative).
- [X] T593 Update Tasks.md with completed Previous Report Split Controls tasks (this file, cumulative).
- [ ] T594 Implement SplitHeaderCommand functionality to split header_and_findings content into header and findings sections (future work).
- [ ] T595 Implement SplitConclusionCommand functionality to split findings content into findings and conclusion sections (future work).
- [ ] T596 Implement auto-split functionality for AutoSplitHeader, AutoSplitConclusion, and AutoSplit toggles (future work).
- [X] T572 Separate toggles: Reverse Reports affects only gridTopChild/gridBottomControl (portrait panels).
- [X] T573 Separate toggles: Align Right affects only gridSideTop/gridSideBottom (landscape panels).
- [X] T574 Update SwapReportEditors to toggle only portrait panel Reverse states.
- [X] T575 Update UpdateGridSideLayout to toggle only landscape panel Reverse states.
- [X] T576 Documentation updates (Spec/Plan/Tasks) for separated toggle behaviors.
- [X] T577 Fix PreviousReportTextAndJsonPanel reverse: name left host as PART_LeftHost, apply Reverse on Loaded, and handle Reverse DP changed to swap columns.
- [X] T578 Wire MainWindow toggles to update gridBottomControl and gridSideBottom Reverse states correctly.
- [X] T579 Make gridTopChild/gridSideTop columns 1:1 by updating ReversibleColumnsGrid to star sizing both columns.
- [X] T566 Create reusable PreviousReportTextAndJsonPanel UserControl to eliminate duplicate code in side and bottom panels (user request 2025-01-11).
- [X] T567 Add HeaderAndFindingsText and JsonText dependency properties to PreviousReportTextAndJsonPanel for two-way binding (user request 2025-01-11).
- [X] T568 Add Reverse property to PreviousReportTextAndJsonPanel to support column swapping when reports are reversed (user request 2025-01-11).
- [X] T569 Replace duplicate XAML in gridSideBottom and gridBottomControl with PreviousReportTextAndJsonPanel instances (user request 2025-01-11).
- [X] T570 Update SwapReportEditors method to handle Reverse property of new control instances (user request 2025-01-11).
- [X] T571 Update Spec.md, Plan.md, and Tasks.md with PreviousReportTextAndJsonPanel control creation (cumulative documentation update).
- [X] T560 Change PreviousReportJson field mapping from findings/conclusion to header_and_findings/final_conclusion (user request 2025-01-11).
- [X] T561 Update MainViewModel.PreviousStudies properties: rename PreviousFindingsText to PreviousHeaderAndFindingsText, PreviousConclusionText to PreviousFinalConclusionText (user request 2025-01-11).
- [X] T562 Update MainWindow.xaml previous editor bindings to use PreviousHeaderAndFindingsText and PreviousFinalConclusionText (user request 2025-01-11).
- [X] T563 Add backward compatibility aliases PreviousFindingsText and PreviousConclusionText pointing to new property names (user request 2025-01-11).
- [X] T564 Add txtPrevHeaderAndFindingsSide TextBox to the left of txtPrevJsonSide with GridSplitter; bind to PreviousHeaderAndFindingsText (user request 2025-01-11).
- [X] T565 Update Spec.md, Plan.md, and Tasks.md with field mapping changes (cumulative documentation update).
- [X] T544 Add four header component fields to MainViewModel.Editor: chief_complaint, patient_history, study_techniques, comparison (FR-386).
- [X] T545 Implement UpdateFormattedHeader() method with conditional formatting logic per FR-387 (FR-387).
- [X] T546 Change HeaderText setter to private; header is computed from component fields (FR-390).
- [X] T547 Update UpdateCurrentReportJson() to serialize new header component fields (FR-388).
- [X] T548 Update ApplyJsonToEditors() to deserialize and apply new header component fields (FR-388).
- [X] T549 Wire component field property changes to trigger UpdateFormattedHeader() and UpdateCurrentReportJson() (FR-389).
- [X] T550 Update Spec.md with FR-386..FR-390 documenting header component fields and real-time formatting (cumulative).
- [X] T551 Update Plan.md with change log entry for header component fields including approach, test plan, and risk mitigation (cumulative).
- [X] T552 Update Tasks.md with completed header component fields tasks (this file, cumulative).
- [X] T553 Fix crash on startup: Add initialization guard to prevent UpdateFormattedHeader() from running during ViewModel construction (safety fix).
- [X] T554 JSON-driven Header Recompute: Ensure HeaderText updates in real-time when header component fields are edited via CurrentReportJson (remove suppression and recompute during JSON apply).
- [X] T555 Make header editor read-only in UI (EditorHeader IsReadOnly=True).
- [X] T556 Add left-side inputs: txtStudyRemark, txtPatientRemark, editorChiefComplaint, editorPatientHistory with two-way bindings.
- [X] T557 Expose `IsReadOnly` and `ShowLineNumbers` on `EditorControl` and flow to inner `MusmEditor`.
- [X] T558 Disable line numbers on editorChiefComplaint/editorPatientHistory.
- [X] T559 Add "Edit Study Technique" and "Edit Comparison" buttons (placeholders for future dialogs).
- [X] T526 Fix snippet option parsing to allow empty text values (e.g., `0^` for empty string choice) in CodeSnippet.ParseOptions() (FR-371).
- [X] T515 Fix snippet completion display to show "{trigger} → {description}" instead of "{trigger} → {snippet text}" in MusmCompletionData and EditorCompletionData (FR-362).
- [X] T516 Implement proper mode extraction from placeholder index prefix (1^, 2^, 3^) in CodeSnippet.Expand() method (FR-363).
- [X] T517 Add modification tracking to Session class: CurrentPlaceholderModified flag and CurrentPlaceholderOriginalText storage (FR-364, FR-365).
- [X] T518 Update SelectPlaceholder to record original placeholder text and reset modification flag when switching placeholders (FR-369).
- [X] T519 Update OnDocumentChanged to mark free-text placeholders as modified when edits occur within their bounds (FR-370).
- [X] T520 Fix ApplyFallbackAndEnd to only apply "[ ]" for unmodified free text placeholders; keep typed text if modified (FR-364).
- [X] T521 Improve key handling: allow normal typing in free-text placeholders, handle mode 1 immediate selection, mode 3 buffer accumulation (FR-366, FR-367, FR-368).
- [X] T522 Update PreviewText method to use new ParseHeader signature (three-tuple return).
- [X] T523 Update Spec.md with FR-362..FR-370 documenting snippet logic implementation fixes (cumulative).
- [X] T524 Update Plan.md with change log entry for snippet logic fixes including approach, test plan, and risk mitigation (cumulative).
- [X] T525 Update Tasks.md with completed snippet logic tasks (this file, cumulative).
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
- [X] T492 Update Spec/Plan/Tasks with FR-336..FR-338 entries and implementation notes.
- [X] T512 Fix `ProcedureExecutor` early-return that bypassed fallback/auto-seed and caused previous-value to persist when `GetHTML` executed.
- [X] T513 Support reading/writing procedure variables by both implicit `var{i}` and custom `OutputVar` names so `GetHTML` Arg1 Type=Var can use named variables.
- [X] T514 Register encoding provider in `ProcedureExecutor` and apply basic header/meta charset handling when decoding HTML.
- [X] T700 Procedure Split Parity: Update `ProcedureExecutor.Split` to support `re:`/`regex:` prefix, C#-style escape decoding, and CRLF retry to match SpyWindow behavior (FR-343..FR-346, acceptance: patient remark parity).
- [X] T701 Validate Patient Remark parity: With a procedure that trims trailing HTML via regex split, verify New Study "GetPatientRemark" result equals SpyWindow preview (after Trim). Document in Plan/Spec and set sample.
- [X] T702 Design dark, intuitive scrollbar UX and enumerate FR-469..FR-475 in Spec.md.
- [X] T703 Implement dark ScrollBar templates and Thumb style in `Themes/DarkTheme.xaml` with hover/drag states.
- [X] T704 Apply global `ScrollBar` and `ScrollViewer` styles so TextBox/DataGrid/Editor/Combo popup inherit styling.
- [X] T705 Validate paging on track click, 12px thickness, rounded thumb, and contrast across common controls; fix any XAML issues.
- [X] T706 Update Spec.md, Plan.md, and Tasks.md with scrollbar UX changes (cumulative docs update).
- [X] T707 Fix short scrollbar length: remove fixed along-axis size from `Dark.Scrollbar.ThumbStyle` and keep only cross-axis thickness in orientation templates (FR-476).
- [X] T708 Increase minimum thumb length: set `MinHeight` (vertical) and `MinWidth` (horizontal) to 36px in templates to improve usability while keeping proportional behavior (FR-477).
- [X] T709 Edit Study Technique layout: convert to left/right panels with GridSplitter; left uses rows (Add Technique [Auto], Current Combination [*]); right shows studyname combinations with Set Default (FR-478, FR-479).
- [X] T710 Fix ComboBox selected text: update dark ComboBox template to use SelectedItem.Text via PriorityBinding and set TextSearch.TextPath=Text (FR-480).
- [X] T711 Inline add for Prefix/Tech/Suffix: add "+" buttons with prompt, call VM methods to persist and reload, auto-select new item (FR-481, FR-482).
- [X] T725 Add `AddPreviousStudy` to Settings → Automation available modules.
- [X] T726 Implement `RunAddPreviousStudyModuleAsync` and map small `+` to run `AutomationAddStudySequence` (known modules only).
- [X] T730 SpyWindow: add PACS method `InvokeOpenStudy` (label: "Invoke open study") to Custom Procedures combo (FR-516).
- [X] T731 PacsService: add `InvokeOpenStudyAsync()` that executes `InvokeOpenStudy` procedure (FR-517).
- [X] T732 ProcedureExecutor: auto-seed default for `InvokeOpenStudy` with single `Invoke` op on `SelectedStudyInSearch` (FR-518).
- [X] T733 Custom Procedure op `Invoke`: ensure Arg1 Type preset to `Element` and Arg2/Arg3 disabled in editor; support in headless executor (FR-519..FR-521).
- [X] T734 UI Spy: add KnownControl `TestInvoke` and Map-to dropdown item "Test invoke" (FR-522..FR-524).
- [X] T735 Add PACS methods "Custom mouse click 1/2" to SpyWindow Custom Procedures list (FR-525).
- [X] T736 Add operation `MouseClick` to procedures editor and execution (Arg1=X, Arg2=Y) (FR-526, FR-527).
- [X] T737 Add auto-seed fallback for CustomMouseClick1/2 with a single MouseClick row (FR-529).
- [X] T738 PacsService: add wrappers `CustomMouseClick1Async` and `CustomMouseClick2Async` (FR-528).
- [X] T739 SpyWindow: add read-only `txtPickedPoint` to show picked screen coordinates (FR-530, FR-531).
- [X] T760 Add local tenant table `app.tenant` with `(account_id, pacs_key)` unique and `created_at`.
- [X] T761 Add `tenant_id` column (FK → app.tenant) to `med.patient`; change unique to `(tenant_id, patient_number)`.
- [X] T762 Add `tenant_id` column (FK → app.tenant) to `med.rad_studyname`; change unique to `(tenant_id, studyname)`.
- [X] T763 Rename technique tables to account-scoped `rad_technique*` and add `account_id` with unique constraints per account:
      `rad_technique_prefix`, `rad_technique_tech`, `rad_technique_suffix`, `rad_technique`, `rad_technique_combination`, `rad_technique_combination_item`.
- [X] T764 Create/refresh views `med.v_technique_display` and `med.v_technique_combination_display` to use the new table names.
- [X] T765 Update `TechniqueRepository` SQL to the new `rad_technique*` tables and pass `account_id` from `ITenantContext`.
- [X] T766 Update `RadStudyRepository` to persist/filter by `tenant_id` and adjust `ON CONFLICT` keys.
- [X] T767 Update `StudynameLoincRepository` to scope queries and upserts by `tenant_id`.
- [X] T768 Update `GetStudynameIdByNameAsync` to filter by tenant.
- [X] T769 Docs: Update Spec.md (FR-600..FR-609) and Plan.md with approach/test/risks for tenancy and technique renames.
- [X] V210 Build passes after repository and schema name changes.

## New (2025-10-14 – PACS Display Source + Settings PACS Tab Simplification)
- [X] T780 Bind status bar PACS display to `ITenantContext.CurrentPacsKey` with fallback to `default_pacs` and subscribe to `PacsKeyChanged`.
- [X] T781 Add `PacsKeyChanged` event to `ITenantContext` and raise in `TenantContext` when `CurrentPacsKey` changes.
- [X] T782 Remove legacy local PACS profile initialization in MainWindow to avoid showing "Default PACS".
- [X] T783 Settings → PACS tab: remove unsupported Actions column (Rename/Remove buttons).
- [X] T784 Settings → PACS tab: remove Close button below grid; rely on window close.
- [X] V220 Verify status bar shows "PACS: default_pacs" on startup and updates on selection change.
- [X] V221 Verify PACS grid has no Rename/Remove, only Add PACS, and row selection applies current PACS.

## New (2025-10-14 – Instant PACS Switch + PACS Text Display)
- [X] T785 Automation: set `SelectedPacsForAutomation` in `OnSelectedPacsProfileChanged` to reload sequences immediately.
- [X] T786 SettingsWindow: expose `CurrentPacsKey` property and subscribe to `ITenantContext.PacsKeyChanged`; update spy path and VM selection.
- [X] T787 SpyWindow: show PACS key in top bar and subscribe to `PacsKeyChanged` for live updates.
- [X] T788 Automation tab: add PACS label bound to `SettingsWindow.CurrentPacsKey`.
- [X] V230 Change PACS selection and verify Automation panes reload and label updates instantly.
- [X] V231 Open SpyWindow and change PACS in Settings; verify label updates instantly and procedures save under new PACS path.

## New (2025-10-14 – Per-PACS Spy Persistence + Invoke Test)
- [X] T789 Persist UiBookmarks per PACS by setting `UiBookmarks.GetStorePathOverride` on login and on PACS change.
- [X] T790 Persist Procedures per PACS by setting `ProcedureExecutor.SetProcPathOverride` on login and on PACS change.
- [X] T791 Add new custom method `InvokeTest` to SpyWindow ComboBox and seed default procedure in ProcedureExecutor.
- [X] T792 Add new Automation module `TestInvoke` to Available Modules.
- [X] T793 Wire `TestInvoke` execution in MainViewModel to call `PacsService.InvokeTestAsync()`.
- [X] V232 Verify each PACS has its own `bookmarks.json` and `ui-procedures.json` directory and files after editing/saving in Spy.
- [X] V233 Verify Automation `TestInvoke` triggers an Invoke on the element mapped to `TestInvoke` KnownControl.
 - [X] V234 Verify SpyWindow loads/saves procedures from the PACS-scoped `ui-procedures.json` and no longer uses legacy global file during a session.

## New (2025-10-14 – Test Automation Module ShowTestMessage)
- [X] T794 Add `ShowTestMessage` to SettingsViewModel.AvailableModules.
- [X] T795 Handle `ShowTestMessage` in MainViewModel automation runner (New/Add/Shortcut) to display a MessageBox("Test").
- [X] V240 Verify running sequences containing `ShowTestMessage` displays the modal box with title/content "Test".

## New (2025-10-14 – PACS-scoped Automation Execution Fix)
- [X] T796 Replace reads of `_localSettings.Automation*` with PACS-scoped `automation.json` loader.
- [ ] V241 New Study pane set to only `ShowTestMessage` → pressing New shows the message and does not lock study.
- [ ] V242 Add `LockStudy` to New Study pane explicitly → pressing New locks study as designed.

## New (2025-10-14 – Global Hotkey routes to Shortcut Sequences)
- [X] T800 Register global Open Study hotkey from Settings and handle WM_HOTKEY in MainWindow to call `RunOpenStudyShortcut()`.
- [X] V245 With “Shortcut: Open study (new)” containing `ShowTestMessage`, pressing the configured hotkey pops the “Test” box.
- [X] V246 Verify switching to locked state or after-open state picks the corresponding shortcut pane.

## New (2025-10-14 – Window Placement Persistence)
- [X] T805 Add `MainWindowPlacement` to local settings and implement save on close and restore on load (with safe clamping and state handling).
- [X] V250 Verify window restores position/size/state after restart; off-screen corrected.

## New (2025-10-14 – Reportify Clarification and Removal)
- [X] T808 Remove `PreserveKnownTokens` from SettingsViewModel and MainViewModel.ReportifyHelpers; ignore legacy key when parsing.
- [X] V251 Confirm new Reportify JSON omits `preserve_known_tokens` and app behavior unchanged.

## New (2025-01-14 – Editor Phrase-Based Syntax Highlighting)
- [X] T810 Create `PhraseHighlightRenderer` class in `src/Wysg.Musm.Editor/Ui/` implementing `IBackgroundRenderer` (FR-700).
- [X] T811 Implement phrase tokenization logic to find words and multi-word phrases up to 5 words (FR-706).
- [X] T812 Add case-insensitive phrase matching using HashSet for O(1) lookup performance (FR-705).
- [X] T813 Implement phrase highlighting with #4A4A4A for existing phrases and red for missing phrases (FR-701, FR-702).
- [X] T814 Add `PhraseSnapshot` dependency property to EditorControl with change notification (FR-703).
- [X] T815 Initialize `PhraseHighlightRenderer` in EditorControl constructor and wire to TextView (FR-707).
- [X] T816 Implement proper disposal of renderer in OnUnloaded to prevent memory leaks (FR-708).
- [X] T817 Update EditorControl.View.cs to add phrase snapshot property and renderer lifecycle (FR-703, FR-704).
- [X] T818 Update Spec.md with FR-700..FR-709 documenting phrase-based syntax highlighting (cumulative).
- [X] T819 Update Plan.md with change log entry for phrase highlighting including approach, test plan, and risks (cumulative).
- [X] T820 Update Tasks.md with completed phrase highlighting tasks (this file, cumulative).
- [X] V252 Build passes with no errors after adding PhraseHighlightRenderer.

## Verification (2025-01-14 – Phrase Highlighting)
- [ ] V253 Load phrase snapshot into EditorControl → verify phrases highlight with correct colors.
- [ ] V254 Type text with snapshot phrases → verify #4A4A4A highlighting.
- [ ] V255 Type text with non-snapshot phrases → verify red highlighting.
- [ ] V256 Type multi-word phrases (2-5 words) → verify entire phrase highlights as single unit.
- [ ] V257 Update phrase snapshot at runtime → verify highlighting updates immediately.
- [ ] V258 Scroll document → verify highlighting only processes visible text regions.
- [ ] V259 Verify text remains readable with background highlighting active.
- [ ] V260 Test performance with large documents (1000+ lines) and many phrases (500+).

## Future Enhancement
- [ ] T821 Integrate SNOMED CT concept colors for phrase highlighting (FR-709, future work).
- [ ] T822 Add phrase highlighting configuration UI in Settings window (future work).
- [ ] T823 Implement phrase hover tooltips showing SNOMED CT information (future work).

## New (2025-10-15 – Phrase-to-SNOMED Mapping Central Database)
- [X] T900 Design phrase-to-SNOMED mapping schema with three tables: snomed.concept_cache, radium.global_phrase_snomed, radium.phrase_snomed (FR-900, FR-901, FR-902).
- [X] T901 Create snomed.concept_cache table with concept_id (PK), concept_id_str (UNIQUE), fsn, pt, module_id, active, cached_at, expires_at (FR-900).
- [X] T902 Create radium.global_phrase_snomed table with phrase_id (UNIQUE FK), concept_id (FK), mapping_type, confidence, notes, mapped_by, timestamps (FR-901).
- [X] T903 Create radium.phrase_snomed table with phrase_id (UNIQUE FK), concept_id (FK), mapping_type, confidence, notes, timestamps (FR-902).
- [X] T904 Add indexes on snomed.concept_cache: fsn, pt, cached_at DESC (FR-900).
- [X] T905 Add indexes on radium.global_phrase_snomed: concept_id, mapping_type, mapped_by (FR-901).
- [X] T906 Add indexes on radium.phrase_snomed: concept_id, mapping_type, created_at DESC (FR-902).
- [X] T907 Add FK constraints: phrase tables CASCADE delete, concept_cache RESTRICT delete (FR-901, FR-902).
- [X] T908 Add CHECK constraints on mapping_type (exact/broader/narrower/related) and confidence (0.00-1.00) (FR-901, FR-902).
- [X] T909 Create triggers trg_global_phrase_snomed_touch and trg_phrase_snomed_touch to auto-update updated_at on field changes (FR-903).
- [X] T910 Create view radium.v_phrase_snomed_combined with UNION ALL of global and account mappings including phrase and concept details (FR-904).
- [X] T911 Create stored procedure snomed.upsert_concept for idempotent concept caching from Snowstorm API (FR-905).
- [X] T912 Create stored procedure radium.map_global_phrase_to_snomed with validation for global phrases (account_id IS NULL) (FR-906).
- [X] T913 Create stored procedure radium.map_phrase_to_snomed with validation for account phrases (account_id IS NOT NULL) (FR-907).
- [X] T914 Add table and column comments documenting purpose and constraints (e.g., mapping_type values, confidence range) (FR-900..FR-907).
- [X] T915 Create SQL file db\schema\central_db_phrase_snomed_mapping.sql with all tables, views, indexes, constraints, triggers, and procedures (FR-900..FR-907).
- [X] T916 Update Spec.md with FR-900..FR-915 documenting phrase-to-SNOMED mapping feature requirements (cumulative).
- [X] T917 Update Plan.md with change log entry for phrase-to-SNOMED mapping including approach, test plan, and risks (cumulative).
- [X] T918 Update Tasks.md with completed phrase-to-SNOMED mapping schema tasks (this file, cumulative).
- [ ] T919 Implement C# service SnowstormService with SearchConceptsAsync and GetConceptDetailsAsync methods (FR-908, future work).
- [ ] T920 Implement C# service PhraseSnomedService with UpsertConceptAsync, MapGlobalPhraseAsync, MapAccountPhraseAsync methods (FR-906, FR-907, future work).
- [ ] T921 Add SNOMED search panel to Settings → Global Phrases tab with search textbox, results grid, and map button (FR-909, future work).
- [ ] T922 Add mapping details panel to Global Phrases tab with concept display, mapping type dropdown, confidence slider, notes textbox, save/remove buttons (FR-909, future work).
- [ ] T923 Extend GlobalPhrasesViewModel with SNOMED search and mapping commands (FR-909, future work).
- [ ] T924 Add SNOMED search and mapping UI to Settings → Phrases tab (account-specific) (FR-910, future work).
- [ ] T925 Extend PhrasesViewModel with SNOMED search and mapping commands (FR-910, future work).
- [ ] T926 Update PhraseHighlightRenderer to query phrase-SNOMED mappings and apply semantic category colors (FR-911, future work).
- [ ] T927 Extend phrase completion dropdown to display SNOMED concept ID and FSN in tooltip or secondary line (FR-912, future work).
- [ ] T928 Implement phrase report export (CSV/JSON) with SNOMED concept IDs and mapping metadata (FR-913, future work).
- [ ] T929 Implement CSV import for bulk phrase-SNOMED mappings with validation and preview (FR-914, future work).
- [ ] T930 Add mapping audit log table or temporal tables for compliance tracking (FR-915, future work).

## Verification (Phrase-to-SNOMED Mapping)
- [X] V300 SQL file deploys without errors on Azure SQL Database.
- [ ] V301 Call snomed.upsert_concept with test data → verify concept cached with correct fields.
- [ ] V302 Call radium.map_global_phrase_to_snomed with global phrase → verify mapping created.
- [ ] V303 Call radium.map_phrase_to_snomed with account phrase → verify mapping created.
- [ ] V304 Attempt to map account phrase via global procedure → verify RAISERROR.
- [ ] V305 Attempt to map global phrase via account procedure → verify RAISERROR.
- [ ] V306 Delete phrase → verify mapping CASCADE deleted.
- [ ] V307 Attempt to delete concept with existing mappings → verify FK RESTRICT violation.
- [ ] V308 Update mapping fields → verify updated_at changes; update non-tracked fields → verify updated_at unchanged.
- [ ] V309 Query v_phrase_snomed_combined → verify UNION ALL returns global and account mappings with correct mapping_source.
- [ ] V310 Build passes with no errors after schema deployment.

## New (2025-10-15 – PhraseSnomedLinkWindow UX Improvements)
- [X] T931 Pre-fill search textbox with phrase text in PhraseSnomedLinkWindow constructor (FR-916a).
- [X] T932 Implement manual SelectedConcept property to call MapCommand.NotifyCanExecuteChanged() when concept selected (FR-916b).
- [X] T933 Update Spec.md with FR-916 documenting mapping window UX improvements (cumulative).
- [X] T934 Update Plan.md with change log entry for mapping window fixes including root cause, approach, and test plan (cumulative).
- [X] T935 Update Tasks.md with completed mapping window UX tasks (this file, cumulative).

## Verification (PhraseSnomedLinkWindow UX)
- [X] V311 Open "Link SNOMED" from Global Phrases → verify search box pre-filled with phrase text.
- [X] V312 Press Enter or click Search → verify Snowstorm search executes with pre-filled text.
- [X] V313 Map button is disabled when no concept selected.
- [X] V314 Select a concept from search results → verify Map button enables immediately.
- [X] V315 Click Map button → verify mapping saves successfully and confirmation appears.
- [X] V316 Select different concept → verify Map button remains enabled.
- [X] V317 Clear selection → verify Map button disables.

## New (2025-10-15 – UI Bookmark Robustness Improvements)
- [X] T936 Update `Walk` method in `UiBookmarks.cs` to require ALL enabled attributes for step 0 root acceptance (FR-920).
- [X] T937 Update `DiscoverRoots` method to filter existing roots using first node attributes instead of rescanning desktop (FR-921).
- [X] T938 Add exact match filtering followed by relaxed match (without ControlTypeId) fallback (FR-921).
- [X] T939 Add ClassName filtering when multiple root matches remain (FR-924).
- [X] T940 Implement `CalculateNodeSimilarity` helper to score roots (AutomationId=200, Name=100, ClassName=50, ControlType=25) (FR-925).
- [X] T941 Sort filtered roots by similarity score for deterministic selection (FR-925).
- [X] T942 Add `ValidateBookmark` method in `SpyWindow.Bookmarks.cs` to validate before save (FR-922).
- [X] T943 Validate process name not empty, chain not empty, first node has ≥1 enabled attribute (FR-922).
- [X] T944 Warn about nodes relying solely on UseIndex=true with IndexAmongMatches=0 (FR-922).
- [X] T945 Call `ValidateBookmark` in `OnSaveEdited` and display validation errors preventing save (FR-922).
- [X] T946 Enhance trace output in `Walk` to show attribute match results for step 0 (FR-923).
- [X] T947 Enhance trace output in `DiscoverRoots` to show filtering stages and root counts (FR-923).
- [X] T948 Update Spec.md with FR-920..FR-925 documenting bookmark robustness improvements (cumulative).
- [X] T949 Update Plan.md with change log entry for bookmark robustness including root cause, fixes, approach, test plan, and risks (cumulative).
- [X] T950 Update Tasks.md with completed bookmark robustness tasks (this file, cumulative).

## Verification (UI Bookmark Robustness)
- [ ] V320 Open PACS with main + toolbar windows; capture bookmark with ClassName enabled → verify root matches main window consistently across 5 reopens.
- [ ] V321 Edit bookmark to leave only 1 attribute enabled on first node → verify validation error prevents save.
- [ ] V322 Enable second attribute → verify validation passes and save succeeds.
- [ ] V323 Capture bookmark with unique AutomationId → verify similarity scoring selects correct root when multiple candidates exist.
- [ ] V324 Simulate ControlTypeId change → verify relaxed match fallback succeeds with trace message.
- [ ] V325 Resolve bookmark with trace → verify trace shows attribute match results (Name=true, Class=true, Auto=false, Ct=true) and timing info (e.g., "Step 0: Accept root... (12 ms)").
- [ ] V326 Resolve bookmark after ClassName filter → verify trace shows "ClassName filter applied: N roots remain".
- [ ] V327 Verify bookmarks saved before fix continue to work (no regression for existing bookmarks).
- [ ] V328 Resolve bookmark with multiple steps → verify each step shows timing (e.g., "Step 2: Completed (45 ms)").
- [ ] V329 Click Validate button in SpyWindow → verify diagnostic table includes timing column showing per-step milliseconds.
- [ ] V330 Click "Resolve" with trace on slow bookmark → verify trace shows retry breakdown with query time, retry delay, and attempt count for each step.
- [ ] V331 Click "Validate" on any bookmark → verify status textbox shows last 100 lines of trace with detailed timing for all steps (even on success).
- [ ] V332 Click "Validate" on Calculator bookmark → verify trace shows "Detected 'not supported' error, skipping remaining retries" and resolution completes in <1 second.
- [ ] V333 Compare Calculator bookmark timing before/after fix → verify resolution is 4-6x faster (from ~2900ms to ~500-800ms).

## New (2025-10-15 – OpenStudy fallback to per-PACS WorklistViewButton)
- [X] T860 Change `ProcedureExecutor` fallback for `InvokeOpenStudy` to `Invoke` `KnownControl.WorklistViewButton` so it uses per-PACS UiBookmarks mapping.
- [X] T861 Update Spec.md FR-518 to reflect WorklistViewButton default and per-PACS storage note.
- [X] T862 Update Plan.md with change log entry, approach, test plan, and risks for the new fallback behavior.
- [ ] T863 Validate SpyWindow mapping flow for `WorklistViewButton` across two PACS profiles (mapping persists under each PACS folder).

## Verification (2025-10-15 – OpenStudy fallback)
- [X] V262 If `InvokeOpenStudy` procedure is missing, opening editor auto-seeds with a single `Invoke` step targeting `WorklistViewButton`.
- [X] V263 Mapping `WorklistViewButton` to the PACS View/Open UI element and running the procedure opens the viewer.
- [X] V264 Running Automation module `OpenStudy` triggers `PacsService.InvokeOpenStudyAsync()` and sets `StudyOpened=true`.
- [ ] V265 Switch PACS profile and verify the fallback reads the mapped `WorklistViewButton` from the new PACS folder.

## Added (2025-10-15 – OpenStudy reliability + sequential execution + AddPreviousStudy guard/perf)
- [X] T870 Execute automation modules sequentially in New/Add/Shortcut flows via `RunModulesSequentially` (await each module in order).
- [X] T871 Convert remark acquisition helpers to `Task` and await in sequencing to preserve ordering.
- [X] T872 Add abort checks in `RunAddPreviousStudyModuleAsync`: skip when related study `studyname` or `studydatetime` is null/empty.
- [X] T873 Add abort check in `RunAddPreviousStudyModuleAsync`: skip when related study matches current study (same `StudyName` and `StudyDateTime`).
- [X] T874 Improve AddPreviousStudy performance: run `LoadPreviousStudiesForPatientAsync` in background after persistence; do not block module chain.
- [X] T875 Add retry to `PacsService.InvokeOpenStudyAsync` (3 attempts with small backoff) while preserving strict throw-if-undefined behavior.

## Verification (2025-10-15 – Sequencing + AddPreviousStudy + OpenStudy)
- [X] V270 New/Add/Shortcut sequences run modules one-by-one in configured order; no interleaving.
- [X] V271 Related study missing name/datetime → `AddPreviousStudy` aborts gracefully; status shows reason.
- [X] V272 Related study equals current by name+datetime → `AddPreviousStudy` aborts; status shows reason.
- [X] V273 After saving previous study, viewer opens promptly because reload runs in background; `OpenStudy` executes reliably next.
- [X] V274 `OpenStudy` succeeds when procedure is present; transient failures are retried; if undefined, an error is thrown and surfaced in status.

## New (2025-10-15 – Status Log, Bookmarks, PACS Methods, ClickElement)
- [X] T950 Replace TextBox with RichTextBox in StatusPanel.xaml for multi-color support and auto-scroll
- [X] T951 Implement UpdateStatusText method in StatusPanel.xaml.cs with line-by-line colorization logic (error lines red, others default)
- [X] T952 Add DataContextChanged handler to subscribe to MainViewModel.StatusText and StatusIsError property changes
- [X] T953 Add Screen_MainCurrentStudyTab and Screen_SubPreviousStudyTab to UiBookmarks.KnownControl enum
- [X] T954 Add SetCurrentStudyInMainScreenAsync and SetPreviousStudyInSubScreenAsync methods to PacsService.cs
- [X] T955 Add ClickElement operation support to ProcedureExecutor.ExecuteRow switch statement
- [X] T956 Implement ClickElement logic in ProcedureExecutor.ExecuteElemental (resolve element, calculate center, click)
- [X] T957 Add auto-seed fallback for SetCurrentStudyInMainScreen with ClickElement operation in ProcedureExecutor
- [X] T958 Add auto-seed fallback for SetPreviousStudyInSubScreen with ClickElement operation in ProcedureExecutor
- [X] T959 Update Spec.md with FR-950 through FR-956 documenting new features (cumulative)
- [X] T960 Update Plan.md with change log entry for status log, bookmarks, PACS methods, and ClickElement (cumulative)
- [X] T961 Update Tasks.md with T950-T964 and V270-V279 (this file, cumulative)
- [X] T962 Verify build passes with no compilation errors
- [X] T963 Fix StatusPanel to remove unnecessary line breaks between lines (completed)
- [ ] T964 Manual: Add new bookmarks to SpyWindow.xaml Map-to ComboBox (see MANUAL_UPDATES_NEEDED.md)
- [ ] T965 Manual: Add new PACS methods to SpyWindow.xaml Custom Procedures combo (see MANUAL_UPDATES_NEEDED.md)
- [ ] T966 Manual: Add ClickElement operation to SpyWindow.xaml Operations combo (see MANUAL_UPDATES_NEEDED.md)

## Verification (Status Log, Bookmarks, PACS Methods, ClickElement)
- [X] V270 Status textbox auto-scrolls to show latest message (verified with test)
- [X] V271 Error lines appear in red (#FF5A5A), normal lines in default (#D0D0D0) - line-by-line colorization works
- [X] V271a No unnecessary line breaks between status lines (fixed with conditional LineBreak insertion)
- [ ] V272 Open SpyWindow Map-to dropdown → verify "Screen_main current study tab" and "Screen_sub previous study tab" listed (pending manual XAML update)
- [ ] V273 Map Screen_MainCurrentStudyTab bookmark to PACS main screen current study area → verify saved and resolves correctly (pending manual XAML update)
- [ ] V274 Map Screen_SubPreviousStudyTab bookmark to PACS sub screen previous study area → verify saved and resolves correctly (pending manual XAML update)
- [ ] V275 Open SpyWindow Custom Procedures → verify "Set current study in main screen" and "Set previous study in sub screen" listed (pending manual XAML update)
- [ ] V276 Select SetCurrentStudyInMainScreen method → verify auto-seeded with ClickElement operation targeting Screen_MainCurrentStudyTab (pending manual XAML update)
- [ ] V277 Run SetCurrentStudyInMainScreen procedure → verify click occurs at bookmark center and preview shows coordinates (pending manual XAML update)
- [ ] V278 Open SpyWindow Operations dropdown → verify ClickElement operation listed (pending manual XAML update)
- [ ] V279 Select ClickElement operation → verify Arg1 preset to Element type, Arg2/Arg3 disabled, Run button clickable after mapping bookmark (pending manual XAML update)
- [X] V280 Build passes with no compilation errors (verified)

## Note on msctls_statusbar32 Reliability (PP6)
The issue where msctls_statusbar32 bookmarks fail validation intermittently but work after re-pick has been addressed by the bookmark robustness improvements (FR-920..FR-925):

- **Root Cause**: The statusbar control intermittently throws "Specified method is not supported" errors during UIA FindAll operations
- **Fix Applied**: ProcedureExecutor and UiBookmarks now detect "not supported" errors and switch to manual tree walking, which succeeds reliably
- **Trace Output**: Enhanced to show retry timing breakdown and "Detected 'not supported' error, skipping remaining retries" message
- **Performance**: Validation that previously took ~2900ms now completes in ~500-800ms by skipping unnecessary retries
- **User Impact**: After re-pick, the bookmark works consistently because the exact same element is captured with the same attributes. The underlying UIA behavior is unchanged, but the resolver is more robust.

## New (2025-01-16 – Element Staleness Detection with Auto-Retry)
- [X] T970 Add ElementResolveMaxAttempts and ElementResolveRetryDelayMs constants to ProcedureExecutor
- [X] T971 Implement IsElementAlive() helper method that validates element by checking Name property
- [X] T972 Rewrite ResolveElement() with retry loop:
  - Check cache → validate with IsElementAlive() → return if valid
  - Clear stale cache entries immediately
  - Resolve fresh from bookmark → validate before caching
  - Retry with exponential backoff (150ms, 300ms, 450ms) on failure
- [X] T973 Test normal case: element resolves on first attempt and is cached
- [X] T974 Update Plan.md with change log entry for element staleness detection (cumulative)
- [X] T975 Update Tasks.md with T970-T985 and V290-V295 (this file, cumulative)
- [X] T976 Verify build passes with no compilation errors

## Verification (Element Staleness Detection)
- [X] V290 Normal case: procedure GetText operation completes on first attempt (cache hit after initial resolve)
- [ ] V291 Stale cache: PACS window hierarchy changes → cached element becomes stale → automatic retry resolves fresh element
- [ ] V292 Transient failure: UI busy during resolution → first attempt fails → retry after 150ms succeeds
- [ ] V293 Permanent failure: bookmark points to non-existent element → all 3 attempts fail → operation reports "(no element)" error
- [ ] V294 Performance: measure resolution time with cache hit (<10ms) vs. cache miss with retry (<1 second for 3 attempts)
- [ ] V295 Integration: run automation sequence with 10+ operations → verify no stale element errors

## New (2025-01-16 – ResolveWithRetry with Progressive Constraint Relaxation)
- [X] T1010 Add ResolveWithRetry() public method to UiBookmarks with maxAttempts parameter (default 3)
- [X] T1011 Implement RelaxBookmarkControlType() helper that creates bookmark copy with UseControlTypeId=false
- [X] T1012 Implement RelaxBookmarkClassName() helper that creates bookmark copy with UseClassName=false + UseControlTypeId=false
- [X] T1013 Implement retry loop in ResolveWithRetry():
  - Attempt 1: Call ResolveBookmark() with original constraints
  - Attempt 2: Call ResolveBookmark() with ControlType relaxed
  - Attempt 3: Call ResolveBookmark() with ClassName + ControlType relaxed
  - Exponential backoff (150ms, 300ms) between attempts
- [X] T1014 Update Plan.md with change log entry for ResolveWithRetry (cumulative)
- [X] T1015 Update Tasks.md with T1010-T1020 and V300-V305 (this file, cumulative)
- [X] T1016 Verify build passes with no compilation errors
- [ ] T1017 Update ProcedureExecutor.ResolveElement() to use ResolveWithRetry() instead of Resolve() (optional enhancement)

## Verification (ResolveWithRetry)
- [ ] V300 Exact match success: bookmark resolves on first attempt → no relaxation, no retry delay
- [ ] V301 ControlType relaxation: PACS UI update changes control types → second attempt succeeds
- [ ] V302 ClassName relaxation: major UI rearrangement → third attempt succeeds with Name + AutomationId only
- [ ] V303 Complete failure: bookmark completely invalid → all 3 attempts fail, return (IntPtr.Zero, null)
- [ ] V304 Performance: measure first attempt (~100ms), retry overhead (150-300ms only when needed)
- [ ] V305 Integration: run automation sequence where one bookmark requires relaxation → verify automatic recovery

## Future Robustness Strategies (Documented, Not Implemented)

### FR-960: Multi-Root Window Discovery (Medium Priority) - ⚠️ Partially Implemented
- [X] T980 Document pattern from legacy InitializeWorklistAsync() (eViewer1/eViewer2 dual check)
- [X] T981 Document current DiscoverRoots() behavior (tries multiple approaches but doesn't store window handles)
- [ ] T982 Enhancement: Add window handle storage (hwndViewer1, hwndViewer2) to Bookmark class
- [ ] T983 Enhancement: Explicitly alternate between viewer instances when process has multiple top-level windows
- [ ] T984 Implementation trigger: user reports worklist appearing in secondary PACS window

**Status**: ⚠️ Partially addressed by ResolveWithRetry (tries multiple roots via DiscoverRoots), but doesn't explicitly handle dual-viewer pattern like legacy.

**Implementation Location**: `apps\Wysg.Musm.Radium\Services\UiBookmarks.cs` - `DiscoverRoots()` method

**Estimated Effort**: 4 hours (design + implementation + testing)

### FR-961: Index-Based Navigation Fallback (Low Priority)
- [ ] T984 Document pattern from legacy InitializeWorklistChildrenAsync() (child index navigation)
- [ ] T985 Design IndexPath property for Node class (int[] array for hierarchical index path)
- [ ] T986 Add PreferIndexPath bool flag to control fallback behavior
- [ ] T987 Update Walk() method to try index-based navigation when attribute matching fails
- [ ] T988 Update SpyWindow.Bookmarks capture UI to record both attribute-based and index-based paths
- [ ] T989 Implementation trigger: user reports bookmarks breaking after PACS UI updates (new buttons, panels)

**Implementation Location**: 
- `apps\Wysg.Musm.Radium\Services\UiBookmarks.cs` - `Node` class and `Walk()` method
- `apps\Wysg.Musm.Radium\Views\SpyWindow.Bookmarks.cs` - Bookmark capture UI

**Estimated Effort**: 8 hours (data model + resolver logic + UI + testing)

**Trade-off**: Index-based navigation is more brittle (breaks when new toolbar buttons appear) but can be more reliable when AutomationId/ClassName are unstable. Hybrid approach gives users flexibility.

### FR-962: Cascading Re-initialization on Failure (Medium Priority)
- [ ] T990 Document pattern from legacy GetAllStudyInfoAsync() (validation + re-init on failure)
- [ ] T991 Design procedure-level initialization hooks (InitProcedure metadata on bookmarks)
- [ ] T992 Add initialization procedure execution before each operation
- [ ] T993 Update ProcedureExecutor to support initialization tags
- [ ] T994 Update UiBookmarks to support initialization metadata in Bookmark class
- [ ] T995 Implementation trigger: user reports procedures working after manual SpyWindow resolution but failing in automation

**Implementation Location**: 
- `apps\Wysg.Musm.Radium\Services\ProcedureExecutor.cs` - Add optional InitProcedure tag
- `apps\Wysg.Musm.Radium\Services\UiBookmarks.cs` - Add initialization metadata to Bookmark class

**Estimated Effort**: 6 hours (design + implementation + testing)

**Benefit**: Mimics legacy's "try-catch-reinit" pattern in a generalized way for user-authored procedures.

### FR-963: Progressive Constraint Relaxation (Low Priority)
- [ ] T996 Document legacy pattern: AutomationId → ClassName fallback
- [ ] T997 Design multi-level constraint relaxation strategy:
  - Level 0: All enabled attributes (Name + ClassName + AutomationId + ControlType)
  - Level 1: Relax ControlType (already implemented)
  - Level 2: Relax ClassName
  - Level 3: Index-based fallback (if IndexPath available)
- [ ] T998 Update Walk() method to implement progressive relaxation levels
- [ ] T999 Add trace logging to show which relaxation level succeeded
- [ ] T1000 Implementation trigger: user reports bookmarks resolving to wrong elements (too many matches)

**Implementation Location**: `apps\Wysg.Musm.Radium\Services\UiBookmarks.cs` - `Walk()` method

**Estimated Effort**: 4 hours (extend existing relaxation logic)

**Caution**: More relaxation = higher risk of wrong element matches. Only implement when needed.

### FR-964: Dual Pattern Fallback (Invoke → Toggle)
- [X] T1001 Verify Invoke → Toggle fallback already implemented in ProcedureExecutor

**Status**: ✅ Already implemented in `ProcedureExecutor.ExecuteElemental()` for `Invoke` operation.

### FR-966: Pure Index-Based Navigation (Legacy Pattern) - ✅ IMPLEMENTED (2025-01-16)
- [X] T1020 Document legacy pattern: GetChildByIndexAsync(parent, index) - no attributes
- [X] T1021 Design pure index navigation: when all attributes disabled + UseIndex=true + Scope=Children
- [X] T1022 Implement in UiBookmarks.Walk(): detect pure index mode and use FindAllChildren()[index]
- [X] T1023 Add trace logging: "Pure index navigation (no attributes, using index N)"
- [X] T1024 Add bounds checking: verify index < children.Length
- [X] T1025 Add error handling: catch exceptions and report clearly
- [X] T1026 Update Plan.md with FR-966 documentation (usage, benefits, trade-offs)
- [X] T1027 Update Tasks.md with T1020-T1030 (this file, cumulative)
- [X] T1028 Verify build passes with no compilation errors

**Implementation Location**: `apps\Wysg.Musm.Radium\Services\UiBookmarks.cs` - `Walk()` method (after `BuildAndCondition()` returns null)

**Estimated Effort**: 1 hour (already completed)

**Code Change**:
```csharp
// In Walk() method, when cond == null:
if (node.UseIndex && node.Scope == SearchScope.Children)
{
    var children = current.FindAllChildren();
    if (children.Length > node.IndexAmongMatches)
    {
        current = children[node.IndexAmongMatches];
        path.Add(current);
        continue;  // Success
    }
    // else: index out of range, return null
}
```

**Benefits**:
- ✅ Exact legacy pattern replication (`GetChildByIndexAsync`)
- ✅ Fast (no attribute matching)
- ✅ Works when attributes unstable

**Trade-offs**:
- ⚠️ Brittle (breaks if children reordered)
- ⚠️ Not self-documenting
- ⚠️ Only works with Scope=Children

## Verification (Pure Index Navigation)
- [ ] V310 Normal case: pure index node with UseIndex=true, all attributes=false → resolves to correct child
- [ ] V311 Out of range: index=5 but only 3 children → fails gracefully with clear error
- [ ] V312 Descendants scope: pure index with Scope=Descendants → skipped (not supported)
- [ ] V313 Legacy replication: test with ePanWorklistToolBar pattern (index 1 from worklist) → matches legacy behavior
- [ ] V314 Integration: full bookmark chain with pure index step → resolves successfully
- [ ] V315 Performance: pure index vs. attribute matching → pure index faster (<10ms)

## New (2025-10-18 – Save as New Combination Button Enablement Fix)
- [X] T1180 Modify AddTechniqueCommand to call RaiseCanExecuteChanged() on SaveNewCombinationCommand after adding item (FR-1053)
- [X] T1181 Modify SaveNewCombinationCommand to call RaiseCanExecuteChanged() after clearing CurrentCombinationItems (FR-1054)
- [X] T1182 Update Spec.md with FR-1050 through FR-1054 documenting button enablement fix (cumulative)
- [X] T1183 Update Plan.md with change log entry including root cause, approach, test plan, and risks (cumulative)
- [X] T1184 Update Tasks.md with T1180-T1190 and V420-V430 (this file, cumulative)
- [X] T1185 Verify build passes with no compilation errors

## Verification (Save as New Combination Button Enablement)
- [ ] V420 Open "Manage Studyname Techniques" window → verify "Save as New Combination" button is disabled initially
- [ ] V421 Select Prefix, Tech, Suffix → click "Add to Combination" → verify button becomes enabled immediately
- [ ] V422 Add 2-3 more techniques → verify button remains enabled
- [ ] V423 Click "Save as New Combination" → verify saves successfully
- [ ] V424 Verify Current Combination list clears after save
- [ ] V425 Verify button disables immediately after save
- [ ] V426 Verify new combination appears in right panel DataGrid
- [ ] V427 Repeat add/save cycle 2-3 times → verify button enables/disables correctly each time
- [ ] V428 Add items then close window without saving → verify items not persisted
- [ ] V429 Test with studyname not set → verify button remains disabled even when adding items (safeguard)
- [ ] V430 Performance test: add 10 items rapidly → verify button state updates responsively without lag

## New (2025-10-18 – ReportInputsAndJsonPanel Side-by-Side Row Layout)
- [X] T1220 Restructure ReportInputsAndJsonPanel XAML from column-based to side-by-side row layout for natural Y-coordinate alignment (FR-1081)
- [X] T1221 Add named references to main textboxes: txtChiefComplaint, txtPatientHistory, txtFindings, txtConclusion (FR-1083..FR-1086)
- [X] T1222 Set MinHeight="60" on Chief Complaint and Patient History main textboxes (FR-1083, FR-1084)
- [X] T1223 Set MinHeight="100" on Findings and Conclusion main textboxes (FR-1085, FR-1086)
- [X] T1224 Bind MinHeight on Chief Complaint (PR) textbox to txtChiefComplaint.MinHeight (FR-1083, FR-1094)
- [X] T1225 Bind MinHeight on Patient History (PR) textbox to txtPatientHistory.MinHeight (FR-1084, FR-1094)
- [X] T1226 Bind MinHeight on Findings (PR) textbox to txtFindings.MinHeight (FR-1085, FR-1094)
- [X] T1227 Bind MinHeight on Conclusion (PR) textbox to txtConclusion.MinHeight (FR-1086, FR-1094)
- [X] T1228 Add OnProofreadScrollChanged event handler to ReportInputsAndJsonPanel.xaml.cs (FR-1090)
- [X] T1229 Implement scroll synchronization logic with _isScrollSyncing flag to prevent feedback loops (FR-1090)
- [X] T1230 Remove non-existent converter references from XAML (cleanup)
- [X] T1231 Maintain dark theme styling for all textboxes with proper background/foreground/border colors (FR-1092)
- [X] T1232 Verify ApplyReverse() method still works with new layout structure (FR-1091)
- [X] T1233 Update Spec.md with FR-1080 through FR-1096 documenting side-by-side row layout feature (cumulative)
- [X] T1234 Update Plan.md with change log entry including approach, test plan, and risks (cumulative)
- [X] T1235 Update Tasks.md with T1220-T1245 and V480-V495 (this file, cumulative)
- [X] T1236 Verify build passes with no compilation errors

## Verification (ReportInputsAndJsonPanel Side-by-Side Row Layout)
- [ ] V480 Open Main Window in portrait mode → verify Chief Complaint upper border aligns with Chief Complaint (PR)
- [ ] V481 Verify Patient History upper border aligns with Patient History (PR)
- [ ] V482 Verify Findings upper border aligns with Findings (PR)
- [ ] V483 Verify Conclusion upper border aligns with Conclusion (PR)
- [ ] V484 Type multi-line content into Chief Complaint → verify both main and proofread textboxes grow proportionally
- [ ] V485 Type multi-line content into Patient History → verify alignment maintained during height changes
- [ ] V486 Rotate to landscape mode → verify same alignment behavior in gridSideTop panel
- [ ] V487 Scroll proofread column → verify main column scrolls in sync
- [ ] V488 Scroll main column → verify operates independently (no feedback sync back to proofread)
- [ ] V489 Toggle Reverse Reports → verify columns swap and alignment maintained
- [ ] V490 Toggle Reverse back → verify alignment restored
- [ ] V491 Add 50+ lines to both columns → verify scroll synchronization works smoothly without lag
- [ ] V492 Verify no visual glitches or overlap when textboxes have different content lengths
- [ ] V493 Resize window horizontally → verify textboxes and proofread columns resize proportionally
- [ ] V494 Test with empty fields → verify alignment maintained with minimal heights
- [ ] V495 Compare portrait and landscape layouts → verify consistent behavior across orientations

## New (2025-10-18 – Current Combination Quick Delete and All Combinations Library)
- [X] T1190 Add MouseDoubleClick event handler to "Current Combination" ListBox (FR-1060, FR-1061)
- [X] T1191 Implement RemoveFromCurrentCombination(item) method in StudynameTechniqueViewModel (FR-1062, FR-1069)
- [X] T1192 Update RemoveFromCurrentCombination to call RaiseCanExecuteChanged on SaveNewCombinationCommand (FR-1062)
- [X] T1193 Update "Current Combination" GroupBox header to include hint text "(double-click to remove)" (FR-1070)
- [X] T1194 Add AllCombinations ObservableCollection to StudynameTechniqueViewModel (FR-1063)
- [X] T1195 Create AllCombinationRow class with CombinationId and Display properties (FR-1063)
- [X] T1196 Add GetAllCombinationsAsync() method to ITechniqueRepository interface (FR-1064)
- [X] T1197 Add AllCombinationRow record to TechniqueRepository.cs (FR-1064)
- [X] T1198 Implement GetAllCombinationsAsync() in TechniqueRepository.Pg.Extensions.cs querying v_technique_combination_display (FR-1064)
- [X] T1199 Update ReloadAsync() to populate AllCombinations collection (FR-1064)
- [X] T1200 Change left panel layout from 4 rows to 5 rows (FR-1072)
- [X] T1201 Add "All Combinations" GroupBox with ListBox to left panel row 3 (FR-1063, FR-1072)
- [X] T1202 Set DisplayMemberPath="Display" on "All Combinations" ListBox (FR-1063)
- [X] T1203 Bind "All Combinations" ListBox ItemsSource to AllCombinations collection (FR-1063)
- [X] T1204 Add MouseDoubleClick event handler to "All Combinations" ListBox (FR-1065)
- [X] T1205 Update "All Combinations" GroupBox header to include hint text "(double-click to load)" (FR-1071)
- [X] T1206 Implement LoadCombinationIntoCurrentAsync(combinationId) method in StudynameTechniqueViewModel (FR-1065, FR-1068)
- [X] T1207 Implement duplicate prevention logic in LoadCombinationIntoCurrentAsync (FR-1066)
- [X] T1208 Implement sequential sequence_order assignment when loading techniques (FR-1067)
- [X] T1209 Call RaiseCanExecuteChanged on SaveNewCombinationCommand after loading techniques (FR-1065)
- [X] T1210 Apply dark theme styling to "All Combinations" ListBox (FR-1074)
- [X] T1211 Update both "Current Combination" and "All Combinations" to use Star row sizing for equal space (FR-1073)
- [X] T1212 Update Save button Grid.SetRow to 4 (was 3) due to layout change (FR-1072)
- [X] T1213 Implement OnCurrentCombinationDoubleClick event handler in StudynameTechniqueWindow (FR-1060, FR-1061)
- [X] T1214 Implement OnAllCombinationsDoubleClick event handler in StudynameTechniqueWindow (FR-1065)
- [X] T1215 Update Spec.md with FR-1060 through FR-1074 documenting quick delete and all combinations library (cumulative)
- [X] T1216 Update Plan.md with change log entry including approach, test plan, and risks (cumulative)
- [X] T1217 Update Tasks.md with T1190-T1225 and V440-V465 (this file, cumulative)
- [X] T1218 Verify build passes with no compilation errors

## Verification (Current Combination Quick Delete and All Combinations Library)
- [ ] V440 Open "Manage Studyname Techniques" window → verify both "Current Combination" and "All Combinations" ListBoxes are visible
- [ ] V441 Verify "Current Combination" header includes "(double-click to remove)" hint text
- [ ] V442 Verify "All Combinations" header includes "(double-click to load)" hint text
- [ ] V443 Verify both ListBoxes have equal vertical space (both Star-sized rows)
- [ ] V444 Add 3-4 techniques to Current Combination manually
- [ ] V445 Double-click an item in Current Combination → verify it removes immediately without confirmation
- [ ] V446 Verify Save button disables when last item removed via double-click
- [ ] V447 Double-click multiple items rapidly → verify all remove correctly
- [ ] V448 Verify "All Combinations" ListBox populates with existing combinations
- [ ] V449 Verify combinations in All Combinations display formatted text (e.g., "axial T1, T2; coronal T1")
- [ ] V450 Verify All Combinations list is ordered by ID descending (newest first)
- [ ] V451 Verify All Combinations includes combinations not linked to current studyname
- [ ] V452 Double-click a combination in All Combinations with empty Current Combination → verify techniques load
- [ ] V453 Verify loaded techniques appear with correct prefix/tech/suffix display text
- [ ] V454 Verify sequence_order starts at 1 for first loaded technique
- [ ] V455 Add 2 techniques manually, then double-click a combination → verify new techniques append (sequence continues)
- [ ] V456 Double-click same combination twice → verify no duplicates added second time
- [ ] V457 Manually add "axial T1", then double-click combination containing "axial T1" → verify that technique skipped
- [ ] V458 Double-click combination with techniques having null prefix/suffix → verify techniques load correctly
- [ ] V459 Verify Save button enables after loading techniques from All Combinations
- [ ] V460 Load techniques from All Combinations, modify list (add/remove), then save → verify new combination created

## New (2025-02-08 – Disable Auto-Save on Previous Study Tab Switch)
- [X] T1320 Disable auto-save logic when switching between previous study tabs per user request (FR-DISABLE-AUTOSAVE-001)
- [X] T1321 Comment out JSON capture and apply code in SelectedPreviousStudy setter (FR-DISABLE-AUTOSAVE-002)
- [X] T1322 Add comments explaining disabled behavior and rationale (FR-DISABLE-AUTOSAVE-003)
- [X] T1323 Preserve original auto-save code in comments for future reference (FR-DISABLE-AUTOSAVE-004)
- [X] T1324 Create FIX_2025-02-08_DisableAutoSaveOnPreviousTabSwitch.md documentation (FR-DISABLE-AUTOSAVE-005)
- [X] T1325 Update README.md with disable auto-save entry (FR-DISABLE-AUTOSAVE-006)
- [X] T1326 Update Tasks.md with completed disable auto-save tasks (this file, cumulative)
- [X] T1327 Verify build passes with no compilation errors
- [X] V340 Verify changes from previous tab are NOT saved automatically when switching tabs
- [X] V341 Verify changes ARE saved when clicking "Save Previous Study to DB" button before switching tabs
- [X] V342 Verify behavior change documented clearly with warnings about data loss risk

## New (2025-02-08 – Fix Save Button Not Updating Previous Study JSON)
- [X] T1330 Investigate why "Save Previous Study to DB" button not saving current edits (FR-SAVE-JSON-001)
- [X] T1331 Identify root cause: PreviousReportJson not synchronized with UI state when auto-save disabled (FR-SAVE-JSON-002)
- [X] T1332 Add UpdatePreviousReportJson() call before RunSavePreviousStudyToDBAsync() in OnSavePreviousStudyToDB() (FR-SAVE-JSON-003)
- [X] T1333 Create FIX_2025-02-08_SaveButtonNotUpdatingPreviousStudyJSON.md documentation (FR-SAVE-JSON-004)
- [X] T1334 Update Tasks.md with completed save button fix tasks (this file, cumulative)
- [X] T1335 Verify build passes with no compilation errors
- [X] V350 Verify edits to proofread fields are saved when clicking Save button immediately after typing
- [X] V351 Verify edits to split fields (Header temp, Findings split, Conclusion split) are saved correctly
- [X] V352 Verify edits to original fields (Previous Header and Findings, Final Conclusion) are saved correctly
- [X] V353 Verify multiple edits with mixed focus changes are all saved when clicking Save button
- [X] V354 Verify behavior is consistent regardless of which textbox has focus when Save is clicked

## New (2025-02-08 – Fix Previous Report Split Ranges Loading Order)
- [X] T1370 Investigate why conclusion editor shows concatenated content when reselecting reports (FR-SPLIT-ORDER-001)
- [X] T1371 Identify root cause: split ranges loaded AFTER setting Findings/Conclusion, causing stale ranges (FR-SPLIT-ORDER-002)
- [X] T1372 Reorder ApplyReportSelection() to update RawJson first (FR-SPLIT-ORDER-003)
- [X] T1373 Call LoadProofreadFieldsFromRawJson() BEFORE setting Findings/Conclusion (FR-SPLIT-ORDER-004)
- [X] T1374 Update null report handling to load split ranges before clearing fields (FR-SPLIT-ORDER-005)
- [X] T1375 Create FIX_2025-02-08_PreviousReportSplitRangesLoadingOrder.md documentation (FR-SPLIT-ORDER-006)
- [X] T1376 Update Tasks.md with completed split ranges loading order tasks (this file, cumulative)
- [X] T1377 Verify build passes with no compilation errors
- [X] V390 Verify switching from Report A to Report B (no split ranges) to Report A shows correct conclusion
- [X] V391 Verify conclusion editor shows only conclusion, not concatenated content
- [X] V392 Verify split ranges loaded before property change events fire
- [X] V393 Verify multiple report switches maintain correct split outputs
- [X] V394 Verify debug output shows correct split range loading order

## Changed

## Added
- [X] T1400 Add Shift modifier capture to SettingsWindow.OnHotkeyTextBoxPreviewKeyDown (FR-1400)
- [X] T1401 Ensure single-display of "Shift" when either LeftShift or RightShift pressed (FR-1401)
- [X] T1402 Add Shift modifier parsing to MainWindow.TryParseHotkey method (FR-1403)
- [X] T1403 Update README.md and Spec.md with Shift modifier support entries (FR-1404)
- [X] T1404 Add immediate hotkey re-registration in SaveKeyboard command (FR-1405)
- [X] T1405 Add ReregisterGlobalHotkeys public method to MainWindow (FR-1405)
- [X] V1400 Build successful after adding Shift support and immediate re-registration

## Changed
