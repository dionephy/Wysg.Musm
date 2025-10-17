# Feature Specification: Radium Cumulative – Reporting Workflow, Editor Experience, PACS & Studyname→LOINC Mapping

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

### FR-970: PACS Method – ReportText Is Visible (2025-01-17)
**Requirement**: Add new PACS custom procedure method `ReportTextIsVisible` for checking if the report text editor is visible in the current PACS view.

**Method Behavior**:
- Executes custom procedure tag `ReportTextIsVisible`
- Default auto-seed: Single `IsVisible` operation targeting `ReportText` bookmark
- PacsService exposes `ReportTextIsVisibleAsync()` wrapper
- Returns "true" if report text editor is visible and reachable, "false" otherwise

**Use Cases**:
- AddPreviousStudy automation module to determine which getter methods to use
- Conditional report extraction based on PACS UI state
- Quality control workflows requiring specific report editor states

**Rationale**: Different PACS UI states require different methods to extract report content; visibility check enables automatic adaptation to current UI layout.

### FR-971: AddPreviousStudy – Conditional Getter Selection (2025-01-17)
**Requirement**: Modify AddPreviousStudy automation module to check ReportText visibility and conditionally select appropriate getter methods for findings and conclusion.

**Logic Flow**:
1. Before extracting report content, call `ReportTextIsVisibleAsync()` to check editor visibility
2. If `ReportTextIsVisible` returns "true":
   - Use `GetCurrentFindings` and `GetCurrentConclusion` methods (primary getters)
   - These methods target the visible report text editor elements
   - Set status: "ReportText visible - using primary getters"
3. If `ReportTextIsVisible` returns "false":
   - Use `GetCurrentFindings2` and `GetCurrentConclusion2` methods (alternate getters)
   - These methods target alternate UI elements when report text is not visible
   - Set status: "ReportText not visible - using alternate getters"
4. Continue with existing logic to persist and load previous study

**Behavior Changes**:
- Previous behavior: Always called all four getters and picked the longer result
- New behavior: Conditionally calls only the appropriate getters based on visibility
- Fallback: If visibility check fails or returns unexpected value, uses alternate getters (safe default)

**Rationale**: Improves reliability by selecting the correct extraction method based on actual PACS UI state; reduces redundant API calls; provides clearer diagnostic status messages.

### FR-974: AddPreviousStudy – Fallback to Alternate Getters When Primary Returns Blank (2025-01-17)
**Requirement**: Enhance AddPreviousStudy conditional getter logic to support fallback to alternate getters when primary getters return blank results.

**Previous Behavior (FR-971)**:
- If ReportText visible: use primary getters only
- If ReportText not visible: use alternate getters only
- No fallback mechanism when primary getters succeeded but returned blank content

**New Behavior (Enhanced)**:
1. Check ReportText visibility using `ReportTextIsVisibleAsync()`
2. If ReportText is visible:
   - First, try primary getters (`GetCurrentFindings`, `GetCurrentConclusion`)
   - If BOTH findings AND conclusion are blank → try alternate getters as fallback
   - Use longer result from primary/alternate for each field (findings, conclusion)
   - Status: "ReportText visible - using primary + alternate getters (fallback)" when fallback triggered
   - Status: "ReportText visible - using primary getters" when primary returns content
3. If ReportText is not visible:
   - Use alternate getters only (unchanged from FR-971)
   - Status: "ReportText not visible - using alternate getters"

**Rationale**:
- Handles edge case where ReportText is visible but empty (e.g., new blank report)
- Provides automatic fallback without requiring user intervention
- Maximizes chance of capturing report content regardless of PACS UI state
- Maintains clear status messages for troubleshooting

**Example Scenarios**:
1. **Primary Success**: ReportText visible with content → primary getters return content → use primary results
2. **Primary Blank + Alternate Success**: ReportText visible but blank → primary returns "" → alternate getters tried → use alternate results
3. **Both Blank**: ReportText visible but all getters return blank → both attempted → blank results accepted
4. **ReportText Hidden**: ReportText not visible → only alternate getters tried → use alternate results

**Implementation Details**:
- Fallback logic checks `string.IsNullOrWhiteSpace()` for both findings and conclusion
- PickLonger helper selects result with most characters (fallback to first if equal)
- Sequential async execution: primary first, then alternate only if needed
- Status messages clearly indicate which path was taken for diagnostics

### FR-975: GetPatientRemark – Remove Duplicate Lines (2025-01-17)
**Requirement**: After retrieving the patient remark string via `GetCurrentPatientRemarkAsync()`, the system MUST automatically remove duplicate lines before storing in the `PatientRemark` property.

**Duplicate Definition**:
- Lines are considered duplicate if the text wrapped within angle brackets (`<` and `>`) is the same (case-insensitive).
- Example:
  - Line 1: `Patient has <diabetes> since 2020`
  - Line 2: `History of <diabetes> with complications`
  - These lines are duplicates because both contain `<diabetes>`

**Behavior**:
1. After retrieving patient remark string from PACS, split into lines
2. For each line, extract the text between `<` and `>` (if present)
3. Track seen angle-bracket content in a case-insensitive set
4. Keep only the first occurrence of each unique angle-bracket content
5. Lines without angle-bracket content are always kept (no deduplication)
6. Rejoin lines with newline separator

**Implementation**:
- Method: `RemoveDuplicateLinesInPatientRemark(string input)` in `MainViewModel.Commands.cs`
- Helper: `ExtractAngleBracketContent(string line)` extracts text between `<` and `>`
- Uses `HashSet<string>` with `StringComparer.OrdinalIgnoreCase` for duplicate detection
- Called automatically in `AcquirePatientRemarkAsync()` before setting `PatientRemark` property

**Edge Cases**:
- Empty or null input: returns unchanged
- Lines with no angle brackets: always kept (not considered for deduplication)
- Lines with only opening `<` or only closing `>`: not considered for deduplication
- Lines with `<>` (empty content): not considered for deduplication
- Multiple angle bracket pairs per line: only first pair is extracted
- Malformed brackets (`>` before `<`): not considered for deduplication

**Example**:
```
Input:
<DM> diagnosed in 2020
<HTN> under control
<DM> with complications
No angle brackets here
<CKD> stage 3

Output:
<DM> diagnosed in 2020
<HTN> under control
No angle brackets here
<CKD> stage 3
```

**Rationale**:
- Patient remarks often contain redundant entries with similar structured tags
- Duplicate lines clutter the UI and report editor
- Removing duplicates based on tag content reduces noise while preserving unique information
- Case-insensitive matching handles inconsistent capitalization in PACS data

## Update: Enhanced Manage Studyname Techniques Window with Combination Building (2025-01-17)
- FR-1020 StudynameTechniqueWindow MUST provide a split-panel layout with left panel for building new combinations and right panel for managing existing combinations.
- FR-1021 The left panel MUST include ComboBoxes for selecting Prefix, Tech, and Suffix components with "+" buttons to add new components inline.
- FR-1022 The left panel MUST display a "Current Combination" list showing techniques added to the working combination before saving.
- FR-1023 Users MUST be able to click "Add to Combination" to add the selected prefix+tech+suffix to the current combination list.
- FR-1024 The window MUST prevent duplicate techniques (same prefix, tech, suffix triple) from being added to the current combination.
- FR-1025 Users MUST be able to save the current combination as a new non-default combination using a "Save as New Combination" button.
- FR-1026 The saved combination MUST automatically appear in the existing combinations list on the right panel without requiring window close/reopen.
- FR-1027 The right panel MUST continue to display existing combinations with their display text and default status indicator.
- FR-1028 The ComboBox items MUST display properly by overriding ToString() in TechText, CombinationItem, and ComboRow classes.
- FR-1029 The window layout MUST use a GridSplitter between left and right panels for user-adjustable sizing.
- FR-1030 All UI components MUST follow the dark theme styling consistent with other Radium windows.
- FR-1031 The "+" buttons MUST open simple modal dialogs prompting for text input to add new prefix/tech/suffix components.
- FR-1032 After adding a new component via "+" button, the component MUST be automatically selected in the corresponding ComboBox.

## Update: Set Default Technique Combination in Manage Studyname Techniques Window (2025-01-17)
- FR-1000 StudynameTechniqueWindow MUST display a list of existing technique combinations for the selected studyname with their default status.
- FR-1001 The combinations list MUST show combination display text and an indicator (e.g., checkmark) for the currently marked default combination.
- FR-1002 Users MUST be able to select any combination from the list and click a "Set Selected As Default" button to change the default for the studyname.
- FR-1003 After setting a new default, the combinations list MUST refresh to show the updated default status without requiring the user to close and reopen the window.
- FR-1004 The "Set Selected As Default" button MUST only be enabled when a combination is selected from the list.
- FR-1005 The combinations list MUST be displayed in a DataGrid with columns for the combination display text and default status.
- FR-1006 The default status indicator MUST use a checkmark symbol (✓) for clarity.
- FR-1007 The window layout MUST accommodate the combinations list and button below the header information, with proper spacing and sizing.
- FR-1008 The SetDefaultCommand in StudynameTechniqueViewModel MUST reload the combinations list after successfully setting a new default to reflect the change immediately.
- FR-1009 The implementation MUST maintain existing functionality for adding new default combinations via the "Add And Set As Default" button.
- FR-1010 The window MUST maintain proper dark theme styling consistent with other Radium windows.

## Update: DataGrid Text Visibility Fix (2025-01-17)
- FR-1042 The "Technique Combination" DataGridTextColumn MUST display text in visible Gainsboro color by setting ElementStyle with Foreground property to ensure visibility against black background in dark theme.

## Update: Delete Combination and ListBox Display Fix in Manage Studyname Techniques Window (2025-01-17)
- FR-1033 The "Current Combination" ListBox MUST display technique text properly by setting DisplayMemberPath to "TechniqueDisplay" as a string property, not via binding.
- FR-1034 Users MUST be able to delete existing combinations from the studyname via a "Delete Selected Combination" button.
- FR-1035 The delete button MUST be placed next to the "Set Selected As Default" button in a vertical stack layout.
- FR-1036 Clicking delete MUST show a confirmation dialog with the combination display text and Yes/No buttons.
- FR-1037 After successful deletion, the combinations list MUST refresh automatically and the selected combination MUST be cleared.
- FR-1038 The "Delete Selected Combination" button MUST only be enabled when a combination is selected from the list.
- FR-1039 The delete operation MUST only remove the link between the studyname and combination (not delete the combination itself from the database).
- FR-1040 If deletion fails, an error message MUST be displayed to the user with the exception message.
- FR-1041 The info text in the right panel MUST be updated to indicate that users can "set default or delete" combinations.

## Update: Save as New Combination Button Enablement Fix (2025-01-18)
- FR-1050 The "Save as New Combination" button MUST enable automatically when at least one technique is added to the Current Combination list.
- FR-1051 The "Save as New Combination" button MUST disable automatically when the Current Combination list is empty (after save or initially).
- FR-1052 The button's CanExecute state MUST be updated by raising CanExecuteChanged on the SaveNewCombinationCommand when CurrentCombinationItems changes.
- FR-1053 The AddTechniqueCommand MUST notify SaveNewCombinationCommand after adding an item to CurrentCombinationItems.
- FR-1054 The SaveNewCombinationCommand MUST notify itself after clearing CurrentCombinationItems following a successful save.

## Update: Current Combination Quick Delete and All Combinations Library (2025-01-18)
- FR-1060 The "Current Combination" ListBox MUST support double-click to remove items from the working combination.
- FR-1061 When a user double-clicks an item in the "Current Combination" ListBox, the item MUST be removed immediately without confirmation.
- FR-1062 After removing an item via double-click, the SaveNewCombinationCommand's CanExecute state MUST be updated (button disables if list becomes empty).
- FR-1063 The left panel MUST include an "All Combinations" ListBox displaying all technique combinations in the database regardless of studyname or study association.
- FR-1064 The "All Combinations" ListBox MUST be populated from a new repository method GetAllCombinationsAsync() that queries all combinations.
- FR-1065 When a user double-clicks an item in the "All Combinations" ListBox, all techniques from that combination MUST be loaded into the "Current Combination" list.
- FR-1066 When loading techniques from "All Combinations", duplicate techniques (same prefix_id, tech_id, suffix_id) MUST be excluded from being added to "Current Combination".
- FR-1067 Techniques loaded from "All Combinations" MUST be appended to the end of "Current Combination" with sequential sequence_order values.
- FR-1068 The LoadCombinationIntoCurrentAsync method MUST fetch combination items via GetCombinationItemsAsync() and match them against loaded component lookup lists.
- FR-1069 The RemoveFromCurrentCombination method MUST remove the specified CombinationItem from CurrentCombinationItems and notify SaveNewCombinationCommand.
- FR-1070 The "Current Combination" GroupBox header MUST include hint text "(double-click to remove)" for user guidance.
- FR-1071 The "All Combinations" GroupBox header MUST include hint text "(double-click to load)" for user guidance.
- FR-1072 The left panel MUST use a 5-row layout: Header (Auto), Builder UI (Auto), Current Combination (Star), All Combinations (Star), Save Button (Auto).
- FR-1073 Both "Current Combination" and "All Combinations" ListBoxes MUST share equal vertical space (both use Star sizing) for balanced UX.
- FR-1074 The window layout MUST remain consistent with existing dark theme styling for the new "All Combinations" ListBox.

## Update: ReportInputsAndJsonPanel Side-by-Side Row Layout for Y-Coordinate Alignment (2025-01-18)
- FR-1080 The ReportInputsAndJsonPanel MUST restructure from column-based to side-by-side row layout to ensure natural Y-coordinate alignment between main and proofread textboxes.
- FR-1081 The layout MUST use 3 columns: Main Input (1*) | Splitter (2px) | Proofread (1*) | Splitter (2px) | JSON (1*).
- FR-1082 Each corresponding textbox pair MUST be placed in the same row to ensure their upper borders align naturally without custom layout code.
- FR-1083 Chief Complaint textbox and Chief Complaint (Proofread) textbox MUST share the same row position and bind MinHeight to synchronize vertical space.
- FR-1084 Patient History textbox and Patient History (Proofread) textbox MUST share the same row position and bind MinHeight to synchronize vertical space.
- FR-1085 Findings textbox and Findings (Proofread) textbox MUST share the same row position and bind MinHeight to synchronize vertical space.
- FR-1086 Conclusion textbox and Conclusion (Proofread) textbox MUST share the same row position and bind MinHeight to synchronize vertical space.
- FR-1087 The Main Input column MUST include non-paired elements (Study Remark, Patient Remark, Edit Buttons) with appropriate spacing.
- FR-1088 The Proofread column MUST display abbreviated labels (e.g., "Chief Complaint (PR)") and smaller controls to fit alongside main column.
- FR-1089 Both Main and Proofread columns MUST use ScrollViewers to handle overflow content independently.
- FR-1090 Scroll synchronization MUST be implemented via ScrollChanged event handler to link main and proofread column scrolling.
- FR-1091 The reverse layout feature MUST continue to work by swapping column positions (Main ↔ JSON) while keeping side-by-side alignment intact.
- FR-1092 All textboxes MUST maintain dark theme styling with appropriate background (#1E1E1E), foreground (#D0D0D0), and border colors (#2D2D30).
- FR-1093 The implementation MUST NOT require custom Y-coordinate calculation, attached behaviors, or manual layout logic—WPF's Grid naturally aligns row elements.
- FR-1094 MinHeight bindings MUST reference corresponding main textbox elements (e.g., txtChiefComplaint, txtPatientHistory) to ensure proofread textboxes don't shrink below main textbox height.
- FR-1095 The layout change MUST maintain backward compatibility with existing bindings (ChiefComplaint, PatientHistory, FindingsText, ConclusionText, and their Proofread counterparts).
- FR-1096 The window MUST remain responsive and functional on both landscape and portrait orientations used in gridTop and gridSideTop panels.

## Update: Foreign Textbox One-Way Sync Feature (2025-01-19)
- FR-1100 Add new UI bookmark `ForeignTextbox` to KnownControl enum for mapping external application textboxes (e.g., Notepad).
- FR-1101 Add "Sync Text" toggle button next to the "Lock" toggle button in MainWindow, default off.
- FR-1102 When sync toggle is ON, start polling foreign textbox for changes (800ms interval).
- FR-1103 When foreign textbox content changes (detected via polling), update read-only EditorForeignText automatically.
- FR-1104 EditorForeignText MUST appear between EditorHeader and EditorFindings with seamless borders (top border on foreign, bottom border on findings).
- FR-1105 EditorForeignText MUST be read-only and collapse to 0 height when sync is disabled.
- FR-1106 EditorForeignText MUST expand to 60-300px height when sync is enabled, with scroll if content exceeds height.
- FR-1107 EditorFindings MUST occupy remaining vertical space in the shared row.
- FR-1108 Border styling MUST make EditorForeignText and EditorFindings appear as one continuous editor.
- FR-1109 TextSyncService MUST use UIA ValuePattern, TextPattern, or Name property as fallback read methods.
- FR-1110 TextSyncService MUST poll foreign textbox at 800ms intervals when sync is enabled.
- FR-1111 TextSyncService MUST raise ForeignTextChanged event on dispatcher thread when changes detected.
- FR-1112 MainViewModel MUST initialize TextSyncService with application dispatcher in constructor.
- FR-1113 MainViewModel MUST handle ForeignTextChanged event to update ForeignText property.
- FR-1114 ForeignText property MUST be read-only (private setter) and cleared when sync is disabled.
- FR-1115 TextSyncEnabled property setter MUST call TextSyncService.SetEnabled to start/stop sync.
- FR-1116 TextSyncService MUST dispose timer and clear state when sync is disabled.
- FR-1117 Foreign textbox resolution MUST use UiBookmarks.Resolve(KnownControl.ForeignTextbox).
- FR-1118 Status messages MUST display "Text sync enabled" and "Text sync disabled" on toggle changes.
- FR-1119 Implementation MUST NOT block UI thread during polling operations (all async).
- FR-1120 TextSyncService MUST handle exceptions gracefully and log to debug output without crashes.
- FR-1121 EditorForeignText and EditorFindings MUST allow seamless caret movement between them (future enhancement).
- FR-1122 Sync is one-way only (foreign → app) to avoid focus stealing and sluggish performance.
