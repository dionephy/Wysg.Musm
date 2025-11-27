# Automation Module Rename and Field-Clearing Modularization (2025-11-27)

**Date**: 2025-11-27  
**Type**: Enhancement + Refactoring  
**Status**: ? Complete  
**Priority**: Normal

---

## Overview

This enhancement renames the "ToggleOff" automation module to "SetCurrentTogglesOff" for clarity, and creates three new modular field-clearing procedures ("ClearCurrentFields", "ClearPreviousFields", and "ClearPreviousStudies") to begin decomposing the monolithic "NewStudy" module.

---

## Changes Summary

### 1. Module Rename: ToggleOff ¡æ SetCurrentTogglesOff

**Rationale**: The name "SetCurrentTogglesOff" is more explicit about what the module does - it sets current report toggles (ProofreadMode and Reportified) to OFF.

**Files Modified**:
- `apps/Wysg.Musm.Radium/ViewModels/SettingsViewModel.cs` - Updated AvailableModules list
- `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.Automation.cs` - Updated module handler

**Behavior** (unchanged):
```csharp
ProofreadMode = false;
Reportified = false;
SetStatus("Toggles off (proofread/reportified off)");
```

**Migration Path**:
- Existing automation sequences using "ToggleOff" will need to be updated to "SetCurrentTogglesOff"
- Settings UI will show the new name in the Available Modules list

---

### 2. New Module: ClearCurrentFields

**Purpose**: Clears all current report fields (reported fields, editable fields, metadata, proofread, patient/study info).

**Interface**: `IClearCurrentFieldsProcedure`  
**Implementation**: `ClearCurrentFieldsProcedure.cs`

**Fields Cleared**:
- **Reported report fields**: ReportedHeaderAndFindings, ReportedFinalConclusion, ReportRadiologist
- **Editable report fields**: FindingsText, ConclusionText
- **Preorder**: FindingsPreorder
- **Metadata**: StudyRemark, PatientRemark
- **Header components**: ChiefComplaint, PatientHistory, StudyTechniques, Comparison
- **Patient/Study info**: PatientName, PatientNumber, PatientSex, PatientAge, StudyName, StudyDateTime
- **Proofread**: FindingsProofread, ConclusionProofread
- **Updates**: UpdateCurrentStudyLabelInternal()

**Usage in Automation**:
```
Sequence: ClearCurrentFields
Effect: Empties all current report JSON fields without affecting previous reports
```

---

### 3. New Module: ClearPreviousFields

**Purpose**: Clears all previous study report fields for EVERY previous study tab.

**Interface**: `IClearPreviousFieldsProcedure`  
**Implementation**: `ClearPreviousFieldsProcedure.cs`

**Fields Cleared** (for each previous study tab):
- **Original report**: Header, Findings, Conclusion, OriginalFindings, OriginalConclusion
- **Split output**: HeaderTemp, FindingsOut, ConclusionOut
- **Metadata**: ChiefComplaint, PatientHistory, StudyTechniques, Comparison, StudyRemark, PatientRemark
- **Proofread**: FindingsProofread, ConclusionProofread
- **Split ranges**: HfHeaderFrom/To, HfConclusionFrom/To, FcHeaderFrom/To, FcFindingsFrom/To

**Usage in Automation**:
```
Sequence: ClearPreviousFields
Effect: Empties all previous study JSON fields without affecting current report
```

---

### 4. New Module: ClearPreviousStudies

**Purpose**: Clears the PreviousStudies collection and resets SelectedPreviousStudy.

**Interface**: `IClearPreviousStudiesProcedure`  
**Implementation**: `ClearPreviousStudiesProcedure.cs`

**Fields Cleared**:
- **PreviousStudies**: Collection cleared
- **SelectedPreviousStudy**: Set to null

**Usage in Automation**:
```
Sequence: ClearPreviousStudies
Effect: Removes all previous study tabs from UI
```

---

### 5. NewStudy Modularization

**Before**: NewStudyProcedure contained all field-clearing logic inline

**After**: NewStudyProcedure now calls modular procedures via dependency injection

**Constructor**:
```csharp
public NewStudyProcedure(
    IClearCurrentFieldsProcedure? clearCurrentFields = null,
    IClearPreviousFieldsProcedure? clearPreviousFields = null,
    IClearPreviousStudiesProcedure? clearPreviousStudies = null,
    ITechniqueRepository? techRepo = null)
```

**Execution Flow**:
1. Call `ClearCurrentFieldsProcedure.ExecuteAsync(vm)`
2. Call `ClearPreviousFieldsProcedure.ExecuteAsync(vm)`
3. Call `ClearPreviousStudiesProcedure.ExecuteAsync(vm)`
4. Toggle off Proofread and Reportified
5. Fetch current study from PACS
6. Auto-fill study techniques if available

**Benefits**:
- **Reusability**: Field-clearing logic can be used independently
- **Testability**: Each procedure can be tested in isolation
- **Maintainability**: Easier to update field-clearing behavior
- **Future-proofing**: Sets foundation for full NewStudy decomposition

---

## Files Created

### 1. `apps/Wysg.Musm.Radium/Services/Procedures/ClearCurrentFieldsProcedure.cs`
- Interface: `IClearCurrentFieldsProcedure`
- Implementation: `ClearCurrentFieldsProcedure`
- Clears all current report fields including patient/study info

### 2. `apps/Wysg.Musm.Radium/Services/Procedures/ClearPreviousFieldsProcedure.cs`
- Interface: `IClearPreviousFieldsProcedure`
- Implementation: `ClearPreviousFieldsProcedure`
- Clears all previous study fields (all tabs)

### 3. `apps/Wysg.Musm.Radium/Services/Procedures/ClearPreviousStudiesProcedure.cs`
- Interface: `IClearPreviousStudiesProcedure`
- Implementation: `ClearPreviousStudiesProcedure`
- Clears PreviousStudies collection and SelectedPreviousStudy

---

## Files Modified

### 1. `apps/Wysg.Musm.Radium/ViewModels/SettingsViewModel.cs`
**Change**: Updated AvailableModules list

**Before**:
```csharp
"NewStudy", "LockStudy", "UnlockStudy", "ToggleOff", ...
```

**After**:
```csharp
"NewStudy", "LockStudy", "UnlockStudy", "SetCurrentTogglesOff", "ClearCurrentFields", "ClearPreviousFields", "ClearPreviousStudies", ...
```

### 2. `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.Automation.cs`
**Change**: Updated module handlers and added new procedure fields

**Added**:
```csharp
else if (string.Equals(m, "SetCurrentTogglesOff", StringComparison.OrdinalIgnoreCase))
{ ... }
else if (string.Equals(m, "ClearCurrentFields", StringComparison.OrdinalIgnoreCase) && _clearCurrentFieldsProc != null)
{ await _clearCurrentFieldsProc.ExecuteAsync(this); }
else if (string.Equals(m, "ClearPreviousFields", StringComparison.OrdinalIgnoreCase) && _clearPreviousFieldsProc != null)
{ await _clearPreviousFieldsProc.ExecuteAsync(this); }
else if (string.Equals(m, "ClearPreviousStudies", StringComparison.OrdinalIgnoreCase) && _clearPreviousStudiesProc != null)
{ await _clearPreviousStudiesProc.ExecuteAsync(this); }
```

### 3. `apps/Wysg.Musm.Radium/Services/Procedures/NewStudyProcedure.cs`
**Change**: Refactored to use modular procedures via dependency injection

**Before**: All field-clearing logic inline (~80 lines)

**After**: Calls ClearCurrentFieldsProcedure, ClearPreviousFieldsProcedure, and ClearPreviousStudiesProcedure (~30 lines)

### 4. `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.cs`
**Change**: Added procedure fields and constructor parameters

**Added Fields**:
```csharp
private readonly IClearCurrentFieldsProcedure? _clearCurrentFieldsProc;
private readonly IClearPreviousFieldsProcedure? _clearPreviousFieldsProc;
private readonly IClearPreviousStudiesProcedure? _clearPreviousStudiesProc;
```

**Updated Constructor**:
```csharp
public MainViewModel(
    ...,
    IClearCurrentFieldsProcedure? clearCurrentFieldsProc = null,
    IClearPreviousFieldsProcedure? clearPreviousFieldsProc = null,
    IClearPreviousStudiesProcedure? clearPreviousStudiesProc = null,
    ...)
```

### 5. `apps/Wysg.Musm.Radium/App.xaml.cs`
**Change**: Registered new procedures in DI container and updated MainViewModel instantiation

**Added**:
```csharp
services.AddSingleton<IClearCurrentFieldsProcedure, ClearCurrentFieldsProcedure>();
services.AddSingleton<IClearPreviousFieldsProcedure, ClearPreviousFieldsProcedure>();
services.AddSingleton<IClearPreviousStudiesProcedure, ClearPreviousStudiesProcedure>();
```

---

## Testing

### Build Verification
? Build succeeded with no errors

### Test Scenarios

#### Scenario 1: SetCurrentTogglesOff Module
```
Steps:
1. Set ProofreadMode=true and Reportified=true
2. Run automation with "SetCurrentTogglesOff"
3. Verify both toggles turn OFF
4. Verify status shows "Toggles off (proofread/reportified off)"

Expected: Both toggles OFF, status updated
```

#### Scenario 2: ClearCurrentFields Module
```
Steps:
1. Fill current report with data (Findings, Conclusion, StudyRemark, PatientName, etc.)
2. Run automation with "ClearCurrentFields"
3. Verify all current fields are empty
4. Verify previous study fields are UNCHANGED

Expected: Current fields cleared, previous studies intact
```

#### Scenario 3: ClearPreviousFields Module
```
Steps:
1. Add previous study with data
2. Run automation with "ClearPreviousFields"
3. Verify all previous study fields are empty
4. Verify current report fields are UNCHANGED

Expected: Previous fields cleared, current report intact
```

#### Scenario 4: ClearPreviousStudies Module
```
Steps:
1. Add 2-3 previous studies
2. Run automation with "ClearPreviousStudies"
3. Verify PreviousStudies collection is empty
4. Verify SelectedPreviousStudy is null

Expected: Previous studies collection cleared
```

#### Scenario 5: NewStudy Module (Modularized)
```
Steps:
1. Fill both current and previous fields with data
2. Add 2-3 previous studies
3. Run automation with "NewStudy"
4. Verify all fields are cleared (current and previous)
5. Verify PreviousStudies collection is cleared
6. Verify toggles are OFF
7. Verify study is unlocked and initialized

Expected: Full NewStudy behavior works as before
```

#### Scenario 6: Custom Sequence with New Modules
```
Sequence: ClearCurrentFields, ClearPreviousFields, ClearPreviousStudies, SetCurrentTogglesOff
Effect: Equivalent to NewStudy field-clearing without study fetch/unlock
```

---

## Use Cases

### Use Case 1: Selective Field Clearing
**Problem**: User wants to clear only current report without affecting previous studies  
**Solution**: Run "ClearCurrentFields" module alone

### Use Case 2: Reset Previous Studies Only
**Problem**: User wants to clear all previous study data without touching current report  
**Solution**: Run "ClearPreviousFields" module alone

### Use Case 3: Remove Previous Studies
**Problem**: User wants to clear previous studies collection  
**Solution**: Run "ClearPreviousStudies" module alone

### Use Case 4: Quick Toggle Reset
**Problem**: User wants to turn off Proofread/Reportified without clearing fields  
**Solution**: Run "SetCurrentTogglesOff" module alone

### Use Case 5: Custom Initialization Sequence
**Problem**: User wants to customize NewStudy behavior  
**Solution**: Create custom sequence like "ClearCurrentFields, GetStudyRemark, LockStudy"

---

## Migration Notes

### Backward Compatibility
?? **Breaking Change**: "ToggleOff" module renamed to "SetCurrentTogglesOff"

**Impact**: Existing automation sequences using "ToggleOff" will fail to find the module

**Migration Required**:
1. Open Settings ¡æ Automation
2. For each sequence containing "ToggleOff":
   - Remove "ToggleOff" from sequence
   - Add "SetCurrentTogglesOff" to sequence at the same position
3. Save automation settings

**Example**:
```
OLD: UnlockStudy, ToggleOff, GetStudyRemark
NEW: UnlockStudy, SetCurrentTogglesOff, GetStudyRemark
```

### Data Migration
? **No data migration required**

---

## Future Roadmap: NewStudy Decomposition

The creation of ClearCurrentFields, ClearPreviousFields, and ClearPreviousStudies is the first step toward full NewStudy modularization.

**Planned Future Modules**:
1. **FetchCurrentStudy** - Call FetchCurrentStudyAsyncInternal()
2. **AutoFillStudyTechniques** - Load default techniques from studyname
3. **UnlockAndInitialize** - Clear locks and update status

**End Goal**: Deprecate monolithic "NewStudy" module in favor of composable sub-modules

---

## Related Documentation

- `apps/Wysg.Musm.Radium/docs/04-archive/2025/ENHANCEMENT_2025-11-09_AutomationModuleSplit.md` - Previous module split (UnlockStudy + ToggleOff)
- `apps/Wysg.Musm.Radium/docs/NEW_OPERATIONS_UNLOC_2025_01_20.md` - Related automation enhancements

---

## Available Automation Modules (Updated)

As of 2025-11-27:
- **NewStudy** - Full study initialization (now calls ClearCurrentFields, ClearPreviousFields, and ClearPreviousStudies internally)
- **LockStudy** - Set PatientLocked=true
- **UnlockStudy** - Set PatientLocked=false, StudyOpened=false
- **SetCurrentTogglesOff** ? (renamed from ToggleOff) - Set ProofreadMode=false, Reportified=false
- **ClearCurrentFields** ? (new) - Clear all current report fields including patient/study info
- **ClearPreviousFields** ? (new) - Clear all previous study fields
- **ClearPreviousStudies** ? (new) - Clear PreviousStudies collection
- GetStudyRemark
- GetPatientRemark
- AddPreviousStudy
- GetUntilReportDateTime
- GetReportedReport
- OpenStudy
- MouseClick1
- MouseClick2
- TestInvoke
- ShowTestMessage
- SetCurrentInMainScreen
- AbortIfWorklistClosed
- AbortIfPatientNumberNotMatch
- AbortIfStudyDateTimeNotMatch
- OpenWorklist
- ResultsListSetFocus
- SendReport
- Reportify
- Delay
- SaveCurrentStudyToDB
- SavePreviousStudyToDB

---

**Implementation Status**: ? Complete  
**Build Status**: ? Success  
**Documentation**: ? Complete  
**Testing**: ? Pending User Verification  
**Migration Guide**: ? Provided

---

*This enhancement improves automation module naming clarity and begins the decomposition of the NewStudy procedure into reusable, testable components.*
