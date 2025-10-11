# Implementation Plan: Radium Cumulative (Reporting Workflow + Editor + Mapping + PACS)

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

## Change Log Addition (2025-01-10 - Report Header Component Fields & Real-Time Formatting)
- Added four new header component fields to report JSON: `chief_complaint`, `patient_history`, `comparison`, `study_techniques`.
- Implemented real-time header formatting in `MainViewModel.Editor.cs`:
  - Added public properties for the four component fields with change notification and JSON update triggers.
  - Changed `HeaderText` setter to private; header is now automatically computed from component fields.
  - Implemented `UpdateFormattedHeader()` method applying all formatting rules:
    - Clinical information line shown with "NA" when chief_complaint is empty but patient_history exists.
    - Patient history lines prefixed with "- ".
    - Techniques line shown only when study_techniques is not empty.
    - Comparison shown as "NA" when empty but other header content exists; omitted entirely when all header content is empty.
    - Final header is trimmed.
- Updated `UpdateCurrentReportJson()` to serialize new fields.
- Updated `ApplyJsonToEditors()` to deserialize and apply new fields, triggering header formatting update.
- Header updates are real-time: any change to component fields immediately updates the formatted header display.

## Change Log Addition (2025-10-11 - JSON-driven Header Recompute)
- Adjusted header update pipeline so that editing `CurrentReportJson` directly and changing any of the header component fields recomputes `HeaderText` immediately.
- Removed suppression that prevented `UpdateFormattedHeader()` during JSON apply; header now refreshes in real time on JSON edits.
- Safeguards remain to prevent recursion and maintain `_raw*` backing fields integrity for findings/conclusion.

### Approach
1) In `UpdateFormattedHeader()`, remove early return that skipped updates when `_updatingFromJson` was true.
2) Keep `ApplyJsonToEditors()` calling `UpdateFormattedHeader()` after setting the component backing fields.
3) Preserve private setter for `HeaderText` to keep it computed-only.

### Test Plan
- Edit `CurrentReportJson` in the side/bottom JSON editor to set: 
  - only `chief_complaint` → header shows Clinical information.
  - only `patient_history` (multi-line) → header shows Clinical information: NA and lines with “- ” prefix.
  - empty `comparison` with other content → shows "Comparison: NA".
  - empty `comparison` with no other header content → no Comparison line.
- Verify header updates immediately as JSON is edited (PropertyChanged).

## Change Log Addition (2025-10-11 - Header UI Inputs + Read-only Header)
- Added UI editors in `MainWindow.xaml` for: Study Remark, Patient Remark, Chief Complaint, Patient History; placed in a left column next to the JSON editor.
- Set `EditorHeader` to read-only to prevent direct edits; header remains computed from component fields.
- Bound editors two-way to `StudyRemark`, `PatientRemark`, `ChiefComplaint`, `PatientHistory`.
- Turned off line numbers on the two mini editors.
- Added `Edit Study Technique` and `Edit Comparison` buttons as entry points for future dialogs.

### Approach
1) Expose `IsReadOnly` and `ShowLineNumbers` DPs on `EditorControl`; bind through to inner `MusmEditor`.
2) Update `MainWindow.xaml` top area: split into a 3-column grid (left editors, splitter, right JSON).
3) Mark `EditorHeader` as `IsReadOnly=True`.
4) Keep existing JSON bindings; header recompute continues to run on property and JSON changes.

### Test Plan
- Typing in Chief Complaint / Patient History editors updates HeaderText in real time.
- Editing StudyRemark/PatientRemark updates JSON fields accordingly.
- The header editor cannot be edited (no caret insertion or text change).
- Line numbers are hidden for Chief Complaint and Patient History editors.

## Approach (Header Component Fields)
1) Store component fields separately instead of free-form header text.
2) Compute formatted header on-demand using conditional logic for each section.
3) Round-trip component fields through JSON for persistence.
4) Prevent direct editing of HeaderText (computed property).

## Test Plan (Header Component Fields)
- Set chief_complaint only → verify "Clinical information: {text}" appears.
- Set patient_history only → verify "Clinical information: NA" appears with history lines prefixed by "- ".
- Set both chief_complaint and patient_history → verify chief_complaint shown first, then history lines.
- Set study_techniques → verify "Techniques: {text}" appears.
- Set comparison → verify "Comparison: {text}" appears.
- Leave comparison empty with other content → verify "Comparison: NA" appears.
- Leave all fields empty → verify header is empty (no "Comparison: " line).
- Verify round-trip: set fields, check JSON, parse JSON, verify fields restored.
- Verify multi-line patient_history → each line prefixed with "- ".

## Risks / Mitigations (Header Component Fields)
- Users cannot directly edit the header text; must edit component fields. Consider adding UI controls for each field or allow editing via JSON view.
- Formatting logic must be kept in sync with user expectations; any changes to formatting rules require code update.
- Multi-line patient_history uses simple line split; complex formatting (nested bullets, etc.) not supported initially.
- **Crash Fix (2025-01-10)**: Added initialization guard (`_isInitialized` flag) to prevent `UpdateFormattedHeader()` and `UpdateCurrentReportJson()` from executing during ViewModel construction. The flag is set to `true` at the end of the constructor, and safe wrapper methods (`SafeUpdateJson()`, `SafeUpdateHeader()`) check this flag before executing.

## Change Log Addition (2025-01-10 - Snippet Empty Text Option Support)
- Fixed snippet option parsing to allow empty text values (e.g., `0^` for empty string choice).
- Modified `CodeSnippet.ParseOptions()` to:
  - Accept options where the text portion is empty string (text after `^` separator).
  - Only require non-empty key (before `^` separator).
  - Handle edge cases like `0^|1^and the pons` where first option has key "0" and empty text "".
- Example use case: `${1^pons=0^|1^and the pons}` now correctly provides two options:
  - Key "0" → empty string (nothing inserted)
  - Key "1" → "and the pons"
- Previous behavior: Empty text options were skipped entirely due to `if (key.Length > 0 && text.Length > 0)` check.
- New behavior: Empty text allowed, only key must be non-empty via `if (key.Length > 0)` check.

## Approach (Empty Text Options)
1) Modified ParseOptions to handle `^` separator with optional trailing text.
2) Extract text using substring with bounds check: `(idx < tok.Length - 1) ? tok.Substring(idx + 1).Trim() : string.Empty`.
3) Accept option when key is non-empty regardless of text length.
4) Empty text options insert nothing (empty string) when selected in mode 1/3, or contribute nothing to joined output in mode 2.

## Test Plan (Empty Text Options)
- Create snippet: `${1^severity=1^mild|2^moderate|3^severe} degree of microangiopathy in the bilateral cerebral white matter ${1^pons=0^|1^and the pons}`
- Insert snippet and navigate to second placeholder (pons).
- Press '0' → verify empty string is inserted (nothing appears).
- Insert snippet again, press '1' → verify "and the pons" is inserted.
- Verify both options appear in popup with keys "0" and "1".

## Risks / Mitigations (Empty Text Options)
- Empty text might be confusing to users; consider UI feedback showing "(nothing)" for empty options in popup.
- Current implementation shows empty string as-is which is acceptable for medical report templates where omission is intentional.

## Change Log Addition (2025-01-10 - Snippet Logic Implementation Fixes)
- Fixed snippet completion display to show "{trigger} → {description}" instead of "{trigger} → {snippet text}":
  - Updated `MusmCompletionData.Snippet` factory to use `snippet.Shortcut` and `snippet.Description` in content string.
  - Updated `EditorCompletionData.ForSnippet` to use same format pattern.
  - ToString() method now returns the properly formatted display string.
- Implemented proper mode extraction from placeholder syntax:
  - Mode number is now extracted from the index prefix (1^, 2^, 3^) in placeholder definitions.
  - Modified `CodeSnippet.Expand()` to parse mode from first digit of index for indices like 1, 2, 3, or 10-39, or 100+.
  - Mode determines placeholder behavior: 1=immediate single choice, 2=multi-choice with joiner, 3=multi-char single replace.
- Fixed free text placeholder fallback logic:
  - Added tracking in `Session` class to record whether current placeholder was modified during typing.
  - `SelectPlaceholder` now records original text and resets modification flag when switching placeholders.
  - `OnDocumentChanged` detects edits within free-text placeholder bounds and marks as modified.
  - `ApplyFallbackAndEnd` only applies "[ ]" replacement for unmodified free text; keeps typed text if modified.
- Improved mode-specific key handling:
  - Mode 1: Single alphanumeric key immediately selects matching option and completes placeholder.
  - Mode 3: Accumulates multi-char keys in buffer until Tab is pressed for matching.
  - Free text: Allows normal typing without interference (key events not handled by snippet mode).
  - Mode 2: Space or matching letter toggles selection in multi-choice popup.
- Enhanced placeholder navigation:
  - Tab completes current placeholder with appropriate logic per mode and moves to next.
  - Enter exits snippet mode with fallback and moves caret to next line.
  - Escape exits snippet mode with fallback and moves caret to end of inserted snippet.
  - Arrow keys, Home, End are blocked to keep caret within active placeholder bounds.

## Approach (Snippet Logic)
1) Mode extraction: Parse first digit of placeholder index to determine behavior mode (1, 2, or 3).
2) Modification tracking: Track document changes within placeholder bounds to determine if user typed vs left default.
3) Smart fallback: Apply "[ ]" only for unmodified free text; preserve user typing otherwise.
4) Mode-specific input: Handle single-key immediate (mode 1), multi-char buffered (mode 3), and multi-select toggle (mode 2).
5) Cursor confinement: Prevent navigation outside active placeholder during snippet mode.

## Test Plan (Snippet Logic)
- Free text placeholder:
  - Insert snippet with `${free text}` placeholder.
  - Type some text → Tab → verify typed text is kept and cursor moves to next placeholder or end.
  - Insert again, don't type → Esc → verify "[ ]" is inserted for unfilled placeholder.
- Mode 1 (immediate single choice):
  - Insert snippet with `${1^fruit=a^apple|b^banana}`.
  - Press 'a' → verify immediate replacement with "apple" and completion.
  - Insert again, press neither → Esc → verify first option "apple" is used as fallback.
- Mode 2 (multi-choice):
  - Insert snippet with `${2^items^or=a^cola|b^cider|c^juice}`.
  - Press 'a' Space 'c' Tab → verify "cola or juice" is inserted.
  - Insert again, press nothing → Esc → verify all options joined with "or" are inserted.
- Mode 3 (multi-char single replace):
  - Insert snippet with `${3^code=aa^apple|bb^banana}`.
  - Type 'aa' Tab → verify "apple" is inserted.
  - Insert again, type 'zz' Tab → verify first option "apple" is used (no match).
  - Insert again, type nothing → Esc → verify first option "apple" is used as fallback.

## Risks / Mitigations (Snippet Logic)
- If placeholder syntax is malformed, mode may default to 0 (free text behavior).
- If user types partial match in mode 3 then exits without Tab, first option is used as safe fallback.
- Modification tracking depends on document change events firing within placeholder bounds; robust for normal typing scenarios.

## Change Log Addition (2025-10-10 - Automation modules: Study/Patient Remarks)
- Added two automation modules to Settings → Automation:
  - GetStudyRemark: fetches PACS method "Get current study remark" and updates `study_remark` in current report JSON.
  - GetPatientRemark: fetches PACS method "Get current patient remark" and updates `patient_remark` in current report JSON.
- Extended `SettingsViewModel.AvailableModules` to include `GetStudyRemark` and `GetPatientRemark` so users can drag them into sequences.
- `MainViewModel`:
  - Added properties `StudyRemark`, `PatientRemark` and wired them into `CurrentReportJson` serialization.
  - In New Study automation executor, recognized the two modules and fetch via `PacsService.GetCurrentStudyRemarkAsync()` / `GetCurrentPatientRemarkAsync()` and set the properties.
- Remarks update status messages for quick visual feedback.

## Change Log Addition (2025-10-10 - Fix: Patient Remark bookmark)
- Introduced a distinct known control `PatientRemark` in `UiBookmarks.KnownControl` and exposed it in SpyWindow → Known controls combo.
- Reason: Previously only `StudyRemark` existed, leading to "Get current patient remark" procedures accidentally using the study remark bookmark.
- Result: Users can now map the patient remark UI element separately and reference it in the `GetCurrentPatientRemark` procedure.

## Change Log Addition (2025-10-10 - Auto-seed key procedures for Remarks)
- Implemented auto-seeding in `ProcedureExecutor`:
  - When `GetCurrentPatientRemark` has no saved procedure, the system creates and persists a default procedure with a single `GetText` step on Element=`PatientRemark`.
  - Similarly, if `GetCurrentStudyRemark` is missing, auto-seed with `GetText` on Element=`StudyRemark`.
- Effect: "GetPatientRemark" module always has a working key procedure by default and remains user-editable in SpyWindow.

## Change Log Addition (2025-10-10 - Enforcement of patient_remark source)
- Enforced that `patient_remark` is only populated from the `GetCurrentPatientRemark` procedure result by ignoring `patient_remark` edits in `CurrentReportJson` parsing; `StudyRemark` remains round-trippable.

## Change Log Addition (2025-10-10 - ProcedureExecutor GetHTML + Replace + early-return bug)
- Implemented `Replace` op and `GetHTML` op in the procedure executor for parity with the SpyWindow Custom Procedures UI.
- Fixed an early return in `ExecuteInternal` that prevented fallback auto-seeding and could lead to returning the previous step’s value when `GetHTML` was present.
- Registered `CodePagesEncodingProvider` and added light-weight charset handling to decode HTML using header/meta charsets.

## Change Log Addition (2025-01-10 - Snippet Completion Display)
- Updated completion item formatting to use "{trigger} → {description}" consistently.
- Applied in `MusmCompletionData.Snippet` and `EditorCompletionData.ForSnippet` (content + ToString override).
- Tooltip continues to show the full snippet template; preview shows first placeholder preview.

## Change Log Addition (2025-01-10 - Completion uses Snippet Description + Enter handling)
- Completion provider now pulls snippet `description` from DB and displays "{trigger} → {description}"; falls back to first line of template when description is empty.
- Enter key on completion window now commits the selected item without inserting a raw newline; if no selection, Enter closes and inserts newline.
- Updated service contract to return description alongside text and AST.

## Change Log Addition (2025-01-10 - Enter handling + Exit snippet mode)
- MusmCompletionWindow + EditorControl intercept Enter to commit selected completion items and cancel raw newline.
- When no selection: if snippet mode active, delegate Enter to SnippetInputHandler; else insert newline and close popup.
- SnippetInputHandler already ends snippet mode on Enter (ApplyFallbackAndEnd(moveToNextLine: true)).

Tests
- Invoke completion, select a snippet, press Enter → snippet inserts, no extra newline.
- With popup open and no selection, press Enter → newline and close (unless snippet mode active, then snippet handler ends mode and moves to next line).
- During snippet mode, press Enter → mode ends and caret moves to next line.

## Change Log Addition (2025-01-10 - Apply-all placeholder fallbacks)
- Implemented apply-all fallback replacements when exiting snippet mode (Enter/Esc).
- Mode 1 fallback uses first option text even when empty (e.g., `${1^pons=0^|1^and the pons}` falls back to empty string).

Test Plan (Apply-all)
- Insert `${1^severity=1^mild|2^moderate|3^severe} ... ${1^pons=0^|1^and the pons}`
  - Press Enter immediately: result uses "mild" and "" for pons; caret on next line.
  - Press Esc immediately: same replacements; caret at end of snippet.
- Insert with free-text placeholders; without modifying current free-text, press Esc → current and other free-text replaced with "[ ]"; if current was modified, keep its content.

## Change Log Addition (2025-01-10 - Enter newline at end + Mode 1 key handling)
- Enter now moves caret to end of snippet and inserts a newline; caret ends on next line.
- Mode 1 accepts numpad digits for selection.
- Mode 1 ignores non-matching keys; placeholder text remains unchanged.

Test Plan
- Insert a snippet and immediately press Enter → all placeholders receive fallbacks; caret moves to end, newline inserted, caret on next line.
- Mode 1 options with numeric keys: press NumPad1/NumPad2 → correct selection and completion.
- Mode 1: press a letter not in options → nothing changes; caret remains; placeholder unchanged.

## Change Log Addition (2025-01-10 - Caret anchor + Mode 1 ignore)
- Introduced TextAnchor at snippet end to correctly place caret after all programmatic replacements.
- Mode 1 key handling ignores non-matching keys (consumes event; no mutation).

Test Plan
- Snippet with two Mode 1 placeholders, select '1' then '1' → "mild ... and the pons"; caret must be exactly after "pons".
- Mode 1: press 'a' during placeholder → placeholder remains unchanged; event consumed; no stray "a" appears.

## Change Log Addition (2025-01-10 - Key locking + Dark popup + Tab accept)
- Implemented key lock for Mode 1/3 placeholders to consume non-navigation/non-control keys.
- Styled PlaceholderCompletionWindow to dark theme.
- Tab now accepts selected item for Mode 1/3.

Test Plan
- Mode 1/3 placeholder: press punctuation or letters not in options → no text appears; event consumed.
- Popup is dark (bg ~ #1E1E1E, fg ~ #DCDCDC).
- With popup open on Mode 1/3, press Tab → selected option inserts and current placeholder completes; caret moves to next placeholder or snippet end.

## Change Log Addition (2025-01-10 - Placeholder Tab + Completion Space)
- Placeholder popup: Tab is forwarded to snippet handler and no raw tab is inserted; Mode 1/3 complete with selected item.
- Main completion: Space key commits selection and is not inserted into the editor.

Test Plan
- Mode 1/3 placeholder: open placeholder popup; press Tab → inserts selected item; moves to next placeholder.
- Completion window: with an item selected, press Space → inserts suggestion; no space is inserted into document.

## Approach
1) Surface modules in Automation library list (already bound).
2) Procedure-first design via `PacsService` that executes procedure tags with retry.
3) Auto-seed default procedures at first invocation when missing and persist to `ui-procedures.json`.
4) Distinct bookmarks `StudyRemark` and `PatientRemark` avoid cross-reading.
5) Enforce patient remark provenance: JSON Apply ignores `patient_remark` to prevent accidental overwrite.
6) Ensure `GetHTML` op executes and returns the fetched HTML instead of preserving prior step output.

## Notes
- After update, open Spy → map `PatientRemark` to the patient remark field.
- The system will create the default `GetCurrentPatientRemark` procedure on first run if none exists. You can modify it later in SpyWindow → Custom Procedures.

## Test Plan (Manual)
- Procedures grid:
  - Create steps: [Arg1 Var=var1] [Op=Replace] to form a URL in var2 (optional) → [Op=GetHTML, Arg1 Type=Var, Value=var2] → Verify final output shows HTML content.
  - Ensure Arg1 Type=Var references the correct prior variable name, e.g., `var2`.
- Verify fallback:
  - Delete `%APPDATA%\Wysg.Musm\Radium\ui-procedures.json`, run GetPatientRemark module; confirm `GetCurrentPatientRemark` is auto-created and persisted.
- Verify enforcement:
  - Edit `CurrentReportJson` to change `patient_remark`; check that the bound PatientRemark value does not change and is only set when the automation module runs.

## Risks / Mitigations
- If the bookmark for `PatientRemark` is not mapped, `GetCurrentPatientRemark` returns `(no element)`. Map once in SpyWindow.
- If `GetHTML` URL is not http/https or empty, the step returns `(no url)` to signal misconfiguration.

Backlog
- Wire Add Study sequence execution path to honor the modules (T507).

## Change Log Addition (2025-01-11 – Separate Toggle Effects)
- Reverse Reports now affects ONLY portrait panels (top/bottom):
  - `gridTopChild.Reverse = reversed`
  - `gridBottomControl.Reverse = reversed`
- Align Right now affects ONLY side panels (landscape):
  - `gridSideTop.Reverse = right`
  - `gridSideBottom.Reverse = right`
- Removed prior cross-effects so toggles are independent.

### Approach (Separate Toggle Effects)
1) Update `UpdateGridSideLayout` to flip only side panels.
2) Update `SwapReportEditors` to flip only top/bottom panels.
3) Set `gridBottomControl` default `Reverse=False`; driven by Reverse Reports toggle.

### Test Plan (Separate Toggle Effects)
- Toggle Reverse Reports: verify only top/bottom panels swap; side panels unchanged.
- Toggle Align Right: verify only side panels swap; top/bottom unchanged.
- Toggle both: verify expected independent behavior in both panel groups.

## Change Log Addition (2025-01-11 – 1:1 Column Widths for Top/Side Panels)
- Modified `ReversibleColumnsGrid.xaml` to use `* | 2 | *` column widths to enforce 1:1 sizing for left/right panels.
- Affects `ReportInputsAndJsonPanel` used by `gridTopChild` and `gridSideTop`.
- Matches layout behavior of `PreviousReportTextAndJsonPanel` (already `* | 2 | *`).

### Test Plan (1:1 Column Widths)
- Resize window and verify left and right areas under `gridTopChild` stay equal width.
- Resize window and verify left and right areas under `gridSideTop` stay equal width.

