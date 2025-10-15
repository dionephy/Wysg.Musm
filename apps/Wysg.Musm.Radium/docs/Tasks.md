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
- [X] T492 Update Spec/Plan/Tasks docs with FR-336..FR-338 entries and implementation notes.
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

## Verification
- [X] V190 Resize Edit Study Technique window; verify groups and list stretch with window.
- [X] V191 Select combo items; verify selected area shows friendly text.
- [X] V192 Add new prefix/tech/suffix; verify row persists and selection applied.
- [X] V193 Save combination then set default for studyname; verify refresh.
- [X] V200 SpyWindow shows "Invoke open study" in PACS Method list; selecting it loads/saves steps.
- [X] V201 Running the procedure invokes UIA `Invoke` (or `Toggle`) on selected row; viewer opens where supported.
- [X] V202 PacsService.InvokeOpenStudyAsync() returns without exception and triggers the action.
- [X] V203 SpyWindow Map-to shows "Test invoke"; mapping saves/loads and Invoke works against it.
- [X] V204 Custom mouse click 1/2 appear in PACS method combo; saving/running with MouseClick(X,Y) triggers a click at coordinates.
- [X] V205 Selecting operation `MouseClick` enables numeric Arg1/Arg2; Arg3 disabled.
- [X] V206 After Pick, `txtPickedPoint` shows coordinates in "X,Y" format and is selectable.

## Previously Added
- [X] T707 Fix short scrollbar length (remove along-axis size in Thumb style) (FR-476).
- [X] T708 Increase min thumb length (MinHeight/MinWidth = 36px) (FR-477).

---
## Validation Checklist
- [X] FR-478..FR-482 implemented; window resizes properly; selection text fixed; inline add works.
- [X] DarkTheme ComboBox change verified in Edit Study Technique; no regressions detected in other common ComboBoxes (to be observed).
- [X] Build passes.

## New (2025-10-13)
- [X] T720 Implement `TechniqueFormatter.BuildGroupedDisplay` to group by (prefix, suffix) and join techs with ","; join groups by ";".
- [X] T721 Extend TechniqueRepository with `GetDefaultCombinationForStudynameAsync`, `GetCombinationItemsAsync`, and `GetStudynameIdByNameAsync`.
- [X] T722 Autofill current study `StudyTechniques` in NewStudyProcedure after PACS fetch using default combination (if present).
- [X] T723 Add `MainViewModel.RefreshStudyTechniqueFromDefaultAsync` and call after closing StudynameTechniqueWindow and after saving new default.
- [X] T724 Prevent duplicate (prefix,tech,suffix) within a single combination in `StudyTechniqueViewModel` at add-time and save-time.

### New (2025-10-13 – Automation Modules + Keyboard + Toggles)
- [X] T740 Add modules to library in `SettingsViewModel`: OpenStudy, MouseClick1, MouseClick2 (apps/Wysg.Musm.Radium/ViewModels/SettingsViewModel.cs).
- [X] T741 Map modules in `MainViewModel.Commands`: call PacsService for clicks and OpenStudy; set `StudyOpened=true` (apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.cs).
- [X] T742 Add `StudyOpened` property to VM and bind to new toggle (apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.cs).
- [X] T743 Replace icon "Reportified" toggle near Study locked with text "Study opened" toggle (apps/Wysg.Musm.Radium/Views/MainWindow.xaml).
- [X] T744 Remove icon-only Reportified toggle in previous report area (apps/Wysg.Musm.Radium/Views/MainWindow.xaml).
- [X] T745 Add Keyboard tab UI with two capture TextBoxes and Save button (apps/Wysg.Musm.Radium/Views/SettingsWindow.xaml).
- [X] T746 Implement capture handler `OnHotkeyTextBoxPreviewKeyDown` (apps/Wysg.Musm.Radium/Views/SettingsWindow.xaml.cs).
- [X] T747 Add local settings keys for hotkeys in `IRadiumLocalSettings` and `RadiumLocalSettings` (apps/Wysg.Musm.Radium/Services/IRadiumLocalSettings.cs, .../RadiumLocalSettings.cs).
- [X] T748 Add `SaveKeyboardCommand` and VM properties `OpenStudyHotkey`, `SendStudyHotkey` (apps/Wysg.Musm.Radium/ViewModels/SettingsViewModel.cs).
- [X] T749 Docs: Update Spec.md and Plan.md with FR-540..FR-544 and keyboard capture behavior.

### New (2025-10-13 – Open Study Shortcut Panes)
- [X] T750 Add three panes in Settings → Automation (apps/Wysg.Musm.Radium/Views/SettingsWindow.xaml):
  - Shortcut: Open study (new) ListBox `lstShortcutOpenNew`
  - Shortcut: Open study (add) ListBox `lstShortcutOpenAdd`
  - Shortcut: Open study (after open) ListBox `lstShortcutOpenAfterOpen`
- [X] T751 Wire ListBoxes to VM collections in code-behind (apps/Wysg.Musm.Radium/Views/SettingsWindow.xaml.cs).
- [X] T752 Add VM collections and load/save logic (apps/Wysg.Musm.Radium/ViewModels/SettingsViewModel.cs).
- [X] T753 Add local settings keys in interface and implementation (apps/Wysg.Musm.Radium/Services/IRadiumLocalSettings.cs, .../RadiumLocalSettings.cs).
- [X] T754 Add `MainViewModel.RunOpenStudyShortcut()` to execute proper sequence (apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.cs).
- [X] T755 Docs: Update Spec.md with FR-545..FR-547 and Plan.md with approach/test/risks.

## New (2025-10-14 – Multi-PACS Tenant + Account-Scoped Techniques)
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
- [X] V241 New Study pane set to only `ShowTestMessage` → pressing New shows the message and does not lock study.
- [X] V242 Add `LockStudy` to New Study pane explicitly → pressing New locks study as designed.

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

## New (2025-01-15 – Phrase-to-SNOMED Mapping Central Database)
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

## New (2025-01-15 – PhraseSnomedLinkWindow UX Improvements)
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

## New (2025-01-15 – UI Bookmark Robustness Improvements)
- [X] T936 Update `Walk` method in `UiBookmarks.cs` to require ALL enabled attributes for step 0 root acceptance (FR-920).
- [X] T937 Update `DiscoverRoots` method to filter existing roots using first node attributes instead of rescanning desktop (FR-921).
- [X] T938 Add exact match filtering followed by relaxed match (without ControlTypeId) fallback (FR-921).
- [X] T939 Add ClassName filtering when multiple root matches remain (FR-924).
- [X] T940 Implement `CalculateNodeSimilarity` helper to score roots (AutomationId=200, Name=100, ClassName=50, ControlType=25) (FR-925).
- [X] T941 Sort filtered roots by similarity score for deterministic selection (FR-925).
- [X] T942 Add `ValidateBookmark` method in `SpyWindow.Bookmarks.cs` to validate before save (FR-922).
- [X] T943 Validate process name not empty, chain not empty, first node has ≥2 enabled attributes (FR-922).
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

---
# Tasks: Radium Cumulative (Reporting Workflow + Editor + Mapping + PACS)

## Added (2025-10-15 – OpenStudy fallback to per-PACS WorklistViewButton)
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
