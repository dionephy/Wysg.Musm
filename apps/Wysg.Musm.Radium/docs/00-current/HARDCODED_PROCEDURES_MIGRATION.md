# Hardcoded Custom Procedures Migration Guide (2025-11-26)

## Overview

This document lists all 13 hardcoded fallback procedures that were removed from `ProcedureExecutor.TryCreateFallbackProcedure()`. Users can recreate these procedures manually in SpyWindow ¡æ Custom Procedures tab if needed for their PACS profile.

## Migration Strategy

**Phase 1**: Document all hardcoded procedures (this file)  
**Phase 2**: Remove `TryCreateFallbackProcedure()` method  
**Phase 3**: Users define procedures as needed per-PACS in SpyWindow

## Hardcoded Procedures List

### 1. GetCurrentPatientRemark
**Purpose**: Get patient remark text from UI  
**Default Implementation**:
```
Operation: GetText
Arg1 (Element): PatientRemark
```

### 2. GetCurrentStudyRemark
**Purpose**: Get study remark text from UI  
**Default Implementation**:
```
Operation: GetText
Arg1 (Element): StudyRemark
```

### 3. CustomMouseClick1
**Purpose**: Perform custom mouse click at coordinates (0,0)  
**Default Implementation**:
```
Operation: MouseClick
Arg1 (Number): 0
Arg2 (Number): 0
```

### 4. CustomMouseClick2
**Purpose**: Perform second custom mouse click at coordinates (0,0)  
**Default Implementation**:
```
Operation: MouseClick
Arg1 (Number): 0
Arg2 (Number): 0
```

### 5. InvokeTest
**Purpose**: Invoke test element for testing automation  
**Default Implementation**:
```
Operation: Invoke
Arg1 (Element): TestInvoke
```

### 6. SetCurrentStudyInMainScreen
**Purpose**: Click main screen current study tab  
**Default Implementation**:
```
Operation: ClickElement
Arg1 (Element): Screen_MainCurrentStudyTab
```

### 7. SetPreviousStudyInSubScreen
**Purpose**: Click sub screen previous study tab  
**Default Implementation**:
```
Operation: ClickElement
Arg1 (Element): Screen_SubPreviousStudyTab
```

### 8. WorklistIsVisible
**Purpose**: Check if worklist window is visible  
**Default Implementation**:
```
Operation: IsVisible
Arg1 (Element): WorklistWindow
```

### 9. InvokeOpenWorklist
**Purpose**: Open worklist window  
**Default Implementation**:
```
Operation: Invoke
Arg1 (Element): WorklistOpenButton
```

### 10. SetFocusSearchResultsList
**Purpose**: Set focus on search results list  
**Default Implementation**:
```
Operation: SetFocus
Arg1 (Element): SearchResultsList
```

### 11. SendReport
**Purpose**: Send report to PACS  
**Default Implementation**:
```
Operation: Invoke
Arg1 (Element): SendReportButton
```

### 12. PatientNumberMatch
**Purpose**: Compare patient number (uses direct read)  
**Default Implementation**:
```
Operation: GetCurrentPatientNumber
(No arguments)
```
**Note**: This procedure used special direct MainViewModel comparison logic that is no longer needed. Users should define their own comparison logic using custom procedures.

### 13. StudyDateTimeMatch
**Purpose**: Compare study datetime (uses direct read)  
**Default Implementation**:
```
Operation: GetCurrentStudyDateTime
(No arguments)
```
**Note**: This procedure used special direct MainViewModel comparison logic that is no longer needed. Users should define their own comparison logic using custom procedures.

## How to Recreate Procedures

For each procedure you need:

1. **Open SpyWindow** ¡æ Custom Procedures tab
2. **Select PACS Method** from dropdown
3. **Add Row** for each operation
4. **Configure Operation** (Op, Arg1, Arg2, Arg3)
5. **Map UI Elements** using SpyWindow ¡æ UI Bookmark tab
6. **Save Procedure**

### Example: Recreating GetCurrentPatientRemark

1. Open SpyWindow ¡æ Custom Procedures
2. Select "Get current patient remark" from PACS Method dropdown
3. Click "Add" to add a row
4. Set Operation = "GetText"
5. Set Arg1 Type = "Element", Value = "PatientRemark"
6. Go to UI Bookmark tab, create "PatientRemark" bookmark pointing to the patient remark textbox
7. Return to Custom Procedures, click "Save"
8. Click "Run" to test

## Special Cases

### InvokeOpenStudy
This procedure was intentionally **NOT** given a hardcoded fallback. It **MUST** be explicitly configured per-PACS profile in SpyWindow. If missing, the system throws an error indicating the procedure needs to be configured.

### PatientNumberMatch and StudyDateTimeMatch
These procedures previously used special direct comparison logic accessing MainViewModel properties. The comparison logic has been moved to `ProcedureExecutor.ComparePatientNumber()` and `ProcedureExecutor.CompareStudyDateTime()` which are called by `PatientNumberMatch` and `StudyDateTimeMatch` procedures respectively.

Users should define these procedures to return the current values, and the comparison is handled automatically in the background.

## Migration Impact

### Before Removal
- Procedures auto-created on first use if missing
- Same default behavior for all PACS profiles
- No per-PACS customization

### After Removal
- Procedures must be explicitly defined in SpyWindow
- Full per-PACS customization
- No hidden fallback behavior
- Clear error messages when procedures are missing

## User Action Required

**None immediately** - The application will work without these procedures until they are needed. When automation modules try to use missing procedures, users will see clear error messages and can define them in SpyWindow at that time.

### For Existing Users
Existing users who already have these procedures defined (from previous exports or manual creation) will not be affected. Their custom procedures will continue to work.

### For New Users
New users will need to define procedures as they configure automation for their specific PACS system. This is the intended workflow - per-PACS configuration in SpyWindow.

## Related Documentation

- [SpyWindow User Guide](../02-user/SpyWindow.md) - How to use SpyWindow
- [Custom Procedures Guide](../02-user/CustomProcedures.md) - How to define custom procedures
- [UI Bookmarks Guide](../02-user/UIBookmarks.md) - How to map UI elements
- [Automation Guide](../02-user/Automation.md) - How to use automation modules

---

**Status**: Documentation Complete  
**Migration Date**: 2025-11-26  
**Hardcoded Procedures Removed**: 13  
**User Impact**: Low (procedures can be recreated as needed per-PACS)

