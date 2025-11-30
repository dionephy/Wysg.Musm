# REFACTOR: Split MainViewModel.Commands.Automation.cs

**Date**: 2025-12-01  
**Status**: ? COMPLETED  
**Type**: Code Organization / Refactoring

---

## Overview

The `MainViewModel.Commands.Automation.cs` file was too long and difficult to maintain. This refactoring splits it into 6 specialized files based on logical functional groupings.

---

## Changes Made

### Files Created

1. **MainViewModel.Commands.Automation.Core.cs**
   - `RunNewStudyProcedureAsync()` - Entry point for NewStudy automation
   - `RunModulesSequentially()` - Main orchestration method that dispatches to all module handlers
   - Contains the central switch logic for all automation modules

2. **MainViewModel.Commands.Automation.Acquire.cs**
   - `AcquireStudyRemarkAsync()` - Acquires study remark with retry logic
   - `AcquirePatientRemarkAsync()` - Acquires patient remark with retry logic
   - `RemoveDuplicateLinesInPatientRemark()` - Deduplication helper
   - `ExtractAngleBracketContent()` - Helper for deduplication

3. **MainViewModel.Commands.Automation.Screen.cs**
   - `RunOpenStudyAsync()` - Opens study in PACS
   - `RunSetCurrentInMainScreenAsync()` - Sets screen layout (current/previous)
   - `RunOpenWorklistAsync()` - Opens PACS worklist
   - `RunResultsListSetFocusAsync()` - Focuses search results list

4. **MainViewModel.Commands.Automation.Report.cs**
   - `RunGetUntilReportDateTimeAsync()` - Polls for report datetime until valid
   - `RunGetReportedReportAsync()` - Acquires reported findings/conclusion/radiologist
   - `RunSendReportAsync()` - Sends report to PACS
   - `RunSendReportModuleWithRetryAsync()` - Complex retry logic for send
   - `DetermineSendReportProcedureAsync()` - Determines which send procedure to use

5. **MainViewModel.Commands.Automation.Database.cs**
   - `RunSaveCurrentStudyToDBAsync()` - Saves current study to database
   - `RunSavePreviousStudyToDBAsync()` - Saves previous study to database

6. **MainViewModel.Commands.Automation.Custom.cs**
   - `RunCustomModuleAsync()` - Executes custom modules (Run/AbortIf/Set types)
   - `SetPropertyValue()` - Sets MainViewModel properties from custom modules

### Files Modified

- **MainViewModel.Commands.Automation.cs**
  - Converted to a documentation-only file
  - Contains header comment listing all new split files
  - Maintains namespace structure for historical reference

---

## File Organization Logic

The split was organized by **functional domain**:

- **Core**: Orchestration and dispatch
- **Acquire**: Data fetching from PACS
- **Screen**: UI/layout operations
- **Report**: Report workflow (get/send/retry)
- **Database**: Persistence operations
- **Custom**: Extension point for user-defined modules

---

## Benefits

1. **Improved Maintainability**: Each file has a clear, focused purpose
2. **Better Navigation**: Easier to find specific automation functionality
3. **Reduced Cognitive Load**: Smaller files are easier to understand
4. **Logical Grouping**: Related methods are colocated
5. **Consistent Pattern**: Follows existing MainViewModel partial file structure

---

## Migration Impact

- **Build Status**: ? No compilation errors
- **Runtime Impact**: None - partial classes are merged at compile time
- **Breaking Changes**: None - all methods remain in MainViewModel
- **Testing Required**: Standard regression testing of automation modules

---

## File Statistics

| Original File | Lines | Split Into | Combined Lines |
|--------------|-------|------------|----------------|
| MainViewModel.Commands.Automation.cs | ~800+ | 6 files + 1 doc | ~800 (no code duplication) |

---

## Related Files

- All MainViewModel partial files in `ViewModels/` directory
- Custom module definitions in `Models/CustomModule.cs`
- ProcedureExecutor in `Services/ProcedureExecutor*.cs`

---

## Next Steps

None required - refactoring is complete and builds successfully.

---

## Notes

- Original file kept as documentation placeholder
- All functionality preserved in split files
- Pattern matches existing MainViewModel organization (Commands.*, CurrentStudy, Editor, etc.)
- Each split file has appropriate XML documentation header
