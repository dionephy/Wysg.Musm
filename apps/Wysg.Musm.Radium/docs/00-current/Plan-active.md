# Implementation Plan: Radium - Active Plans

**Date**: 2025-11-25  
**Type**: Active Implementation Plans  
**Category**: Technical Documentation  
**Status**: ?? Active (Last 90 Days)

---

## Summary

Detailed implementation plans for Radium project features from the last 90 days. Plans document technical approach, code changes, testing procedures, and cross-references. Historical plans (90+ days) are archived by quarter.

---

## Quick Reference

### Document Statistics
- **Total Active Plans**: 10
- **Date Range**: 2025-11-07 to 2025-11-14
- **All Implemented**: ? 100%
- **Archive Location**: [../../04-archive/](../../04-archive/)

### Plan Categories

| Category | Count | Status |
|----------|-------|--------|
| **Automation Modules** | 2 | ? Complete |
| **PACS Operations** | 3 | ? Complete |
| **UI Enhancements** | 3 | ? Complete |
| **Data Management** | 2 | ? Complete |

---

## Recent Plans by Date

### 2025-11-14

#### SavePreviousStudyToDB Module ?
**Purpose**: Save edited previous study reports to local database

**Implementation**:
- Module: `RunSavePreviousStudyToDBAsync()` in `MainViewModel.Commands.cs`
- Validates: repository, selected previous study, selected report
- Updates: `med.rad_report` table with edited JSON
- Sets: `is_mine=false` (archived reports)

**Files Modified**: 2 (SettingsViewModel.cs, MainViewModel.Commands.cs)

---

#### SaveCurrentStudyToDB Module ?
**Purpose**: Save current study reports to local database

**Implementation**:
- Module: `RunSaveCurrentStudyToDBAsync()` in `MainViewModel.Commands.cs`
- Validates: repository, study context, report datetime
- Inserts: new row in `med.rad_report` table
- Sets: `is_mine=true` (user-authored)

**Files Modified**: 2 (SettingsViewModel.cs, MainViewModel.Commands.cs)

---

### 2025-11-12

#### AddPreviousStudy Comparison Append ?
**Purpose**: Auto-append study info to Comparison field

**Implementation**:
- Extended: `RunAddPreviousStudyModuleAsync()`
- Format: "MODALITY YYYY-MM-DD" (e.g., "CT 2024-01-15")
- Append: Comma-separated for multiple studies

**Files Modified**: 1 (MainViewModel.Commands.cs)

---

#### SetClipboard & TrimString Fixes ?
**Purpose**: Enable variable support and add string trimming

**Implementation**:
- SetClipboard: Removed type forcing to String
- TrimString: New operation removes substring from start/end
- Both: Support String and Var argument types

**Files Modified**: 3 (AutomationWindow files, ProcedureExecutor.cs)

---

#### SetFocus Retry Logic ?
**Purpose**: Improve focus operation reliability

**Implementation**:
- Retry: 3 attempts @ 150ms intervals
- Feedback: Shows attempt count on success
- Error: Detailed message after all attempts fail

**Files Modified**: 2 (AutomationWindow.Procedures.Exec.cs, ProcedureExecutor.cs)

---

### 2025-11-11

#### Foreign Text Merge Caret Preservation ?
**Purpose**: Preserve caret position during text merge

**Implementation**:
- Calculation: `new_caret = old_caret + foreign_length + newline`
- Property: `FindingsCaretOffsetAdjustment` for coordination
- Binding: MainViewModel ¡æ EditorControl ¡æ MusmEditor

**Files Modified**: 8 files
**Archive**: [04-archive/2025-Q4/Plan-2025-Q4-foreign-text-sync.md](../../04-archive/2025-Q4/Plan-2025-Q4-foreign-text-sync.md)

---

### 2025-11-10

#### Current Combination Quick Delete ?
**Purpose**: Quick removal and combination library

**Implementation**:
- Quick Delete: Double-click to remove items
- Library: All combinations ListBox with loading
- Layout: 5-row panel with equal space

**Files Modified**: 4 (ViewModel, Repository, Window files)

---

#### Report Panel Side-by-Side Layout ?
**Purpose**: Natural Y-coordinate alignment

**Implementation**:
- Layout: Side-by-side row structure
- Binding: MinHeight from main to proofread textboxes
- Sync: Scroll synchronization (one-way)

**Files Modified**: 2 (XAML, code-behind)

---

### 2025-11-07

#### Phrase-SNOMED Window UX ?
**Purpose**: Pre-fill search and enable Map button

**Implementation**:
- Pre-fill: SearchText initialized with phrase
- Enable: Manual property setter calls NotifyCanExecuteChanged

**Files Modified**: 1 (PhraseSnomedLinkWindowViewModel.cs)

---

## Implementation Patterns

### Automation Module Pattern
```csharp
// Module registration
availableModules.Add("ModuleName");

// Module handler
if (string.Equals(m, "ModuleName", StringComparison.OrdinalIgnoreCase))
{
    Debug.WriteLine("[Automation] ModuleName - START");
    await RunModuleNameAsync();
    Debug.WriteLine("[Automation] ModuleName - COMPLETED");
}

// Module implementation
private async Task RunModuleNameAsync()
{
    // 1. Validate prerequisites
    // 2. Execute core logic
    // 3. Handle errors
    // 4. Set status message
}
```

### UI Enhancement Pattern
```csharp
// Add event handler
private void OnControlEvent(object sender, EventArgs e)
{
    // 1. Validate state
    // 2. Execute logic
    // 3. Update UI
    // 4. Notify if needed
}

// Update XAML
<Control Event="OnControlEvent" />
```

### Repository Pattern
```csharp
// Interface
Task<TResult?> MethodAsync(parameters);

// Implementation
public async Task<TResult?> MethodAsync(parameters)
{
    // 1. Validate inputs
    // 2. Build query
    // 3. Execute query
    // 4. Map results
    // 5. Return data
}
```

---

## Testing Approach

### Module Testing
1. **Prerequisites**: Test without required setup ¡æ verify error
2. **Success Path**: Test with valid data ¡æ verify success
3. **Error Cases**: Test with invalid data ¡æ verify error handling
4. **Integration**: Test in automation sequence ¡æ verify flow

### UI Testing
1. **User Actions**: Test expected interactions
2. **Edge Cases**: Test boundary conditions
3. **Error States**: Test invalid operations
4. **Visual**: Verify layout and appearance

### Data Testing
1. **CRUD**: Test create, read, update, delete
2. **Constraints**: Test unique keys, foreign keys
3. **Concurrency**: Test simultaneous operations
4. **Performance**: Test with large datasets

---

## Archive Information

### Archived Plans

#### 2025-Q1 (Jan-Mar 2025)
**Location**: [04-archive/2025-Q1/Plan-2025-Q1.md](../../04-archive/2025-Q1/Plan-2025-Q1.md)

**Major Features**:
- Study Technique Management
- PACS Automation Core
- Multi-PACS Tenancy
- UI/UX Improvements
- Previous Report Features

#### 2025-Q2 (Apr-Jun 2025)
**Location**: [04-archive/2025-Q2/](../../04-archive/2025-Q2/)

**Major Features**:
- Phrase-SNOMED Mapping
- Editor Enhancements
- Foreign Text Sync (detailed)

### Archival Policy
**Criteria**:
- Feature complete >90 days
- All tasks completed
- No pending bugs/enhancements

**Next Archive**: 2026-02-12 (90 days from 2025-11-14)

---

## Cross-Reference Index

### By Feature Type

**Automation Modules**:
- SaveCurrentStudyToDB (2025-11-14)
- SavePreviousStudyToDB (2025-11-14)
- AddPreviousStudy Comparison Append (2025-11-12)

**Operations**:
- SetFocus Retry Logic (2025-11-12)
- SetClipboard Variable Support (2025-11-12)
- TrimString (2025-11-12)

**UI Components**:
- Report Panel Layout (2025-11-10)
- Current Combination Management (2025-11-10)
- Phrase-SNOMED Window (2025-11-07)

**Text Synchronization**:
- Foreign Text Merge Caret (2025-11-11)

---

## Related Documents

### Active Documents
- [Spec-active.md](Spec-active.md) - Feature specifications (15 features)
- [Tasks.md](Tasks.md) - Task tracking (1,405 tasks)
- [README.md](../README.md) - Project overview

### Templates & Guides
- [plan-template.md](../../99-templates/plan-template.md) - Implementation plan template
- [12-guides/](../../12-guides/) - User and developer guides

### Archives
- [04-archive/README.md](../../04-archive/README.md) - Archive index
- [04-archive/2025-Q1/](../../04-archive/2025-Q1/) - 2025 Q1 plans
- [04-archive/2025-Q2/](../../04-archive/2025-Q2/) - 2025 Q2 plans
- [04-archive/2025-Q3/](../../04-archive/2025-Q3/) - 2025 Q3 plans
- [04-archive/2025-Q4/](../../04-archive/2025-Q4/) - 2025 Q4 plans

---

## Document Maintenance

### Review Schedule
- **Current Period**: Last 90 days (rolling window)
- **Last Reviewed**: 2025-11-14
- **Next Review**: 2026-02-12 (90 days from last review)
- **Last Archived**: 2025-10-25

### Adding New Plans
1. **Use absolute dates** (YYYY-MM-DD format)
2. **Include**: Problem, Solution, Implementation, Files, Testing
3. **Link**: Related specs, tasks, PRs
4. **Update**: Quick Reference statistics

### Archiving Process
1. **Age check**: >90 days since implementation
2. **Move to**: `04-archive/YYYY-QQ/Plan-YYYY-QQ-{feature}.md`
3. **Update**: Archive index
4. **Remove**: From this document
5. **Preserve**: Cross-references

---

## Tips for Using This Document

### Finding Plans
- **By Date**: Use "Recent Plans by Date" section
- **By Type**: Use Cross-Reference Index
- **By File**: Search for file path
- **Historical**: Check archive/ folders

### Understanding Implementation
- **Pattern**: Check Implementation Patterns section
- **Testing**: Review Testing Approach
- **Code**: See original file for full details

---

## Changelog

### 2025-11-25 - Content Standardization
- Added metadata block and summary
- Added quick reference with statistics
- Added implementation patterns section
- Added testing approach documentation
- Added cross-reference index
- Preserved all 10 active implementation plans
- Improved navigation and structure
- Uses absolute real dates (YYYY-MM-DD format)

### 2025-11-12 - Document Trim
- Archived 2025-Q1 plans
- Archived 2025-Q2 plans
- Kept last 90 days active
- Total archived: ~1000 lines

---

**Last Updated**: 2025-11-25  
**Active Plans**: 10  
**Date Range**: 2025-11-07 to 2025-11-14  
**Completion**: 100%  
**Maintained By**: Development Team

---

## Detailed Plans

The complete detailed implementation plans for all 10 active features are preserved in the original file. This summary provides quick navigation and overview. For full code changes, testing procedures, error handling details, and future enhancements, refer to the original Plan-active.md file.

**Full plans available in**: `apps/Wysg.Musm.Radium/docs/00-current/Plan-active.md` (original file preserved as backup)
