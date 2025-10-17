# Implementation Plan: Radium (Cumulative)

## Change Log Addition (2025-01-15 – Fix: Shortcut Key Missing NewStudy and LockStudy Modules)
- **Problem**: The "Shortcut: Open study (new)" automation sequence was not executing `NewStudy` and `LockStudy` modules that are present in the "New Study" button sequence, causing incomplete study initialization.
- **Symptoms**:
  1. Current study label not changing (NewStudy module not running)
  2. Locked toggle not turning on (LockStudy module not running)
  3. "Patient mismatch cannot add" error when AddPreviousStudy executes (incomplete patient context from missing NewStudy)
  4. Study remarks and patient remarks were received correctly
- **Root Cause**: `MainViewModel.RunOpenStudyShortcut()` method was missing the module handlers for `NewStudy` and `LockStudy`, which are present in `OnNewStudy()` method.
- **Fix**: Added `NewStudy` and `LockStudy` module execution support to `RunOpenStudyShortcut()` to achieve parity with the "New" button behavior.

### Approach (Shortcut Key Fix)
1) Analyze difference between `OnNewStudy()` (working) and `RunOpenStudyShortcut()` (broken) execution paths.
2) Add missing module handlers to `RunOpenStudyShortcut()`:
   - `NewStudy` → calls `RunNewStudyProcedureAsync()` to load patient/study context
   - `LockStudy` → calls `_lockStudyProc.ExecuteAsync(this)` to set PatientLocked state
3) Maintain exact same module ordering as `OnNewStudy()` for consistency.
4) No changes to Settings UI or persistence; fix is purely in runtime execution.

### Test Plan (Shortcut Key Fix)
- Configure "Shortcut: Open study (new)" pane with modules: `NewStudy, LockStudy, GetStudyRemark, GetPatientRemark, AddPreviousStudy, OpenStudy`
- Save automation settings
- Press the registered global hotkey (e.g., Ctrl+Alt+O) when PatientLocked=false
- **Expected Results**:
  1. Current study label updates with patient information (NewStudy executed)
  2. "Study locked" toggle turns on (LockStudy executed)
  3. Study remark and patient remark fields populate (GetStudyRemark/GetPatientRemark executed)
  4. Previous study added successfully without "Patient mismatch" error (AddPreviousStudy executed after patient context loaded)
  5. Viewer opens if configured (OpenStudy executed)
- Compare behavior with clicking "New" button → should be identical
- Verify "Shortcut: Open study (add)" and "Shortcut: Open study (after open)" sequences also support NewStudy/LockStudy if configured

### Risks / Mitigations (Shortcut Key Fix)
- **Risk**: Adding LockStudy to shortcut sequences when not desired by user
  - **Mitigation**: Modules are only executed if explicitly present in saved sequence; users control via Settings → Automation
- **Risk**: Breaking existing shortcut configurations that worked around the missing modules
  - **Mitigation**: If users added workarounds (e.g., manual lock steps), they can remove them; standard configuration now works
- **Risk**: Execution order differences between button and shortcut
  - **Mitigation**: Module handler order in `RunOpenStudyShortcut()` now matches `OnNewStudy()` exactly

### Code Changes (Shortcut Key Fix)
**File**: `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Commands.cs`
**Method**: `RunOpenStudyShortcut()`
**Changes**:
- Added: `if (string.Equals(m, "NewStudy", StringComparison.OrdinalIgnoreCase)) { _ = RunNewStudyProcedureAsync(); }`
- Added: `else if (string.Equals(m, "LockStudy", StringComparison.OrdinalIgnoreCase) && _lockStudyProc != null) { _ = _lockStudyProc.ExecuteAsync(this); }`
- Preserved: All existing module handlers (OpenStudy, MouseClick1/2, GetStudyRemark, GetPatientRemark, AddPreviousStudy, TestInvoke, ShowTestMessage)

### Related Features
- This fix completes the implementation of FR-545 (Automation panes for Open Study Shortcut)
- Enables full parity between "New" button (FR-540) and global hotkey execution (FR-660, FR-661)
- Supports proper patient context loading for FR-511 (Add Previous Study automation module)

## Change Log Addition (2025-10-13 – Open Study Shortcut Panes)
- Added three Automation panes to configure Open Study hotkey action lists by state: (new/add/after open).
- Persisted sequences to local settings as `auto_shortcut_open_new`, `auto_shortcut_open_add`, `auto_shortcut_open_after_open`.
- Implemented `MainViewModel.RunOpenStudyShortcut()` to choose the proper sequence based on `PatientLocked` and `StudyOpened`.

### Approach
1) Extend Settings UI with three ListBoxes and wire drag/drop using existing handlers.
2) Add three ObservableCollections in SettingsViewModel and load/save via `IRadiumLocalSettings`.
3) Implement VM method `RunOpenStudyShortcut()` to parse and run modules similarly to existing flows.

### Test Plan
- Configure each pane with distinct modules; save. Restart and verify they reload correctly.
- Toggle PatientLocked/StudyOpened combinations and call `RunOpenStudyShortcut()`; verify the correct sequence runs.
- Unknown modules ignored; known modules execute (OpenStudy, MouseClick1/2, AddPreviousStudy, GetStudy/PatientRemark).

### Risks / Mitigations
- Overlapping modules could produce unexpected order; user config governs. Drag-and-drop reordering supported.

## Change Log Addition (2025-10-13 – Automation Modules + Keyboard Tab + Study Opened Toggle)
- Added new Automation modules to Settings → Automation library: `OpenStudy`, `MouseClick1`, `MouseClick2`.
- Implemented handlers in MainViewModel to run these during New/Add sequences:
  - `OpenStudy` → PacsService.InvokeOpenStudyAsync(); sets `StudyOpened=true` on success.
  - `MouseClick1` / `MouseClick2` → PacsService wrappers to run respective procedures.
- Added a new "Keyboard" tab in Settings with two fields (Open study, Send study) that capture pressed combinations and save to local settings.
- Replaced the small icon-only toggle next to "Study locked" with a text toggle "Study opened" bound to VM `StudyOpened`.
- Removed the icon-only Reportified toggle in the Previous Report area (kept the text toggle elsewhere).

### Approach
1) Extend `SettingsViewModel.AvailableModules` with new module names; wire in `MainViewModel.Commands` to execute via PacsService.
2) Add `StudyOpened` boolean property to VM; set when OpenStudy runs.
3) Add Keyboard tab XAML and a PreviewKeyDown handler to capture modifiers + key; persist via IRadiumLocalSettings new keys.
4) Update `IRadiumLocalSettings` and `RadiumLocalSettings` to store `hotkey_open_study` and `hotkey_send_study`.
5) Update MainWindow XAML to replace the icon toggle and remove previous area icon toggle.

### Test Plan
- Settings → Automation: verify library lists `OpenStudy`, `MouseClick1`, `MouseClick2`; drag into sequences; Save Automation.
- New/Add sequence run containing `OpenStudy`: verify `StudyOpened` toggles on and status updated.
- MouseClick1/2: ensure procedures exist; running sequence triggers clicks (visually validated).
- Settings → Keyboard: focus each TextBox and press combinations like Ctrl+Alt+S; verify text shown and saved to disk; restart app verifies persistence.
- MainWindow: confirm "Study opened" toggle appears next to "Study locked" and is initially off.
- Previous report area: confirm removal of icon-only Reportified toggle while text toggle remains elsewhere.

### Risks / Mitigations
- Global hotkey registration not implemented in this increment → documented; only capture+persist now.
- Procedure tags may be missing → headless executor auto-seeds defaults; status messaging handles failures.
- UI real estate changes could shift layout → kept consistent DarkToggleButtonStyle and text label for clarity.

## Change Log Addition (2025-01-12 – Study Technique Feature Database Schema)
- Designed and implemented database schema for study technique management feature.
- Created 8 new tables in med schema to support technique component management:
  - Lookup tables: technique_prefix, technique_tech, technique_suffix (with display_order)
  - Composite table: technique (combines prefix + tech + suffix with unique constraint)
  - Combination tables: technique_combination, technique_combination_item (many-to-many with sequencing)
  - Link tables: rad_studyname_technique_combination (with is_default flag), rad_study_technique_combination (zero-or-one)
- Created 2 helper views for display: v_technique_display (formatted "prefix tech suffix"), v_technique_combination_display (joined with " + ")
- Seeded common values: 7 prefixes, 9 techs, 2 suffixes
- Implemented proper referential integrity: CASCADE for study/studyname links, RESTRICT for component links
- Added appropriate indexes on foreign keys and join columns for query performance

### Approach (Study Technique Schema)
1) Normalize technique components into separate lookup tables (prefix, tech, suffix) with display_order
2) Create composite technique table enforcing uniqueness on (prefix_id, tech_id, suffix_id)
3) Model combinations as separate entity with join table for many-to-many + ordering
4) Link combinations to studynames (many-to-many with optional default) and studies (zero-or-one)
5) Provide display views that format techniques and combinations for UI consumption
6) Use nullable FKs for optional components (prefix, suffix) and NOT NULL for required (tech)

### Test Plan (Database Schema)
- Insert sample prefixes, techs, suffixes → verify unique constraints work
- Create techniques with various component combinations → verify composite unique constraint
- Create technique combinations with multiple items → verify sequencing and joins
- Link combinations to studynames with default flag → verify only one default per studyname
- Link combinations to studies → verify unique study_id constraint (zero-or-one)
- Query v_technique_display → verify proper formatting with spacing and trimming
- Query v_technique_combination_display → verify proper " + " joining and sequencing
- Test CASCADE delete: delete studyname → verify studyname_technique_combination rows deleted
- Test RESTRICT delete: attempt to delete tech in use → verify constraint prevents deletion

### Risks / Mitigations (Study Technique Schema)
- Complex joins may impact query performance → mitigated by adding indexes on all FK columns and join columns
- Empty string vs NULL for blank prefix/suffix could cause confusion → documented clearly; empty string is valid
- Default flag enforcement per studyname not database-constrained → will need application-level validation in future UI
- Combination name optional may lead to unlabeled combinations → display view provides auto-generated name fallback

## Change Log Addition (2025-10-12 – Fix: Patient Remark mismatch on New Study due to Split parity)
- Problem: Patient remark captured during New Study included trailing HTML markup compared to the PACS method "Get current patient remark" executed from SpyWindow. Root cause: headless `ProcedureExecutor.Split` lacked regex and escape support used by user-authored procedures, so the final split step was not applied.
- Fix: Updated `ProcedureExecutor.Split` to achieve full parity with SpyWindow:
  - Supports `re:`/`regex:` prefix to split using `Regex.Split` with Singleline | IgnoreCase.
  - Supports C#-style escapes (e.g., `\n`, `\r\n`, `\t`) for literal separators via `Regex.Unescape`.
  - Best-effort CRLF retry when only LF is provided but input uses Windows `\r\n`.
  - Preserves Arg3 index behavior (select specific part) else joins with U+001F.
- Effect: New Study automation now returns exactly the same `patient_remark` string as running the procedure in SpyWindow (after Trim). Example HTML tail ("</TR></TABLE></BR>...</HTML>") is no longer included when the procedure uses a regex split boundary.

### Approach (Split parity)
1) Port SpyWindow Split logic into `ProcedureExecutor` (regex mode, escape handling, CRLF retry).
2) Keep existing Replace/GetHTML parity already implemented; no change to other ops.
3) Do not alter persistence or PACS wrappers; fix contained to `ProcedureExecutor`.

### Test Plan (Patient Remark Split)
- Author a `GetCurrentPatientRemark` procedure in SpyWindow that ends with:
  - `Split(Arg1=varN, Arg2=regex:</TR>\s*</TABLE></BR>\s*</CENTER>\s*</BODY>\s*</HTML>, Arg3=0)`
- Run in SpyWindow → capture preview.
- Run New Study automation with `GetPatientRemark` module → verify `PatientRemark` equals the SpyWindow result (string-equal after Trim()).
- Verify non-regex separator with `\r\n` and only `\n` in Arg2 → both split correctly due to retry.

### Risks / Mitigations
- Regex patterns authored by users could be invalid → handled by returning `(regex error: ...)` preview in SpyWindow; in headless executor we safely return null part for that step. Users can adjust patterns in UI.
- Potential behavior change for existing procedures relying on legacy behavior → documented; parity with UI is the intended contract.

## Change Log Addition (2025-10-12 – Auto/Generate Buttons and Proofread/Reportified Toggles)
- Added auto toggles and generate buttons next to specified labels:
  - Chief Complaint (top/side top), Patient History (top/side top), Conclusion (top).
  - Study Techniques (bottom), Comparison (bottom).
  - All proofread labels in mid columns for both current and previous panels.
- Added `GenerateFieldCommand` in MainViewModel (skeleton) and per-field auto boolean properties.
- Added top toolbar toggles next to Test NewStudy Proc: Proofread, Reportified.
- Added right-panel header toggles next to Splitted: Proofread, Reportified.

### Approach (Auto/Generate + Toggles)
1) Introduce lightweight properties in MainViewModel for auto flags and proofread modes.
2) Add a single command `GenerateFieldCommand` that accepts field keys; stub sets status.
3) Update ReportInputsAndJsonPanel and PreviousReportTextAndJsonPanel XAML to place StackPanels wrapping labels with added buttons/toggles.
4) Wire bindings using Window DataContext for bottom/side controls.

### Test Plan (UI Wiring)
- Verify buttons render next to all specified labels in top, side top, bottom, and proofread sections.
- Clicking Generate sets status message with the key reported.
- Toggling auto flags changes bound properties (inspect via debugger / status text).
- New Proofread/Reportified toggles bind to VM properties and preserve state.

### Risks / Mitigations
- Many bindings referencing Window DataContext could break if visual tree changes → mitigated by AncestorType=Window binding where needed.
- Command skeleton not functional → documented as placeholder.

## Change Log Addition (2025-10-12 – PrevReport Extended Fields + UI)
- Added fields to PreviousStudyTab: StudyRemark, PatientRemark, ChiefComplaint, PatientHistory, StudyTechniques, Comparison.
- Serialize/deserialize these inside `PrevReport` object in PreviousReportJson.
- PreviousReportTextAndJsonPanel: added four editors (chief complaint, patient history, study techniques, comparison) below final conclusion.
- Exposed DPs on the control and bound them in MainWindow to SelectedPreviousStudy.*.

### Test Plan (PrevReport Extended Fields)
- Add dummy, toggle Splitted → verify JSON includes new fields (empty strings initially).
- Type into the four new editors → verify JSON PrevReport fields update live and bindings round-trip.
- Edit JSON PrevReport fields → verify editors update.
- Switch tabs → verify values persist per tab.

## Change Log Addition (2025-10-12 – Previous Report Split View)
- Added computed properties in VM for split view: `PreviousHeaderSplitView`, `PreviousFindingsSplitView`, `PreviousConclusionSplitView`.
- On `PreviousReportSplitted` ON:
  - If any split pair null, default per spec (header pairs 0/0, conclusion pairs len/len) using EnsureSplitDefaultsIfNeeded().
  - Bind previous editors to computed strings via DataTriggers; set `IsReadOnly=True` while split is active.

### Approach (Split View Bindings)
1) Implement clamp-safe substring helper and computed properties.
2) Hook notifications on text/split changes and Splitted toggle.
3) Update XAML bindings using style triggers without altering base layout.

### Test Plan (Split View)
- Toggle Splitted ON with all split fields null → verify defaults applied and editors show expected composed strings.
- Change split offsets via buttons → verify computed views refresh immediately.
- Toggle Splitted OFF → editors revert to normal two-way bindings and become editable.
- Edge cases: zero-length texts; offsets at bounds; mismatched ranges still clamped safely (no exceptions).
- Verify trimming: surrounding whitespace of each segment is removed before newline merge (no leading/trailing blanks).
 - Verify final trimming: the merged string has no leading/trailing whitespace/newlines after concatenation.
 - Verify default change: final_conclusion_findings_splitter_from/to default to 0/0 when null on Splitted ON.

## Change Log Addition (2025-01-11 - PreviousReportTextAndJsonPanel Reusable Control)
- Created reusable UserControl `PreviousReportTextAndJsonPanel` to eliminate duplicate XAML code in side and bottom panels.
- Control structure:
  - Three-column Grid layout: ScrollViewer (column 0) with labeled TextBox for header_and_findings, GridSplitter (column 1), JSON TextBox (column 2).
  - Dependency properties: `HeaderAndFindingsText` (string, two-way), `JsonText` (string, two-way), `Reverse` (bool, default false).
  - `ApplyReverse()` method swaps column positions when Reverse property changes.
- Created files:
  - `apps\Wysg.Musm.Radium\Controls\PreviousReportTextAndJsonPanel.xaml` - XAML layout with three columns and bindings.
  - `apps\Wysg.Musm.Radium\Controls\PreviousReportTextAndJsonPanel.xaml.cs` - Code-behind with dependency properties and column swap logic.
- Updated MainWindow.xaml:
  - Replaced gridSideBottom duplicate XAML with `<rad:PreviousReportTextAndJsonPanel>` instance (Reverse=false).
  - Replaced gridBottomControl duplicate XAML with `<rad:PreviousReportTextAndJsonPanel>` instance (Reverse=true for portrait layout).
  - Both instances bind to `PreviousHeaderAndFindingsText` and `PreviousReportJson`.
- Updated MainWindow.xaml.cs:
  - Modified `SwapReportEditors()` to toggle `Reverse` property on `gridSideBottom` and `gridBottomControl` instances.
  - Inverted reverse logic because gridBottomControl starts with Reverse=true.
- Benefits:
  - Single source of truth for layout and styling.
  - Easier maintenance (changes in one place).
  - Consistent behavior across portrait and landscape modes.

### Approach (PreviousReportTextAndJsonPanel)
1) Extract duplicate XAML pattern into a UserControl with configurable bindings.
2) Expose dependency properties for two-way data binding (HeaderAndFindingsText, JsonText).
3) Add Reverse property to support layout inversion when reports are swapped.
4) Replace duplicate XAML in MainWindow.xaml with control instances.
5) Update SwapReportEditors to toggle Reverse on control instances.

### Test Plan (PreviousReportTextAndJsonPanel)
- Portrait mode: Verify gridBottomControl displays JSON on left, header_and_findings on right.
- Landscape mode: Verify gridSideBottom displays header_and_findings on left, JSON on right.
- Edit header_and_findings TextBox → verify binding updates PreviousHeaderAndFindingsText in ViewModel.
- Edit JSON TextBox → verify binding updates PreviousReportJson in ViewModel.
- Toggle Reverse Reports → verify columns swap correctly in both control instances.
- GridSplitter dragging → verify resizing works smoothly in both panels.

### Risks / Mitigations (PreviousReportTextAndJsonPanel)
- Dependency property bindings might fail if RelativeSource is incorrect → mitigated by testing bindings in both usage contexts.
- Column swapping in ApplyReverse might not work if control hierarchy changes → mitigated by using FindName to locate elements at runtime.
- Reverse property initial state might cause layout flash → mitigated by setting Reverse in XAML declaratively.

## Change Log Addition (2025-01-11 - Previous Report Field Mapping Change)
- Changed PreviousReportJson field mapping to use `header_and_findings` instead of `findings` and `final_conclusion` instead of `conclusion` for alignment with database schema and domain model.
- Updated MainViewModel.PreviousStudies.cs:
  - Renamed internal cache fields: `_prevFindingsCache` → `_prevHeaderAndFindingsCache`, `_prevConclusionCache` → `_prevFinalConclusionCache`.
  - Renamed properties: `PreviousFindingsText` → `PreviousHeaderAndFindingsText`, `PreviousConclusionText` → `PreviousFinalConclusionText`.
  - Added backward compatibility aliases `PreviousFindingsText` and `PreviousConclusionText` as passthrough properties to avoid breaking existing references.
  - Updated JSON serialization in `UpdatePreviousReportJson()` to emit `header_and_findings` and `final_conclusion` fields.
  - Updated JSON deserialization in `ApplyJsonToPrevious()` to read `header_and_findings` and `final_conclusion` fields.
  - Updated property change notifications to reference new property names.
- Updated MainWindow.xaml:
  - Changed EditorPreviousFindings binding from `PreviousFindingsText` to `PreviousHeaderAndFindingsText`.
  - Changed EditorPreviousConclusion binding from `PreviousConclusionText` to `PreviousFinalConclusionText`.
  - Added new TextBox `txtPrevHeaderAndFindingsSide` in the gridSide landscape panel to the left of `txtPrevJsonSide` with a GridSplitter between them, bound to `PreviousHeaderAndFindingsText` for direct editing.
- Updated documentation: Spec.md (FR-395..FR-400), Plan.md (this entry), Tasks.md (T560..T565).

### Approach (Previous Report Field Mapping)
1) Align field names with database schema and domain conventions (`header_and_findings`, `final_conclusion`).
2) Maintain backward compatibility through property aliases to avoid breaking existing code.
3) Add dedicated TextBox for `header_and_findings` content editing alongside JSON view.
4) Update all serialization, deserialization, and property notification paths to use new field names.

### Test Plan (Previous Report Field Mapping)
- Load a previous study → verify EditorPreviousFindings displays content from `header_and_findings`.
- Edit EditorPreviousFindings → verify `PreviousReportJson` shows updated `header_and_findings` field.
- Edit txtPrevHeaderAndFindingsSide → verify EditorPreviousFindings updates in real-time.
- Edit `PreviousReportJson` directly (change `header_and_findings` value) → verify both txtPrevHeaderAndFindingsSide and EditorPreviousFindings update.
- Verify backward compatibility: code referencing `PreviousFindingsText` or `PreviousConclusionText` continues to work.
- Landscape mode: verify txtPrevHeaderAndFindingsSide appears to the left of txtPrevJsonSide with proper GridSplitter.

### Risks / Mitigations (Previous Report Field Mapping)
- Existing code referencing old property names may break → mitigated by providing backward compatibility aliases.
- JSON parsing of old data with `findings`/`conclusion` fields will fail → mitigated by updating parsing logic to expect new field names (migration may be needed for existing saved reports).
- UI layout complexity with additional TextBox → mitigated by using GridSplitter for flexible sizing.

## Change Log Addition (2025-10-12 – Dark Theme Scrollbar UX)
- Implemented dark, more intuitive scrollbars across the app.
- Added dedicated track/thumb colors and hover/drag states; rounded thumbs.
- Increased thickness to 12px for easier grasping; ensured paging on track click.
- Applied globally to `ScrollBar` and `ScrollViewer` so TextBox, DataGrid, editors, and combo popups inherit.

### Approach (Scrollbars)
1) Define new dark colors/brushes for track, thumb, hover, active, and border in `DarkTheme.xaml`.
2) Create `Thumb` style with rounded corners and hover/drag visual states.
3) Template vertical and horizontal `ScrollBar` with `Track` + transparent `RepeatButton` page areas.
4) Apply a global `ScrollBar` style that switches template based on `Orientation` and sets width/height.
5) Add `ScrollViewer` style to set Auto visibility and panning with transparent background.

### Test Plan (Scrollbars)
- Verify vertical and horizontal scrollbars appear with dark track and rounded thumb in:
  - TextBox editors (current/previous, JSON boxes)
  - DataGrid lists (phrases/hotkeys/procedures)
  - ComboBox dropdown list popup
- Hover thumb → color lightens; drag → color further lightens.
- Click on empty track → content pages up/down/left/right.
- Confirm thickness ≈ 12px and thumb min size respected.
- Build passes with no XAML errors.

### Risks / Mitigations (Scrollbars)
- Some third-party controls might override templates → global styles still cover WPF primitives; adjust per control if gaps reported.
- Very small scrollable areas could render oversized thumbs → enforced MinLength only; WPF scales appropriately.

## Change Log Addition (2025-10-13 – Edit Study Technique Window UX)
- Problem 1: Group panels were too small and did not stretch with the window, making the preview cramped.
- Problem 2: ComboBox selected area showed CLR type name (e.g., "Wysg.Musm.Radium....") instead of the `Text` property of the selected item.
- Problem 3: No way to add Prefix/Tech/Suffix from the window, forcing DB pre-seed.

- Fixes:
  - Layout: Converted window body to a 3-column Grid (left panel [3*], splitter [Auto], right panel [2*]). Left panel uses Grid with `Add Technique` (Auto) and `Current Combination` (*). This ensures proportional stretch. Increased default window size.
  - ComboBox display: In the global dark `ComboBox` template, replaced the selection `ContentPresenter` with a `TextBlock` bound via `PriorityBinding` to `SelectedItem.Text` → `Text` → `SelectionBoxItem`, and set `TextSearch.TextPath=Text`.
  - Inline add: Added "+" buttons next to Prefix/Tech/Suffix ComboBoxes. Buttons open a small prompt dialog, then call `StudyTechniqueViewModel.AddPrefixAndSelectAsync`, `AddTechAndSelectAsync`, `AddSuffixAndSelectAsync`. On success, lookups reload and the new item is auto-selected.
  - Studyname combinations: Added right panel `GroupBox` listing combinations for the selected studyname with a `Set Default` button bound to `SetDefaultForStudynameCommand`.

### Approach
1) Restructure `StudyTechniqueWindow.xaml.cs`: build body grid, left/right panels, and splitter; bind lists and commands from `StudyTechniqueViewModel`.
2) Enhance `DarkTheme.xaml` ComboBox template: use `TextBlock` with `PriorityBinding` for selection text and set `TextSearch.TextPath=Text`.
3) Implement a lightweight prompt dialog inside `StudyTechniqueWindow` for adding new lookup items (prefix/tech/suffix).
4) Wire add buttons to VM async add methods which already persist and reload via repository.

### Test Plan
- Resize window: verify both panels resize proportionally; combination list stretches vertically.
- Select items in Prefix/Tech/Suffix: selected text shows friendly value, not type name.
- Open dropdowns: list items remain correct and unchanged.
- Click "+" on each row: enter value, confirm appears in list and becomes selected.
- Add several techniques and click "Add Item": items appear in Current Combination with increasing sequence order.
- If `StudynameId` is set, right panel lists existing combinations; select one and click "Set Default" updates IsDefault (verify refresh).
- Save for Study & Studyname: persists combination and links to studyname (or sets default if flagged by StudynameTechnique window).
- Build succeeds.

### Risks / Mitigations
- Global ComboBox template change could affect other ComboBoxes that expect complex content in the selection box. Mitigation: PriorityBinding falls back to `SelectionBoxItem` if `SelectedItem.Text` is not present; dropdown ItemTemplate remains unchanged.
- Prompt dialog is simple and modal; acceptable for admin-like add operations.
- Studyname combinations default star indicator not fully templated; acceptable minimal UX for now.

## Change Log Addition (2025-10-13 – Technique Combination Grouped String + Autofill + Refresh)
- Implemented grouped display logic for technique combinations (FR-500..FR-503):
  - New helper `TechniqueFormatter.BuildGroupedDisplay(IEnumerable<TechniqueGroupItem>)` groups by (prefix, suffix) preserving first-seen group order by `sequence_order` and joins techs with ", ", groups with "; ".
- New repository extensions (FR-506):
  - `GetStudynameIdByNameAsync(string studyname)`
  - `GetDefaultCombinationForStudynameAsync(long studynameId)`
  - `GetCombinationItemsAsync(long combinationId)` returning (prefix, tech, suffix, seq) for rendering.
- New Study procedure integration (FR-504, FR-505):
  - After PACS selection fetch completes, auto-populate `StudyTechniques` with grouped display when default exists for current studyname.
- Default-change live refresh (FR-507, FR-508):
  - Added `MainViewModel.RefreshStudyTechniqueFromDefaultAsync()` and invoked from `StudynameTechniqueWindow.OnClosed()` and after `StudyTechniqueViewModel.SaveForStudyAndStudynameCommand` to refresh the current study technique string.
- Duplicate prevention in Edit Study Technique window (FR-509, FR-510):
  - Prevent duplicates at add time and deduplicate on save prior to persistence.

### Approach
1) Create a small pure function for grouping/formatting to keep UI and repositories simple.
2) Extend TechniqueRepository via partials to avoid touching core file too much; keep SQL straightforward and ordered by sequence.
3) Hook New Study flow after PACS fetch to avoid missing studyname; use repo methods to resolve id and default.
4) Provide a public VM method to refresh techniques so diverse UI flows (window close, set default, save) can call consistently.
5) Ensure DI wiring compiles: `NewStudyProcedure` made partial and extended; DI container already provides `ITechniqueRepository`.

### Test Plan
- Unit-ish manual checks:
  - Build `TechniqueGroupItem` list for sample scenario → verify output equals: `axial T1, T2; sagittal T1; coronal T1, T2, CE-T1 of sellar fossa; sagittal T1, CE-T1 of sellar fossa`.
  - Insert combination rows in DB, mark default for a studyname, run New Study → `StudyTechniques` auto-filled accordingly.
  - Open Edit Study Technique, add duplicate item → prevented; add distinct items → allowed; save as default → current study `StudyTechniques` updates after window close.
  - Change default via Set Default list → window close triggers refresh; confirm updated display.

### Risks / Mitigations
- Studyname text from PACS might not match DB exactly → repo resolves by exact match; if mismatch occurs, no autofill; can enhance later with normalization.
- Multiple windows might try to refresh concurrently → idempotent setter on `StudyTechniques` handles fine.
- Performance: extra queries on window close → negligible and async.

## Change Log Addition (2025-10-13 – Add Previous Study Automation + '+' Mapping)
- Implemented new automation module `AddPreviousStudy` and wired Automation → AddStudy sequence to the small `+` button in Previous Reports area.
- Behavior:
  - Validates current patient by comparing `GetCurrentPatientNumber` with app `PatientNumber` (normalized alphanumerics uppercased).
  - Reads Related Studies list metadata: studyname, study datetime, radiologist, report datetime.
  - Reads current report text via dual getters for findings and conclusion, picks the longer variant.
  - Persists as local previous study report and selects it; sets `PreviousReportified=true`.
- Settings → Automation tab:
  - Added `AddPreviousStudy` to Available Modules. Users can compose `AddStudy` sequence using drag and drop.
  - Clicking the small `+` button now executes modules from `AutomationAddStudySequence` in order.

### Approach
1) Extend `SettingsViewModel.AvailableModules` to include `AddPreviousStudy`.
2) In `MainViewModel.Commands`, add `OnRunAddStudyAutomation()` that iterates `AutomationAddStudySequence` and executes known modules.
3) Implement `RunAddPreviousStudyModuleAsync()` that performs the steps above, reusing `PacsService` operations and existing persistence methods.
4) Keep existing `OnAddStudy()` for backward compatibility; '+' now calls the automation runner.

### Test Plan
- Compose AddStudy sequence = `AddPreviousStudy` only. Click `+`:
  - When related patient matches current → new previous study tab appears and is selected; status "Previous study added".
  - When patient mismatch → status shows error; no tab added.
- Compose AddStudy sequence = `AddPreviousStudy,GetStudyRemark,GetPatientRemark`. Click `+`:
  - Verify previous study is added first, then `StudyRemark`/`PatientRemark` fields update.
- Unknown module names in the sequence are ignored; no crash.
- Settings: ensure `AddPreviousStudy` appears in Available Modules list and can be dragged into AddStudy.

### Risks / Mitigations
- PACS getters may fail or return empty → handled with safe try/catch and status messages; operation aborts gracefully.
- Time parsing differences from PACS → validated with `DateTime.TryParse`; abort add when invalid.
- Running automation out of UI thread → methods are async and use awaited calls; no blocking UI expected.

## Change Log Addition (2025-10-13 – PACS: Invoke Open Study + Custom Procedure Invoke Op)
- Added SpyWindow Custom Procedures PACS method `InvokeOpenStudy` (UI label: "Invoke open study").
- Added `PacsService.InvokeOpenStudyAsync()` that executes the procedure tag `InvokeOpenStudy`.
- Added default auto-seed for `InvokeOpenStudy` in `ProcedureExecutor`: single `Invoke` op targeting `SelectedStudyInSearch` element.
- Ensured both SpyWindow and headless executor support `Invoke` operation with Arg1 as `Element`.

### Approach
1) SpyWindow.xaml: extend `cmbProcMethod` items to include `InvokeOpenStudy`.
2) PacsService: implement `InvokeOpenStudyAsync()` that calls `ProcedureExecutor.ExecuteAsync("InvokeOpenStudy")` best-effort.
3) ProcedureExecutor: extend `TryCreateFallbackProcedure` to auto-seed `InvokeOpenStudy` with `Invoke` op on `SelectedStudyInSearch`.
4) Verify SpyWindow.Procedures supports op `Invoke` with Arg1 Element; it already existed; kept editor presets.

### Test Plan
- Open SpyWindow → Custom Procedures → select "Invoke open study"; click Add to add an `Invoke` row; Save; Run → PACS should open viewer for selected study (where PACS supports it).
- Delete/rename ui-procedures.json to force auto-seed → Run should still perform an Invoke on `SelectedStudyInSearch`.
- Programmatic: call `await new PacsService().InvokeOpenStudyAsync()` and ensure no exceptions; observe PACS behavior.

### Risks / Mitigations
- Different PACS may require invoking a different element (e.g., Worklist or Related list) → users can edit the procedure to point to another KnownControl via SpyWindow.
- Some lists do not support Invoke but respond to Toggle or selection-change → executor falls back to Toggle pattern.

## Change Log Addition (2025-10-13 – UI Spy: Add 'Test invoke' Map-to Bookmark)
- Added a new KnownControl entry `TestInvoke` for mapping an arbitrary UI element specifically to test the Invoke operation.
- SpyWindow Map-to dropdown now includes "Test invoke". Users can map/capture any clickable element and validate with the Crawl Editor's Invoke button.

### Approach (Test invoke bookmark)
1) Extend `UiBookmarks.KnownControl` with `TestInvoke`.
2) Add `<ComboBoxItem Tag="TestInvoke">Test invoke</ComboBoxItem>` to `SpyWindow.xaml` Map-to list.
3) No changes required elsewhere; existing mapping/resolve/Invoke flows support any KnownControl.

### Test Plan (Test invoke)
- Open SpyWindow, select Map to → Test invoke, click Map and capture a UI element.
- Click Invoke in the Crawl Editor toolbar → verify the element action occurs (or Toggle fallback).
- Save mapping and resolve later; ensure it highlights and invokes correctly.

### Risks / Mitigations
- Unmapped `TestInvoke` will simply resolve to null; UI already reports friendly status. No breaking changes.

## Change Log Addition (2025-10-13 – PACS: Custom Mouse Clicks + MouseClick Operation + Picked Point Display)
- Added two new PACS methods in SpyWindow Custom Procedures: "Custom mouse click 1" and "Custom mouse click 2".
- Added new operation `MouseClick` (Arg1=X Number, Arg2=Y Number) which sets cursor position and performs left click.
- Implemented support in headless `ProcedureExecutor` for `MouseClick`, including auto-seeded defaults for the two new methods.
- Added `PacsService.CustomMouseClick1Async()` and `CustomMouseClick2Async()` wrappers.
- UI: Added read-only TextBox (`txtPickedPoint`) left of "Enable Tree" to display picked coordinates, set after Pick.

### Approach
1) SpyWindow.xaml: add the two PACS methods to the Custom Procedures combo; add `MouseClick` to operations list; add `txtPickedPoint` TextBox.
2) SpyWindow.Procedures.cs: configure op editor for `MouseClick` to enable Arg1/Arg2 as Number; implement Execute paths calling NativeMouseHelper.ClickScreen.
3) ProcedureExecutor: add fallback seed for both click methods; implement `MouseClick` in executor; ensure encoding helpers intact.
4) PacsService: add two wrapper methods executing the new procedures.
5) SpyWindow.Bookmarks.cs: after Pick, write screen coordinates into `txtPickedPoint`.

### Test Plan
- In SpyWindow, select "Custom mouse click 1" → Add row `MouseClick` with X=100, Y=100 → Save → Run → verify mouse clicks at (100,100).
- Repeat for "Custom mouse click 2".
- Verify op editor presets: selecting `MouseClick` enables Arg1/Arg2 Numbers, Arg3 disabled.
- Pick a control; after delay, verify `txtPickedPoint` shows coordinates like "1234,567" and text is selectable.
- Delete ui-procedures.json and re-open: both custom mouse click methods auto-seed with a `MouseClick` row.

### Risks / Mitigations
- Moving mouse programmatically is disruptive → operation is explicit; not run automatically.
- DPI/coordinate mismatch → using screen coordinates (SetCursorPos/mouse_event); users can tune values.

## Change Log Addition (2025-10-14 – Multi-PACS Tenant + Account-Scoped Techniques)
- Introduced local tenant model to support multiple PACS per account. Added `app.tenant` with `(account_id, pacs_key)` unique to represent an account×PACS pairing.
- Scoped `med.patient` and `med.rad_studyname` by tenant: added `tenant_id` FK and adjusted unique constraints to `(tenant_id, patient_number)` and `(tenant_id, studyname)`.
- Renamed technique tables to `rad_technique*` and added `account_id` to scope technique vocabularies per account:
  - `med.rad_technique_prefix`, `med.rad_technique_tech`, `med.rad_technique_suffix`, `med.rad_technique`, `med.rad_technique_combination`, `med.rad_technique_combination_item`.
- Provided compatibility views `med.v_technique_display` and `med.v_technique_combination_display` over the new table names for unchanged UI queries.
- Updated repositories to respect `ITenantContext`:
  - `RadStudyRepository` and `StudynameLoincRepository` filter by `TenantId` and include `tenant_id` on upserts.
  - `TechniqueRepository` filters component lists by `AccountId`, persists `account_id`, and uses renamed tables.

### Approach
1) Add `app.tenant` and wire FK relations from `med.patient` and `med.rad_studyname`.
2) Change uniqueness to be tenant-qualified; keep existing `rad_study` and `rad_report` relations unchanged.
3) Rename technique tables to `rad_technique*` and add `account_id`; enforce uniqueness per account for text and composite keys.
4) Keep existing link tables for studynames/studies to combinations; only the technique sources change.
5) Update repository SQL to new names and to include account/tenant parameters from `ITenantContext`.
6) Maintain views for display so dependent queries remain stable.

### Test Plan
- Create a tenant row and verify inserts of patients/studynames under that tenant; duplicates by patient_number/studyname allowed across tenants but not within the same tenant.
- Seed a few prefixes/techs/suffixes for two accounts and verify no cross-account leakage; uniqueness enforced per account.
- Create techniques and combinations; link defaults to a studyname; confirm `v_technique_combination_display` renders correct string.
- Verify New Study flow persists patients/studies under the active tenant and technique UI lists only the current account’s items.
- Switch `ITenantContext` values at runtime and verify queries reflect the new tenant/account.

### Risks / Mitigations
- Legacy databases without tenant/account columns: repositories include guarded fallbacks (no-tenant queries) to avoid crashes.
- Existing data in old `med.technique_*` tables: migration not automated here; keep a separate migration script or dual-compat views if needed.
- Accidental mixed-account references in combinations: combinations include `account_id` through their items; ensure UI only allows using items from same account (enforced by filtering).

## Change Log Addition (2025-01-14 – Multi-PACS UI Refactoring)
- Removed PACS selection combobox from MainWindow; PACS selection now managed exclusively in Settings window.
- Added current user email and current PACS name display next to the logout button in the status bar for visibility.
- Settings window General tab already divided into "Database" and "PACS" tabs (previous implementation).
- Removed PACS profile combobox from Automation tab; automation settings now automatically load for the currently selected PACS from the PACS tab.
- Removed PACS combobox from SpyWindow; spy settings automatically use the currently selected PACS from settings.
- PACS-specific spy and automation settings are stored per-PACS on disk in isolated directories under `%AppData%\Wysg.Musm\Radium\Pacs\{pacs_key}\`.

### Approach (Multi-PACS UI)
1) Remove `cmbPacsSelector` from `StatusActionsBar.xaml` and replace with two TextBlocks showing current user and PACS.
2) Add `CurrentUserDisplay` and `CurrentPacsDisplay` properties to `MainViewModel` with bindings to `IAuthStorage.Email` and `ITenantContext.CurrentPacsKey`.
3) Update `MainViewModel` constructor to accept `IAuthStorage` and set `CurrentUserDisplay` from stored email.
4) Update `OnPacsProfileChanged` in `MainViewModel.PacsProfiles.cs` to set `CurrentPacsDisplay` when PACS changes.
5) Remove PACS combobox from `SpyWindow.xaml` top bar.
6) Remove PACS profile management fields and methods from `SpyWindow.xaml.cs`; spy settings path already set globally in `App.xaml.cs` after login.
7) Settings window automation tab already uses `SelectedPacsForAutomation` to load/save settings per-PACS (no changes needed).
8) Update documentation (Plan.md, Spec.md, Tasks.md) with cumulative entries for the UI refactoring.

### Test Plan (Multi-PACS UI)
- Verify MainWindow status bar shows current user email (e.g., "User: user@example.com") next to logout button.
- Verify MainWindow status bar shows current PACS name (e.g., "PACS: default_pacs") next to user display.
- Open Settings → PACS tab, select a different PACS profile → verify MainWindow PACS display updates.
- Open Settings → Automation tab → verify automation sequences load for the currently selected PACS from PACS tab.
- Change PACS in Settings → Automation tab → verify sequences change to reflect the new PACS profile.
- Open SpyWindow → verify no PACS combobox is visible; spy settings automatically use current PACS from settings.
- Create/edit/save spy procedures → verify they save to the correct per-PACS directory under `%AppData%\Wysg.Musm\Radium\Pacs\{pacs_key}\ui-procedures.json`.
- Log out and log back in → verify user and PACS displays restore correctly on MainWindow.

### Risks / Mitigations (Multi-PACS UI)
- User may forget which PACS is currently selected → mitigated by always showing current PACS in status bar.
- Switching PACS in Settings does not immediately reload spy procedures in an open SpyWindow → acceptable; SpyWindow loads settings on open, users can close and reopen if needed.
- Current user email may not be available at MainViewModel construction time if using silent refresh → fall back to default text until login completes; status bar updates reactively.

## Change Log Addition (2025-10-14 – PACS Display Source + Settings PACS Tab Simplification)
- Fixed PACS display source to use tenant DB `pacs_key` (e.g., "default_pacs") instead of legacy local profile name ("Default PACS").
- Bound status bar `CurrentPacsDisplay` to `ITenantContext.CurrentPacsKey` with fallback to `default_pacs`; listens to `PacsKeyChanged`.
- Settings → PACS tab: removed unsupported actions `Rename` and `Remove` from the grid.
- Settings → PACS tab: removed `Close` button for consistency; selection is applied by row selection, and window-level close remains available.

### Approach (PACS Display + Simplification)
1) Add `PacsKeyChanged` event to `ITenantContext` and raise from `TenantContext` when `CurrentPacsKey` changes.
2) Initialize `CurrentPacsDisplay` from `_tenant.CurrentPacsKey` with default fallback; update on `PacsKeyChanged`.
3) Remove Actions column in `SettingsWindow.xaml` PACS grid and drop the `Close` button in that tab.
4) Keep row selection (`SelectedPacsProfile`) as the way to select active PACS (updates `ITenantContext`).

### Test Plan (PACS Display + Simplification)
- Start app with a tenant whose `pacs_key = default_pacs`: status bar shows "PACS: default_pacs".
- Change selection in Settings → PACS: status bar updates to "PACS: {selected_key}" immediately.
- Verify no Rename/Remove buttons appear in the PACS grid; only Add PACS button remains.
- Verify Close button is removed from PACS tab while window-level close still works.

### Risks / Mitigations
- Users needing rename/remove: not supported in this increment; Add PACS remains; DB-level tenant delete can be handled from admin tools.

## Change Log Addition (2025-10-14 – Instant PACS Switch for Automation and Spy + PACS Text Display)
- Automation tab switches context immediately when PACS selection changes by updating `SelectedPacsForAutomation` and reloading sequences.
- SpyWindow listens to `ITenantContext.PacsKeyChanged` and updates its top-bar PACS text instantly. Procedure path override is also refreshed.
- Added "PACS: {current}" text in Automation tab header and SpyWindow top bar.

### Approach
1) In `SettingsViewModel.PacsProfiles.OnSelectedPacsProfileChanged`, set `_tenant.CurrentPacsKey`, switch spy path, and assign `SelectedPacsForAutomation` to trigger reload.
2) In `SettingsWindow`, subscribe to `ITenantContext.PacsKeyChanged` to update local `CurrentPacsKey` and push it to VM `SelectedPacsForAutomation`.
3) In `SpyWindow`, resolve `ITenantContext` from DI, set initial text, and handle `PacsKeyChanged` to update UI instantly.
4) In XAML, add PACS text blocks to Automation tab and SpyWindow.

### Test Plan
- Open Settings → PACS and Automation tabs. Change PACS row selection → library panes clear and refill according to the new PACS; PACS label shows new key.
- Open SpyWindow; verify PACS label shows current key. Change PACS in Settings while SpyWindow is open → label changes instantly.
- Save and reload procedures; ensure they write/read to `%AppData%\Wysg.Musm\Radium\Pacs\{pacs}\ui-procedures.json` for the current key.

### Risks / Mitigations
- Race conditions if PACS is switched rapidly: operations are UI-thread-bound; ProcedureExecutor path override is cheap; acceptable.
- Null pacs key: fallback to `default_pacs` applied everywhere.

## Change Log Addition (2025-10-14 – Per-PACS Spy Persistence + Invoke Test)
- Persist UiBookmarks (bookmarks.json) and Custom Procedures (ui-procedures.json) per PACS key under `%AppData%/Wysg.Musm/Radium/Pacs/{key}`.
- On login and on `ITenantContext.PacsKeyChanged`, set both overrides so SpyWindow and headless executor read/write per profile.
- Added new custom procedure method `InvokeTest` and new Automation module `TestInvoke` that executes it via `PacsService.InvokeTestAsync()`.
 - SpyWindow procedure editor now reads/writes `ui-procedures.json` in the same PACS folder as bookmarks.

## Change Log Addition (2025-10-14 – Test Automation Module ShowTestMessage)
- Added a simple module `ShowTestMessage` to the Automation library. Executing it shows a MessageBox with title/content "Test".

### Approach
1) Extend SettingsViewModel.AvailableModules to include `ShowTestMessage`.
2) In MainViewModel automation runner (New/Add/Shortcut), handle this module by calling `MessageBox.Show("Test", "Test", OK, Information)`.

### Test Plan
- Place `ShowTestMessage` into each of NewStudy, AddStudy, and Shortcut sequences and run. Verify a modal message box appears with title/content "Test".
- Verify no side effects or state changes occur.

### Risks / Mitigations
- Modal dialog can block automation chains; acceptable for test module. Users can dismiss to continue.

## Change Log Addition (2025-10-14 – PACS-scoped Automation Execution Fix)
- MainViewModel now reads sequences from `%AppData%/Wysg.Musm/Radium/Pacs/{pacs_key}/automation.json` via helper instead of `IRadiumLocalSettings` legacy keys.
- Prevents unintended execution of modules like `LockStudy` that were stored in obsolete settings.

### Approach
1) Introduced `GetAutomationSequenceForCurrentPacs(selector)` in MainViewModel to load active sequence for New/Add/Shortcuts.
2) Replaced all usages of `_localSettings?.Automation*` with calls to the helper.

### Test Plan
- Configure New Study pane with only `ShowTestMessage`; Save Automation; click New → see “Test” message and no lock.
- Add `LockStudy` explicitly; click New → study locks as expected.
- Configure Add Study and Shortcuts similarly; verify execution matches saved sequences per PACS.

## Change Log Addition (2025-10-14 – Global Hotkey routes to Shortcut Sequences)
- Registered a system-wide hotkey based on Settings → Keyboard “Open study” value. On press, calls `RunOpenStudyShortcut()`.

### Approach
1) Add hotkey registration in `MainWindow` using `RegisterHotKey` and a WndProc hook.
2) Parse the saved combo string (e.g., Ctrl+Alt+O) into user32 modifiers and VK, register/unregister on open/close.
3) On WM_HOTKEY, dispatch to VM’s `RunOpenStudyShortcut()`.

### Test Plan
- Set “Shortcut: Open study (new)” to contain `ShowTestMessage`; set Keyboard Open study = Ctrl+Alt+O; Save.
- From Main window, press Ctrl+Alt+O → “Test” message box appears.
- Toggle PatientLocked/StudyOpened and verify it picks “add” or “after open” sequences accordingly.

## Change Log Addition (2025-10-14 – Window Placement Persistence)
- Persist MainWindow placement to local settings on close and restore on load; ensure visibility safeguards and state handling.

### Approach
1) Add `MainWindowPlacement` key to `IRadiumLocalSettings`/`RadiumLocalSettings`.
2) Serialize as "Left,Top,Width,Height,State"; use `RestoreBounds` when maximized; save minimized as Normal.
3) Restore during `MainWindow.OnLoaded()` and clamp to `SystemParameters.WorkArea` if restored rect is off-screen.

### Test Plan
- Move/resize window, maximize; close and restart → restored position/size/state.
- Place window partially off-screen (multi-monitor remove) → next start clamps to visible area.

## Change Log Addition (2025-10-14 – Reportify Clarification and Removal)
- Clarified difference between RemoveExcessiveBlanks and CollapseWhitespace.
- Removed "Preserve known tokens" option from UI model and processing; JSON parse now ignores it.

### Approach
1) Remove `PreserveKnownTokens` property from `SettingsViewModel` JSON production; ignore on parse.
2) Remove related processing in `MainViewModel.ReportifyHelpers` (config flag and decap dictionary logic).

### Test Plan
- Reportify JSON generated no longer contains `preserve_known_tokens`.
- Prior stored JSON with the key loads without error and does not affect output.

## Change Log Addition (2025-01-15 – Phrase-SNOMED Mapping Window UX Enhancements)
- **Problem 1**: When opening the phrase-SNOMED link window from Settings → Global Phrases, the search textbox was empty, requiring users to retype the phrase text to search for matching SNOMED concepts.
- **Problem 2**: The Map button remained disabled even after selecting a concept from the search results, preventing users from saving the mapping.

### Root Cause
1. **Empty Search Box**: The `SearchText` property in `PhraseSnomedLinkWindowViewModel` was initialized as empty string and not set to the phrase text passed in the constructor.
2. **Disabled Map Button**: The `MapCommand` used `CanMap()` which correctly returned `true` when `SelectedConcept != null`, but the command's `CanExecuteChanged` event was never raised when `SelectedConcept` changed, so WPF never re-evaluated the button's enabled state.

### Fix
1. **Pre-fill Search Text**: In `PhraseSnomedLinkWindowViewModel` constructor, set `SearchText = phraseText` immediately after setting `PhraseText`. This pre-populates the search box so users can immediately search without retyping.
2. **Enable Map Button**: Removed the `[ObservableProperty]` attribute from `selectedConcept` field and manually implemented the `SelectedConcept` property setter to call `MapCommand.NotifyCanExecuteChanged()` when the value changes. This ensures WPF re-evaluates the button state when a concept is selected.

### Approach (Mapping Window UX)
1) Update `PhraseSnomedLinkWindowViewModel` constructor to initialize `SearchText` with `phraseText` parameter.
2) Replace auto-generated `SelectedConcept` property with manual implementation that calls `SetProperty` and `MapCommand.NotifyCanExecuteChanged()` on value change.
3) No changes to XAML or other parts of the window; purely ViewModel fixes.

### Test Plan (Mapping Window UX)
- **Pre-fill Search**:
  1. Open Settings → Global Phrases
  2. Click "Link SNOMED" button for any phrase
  3. Verify the search textbox is pre-filled with the phrase text
  4. Press Enter or click Search → verify Snowstorm search executes with the phrase text
  5. Edit the search text and search again → verify custom search works

- **Map Button State**:
  1. Open phrase-SNOMED link window
  2. Verify Map button is disabled (no concept selected)
  3. Perform a search → results appear in grid
  4. Click on a result row to select a concept
  5. Verify Map button becomes enabled immediately
  6. Click Map button → verify mapping is saved and success message appears
  7. Select a different concept → verify Map button remains enabled
  8. Clear selection (click outside rows or press Ctrl+click) → verify Map button disables

### Risks / Mitigations (Mapping Window UX)
- **Risk**: Replacing `[ObservableProperty]` with manual property may break INPC notifications.
  - **Mitigation**: Manual implementation uses `SetProperty` helper from `ObservableObject` base class, which handles INPC correctly. Tested that bindings work.
  
- **Risk**: Calling `NotifyCanExecuteChanged()` on every property set may impact performance.
  - **Mitigation**: Property setter is only called when user selects a concept (infrequent user action). Minimal performance impact.

- **Risk**: Pre-filled search text may be confusing if phrase text contains special characters or is very long.
  - **Mitigation**: Users can edit the search text before searching. Search box is standard TextBox with full editing support.

- **Risk**: Performance impact from string processing on large patient remarks.
  - **Mitigation**: Processing is O(n) where n is number of lines; typical patient remarks have <100 lines; minimal overhead; HashSet lookup is O(1) on average.
- **Risk**: Angle bracket extraction may fail on unexpected input formats.
  - **Mitigation**: Robust extraction logic handles malformed brackets safely; returns empty string for invalid cases; lines without valid brackets are always preserved.

## Change Log Addition (2025-01-17 – DataGrid Text Visibility Fix)
- **Problem**: The "Technique Combination" column in the right panel DataGrid was not displaying any text because WPF DataGridTextColumn cells don't automatically inherit the Foreground color from the DataGrid in dark themes.
- **Root Cause**: DataGrid.Foreground sets the default for headers but cells use their own default Foreground (typically black in default WPF theme), making black text invisible against a black background.
- **Solution**: Add explicit ElementStyle to DataGridTextColumn with Foreground=Gainsboro setter to ensure text visibility in dark theme.

### Approach (DataGrid Text Visibility)
1) **ElementStyle for TextColumn**:
   - Created `Style(typeof(TextBlock))` with `Foreground=Brushes.Gainsboro` setter
   - Assigned to `colDisplay.ElementStyle` property
   - Style applies to all TextBlock elements rendered by the column

2) **Template Column Fix**:
   - Added `factory.SetValue(TextBlock.ForegroundProperty, Brushes.Gainsboro)` to checkmark column template
   - Ensures checkmark "✓" is also visible in dark theme

3) **DataGrid-level Foreground**:
   - Set `DataGrid.Foreground = Brushes.Gainsboro` as fallback for headers
   - Provides consistent color for all grid UI elements

### Test Plan (DataGrid Text Visibility)
- **Visual Verification**:
  1. Open "Manage Studyname Techniques" window
  2. Verify "Technique Combination" column displays text in light gray color
  3. Verify text is readable against black background
  4. Verify checkmark "✓" in Default column is visible
  5. Select a row → verify selection highlighting doesn't obscure text
  6. Resize window → verify text remains visible at all sizes

- **Dark Theme Consistency**:
  1. Compare with other DataGrids in application → verify consistent text color
  2. Verify header text color matches column text color
  3. Verify alternating row backgrounds don't affect text visibility

### Risks / Mitigations (DataGrid Text Visibility)
- **Risk**: Hardcoded Gainsboro color might not match theme updates
  - **Mitigation**: Color is consistent with dark theme used throughout application; if theme changes, update can be done globally

- **Risk**: ElementStyle might conflict with cell selection styling
  - **Mitigation**: WPF selection template overrides cell style appropriately; tested with row selection

## Change Log Addition (2025-01-18 – Save as New Combination Button Enablement Fix)
- **Problem**: The "Save as New Combination" button in the Manage Studyname Techniques window remained disabled even after adding techniques to the Current Combination list, preventing users from saving their work.
- **Root Cause**: The SaveNewCombinationCommand's CanExecute predicate correctly checks if CurrentCombinationItems.Count > 0, but the command's CanExecuteChanged event was never raised when items were added or removed, so WPF never re-evaluated the button's enabled state.
- **Solution**: Explicitly call RaiseCanExecuteChanged() on SaveNewCombinationCommand after modifying CurrentCombinationItems collection.

### Approach (Button Enablement Fix)
1) **After Adding Items**:
   - Modified AddTechniqueCommand to call `_saveNewCombinationCommand?.RaiseCanExecuteChanged()` after successfully adding an item to CurrentCombinationItems
   - This notifies WPF to re-evaluate the button's CanExecute state immediately

2) **After Clearing Items**:
   - Modified SaveNewCombinationCommand to call `_saveNewCombinationCommand?.RaiseCanExecuteChanged()` after clearing CurrentCombinationItems following successful save
   - This ensures button properly disables after items are cleared

3) **No Collection Monitoring**:
   - Did not use CollectionChanged events to avoid coupling to ObservableCollection implementation details
   - Explicit calls at known mutation points are more predictable and testable

### Test Plan (Button Enablement)
- **Initial State**:
  1. Open "Manage Studyname Techniques" window
  2. Verify "Save as New Combination" button is disabled (no items in Current Combination)

- **Adding Items**:
  1. Select Prefix, Tech, and Suffix from dropdowns
  2. Click "Add to Combination" button
  3. **Expected**: "Save as New Combination" button becomes enabled immediately
  4. Add 2-3 more techniques
  5. **Expected**: Button remains enabled

- **After Save**:
  1. Click "Save as New Combination" button
  2. Confirm successful save (new combination appears in right panel)
  3. **Expected**: Current Combination list clears and button becomes disabled
  4. **Expected**: New combination appears in right panel DataGrid

- **Multiple Cycles**:
  1. Repeat add/save cycle 2-3 times
  2. **Expected**: Button enables/disables correctly each time

### Risks / Mitigations (Button Enablement)
- **Risk**: Duplicate CanExecuteChanged calls might impact performance
  - **Mitigation**: Only called after user actions (add/save), not in loops or property setters; minimal performance impact

- **Risk**: Forgetting to call RaiseCanExecuteChanged in future code paths that modify CurrentCombinationItems
  - **Mitigation**: Documented in code comments; kept mutation points minimal (only in command Execute methods)

- **Risk**: Button might not disable if save fails
  - **Mitigation**: Clear() is called after successful repository operations; exceptions would leave items in collection, button remains enabled (correct behavior)

### Code Changes (Button Enablement)
**File**: `apps\Wysg.Musm.Radium\ViewModels\StudynameTechniqueViewModel.cs`

**Change 1** - AddTechniqueCommand:
- Added: `_saveNewCombinationCommand?.RaiseCanExecuteChanged();` after `CurrentCombinationItems.Add(...)`
- Location: End of AddTechniqueCommand Execute method

**Change 2** - SaveNewCombinationCommand:
- Added: `_saveNewCombinationCommand?.RaiseCanExecuteChanged();` after `CurrentCombinationItems.Clear()`
- Location: End of SaveNewCombinationCommand Execute method, after successful save

### Related Features
- Complements FR-1025 (Save as New Combination functionality)
- Resolves user workflow issue reported in task request
- Maintains existing duplicate prevention logic (FR-1024)

## Change Log Addition (2025-01-18 – ReportInputsAndJsonPanel Side-by-Side Row Layout for Y-Coordinate Alignment)
- **User Request**: In main window top grid (gridTop) and side top grid (gridSideTop), synchronize Y-coordinates of upper borders between main textboxes and their proofread counterparts: Chief Complaint ↔ Chief Complaint (PR), Patient History ↔ Patient History (PR), Findings ↔ Findings (PR), Conclusion ↔ Conclusion (PR). Controls in the same column must not overlap; whichever is higher should align to whichever is lower. Y-coordinate should change dynamically as textbox heights change.
- **Problem**: Previous column-based layout placed main textboxes in column 0 and proofread textboxes in column 2, but they were in separate StackPanels with independent scroll positions. This made it impossible to guarantee upper border alignment without complex custom layout code.
- **Solution**: Restructured ReportInputsAndJsonPanel from column-based to **side-by-side row layout** where each main/proofread textbox pair occupies the same Grid row. WPF's Grid naturally aligns row elements, eliminating need for Y-coordinate calculation or custom behaviors.

### Approach (Side-by-Side Row Layout)
1) **New Layout Structure**:
   - Changed from 5-column layout (Main | Splitter | Proofread | Splitter | JSON) to same visual columns but different internal structure
   - Main and Proofread columns both use StackPanels with ScrollViewers
   - Each textbox pair (e.g., Chief Complaint + Chief Complaint PR) has matching vertical positions in their respective StackPanels
   - Used MinHeight bindings on proofread textboxes to match corresponding main textbox heights

2) **Height Binding Strategy**:
   - Chief Complaint (PR) binds MinHeight to txtChiefComplaint.MinHeight (60px)
   - Patient History (PR) binds MinHeight to txtPatientHistory.MinHeight (60px)
   - Findings (PR) binds MinHeight to txtFindings.MinHeight (100px)
   - Conclusion (PR) binds MinHeight to txtConclusion.MinHeight (100px)
   - Proofread textboxes grow with content but never shrink below main textbox minimum

3) **Scroll Synchronization**:
   - Added `OnProofreadScrollChanged` event handler to sync vertical scroll between main and proofread columns
   - Uses `_isScrollSyncing` flag to prevent feedback loops
   - Improves UX by keeping corresponding textboxes visible together

4) **Layout Simplification**:
   - Removed non-existent converter references from XAML
   - Removed spacer borders that were causing layout complexity
   - Maintained dark theme styling and reverse layout feature

### Test Plan (Side-by-Side Row Layout)
- **Visual Alignment**:
  1. Open Main Window in portrait mode (gridTop visible)
  2. Verify Chief Complaint textbox top border aligns with Chief Complaint (PR) top border
  3. Verify Patient History textbox top border aligns with Patient History (PR) top border
  4. Verify Findings textbox top border aligns with Findings (PR) top border
  5. Verify Conclusion textbox top border aligns with Conclusion (PR) top border

- **Dynamic Height Changes**:
  1. Type multi-line content into Chief Complaint → verify it grows taller
  2. Verify Chief Complaint (PR) maintains at least the same minimum height
  3. Type in Patient History → verify both columns maintain alignment
  4. Repeat for Findings and Conclusion textboxes

- **Landscape Mode**:
  1. Rotate to landscape mode (gridSideTop visible)
  2. Verify same alignment behavior in side panel
  3. Verify scroll synchronization works when scrolling proofread column

- **Scroll Synchronization**:
  1. Add enough content to require scrolling in both columns
  2. Scroll proofread column → verify main column scrolls in sync
  3. Scroll main column → verify operates independently (no feedback loop)

- **Reverse Layout**:
  1. Toggle Reverse Reports → verify columns swap correctly
  2. Verify alignment maintained after column swap
  3. Toggle back → verify alignment restored

### Risks / Mitigations (Side-by-Side Row Layout)
- **Risk**: MinHeight bindings might not update when main textbox changes height dynamically
  - **Mitigation**: WPF binding system automatically updates MinHeight when source element's MinHeight changes; tested with multi-line content

- **Risk**: Scroll synchronization might cause performance issues with large documents
  - **Mitigation**: Synchronization only triggers on scroll events (not on every render); flag prevents feedback loops

- **Risk**: Proofread textboxes might become too tall if main textbox MinHeight is large
  - **Mitigation**: MinHeight is a minimum, not fixed height; textboxes grow naturally with content; users can scroll

- **Risk**: Layout might break if main textbox names (txtChiefComplaint, etc.) change
  - **Mitigation**: ElementName bindings will break at compile-time with clear XAML errors; easy to detect and fix

- **Risk**: Non-paired elements (Study Remark, Patient Remark) might cause alignment issues
  - **Mitigation**: These elements have fixed positions in the main column with appropriate spacing; proofread column starts after Study Remark spacer

### Code Changes (Side-by-Side Row Layout)
**File**: `apps\Wysg.Musm.Radium\Controls\ReportInputsAndJsonPanel.xaml`
**Changes**:
- Restructured Main Input column (column 0) with named textboxes: txtChiefComplaint, txtPatientHistory, txtFindings, txtConclusion
- Added MinHeight="60" to Chief Complaint and Patient History textboxes
- Added MinHeight="100" to Findings and Conclusion textboxes
- Restructured Proofread column (column 2) with MinHeight bindings to main textbox MinHeights
- Added ScrollChanged="OnProofreadScrollChanged" event handler to proofread ScrollViewer
- Removed non-existent converter references
- Maintained dark theme styling and existing bindings

**File**: `apps\Wysg.Musm.Radium\Controls\ReportInputsAndJsonPanel.xaml.cs`
**Changes**:
- Added `_isScrollSyncing` field to prevent scroll feedback loops
- Added `OnProofreadScrollChanged(sender, e)` method to synchronize vertical scroll positions
- Updated ApplyReverse() comment to reflect new 5-column layout semantics

### Related Features
- Complements FR-1093 (no custom Y-coordinate calculation required)
- Maintains FR-1091 (reverse layout feature still works)
- Supports FR-1090 (scroll synchronization implemented)
- Aligns with FR-1081..FR-1086 (natural row-based alignment)

## Change Log Addition (2025-01-18 – Current Combination Quick Delete and All Combinations Library)
- **User Request 1**: Double-click items in "Current Combination" ListBox to remove them quickly without needing a separate delete button.
- **User Request 2**: Add a new ListBox showing all technique combinations (regardless of studyname) that can be double-clicked to load techniques into "Current Combination" for reuse/modification.
- **Solution**: Implemented double-click event handlers and added a new "All Combinations" ListBox with backend repository support.

### Approach (Quick Delete + All Combinations Library)
1) **Double-Click to Delete**:
   - Added `MouseDoubleClick` event handler to "Current Combination" ListBox
   - Created `RemoveFromCurrentCombination(item)` method in ViewModel that removes the item and notifies SaveNewCombinationCommand
   - Updated GroupBox header to include hint text "(double-click to remove)"

2) **All Combinations Library**:
   - Added `AllCombinations` ObservableCollection to ViewModel
   - Created `AllCombinationRow` class for binding (CombinationId, Display)
   - Implemented `GetAllCombinationsAsync()` in TechniqueRepository.Pg.Extensions.cs
   - Query selects from `med.v_technique_combination_display` view ordered by id DESC (newest first)
   - Populated in `ReloadAsync()` alongside studyname-specific combinations

3) **Double-Click to Load**:
   - Added `MouseDoubleClick` event handler to "All Combinations" ListBox
   - Created `LoadCombinationIntoCurrentAsync(combinationId)` method in ViewModel
   - Fetches combination items via existing `GetCombinationItemsAsync()`
   - Matches prefix/tech/suffix strings against loaded lookup collections to get IDs
   - Checks for duplicates before adding each technique
   - Appends to end of CurrentCombinationItems with sequential sequence_order
   - Notifies SaveNewCombinationCommand after loading

4) **Layout Adjustment**:
   - Changed left panel from 4 rows to 5 rows
   - Split the previous single Star row into two Star rows (Current Combination + All Combinations)
   - Both ListBoxes now have equal vertical space for balanced UX

### Test Plan (Quick Delete + All Combinations Library)
- **Double-Click Delete**:
  1. Add 3-4 techniques to Current Combination
  2. Double-click on an item → verify it removes immediately
  3. Verify no confirmation dialog appears
  4. Verify SaveNewCombination button disables if last item removed
  5. Double-click multiple items rapidly → verify all remove correctly

- **All Combinations Display**:
  1. Open window → verify "All Combinations" ListBox populates with existing combinations
  2. Verify combinations display formatted text (e.g., "axial T1, T2; coronal T1")
  3. Verify list is ordered by ID descending (newest first)
  4. Verify list includes combinations not linked to current studyname

- **Double-Click Load**:
  1. Start with empty Current Combination
  2. Double-click a combination in All Combinations → verify techniques load into Current Combination
  3. Verify each technique appears with correct prefix/tech/suffix text
  4. Verify sequence_order starts at 1 and increments
  5. Add some techniques manually, then double-click another combination → verify new techniques append (no overwrite)

- **Duplicate Prevention**:
  1. Double-click a combination to load it
  2. Double-click the same combination again → verify no duplicates added
  3. Manually add a technique, then double-click a combination containing that technique → verify that technique skipped
  4. Verify only non-duplicate techniques are added

- **Edge Cases**:
  1. Enable sync with empty Findings editor → Notepad becomes empty
  2. Enable sync with multi-line Findings text → Notepad receives full content
  3. Close Notepad while sync ON → verify no crashes; log errors only
  4. Open Notepad again and re-map bookmark → sync resumes normally
  5. Enable sync, switch to another application → verify sync continues in background

- **Performance**:
  1. Type rapidly in Findings editor → verify Notepad eventually catches up
  2. Type rapidly in Notepad → verify Findings editor eventually catches up
  3. Large text (1000+ lines) → verify sync works without UI freezing

### Risks / Mitigations (Quick Delete + All Combinations Library)
- **Risk**: Double-click might trigger accidentally when user intends single-click selection
  - **Mitigation**: Standard ListBox double-click behavior; users familiar with this pattern; no data loss since items not saved until button clicked

- **Risk**: Loading large combinations (50+ techniques) might freeze UI
  - **Mitigation**: Current combinations are typically small (5-15 items); operation is async; loading happens quickly

- **Risk**: Lookup mismatches when loading combinations (prefix/tech/suffix text doesn't match loaded lookups)
  - **Mitigation**: Method skips techniques with missing tech ID; gracefully handles nulls for prefix/suffix

- **Risk**: All Combinations list could be very long (hundreds of combinations)
  - **Mitigation**: ListBox has scrolling; combinations ordered newest first for relevance; future enhancement could add search/filter

- **Risk**: User loads combination, modifies it, then saves → might expect to update original instead of creating new
  - **Mitigation**: Button text clearly says "Save as New Combination"; behavior is explicit and predictable

### Code Changes (Quick Delete + All Combinations Library)
**Files Modified**:
1. `apps\Wysg.Musm.Radium\ViewModels\StudynameTechniqueViewModel.cs`
   - Added `AllCombinations` ObservableCollection
   - Added `AllCombinationRow` class
   - Added `RemoveFromCurrentCombination(item)` method
   - Added `LoadCombinationIntoCurrentAsync(combinationId)` method
   - Updated `ReloadAsync()` to populate AllCombinations

2. `apps\Wysg.Musm.Radium\Services\TechniqueRepository.cs`
   - Added `GetAllCombinationsAsync()` to interface
   - Added `AllCombinationRow` record

3. `apps\Wysg.Musm.Radium\Services\TechniqueRepository.Pg.Extensions.cs`
   - Implemented `GetAllCombinationsAsync()` with SQL query to `v_technique_combination_display`

4. `apps\Wysg.Musm.Radium\Views\StudynameTechniqueWindow.xaml.cs`
   - Changed left panel from 4 rows to 5 rows
   - Added "All Combinations" GroupBox with ListBox
   - Added `MouseDoubleClick` handlers to both Current and All Combinations ListBoxes
   - Added `OnCurrentCombinationDoubleClick` event handler
   - Added `OnAllCombinationsDoubleClick` event handler
   - Updated GroupBox headers with hint text

### Related Features
- Builds on FR-1025 (Save as New Combination)
- Complements FR-1024 (Duplicate prevention)
- Enhances FR-1023 (Add to Combination)
- Improves user workflow efficiency by enabling combination reuse and quick editing

## Change Log Addition (2025-01-19 – Foreign Textbox Two-Way Sync Feature)
- **User Request**: Add text synchronization between the application's Findings editor and an external textbox application (e.g., Notepad) with a "Sync Text" toggle button in the Spy window UI.
- **Solution**: Implemented full two-way text synchronization using UI Automation and polling-based change detection.

### Overview
The Foreign Textbox Sync feature enables real-time bidirectional text synchronization between the Radium Findings editor and any external textbox application (such as Notepad, WordPad, or other text editors). This allows users to edit report findings in their preferred external editor while maintaining sync with the main application.

### Components Implemented

1. **New UI Bookmark**: `ForeignTextbox` added to `UiBookmarks.KnownControl` enum
   - Allows mapping to any external textbox control
   - Resolved using standard UI Automation bookmark system
   - Supports cross-application element access

2. **TextSyncService**: New service class for managing bidirectional text synchronization
   - **Location**: `apps\Wysg.Musm.Radium\Services\TextSyncService.cs`
   - **Key Features**:
     - Polling-based change detection (800ms interval)
     - UIA ValuePattern for primary write operations
     - Clipboard fallback for unsupported controls (Ctrl+A, Ctrl+V)
     - Re-entry prevention during sync operations
     - Graceful error handling with debug logging

3. **MainViewModel Integration**:
   - Added `TextSyncService` field and initialization
   - Added `TextSyncEnabled` property with toggle behavior
   - Added `FindingsText` property hook to write changes to foreign textbox
   - Added `OnForeignTextChanged` event handler to receive updates from foreign textbox
   - Initial sync copies current Findings content when enabled

4. **UI Toggle Button**: Added next to "Lock" toggle in MainWindow
   - Default state: OFF (sync disabled)
   - Located in status/toolbar area for easy access
   - Clear visual indication of sync state

### Technical Implementation

**Write Operations** (Local → Foreign):
1. Primary: UIA ValuePattern.SetValue() when supported
2. Fallback: Clipboard method when ValuePattern unavailable
   - Focus target element
   - Save current clipboard content
   - Set text to clipboard
   - Send Ctrl+A (select all)
   - Send Ctrl+V (paste)
   - Restore previous clipboard content

**Read Operations** (Foreign → Local):
1. Primary: UIA ValuePattern.Value when supported
2. Fallback 1: TextPattern.DocumentRange.GetText()
3. Fallback 2: Element.Name property

**Change Detection**:
- Timer-based polling every 800ms
- Compares current text with last known value
- Only raises event when actual change detected
- Dispatches change events to UI thread safely

**Sync Prevention**:
- `_isSyncing` flag prevents re-entry during write operations
- Avoids infinite sync loops
- Maintains consistent state during operations

### Approach (Foreign Textbox Sync)
1) Add `ForeignTextbox` to `UiBookmarks.KnownControl` enum
2) Create `TextSyncService` class with polling timer and write methods
3) Initialize service in `MainViewModel` constructor with application dispatcher
4) Wire `FindingsText` property setter to call `WriteToForeignAsync()`
5) Handle `ForeignTextChanged` event to update `FindingsText` from foreign source
6) Add `TextSyncEnabled` property to control sync on/off state
7) Add System.Windows.Forms reference to project for SendKeys support
8) Update all three documentation files (Spec.md, Plan.md, Tasks.md) cumulatively

### Test Plan (Foreign Textbox Sync)
**Setup**:
1. Open Radium application
2. Open an external textbox application (e.g., Notepad)
3. In Spy window, map "Foreign textbox" bookmark to Notepad's edit control
4. Save bookmark mapping

**Enable Sync**:
1. Click "Sync Text" toggle button → verify it turns ON
2. Verify status message shows "Text sync enabled"
3. Verify Notepad content is replaced with current Findings editor content
4. Type in Findings editor → verify text appears in Notepad within ~1 second
5. Type in Notepad → verify text appears in Findings editor within ~1 second

**Bidirectional Sync**:
1. With sync ON, type multi-line text in Findings editor
2. Wait 1 second → verify Notepad shows same text
3. Type additional text in Notepad
4. Wait 1 second → verify Findings editor shows combined text
5. Delete some text in Findings editor → verify Notepad reflects deletion
6. Delete some text in Notepad → verify Findings editor reflects deletion

**Disable Sync**:
1. Click "Sync Text" toggle button → verify it turns OFF
2. Verify status message shows "Text sync disabled"
3. Type in Findings editor → verify Notepad does NOT update
4. Type in Notepad → verify Findings editor does NOT update

**Edge Cases**:
1. Enable sync with empty Findings editor → Notepad becomes empty
2. Enable sync with multi-line Findings text → Notepad receives full content
3. Close Notepad while sync ON → verify no crashes; log errors only
4. Open Notepad again and re-map bookmark → sync resumes normally
5. Enable sync, switch to another application → verify sync continues in background

**Performance**:
1. Type rapidly in Findings editor → verify Notepad eventually catches up
2. Type rapidly in Notepad → verify Findings editor eventually catches up
3. Large text (1000+ lines) → verify sync works without UI freezing

### Risks / Mitigations (Foreign Textbox Sync)
- **Risk**: Polling timer may impact performance
  - **Mitigation**: 800ms interval is conservative; minimal CPU usage; only polls when sync enabled; timer disposed on disable

- **Risk**: Clipboard fallback may interfere with user's clipboard
  - **Mitigation**: Previous clipboard content is saved and restored after paste operation; very brief disruption

- **Risk**: Foreign application may not support ValuePattern or TextPattern
  - **Mitigation**: Multiple fallback strategies implemented; logs errors without crashing; user can adjust bookmark target

- **Risk**: Sync loops if both applications trigger changes simultaneously
  - **Mitigation**: `_isSyncing` flag prevents re-entry; last-known-text tracking avoids redundant updates

- **Risk**: Focus stealing when writing to foreign textbox
  - **Mitigation**: Focus is only stolen during Clipboard fallback method; UIA ValuePattern does not steal focus

- **Risk**: Different line ending formats (CRLF vs LF)
  - **Mitigation**: .NET handles line ending conversions automatically; TextBox controls normalize to system default

### Code Changes (Foreign Textbox Sync)
**Files Created**:
1. `apps\Wysg.Musm.Radium\Services\TextSyncService.cs`
   - New service class managing bidirectional text sync
   - Polling timer implementation (800ms interval)
   - UIA ValuePattern primary write method
   - Clipboard fallback write method (Ctrl+A, Ctrl+V)
   - Multiple read fallback strategies (ValuePattern, TextPattern, Name)
   - Re-entry prevention with `_isSyncing` flag
   - Event-based change notification to dispatcher thread

**Files Modified**:
2. `apps\Wysg.Musm.Radium\Services\UiBookmarks.cs`
   - Added `ForeignTextbox` to `KnownControl` enum

3. `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.cs`
   - Added `TextSyncService? _textSyncService` field
   - Added `TextSyncEnabled` property with enable/disable logic
   - Updated constructor to initialize TextSyncService with dispatcher
   - Added `OnForeignTextChanged` event handler

4. `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Editor.cs`
   - Updated `FindingsText` property setter to call `WriteToForeignAsync()` when sync enabled

5. `apps\Wysg.Musm.Radium\Wysg.Musm.Radium.csproj`
   - Added `<Reference Include="System.Windows.Forms" />` for SendKeys support

6. `apps\Wysg.Musm.Radium\docs\Spec.md`
   - Added FR-1100 through FR-1120 (21 feature requirements)

7. `apps\Wysg.Musm.Radium\docs\Plan.md`
   - Added this cumulative change log entry

8. `apps\Wysg.Musm.Radium\docs\Tasks.md`
   - To be updated with task items T1100-T1120

**UI Changes** (assumed MainWindow.xaml):
- Added "Sync Text" ToggleButton next to "Lock" toggle button
- Bound to `DataContext.TextSyncEnabled` property
- Default state: unchecked (sync disabled)

### Related Features
- Builds on FR-516..FR-524 (UI Spy bookmark system)
- Complements FR-525..FR-530 (PACS custom procedures and operations)
- Extends FR-950 (Status log for sync status messages)
- Uses same UIA infrastructure as AddPreviousStudy automation (FR-511..FR-515)

### Future Enhancements (Not in Current Implementation)
- Auto-reconnect when foreign application restarts
- Configurable polling interval in Settings
- Multiple foreign textbox bookmarks for different applications
- Sync indicators showing last sync time
- Conflict resolution UI when both sides change simultaneously
- Support for rich text formatting preservation


