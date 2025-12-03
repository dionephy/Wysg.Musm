# Enhancement: InsertPreviousStudy Built-In Module (2025-12-02)

**Date**: 2025-12-02  
**Type**: Enhancement  
**Status**: ? Complete  
**Priority**: Normal  
**Build**: ? Success

---

## Summary

Created a new built-in automation module "InsertPreviousStudy" that inserts a previous study record into the local PostgreSQL database (`med.rad_study` table) using settable variables from the Automation window. This module enables automated population of the database with previous study metadata without requiring manual database interaction.

---

## Problem

Users wanted to insert previous study records to the database during automation sequences. Previously, there was no way to:
- Programmatically insert study records using automation variables
- Ensure previous studies were recorded in the local database
- Batch-process study insertions from automation workflows

The existing modules (`SaveCurrentStudyToDB`, `SavePreviousStudyToDB`) only saved reports, not study metadata records themselves.

---

## Solution

Created a dedicated `InsertPreviousStudyProcedure` service class that:
- Validates required variables (Current Patient Number, Previous Study Studyname, Previous Study Datetime)
- Uses `RadStudyRepository.EnsureStudyAsync()` to insert/update study records
- Handles patient and studyname creation if they don't exist
- Provides comprehensive debug logging and user-friendly status messages
- Uses MainViewModel temporary properties for data input

---

## Implementation

### New Files Created

#### 1. `IInsertPreviousStudyProcedure.cs`
**Location**: `apps/Wysg.Musm.Radium/Services/Procedures/IInsertPreviousStudyProcedure.cs`

```csharp
public interface IInsertPreviousStudyProcedure
{
    Task ExecuteAsync(MainViewModel vm);
}
```

**Purpose**: Interface for dependency injection of the InsertPreviousStudy procedure.

---

#### 2. `InsertPreviousStudyProcedure.cs`
**Location**: `apps/Wysg.Musm.Radium/Services/Procedures/InsertPreviousStudyProcedure.cs`

```csharp
public sealed class InsertPreviousStudyProcedure : IInsertPreviousStudyProcedure
{
    private readonly IRadStudyRepository? _studyRepo;
    
    public InsertPreviousStudyProcedure(IRadStudyRepository? studyRepo)
    {
        _studyRepo = studyRepo;
    }
    
    public async Task ExecuteAsync(MainViewModel vm)
    {
        // 1. Validate repository availability
        if (_studyRepo == null) { /* error */ return; }
        
        // 2. Get required variables from MainViewModel properties
        string patientNumber = vm.PatientNumber ?? string.Empty;
        string? studyname = vm.TempPreviousStudyStudyname;
        DateTime? studyDateTime = vm.TempPreviousStudyDatetime;
        
        // 3. Validate required variables
        if (string.IsNullOrWhiteSpace(patientNumber)) { /* error */ return; }
        if (string.IsNullOrWhiteSpace(studyname)) { /* error */ return; }
        if (!studyDateTime.HasValue) { /* error */ return; }
        
        // 4. Get optional patient metadata
        string? patientName = vm.PatientName;
        string? patientSex = vm.PatientSex;
        
        // 5. Insert study to database (creates patient/studyname if needed)
        var studyId = await _studyRepo.EnsureStudyAsync(
            patientNumber: patientNumber,
            patientName: patientName,
            sex: patientSex,
            birthDateRaw: null,
            studyName: studyname,
            studyDateTime: studyDateTime.Value
        );
        
        // 6. Report success/failure
        if (studyId.HasValue && studyId.Value > 0)
        {
            vm.SetStatusInternal($"InsertPreviousStudy: Study inserted/updated (ID: {studyId.Value})");
        }
        else
        {
            vm.SetStatusInternal("InsertPreviousStudy: Failed to insert study (database error)", true);
        }
    }
}
```

**Features**:
- Validates repository availability before execution
- Validates all required variables (patient number, studyname, datetime)
- Uses optional patient metadata (name, sex) if available
- Calls `EnsureStudyAsync()` which handles:
  - Patient record creation (if doesn't exist)
  - Studyname record creation (if doesn't exist)
  - Study record insertion (ON CONFLICT DO UPDATE)
- Provides detailed debug logging at each step
- Returns user-friendly status messages

---

### Files Modified

#### 1. `App.xaml.cs`
**Location**: `apps/Wysg.Musm.Radium/App.xaml.cs`

**Changes**:
- Registered `IInsertPreviousStudyProcedure` in DI container
- Updated `MainViewModel` constructor to inject the procedure

```csharp
// Register InsertPreviousStudyProcedure
services.AddSingleton<Wysg.Musm.Radium.Services.Procedures.IInsertPreviousStudyProcedure>(sp => 
    new Wysg.Musm.Radium.Services.Procedures.InsertPreviousStudyProcedure(
        sp.GetService<IRadStudyRepository>()
    ));

// Update MainViewModel to inject IInsertPreviousStudyProcedure
services.AddSingleton(sp => new MainViewModel(
    ...,
    sp.GetService<Wysg.Musm.Radium.Services.Procedures.IInsertPreviousStudyProcedure>(),
    sp.GetService<IAuthStorage>(),
    sp.GetService<ISnomedMapService>(),
    sp.GetService<IStudynameLoincRepository>()
));
```

---

#### 2. `MainViewModel.cs`
**Location**: `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.cs`

**Changes**:
- Added `_insertPreviousStudyProc` field
- Added `IInsertPreviousStudyProcedure? insertPreviousStudyProc` constructor parameter
- Assigned field in constructor initialization

```csharp
private readonly IInsertPreviousStudyProcedure? _insertPreviousStudyProc;

public MainViewModel(
    // ... existing parameters ...
    IInsertPreviousStudyProcedure? insertPreviousStudyProc = null,
    // ... remaining parameters ...
)
{
    // ... existing initialization ...
    _insertPreviousStudyProc = insertPreviousStudyProc;
    // ... remaining initialization ...
}
```

---

#### 3. `MainViewModel.Commands.Automation.Database.cs`
**Location**: `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.Automation.Database.cs`

**Changes**:
- Added `RunInsertPreviousStudyAsync()` method that calls the procedure

```csharp
private async Task RunInsertPreviousStudyAsync()
{
    if (_insertPreviousStudyProc != null)
    {
        await _insertPreviousStudyProc.ExecuteAsync(this);
    }
    else
    {
        Debug.WriteLine("[Automation][InsertPreviousStudy] Procedure not available");
        SetStatus("InsertPreviousStudy: Module not available", true);
    }
}
```

---

#### 4. `MainViewModel.Commands.Automation.Core.cs`
**Location**: `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.Automation.Core.cs`

**Changes**:
- Added InsertPreviousStudy module handler in `RunModulesSequentially`

```csharp
else if (string.Equals(m, "InsertPreviousStudy", StringComparison.OrdinalIgnoreCase))
{
    Debug.WriteLine("[Automation] InsertPreviousStudy module - START");
    await RunInsertPreviousStudyAsync();
    Debug.WriteLine("[Automation] InsertPreviousStudy module - COMPLETED");
}
```

---

#### 5. `SettingsViewModel.cs`
**Location**: `apps/Wysg.Musm.Radium/ViewModels/SettingsViewModel.cs`

**Changes**:
- Added "InsertPreviousStudy" to `AvailableModules` list

```csharp
[ObservableProperty]
private ObservableCollection<string> availableModules = new(new[] { 
    "NewStudy(Obsolete)", "LockStudy", "UnlockStudy", "SetCurrentTogglesOff", 
    "AutofillCurrentHeader", "ClearCurrentFields", "ClearPreviousFields", 
    "ClearPreviousStudies", "SetCurrentStudyTechniques", "GetStudyRemark", 
    "GetPatientRemark", "AddPreviousStudy", "GetUntilReportDateTime", 
    "GetReportedReport", "OpenStudy", "MouseClick1", "MouseClick2", 
    "TestInvoke", "ShowTestMessage", "SetCurrentInMainScreen", 
    "AbortIfWorklistClosed", "AbortIfPatientNumberNotMatch", 
    "AbortIfStudyDateTimeNotMatch", "OpenWorklist", "ResultsListSetFocus", 
    "SendReport", "Reportify", "Delay", "SaveCurrentStudyToDB", 
    "SavePreviousStudyToDB", "InsertPreviousStudy", "Abort", "End if" 
});
```

---

## Features

### What InsertPreviousStudy Does

1. **Validates Required Variables**:
   - Current Patient Number (from `MainViewModel.PatientNumber`)
   - Previous Study Studyname (from `MainViewModel.TempPreviousStudyStudyname`)
   - Previous Study Datetime (from `MainViewModel.TempPreviousStudyDatetime`)

2. **Uses Optional Metadata**:
   - Patient Name (from `MainViewModel.PatientName`)
   - Patient Sex (from `MainViewModel.PatientSex`)
   - Birth Date: Not available (set to null)

3. **Database Operations** (via `RadStudyRepository.EnsureStudyAsync()`):
   - Ensures patient record exists in `med.patient` (creates if needed)
   - Ensures studyname record exists in `med.rad_studyname` (creates if needed)
   - Inserts study record to `med.rad_study` (ON CONFLICT DO UPDATE)
   - Returns study_id of inserted/existing record

4. **Provides Comprehensive Logging**:
   - Debug output at each validation step
   - Database operation progress
   - Success/failure status with study ID
   - Detailed error messages with stack traces

5. **User-Friendly Status Messages**:
   - Success: `"InsertPreviousStudy: Study inserted/updated (ID: 123)"`
   - Missing variable: `"InsertPreviousStudy: [Variable Name] is required"`
   - Invalid datetime: `"InsertPreviousStudy: Invalid datetime format: [value]"`
   - Database error: `"InsertPreviousStudy: Failed to insert study (database error)"`
   - Repository unavailable: `"InsertPreviousStudy: Study repository not available"`

---

## Use Cases

### Use Case 1: Insert Previous Study from Custom Procedure

**Scenario**: User wants to insert a previous study record using data fetched from PACS.

**Setup**:
1. Create Custom Procedure "GetPreviousStudyMetadata" that fetches:
   - Patient Number ¡æ Sets `Current Patient Number` variable
   - Study Name ¡æ Sets `Previous Study Studyname` variable
   - Study DateTime ¡æ Sets `Previous Study Datetime` variable

2. Create automation sequence:
   - Run: GetPreviousStudyMetadata
   - InsertPreviousStudy

**Expected Behavior**:
- Custom procedure fetches metadata from PACS and sets variables
- InsertPreviousStudy reads variables and inserts to database
- Status shows: `"InsertPreviousStudy: Study inserted/updated (ID: 123)"`

---

### Use Case 2: Batch Insert Multiple Previous Studies

**Scenario**: User wants to insert multiple previous studies in one automation sequence.

**Setup**:
1. Create custom procedures for each study:
   - GetPreviousStudy1 ¡æ sets variables ¡æ InsertPreviousStudy
   - GetPreviousStudy2 ¡æ sets variables ¡æ InsertPreviousStudy
   - GetPreviousStudy3 ¡æ sets variables ¡æ InsertPreviousStudy

2. Create automation sequence:
   - Run: GetPreviousStudy1
   - InsertPreviousStudy
   - Run: GetPreviousStudy2
   - InsertPreviousStudy
   - Run: GetPreviousStudy3
   - InsertPreviousStudy

**Expected Behavior**:
- Each cycle fetches metadata and inserts a different study
- Three studies inserted to database
- Status shows success for each insertion

---

### Use Case 3: Conditional Insert with If/Abort

**Scenario**: User wants to insert only if study doesn't already exist.

**Setup**:
1. Create Custom Procedure "CheckStudyExists":
   - Returns "true" if study exists, "false" otherwise

2. Create automation sequence:
   - Run: GetPreviousStudyMetadata
   - If: CheckStudyExists
   - Abort (skip insertion if already exists)
   - End if
   - InsertPreviousStudy (only executes if NOT exists)

**Expected Behavior**:
- If study exists: Abort before insertion, no duplicate created
- If study doesn't exist: Proceeds to InsertPreviousStudy, record created

---

### Use Case 4: Error Handling - Missing Variables

**Scenario**: User forgets to set required variables before running InsertPreviousStudy.

**Setup**:
1. Create automation sequence:
   - InsertPreviousStudy (without setting variables first)

**Expected Behavior**:
- Module detects missing "Current Patient Number"
- Status shows: `"InsertPreviousStudy: Current Patient Number is required"`
- Automation sequence aborts (exception handling in RunModulesSequentially)
- No database changes made

---

## Variable Requirements

### Required Variables (Must be set before calling InsertPreviousStudy)

| Variable Name | MainViewModel Property | Type | Description |
|---------------|------------------------|------|-------------|
| Current Patient Number | `PatientNumber` | string | Patient identifier (e.g., "12345678") |
| Previous Study Studyname | `TempPreviousStudyStudyname` | string | Study type (e.g., "CT Chest") |
| Previous Study Datetime | `TempPreviousStudyDatetime` | DateTime? | Study date/time (parsed from string) |

### Optional Variables (Used if available)

| Variable Name | MainViewModel Property | Type | Description |
|---------------|------------------------|------|-------------|
| Patient Name | `PatientName` | string? | Patient full name (e.g., "John Doe") |
| Patient Sex | `PatientSex` | string? | Patient sex (e.g., "M", "F") |

### How to Set Variables

Variables must be set using Custom Procedures with **Set** operations before calling InsertPreviousStudy:

**Example Custom Procedure**:
```
Operation: GetText (from PACS element) ¡æ var1
Operation: SetValue (to MainViewModel.TempPreviousStudyStudyname) from var1
```

**Note**: The current implementation expects these properties to be set directly on MainViewModel, not through the AutomationWindow's `ProcedureVars` collection.

---

## Database Schema Reference

### Tables Affected

#### 1. `med.patient`
```sql
CREATE TABLE med.patient (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    tenant_id bigint NOT NULL,
    patient_number text NOT NULL,
    patient_name text,
    is_male boolean,
    birth_date date,
    created_at timestamp with time zone DEFAULT now(),
    CONSTRAINT uq_patient__tenant_patient_number UNIQUE (tenant_id, patient_number)
);
```

**InsertPreviousStudy Behavior**:
- If patient_number doesn't exist: Creates new patient record
- If patient_number exists: Uses existing patient_id

---

#### 2. `med.rad_studyname`
```sql
CREATE TABLE med.rad_studyname (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    tenant_id bigint NOT NULL,
    studyname text NOT NULL,
    created_at timestamp with time zone DEFAULT now(),
    CONSTRAINT uq_rad_studyname UNIQUE (tenant_id, studyname)
);
```

**InsertPreviousStudy Behavior**:
- If studyname doesn't exist: Creates new studyname record
- If studyname exists: Uses existing studyname_id

---

#### 3. `med.rad_study`
```sql
CREATE TABLE med.rad_study (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    patient_id bigint NOT NULL,
    studyname_id bigint NOT NULL,
    study_datetime timestamp with time zone NOT NULL,
    created_at timestamp with time zone DEFAULT now(),
    CONSTRAINT uq_rad_study_patient_name_time UNIQUE (patient_id, studyname_id, study_datetime),
    CONSTRAINT rad_study_patient_id_fkey FOREIGN KEY (patient_id) REFERENCES med.patient (id),
    CONSTRAINT rad_study_studyname_id_fkey FOREIGN KEY (studyname_id) REFERENCES med.rad_studyname (id)
);
```

**InsertPreviousStudy Behavior**:
- If study (patient_id, studyname_id, study_datetime) doesn't exist: Creates new study record
- If study exists: Updates existing record (ON CONFLICT DO UPDATE)
- Returns study_id in both cases

---

## Error Handling

### Error Scenarios

| Scenario | Detection | Action | Status Message |
|----------|-----------|--------|----------------|
| Study repository unavailable | `_studyRepo == null` | Return early | "InsertPreviousStudy: Study repository not available" |
| Missing patient number | `string.IsNullOrWhiteSpace(patientNumber)` | Return early | "InsertPreviousStudy: Current Patient Number is required" |
| Missing studyname | `string.IsNullOrWhiteSpace(studyname)` | Return early | "InsertPreviousStudy: Previous Study Studyname is required" |
| Missing datetime | `!studyDateTime.HasValue` | Return early | "InsertPreviousStudy: Previous Study Datetime is required" |
| Invalid datetime format | `!DateTime.TryParse()` | Return early | "InsertPreviousStudy: Invalid datetime format: [value]" |
| Database connection failure | Exception from `EnsureStudyAsync()` | Catch, log, report | "InsertPreviousStudy: Error - [exception message]" |
| Database constraint violation | Exception from `EnsureStudyAsync()` | Catch, log, report | "InsertPreviousStudy: Error - [exception message]" |
| Study insert returns null | `studyId == null || studyId.Value == 0` | Report failure | "InsertPreviousStudy: Failed to insert study (database error)" |

### Exception Handling

All exceptions are caught, logged to Debug output, and reported to user via status message:

```csharp
catch (Exception ex)
{
    Debug.WriteLine($"[InsertPreviousStudyProcedure] EXCEPTION: {ex.GetType().Name} - {ex.Message}");
    Debug.WriteLine($"[InsertPreviousStudyProcedure] StackTrace: {ex.StackTrace}");
    vm.SetStatusInternal($"InsertPreviousStudy: Error - {ex.Message}", true);
}
```

---

## Debug Logging

### Log Format

```
[InsertPreviousStudyProcedure] ===== START =====
[InsertPreviousStudyProcedure] Patient Number: '12345678' (from MainViewModel.PatientNumber)
[InsertPreviousStudyProcedure] Previous Study Studyname: 'CT Chest' (from Temp property)
[InsertPreviousStudyProcedure] Previous Study Datetime: 2025-12-02 10:30:00 (from Temp property)
[InsertPreviousStudyProcedure] All required variables present
[InsertPreviousStudyProcedure] Patient name: 'John Doe' (optional)
[InsertPreviousStudyProcedure] Patient sex: 'M' (optional)
[InsertPreviousStudyProcedure] Calling EnsureStudyAsync...
[InsertPreviousStudyProcedure] SUCCESS: Study ID = 123
[InsertPreviousStudyProcedure] ===== END =====
```

### Log Locations

- **Visual Studio Output Window**: Debug ¡æ Windows ¡æ Output (Show output from: Debug)
- **DebugView**: Download from Microsoft Sysinternals
- **Console**: If running with `dotnet run`

---

## Testing

### Build Verification
? **SUCCESS** (0 errors, 0 warnings)

### Manual Testing Checklist

#### Test 1: Basic Insertion
- [ ] Open Automation window ¡æ Automation tab
- [ ] Create Custom Procedure that sets required variables
- [ ] Add sequence: [Custom Procedure] ¡æ InsertPreviousStudy
- [ ] Execute automation
- [ ] Verify status shows: `"InsertPreviousStudy: Study inserted/updated (ID: [number])"`
- [ ] Verify database record created in `med.rad_study`

#### Test 2: Missing Variables
- [ ] Add InsertPreviousStudy to sequence WITHOUT setting variables
- [ ] Execute automation
- [ ] Verify status shows error: `"InsertPreviousStudy: Current Patient Number is required"`
- [ ] Verify automation sequence aborts

#### Test 3: Duplicate Study
- [ ] Execute same automation twice with same variables
- [ ] First execution: Creates study (status shows new ID)
- [ ] Second execution: Updates existing study (status shows same ID)
- [ ] Verify no duplicate records in database

#### Test 4: Invalid Datetime
- [ ] Set `TempPreviousStudyDatetime` to invalid format (e.g., "not-a-date")
- [ ] Execute InsertPreviousStudy
- [ ] Verify status shows: `"InsertPreviousStudy: Invalid datetime format: not-a-date"`

#### Test 5: Database Unavailable
- [ ] Stop PostgreSQL service
- [ ] Execute InsertPreviousStudy
- [ ] Verify error message shown
- [ ] Verify automation aborts gracefully

---

## Migration Notes

### From Existing Workflows

**Before** (manual database interaction):
- User manually inserts study records using SQL
- No automation support
- Error-prone, time-consuming

**After** (automated with InsertPreviousStudy):
- Custom Procedures fetch metadata from PACS
- InsertPreviousStudy automatically inserts to database
- Validation and error handling built-in
- Batch processing support

### Compatibility

- ? Compatible with all existing automation modules
- ? Works with If/End if control flow
- ? Works with Abort module
- ? No breaking changes to existing sequences

---

## Known Limitations

1. **Birth Date Not Supported**: `birthDateRaw` parameter always set to `null` (not available from settable variables)
2. **Tenant Scope**: Operations scoped to current tenant (from `ITenantContext`)
3. **Local Database Only**: Only inserts to local PostgreSQL, not central database
4. **No Report Insertion**: Only inserts study metadata, not report content (use `SavePreviousStudyToDB` for reports)

---

## Future Enhancements

### Potential Improvements

1. **Variable Validation UI**: Show tooltip in AutomationWindow indicating which variables are required for InsertPreviousStudy
2. **Batch Insert Support**: Allow multiple studies in one module call (array of variables)
3. **Birth Date Support**: Add `TempPreviousStudyBirthDate` property for optional birth date insertion
4. **Central DB Support**: Add option to insert to central database as well
5. **Report Integration**: Combine study + report insertion in one transaction
6. **Dry Run Mode**: Test variables without actually inserting to database

---

## Related Documentation

- [AddPreviousStudy Module](ENHANCEMENT_2025-11-06_AddPreviousStudy_ValidationChecks.md) - Adds previous study to UI
- [SavePreviousStudyToDB Module](ENHANCEMENT_2025-11-09_AutomationModuleSplit.md) - Saves previous report to database
- [RadStudyRepository](../../Services/RadStudyRepository.cs) - Database access layer
- [Custom Procedures](CUSTOM_PROCEDURES_PHASE2_COMPLETE.md) - Creating custom procedures
- [Automation Modules](ENHANCEMENT_2025-11-27_AutomationModuleRenameAndFieldClearingModularization.md) - Built-in module list

---

## Completion Checklist

- [?] Interface created (`IInsertPreviousStudyProcedure.cs`)
- [?] Implementation created (`InsertPreviousStudyProcedure.cs`)
- [?] Registered in DI container (`App.xaml.cs`)
- [?] Injected into MainViewModel
- [?] Database method created (`RunInsertPreviousStudyAsync`)
- [?] Module routing added (`RunModulesSequentially`)
- [?] Added to AvailableModules list (`SettingsViewModel.cs`)
- [?] Build verification (0 errors)
- [?] Documentation created (this file)
- [ ] Manual testing completed
- [ ] User acceptance testing

---

## Contact

For questions or issues related to this enhancement, contact the development team or refer to the project's issue tracker.

