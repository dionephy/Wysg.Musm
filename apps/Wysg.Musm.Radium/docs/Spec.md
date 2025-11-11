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

## Update: New PACS Methods and Automation Modules (2025-01-16)
- FR-1100 Add PACS method "Invoke open worklist" (`InvokeOpenWorklist`) to open PACS worklist window programmatically by invoking worklist open button.
- FR-1101 Add PACS method "Set focus search results list" (`SetFocusSearchResultsList`) to set keyboard focus on search results list element for user navigation.
- FR-1102 Add PACS method "Send report" (`SendReport`) to submit findings and conclusion to PACS; user must configure actual UI interaction steps per PACS.
- FR-1103 Add custom procedure operation "SetFocus" with single Element argument; calls `element.SetFocus()` on resolved UI automation element.
- FR-1104 Add automation module "OpenWorklist" that executes `InvokeOpenWorklist` customProcedure; can be added to any automation sequence.
- FR-1105 Add automation module "ResultsListSetFocus" that executes `SetFocusSearchResultsList` custom procedure; prepares list for keyboard navigation.
- FR-1106 Add automation module "SendReport" that executes `SendReport` custom procedure passing current `FindingsText` and `ConclusionText` from MainViewModel.
- FR-1107 Add three new KnownControl entries: `WorklistOpenButton`, `SearchResultsList`, `SendReportButton` for bookmark mapping in SpyWindow.
- FR-1108 Auto-seed default procedures for all three PACS methods: `InvokeOpenWorklist` uses `Invoke` on `WorklistOpenButton`, `SetFocusSearchResultsList` uses `SetFocus` on `SearchResultsList`, `SendReport` uses `Invoke` on `SendReportButton`.

## Update: Editor Phrase-Based Syntax Highlighting (2025-01-14)
- FR-700 Editor MUST provide real-time syntax highlighting based on phrase snapshots from the database.
- FR-701 Phrases present in the current phrase snapshot MUST be highlighted with color #4A4A4A (Dark.Color.BorderLight from DarkTheme.xaml).
- FR-702 Phrases NOT present in the phrase snapshot MUST be highlighted with red color to indicate they are not in the vocabulary.
- FR-703 The EditorControl MUST expose a `PhraseSnapshot` dependency property accepting an `IReadOnlyList<string>` for binding to the ViewModel.
- FR-704 Phrase highlighting MUST update in real-time when the phrase snapshot changes.
- FR-705 Phrase matching MUST be case-insensitive for better user experience.
- FR-706 Multi-word phrases MUST be detected and highlighted as a single unit (up to 5 words).
- FR-707 Highlighting MUST be implemented using a background renderer (KnownLayer.Background) so text remains readable.
- FR-708 Phrase highlighting MUST only affect visible text regions for performance (no full-document scan).
- FR-709 Future enhancement: phrase colors will be diversified using SNOMED CT concepts linked to each phrase.

## FR-900 through FR-915: Phrase-to-SNOMED Mapping (Central Database)

### Overview
Enable mapping of global and account-specific phrases to SNOMED CT concepts via Snowstorm API. Store mappings in central database for terminology management and semantic interoperability.

### FR-900: SNOMED Concept Cache Table
**Requirement**: Create `snomed.concept_cache` table in central database to cache SNOMED CT concepts retrieved from Snowstorm API.

**Schema**:
- `concept_id` (BIGINT, PK): SNOMED CT Concept ID (numeric, e.g., 22298006)
- `concept_id_str` (VARCHAR(18), UNIQUE): String representation for compatibility
- `fsn` (NVARCHAR(500), NOT NULL): Fully Specified Name
- `pt` (NVARCHAR(500), NULL): Preferred Term (may differ from FSN)
- `module_id` (VARCHAR(18), NULL): SNOMED CT Module ID
- `active` (BIT, NOT NULL, DEFAULT 1): Active status from Snowstorm
- `cached_at` (DATETIME2(3), NOT NULL, DEFAULT SYSUTCDATETIME()): Cache timestamp
- `expires_at` (DATETIME2(3), NULL): Optional cache expiration

**Indexes**:
- `IX_snomed_concept_cache_fsn` on `fsn`
- `IX_snomed_concept_cache_pt` on `pt`
- `IX_snomed_concept_cache_cached_at` on `cached_at DESC`

**Rationale**: Reduces Snowstorm API calls, improves performance, enables offline operation.

### FR-901: Global Phrase SNOMED Mapping Table
**Requirement**: Create `radium.global_phrase_snomed` table to map global phrases (account_id IS NULL) to SNOMED concepts.

**Schema**:
- `id` (BIGINT IDENTITY, PK)
- `phrase_id` (BIGINT, UNIQUE, FK → radium.phrase): Must reference global phrase (account_id IS NULL)
- `concept_id` (BIGINT, FK → snomed.concept_cache): SNOMED concept
- `mapping_type` (VARCHAR(20), NOT NULL, DEFAULT 'exact'): Values: 'exact', 'broader', 'narrower', 'related'
- `confidence` (DECIMAL(3,2), NULL): Optional confidence score (0.00-1.00)
- `notes` (NVARCHAR(500), NULL): Optional mapping notes
- `mapped_by` (BIGINT, NULL): Optional account_id of user who created mapping
- `created_at` (DATETIME2(3), NOT NULL, DEFAULT SYSUTCDATETIME())
- `updated_at` (DATETIME2(3), NOT NULL, DEFAULT SYSUTCDATETIME())

**Constraints**:
- FK to `radium.phrase` with CASCADE DELETE
- FK to `snomed.concept_cache` with RESTRICT DELETE
- CHECK constraint on `mapping_type` values
- CHECK constraint on `confidence` range (0.00-1.00)

**Indexes**:
- `IX_global_phrase_snomed_concept` on `concept_id`
- `IX_global_phrase_snomed_mapping_type` on `mapping_type`
- `IX_global_phrase_snomed_mapped_by` on `mapped_by`

**Rationale**: Global mappings are available to all accounts; one phrase maps to one concept.

### FR-902: Account Phrase SNOMED Mapping Table
**Requirement**: Create `radium.phrase_snomed` table to map account-specific phrases to SNOMED concepts.

**Schema**:
- `id` (BIGINT IDENTITY, PK)
- `phrase_id` (BIGINT, UNIQUE, FK → radium.phrase): Must reference account phrase (account_id IS NOT NULL)
- `concept_id` (BIGINT, FK → snomed.concept_cache): SNOMED concept
- `mapping_type` (VARCHAR(20), NOT NULL, DEFAULT 'exact'): Values: 'exact', 'broader', 'narrower', 'related'
- `confidence` (DECIMAL(3,2), NULL: Optional confidence score (0.00-1.00)
- `notes` (NVARCHAR(500), NULL): Optional mapping notes
- `created_at` (DATETIME2(3), NOT NULL, DEFAULT SYSUTCDATETIME())
- `updated_at` (DATETIME2(3), NOT NULL, DEFAULT SYSUTCDATETIME())

**Constraints**:
- UNIQUE constraint on `phrase_id` (one phrase → one concept)
- FK to `radium.phrase` with CASCADE DELETE
- FK to `snomed.concept_cache` with RESTRICT DELETE
- CHECK constraint on `mapping_type` values
- CHECK constraint on `confidence` range (0.00-1.00)

**Indexes**:
- `IX_phrase_snomed_concept` on `concept_id`
- `IX_phrase_snomed_mapping_type` on `mapping_type`
- `IX_phrase_snomed_created` on `created_at DESC`

**Rationale**: Account-specific mappings allow per-account customization; override global mappings.

### FR-903: Auto-Update Triggers
**Requirement**: Implement triggers `trg_global_phrase_snomed_touch` and `trg_phrase_snomed_touch` to automatically update `updated_at` when meaningful fields change.

**Trigger Behavior**:
- Fire AFTER UPDATE
- Detect changes in: `concept_id`, `mapping_type`, `confidence`, `notes`
- Set `updated_at = SYSUTCDATETIME()` for changed rows only

**Rationale**: Automatic timestamp maintenance for audit and sync tracking.

### FR-904: Combined Phrase-SNOMED View
**Requirement**: Create view `radium.v_phrase_snomed_combined` to unify global and account-specific mappings with phrase details.

**Columns**:
- `phrase_id`: Phrase ID
- `account_id`: Account ID (NULL for global)
- `phrase_text`: Phrase text
- `concept_id`: SNOMED concept ID (numeric)
- `concept_id_str`: SNOMED concept ID (string)
- `fsn`: Fully Specified Name
- `pt`: Preferred Term
- `mapping_type`: Mapping type
- `confidence`: Confidence score
- `notes`: Mapping notes
- `mapping_source`: 'global' or 'account'
- `created_at`: Mapping creation timestamp
- `updated_at`: Mapping update timestamp

**Join Logic**:
- UNION ALL of global and account mappings
- Include phrase details and concept cache details
- Use for lookups and reporting

**Rationale**: Simplifies queries for combined phrase-concept data.

### FR-905: Upsert SNOMED Concept Procedure
**Requirement**: Create stored procedure `snomed.upsert_concept` to insert or update concept cache from Snowstorm API responses.

**Parameters**:
- `@concept_id` (BIGINT)
- `@concept_id_str` (VARCHAR(18))
- `@fsn` (NVARCHAR(500))
- `@pt` (NVARCHAR(500), optional)
- `@module_id` (VARCHAR(18), optional)
- `@active` (BIT, default 1)
- `@expires_at` (DATETIME2(3), optional)

**Behavior**:
- MERGE on `concept_id`
- UPDATE existing: refresh all fields and `cached_at`
- INSERT new: create cache entry

**Rationale**: Standardized API → database sync; idempotent upsert pattern.

### FR-906: Map Global Phrase to SNOMED Procedure
**Requirement**: Create stored procedure `radium.map_global_phrase_to_snomed` to map a global phrase to a SNOMED concept.

**Parameters**:
- `@phrase_id` (BIGINT): Must be global phrase (account_id IS NULL)
- `@concept_id` (BIGINT): Must exist in concept_cache
- `@mapping_type` (VARCHAR(20), default 'exact')
- `@confidence` (DECIMAL(3,2), optional)
- `@notes` (NVARCHAR(500), optional)
- `@mapped_by` (BIGINT, optional): Account ID of mapper

**Validations**:
- Verify phrase is global (account_id IS NULL)
- Verify concept exists in cache
- RAISERROR if validations fail

**Behavior**:
- MERGE on `phrase_id`
- UPDATE existing mapping
- INSERT new mapping

**Rationale**: Enforce global phrase constraint; standardized mapping workflow.

### FR-907: Map Account Phrase to SNOMED Procedure
**Requirement**: Create stored procedure `radium.map_phrase_to_snomed` to map an account-specific phrase to a SNOMED concept.

**Parameters**:
- `@phrase_id` (BIGINT): Must be account phrase (account_id IS NOT NULL)
- `@concept_id` (BIGINT): Must exist in concept_cache
- `@mapping_type` (VARCHAR(20), default 'exact')
- `@confidence` (DECIMAL(3,2), optional)
- `@notes` (NVARCHAR(500), optional)

**Validations**:
- Verify phrase is account-specific (account_id IS NOT NULL)
- Verify concept exists in cache
- RAISERROR if validations fail

**Behavior**:
- MERGE on `phrase_id`
- UPDATE existing mapping
- INSERT new mapping

**Rationale**: Enforce account phrase constraint; standardized mapping workflow.

### FR-908: Snowstorm API Integration
**Requirement**: Implement C# service to query Snowstorm API for SNOMED concept search and details.

**Service Methods**:
- `SearchConceptsAsync(string query, int limit = 50)`: Search concepts by term
- `GetConceptDetailsAsync(string conceptId)`: Get concept details by ID
- `CacheConceptAsync(ConceptDto concept)`: Cache concept to database

**API Endpoints** (Snowstorm):
- Search: `GET /MAIN/concepts?term={query}&limit={limit}&activeFilter=true`
- Details: `GET /MAIN/concepts/{conceptId}`

**Response Mapping**:
- Extract `conceptId`, `fsn.term`, `pt.term`, `moduleId`, `active`
- Map to `ConceptCacheDto` and upsert to database

**Rationale**: Abstraction layer for Snowstorm API; automatic caching.

### FR-909: Global Phrases Tab - SNOMED Mapping UI
**Requirement**: Extend Settings → Global Phrases tab to support SNOMED mapping.

**UI Elements**:
1. **Phrase List** (existing): Show global phrases with mapped SNOMED indicator (icon or badge)
2. **SNOMED Search Panel**:
   - Search textbox: Enter SNOMED term
   - Search button: Query Snowstorm
   - Results grid: Display `conceptId`, `fsn`, `pt` with columns
   - Select action: Double-click or button to map selected concept
3. **Mapping Details Panel** (when phrase selected):
   - Show current mapping if exists: Concept ID, FSN, PT, Mapping Type
   - Edit controls: Mapping Type dropdown, Confidence slider (0-100%), Notes textbox
   - Save Mapping button: Persist changes
   - Remove Mapping button: Delete mapping

**Behavior**:
- Search results populate from Snowstorm API (cache concepts automatically)
- Mapping action creates/updates `global_phrase_snomed` row
- Refresh phrase list to show mapping indicator

**Rationale**: User-friendly interface for terminology management; supports semantic standardization.

### FR-910: Account Phrases Tab - SNOMED Mapping UI
**Requirement**: Extend Settings → Phrases tab (account-specific) to support SNOMED mapping.

**UI Elements**: Same as FR-909 but operates on account-specific phrases.

**Behavior**:
- Account mappings override global mappings for same phrase text
- UI shows if global mapping exists and allows override
- Search/select/save workflow identical to global tab

**Rationale**: Per-account customization while leveraging global standards.

### FR-911: Phrase Highlighting - SNOMED Color Coding
**Requirement**: Extend phrase highlighting renderer to use SNOMED semantic category colors.

**Color Scheme** (example):
- Clinical Finding (finding): Light blue (#A0C4FF)
- Procedure (procedure): Light green (#A0FFA0)
- Body Structure (body structure): Light yellow (#FFFFCC)
- Disorder (disorder): Light red (#FFB3B3)
- Observable Entity (observable entity): Light purple (#E0C4FF)
- Unmapped phrase: Red (#FF6666)

**Implementation**:
- Query `radium.v_phrase_snomed_combined` for mapped phrases and concept semantic tags
- Lookup SNOMED concept details via Snowstorm or cache for semantic tag
- Apply color based on semantic tag in `PhraseHighlightRenderer`

**Rationale**: Visual feedback for semantic categories; aids report review and quality.

### FR-912: Phrase Completion - SNOMED Metadata Display
**Requirement**: Show SNOMED concept details in phrase completion dropdown.

**Display Format**:
```
[phrase text]
SNOMED: [conceptId] | [fsn]
```

**Example**:
```
myocardial infarction
SNOMED: 22298006 | Myocardial infarction (disorder)
```

**Behavior**:
- Query phrase mappings when building completion list
- Include SNOMED metadata in completion item tooltip or secondary line
- Distinguish mapped vs unmapped phrases visually (e.g., icon)

**Rationale**: Contextual awareness of terminology during typing; promotes correct usage.

### FR-913: Phrase Report Export with SNOMED
**Requirement**: Include SNOMED concept IDs in exported phrase reports (CSV/JSON).

**Export Format** (CSV):
```
phrase_id,phrase_text,account_id,concept_id,fsn,pt,mapping_type
123,myocardial infarction,NULL,22298006,"Myocardial infarction (disorder)","Myocardial infarction",exact
```

**Export Format** (JSON):
```json
{
  "phrase_id": 123,
  "phrase_text": "myocardial infarction",
  "account_id": null,
  "snomed_mapping": {
    "concept_id": "22298006",
    "fsn": "Myocardial infarction (disorder)",
    "pt": "Myocardial infarction",
    "mapping_type": "exact",
    "confidence": 1.0
  }
}
```

**Rationale**: Enables downstream analytics, interoperability, and quality metrics.

### FR-914: Bulk Import SNOMED Mappings
**Requirement**: Support CSV import of phrase-SNOMED mappings for batch operations.

**Import CSV Format**:
```
phrase_text,concept_id,mapping_type,confidence,notes
myocardial infarction,22298006,exact,1.0,"Standard mapping"
```

**Import Workflow**:
1. Upload CSV file
2. Validate: Check phrase exists, concept in cache (fetch if missing), valid mapping_type
3. Preview: Show import summary with warnings
4. Execute: Batch insert/update mappings via stored procedures
5. Report: Show success/error counts

**Rationale**: Efficient bulk setup from external terminology resources or prior work.

### FR-915: Mapping Audit Log
**Requirement**: Track mapping changes for compliance and troubleshooting.

**Implementation Options**:
1. **Temporal tables** (SQL Server): Automatic history tracking
2. **Audit table** `radium.phrase_snomed_audit`:
   - Columns: `audit_id`, `phrase_id`, `concept_id`, `mapping_type`, `action` (INSERT/UPDATE/DELETE), `changed_by`, `changed_at`
   - Trigger: Capture changes on `global_phrase_snomed` and `phrase_snomed`

**Query Examples**:
- Who mapped phrase X?
- When did mapping for phrase Y change?
- What concepts were previously mapped to phrase Z?

**Rationale**: Compliance, accountability, and debugging support.

### FR-916: Phrase-SNOMED Mapping Window UX Improvements
**Requirement**: Enhance the PhraseSnomedLinkWindow user experience with automatic search initialization and proper command state management.

**FR-916a: Pre-fill Search Text**
- When opening the phrase-SNOMED mapping window, automatically populate the search textbox with the phrase text being mapped.
- User can immediately press Enter or click Search without retyping the phrase.
- Improves workflow efficiency for mapping workflows.

**FR-916b: Enable Map Button**
- Map button must enable when a SNOMED concept is selected from the search results grid.
- Map button must disable when no concept is selected.
- Implemented by notifying `MapCommand.CanExecuteChanged` when `SelectedConcept` property changes.

**Rationale**: Reduces friction in the mapping workflow; users can immediately search and map without redundant typing and button state is clear and responsive.

## FR-920 through FR-925: UI Bookmark Robustness Improvements (2025-01-15)

### FR-920: Stricter Root Validation
**Requirement**: When resolving bookmarks, the root window matching MUST enforce ALL enabled attributes (Name, ClassName, AutomationId, ControlTypeId) instead of relaxed matching.

**Previous Behavior** (Issue):
- Step 0 accepted root by ANY attribute match (Name OR AutomationId OR ControlType) while ignoring ClassName
- Could match wrong window if multiple windows existed with similar properties
- Led to bookmark failures when PACS had multiple windows open

**New Behavior** (Fix):
- Step 0 requires ALL enabled attributes to match exactly (Name AND ClassName AND AutomationId AND ControlType)
- Only accepts root if every enabled attribute matches the captured values
- Falls through to normal search if root doesn't match perfectly
- Trace output shows which attributes matched/mismatched for debugging

**Rationale**: Prevents misidentification of root window; improves bookmark reliability across different PACS states.

### FR-921: Improved Root Discovery Filtering
**Requirement**: Root discovery MUST filter and prioritize candidate roots using the first bookmark node's attributes instead of rescanning the entire desktop.

**Previous Behavior** (Issue):
- Discovered all windows via multiple fallback strategies
- Rescanned desktop to find windows matching first node
- Non-deterministic ordering led to different roots on different runs
- Performance overhead from repeated desktop scans

**New Behavior** (Fix):
- Filters existing root candidates using first node attributes
- Applies exact match filtering first, then relaxed match (without ControlTypeId) if needed
- Additional ClassName filtering when multiple matches remain
- Sorts remaining roots by similarity score (AutomationId=200pts, Name=100pts, ClassName=50pts, ControlType=25pts)
- Deterministic selection based on similarity scores

**Rationale**: Ensures consistent root selection across runs; reduces unnecessary API calls; prioritizes most distinctive attributes.

### FR-922: Bookmark Validation Before Save
**Requirement**: SpyWindow MUST validate bookmarks before saving to catch common robustness issues.

**Validation Rules**:
1. Process name must not be empty
2. Chain must not be empty
3. First node must have at least 1 enabled identifying attribute (Name, ClassName, AutomationId, ControlTypeId)
4. Nodes relying solely on UseIndex=true with IndexAmongMatches=0 generate warnings (recommend adding more attributes)

**Error Handling**:
- Validation failures prevent save and display specific error messages
- Warnings allow save but inform user of potential issues
- Validation messages are actionable (explain what to fix)

**Rationale**: Catches under-specified bookmarks at save time instead of discovering failures at runtime; guides users toward more robust bookmark configurations.

### FR-923: Enhanced Trace Diagnostics
**Requirement**: Bookmark resolution trace output MUST include detailed matching information for step 0 root acceptance.

**Trace Output Enhancements**:
- Step 0: Shows match results for each attribute (Name=true, Class=true, Auto=false, Ct=true)
- Root mismatch: Explains which attributes failed to match and continues to normal search
- Root discovery: Lists filtering stages (exact match, relaxed match, ClassName filter, similarity sort)
- Root count after each filtering stage for transparency
- **Per-step timing**: Each step shows elapsed time in milliseconds (e.g., "Step 2: Completed (45 ms)")
- **Validate button timing**: SpyWindow "Validate" button displays per-step timing in the diagnostic table output
- **Full trace display**: "Validate" button always shows last 100 lines of trace output (including on success) for comprehensive timing analysis
- **Retry timing breakdown**: Each step shows detailed breakdown:
  - Individual attempt timing (e.g., "Attempt 1/3 - Query took 50ms")
  - Total query time across all attempts
  - Total retry delay time (accumulated sleep between attempts)
  - Number of attempts made
  - Success indication (e.g., "Success on attempt 2")
- **Smart exception handling**: Detects "Specified method is not supported" errors and immediately skips to manual walker (prevents wasted retry delays)
- **Optimized retry count**: Reduced from 2 retries (3 total attempts) to 1 retry (2 total attempts) to minimize delay overhead

**Performance Impact**: These optimizations typically reduce bookmark resolution time by 4-6x for UWP apps where UIA Find operations fail with "not supported" errors. Example: Calculator app improved from 2934ms to ~500-800ms.

**Rationale**: Enables developers and power users to diagnose bookmark failures quickly; reduces trial-and-error debugging; detailed retry timing helps identify whether slowness is due to UIA queries or retry overhead; smart exception detection prevents wasted retry delays on permanent errors; allows data-driven optimization decisions.

### FR-924: ClassName Match Enforcement
**Requirement**: When first node specifies ClassName, root discovery MUST prefer roots matching that ClassName even if other attributes match.

**Previous Behavior** (Issue):
- ClassName was captured but often ignored during root matching
- Could match wrong window type (e.g., toolbar instead of main window)
- Special "relaxed match" logic at step 0 skipped ClassName entirely

**New Behavior** (Fix):
- ClassName filter applied to root candidates when multiple matches remain
- Step 0 requires ClassName match if UseClassName=true
- Only relaxes ClassName if no other matches found (explicit fallback)
- Trace shows when ClassName filter is applied

**Rationale**: ClassName is often the most reliable distinguisher for window type; enforcing it prevents matching auxiliary windows.

### FR-925: Similarity-Based Root Prioritization
**Requirement**: When multiple root candidates match first node attributes, sort by similarity score to ensure deterministic selection.

**Similarity Scoring**:
- AutomationId match: +200 points (most unique)
- Name match: +100 points
- ClassName match: +50 points
- ControlTypeId match: +25 points
- Highest score selected first

**Behavior**:
- Applied after all filtering stages
- Breaks ties deterministically (prefer highest cumulative match)
- Logged in trace output
- Ensures same root selected across runs when multiple windows exist

**Rationale**: Eliminates non-deterministic bookmark resolution; prioritizes most distinctive attributes; improves reliability when PACS has multiple windows.

## FR-950 through FR-956: Status Log, UI Bookmarks, and PACS Methods (2025-01-15)

### FR-950: Status Textbox Auto-Scroll and Line-by-Line Colorization
**Requirement**: Status log in MainWindow must auto-scroll to show the most recent entries and support line-by-line color coding for better visibility.

**FR-950a: Auto-Scroll Behavior**
- Status textbox must automatically scroll to the end whenever new log entries are added
- Users should always see the most recent log message without manual scrolling
- Implemented by using RichTextBox instead of plain TextBox and calling `ScrollToEnd()` after content updates

**FR-950b: Line-by-Line Colorization**
- Error lines (containing "error", "failed", "exception", "validation failed") must appear in red (#FF5A5A)
- Non-error lines must appear in default color (#D0D0D0)
- Colorization applied per line during status text updates
- Preserves readability with appropriate contrast ratios

**Rationale**: Improves user awareness of automation status; errors stand out immediately; recent activity always visible without scrolling.

### FR-951: New UI Bookmarks for Screen Areas
**Requirement**: Add new KnownControl bookmarks for specific screen tab areas in PACS.

**Screen_MainCurrentStudyTab**:
- Maps to the current study tab area in the main screen
- Used by `SetCurrentStudyInMainScreen` automation action
- Allows automated switching to current study view

**Screen_SubPreviousStudyTab**:
- Maps to the previous study tab area in the sub/auxiliary screen
- Used by `SetPreviousStudyInSubScreen` automation action
- Allows automated switching to previous study view for comparison

**Rationale**: Enables multi-monitor or split-screen PACS workflows where studies are displayed in different areas; supports automated view switching for comparison tasks.

### FR-952: PACS Method – Set Current Study in Main Screen
**Requirement**: Add new PACS custom procedure method `SetCurrentStudyInMainScreen` for automated screen switching.

**Method Behavior**:
- Executes custom procedure tag `SetCurrentStudyInMainScreen`
- Default auto-seed: Single `ClickElement` operation targeting `Screen_MainCurrentStudyTab` bookmark
- PacsService exposes `SetCurrentStudyInMainScreenAsync()` wrapper

**Use Cases**:
- Automation sequences that need to ensure main screen shows current study before performing actions
- Multi-screen PACS workflows where studies open in different monitors
- Quality control workflows requiring specific screen configurations

**Rationale**: Standardizes screen state management across automation sequences; reduces manual coordination in multi-screen setups.

### FR-953: PACS Method – Set Previous Study in Sub Screen
**Requirement**: Add new PACS custom procedure method `SetPreviousStudyInSubScreen` for automated screen switching.

**Method Behavior**:
- Executes custom procedure tag `SetPreviousStudyInSubScreen`
- Default auto-seed: Single `ClickElement` operation targeting `Screen_SubPreviousStudyTab` bookmark
- PacsService exposes `SetPreviousStudyInSubScreenAsync()` wrapper

**Use Cases**:
- Comparison workflows requiring previous study visible in auxiliary screen
- Side-by-side review automation
- Teaching/demonstration scenarios with controlled screen layouts

**Rationale**: Complements FR-952 for complete dual-screen automation; enables sophisticated comparison workflows.

### FR-954: Custom Procedure Operation – ClickElement
**Requirement**: Add new operation `ClickElement` to Custom Procedures for clicking at the center of a resolved UI element.

**Operation Signature**:
- Arg1: Element (Type=Element, maps to UiBookmarks.KnownControl)
- Arg2: Disabled
- Arg3: Disabled

**Behavior**:
1. Resolve UI element via bookmark system
2. Get element's bounding rectangle
3. Calculate center point: `(Left + Width/2, Top + Height/2)`
4. Perform left mouse click at calculated screen coordinates
5. Preview text: `(clicked element center X,Y)` on success

**Error Handling**:
- No element: `(no element)`
- No bounds: `(no bounds)`
- Click error: `(error: {message})`

**Rationale**: Bridges gap between bookmark resolution and mouse actions; more reliable than hardcoded coordinates for dynamic UI elements; natural complement to existing `MouseClick` operation.

### FR-955: SpyWindow Custom Procedures – New PACS Methods UI
**Requirement**: SpyWindow Custom Procedures combo must list the new PACS methods for user configuration.

**Methods to Add**:
- "Set current study in main screen" (tag: `SetCurrentStudyInMainScreen`)
- "Set previous study in sub screen" (tag: `SetPreviousStudyInSubScreen`)

**Editor Behavior**:
- Selecting these methods loads existing procedure or auto-seeds default (ClickElement operation)
- Users can edit operation sequence and test via SpyWindow Run button
- Saved procedures persist per-PACS profile

**Rationale**: Discoverability of new automation capabilities; consistent with existing SpyWindow UI patterns.

### FR-956: SpyWindow Operations Dropdown – ClickElement
**Requirement**: SpyWindow Custom Procedures operation dropdown must include `ClickElement` with appropriate editor presets.

**Editor Presets**:
- Operation name: `ClickElement`
- Arg1: Type=Element, enabled
- Arg2: Disabled
- Arg3: Disabled
- Preview: Shows clicked coordinates after execution

**Usage Flow**:
1. Select `ClickElement` operation from dropdown
2. Arg1 automatically preset to Element type
3. User selects KnownControl from Element dropdown (e.g., `Screen_MainCurrentStudyTab`)
4. Click Run to test operation
5. Preview shows `(clicked element center X,Y)`

**Rationale**: Consistent operation UI pattern; guides users to correct argument configuration; testable in SpyWindow before deployment.

### FR-957: PACS Method – Worklist Is Visible (2025-01-16)
**Requirement**: Add new PACS custom procedure method `WorklistIsVisible` for checking if worklist window is visible.

**Method Behavior**:
- Executes custom procedure tag `WorklistIsVisible`
- Default auto-seed: Single `IsVisible` operation targeting `WorklistWindow` bookmark
- PacsService exposes `WorklistIsVisibleAsync()` wrapper
- Returns "true" if worklist window is visible and reachable, "false" otherwise

**Use Cases**:
- Automation sequences that need to check worklist state before performing actions
- Conditional logic workflows where actions depend on worklist visibility
- Quality control workflows requiring specific UI states

**Rationale**: Enables conditional automation based on UI visibility state; complements existing PACS control methods.

### FR-1160: Custom Procedure Operation – MouseMoveToElement (2025-01-18)
**Requirement**: Add new operation `MouseMoveToElement` to Custom Procedures for moving the mouse cursor to the center of a UI element without clicking.

**Operation Signature**:
- Arg1: Element (Type=Element, maps to UiBookmarks.KnownControl)
- Arg2: Disabled
- Arg3: Disabled

**Behavior**:
1. Resolve UI element via bookmark system
2. Get element's bounding rectangle
3. Calculate center point: `(Left + Width/2, Top + Height/2)`
4. Move mouse cursor to calculated screen coordinates (no click)
5. Preview text: `(moved to element center X,Y)` on success

**Error Handling**:
- No element: `(no element)`
- No bounds: `(no bounds)`
- Move error: `(error: {message})`

**Use Cases**:
- Hover interactions that require mouse presence without clicking
- Preparation for manual mouse actions by user
- UI element highlighting for user guidance
- Testing element accessibility by moving cursor to it

**Rationale**: Provides non-destructive cursor positioning for scenarios where clicking is undesirable; complements ClickElement operation; useful for hover-triggered UI elements and user guidance workflows.

### FR-1170: Custom Procedure Operation – SetClipboard (2025-01-18)
**Requirement**: Add new operation `SetClipboard` to Custom Procedures for setting the Windows clipboard with text content.

**Operation Signature**:
- Arg1: Text (Type=String, can be literal or variable reference)
- Arg2: Disabled
- Arg3: Disabled

**Behavior**:
1. Resolve text value from Arg1 (supports variables like `var1`, `var2`)
2. Set Windows clipboard content using `System.Windows.Clipboard.SetText()`
3. Preview text: `(clipboard set, N chars)` where N is the character count
4. Return value: null (operation is side-effect only)

**Error Handling**:
- Null text: `(null)`
- Clipboard error: `(error: {message})`

**Use Cases**:
- Copy data from PACS UI elements to clipboard for pasting into external applications
- Prepare text content for subsequent `SimulatePaste` operations
- Extract and copy patient data, study information, or report text
- Clipboard-based data transfer workflows

**Rationale**: Enables clipboard-based data transfer from PACS to other applications; common pattern in legacy systems; complements SimulatePaste operation.

### FR-1171: Custom Procedure Operation – SimulateTab (2025-01-18)
**Requirement**: Add new operation `SimulateTab` to Custom Procedures for simulating keyboard Tab key press.

**Operation Signature**:
- Arg1: Disabled
- Arg2: Disabled
- Arg3: Disabled

**Behavior**:
1. Send Tab key press using `System.Windows.Forms.SendKeys.SendWait("{TAB}")`
2. Wait for key processing to complete before continuing
3. Preview text: `(Tab key sent)`
4. Return value: null (operation is side-effect only)

**Error Handling**:
- Send error: `(error: {message})`

**Use Cases**:
- Navigate between form fields in PACS dialogs
- Trigger field validation or auto-complete behaviors
- Move focus to next control in tab order
- Keyboard-driven navigation workflows
- Legacy PACS systems that rely on Tab navigation

**Rationale**: Provides keyboard navigation capability essential for form-based PACS workflows; simpler than element-based focus operations when tab order is predictable; based on legacy PacsService patterns.

### FR-1172: Custom Procedure Operation – SimulatePaste (2025-01-18)
**Requirement**: Add new operation `SimulatePaste` to Custom Procedures for simulating keyboard Ctrl+V paste action.

**Operation Signature**:
- Arg1: Disabled
- Arg2: Disabled
- Arg3: Disabled

**Behavior**:
1. Send Ctrl+V using `System.Windows.Forms.SendKeys.SendWait("^v")`
2. Wait for paste processing to complete before continuing
3. Preview text: `(Ctrl+V sent)`
4. Return value: null (operation is side-effect only)

**Error Handling**:
- Send error: `(error: {message})`

**Use Cases**:
- Paste clipboard content into PACS text fields
- Combine with `SetClipboard` for automated text entry
- Paste into fields that don't support UIA ValuePattern
- Keyboard-driven data entry workflows
- Legacy PACS systems that require keyboard input

**Rationale**: Complements SetClipboard operation for complete clipboard-based data entry; keyboard paste is more reliable than UIA for some legacy controls; based on legacy PacsService patterns.

### FR-1173: Custom Procedure Operation – GetSelectedElement (2025-01-18)
**Requirement**: Add new operation `GetSelectedElement` to Custom Procedures for retrieving the selected element (item/row) from any list or container element.

**Operation Signature**:
- Arg1: Element (parent list/container element)
- Arg2: Disabled
- Arg3: Disabled

**Behavior**:
1. Resolve parent element from Arg1 (any Element bookmark)
2. Get selected item using Selection pattern or SelectionItem pattern scan
3. **Cache element in runtime memory** for use by subsequent operations (FR-1175)
4. Return element reference with name and automation ID
5. Preview text: `(element: {name}, automationId: {autoId})`
6. Return value: `SelectedElement:{name}` (element identifier string)

**Error Handling**:
- Element not resolved: `(element not resolved)`
- No selection: `(no selection)`
- Resolution error: `(error: {message})`

**Use Cases**:
- Get reference to selected row from SearchResultsList
- Get reference to selected study from related studies
- Get reference to selected item from any list control
- Extract element metadata (name, automation ID) for logging
- Validate selection state before performing actions
- **Chain with ClickElement, MouseMoveToElement, etc. using var output** (FR-1174, FR-1175)

**Implementation Notes**:
- Takes any Element as argument (generalized, not hardcoded to specific list)
- Works with any list/container that supports Selection or SelectionItem patterns
- Returns element reference as string identifier (name-based)
- Supports both Selection pattern and SelectionItem pattern fallback
- Can be used with SearchResultsList, RelatedStudiesList, or any custom list bookmark
- **Stores actual AutomationElement in runtime cache** for operation chaining (FR-1175)
- **Element cache cleared at start of each procedure run** to prevent stale references

**Rationale**: Provides generalized element reference capability for any selected item; more flexible than hardcoded list-specific operations; enables reuse across different list controls; complements field-specific getters (GetValueFromSelection); follows same pattern as existing element operations but returns the element itself rather than field values.

**Example Usage**:
```
# Get selected study from search results
GetSelectedElement(SearchResultsList) → var1

# Get selected study from related studies
GetSelectedElement(RelatedStudiesList) → var2

# Get field value from selected element (combine operations)
GetValueFromSelection(SearchResultsList, "Patient Name") → var3

# NEW: Chain operations - click the selected element (FR-1174)
GetSelectedElement(SearchResultsList) → var1
ClickElement(var1) → var2
```

### FR-1174: ClickElement Operation – Accept Var Type for Element Chaining (2025-01-18)
**Requirement**: Enhance `ClickElement` operation to accept both `Element` (bookmark) and `Var` (from `GetSelectedElement` output) argument types, enabling operation chaining.

**Operation Signature (Enhanced)**:
- Arg1: Element **OR Var** (bookmark or variable containing element reference)
- Arg2: Disabled
- Arg3: Disabled

**Behavior**:
1. When Arg1 Type = Element: Resolve from bookmark (existing behavior)
2. **When Arg1 Type = Var: Retrieve cached element from runtime cache** (new)
3. Validate element is still alive (staleness check)
4. Calculate element center coordinates
5. Click element center

**Error Handling**:
- Cached element not found: `(no element)`
- Cached element stale (UI changed): `(no element)` (element removed from cache)
- Element has no bounds: `(no bounds)`
- Click failed: `(error: {message})`

**Use Cases**:
- Click selected item from search results without creating explicit bookmark
- Dynamic clicking based on runtime selection
- Multi-step workflows where selection changes between steps
- Testing scenarios where element position changes

**Implementation Notes**:
- `ResolveElement()` method enhanced to check argument type
- If Arg1 Type = Var: look up value in runtime element cache
- Element staleness validation using Name property access
- Stale elements automatically removed from cache
- Same click logic applies (calculate center, use NativeMouseHelper)

**Rationale**: Enables powerful operation chaining without requiring bookmarks for every intermediate element; mirrors pattern from field extraction operations; reduces manual bookmark management; makes procedures more dynamic and adaptive.

**Example Usage**:
```
# Traditional approach (still supported)
ClickElement(SearchResultsList) → clicks the list itself

# NEW: Chaining approach (dynamic element clicking)
GetSelectedElement(SearchResultsList) → var1  # Get currently selected row
ClickElement(var1) → var2                      # Click that specific row
```

### FR-1175: Runtime Element Cache for Operation Chaining (2025-01-18)
**Requirement**: Implement runtime element cache to store `AutomationElement` objects from `GetSelectedElement` for use by subsequent operations (`ClickElement`, `MouseMoveToElement`, `IsVisible`, `SetFocus`).

**Cache Design**:
- **Scope**: Procedure execution lifetime (cleared at start of each run)
- **Key**: String identifier (e.g., `SelectedElement:{name}`)
- **Value**: `FlaUI.Core.AutomationElements.AutomationElement` object
- **Storage**: Dictionary in SpyWindow and ProcedureExecutor classes
- **Lifecycle**: Created when procedure starts, cleared before next run

**Cache Operations**:
1. **Store**: `GetSelectedElement` saves element with key = output variable value
2. **Retrieve**: `ResolveElement(Var)` looks up element by variable value
3. **Validate**: Check if element still alive (access Name property)
4. **Evict**: Remove stale elements on validation failure
5. **Clear**: Wipe entire cache at start of procedure run

**Staleness Detection**:
- Before using cached element: try accessing `element.Name` property
- If exception thrown: element is stale (UI changed), remove from cache
- Return null to indicate element unavailable
- Operation reports `(no element)` error

**Supported Operations** (via `ResolveElement` enhancement):
- ClickElement (FR-1174)
- MouseMoveToElement
- IsVisible
- SetFocus
- GetText
- GetName
- GetTextOCR
- Invoke
- GetValueFromSelection

**Implementation Locations**:
- `SpyWindow.Procedures.Exec.cs`: Interactive execution cache (`_elementCache`)
- `ProcedureExecutor.cs`: Headless execution cache (`_elementCache`)
- Both caches operate independently (different execution contexts)

**Limitations**:
- Elements only valid within single procedure execution
- Element references cannot persist across procedure runs
- Element identifier based on Name property (may not be unique)
- Cache does not handle multiple elements with same name

**Future Enhancements** (Not Implemented):
- Persistent element references across runs
- Unique identifier generation (GUID-based)
- Cache expiration based on time
- Multi-element selection support

**Rationale**: Enables operation chaining with actual element objects rather than string identifiers; provides foundation for dynamic, selection-based automation workflows; balances simplicity (clear cache each run) with power (full AutomationElement access); mirrors pattern from bookmark resolution but with runtime scope.

**Example Internal Flow**:
```csharp
// Step 1: GetSelectedElement stores element
GetSelectedElement(SearchResultsList)
  → Resolves list bookmark
  → Finds selected row (AutomationElement)
  → Stores in cache: _elementCache["SelectedElement:MRI Brain"] = element
  → Returns "SelectedElement:MRI Brain"

// Step 2: ClickElement retrieves from cache
ClickElement(var1)  // var1 = "SelectedElement:MRI Brain"
  → ResolveElement checks Arg1.Type = Var
  → Looks up _elementCache["SelectedElement:MRI Brain"]
  → Validates element.Name (staleness check)
  → Returns cached AutomationElement
  → Clicks element center
```

## FR-970: PACS Method – ReportText Is Visible (2025-01-17)

## Update: GetStudyRemark Module Enhancement – Fill Chief Complaint (2025-02-09)
- FR-1180 `GetStudyRemark` automation module MUST fill BOTH `StudyRemark` and `ChiefComplaint` properties with the same text captured from PACS.
- FR-1181 When study remark is successfully acquired:
  - Set `StudyRemark` property (existing behavior)
  - Set `ChiefComplaint` property with the same text (new behavior)
  - Display status: "Study remark captured (N chars)"
  - Log both property assignments for diagnostics
- FR-1182 When study remark is empty or capture fails:
  - Set `StudyRemark = string.Empty`
  - Set `ChiefComplaint = string.Empty`
  - Maintain consistent clearing behavior for both fields
- FR-1183 Implementation MUST preserve existing retry logic (3 attempts with 200ms delays)
- FR-1184 User can manually edit `ChiefComplaint` after automation if study remark text is not appropriate
- FR-1185 Rationale: In many radiology workflows, study remark directly represents chief complaint; auto-filling eliminates manual copy-paste step
