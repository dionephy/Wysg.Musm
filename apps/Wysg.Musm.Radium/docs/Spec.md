# Feature Specification: Radium (Cumulative)

> **⚠️ DEPRECATION NOTICE (2025-01-19)**  
> This file is being phased out in favor of an archive-based structure.
> 
> **Please use instead:**
> - **Current features**: [Spec-active.md](Spec-active.md) (last 90 days)
> - **Historical features**: [archive/](archive/) (organized by quarter and domain)
> - **Complete index**: [archive/README.md](archive/README.md)
> 
> This file will be removed on 2025-02-18 after the transition period.
> See [MIGRATION.md](MIGRATION.md) for details.

---

## Update: Technique Combination Grouped String (2025-10-13)
- FR-500 Technique grouped display: Build a single string from a technique combination by grouping items by (prefix, suffix) pair, preserving first-seen order (by sequence_order). Within each group, join tech names with ", ". Join groups with "; ".
  - Example input techniques (sequence order respected):
    - axial T1, axial T2, coronal T1 of sellar fossa, coronal T2 of sellar fossa, coronal CE-T1 of sellar fossa, sagittal T1, sagittal T1 of sellar fossa, sagittal CE-T1 of sellar fossa
  - Example output string:
    - axial T1, T2; sagittal T1; coronal T1, T2, CE-T1 of sellar fossa; sagittal T1, CE-T1 of sellar fossa
- FR-501 Implementation MUST ignore empty tech rows; prefix/suffix may be blank and must be trimmed.
- FR-502 Duplicates within same group (same tech text) MUST be collapsed in the rendered output.
- FR-503 API surface: `TechniqueFormatter.BuildGroupedDisplay(IEnumerable<TechniqueGroupItem>)` where item has Prefix, Tech, Suffix, SequenceOrder.

## Update: New Study Autofill Techniques (2025-10-13)
- FR-504 New Study flow MUST auto-fill the header component field `study_techniques` after studyname is loaded when a default technique combination exists for the studyname.
- FR-505 Auto-filled content MUST use the grouped display logic (FR-500) built from the default combination items.
- FR-506 Repository MUST provide methods to resolve studyname id from text, fetch default combination id, and fetch combination items with component texts ordered by sequence_order.

## Update: Refresh Current Study Techniques on Default Change (2025-10-13)
- FR-507 When a new default technique combination is created or set for the current studyname, the current study’s `study_techniques` field MUST refresh to reflect the new default without restarting New Study.
- FR-508 Implementation MAY invoke a VM method `MainViewModel.RefreshStudyTechniqueFromDefaultAsync()` after window close or after save in the builder window.

## Update: Edit Study Technique – Duplicate Rule (2025-10-13)
- FR-509 Within a single technique combination build session, duplicates by exact (prefix_id, tech_id, suffix_id) MUST be prevented at add time.
- FR-510 On save, the system MUST ensure the persisted list is unique by the same triple; first occurrence order preserved and sequence numbers compacted (1..N).

## Update: Add Previous Study Automation Module (2025-10-13)
- FR-511 Add a module named `AddPreviousStudy` to Automation tab → Available Modules list.
- FR-512 Module behavior:
  - Step 1: Run `GetCurrentPatientNumber`; if it matches the application's current `PatientNumber` (after normalization), continue; else abort with status "Patient mismatch".
  - Step 2: Read from Related Studies list: `GetSelectedStudynameFromRelatedStudies`, `GetSelectedStudyDateTimeFromRelatedStudies`, `GetSelectedRadiologistFromRelatedStudies`, `GetSelectedReportDateTimeFromRelatedStudies`.
  - Step 3: Read current report text via dual getters and pick longer variants: `GetCurrentFindings`/`GetCurrentFindings2`, `GetCurrentConclusion`/`GetCurrentConclusion2`.
  - Step 4: Persist/create previous study tab using existing local DB (`EnsureStudyAsync`, `UpsertPartialReportAsync`), refresh `PreviousStudies`, select the new tab, set `PreviousReportified=true`.
- FR-513 Errors MUST not throw; status text updated on failure and previous selection preserved.

## Update: Map Add study in Automation to '+' Button (2025-10-13)
- FR-514 Automation tab MUST expose an ordered list: `AddStudy` sequence (already present). Clicking the small `+` button in Previous Reports section MUST execute the configured modules in `AddStudy` sequence in order.
- FR-515 Supported modules in `AddStudy` include: `AddPreviousStudy`, `GetStudyRemark`, `GetPatientRemark`. Unknown modules are ignored.

## Update: PACS Custom Procedure – Invoke Open Study (2025-10-13, updated 2025-10-15)
- FR-516 SpyWindow Custom Procedures MUST include a PACS method `InvokeOpenStudy` labeled "Invoke open study".
- FR-517 `PacsService` MUST expose `InvokeOpenStudyAsync()` which runs the procedure tag `InvokeOpenStudy`.
- FR-518 No fallback or auto-seed MUST exist for `InvokeOpenStudy`. The procedure MUST be authored explicitly by the user per PACS. If it is missing, the system MUST throw an error indicating the procedure is not defined for the current PACS profile.
- FR-518a Per-PACS storage: UiBookmarks and Custom Procedures are stored under `%AppData%/Wysg.Musm/Radium/Pacs/{pacs_key}/` so each PACS profile maintains its own configuration.

## Update: OpenStudy Reliability and Sequencing (2025-10-15)
- FR-710 Automation modules MUST execute sequentially in the exact order configured by the user in New/Add/Shortcut sequences. No fire-and-forget interleaving.
- FR-711 `OpenStudy` MUST execute after prior modules complete. `PacsService.InvokeOpenStudyAsync()` MUST retry the custom procedure a few times (default 3 attempts, 200ms+ backoff) to allow UI stabilization. If the procedure is undefined, it MUST throw immediately (no retry/fallback).
- FR-712 `AddPreviousStudy` MUST abort when the selected related study has null/empty studyname OR null/empty studydatetime.
- FR-713 `AddPreviousStudy` MUST abort when the selected related study matches the current study by both studyname (case-insensitive) and study datetime (exact match after parse).
- FR-714 `AddPreviousStudy` MUST NOT block subsequent modules with heavy previous-studies reload; after persisting the previous study and report, the list reload/selection MAY run in the background.
- FR-715 Remark acquisition helpers (`GetStudyRemark`, `GetPatientRemark`) MUST be awaited within sequencing to preserve order.

## Update: Custom Procedure Operation – Invoke (2025-10-13)
- FR-519 Add a new operation `Invoke` to Custom Procedures. Behavior:
  - Arg1 Type MUST be `Element` and map to a `UiBookmarks.KnownControl` entry.
  - Execution MUST attempt UIA `Invoke` pattern; if not supported, attempt `Toggle` pattern.
  - Preview text MUST show "(invoked)" on success.
- FR-520 SpyWindow op editor MUST preconfigure Arg1 Type=`Element` and disable Arg2/Arg3 for `Invoke`.
- FR-521 Headless `ProcedureExecutor` MUST support `Invoke` with same behavior as SpyWindow.

## Update: UI Spy Map-to Bookmark – Test invoke (2025-10-13)
- FR-522 UI Spy MUST include a new KnownControl `TestInvoke` to allow mapping any arbitrary element for Invoke testing.
- FR-523 SpyWindow Map-to dropdown MUST list "Test invoke" and save/load its mapping like other known controls.
- FR-524 No additional logic required beyond existing Map/Resolve/Invoke flows; this is a convenience target.

## Update: PACS Custom Mouse Clicks + Procedure Operation (2025-10-13)
- FR-525 Add new PACS methods in Custom Procedures: "Custom mouse click 1" and "Custom mouse click 2".
- FR-526 Add new operation `MouseClick` to Custom Procedures with Arg1=X (Number), Arg2=Y (Number). Action: move cursor to screen X,Y and perform a left click.
- FR-527 Headless `ProcedureExecutor` MUST support `MouseClick` with identical behavior.
- FR-528 `PacsService` MUST expose wrappers `CustomMouseClick1Async()` and `CustomMouseClick2Async()` to run the respective procedures.
- FR-529 Provide auto-seeded defaults for both click methods consisting of a single `MouseClick` op with 0,0 coordinates (user will edit).

## Update: SpyWindow Picked Point Display (2025-10-13)
- FR-530 Add a read-only selectable TextBox to the left of the "Enable Tree" checkbox to display picked point screen coordinates.
- FR-531 After performing Pick, show the captured mouse position as "X,Y" in the new TextBox.

[Other existing sections unchanged]

## Update: Settings → Automation modules and Keyboard (2025-10-13)
- FR-540 Add three new Automation modules to library: `OpenStudy`, `MouseClick1`, `MouseClick2`.
  - `OpenStudy` executes PACS procedure tag `InvokeOpenStudy`.
  - `MouseClick1`/`MouseClick2` execute their respective procedure tags (headless wrappers in PacsService).
- FR-541 Settings window MUST include a new tab named "Keyboard".
  - Provide two capture TextBoxes: "Open study" and "Send study".
  - When focused, pressing a key combination (Ctrl/Alt/Win + key allowed) MUST render human-readable text in the TextBox (e.g., "Ctrl+Alt+S").
  - Clicking Save persists values to local settings as `hotkey_open_study` and `hotkey_send_study`.
  - Note: only capture+persist is implemented; global system-wide hook/registration occurs in a later increment.

## Update: Settings → Automation panes for Open Study Shortcut (2025-10-13)
- FR-545 Add three new panes under Automation tab to configure sequences for the Open Study global hotkey:
  - Shortcut: Open study (new) → executes when PatientLocked == false.
  - Shortcut: Open study (add) → executes when PatientLocked == true and StudyOpened == false.
  - Shortcut: Open study (after open) → executes when PatientLocked == true and StudyOpened == true.
- FR-546 Persist sequences locally: `auto_shortcut_open_new`, `auto_shortcut_open_add`, `auto_shortcut_open_after_open`.
- FR-547 MainViewModel MUST expose `RunOpenStudyShortcut()` that selects and executes the appropriate sequence.

## Update: Main Window Toggles (2025-10-13)
- FR-542 Replace the small icon-only toggle next to "Study locked" with a text toggle "Study opened" bound to `StudyOpened`.
- FR-543 `StudyOpened` MUST be toggled on programmatically when module `OpenStudy` runs.
- FR-544 Remove the icon-only "reportified" toggle in the previous report area; keep the text "Reportified" toggle elsewhere unchanged.

## Update: Multi-PACS Tenant Model + Account-Scoped Techniques (2025-10-14)
- FR-600 Multi-PACS tenancy: A local tenant represents a unique (account_id × PACS combination). Persist tenants in `app.tenant` with `(account_id, pacs_key)` unique.
- FR-601 Patient tenancy: `med.patient` MUST include `tenant_id` FK to `app.tenant`. Patient uniqueness MUST be `(tenant_id, patient_number)`.
- FR-602 Studyname tenancy: `med.rad_studyname` MUST include `tenant_id` FK to `app.tenant`. Studyname uniqueness MUST be `(tenant_id, studyname)`.
- FR-603 Repository behavior: RadStudyRepository and StudynameLoincRepository MUST scope CRUD/queries to current `ITenantContext.TenantId` when set (>0).
- FR-604 Technique table rename: All technique-related tables MUST be prefixed with `rad_technique`:
  - `med.rad_technique_prefix`, `med.rad_technique_tech`, `med.rad_technique_suffix`, `med.rad_technique`,
  - `med.rad_technique_combination`, `med.rad_technique_combination_item`.
- FR-605 Technique account-scope: All technique tables MUST include `account_id` and enforce uniqueness per account (e.g., `(account_id, prefix_text)`, `(account_id, prefix_id, tech_id, suffix_id)`).
- FR-606 Views compatibility: Provide views `med.v_technique_display` and `med.v_technique_combination_display` that join the new table names and preserve display behavior.
- FR-607 UI compatibility: Technique UI must continue to list and create components/combos for the current account; scoping based on `ITenantContext.AccountId`.
- FR-608 Default technique resolution: Existing default resolution via `med.rad_studyname_technique_combination` remains; combinations are now produced from account-scoped technique tables.
- FR-609 Backward compatibility: If `TenantId` is 0 (unset), repositories may fallback to non-tenant queries (legacy DBs) for read/ensure operations.

## Update: PACS Display Source and Settings PACS Simplification (2025-10-14)
- FR-610 The status bar MUST display the current PACS using `ITenantContext.CurrentPacsKey` from the local DB (e.g., "default_pacs").
- FR-611 The application MUST raise an event when `CurrentPacsKey` changes so listeners can update UI reactively.
- FR-612 Settings → PACS tab MUST NOT expose Rename/Remove actions; selection is applied by row selection.
- FR-613 Settings → PACS tab MUST omit a dedicated Close button; users close the window using the window close control.

## Update: Instant PACS Switch for Automation and Spy (2025-10-14)
- FR-620 When PACS selection changes in Settings → PACS, the Automation tab MUST immediately switch its active PACS context (load sequences for the new key).
- FR-621 When PACS selection changes, SpyWindow MUST immediately reflect the current PACS context for procedure storage and display the current PACS key.
- FR-622 Automation tab MUST display text "PACS: {current_pacs_key}" near its header area.
- FR-623 SpyWindow MUST display text "PACS: {current_pacs_key}" in the top bar and update live when the key changes.

## Update: Per-PACS Spy Persistence + Invoke Test (2025-10-14)
- FR-630 UI bookmarks map and custom procedures MUST be saved per PACS profile under: `%AppData%/Wysg.Musm/Radium/Pacs/{pacs_key}/bookmarks.json` and `ui-procedures.json`.
- FR-631 On login and whenever `CurrentPacsKey` changes, both ProcedureExecutor and UiBookmarks MUST switch their storage paths to the current PACS directory.
- FR-632 Add a new PACS method `InvokeTest` available in SpyWindow’s Custom Procedures list. Default auto-seed uses a single `Invoke` op targeting `TestInvoke` KnownControl.
- FR-633 Add a new Automation module `TestInvoke` that runs the `InvokeTest` custom procedure via `PacsService.InvokeTestAsync()`.
 - FR-634 SpyWindow custom PACS methods list and editor MUST load/save procedures from the per-PACS `ui-procedures.json` (not the legacy global file).

## Update: Test Automation Module – ShowTestMessage (2025-10-14)
- FR-640 Add an Automation module `ShowTestMessage` which displays a modal message box with title "Test" and content "Test" when executed.
- FR-641 Module must be executable from all sequences: NewStudy, AddStudy, and all OpenStudy Shortcut sequences.

## Update: PACS-scoped Automation Execution (2025-10-14)
- FR-650 MainViewModel MUST execute automation from the PACS-scoped `automation.json` for the current PACS key, not from legacy local settings.
- FR-651 Only modules present in the saved sequence are executed; no implicit modules (e.g., `LockStudy`) are auto-inserted.

## Update: Window Placement Persistence (2025-10-14)
- FR-670 On app close, persist MainWindow placement (Left, Top, Width, Height, State) to local settings.
- FR-671 On app start, restore previous placement; clamp to visible work area if off-screen; maximize honored; minimize saved as Normal.

## Update: Reportify Clarifications + Toggle Removal (2025-10-14)
- FR-680 Clarify: "Remove excessive blanks" collapses repeated spaces within a line to a single space. "Collapse whitespace" reduces any whitespace (spaces, tabs) to a single space after other line-normalization steps; both operate per-line, with CollapseWhitespace stronger and applied later.
- FR-681 Remove "Preserve known tokens" from Reportify settings and processing. Any previously stored value is ignored during parse. UI toggle and sample removed.

## Update: Global Hotkey – Open Study Shortcut Execution (2025-10-14)
- FR-660 Application registers a global hotkey from Settings (Keyboard → Open study). When pressed, it MUST invoke `MainViewModel.RunOpenStudyShortcut()`.
- FR-661 The invoked shortcut sequence MUST honor the PACS-scoped `automation.json` panes (new/add/after open). Modules like `ShowTestMessage` must execute if present.

## Update: New PACS Method Items - InvokeSendReport and SendReportWithoutHeader (2025-02-09, updated 2025-02-10)
- FR-1190 Add new PACS method "Invoke send report" (`InvokeSendReport`) to SpyWindow Custom Procedures dropdown for primary send report automation.
- FR-1191 Add new PACS method "Send report without header" (`SendReportWithoutHeader`) to SpyWindow Custom Procedures dropdown for sending reports without header information (replaced SendReportRetry on 2025-02-10).
- FR-1192 `PacsService` MUST expose `InvokeSendReportAsync()` wrapper that executes the `InvokeSendReport` custom procedure tag.
- FR-1193 `PacsService` MUST expose `SendReportWithoutHeaderAsync()` wrapper that executes the `SendReportWithoutHeader` custom procedure tag (replaced SendReportRetryAsync on 2025-02-10).
- FR-1194 Both methods MUST return `Task<bool>` and always return `true` after procedure execution (success/failure determined by PACS state validation).
- FR-1195 Custom procedures for both methods MUST be configured per-PACS profile in SpyWindow (no auto-seeded defaults provided).
- FR-1196 Rationale: InvokeSendReport provides primary send report entry point; SendReportWithoutHeader enables sending reports without header component, which is commonly needed for certain PACS workflows.
- FR-1197 User workflow: Configure procedures in SpyWindow using bookmark resolution, click operations, delays, and validation checks; test using Run button before deployment.
- FR-1198 These methods complement existing `SendReport` method (which accepts findings/conclusion parameters) and provide alternative entry points for PACS-specific automation.

## Update: New Custom Procedure Operation - SetValue (2025-02-09)
- FR-1200 Add new operation "SetValue" to SpyWindow Custom Procedures with Arg1=Element (target control) and Arg2=String or Var (value to set).
- FR-1201 SetValue operation MUST use UIA `ValuePattern.SetValue()` to programmatically set text field and control values.
- FR-1202 Operation MUST validate element supports ValuePattern before attempting to set value; return `(no value pattern)` if unsupported.
- FR-1203 Operation MUST check `ValuePattern.IsReadOnly` property; return `(read-only)` if true without attempting to set value.
- FR-1204 Arg2 MUST accept both String (literal) and Var (variable reference) types for flexible value sourcing.
- FR-1205 Null values in Arg2 MUST be converted to empty string before setting.
- FR-1206 Preview text MUST show `(value set, N chars)` on success where N is the character count of the set value.
- FR-1207 Operation returns no output value (side-effect only); preview text indicates success/failure status.
- FR-1208 Supported controls include: TextBox, editable ComboBox, Spinner, NumericUpDown, RichEdit, and other controls exposing ValuePattern.
- FR-1209 Unsupported controls: labels (static text), buttons, checkboxes (use Invoke/Toggle), non-editable controls, read-only fields.
- FR-1210 Error handling MUST return descriptive preview messages: `(no element)`, `(no value pattern)`, `(read-only)`, `(error: message)`.
- FR-1211 Debug logging MUST trace element resolution, value to set, ValuePattern checks, and SetValue execution for troubleshooting.
- FR-1212 Rationale: Direct value setting is faster and more reliable than clipboard-based (SetClipboard + SimulatePaste) approaches; works with any ValuePattern control; enables form filling automation.
- FR-1213 Use cases: Fill text fields, copy values between controls, clear fields, populate forms, transform and set data from variables.
- FR-1214 Best practices: Use SetFocus before SetValue for reliability; verify value format matches control expectations; add validation after SetValue using GetText.

## Update: New PACS Method - ClearReport (2025-02-09)
- FR-1220 Add new PACS method "Clear report" (`ClearReport`) to SpyWindow Custom Procedures dropdown for clearing report text fields in PACS.
- FR-1221 `PacsService` MUST expose `ClearReportAsync()` wrapper that executes the `ClearReport` custom procedure tag.
- FR-1222 Method MUST return `Task<bool>` and always return `true` after procedure execution (success/failure determined by PACS state validation).
- FR-1223 Custom procedure MUST be configured per-PACS profile in SpyWindow (no auto-seeded defaults provided).
- FR-1224 Rationale: Provides dedicated method for clearing report fields before starting new report; commonly needed in report correction workflows.
- FR-1225 User workflow: Configure procedure in SpyWindow using SetValue operations with empty strings for findings/conclusion fields; test using Run button before deployment.
- FR-1226 Common implementation: Use SetValue operations to clear findings field, conclusion field, and any other report text fields specific to PACS.

## Update: New Custom Procedure Operation - SimulateSelectAll (2025-02-09)
- FR-1230 Add new operation "SimulateSelectAll" to SpyWindow Custom Procedures for selecting all text in the currently focused control.
- FR-1231 Operation MUST send Ctrl+A keyboard shortcut using `System.Windows.Forms.SendKeys.SendWait("^a")`.
- FR-1232 Operation requires no arguments (Arg1, Arg2, Arg3 all disabled).
- FR-1233 Preview text MUST show "(Ctrl+A sent)" on success or "(error: message)" on failure.
- FR-1234 Operation returns no output value (side-effect only); preview text indicates success/failure status.
- FR-1235 Operation affects the currently focused control at the time of execution; use SetFocus beforehand to ensure correct target.
- FR-1236 Rationale: Provides quick text selection for copy, replace, or delete operations; commonly needed before paste or clear operations.
- FR-1237 Use cases: Select all text before paste, select all before copy, select all text in field before clearing with delete key, bulk text operations.
- FR-1238 Best practices: Use SetFocus before SimulateSelectAll to ensure correct control is targeted; combine with SimulatePaste for replace-all operation; use with SetClipboard for copy-all scenarios.
- FR-1239 Integration: Can be chained with other keyboard operations (SimulatePaste, SetClipboard) and clipboard operations for complete text manipulation workflows.

## Update: New Custom Procedure Operation - SimulateDelete (2025-02-09)
- FR-1240 Add new operation "SimulateDelete" to SpyWindow Custom Procedures for deleting selected text or character at cursor position.
- FR-1241 Operation MUST send Delete key using `System.Windows.Forms.SendKeys.SendWait("{DELETE}")`.
- FR-1242 Operation requires no arguments (Arg1, Arg2, Arg3 all disabled).
- FR-1243 Preview text MUST show "(Delete key sent)" on success or "(error: message)" on failure.
- FR-1244 Operation returns no output value (side-effect only); preview text indicates success/failure status.
- FR-1245 Operation affects the currently focused control at the time of execution; use SetFocus beforehand to ensure correct target.
- FR-1246 Behavior: If text is selected, deletes selected text; if no selection, deletes character at cursor position (forward delete).
- FR-1247 Rationale: Provides keyboard-based deletion for clearing fields, removing selected text, or character-by-character deletion; essential for text manipulation workflows.
- FR-1248 Use cases: Clear field after select all, delete selected text, remove character at cursor, simulate user deletion, legacy control support.
- FR-1249 Best practices: Use SimulateSelectAll + SimulateDelete for clearing entire field; use SetFocus before SimulateDelete to ensure correct control; prefer SetValue with empty string for simple field clearing when possible.
- FR-1250 Integration: Can be chained with SimulateSelectAll for field clearing, works with SetFocus for targeted deletion, combines with other keyboard operations for complex text manipulation.

## Bugfix: IsMatch and IsAlmostMatch Argument Type Forcing (2025-02-09)
- BUG-1260 Fix issue where IsMatch and IsAlmostMatch operations force both Arg1 and Arg2 to Var type, preventing users from using String literals for comparison.
- FIX-1261 Change IsMatch and IsAlmostMatch configuration to not force argument types; only enable arguments without resetting Type property.
- FIX-1262 Allow Arg1 and Arg2 to accept both String (literal comparison value) and Var (variable reference) types for flexibility.
- FIX-1263 Rationale: Users need ability to compare variables to literal strings (e.g., `IsMatch(var1, "expected")`) without argument type being forced to Var on "Set" or "Run" button clicks.
- FIX-1264 Impact: Existing procedures with IsMatch/IsAlmostMatch preserve user-selected argument types; new procedures allow flexible String or Var selection.
- FIX-1265 Common use case: Compare variable to literal string `IsMatch(var1, "true")` to check boolean results or validate expected values.

## Update: New Custom Procedure Operation - And (2025-02-09)
- FR-1270 Add new operation "And" to SpyWindow Custom Procedures for boolean logic operations with two Var arguments.
- FR-1271 Operation MUST accept Arg1=Var and Arg2=Var, both representing boolean values ("true" or "false").
- FR-1272 Operation returns "true" if BOTH arguments are "true" (case-insensitive), otherwise returns "false".
- FR-1273 Preview text MUST show result and both input values: `"true (true AND true)"` or `"false (true AND false)"`.
- FR-1274 Operation returns string value "true" or "false" for use in conditional logic and validation chains.
- FR-1275 Comparison is case-insensitive: "True", "TRUE", "true" all treated as true; any other value treated as false.
- FR-1276 Rationale: Enables combining multiple boolean conditions (e.g., `IsMatch` results) for complex validation logic without branching.
- FR-1277 Use cases: Validate multiple conditions simultaneously, combine visibility checks, verify multiple field matches, gate automation steps.
- FR-1278 Best practices: Chain with IsMatch/IsVisible/IsAlmostMatch operations; use for multi-condition validation; combine with conditional execution patterns.
- FR-1279 Common patterns: `IsMatch(var1, "expected") → var2`, `IsMatch(var3, "expected2") → var4`, `And(var2, var4) → var5` to check if both conditions pass.

## Enhancement: SendReport Module Retry Logic (2025-02-09)
- FR-1280 Update "SendReport" automation module to implement comprehensive retry flow with user interaction.
- FR-1281 Module execution flow: (1) Run SendReport custom procedure → (2) If result="true", run InvokeSendReport and succeed → (3) If result="false", prompt user with "Send failed. Retry?" messagebox.
- FR-1282 Retry flow: If user clicks OK → Run ClearReport custom procedure → If result="true", retry SendReport from step 1 → If result="false", show "Clear Report failed. Retry?" messagebox.
- FR-1283 Nested retry: If ClearReport fails and user clicks OK on retry prompt, retry ClearReport → If succeeds, continue to SendReport retry → If user clicks Cancel at any point, abort entire procedure.
- FR-1284 Success path: SendReport returns "true" → InvokeSendReport executes → Procedure completes with success message.
- FR-1285 Abort points: User cancels on "Send failed. Retry?" OR user cancels on "Clear Report failed. Retry?" → Procedure aborts with error status.
- FR-1286 Rationale: Provides robust error recovery for intermittent PACS send failures; allows user to clear stale UI state and retry without restarting entire workflow.
- FR-1287 Implementation: New `RunSendReportModuleWithRetryAsync()` method with nested retry loops; outer loop for SendReport retry, inner loop for ClearReport retry.
- FR-1288 User experience: Clear prompts for each failure scenario; OK/Cancel buttons for user control; status messages reflect current operation state.
- FR-1289 Debug logging: Comprehensive logging at each step for troubleshooting; logs procedure results, user choices, and exception details.
