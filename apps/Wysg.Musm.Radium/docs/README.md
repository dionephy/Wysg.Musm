# Radium Documentation

**Last Updated**: 2025-02-11

---

## Quick Start

### For Current Work (Last 90 Days)
- **[Spec-active.md](Spec-active.md)** - Active feature specifications
- **[Plan-active.md](Plan-active.md)** - Recent implementation plans  
- **[Tasks.md](Tasks.md)** - Active and pending tasks

### Recent Major Features (2025-02-11)

- [NEW] **[ENHANCEMENT_2025-02-11_HotkeysSnippetsEditFeature.md](ENHANCEMENT_2025-02-11_HotkeysSnippetsEditFeature.md)** - ✅ **UI ENHANCEMENT** - Added ability to modify/edit existing hotkeys and snippets in Settings window; users can now click "Edit Selected" to populate input fields with selected item's data, modify expansion/template/description, and click "Update" to save changes; trigger field becomes read-only during edit (prevents accidental trigger change); "Cancel" button allows discarding changes; "Delete" and "Edit" buttons disabled during edit mode for safety; reuses existing UpsertAsync infrastructure (no database changes); improves workflow efficiency by eliminating delete-and-readd process; consistent pattern applied to both Hotkeys and Snippets tabs

- [NEW] **[ENHANCEMENT_2025-02-11_AltLeftCopyToCaretPosition.md](ENHANCEMENT_2025-02-11_AltLeftCopyToCaretPosition.md)** - ✅ **UI ENHANCEMENT** - Modified Alt+Left navigation from previous report editors (editorPreviousFindings, editorPreviousConclusion, editorPreviousHeader) to current findings editor to paste copied text at current caret position instead of appending at end; uses AvalonEdit Document.Insert() for atomic insertion; caret positioned immediately after inserted text for continued typing; matches standard editor behavior (VS Code, Word, etc.); improves workflow efficiency by eliminating manual repositioning steps

- [NEW] **Keyboard Hotkey Shift Modifier Support (2025-02-11)** - ✅ **USABILITY ENHANCEMENT** - Added Shift key recognition to Settings → Keyboard tab hotkey capture. Users can now configure combinations including Shift (e.g., Ctrl+Shift+O, Shift+Alt+T) for Open Study, Send Study, and Toggle Sync Text global hotkeys. Implementation extends capture logic to include LeftShift/RightShift state; parsing and registration logic already supported Shift flag (MOD_SHIFT) so only capture UI needed update. No breaking changes; existing saved hotkeys without Shift unaffected.

- [NEW] **[ENHANCEMENT_2025-02-11_AltArrowNavigationDirectionChanges.md](ENHANCEMENT_2025-02-11_AltArrowNavigationDirectionChanges.md)** - ✅ **UI ENHANCEMENT** - Reorganized Alt+Arrow navigation directions in ReportInputsAndJsonPanel for more intuitive and workflow-optimized navigation; implemented bidirectional navigation where appropriate with clear copyable vs non-copyable transitions; horizontal navigation (Left/Right) for related fields, vertical navigation (Up/Down) for sequential fields; copyable transitions on key workflows (Study Remark → Chief Complaint, Patient Remark → Patient History, Findings → Findings PR); all textboxes now have clearly defined navigation targets for each direction

### Recent Major Features (2025-01-21)

- [NEW] **[BUGFIX_2025-01-21_UISpyAdminPrivilegeSupport.md](BUGFIX_2025-01-21_UISpyAdminPrivilegeSupport.md)** - ✅ **CRITICAL FIX** - Fixed UI Spy "Pick" feature throwing access denied when targeting applications running with administrator privileges; root cause was Windows UIPI (User Interface Privilege Isolation) blocking cross-privilege UI Automation access; solution adds application manifest (`app.manifest`) requesting `highestAvailable` execution level to enable UAC elevation when admin credentials available; now supports picking UI elements from both standard and elevated PACS applications; enables full automation capability for medical imaging workflows requiring elevated privileges; users with admin rights see UAC prompt on launch (standard Windows security behavior)

### Recent Major Features (2025-02-10)

- [NEW] **[BUGFIX_2025-02-10_WebBrowserElementPickerRobustness.md](BUGFIX_2025-02-10_WebBrowserElementPickerRobustness.md)** - ✅ **CRITICAL FIX** - Fixed web browser element picker creating fragile bookmarks that failed validation when tab titles changed; root cause was UseName=True on browser window nodes matching exact dynamic titles (e.g., "ITR Worklist Report - 개인 - Microsoft​ Edge" vs "ITR Worklist Report - Microsoft Edge"); solution disables Name matching for first 3 levels (browser chrome structure), keeps ClassName + ControlTypeId for structural matching, enables AutomationId for web content nodes (level 3+), changes all to Descendants scope for faster search; optimized bookmark creation now robust to tab title changes, faster resolution (~45ms), and uses best identifiers (AutomationId for web elements, ClassName for browser structure); status message updated to indicate "optimized for web stability"

- [NEW] **[ENHANCEMENT_2025-02-10_WebBrowserElementPicker.md](ENHANCEMENT_2025-02-10_WebBrowserElementPicker.md)** - ✅ **NEW FEATURE** - Added "Pick Web" button to UI Spy window for capturing web browser elements with window name context; streamlined workflow: click button → move mouse to element → enter bookmark name → save; bookmarks include process name, element chain (AutomationId preferred), and special comment node with browser window title; enables quick reusable bookmarks for web-based PACS systems and browser automation; auto-reloads bookmarks list after save; dark-themed naming dialog with Enter key support; saved bookmarks appear in dropdown and can be used in custom procedures

- [NEW] **[BUGFIX_2025-02-10_EditorSelectionReplacement.md](BUGFIX_2025-02-10_EditorSelectionReplacement.md)** - ✅ **BUG FIX** - Fixed abnormal text editor behavior where pressing Enter or typing characters when text is selected would not delete the selected text first; root cause was OnTextAreaPreviewKeyDown and OnTextEntering methods not checking for selection before inserting characters; solution adds selection deletion logic before inserting newlines, spaces, or any character; now when text is selected and Enter is pressed, the selected text is deleted and a newline is inserted at that position (standard text editor behavior); when text is selected and Space or any character is pressed, the selected text is deleted and the character is inserted; affects all EditorControl instances (header, findings, conclusion, previous report editors); provides intuitive and expected text editing behavior matching Visual Studio Code, Notepad++, and other standard editors

- [NEW] **[REMOVAL_2025-02-10_ChiefComplaintPatientHistoryProofread.md](REMOVAL_2025-02-10_ChiefComplaintPatientHistoryProofread.md)** - ✅ **UI CLEANUP** - Removed Chief Complaint (PR) and Patient History (PR) textboxes, related toggle buttons, and JSON components; simplified UI by removing 2 rows from ReportInputsAndJsonPanel (from 13 to 11 rows); removed auto toggle properties (AutoChiefComplaintProofread, AutoPatientHistoryProofread) from settings; cleaned up navigation setup and ViewModel properties; only Findings (PR) and Conclusion (PR) remain as proofread fields; reduces UI clutter and improves workflow efficiency

- [NEW] **[BUGFIX_2025-02-10_ComparisonFieldFirstLoad.md](BUGFIX_2025-02-10_ComparisonFieldFirstLoad.md)** - ✅ **BUG FIX** - Fixed comparison field not loading on first tab navigation when patient loaded; root cause was comparison being loaded AFTER tab selection event fired, causing tab to see empty comparison string; solution moves comparison load to run BEFORE tab population so comparison is available for comparison string computation when EditComparisonViewModel initializes; now switching to previous study tabs immediately shows correct comparison field with proper study selection state; completes previous study comparison field feature chain

- [NEW] **[ENHANCEMENT_2025-02-10_CopyStudyRemarkToggle.md](ENHANCEMENT_2025-02-10_CopyStudyRemarkToggle.md)** - ✅ **UI ENHANCEMENT** - Added "copy" toggle button to Chief Complaint field that copies Study Remark text to Chief Complaint; mutually exclusive with "auto" toggle (turning one ON turns the other OFF); provides quick way to use Study Remark as Chief Complaint without manual copy-paste; toggle state persists in local settings; implements efficient one-way copy operation that preserves user edits

### Recent Major Features (2025-02-09)

- [NEW] **[ENHANCEMENT_2025-02-09_DarkThemeTabControl.md](ENHANCEMENT_2025-02-09_DarkThemeTabControl.md)** - ✅ **UI ENHANCEMENT** - Applied dark theme styling to TabControl in JSON panel; selected tab now has darker background (#1E1E1E) instead of bright default; added blue accent border (#2F65C8) for selected state; created DarkTabControlStyle and DarkTabItemStyle with proper hover and disabled states; matches overall dark UI design with consistent color scheme; improves visual comfort and professional appearance

- [NEW] **[FIX_2025-02-09_JsonTextBoxVerticalScrollbar.md](FIX_2025-02-09_JsonTextBoxVerticalScrollbar.md)** - ✅ **BUG FIX** - Added vertical scrollbar to JSON TextBox in ReportInputsAndJsonPanel by wrapping it in a ScrollViewer; fixed issue where JSON TextBox would stretch parent control when content exceeded available height; scrollbar now appears automatically when JSON content is long; horizontal scrolling disabled with text wrapping enabled; uses proper WPF pattern for scrollable TextBox

- [NEW] **[ENHANCEMENT_2025-02-09_RemoveJsonToggleButton.md](ENHANCEMENT_2025-02-09_RemoveJsonToggleButton.md)** - ✅ **UI ENHANCEMENT** - Removed JSON toggle button from ReportInputsAndJsonPanel and made JSON panel always visible by default; simplified UI by eliminating unnecessary toggle action; JSON data now immediately accessible without extra clicks; changed IsJsonCollapsed default value from true to false; kept property for backward compatibility but removed toggle logic and visibility update methods

- [NEW] **[ENHANCEMENT_2025-02-09_MoveEditButtonsToCurrentReportPanel.md](ENHANCEMENT_2025-02-09_MoveEditButtonsToCurrentReportPanel.md)** - ✅ **UI ENHANCEMENT** - Moved "Edit Study Technique" and "Edit Comparison" buttons to CurrentReportEditorPanel toolbar, positioning them between "Send" and "Phrases" buttons for better accessibility; button labels shortened to "Edit Tech" and "Edit Comp" to fit toolbar layout; commands were already implemented, only XAML placement changed; buttons remain disabled when no study loaded and enabled when PatientLocked=true

- [NEW] **[BUGFIX_2025-02-09_BypassAltKeySystemMenu.md](BUGFIX_2025-02-09_BypassAltKeySystemMenu.md)** - ✅ **BUG FIX** - Fixed Alt key system menu activation that was stealing focus to window title bar and causing next key press to be ignored or trigger menu actions; added `OnPreviewKeyDownBypassAlt` event handler that intercepts Alt key when pressed alone (without other modifiers) and marks event as handled to suppress system menu behavior; Alt-based shortcuts (Alt+Arrow, Ctrl+Alt+S, Ctrl+Alt+O, etc.) continue working normally; improves keyboard navigation workflow by preventing unwanted focus changes

### Recent Major Features (2025-02-08)

- [NEW] **[FIX_2025-02-08_PreviousReportSplitRangesLoadingOrder.md](FIX_2025-02-08_PreviousReportSplitRangesLoadingOrder.md)** - ✅ **CRITICAL FIX** - Fixed conclusion editor showing concatenated content when switching between reports; root cause was split ranges being loaded AFTER setting `Findings`/`Conclusion` properties, causing split output computation to use stale/cleared split ranges from previously selected report (e.g., Report B with no split ranges leaves hfCTo=0, then switching to Report A computes splitConclusion with hfCTo=0 instead of 142); solution reorders `ApplyReportSelection()` to update `RawJson` and call `LoadProofreadFieldsFromRawJson()` BEFORE setting `Findings`/`Conclusion`, ensuring split ranges are correct when property change events fire; fixes issue where conclusion editor showed "header + findings + conclusion" instead of just conclusion when reselecting a report after viewing one with no split ranges; completes the previous report selection feature chain

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
- Removed Chief Complaint (PR), Patient History (PR), Study Techniques (PR), and Comparison (PR) textboxes and associated logic from the Previous Report panel
- Removed binding
