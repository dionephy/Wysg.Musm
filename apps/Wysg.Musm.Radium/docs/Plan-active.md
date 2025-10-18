# Implementation Plan: Radium - Active Plans (Last 90 Days)

**Purpose**: This document contains active implementation plans from the last 90 days.  
**Archive Location**: [docs/archive/](archive/) contains historical plans organized by quarter and feature domain.  
**Last Updated**: 2025-01-19

[¡æ View Archive Index](archive/README.md) | [¡æ View All Archives](archive/)

---

## Quick Navigation

### Recent Implementations (2025-01-01 onward)
- [Foreign Text Merge Caret Preservation (2025-01-19)](#change-log-addition-2025-01-19--foreign-text-merge-caret-preservation-and-focus-management) - **NEW**
- [Current Combination Quick Delete (2025-01-18)](#change-log-addition-2025-01-18--current-combination-quick-delete-and-all-combinations-library)
- [Report Inputs Side-by-Side Layout (2025-01-18)](#change-log-addition-2025-01-18--reportinputsandjsonpanel-side-by-side-row-layout)
- [Phrase-SNOMED Link Window UX (2025-01-15)](#change-log-addition-2025-01-15--phrase-snomed-mapping-window-ux-enhancements)

### Archived Plans (2024 and earlier)
- **2024-Q4**: PACS Automation, Multi-PACS Tenancy ¡æ [archive/2024/Plan-2024-Q4.md](archive/2024/Plan-2024-Q4.md)
- **2025-Q1 (older)**: Technique Management, Editor Features ¡æ [archive/2025-Q1/](archive/2025-Q1/)

---

## Change Log Addition (2025-01-19 ? Foreign Text Merge Caret Preservation and Focus Management)

> **Archive Note**: Detailed plan available in [archive/2025-Q1/Plan-2025-Q1-foreign-text-sync.md](archive/2025-Q1/Plan-2025-Q1-foreign-text-sync.md)

### User Requests
1. On sync text OFF, preserve caret position: `new_caret = old_caret + foreign_text_length + newline`
2. Prevent foreign textbox from stealing focus when cleared (best-effort)

### Implementation Summary

**Caret Preservation Flow**:
1. TextSyncEnabled setter calculates adjustment: `foreignLength = ForeignText.Length + newline`
2. Merge text and set `FindingsCaretOffsetAdjustment = foreignLength`
3. XAML binding flows adjustment: MainViewModel ¡æ EditorControl ¡æ MusmEditor
4. OnDocumentTextChanged applies adjustment after text update, then resets to 0

**Focus Prevention**:
- WriteToForeignAsync no longer calls SetFocus()
- **Note**: Some apps (Notepad) may still steal focus due to app-specific behavior
- UIA ValuePattern.SetValue() works without focus in most cases

### Files Modified (8 files)
1. `MainViewModel.cs` - Added FindingsCaretOffsetAdjustment property
2. `MusmEditor.cs` - Added CaretOffsetAdjustment DP and adjustment logic
3. `EditorControl.View.cs` - Added CaretOffsetAdjustment DP forwarding
4. `CurrentReportEditorPanel.xaml` - Added binding to EditorFindings
5. `TextSyncService.cs` - Removed SetFocus() call, updated comments
6. Documentation updates (Spec.md, Plan-update-caretsync.md, Tasks.md)

### Status
? **Implemented** - Build successful, all tests passing

---

## Change Log Addition (2025-01-18 ? Current Combination Quick Delete and All Combinations Library)

### User Requests
1. Double-click items in "Current Combination" to remove them quickly
2. Add ListBox showing all combinations that can be double-clicked to load

### Implementation Summary

**Quick Delete**:
- Added MouseDoubleClick handler to Current Combination ListBox
- Created `RemoveFromCurrentCombination(item)` method
- Updates SaveNewCombinationCommand after removal
- Updated GroupBox header with hint text

**All Combinations Library**:
- Added `AllCombinations` ObservableCollection to ViewModel
- Created `GetAllCombinationsAsync()` in repository
- Query from `v_technique_combination_display` ordered by id DESC
- Double-click loads combination into Current for modification
- Prevents duplicates during load

**Layout Adjustment**:
- Changed left panel from 4 rows to 5 rows
- Both ListBoxes have equal vertical space

### Files Modified (4 files)
1. `StudynameTechniqueViewModel.cs` - Added collections and methods
2. `TechniqueRepository.cs` + `.Pg.Extensions.cs` - Added query methods
3. `StudynameTechniqueWindow.xaml.cs` - Added layout and event handlers

### Status
? **Implemented** - Feature complete and tested

---

## Change Log Addition (2025-01-18 ? ReportInputsAndJsonPanel Side-by-Side Row Layout)

### User Request
Synchronize Y-coordinates between main textboxes and proofread counterparts dynamically as heights change.

### Problem
Previous column-based layout made alignment impossible without custom behaviors.

### Solution
Restructured to side-by-side row layout where each textbox pair shares the same vertical position naturally via WPF Grid mechanics.

**Height Binding Strategy**:
- Proofread textboxes bind MinHeight to corresponding main textbox MinHeight
- Chief Complaint / Patient History: 60px minimum
- Findings / Conclusion: 100px minimum
- Textboxes grow with content but never shrink below main textbox minimum

**Scroll Synchronization**:
- Added `OnProofreadScrollChanged` event handler
- Uses `_isScrollSyncing` flag to prevent feedback loops
- One-way sync: proofread scroll affects main column

### Files Modified (2 files)
1. `ReportInputsAndJsonPanel.xaml` - Restructured layout with MinHeight bindings
2. `ReportInputsAndJsonPanel.xaml.cs` - Added scroll sync handler

### Status
? **Implemented** - Alignment natural, no custom calculation needed

---

## Change Log Addition (2025-01-15 ? Phrase-SNOMED Mapping Window UX Enhancements)

### Problems
1. Search textbox empty when window opens (user must retype phrase)
2. Map button stays disabled after selecting concept

### Root Causes
1. `SearchText` property not initialized with phrase text
2. `MapCommand.CanExecuteChanged` not raised when `SelectedConcept` changes

### Fixes
1. **Pre-fill Search**: Set `SearchText = phraseText` in constructor
2. **Enable Map Button**: Replace `[ObservableProperty]` with manual property setter calling `MapCommand.NotifyCanExecuteChanged()`

### Files Modified (1 file)
1. `PhraseSnomedLinkWindowViewModel.cs` - Constructor init + manual property

### Status
? **Implemented** - UX improvements deployed

---

## Archived Implementation Plans

Historical implementation plans have been organized by feature domain:

### 2024 Q4 Archives
Contains plans for:
- Study Technique Management (grouped display, autofill, refresh)
- PACS Automation (modules, keyboard shortcuts, global hotkeys)
- Multi-PACS Tenancy (database schema, repositories)
- UI/UX Improvements (window placement, dark scrollbars, ComboBox fixes)
- Previous Report Features (field mapping, split view, reusable control)

**Location**: [archive/2024/Plan-2024-Q4.md](archive/2024/Plan-2024-Q4.md)

### 2025 Q1 Archives (Older Entries)
Contains plans for:
- Phrase-SNOMED Mapping (database schema, Snowstorm API)
- Editor Enhancements (phrase highlighting, syntax coloring)
- Foreign Text Sync (bidirectional sync, polling, merge logic)

**Location**: [archive/2025-Q1/](archive/2025-Q1/)

---

## Finding Historical Plans

### By Date
1. Identify quarter: Q1 (Jan-Mar), Q2 (Apr-Jun), Q3 (Jul-Sep), Q4 (Oct-Dec)
2. Open appropriate archive directory
3. Search by feature name or change log date

### By Feature
Use [archive/README.md](archive/README.md) Feature Domains Index to locate specific implementations

### By Task ID
- T1-T100: Various 2024 features ¡æ [archive/2024/Plan-2024-Q4.md](archive/2024/Plan-2024-Q4.md)
- T1100-T1148: Foreign text sync ¡æ [archive/2025-Q1/Plan-2025-Q1-foreign-text-sync.md](archive/2025-Q1/Plan-2025-Q1-foreign-text-sync.md)

---

## Document Maintenance

### Active Document Criteria
- Implementation plans from last 90 days
- Features under active development
- Features with pending tasks or verification

### Archival Policy
Plans archived when:
- Feature complete and stable 90+ days
- All tasks completed
- No active bugs or enhancement requests

### Archive Maintenance
- Plans maintain full cumulative detail
- Cross-references preserved
- Index updated with each archive

---

*Document last trimmed: 2025-01-19*  
*Next review: 2025-04-19 (90 days)*  
*Total archived: ~1000 lines moved to organized feature archives*
