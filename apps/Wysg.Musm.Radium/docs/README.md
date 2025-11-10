# Radium Documentation

**Last Updated**: 2025-02-08

---

## Quick Start

### For Current Work (Last 90 Days)
- **[Spec-active.md](Spec-active.md)** - Active feature specifications
- **[Plan-active.md](Plan-active.md)** - Recent implementation plans  
- **[Tasks.md](Tasks.md)** - Active and pending tasks

### Recent Major Features (2025-02-08)

- [NEW] **[FIX_2025-02-08_PreviousReportSplitRangesLoadingOrder.md](FIX_2025-02-08_PreviousReportSplitRangesLoadingOrder.md)** - ✅ **CRITICAL FIX** - Fixed conclusion editor showing concatenated content when switching between reports; root cause was split ranges being loaded AFTER setting `Findings`/`Conclusion` properties, causing split output computation to use stale/cleared split ranges from previously selected report (e.g., Report B with no split ranges leaves hfCTo=0, then switching to Report A computes splitConclusion with hfCTo=0 instead of 142); solution reorders `ApplyReportSelection()` to update `RawJson` and call `LoadProofreadFieldsFromRawJson()` BEFORE setting `Findings`/`Conclusion`, ensuring split ranges are correct when property change events fire; fixes issue where conclusion editor showed "header + findings + conclusion" instead of just conclusion when reselecting a report after viewing one without split ranges; completes the previous report selection feature chain

- [NEW] **[FIX_2025-02-08_ProofreadFieldsNotUpdatingOnReportChange.md](FIX_2025-02-08_ProofreadFieldsNotUpdatingOnReportChange.md)** - ✅ **CRITICAL FIX** - Fixed proofread textboxes not updating when changing report selection in Previous Report ComboBox; root cause was `ApplyReportSelection()` only updating original text fields (`Findings`, `Conclusion`) and not loading proofread fields from the newly selected report's JSON; solution stores JSON for each individual report in `PreviousReportChoice.ReportJson`, updates `RawJson` when selection changes, parses JSON to extract all 6 proofread fields and 8 split range properties, and notifies all property changes to update UI; now switching reports immediately shows the correct proofread data and split ranges for the selected report; completes the previous study report selection experience

- [NEW] **[FIX_2025-02-08_ProofreadFieldsNotUpdatingToJSON.md](FIX_2025-02-08_ProofreadFieldsNotUpdatingToJSON.md)** - ✅ **CRITICAL FIX** - Fixed proofread field edits not being written to JSON when saving previous studies; root cause was `UpdatePreviousReportJson()` copying stale proofread values from `RawJson` (original database JSON) instead of reading current edited values from tab properties; solution excludes 6 proofread fields (`chief_complaint_proofread`, `patient_history_proofread`, `study_techniques_proofread`, `comparison_proofread`, `findings_proofread`, `conclusion_proofread`) from RawJson copy operation and explicitly writes them from tab properties, ensuring JSON always contains current edited state; fixes data loss issue where user edits to proofread fields would appear in UI but not persist to database; completes the previous study persistence feature chain (disable auto-save → fix Save button → fix split range loading → fix proofread JSON updates)

- [NEW] **[FIX_2025-02-08_PreviousStudySplitRangesNotLoading.md](FIX_2025-02-08_PreviousStudySplitRangesNotLoading.md)** - ✅ **CRITICAL FIX** - Fixed split ranges not persisting across sessions; root cause was `LoadPreviousStudiesForPatientAsync` only reading text fields from JSON and completely skipping the `PrevReport` section containing split range values (`header_and_findings_header_splitter_from/to`, etc.); solution adds JSON parsing of `PrevReport` section and populates all 8 split range properties (`HfHeaderFrom`, `HfHeaderTo`, `HfConclusionFrom`, `HfConclusionTo`, `FcHeaderFrom`, `FcHeaderTo`, `FcFindingsFrom`, `FcFindingsTo`) into the `PreviousStudyTab` object when loading from database; split UI state now fully persists - user can split a report, save it, close patient, reopen, and all splits are restored exactly as saved; completes the split range persistence feature chain (disable auto-save → fix Save button → fix loading from DB)

- [NEW] **[FIX_2025-02-08_SaveButtonNotUpdatingPreviousStudyJSON.md](FIX_2025-02-08_SaveButtonNotUpdatingPreviousStudyJSON.md)** - ✅ **FIXED SAVE BUTTON** - Fixed "Save Previous Study to DB" button not persisting current edits after auto-save on tab switch was disabled; root cause was `PreviousReportJson` property not being synchronized with UI state before save operation; solution adds explicit `UpdatePreviousReportJson()` call in `OnSavePreviousStudyToDB()` to ensure JSON is always current when user clicks Save button; now works correctly regardless of focus state or timing of property change events; complements the auto-save disable feature to give users full control over when changes are persisted; includes enhanced debug logging to diagnose split range persistence issues

- [NEW] **[FIX_2025-02-08_DisableAutoSaveOnPreviousTabSwitch.md](FIX_2025-02-08_DisableAutoSaveOnPreviousTabSwitch.md)** - ✅ **DISABLED AUTO-SAVE** - Disabled automatic saving of previous study JSON changes when switching between tabs; users must now explicitly click "Save Previous Study to DB" button to persist changes; original auto-save code preserved in comments for potential future re-enablement; this change was requested by user to prevent unintended data persistence and give users full control over when previous study edits are saved to database

### Recent Major Features (2025-02-05)

- [NEW] **[FIX_2025-02-05_PhraseSnomedLinkWindow_SearchAndFSN.md](FIX_2025-02-05_PhraseSnomedLinkWindow_SearchAndFSN.md)** - ✅ **COMPLETE FIX** - Fixed "Link Phrase to SNOMED" window search and display issues: (1) **XAML Binding Fix** - corrected FSN column binding from `ConceptId` to `Fsn` property (was showing concept ID numbers instead of Fully Specified Names), (2) **API Endpoint Fix** - switched from `/browser/MAIN/concepts` (returned products/pharmaceuticals) to `/MAIN/concepts` (returns clinical/anatomical concepts); root causes were XAML property mismatch and wrong Snowstorm API terminology subset; search now returns relevant clinical concepts with proper SNOMED terminology matching the query

### Recent Major Features (2025-11-04)

- [NEW] **[FIX_2025-11-04_NumberDigitsInTriggers.md](FIX_2025-11-04_NumberDigitsInTriggers.md)** - Fixed completion popup closing when typing digits in hotkey triggers by unifying word-boundary detection to include digits (and `_`, `-`). Keeps popup open for triggers like "no2" or "f3".

### Recent Major Features (2025-02-02)

- **[FIX_2025-02-02_WordLevelDiffGranularity.md](FIX_2025-02-02_WordLevelDiffGranularity.md)** - Fixed diff viewer to show word-by-word differences instead of marking entire sentences as changed; uses word-level tokenization to preserve spaces and punctuation as separate tokens; now correctly shows granular changes like capitalization fixes ("Decreased" → "decreased"), word insertions ("in hemorrhages" → "in the hemorrhages"), and word reordering ("both frontal, temporal" → "bilateral frontal and temporal"); resolves issue where sentences with many common words were completely striked out and rewritten instead of showing actual word-level changes
- **[FEATURE_2025-02-02_FindingsDiffVisualization.md](FEATURE_2025-02-02_FindingsDiffVisualization.md)** - Findings (PR) textbox now has a collapsible diff viewer panel below showing character-by-character differences between original and proofread text using inline color-coded highlighting (green for additions, red with strikethrough for deletions); uses DiffPlex library (Myers diff algorithm) for production-grade performance (6-13x faster than custom implementation); toggle button to show/hide diff on demand; original Findings (PR) textbox remains fully editable; provides instant visual feedback for AI-generated changes without interfering with editing workflow
- **[FIX_2025-02-02_PreviousReportJsonFieldLoading.md](FIX_2025-02-02_PreviousReportJsonFieldLoading.md)** - Fixed previous report JSON field loading to extract and populate ALL fields from database (metadata, header components, proofread fields) instead of only basic fields; resolves data loss issue where study_remark, patient_remark, and other fields appeared empty despite being stored in database
- **[ENHANCEMENT_2025-02-02_NewStudyEmptyAllJson.md](ENHANCEMENT_2025-02-02_NewStudyEmptyAllJson.md)** - NewStudy module now empties ALL JSON fields (current and previous reports) at the very beginning, not just proofread fields, ensuring clean state for every new study
- **[ENHANCEMENT_2025-02-02_SpyWindowUIEnhancements.md](ENHANCEMENT_2025-02-02_SpyWindowUIEnhancements.md)** - Added GetTextWait operation (waits up to 5 seconds for element visibility), GetCurrentFindingsWait PACS method, and changed "Map to:" label to "Bookmark:" for better clarity in Spy Window
- **[PERFORMANCE_2025-02-02_UiBookmarksFastFail.md](PERFORMANCE_2025-02-02_UiBookmarksFastFail.md)** - Optimized UI bookmark resolution failure time from 30+ seconds to <3 seconds (90%+ faster) by implementing fast-fail heuristic with 150ms threshold (increased from 100ms to catch queries that take 106ms), reduce manual walker cap from 100k to 5k elements for Descendants scope, and add 3-second hard timeout for manual walker to prevent pathological 30-second hangs
- **[PERFORMANCE_2025-02-02_AddPreviousStudyAggressiveRetryReduction.md](PERFORMANCE_2025-02-02_AddPreviousStudyAggressiveRetryReduction.md)** - Further optimized AddPreviousStudy retry logic: reduced primary getters from 2 to 1 attempt with 100ms delay (down from 200ms), optimized alternate getters with 50ms stabilization delay, fixes 52-second pathological case, now completes in <400ms worst-case
- **[ENHANCEMENT_2025-02-02_CollapsibleJsonPanels.md](ENHANCEMENT_2025-02-02_CollapsibleJsonPanels.md)** - Made JSON panels in ReportInputsAndJsonPanel and PreviousReportTextAndJsonPanel collapsible with toggle button, default to collapsed state for better screen space utilization
- **[PERFORMANCE_2025-02-02_AddPreviousStudyEarlyExit.md](PERFORMANCE_2025-02-02_AddPreviousStudyEarlyExit.md)** - Optimized duplicate study detection in AddPreviousStudy from 5.3s to <400ms (93% faster) by moving duplicate check before unnecessary metadata fetches
- **[ENHANCEMENT_2025-02-02_PacsModuleTimingLogs.md](ENHANCEMENT_2025-02-02_PacsModuleTimingLogs.md)** - Added execution time measurement to PACS module logs (e.g., "END: GetSelectedStudynameFromRelatedStudies (543 ms)") for performance monitoring and debugging
- **[PERFORMANCE_2025-02-02_AddPreviousStudyRetryReduction.md](PERFORMANCE_2025-02-02_AddPreviousStudyRetryReduction.md)** - Reduced excessive retries in AddPreviousStudy module from 20 attempts (~2.8s) to 4-5 attempts (~1.0s max), improving performance by 65-85%
- **[ENHANCEMENT_2025-02-02_ConsiderArrowBulletContinuation.md](ENHANCEMENT_2025-02-02_ConsiderArrowBulletContinuation.md)** - New reportify option to treat arrows/bullets as continuations of previous numbered line (hierarchical formatting support)
- **[MAINTENANCE_2025-02-02_DebugLogCleanup.md](MAINTENANCE_2025-02-02_DebugLogCleanup.md)** - Removed excessive debug logging from editor hot paths to improve input responsiveness (Performance improvement)
- **[ENHANCEMENT_2025-02-02_PreviousReportSelector.md](ENHANCEMENT_2025-02-02_PreviousReportSelector.md)** - Previous report selector ComboBox now auto-populates with all reports for selected study, with most recent report selected by default
- **[FIX_2025-01-31_GetCurrentEditorOperationsActualText.md](FIX_2025-01-31_GetCurrentEditorOperationsActualText.md)** - Fixed GetCurrent* operations to return actual editor text instead of bound property values (now returns proofread/reportified text when toggles are ON)
- **[FIX_2025-01-30_CompletionFilterTriggerTextOnly.md](FIX_2025-01-30_CompletionFilterTriggerTextOnly.md)** - Fixed completion window to filter only on trigger text, not description (e.g., "ngi" typed no longer matches "noaa → normal angio")
- **[ENHANCEMENT_2025-01-30_AbortModulesConfirmationDialog.md](ENHANCEMENT_2025-01-30_AbortModulesConfirmationDialog.md)** - Abort modules now show confirmation dialogs instead of immediately aborting, allowing users to force continue procedures despite mismatches
- **[ENHANCEMENT_2025-01-30_CurrentStudyHeaderProofreadVisualization.md](ENHANCEMENT_2025-01-30_CurrentStudyHeaderProofreadVisualization.md)** - Header editor now displays proofread versions of header components when Proofread toggle is ON

### For Historical Reference
- **[archive/](archive/)** - Organized by quarter and feature domain
- **[archive/README.md](archive/README.md)** - Complete archive index

---

## Document Structure

### Active Documents
Files prefixed with `-active` contain work from the last 90 days:
- Small, focused, easy to navigate
- Only current and recent features
- Cross-references to archives for history

### Feature Documentation
Complete feature specifications and implementation guides:
- **SNOMED Integration** - Full SNOMED CT terminology integration
  - `SNOMED_INTEGRATION_COMPLETE.md` - Implementation status and testing
  - `SNOMED_BROWSER_FEATURE_SUMMARY.md` - Feature specification and architecture
  - `SNOMED_CSHARP_INTEGRATION_STEPS.md` - Original integration steps

### Archives
Located in `archive/` directory:
- Organized by year/quarter (e.g., `2024/`, `2025-Q1/`)
- Grouped by feature domain (e.g., `foreign-text-sync`, `pacs-automation`)
- Maintain full cumulative detail
- Never deleted, only updated with corrections

---

## Finding Information

### By Recency
| Time Period | Location |
|-------------|----------|
| Last 7 days | `SNOMED_INTEGRATION_COMPLETE.md`, `SNOMED_BROWSER_FEATURE_SUMMARY.md` |
| Last 90 days | `Spec-active.md`, `Plan-active.md` |
| 2025 Q1 (older) | `archive/2025-Q1/` |
| 2024 Q4 | `archive/2024/` |

### By Feature Domain
| Domain | Documentation Location |
|--------|------------------------|
| **SNOMED CT Integration** | `SNOMED_INTEGRATION_COMPLETE.md`, `SNOMED_BROWSER_FEATURE_SUMMARY.md` |
| Foreign Text Sync | `archive/2025-Q1/Spec-2025-Q1-foreign-text-sync.md` |
| PACS Automation | `archive/2024/Spec-2024-Q4.md` |
| Study Techniques | `archive/2024/Spec-2024-Q4.md` |
| Multi-PACS Tenancy | `archive/2024/Plan-2024-Q4.md` |
| Phrase-SNOMED Mapping | `archive/2025-Q1/Spec-2025-Q1-phrase-snomed.md` |

### By Requirement ID
Use workspace search (Ctrl+Shift+F) to find specific FR-XXX requirements across all docs.

---

## Other Documentation

### Feature-Specific Guides
- `phrase-highlighting-usage.md` - How to use phrase highlighting feature
- `snippet_logic.md` - Snippet expansion rules
- `data_flow.md` - Data flow diagrams

### Architecture & Design
- `spec_editor.md` - Editor component specification
- `snomed-semantic-tag-debugging.md` - SNOMED integration debugging
- `PROCEDUREEXECUTOR_REFACTORING.md` - ProcedureExecutor refactoring (2025-01-16)
- `OPERATION_EXECUTOR_CONSOLIDATION.md` - Operation execution consolidation Phase 1 (2025-01-16)
- `OPERATION_EXECUTOR_PARTIAL_CLASS_SPLIT.md` - Operation execution split Phase 2 (2025-01-16)

### SNOMED CT Integration
- `SNOMED_INTEGRATION_COMPLETE.md` - Complete implementation status
- `SNOMED_BROWSER_FEATURE_SUMMARY.md` - Browser feature specification
- `SNOMED_CSHARP_INTEGRATION_STEPS.md` - Step-by-step integration guide

### Database Documentation
- `db/README_SCHEMA_REFERENCE.md` - Database schema reference
- `db/DEPLOYMENT_GUIDE.md` - Database deployment procedures
- `db/db_central_azure_migration_20251019.sql` - SNOMED schema migration

### Historical Summaries
- `CHANGELOG_*.md` - Feature-specific change logs
- `*_SUMMARY.md` - Implementation summaries for major refactorings

### Templates
- `spec-template.md` - Template for new feature specifications
- `plan-template.md` - Template for implementation plans
- `tasks-template.md` - Template for task tracking
- `agent-file-template.md` - Template for AI agent instructions

---

## Recent Updates (2025-11-04)

### Hotkey Digit Completion Fix (2025-11-04)

**What Changed:**
- Fixed completion popup closing when typing digits in hotkey triggers
- Unified word-boundary detection to include digits (and `_`, `-`)
- Keeps popup open for triggers like "no2" or "f3"

**Why This Matters:**
- **Improved Usability** - Hotkeys with digits can now be used more flexibly
- **Consistency** - Trigger behavior is now consistent regardless of digit presence
- **Efficiency** - Reduces accidental popup closing when typing number-prefixed hotkeys

**Technical Details:**
- Completion word-boundary detection logic was updated
- Now includes digits as part of the trigger word
- Fixes issue where typing a digit would close the completion popup

**Key File Changes:**
- `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.ReportifyHelpers.cs` - Updated word-boundary detection logic

**Documentation:**
- See `FIX_2025-11-04_NumberDigitsInTriggers.md` for complete technical details

**Benefits:**
- ✅ Flexibility in using digits within hotkeys
- ✅ Consistent and predictable completion behavior
- ✅ Improved efficiency when working with number-prefixed triggers

---

### SNOMED Mapping Column Visibility Fix (2025-02-02)

**What Changed:**
- Fixed SNOMED mapping column not displaying details in Global Phrases tab
- Changed from reading non-existent `TagsSemanticTag` and `Tags` properties to proper database loading
- Implemented batch loading of SNOMED mappings using `ISnomedMapService.GetMappingsBatchAsync()`
- Only loads mappings for visible phrases (current page) for optimal performance
- **Displays FSN (Fully Specified Name)** instead of phrase text for clarity

**Why This Matters:**
- **Visibility** - Users can now see which phrases have SNOMED mappings
- **Color Coding** - Semantic tag colors (green for body structure, blue for finding, etc.) now work correctly
- **Performance** - Batch loading avoids N+1 query problem (one query vs 100 queries per page)
- **Reliability** - Mappings loaded from authoritative source (database) not cached properties
- **Clarity** - FSN provides official SNOMED terminology instead of abbreviated phrase text

**Example Behavior:**
```
Before Fix:
  SNOMED Mapping column shows: (empty/blank for all phrases)
  
After Fix:
  SNOMED Mapping column shows:
    - "Chest pain (finding) (SNOMED 29857009)" (in light blue)
    - "Aortic structure (body structure) (SNOMED 15825003)" (in light green)
    - "bilateral" (no mapping, stays default color)
```

**Key Technical Details:**
- Changed `ApplyPhraseFilter()` to async method
- Calls `GetMappingsBatchAsync()` with all phrase IDs on current page
- Uses `mapping.GetSemanticTag()` to extract tag from FSN
- Displays `mapping.Fsn` (Fully Specified Name) for accurate SNOMED terminology
- Silently fails if SNOMED service unavailable (no errors thrown)
- Respects existing color scheme from DataTriggers in XAML

**Key File Changes:**
- `apps\Wysg.Musm.Radium\ViewModels\GlobalPhrasesViewModel.cs` - Updated ApplyPhraseFilter() to async with batch loading and FSN display

**Benefits:**
- **Visual Feedback** - Users can quickly identify SNOMED-mapped phrases
- **Performance** - Single batch query vs multiple individual queries
- **Reliability** - Data comes from database, not stale cache
- **UX** - Color coding helps categorize phrases by semantic type
- **Accuracy** - FSN shows official SNOMED terminology with semantic tag in parentheses

---

### Phrase Tabs Performance Optimization (2025-02-02)

**What Changed:**
- Optimized Settings window Phrases and Global Phrases tabs to handle large datasets (2000+ phrases) efficiently
- Implemented search/filter functionality, pagination (default 100 items per page), alphabetical sorting, UI virtualization with DataGrid row recycling, and deferred SNOMED mapping loading for visible items only
- Achieves 90%+ faster initial load (<1s vs 10-30s), 90%+ lower memory usage (50 MB vs 500 MB), 60 FPS smooth scrolling, and eliminates UI freezing
- Users can search instantly with Enter/Escape key support, adjust page size (10-500), and navigate with First/Prev/Next/Last buttons
- Phrases automatically sorted alphabetically for easy navigation
- Separate pagination for phrase list (PhrasePageSize, PhraseCurrentPageIndex) and Bulk SNOMED search (PageSize, CurrentPage) to avoid naming conflicts

**Why This Matters:**
- **Improved Performance** - Large phrase datasets no longer cause delays or high memory usage
- **Better UX** - Instant search results, smooth scrolling, and responsive pagination controls
- **Efficient Memory Usage** - Significant reduction in memory footprint for large datasets
- **No More Freezes** - Long-loading tabs теперь работают без зависаний UI

**Key Technical Details:**
- Search/filter implemented with TextBox and CollectionViewSource
- Pagination implemented with `PagedCollectionView` and custom page size support
- UI virtualization enabled in DataGrid for efficient row recycling
- SNOMED mapping loading deferred until item is visible in the UI

**Key File Changes:**
- `apps\Wysg.Musm.Radium\Views\SettingsTabs\ReportifySettingsTab.xaml` - Updated Phrases and Global Phrases tab controls
- `apps\Wysg.Musm.Radium\ViewModels\SettingsViewModel.cs` - Updated logic for phrase loading and pagination

**Documentation:**
- See `PERFORMANCE_2025-02-02_PhraseTabsOptimization.md` for complete technical details
- See `ENHANCEMENT_2025-02-02_PreviousReportSelector.md` for related performance improvements

**Benefits:**
- ✅ Instant search/filter results in Phrases and Global Phrases tabs
- ✅ 60 FPS smooth scrolling in DataGrid with UI virtualization
- ✅ Responsive pagination controls with adjustable page size
- ✅ Deferred SNOMED mapping loading for improved tab loading times
- ✅ Overall faster and more efficient phrase management

---

### Unicode Dash Normalization Fix (2025-02-02)

**What Changed:**
- Added automatic normalization of Unicode dash characters (en-dash, em-dash, minus sign) to ASCII hyphens
- Prevents character loss when sending reports to PACS
- Fixes issue where "A2–A3" became "A22A3" due to dash character stripping

**Why This Matters:**
- **Clinical Accuracy** - Medical terminology like "A2-A3 segments" must be preserved exactly
- **Copy-Paste Safety** - Users can now safely copy text from Word, PDF, or web sources
- **Auto-Correct Protection** - Smart editors that convert hyphens to en-dashes no longer cause issues
- **IME Compatibility** - Works correctly with international input methods

**Example Behavior:**
```
User types or pastes: "A2–A3 segments" (en-dash from auto-correct)
System normalizes to:  "A2-A3 segments" (regular hyphen)
PACS receives:         "A2-A3 segments" ✓ Correct!
```

**Technical Implementation:**
- Normalization occurs at the beginning of `ApplyReportifyBlock()` method
- Converts three Unicode dash variants to ASCII hyphen (U+002D):
  - En-dash (U+2013 `–`) → Hyphen
  - Em-dash (U+2014 `—`) → Hyphen  
  - Minus sign (U+2212 `−`) → Hyphen
- Safe: Only affects dash characters, preserves all other Unicode
- Transparent: No user action required, works automatically

**Key File Changes:**
- `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.ReportifyHelpers.cs` - Added Unicode dash normalization

**Documentation:**
- See `FIX_2025-02-02_HyphenNormalization.md` for complete technical details
- See `INVESTIGATION_2025-02-02_HyphenRemovalIssue.md` for root cause analysis

**Benefits:**
- ✅ Prevents medical terminology corruption
- ✅ Copy-paste from any source works correctly
- ✅ Auto-correct protection
- ✅ International input method compatibility
- ✅ No user behavior change required

---

### Previous Study JSON Save on Tab Switch (2025-02-02)

**What Changed:**
- Switching between previous study tabs now automatically saves JSON changes made in the previous tab
- JSON edits (including splitting and manual field changes) are preserved when toggling studies
- No user action required - happens automatically on tab switch

**Why This Matters:**
- **No Data Loss** - Splitting ranges and other JSON edits are automatically persisted when changing tabs
- **Better UX** - Users can freely switch between studies without worrying about losing their work
- **Workflow Efficiency** - Eliminates need to manually save JSON before switching tabs
- **Consistency** - Matches expected behavior from other modern applications

**Example Behavior:**
```
User is viewing Study A with manual splitting applied:
  - Findings split at position 150
  - Conclusion split at position 50
  - JSON edited manually for header_temp

User clicks Study B tab (toggle button / invoke tab):
  1. Study A's current JSON state is saved to its tab object
  2. Study B's JSON is loaded and displayed
  3. User can now edit Study B

User clicks back to Study A tab:
  - Study A's JSON shows the saved splits and edits (preserved!)
  - No data loss occurred
```

**Before This Fix:**
```
User switches from Study A to Study B:
  ❌ Study A's JSON changes lost (splits reset to defaults)
  ❌ Manual JSON edits discarded
  ❌ User must remember to avoid switching tabs while editing
```

**After This Fix:**
```
User switches from Study A to Study B:
  ✅ Study A's JSON changes saved automatically
  ✅ Manual JSON edits preserved
  ✅ User can freely switch tabs without data loss
```

**Key Technical Details:**
- `SelectedPreviousStudy` setter enhanced with save logic
- Calls `ApplyJsonToPrevious()` on outgoing tab before switching
- JSON parse/apply happens synchronously to ensure atomicity
- Error handling preserves tab switch even if JSON save fails

**Key File Changes:**
- `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.PreviousStudies.cs` - Enhanced SelectedPreviousStudy setter with pre-switch save logic

**Benefits:**
- **User Confidence** - Users can experiment with splitting without fear of data loss
- **Workflow Freedom** - No need to complete all edits before switching tabs
- **Reliability** - Automatic saves prevent accidental data loss
- **Consistency** - Behavior matches user expectations from other apps

---

### Alphabetical Sorting in Global Phrases Tab (2025-02-02)

**What Changed:**
- Global phrases now display in alphabetical order (A-Z)
- Case-insensitive sorting for natural ordering
- Sorting applied automatically to all phrases and search results
- No user action required

**Why This Matters:**
- **Easy to Find** - Users can quickly locate phrases by scrolling alphabetically
- **Predictable** - Consistent ordering across sessions and searches
- **Professional** - Standard UI pattern for lists of text items
- **Performance** - Sorting happens in-memory, no database query overhead

**Example Behavior:**
```
Before Sorting (by update date):
  - chest pain (updated 2025-01-01)
  - aortic dissection (updated 2025-01-30)
  - bilateral (updated 2025-01-28)
  - Artery (updated 2025-01-25)

After Sorting (alphabetical A-Z):
  - aortic dissection
  - Artery
  - bilateral
  - chest pain
```

**Key Technical Details:**
- Uses `StringComparer.OrdinalIgnoreCase` for case-insensitive comparison
- Sorting applied in `ApplyPhraseFilter()` method after filtering
- Occurs before pagination, so each page shows alphabetically ordered subset
- Status message updated to show "sorted A-Z" indicator

**Key File Changes:**
- `apps\Wysg.Musm.Radium\ViewModels\GlobalPhrasesViewModel.cs` - Added OrderBy() in ApplyPhraseFilter()
- `apps\Wysg.Musm.Radium\ViewModels\GlobalPhrasesViewModel.Commands.cs` - Removed OrderByDescending() from RefreshPhrasesAsync()

**Benefits:**
- **Easier Navigation** - Users can quickly find phrases by first letter
- **Consistent UX** - Matches standard list sorting behavior
- **No Performance Impact** - In-memory sorting is instant
- **Works with Search** - Search results also alphabetically sorted

---

### Collapsible JSON Panels (2025-02-02)

**What Changed:**
- Added collapse/expand toggle button to JSON columns in both ReportInputsAndJsonPanel and PreviousReportTextAndJsonPanel
- JSON panels now default to collapsed state (hidden) to maximize editing space
- Toggle button shows ▶ when collapsed (click to expand) and ◀ when expanded (click to collapse)
- Column width dynamically changes between 0 (collapsed) and 1* MinWidth 200 (expanded)
- GridSplitter visibility synchronized with collapsed state

**Why This Matters:**
- **More Editing Space** - Default collapsed state provides maximum room for text fields (60% more horizontal space)
- **User Control** - Easy one-click access to JSON view when needed
- **Visual Clarity** - Reduces clutter for normal editing workflow (JSON is reference data, not primary input)
- **Consistent UX** - Both JSON panels (current and previous reports) behave identically

**Example Behavior:**
```
Default State (Collapsed):
  ┌──────────────────────────────────────┐
  │ Main Input  │ Proofread  │ [▶ JSON]  │
  │             │            │           │
  │ [Text...]   │ [Text...]  │           │
  │             │            │           │
  └──────────────────────────────────────┘
  JSON column: 0px width, completely hidden

After Clicking Toggle (Expanded):
  ┌─────────────────────────────────────────────────┐
  │ Main Input  │ Proofread  │ [◀ JSON] │ {       }│
  │             │            │          │ "field" │
  │ [Text...]   │ [Text...]  │          │ ...     │
  │             │            │          │ }       │
  └─────────────────────────────────────────────────┘
  JSON column: 200px minimum, star-sized, splitter visible
```

**Key Technical Details:**
- New dependency property: `IsJsonCollapsed` (bool, default: `true`)
- Binding: Toggle button uses `InverseBooleanConverter` (expanded = IsChecked = true)
- Column structure changed from 5 columns to support splitter visibility:
  - Column 0: Main input (1* MinWidth 200)
  - Column 1: GridSplitter (2px)
  - Column 2: Proofread (1* MinWidth 200)
  - Column 3: JSON GridSplitter (0 when collapsed, 2px when expanded)
  - Column 4: JSON TextBox (0 when collapsed, 1* MinWidth 200 when expanded)
- Unicode characters: ▶ (`\u25B6` / `&#9654;`) and ◀ (`\u25C0` / `&#9664;`)

**Key File Changes:**
- `apps\Wysg.Musm.Radium\App.xaml` - Registered InverseBooleanConverter
- `apps\Wysg.Musm.Radium\Controls\ReportInputsAndJsonPanel.xaml` - Added toggle button and column structure
- `apps\Wysg.Musm.Radium\Controls\ReportInputsAndJsonPanel.xaml.cs` - Added IsJsonCollapsed property and UpdateJsonColumnVisibility method
- `apps\Wysg.Musm.Radium\Controls\PreviousReportTextAndJsonPanel.xaml` - Added toggle button and column structure
- `apps\Wysg.Musm.Radium\Controls\PreviousReportTextAndJsonPanel.xaml.cs` - Added IsJsonCollapsed property and UpdateJsonColumnVisibility method

**Documentation:**
- See `ENHANCEMENT_2025-02-02_CollapsibleJsonPanels.md` for complete feature specification
- See `IMPLEMENTATION_SUMMARY_2025-02-02_CollapsibleJsonPanels.md` for detailed technical implementation

**Benefits:**
- **Space Efficiency** - Default collapsed state optimizes screen real estate for editing
- **Flexibility** - Users can expand JSON when needed for debugging or manual editing
- **Consistency** - Identical behavior in both current and previous report panels
- **Performance** - No performance impact (visibility change is instant)
- **Compatibility** - Works with existing features (Reverse layout, Alt+Arrow navigation, Proofread toggle)

---

### Remove Previous Report Proofread Fields (2025-02-02)

**What Changed:**
- Removed Chief Complaint (PR), Patient History (PR), Study Techniques (PR), and Comparison (PR) textboxes and buttons from the Previous Report panel
- Removed associated dependency properties and logic from PreviousReportTextAndJsonPanel control
- Removed bindings from MainWindow.xaml instances
- Kept Findings (PR) and Conclusion (PR) fields intact

**Why This Matters:**
- **Simplified UI** - Previous Report panel now focuses only on core report content (findings and conclusion)
- **Reduced Clutter** - Removed 8 rows of UI elements (4 label rows + 4 textbox rows) that were rarely used
- **Cleaner Code** - Removed unused dependency properties and bindings
- **Better Focus** - Users can concentrate on the main report content without distraction

**What Was Removed:**
```
Previous Report Panel:
  ❌ Chief Complaint label + textbox + auto/generate buttons
  ❌ Patient History label + textbox + auto/generate buttons
  ❌ Study Techniques label + textbox + auto/generate buttons
  ❌ Comparison label + textbox + auto/generate buttons

Proofread Column:
  ❌ Chief Complaint (PR) label + textbox + auto/generate buttons
  ❌ Patient History (PR) label + textbox + auto/generate buttons
  ❌ Study Techniques (PR) label + textbox + auto/generate buttons
  ❌ Comparison (PR) label + textbox + auto/generate buttons
```

**What Remains:**
```
Previous Report Panel (Column 0):
  ✅ Previous Header and Findings textbox + split buttons
  ✅ Final Conclusion textbox + split buttons
  ✅ Header (temp) textbox
  ✅ Findings (split) textbox
  ✅ Conclusion (split) textbox

Proofread Column (Column 2):
  ✅ Findings (PR) textbox + auto/generate buttons
  ✅ Conclusion (PR) textbox + auto/generate buttons
```

**Key File Changes:**
- `apps\Wysg.Musm.Radium\Controls\PreviousReportTextAndJsonPanel.xaml` - Removed rows 9-16 (8 rows for 4 fields)
- `apps\Wysg.Musm.Radium\Controls\PreviousReportTextAndJsonPanel.xaml.cs` - Removed 8 dependency properties
- `apps\Wysg.Musm.Radium\Views\MainWindow.xaml` - Removed bindings from 2 control instances

**Grid Layout Changes:**
- Total rows reduced from 21 to 13 (removed 8 rows)
- GridSplitter RowSpan updated from 21 to 13
- JSON TextBox RowSpan updated from 21 to 13

**Build Status:**
- ✅ Build successful with no errors

**Documentation:**
- Updated README.md with cumulative change entry

---

### Previous Report Selector - Automatic Population (2025-02-02)

**What Changed:**
- Previous report selector ComboBox (`cboPrevReport`) now automatically populates with all available reports when a previous study tab is selected
- Most recent report (by `report_datetime`) is automatically selected as the default
- Removed dummy ComboBoxItem and simplified XAML binding
- Clean, direct binding to `SelectedPreviousStudy.Reports` collection

**Why This Matters:**
- **Automatic Population** - No manual intervention required to see available reports
- **Smart Default Selection** - Most recent report shown immediately for quick review
- **Version Control** - Easy access to all report versions (preliminary, final, addendum)
- **Cleaner Code** - Removed `CompositeCollection` hack and dummy items

**Example Behavior:**
```
User selects "CT Chest 2025-01-15" tab
  ↓
ComboBox populates with:
  • CT Chest (2025-01-15 10:30:00) - 2025-01-15 14:30:00 by Dr. Smith  ← Auto-selected (most recent)
  • CT Chest (2025-01-15 10:30:00) - 2025-01-15 11:45:00 by Dr. Jones
  • CT Chest (2025-01-15 10:30:00) - (no report dt) by Resident
  ↓
Editors populate with Dr. Smith's report (most recent)
  ↓
User can select Dr. Jones' report from dropdown to compare versions
```

**Report Display Format:**
```
{Studyname} ({StudyDateTime}) - {ReportDateTime} by {CreatedBy}

Examples:
  MRI Brain (2025-01-20 14:30:00) - 2025-01-20 16:45:00 by Dr. Wilson
  CT Abdomen (2025-01-18 09:15:00) - (no report dt) by Dr. Lee
```

**Key Technical Details:**
- Reports ordered by `report_datetime DESC NULLS LAST` in database query
- `PreviousReportChoice.Display` property formats the dropdown text
- `SelectedPreviousStudy.SelectedReport` drives which report content is shown
- Two-way binding allows user to change selection and updates editors immediately

**Key File Changes:**
- `apps\Wysg.Musm.Radium\Controls\PreviousReportEditorPanel.xaml` - Simplified ComboBox binding

**Documentation:**
- See `ENHANCEMENT_2025-02-02_PreviousReportSelector.md` for complete feature details
- See `IMPLEMENTATION_SUMMARY_2025-02-02_PreviousReportSelector.md` for technical implementation

**Benefits:**
- **User Experience** - Instant access to report history, no extra clicks
- **Code Quality** - Clean XAML, standard WPF patterns, maintainable
- **Workflow Efficiency** - Faster navigation, default selection, easy version comparison

---

### GetCurrent* Operations Now Return Actual Editor Text (2025-01-31)

**What Changed:**
- `GetCurrentHeader`, `GetCurrentFindings`, and `GetCurrentConclusion` operations now read actual editor text instead of bound ViewModel properties
- Operations now correctly return proofread text when Proofread toggle is ON
- Operations now correctly return reportified text when Reportified toggle is ON

**Why This Matters:**
- **Accurate Data Capture** - Automation modules now get exactly what radiologists see on screen
- **Proofread Support** - SendReport and other operations send the proofread versions when enabled
- **Reportified Support** - Operations capture formatted text (capitalization, periods, numbering)
- **WYSIWYG Automation** - What You See Is What You Get - no more mismatches

**Example Behavior:**
```
Before Fix:
  User sees (Proofread ON): "Chest pain, shortness of breath"
  GetCurrentHeader returns: "chest pain" (raw unproofread) ❌ MISMATCH

After Fix:
  User sees (Proofread ON): "Chest pain, shortness of breath"
  GetCurrentHeader returns: "Chest pain, shortness of breath" ✅ MATCH
```

**Technical Implementation:**
```csharp
// OLD (WRONG):
result = mainVM.HeaderText ?? string.Empty;

// NEW (CORRECT):
var gridCenter = mainWindow.FindName("gridCenter") as Controls.CenterEditingArea;
var editorHeader = gridCenter.EditorHeader;
var musmEditor = editorHeader.FindName("Editor") as ICSharpCode.AvalonEdit.TextEditor;
result = musmEditor.Text ?? string.Empty;
```

**Key File Changes:**
- `apps\Wysg.Musm.Radium\Services\OperationExecutor.MainViewModelOps.cs` - Updated all three Get methods

**Documentation:**
- See `FIX_2025-01-31_GETCURRENTEDITOROPERATIONSDACTUALTEXT.md` for complete details

---

### Completion Window Filter - Trigger Text Only (2025-01-30)

**What Changed:**
- Completion window now filters hotkeys and snippets based on trigger/shortcut text only
- Descriptions are no longer included in filter matching
- Display still shows full "trigger → description" format for clarity

**Why This Matters:**
- **Accurate Filtering** - Typing "ngi" no longer shows "noaa → normal angio" (where "ngi" matched "angio" in description)
- **Predictable Behavior** - Only triggers that start with typed prefix appear in list
- **Consistent UX** - Matches modern IDE completion behavior (VS Code, IntelliJ, etc.)
- **Reduced Noise** - Fewer false matches in completion dropdown

**Example Fix:**
```
User types: "ngi"

Before Fix:
  ❌ "noaa → normal angio" appears (wrong - "ngi" matched description "angio")
  ❌ "ct → CT angiogram" appears (wrong - "ngi" matched description "angiogram")
  ✅ "ngi → some snippet" appears (correct - matches trigger)

After Fix:
  ✅ "ngi → some snippet" appears (correct - matches trigger)
  ✅ "noaa → normal angio" correctly filtered out
  ✅ "ct → CT angiogram" correctly filtered out
```

**Key Technical Details:**
- `ICompletionData.Text` property (used for filtering) now contains trigger only
- `ICompletionData.Content` property (used for display) still shows full "trigger → description"
- `ICompletionData.Description` property (used for tooltip) shows detailed info
- Separation of concerns: filter vs display vs tooltip

**Key File Changes:**
- `src\Wysg.Musm.Editor\Snippets\MusmCompletionData.cs` - Fixed Snippet() and Hotkey() factories

**Documentation:**
- See `FIX_2025-01-30_CompletionFilterTriggerTextOnly.md` for complete details
- See `IMPLEMENTATION_SUMMARY_2025-01-30_CompletionFilterTriggerTextOnly.md` for technical summary

---

### Abort Modules Confirmation Dialog (2025-01-30)

**What Changed:**
- Abort modules now show confirmation dialogs instead of immediately aborting
- Users can choose to continue or cancel the abort operation
- Mismatched module parameters are highlighted in red
- Default action button is determined by context (e.g., "Continue Anyway", "Abort", "Ignore", "Retry")

**Why This Matters:**
- **Prevents Accidental Aborts** - Users are less likely to accidentally abort long-running procedures
- **Informed Decisions** - Users can see what will be aborted and why
- **Flexible Recovery** - Users can quickly retry or ignore specific module failures
- **Consistent UX** - Matches existing confirmation dialogs in other parts of the application

**Example Behavior:**
```
1. User clicks "Abort" on a running module.
2. Confirmation dialog appears:
   ┌────────────────────────────┐
   │ Abort Module Confirmation  │
   │                            │
   │ The following module will  │
   │ be aborted:                │
   │ - FetchPatientData         │
   │ - GenerateReport           │
   │                            │
   │ There is a mismatch in the  │
   │ expected parameters. Do you │
   │ want to continue aborting? │
   │                            │
   │ [Abort]                   │
   │ [Continue Anyway]         │
   └────────────────────────────┘
3. User reviews the information and clicks "Continue Anyway".
4. The module aborts, and the remaining procedures continue.
```

**Key Technical Details:**
- Added `AbortConfirmationDialog` component for consistent dialog appearance
- Integrated dialog into `AbortModuleCommand` and `StopProcedureCommand`
- Retained original command behavior as fallback (e.g., `x => true`)

**Documentation:**
- See `ENHANCEMENT_2025-01-30_AbortModulesConfirmationDialog.md` for complete details

---

### Current Study Header Proofread Visualization (2025-01-30)

**What Changed:**
- Header editor in current study now displays proofread versions when Proofread toggle is ON
- Added computed display properties for all header components (ChiefComplaint, PatientHistory, StudyTechniques, Comparison)
- Added `HeaderDisplay` property that formats header using proofread component values
- Header editor binding switches between `HeaderText` (raw) and `HeaderDisplay` (proofread) based on toggle state

**Why This Matters:**
- **Consistent UX** - All report sections (header, findings, conclusion) now support proofread visualization
- **Complete Review** - Users can review entire proofread report before sending (no blind spots)
- **Placeholder Support** - Header proofread text includes `{DDx}`, `{arrow}`, `{bullet}` placeholder replacements
- **Matches Previous Reports** - Mirrors the proofread pattern already working in Previous Reports panel

**Example Behavior:**
```
Proofread Toggle OFF:
  Header shows: "Clinical information: chest pain\n- History of hypertension\nTechniques: CT Chest\nComparison: NA"

Proofread Toggle ON (with proofread fields populated):
  Header shows: "Clinical information: Chest pain\n- History of hypertension, diabetes\nTechniques: CT Chest with IV contrast\nComparison: Previous CT 2024-12-15"
  (Note: Proofread versions may have corrections, expansions, or formatting improvements)
```

**Key Technical Details:**
- Each header component has a `*Display` property (e.g., `ChiefComplaintDisplay`)
- Display properties return proofread version when available, otherwise raw version
- `HeaderDisplay` computes formatted header using display properties instead of raw fields
- Header editor remains read-only (no change to existing behavior)
- Proofread values populated via automation or manual JSON editing

**Key File Changes:**
- `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Editor.cs` - Added display properties and HeaderDisplay
- `apps\Wysg.Musm.Radium\Controls\CurrentReportEditorPanel.xaml` - Updated Header editor binding with DataTrigger
- `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Commands.cs` - Updated ProofreadMode to notify display properties

**Documentation:**
- See `ENHANCEMENT_2025-01-30_CurrentStudyHeaderProofreadVisualization.md` for complete details

---

### Previous Study Conclusion Editor Blank on AddPreviousStudy (2025-01-30)

**What Changed:**
- Fixed conclusion editor appearing blank after `AddPreviousStudy` module execution
- Root cause: Binding initialization race condition where `ConclusionOut` was computed after binding read it
- Solution: Call `UpdatePreviousReportJson()` before notifying editor property changes

**Why This Matters:**
- **Better UX** - Conclusion text appears immediately after adding previous study
- **No Workaround Needed** - Users no longer need to switch tabs and return to see conclusion
- **Consistent Behavior** - Matches findings editor behavior (both populate immediately)

**Technical Details:**
- `SelectedPreviousStudy` setter now calls `UpdatePreviousReportJson()` before `OnPropertyChanged(nameof(PreviousConclusionEditorText))`
- This ensures split outputs (`ConclusionOut`, `FindingsOut`, `HeaderTemp`) are computed before WPF binding reads them
- No breaking changes - all existing previous study functionality preserved
- Minimal performance impact - method was already being called, just moved earlier

**Example Fix:**
```
Before:
  AddPreviousStudy → Select new tab → Notify bindings → Conclusion editor shows "" (ConclusionOut not computed yet)
  User switches tabs → UpdatePreviousReportJson() called → Returns to tab → Conclusion appears

After:
  AddPreviousStudy → Select new tab → UpdatePreviousReportJson() → Notify bindings → Conclusion editor shows conclusion ✓
```

**Key File Changes:**
- `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.PreviousStudies.cs` - Fixed `SelectedPreviousStudy` setter order

**Documentation:**
- See `FIX_2025-01-30_PreviousStudyConclusionBlankOnAdd.md` for complete details

---

### Global Phrase Completion Filter - 3-Word Phrases Fix (2025-01-29)

**What Changed:**
- Increased completion window filter from ≤3 words to ≤4 words for global phrases
- Fixed edge case where 3-word phrases like "vein of calf" were not appearing in completion
- Enhanced debug logging to track vein/ligament/artery phrases

**Why This Matters:**
- **Better UX** - 3-word phrases are still short and useful for medical terminology
- **Consistent Behavior** - Users expect 3-word phrases to work in completion
- **Conservative Fix** - Still filters out very long phrases (5+ words)
- **Priority Ordering** - Completion items now display snippets first, then hotkeys, then phrases

**Example Fix:**
```
3-Word Phrases (NOW APPEAR):
  ✅ "vein of calf"
  ✅ "no evidence of"
  ✅ "anterior descending branch"

4-Word Phrases (NOW APPEAR):
  ✅ "left anterior descending artery"

5+ Word Phrases (STILL FILTERED):
  ❌ "anterior descending branch of left coronary artery" (8 words - too long for dropdown)
```

**Key Technical Details:**
- Changed filter condition from `CountWords(r.Text) <= 3` to `CountWords(r.Text) <= 4`
- Applied to both `GetGlobalPhrasesAsync()` and `GetGlobalPhrasesByPrefixAsync()`
- Syntax highlighting remains unaffected (uses `GetAllPhrasesForHighlightingAsync` with no filter)

**Performance Impact:**
- Minimal - estimated +10-15% more global phrases in dropdown
- Dropdown remains manageable
- No additional database queries

**Key File Changes:**
- `apps\Wysg.Musm.Radium\Services\PhraseService.cs` - Updated filter logic and debug.logging

**Documentation:**
- See `FIX_2025-01-29_GlobalPhraseCompletionFilter3WordFix.md` for complete details

---

### Global Phrase Highlighting Filter Fix (2025-01-29)

**What Changed:**
- Fixed bug where global phrases with >3 words were not appearing in syntax highlighting
- Changed `MainViewModel.Phrases.cs` to use `GetAllPhrasesForHighlightingAsync()` instead of `GetCombinedPhrasesAsync()`
- Completion window still uses filtered list (≤3 words) for performance
- Syntax highlighting now shows ALL phrases regardless of word count

**Why This Matters:**
- **Accuracy** - All global phrases now highlight correctly (e.g., "vein of calf", "anterior descending branch")
- **SNOMED Colors** - Long anatomical terms now show semantic tag colors
- **No Regression** - Completion window still optimized with 3-word filter
- **Separation of Concerns** - Completion (filtered) vs highlighting (unfiltered)

**Example Fix:**
```
Phrase: "vein of calf" (3 words)
  Before: ❌ No highlighting, ✅ Shows in completion
  After:  ✅ Highlights with body structure color (light green), ✅ Shows in completion

Phrase: "anterior descending branch of left coronary artery" (8 words)
  Before: ❌ No highlighting, ❌ Not in completion
  After:  ✅ Highlights with body structure color (light green), ❌ Not in completion (by design)
```

**Key Technical Details:**
- `GetCombinedPhrasesAsync()` - Filters global to ≤3 words (for completion performance)
- `GetAllPhrasesForHighlightingAsync()` - No filter (for highlighting completeness)
- Completion uses `GetCombinedPhrasesByPrefixAsync()` which has built-in 3-word filter
- Highlighting uses unfiltered snapshot for accurate phrase detection

**Performance Impact:**
- Minimal memory increase (~1000 extra phrases)
- No CPU impact (visible lines only)
- No regression in completion window speed

**Key File Changes:**
- `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Phrases.cs` - Changed to use `GetAllPhrasesForHighlightingAsync()`

**Documentation:**
- See `FIX_2025-01-29_GlobalPhraseHighlightingFilter.md` for detailed technical documentation
- See `IMPLEMENTATION_SUMMARY_2025-01-29_GlobalPhraseHighlightingFilter.md` for complete implementation summary

---

### GetTextOCR - Top 40 Pixels Only (2025-01-29)

**What Changed:**
- GetTextOCR operation now captures only the top 40 pixels of the selected element boundary
- Both synchronous and asynchronous variants updated
- Uses `Math.Min(40, elementHeight)` to handle elements shorter than 40 pixels

**Why This Matters:**
- **Faster OCR Processing** - Smaller image region means faster text recognition
- **Reduced Noise** - Avoids capturing irrelevant content below the main text
- **Better Accuracy** - Focuses OCR on the most relevant text area (top portion where text typically resides)
- **Consistent Results** - Standardized capture region across different element heights

**Example Behavior:**
```
Tall Element (200px height):
  Before: Captured 200px height
  After:  Captures 40px height ✓ Faster, more focused

Short Element (25px height):
  Before: Captured 25px height
  After:  Captures 25px height ✓ No change (element height preserved)
```

**Key File Changes:**
- `apps\Wysg.Musm.Radium\Services\OperationExecutor.ElementOps.cs` - Updated ExecuteGetTextOCR and ExecuteGetTextOCRAsync methods

**Documentation:**
- See `ENHANCEMENT_2025-01-29_GetTextOCR_Top40Pixels.md` for complete details

---

### Clear JSON Components and Toggles on NewStudy (2025-01-29)

**What Changed:**
- NewStudy automation module now clears all JSON components BEFORE fetching PACS data
- Toggles off "Proofread" and "Reportified" in current editor section
- Clears all 6 proofread fields in both current and previous reports
- Provides clean slate for every new study

**Why This Matters:**
- **No Cross-Contamination** - Prevents old proofread text from previous study appearing in new study
- **Consistent State** - Every new study starts with same clean state
- **Better UX** - Users don't need to manually clear toggles and JSON fields
- **Automation-Friendly** - Works seamlessly in automation sequences

**Example Behavior:**
```
Before NewStudy:
  - ProofreadMode = true
  - Reportified = true
  - FindingsProofread = "LLM-generated text from previous study"
  - ConclusionProofread = "LLM-generated conclusion"

After NewStudy:
  ✓ ProofreadMode = false
  ✓ Reportified = false
  ✓ FindingsProofread = "" (empty)
  ✓ ConclusionProofread = "" (empty)
  ✓ All 6 current proofread fields cleared
  ✓ All 6 previous proofread fields cleared (if tab selected)
```

**Key File Changes:**
- `apps\Wysg.Musm.Radium\Services\Procedures\NewStudyProcedure.cs` - Added clearing logic at beginning of ExecuteAsync()

**Documentation:**
- See `IMPLEMENTATION_SUMMARY_2025-01-29_ClearJSONOnNewStudy.md` for complete details

---

### Default Differential Diagnosis Field (2025-01-28)

**What Changed:**
- Added new configurable textbox "Default differential diagnosis" in Settings → Reportify → Defaults pane
- Default value: "DDx:" (common medical abbreviation for Differential Diagnosis)
- Persists to database with other reportify settings

**Why This Matters:**
- **User Customization** - Users can set their preferred differential diagnosis prefix (e.g., "DDx:", "Differential:", "DD:")
- **Consistency** - Foundation for future automation features (auto-insertion, templates)
- **Flexibility** - Each account can have their own default

**Example Usage:**
```
Settings → Reportify → Defaults
┌─────────────────────────────────────────────┐
│ Default arrow                    [   -->  ] │
│ Default conclusion numbering     [   1.   ] │
│ Default detailing prefix         [   -    ] │
│ Default differential diagnosis   [   DDx: ] │ ← NEW
└─────────────────────────────────────────────┘
```

**Key File Changes:**
- `apps\Wysg.Musm.Radium\ViewModels\SettingsViewModel.cs` - Added DefaultDifferentialDiagnosis property
- `apps\Wysg.Musm.Radium\Views\SettingsTabs\ReportifySettingsTab.xaml` - Added UI textbox

**Documentation:**
- See `ENHANCEMENT_2025-01-28_DefaultDifferentialDiagnosisField.md` for complete details
- See `IMPLEMENTATION_SUMMARY_2025-01-28_DefaultDifferentialDiagnosisField.md` for technical implementation

---

### Snippet Mode 2 - Only Insert Selected Items (2025-01-28)

**What Changed:**
- Fixed snippet mode 2 (multi-choice) to insert only checked items, not all items
- **Tab key**: When no items are checked, now inserts first option as default (not highlighted item)
- **Enter key**: Inserts checked items for current placeholder (or all items if none checked), defaults for others
- Fallback logic corrected to respect user's explicit selections

**Why This Matters:**
- **Correct Behavior** - Mode 2 snippets now work as documented
- **User Control** - Users can precisely control which items are inserted
- **Predictable** - Different but consistent defaults for Tab (first) vs Enter (all)

**Example Fix:**
```
Snippet: ${2^location^or=r^right|l^left|b^bilateral}

Tab Behavior:
  User checks: r (right), then Tab
  Before: "right, left, or bilateral" ❌ All items
  After:  "right" ✅ Only checked item

User presses Tab without checking
  Before: "right, left, or bilateral" ❌ All items
  After:  "right" ✅ First option (conservative)

Enter Behavior:
  User checks: r (right), then Enter
  Before: "right, left, or bilateral" ❌ All items
  After:  "right" ✅ Only checked item
  
  User presses Enter without checking
  Before: "right, left, or bilateral" ✅ All items (intended fallback)
  After:  "right, left, or bilateral" ✅ All items (fallback preserved)
```

**Key File Changes:**
- `src\Wysg.Musm.Editor\Snippets\SnippetInputHandler.cs` - Fixed Tab and Enter key handlers for mode 2

**Documentation:**
- See `FIX_2025-01-28_SnippetMode2OnlySelectedItems.md` for complete details

---

### Remove Previous Reportified Toggle (2025-01-28)

**What Changed:**
- Removed "Reportified" toggle button from Previous Report panel
- Removed "test button" from Previous Report panel
- Removed all related transformation code (~50 lines)
- Kept "Splitted" and "Proofread" toggles intact

**Why This Matters:**
- **Simplified UI** - Removed confusing and unused toggle
- **Better UX** - Only shows relevant toggles for previous reports  
- **Cleaner Code** - Removed unused transformation logic

**Key File Changes:**
- `apps\Wysg.Musm.Radium\Controls\PreviousReportEditorPanel.xaml` - Removed toggle button
- `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.PreviousStudies.cs` - Removed property and methods
- `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Commands.cs` - Removed toggle set in automation

**Documentation:**
- See `IMPLEMENTATION_SUMMARY_2025-01-28_RemovePreviousReportifiedToggle.md` for complete details

---

### Operation Executor Consolidation (2025-01-16)

**What Changed:**
- **Phase 1 (Consolidation)**: Consolidated duplicate operation execution logic from SpyWindow and ProcedureExecutor
- **Phase 2 (Partial Class Split)**: Split 1500-line OperationExecutor.cs into 8 focused files
- Created shared `OperationExecutor` service with 30+ operations
- Moved sophisticated HTTP/encoding logic from SpyWindow to shared service
- Eliminated ~1500 lines of duplicate code

**Why This Matters:**
- **Fixed GetHTML bug** - Korean encoding now works in both SpyWindow and ProcedureExecutor
- **Consistent behavior** - All operations execute identically in UI testing and automation
- **Single source of truth** - Bug fixes only needed once
- **Easier maintenance** - New operations added in one place
- **Better navigation** - 8 files averaging 190 lines vs 1 file with 1500 lines

**Key Benefits:**
- ✅ GetHTML with Korean/UTF-8/CP949 encoding works everywhere
- ✅ Reduced code duplication: 2200 lines → 1680 lines
- ✅ Simplified operation addition (implement once vs twice)
- ✅ Testable in isolation with mock resolution functions
- ✅ Largest file reduced from 1500 → 350 lines (-77%)

**Documentation:**
- See `OPERATION_EXECUTOR_CONSOLIDATION.md` for Phase 1 (consolidation) details
- See `OPERATION_EXECUTOR_PARTIAL_CLASS_SPLIT.md` for Phase 2 (split) details
- Includes encoding detection pipeline and migration guide

---

### Reportified Toggle Button Automation Fix (2025-01-21)

**What Changed:**
- Fixed Reportified toggle button not updating when automation modules set `Reportified=true`
- PropertyChanged event now always raised to ensure UI synchronization
- Text transformations still only run when value actually changes (no performance impact)

**Why This Matters:**
- Automation sequences can now reliably control the Reportified toggle
- Settings Window > Automation tab can include "Reportify" module in sequences
- Example: `Reportify, Delay, SendReport` now works correctly

**Key File Changes:**
- `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Editor.cs` - Always raise PropertyChanged

**Documentation:**
- See `REPORTIFIED_TOGGLE_AUTOMATION_FIX_2025_01_21.md` for complete details

---

### ProcedureExecutor Refactoring (2025-01-16)

**What Changed:**
- Refactored single ~1600 line file into 5 focused partial class files
- Improved maintainability, testability, and code navigation
- Zero breaking changes - 100% API compatibility maintained

**New File Structure:**
- `ProcedureExecutor.cs` (~400 lines) - Main coordinator and execution flow
- `ProcedureExecutor.Models.cs` (~30 lines) - Data models (ProcStore, ProcOpRow, ProcArg)
- `ProcedureExecutor.Storage.cs` (~40 lines) - JSON persistence layer
- `ProcedureExecutor.Elements.cs` (~200 lines) - Element resolution and caching
- `ProcedureExecutor.Operations.cs` (~900 lines) - 30+ operation implementations

**Benefits:**
- **Maintainability** - Each file has single, clear responsibility
- **Testability** - Isolated concerns enable focused unit testing
- **Extensibility** - Add new operations without affecting other parts
- **Team Collaboration** - Reduced merge conflicts, parallel development

**Documentation:**
- See `PROCEDUREEXECUTOR_REFACTORING.md` for complete details
- Includes architecture diagrams, design patterns, and future improvements

---

### Global Phrase Word Limit in Completion Window + Priority Ordering

**What's New:**
- **Filtered Completion** - Global phrases (account_id IS NULL) now filtered to 3 words or less in completion window
- **Priority Ordering** - Completion items now display in this order:
  1. **Snippets** (Priority 3.0) - Templates with placeholders
  2. **Hotkeys** (Priority 2.0) - Quick text expansions
  3. **Phrases** (Priority 0.0) - Filtered global + unfiltered local phrases
- **Selective Filtering** - Word limit does NOT apply to:
  - Account-specific (local) phrases
  - Hotkeys
  - Snippets

**Rationale:**
- Global phrases are shared across all accounts and can contain long multi-word medical terms
- The completion window becomes cluttered with these longer phrases
- 3-word limit keeps common short phrases available while reducing noise
- Priority ordering ensures most useful items (snippets, hotkeys) appear first
- Users can still access longer phrases via other mechanisms

**Key File Changes:**
- `apps\Wysg.Musm.Radium\Services\PhraseService.cs` - Added word counting and filtering logic
- `src\Wysg.Musm.Editor\Snippets\MusmCompletionData.cs` - Added priority property to control ordering

**Technical Details:**
- Words are counted by splitting on whitespace (space, tab, newline)
- Filter applied in both `GetGlobalPhrasesAsync()` and `GetGlobalPhrasesByPrefixAsync()` methods
- Combined list (`GetCombinedPhrasesByPrefixAsync()`) includes filtered global + unfiltered local phrases
- AvalonEdit's CompletionList automatically sorts by Priority (descending), then alphabetically

---

### SNOMED CT Browser - Complete Implementation ✓

**What's New:**
1. **Full SNOMED Browser UI** - Browse 7 semantic tag domains with pagination
2. **Smart Phrase Management** - Add terms as global phrases with automatic concept mapping
3. **Existence Checking** - Prevents duplicate phrase+concept mappings across all phrases (not just first 100!)
4. **Visual Feedback** - Dark red concept panels for existing mappings
5. **Delete Functionality** - Soft delete global phrases with confirmation
6. **Edit Functionality** - Modify phrase text while preserving SNOMED mappings

**Key Files Added:**
- `Views/SnomedBrowserWindow.xaml` - Browser UI
- `ViewModels/SnomedBrowserViewModel.cs` - Browser logic
- `Services/SnowstormClient.cs` - ECL query support + dual-endpoint strategy
- `SNOMED_BROWSER_FEATURE_SUMMARY.md` - Complete feature documentation

**Performance Improvements:**
- Removed hardcoded `LIMIT 100` from global phrase loading
- Now loads ALL global phrases for 100% accurate existence checks
- Typical memory footprint: ~300 KB per browser session

See **[SNOMED_INTEGRATION_COMPLETE.md](SNOMED_INTEGRATION_COMPLETE.md)** for full details.

---

## Migration Notice

We recently reorganized documentation to improve maintainability:
- **Old files** (Spec.md, Plan.md) are being phased out
- **New structure** uses active docs + archives
- **Transition period**: Until 2025-02-18

See [MIGRATION.md](MIGRATION.md) for details.

---

## Contributing

### Adding New Features
1. Add specification to `Spec-active.md`
2. Add implementation plan to `Plan-active.md`
3. Add tasks to `Tasks.md`
4. Follow cumulative format (append, don't delete)
5. Create feature summary document (see `SNOMED_BROWSER_FEATURE_SUMMARY.md` as example)

### Updating Documentation
- **Recent features**: Update active docs directly
- **Archived features**: Update archive file with correction note
- **Cross-references**: Maintain links between related features
- **Major features**: Create dedicated feature summary document

### Archival Process
Every 90 days:
1. Identify entries older than 90 days in active docs
2. Group by feature domain
3. Move to appropriate archive directory
4. Update `archive/README.md` index
5. Add cross-references in active docs

---

## Documentation Principles

### Cumulative Format
- **Never delete** historical information
- **Always append** new information
- **Mark corrections** with date and reason

### Cross-References
- Link requirements to implementation plans
- Link plans to task lists
- Link archives to related archives
- Link active docs to archives for context

### Clarity
- Use consistent terminology
- Include examples where helpful
- Provide context for decisions
- Document alternatives considered

### Feature Documentation
For major features (like SNOMED Browser):
- Create dedicated feature summary document
- Include user stories and acceptance criteria
- Document architecture with diagrams
- Provide testing guidelines
- List known limitations and future enhancements

---

## File Size Targets

| File Type | Target Size | Action Threshold |
|-----------|-------------|------------------|
| Active Spec | < 500 lines | Archive at 90 days |
| Active Plan | < 500 lines | Archive at 90 days |
| Feature Summary | Any size | Split by sub-feature if > 2000 lines |
| Archive File | Any size | Split by feature if > 2000 lines |

---

## Need Help?

### Can't Find Something?
1. Check `archive/README.md` for complete index
2. Use workspace search (Ctrl+Shift+F)
3. Check recent updates section above
4. Ask team members

### Documentation Issues?
- Broken links? Report immediately
- Missing information? Check archives first
- Unclear sections? Propose improvements
- New major feature? Create feature summary document

---

*For complete archive index, see [archive/README.md](archive/README.md)*  
*For SNOMED integration status, see [SNOMED_INTEGRATION_COMPLETE.md](SNOMED_INTEGRATION_COMPLETE.md)*
