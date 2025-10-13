# Implementation Plan: Radium Cumulative (Reporting Workflow + Editor + Mapping + PACS)

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

