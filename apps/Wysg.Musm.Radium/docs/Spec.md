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

## Update: PACS Custom Procedure – Invoke Open Study (2025-10-13)
- FR-516 SpyWindow Custom Procedures MUST include a PACS method `InvokeOpenStudy` labeled "Invoke open study".
- FR-517 `PacsService` MUST expose `InvokeOpenStudyAsync()` which runs the procedure tag `InvokeOpenStudy` (best-effort, no return value required).
- FR-518 Procedure default (auto-seed) MUST consist of a single `Invoke` op with Arg1 Type=`Element`, Arg1 Value=`SelectedStudyInSearch` to simulate double-click/enter on the selected study row in the search results list.

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
  - `OpenStudy` executes PACS procedure tag `InvokeOpenStudy` (headless: PacsService.InvokeOpenStudyAsync()).
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
- FR-603 Repository behavior: RadStudyRepository and StudynameLoincRepository MUST scope CRUD/queries by current `ITenantContext.TenantId` when set (>0).
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
- `confidence` (DECIMAL(3,2), NULL): Optional confidence score (0.00-1.00)
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

### Notes
- Snowstorm base URL configurable in settings (e.g., `http://localhost:8080`)
- Support multiple SNOMED editions/branches (INT, US, KR) via Snowstorm branch parameter
- Handle API failures gracefully (offline mode uses cached concepts only)
- Consider rate limiting and batching for bulk operations
- Future: Support SNOMED relationships and expression constraints for advanced mappings
