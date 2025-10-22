# Feature Specification: Radium - Active Features (Last 90 Days)

**Purpose**: This document contains active feature specifications from the last 90 days.  
**Archive Location**: [docs/archive/](archive/) contains historical specifications organized by quarter and feature domain.  
**Last Updated**: 2025-01-20

[?? View Archive Index](archive/README.md) | [??? View All Archives](archive/)

---

## Quick Navigation

### Recent Features (2025-01-01 onward)
- [FR-1150: SavePreviousStudyToDB Automation Module](#fr-1150-savepreviousstudytodb-automation-module-2025-01-22) - **NEW**
- [FR-1149: SaveCurrentStudyToDB Automation Module](#fr-1149-savecurrentstudytodb-automation-module-2025-01-22)
- [FR-1148: GetReportedReport Automation Module](#fr-1148-getreportedreport-automation-module-2025-01-21)
- [FR-1147: Test Automation Pane](#fr-1147-test-automation-pane-2025-01-21)
- [FR-1146: Test Button in Main Window](#fr-1146-test-button-in-main-window-2025-01-21)
- [FR-1145: GetUntilReportDateTime Module](#fr-1145-getuntilreportdatetime-module-2025-01-21)
- [FR-1144: AddPreviousStudy Comparison Append](#fr-1144-addpreviousstudy-comparison-append-2025-01-20)
- [FR-1143: Global Phrase Word Limit in Completion](#fr-1143-global-phrase-word-limit-in-completion-2025-01-20)
- [FR-1141: SetClipboard Variable Support](#fr-1141-setclipboard-variable-support-2025-01-20)
- [FR-1142: TrimString Operation](#fr-1142-trimstring-operation-2025-01-20)
- [FR-1140: SetFocus Operation Retry Logic](#setfocus-operation-retry-logic-2025-01-20)
- [FR-1100..FR-1136: Foreign Text Sync & Caret Management](#foreign-text-sync--caret-management-2025-01-19)
- [FR-1025..FR-1027: Current Combination Quick Delete](#current-combination-quick-delete-2025-01-18)
- [FR-1081..FR-1093: Report Inputs Panel Layout](#report-inputs-panel-layout-2025-01-18)
- [FR-950..FR-965: Phrase-SNOMED Mapping Window](#phrase-snomed-mapping-window-2025-01-15)

### Archived Features (2024 and earlier)
- **2024-Q4**: PACS Automation, Multi-PACS Tenancy, Study Techniques ¡æ [archive/2024/Spec-2024-Q4.md](archive/2024/Spec-2024-Q4.md)
- **2025-Q1**: Phrase Highlighting, Editor Enhancements ¡æ [archive/2025-Q1/](archive/2025-Q1/)

---

## SavePreviousStudyToDB Automation Module (2025-01-22)

### FR-1150: SavePreviousStudyToDB Automation Module

**Problem**: Users need a way to save edited previous study report JSON back to the local database during automation sequences, particularly when reviewing and correcting historical reports or applying reportify/proofread operations to previous studies.

**Requirement**: Add a new automation module `SavePreviousStudyToDB` that updates the `report` column in `med.rad_report` table for the currently visible (selected) previous study report.

**Behavior**:

1. **Prerequisites Validation**
   - Requires `_studyRepo` (IRadStudyRepository) to be available
   - Requires `SelectedPreviousStudy` to be non-null (a previous study tab must be selected)
   - Requires `SelectedPreviousStudy.SelectedReport` to be non-null (a report must be selected within the tab)
   - All prerequisites must pass before database update

2. **Study Record Identification**
   - Uses `SelectedPreviousStudy.StudyDateTime` and `SelectedReport.Studyname` to identify the study
   - Calls `EnsureStudyAsync()` to retrieve study_id (study should already exist since it was loaded from DB)
   - Uses current `PatientNumber`, `PatientName`, `PatientSex` for patient context

3. **Report Update**
   - Uses `UpsertPartialReportAsync()` repository method
   - Parameters:
     - `studyId`: Study ID from EnsureStudyAsync
     - `reportDateTime`: `SelectedReport.ReportDateTime` (maintains existing report datetime key)
     - `reportJson`: `PreviousReportJson` (the visible edited JSON from the UI)
     - `isMine`: `false` (always, indicates previous/archived report, not user-authored)
   - ON CONFLICT (study_id, report_datetime): Updates existing report

4. **Previous Report JSON Source**
   - Uses `PreviousReportJson` property which contains the complete JSON structure from the visible previous study editor
   - This includes all edits made by the user in the previous report editors (findings, conclusion, metadata fields)
   - The JSON structure matches the database schema (header_and_findings, final_conclusion, etc.)

5. **is_mine Flag**
   - Always set to `false` for previous studies
   - Distinguishes from current study reports (which use `is_mine=true` in SaveCurrentStudyToDB)
   - Maintains the semantics that previous studies are historical/archived reports

6. **Error Handling**
   - If repository null: "Save previous to DB failed - repository unavailable"
   - If no previous study selected: "Save previous to DB failed - no previous study selected"
   - If no report selected in tab: "Save previous to DB failed - no report selected"
   - If EnsureStudyAsync fails: "Save previous to DB failed - study record error"
   - If UpsertPartialReportAsync fails: "Save previous to DB failed - upsert error"
   - On exception: "Save previous to DB error"
   - All errors logged to Debug output with full stack trace

7. **Success Feedback**
   - Status message: "Previous study saved to DB (report ID: {reportId})"
   - Debug logging includes study_id and report_id

**Use Cases**:

**Edit and Save Previous Report**:
```
Manual workflow:
1. Select previous study tab
2. Edit findings/conclusion in previous editors
3. Run automation sequence with SavePreviousStudyToDB module
4. Changes saved to database
```

**Batch Reportify Previous Studies**:
```
Automation sequence:
1. (User selects previous study)
2. Reportify previous report (via UI toggle)
3. SavePreviousStudyToDB (save reportified version)
```

**Proofread Historical Reports**:
```
Automation sequence:
1. (User selects previous study)
2. Apply proofreading edits manually
3. SavePreviousStudyToDB (save corrections)
```

**Multi-Step Previous Report Processing**:
```
Test sequence:
1. (User selects previous study)
2. Custom processing via automation (future modules)
3. SavePreviousStudyToDB (save processed version)
4. ShowTestMessage (confirm completion)
```

**Database Table Structure**:
```sql
CREATE TABLE med.rad_report (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    study_id bigint NOT NULL REFERENCES med.rad_study(id) ON DELETE CASCADE,
    is_mine boolean NOT NULL DEFAULT false,
    report_datetime timestamp with time zone,
    report jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT uq_rad_report__studyid_reportdt UNIQUE (study_id, report_datetime)
);
```

**Key Constraint**:
- `UNIQUE (study_id, report_datetime)` ensures only one report per study per report datetime
- Calling SavePreviousStudyToDB updates the existing report for the selected study/report_datetime combination
- Report datetime comes from the selected report (loaded from database), not user-modifiable

**JSON Structure Updated**:
```json
{
  "header_and_findings": "Previous study findings...",
  "final_conclusion": "Previous study conclusion...",
  "study_remark": "",
  "patient_remark": "",
  "chief_complaint": "",
  "patient_history": "",
  "study_techniques": "",
  "comparison": "",
  "chief_complaint_proofread": "",
  "patient_history_proofread": "",
  "study_techniques_proofread": "",
  "comparison_proofread": "",
  "findings_proofread": "",
  "conclusion_proofread": "",
  "PrevReport": {
    "header_and_findings_header_splitter_from": 0,
    "header_and_findings_header_splitter_to": 0,
    "header_and_findings_conclusion_splitter_from": 1234,
    "header_and_findings_conclusion_splitter_to": 1234,
    "final_conclusion_header_splitter_from": 0,
    "final_conclusion_header_splitter_to": 0,
    "final_conclusion_findings_splitter_from": 0,
    "final_conclusion_findings_splitter_to": 0
  }
}
```

**Implementation Details**:

1. **Module Registration**
   - Added to `AvailableModules` list in `SettingsViewModel`
   - Available in all automation panes (Test is most common use case)

2. **Module Handler**
   - Method: `RunSavePreviousStudyToDBAsync()` in `MainViewModel.Commands.cs`
   - Uses existing `_studyRepo` service for database operations
   - Accesses `SelectedPreviousStudy` and `PreviousReportJson` properties from MainViewModel
   - Validates prerequisites before database update
   - Comprehensive error handling with debug logging

3. **Repository Methods Used**
   - `EnsureStudyAsync()`: Retrieves existing study record (should already exist)
   - `UpsertPartialReportAsync()`: Updates report JSON in database

4. **Property Dependencies**
   - `SelectedPreviousStudy`: The currently visible previous study tab (set by user selection)
   - `SelectedPreviousStudy.SelectedReport`: The selected report within the tab (contains report_datetime key)
   - `PreviousReportJson`: Auto-updated by previous editor property changes
   - `PatientNumber`, `StudyName`: Used for study context validation

**Difference from SaveCurrentStudyToDB**:

| Aspect | SaveCurrentStudyToDB | SavePreviousStudyToDB |
|--------|----------------------|------------------------|
| **JSON Source** | `CurrentReportJson` | `PreviousReportJson` |
| **Study Source** | Current study (PACS selection) | Selected previous study tab |
| **Report DateTime** | `CurrentReportDateTime` (from GetUntilReportDateTime) | `SelectedReport.ReportDateTime` (from DB) |
| **is_mine Flag** | `true` (user-authored) | `false` (historical/archived) |
| **Prerequisites** | CurrentReportDateTime must be set | SelectedPreviousStudy must be set |
| **Use Case** | Save new report being dictated | Save edited historical report |

**Limitations**:
- Cannot save without selected previous study tab (must have user selection)
- Cannot save without selected report within tab (must have report_datetime key)
- No automatic report datetime generation (uses existing report_datetime from database)
- No validation of JSON content before saving
- Updates only the selected report (if multiple reports exist for same study, only selected one updates)
- No automatic refresh of previous studies list after save

**Future Enhancements**:
- Optional confirmation before overwriting existing report
- Automatic refresh of previous studies list after save
- Visual indicator when previous report has unsaved changes
- Diff view comparing database version vs. edited version
- Export/import previous report JSON
- Batch save for all previous study tabs
- Validation of JSON structure before save

**Status**: ? **Implemented** (2025-01-22)

**Cross-References**:
- Related Modules: SaveCurrentStudyToDB (similar pattern, different target)
- Repository: `RadStudyRepository.UpsertPartialReportAsync()` in `Services/RadStudyRepository.cs`
- Properties: 
  - `MainViewModel.SelectedPreviousStudy` in `ViewModels/MainViewModel.PreviousStudies.cs`
  - `MainViewModel.PreviousReportJson` in `ViewModels/MainViewModel.PreviousStudies.cs`
- Implementation: `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Commands.cs`

**Testing**:
- Configure SavePreviousStudyToDB in Test pane
- Load patient with previous studies
- Select a previous study tab
- Edit previous report fields (findings, conclusion, metadata)
- Click Test ¡æ run automation sequence with SavePreviousStudyToDB
- Verify status shows "Previous study saved to DB (report ID: X)"
- Reload patient ¡æ verify previous study shows edited content
- Verify database row updated:
  - Correct study_id
  - is_mine = false
  - report_datetime unchanged (maintains existing key)
  - report JSON matches edited content
- Run module twice ¡æ verify second run updates existing report (no duplicate)
- Run without selecting previous study ¡æ verify error "no previous study selected"
- Run without selecting report within tab ¡æ verify error "no report selected"

**Database Verification**:
```sql
SELECT 
    r.id,
    r.study_id,
    r.is_mine,
    r.report_datetime,
    r.report ->> 'header_and_findings' AS findings_preview,
    r.report ->> 'final_conclusion' AS conclusion_preview,
    r.created_at
FROM med.rad_report r
JOIN med.rad_study s ON s.id = r.study_id
WHERE r.is_mine = false
ORDER BY r.created_at DESC
LIMIT 10;
```

---

## SaveCurrentStudyToDB Automation Module (2025-01-22)

### FR-1149: SaveCurrentStudyToDB Automation Module

**Problem**: Users need a way to save the current study's report to the local database during automation sequences, particularly when capturing reports from PACS for archival or reference purposes.

**Requirement**: Add a new automation module `SaveCurrentStudyToDB` that inserts a new row in `med.rad_report` table with:
- `study_id`: ID of current study (from patient/study metadata)
- `is_mine`: `true` (indicates user-created report)
- `report_datetime`: The report datetime saved by `GetUntilReportDateTime` module (stored in `CurrentReportDateTime` property)
- `report`: The complete JSON of the current report (from `CurrentReportJson` property)

**Behavior**:

1. **Prerequisites Validation**
   - Requires `_studyRepo` (IRadStudyRepository) to be available
   - Requires `PatientNumber`, `StudyName`, `StudyDateTime` to be non-empty
   - Requires `CurrentReportDateTime` property to be set (via GetUntilReportDateTime module)
   - All prerequisites must pass before database insertion

2. **Study Record Ensuring**
   - Calls `EnsureStudyAsync()` to create/retrieve study record
   - Creates patient and studyname records if they don't exist
   - Returns `study_id` for report insertion

3. **Report Insertion**
   - Uses `UpsertPartialReportAsync()` repository method
   - Parameters:
     - `studyId`: Study ID from EnsureStudyAsync
     - `reportDateTime`: CurrentReportDateTime.Value (from GetUntilReportDateTime)
     - `reportJson`: CurrentReportJson (contains all report fields)
     - `isMine`: `true` (always, indicates user-authored report)
   - ON CONFLICT: Updates existing report if one exists with same (study_id, report_datetime)

4. **CurrentReportDateTime Source**
   - Must be set by `GetUntilReportDateTime` module prior to SaveCurrentStudyToDB
   - Stores the report datetime from PACS (when report was created/modified)
   - Not the same as `StudyDateTime` (which is when study was performed)

5. **JSON Report Structure**
   - Saves complete `CurrentReportJson` which includes:
     - `findings` (header_and_findings)
     - `conclusion` (final_conclusion)
     - `study_remark`
     - `patient_remark`
     - `report_radiologist`
     - `chief_complaint`
     - `patient_history`
     - `study_techniques`
     - `comparison`
     - All proofread fields

6. **Error Handling**
   - If repository null: "Save to DB failed - repository unavailable"
   - If missing patient/study metadata: "Save to DB failed - missing study context"
   - If CurrentReportDateTime null: "Save to DB failed - report datetime not set (run GetUntilReportDateTime first)"
   - If invalid StudyDateTime format: "Save to DB failed - invalid study datetime"
   - If EnsureStudyAsync fails: "Save to DB failed - study record error"
   - If UpsertPartialReportAsync fails: "Save to DB failed - upsert error"
   - On exception: "Save to DB error"
   - All errors logged to Debug output with full stack trace

7. **Success Feedback**
   - Status message: "Current study saved to DB (report ID: {reportId})"
   - Debug logging includes study_id and report_id

**Use Cases**:

**Archive PACS Report**:
```
Automation sequence:
1. GetUntilReportDateTime (capture report datetime)
2. GetReportedReport (fetch report text)
3. SaveCurrentStudyToDB (save to local database)
```

**Create Report Snapshot**:
```
Automation sequence:
1. GetStudyRemark (fetch metadata)
2. GetPatientRemark (fetch metadata)
3. GetUntilReportDateTime (capture report datetime)
4. GetReportedReport (fetch report content)
5. SaveCurrentStudyToDB (persist snapshot)
```

**Workflow Integration**:
```
New Study sequence:
1. NewStudy (clear current report)
2. LockStudy (lock patient context)
3. GetUntilReportDateTime (get report datetime)
4. GetReportedReport (fetch PACS report)
5. SaveCurrentStudyToDB (save initial version)
6. (User edits report)
7. SaveCurrentStudyToDB (save edited version with same datetime ¡æ updates)
```

**Database Table Structure**:
```sql
CREATE TABLE med.rad_report (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    study_id bigint NOT NULL REFERENCES med.rad_study(id) ON DELETE CASCADE,
    is_mine boolean NOT NULL DEFAULT false,
    report_datetime timestamp with time zone,
    report jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT uq_rad_report__studyid_reportdt UNIQUE (study_id, report_datetime)
);
```

**Key Constraint**:
- `UNIQUE (study_id, report_datetime)` ensures only one report per study per report datetime
- Calling SaveCurrentStudyToDB multiple times with same CurrentReportDateTime updates existing report
- Changing CurrentReportDateTime and calling SaveCurrentStudyToDB creates new report row

**JSON Example**:
```json
{
  "findings": "Normal chest CT...",
  "conclusion": "No acute abnormality",
  "study_remark": "Follow-up study",
  "patient_remark": "History of COPD",
  "report_radiologist": "Dr. Smith",
  "chief_complaint": "Chest pain",
  "patient_history": "- Smoker 20 pack-years\n- COPD",
  "study_techniques": "axial T1, T2",
  "comparison": "CT 2024-01-15",
  "chief_complaint_proofread": "",
  "patient_history_proofread": "",
  "study_techniques_proofread": "",
  "comparison_proofread": "",
  "findings_proofread": "",
  "conclusion_proofread": ""
}
```

**Implementation Details**:

1. **Module Registration**
   - Added to `AvailableModules` list in `SettingsViewModel`
   - Available in all automation panes (NewStudy, AddStudy, Test, SendReport, etc.)

2. **Module Handler**
   - Method: `RunSaveCurrentStudyToDBAsync()` in `MainViewModel.Commands.cs`
   - Uses existing `_studyRepo` service for database operations
   - Validates prerequisites before database insertion
   - Comprehensive error handling with debug logging

3. **Repository Methods Used**
   - `EnsureStudyAsync()`: Ensures patient and study records exist
   - `UpsertPartialReportAsync()`: Inserts or updates report record

4. **Property Dependencies**
   - `CurrentReportDateTime`: Must be set by GetUntilReportDateTime module
   - `CurrentReportJson`: Auto-updated by editor property changes
   - `PatientNumber`, `StudyName`, `StudyDateTime`: Set by PACS selection fetch

**Execution Sequence Requirements**:

**Required Order**:
```
1. GetUntilReportDateTime (sets CurrentReportDateTime)
2. SaveCurrentStudyToDB (uses CurrentReportDateTime)
```

**Incorrect Order** (will fail):
```
1. SaveCurrentStudyToDB (CurrentReportDateTime is null) ¡æ ERROR
```

**Complete Example**:
```
Test Sequence:
1. GetStudyRemark (optional, for metadata)
2. GetPatientRemark (optional, for metadata)
3. GetUntilReportDateTime (required, sets report datetime)
4. GetReportedReport (optional, fetches PACS report text)
5. SaveCurrentStudyToDB (saves to database)
6. ShowTestMessage (confirms completion)
```

**Limitations**:
- Cannot save without valid CurrentReportDateTime (must run GetUntilReportDateTime first)
- Cannot save without patient/study context (must have active study)
- No automatic report datetime generation (must come from PACS)
- No validation of JSON content before saving
- No duplicate detection across different report datetimes
- No automatic cleanup of old report versions

**Future Enhancements**:
- Optional confirmation before overwriting existing report
- Automatic report datetime generation if not set
- Report version history UI
- Diff view comparing multiple report versions
- Export/import report JSON
- Configurable `is_mine` flag (currently always true)
- Bulk save for multiple studies

**Status**: ? **Implemented** (2025-01-22)

**Cross-References**:
- Related Modules: GetUntilReportDateTime (prerequisite), GetReportedReport (common preceding module)
- Repository: `RadStudyRepository.UpsertPartialReportAsync()` in `Services/RadStudyRepository.cs`
- Property: `MainViewModel.CurrentReportDateTime` in `ViewModels/MainViewModel.CurrentStudy.cs`
- JSON: `MainViewModel.CurrentReportJson` in `ViewModels/MainViewModel.Editor.cs`
- Implementation: `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Commands.cs`

**Testing**:
- Configure SaveCurrentStudyToDB in Test pane
- Click Test without GetUntilReportDateTime first ¡æ error "report datetime not set"
- Configure sequence: GetUntilReportDateTime ¡æ SaveCurrentStudyToDB ¡æ ShowTestMessage
- Click Test with no study selected ¡æ error "missing study context"
- Click Test with study selected ¡æ success, check database for new report row
- Verify report row has:
  - Correct study_id
  - is_mine = true
  - report_datetime from GetUntilReportDateTime
  - report JSON contains all fields
- Run sequence twice ¡æ verify second run updates existing report (no duplicate)
- Change CurrentReportDateTime and run again ¡æ verify new report row created

**Database Verification**:
```sql
SELECT 
    r.id,
    r.study_id,
    r.is_mine,
    r.report_datetime,
    r.report ->> 'findings' AS findings_preview,
    r.report ->> 'conclusion' AS conclusion_preview,
    r.report ->> 'report_radiologist' AS radiologist,
    r.created_at
FROM med.rad_report r
WHERE r.is_mine = true
ORDER BY r.created_at DESC
LIMIT 10;
```

---

## GetReportedReport Automation Module (2025-01-21)

### FR-1148: GetReportedReport Automation Module

**Problem**: Users need a way to fetch the currently reported/finalized report text from PACS (findings and conclusion) and save it to the application's current report JSON structure.

**Requirement**: Add a new automation module `GetReportedReport` that executes `GetCurrentFindings`, `GetCurrentConclusion`, and `GetSelectedRadiologistFromSearchResultsList` from PACS and saves the results to the current JSON report's `header_and_findings`, `final_conclusion`, and `report_radiologist` fields.

**Behavior**:

1. **PACS Method Execution**
   - Calls `GetCurrentFindingsAsync()` from PacsService
   - Calls `GetCurrentConclusionAsync()` from PacsService
   - Both methods execute concurrently using `Task.WhenAll()`
   - After findings and conclusion are fetched, calls `GetSelectedRadiologistFromSearchResultsAsync()` to get the radiologist name

2. **Result Handling**
   - Null results converted to empty string
   - Empty results are valid (no error thrown)
   - Results logged to debug output with character counts
   - Radiologist name logged separately

3. **JSON Report Update**
   - Sets `FindingsText` property with GetCurrentFindings result
   - Sets `ConclusionText` property with GetCurrentConclusion result
   - Sets `ReportRadiologist` property with GetSelectedRadiologistFromSearchResultsList result
   - Property setters automatically trigger `UpdateCurrentReportJson()`
   - JSON report structure updated with:
     - `Report.header_and_findings` ¡ç findings text
     - `Report.final_conclusion` ¡ç conclusion text
     - `Report.report_radiologist` ¡ç radiologist name

4. **Error Handling**
   - Exceptions caught and logged to debug output
   - Status message: "Get reported report acquisition error"
   - Does NOT throw exception (allows automation sequence to continue)
   - Partial success allowed (if one getter fails, the other may succeed)

5. **Status Messages**
   - On success: "Reported report acquired: {total_characters} total characters, radiologist: {radiologist_name_or_none}"
   - On error: "Get reported report acquisition error"
   - Debug logging for findings/conclusion/radiologist lengths

**Use Cases**:

**Import Existing Report with Radiologist**:
```
Automation sequence:
1. ResultsListSetFocus (select study in PACS)
2. OpenStudy (open study to show report text)
3. Delay (wait for UI to load)
4. GetReportedReport (fetch and save report text + radiologist)
```

**Comparison Workflow**:
```
Automation sequence:
1. GetReportedReport (save original PACS report + radiologist)
2. User edits findings/conclusion
3. Compare CurrentReportJson before/after editing
```

**Report Migration**:
```
Automation sequence:
1. OpenStudy (open old study)
2. GetReportedReport (fetch old report + radiologist)
3. SendReport (save to new format)
```

**JSON Structure After Execution**:
```json
{
  "findings": "[findings text from PACS]",
  "conclusion": "[conclusion text from PACS]",
  "report_radiologist": "[radiologist name from PACS]",
  "study_remark": "",
  "patient_remark": "",
  "chief_complaint": "",
  "patient_history": "",
  "study_techniques": "",
  "comparison": ""
}
```

**Database Changes (2025-01-21)**:
- Radiologist is now saved in JSON `report_radiologist` field instead of database `created_by` column
- Database columns `is_created` and `created_by` can be dropped after deployment
- Migration script: `apps/Wysg.Musm.Radium/docs/db/migrations/20250121_drop_rad_report_columns.sql`
- This aligns with the pattern of storing all report metadata in JSON for better flexibility

**Deployment Steps**:
1. Deploy updated application code that saves/reads radiologist from JSON
2. Verify new reports contain `report_radiologist` in JSON
3. Verify previous studies display radiologist correctly
4. Run migration SQL to drop obsolete columns:
   ```sql
   ALTER TABLE med.rad_report 
     DROP COLUMN IF EXISTS is_created,
     DROP COLUMN IF EXISTS created_by;
   ```

**Note**: The module saves to `findings`, `conclusion`, and `report_radiologist` fields which map to:
- `findings` ¡æ `Report.header_and_findings` (via `FindingsText` property)
- `conclusion` ¡æ `Report.final_conclusion` (via `ConclusionText` property)
- `report_radiologist` ¡æ `Report.report_radiologist` (via `ReportRadiologist` property)

**Implementation Details**:

1. **Module Registration**
   - Added to `AvailableModules` list in `SettingsViewModel`
   - Available in all automation panes (NewStudy, AddStudy, Test, SendReport, etc.)

2. **Module Handler**
   - Method: `RunGetReportedReportAsync()` in `MainViewModel.Commands.cs`
   - Uses existing `_pacs` service for PACS connectivity
   - Updates `FindingsText`, `ConclusionText`, and `ReportRadiologist` properties directly

3. **Property Synchronization**
   - `FindingsText` property setter calls `UpdateCurrentReportJson()`
   - `ConclusionText` property setter calls `UpdateCurrentReportJson()`
   - `ReportRadiologist` property setter calls `UpdateCurrentReportJson()`
   - JSON sync happens automatically via existing property change notification

4. **New Property**
   - `ReportRadiologist` property added to `MainViewModel.Editor.cs`
   - Stores radiologist name from GetSelectedRadiologistFromSearchResultsList
   - Synchronized with JSON `report_radiologist` field
   - Round-trippable through JSON serialization/deserialization

**Error Handling Strategy**:

**Non-Blocking Design**:
```csharp
catch (Exception ex)
{
    Debug.WriteLine($"[Automation][GetReportedReport] EXCEPTION: {ex.GetType().Name} - {ex.Message}");
    SetStatus("Get reported report acquisition error", true);
    // Do not throw - allow sequence to continue
}
```

**Rationale**: Unlike GetUntilReportDateTime (which must abort on failure), GetReportedReport is a data capture operation that should allow the sequence to continue even if PACS report text is unavailable.

**Comparison with Similar Modules**:

| Module | Purpose | Abort on Failure? |
|--------|---------|-------------------|
| GetStudyRemark | Fetch study remark | No |
| GetPatientRemark | Fetch patient remark | No |
| **GetReportedReport** | Fetch findings + conclusion + radiologist | **No** |
| GetUntilReportDateTime | Wait for valid report datetime | **Yes** |

**Limitations**:
- Requires GetCurrentFindings, GetCurrentConclusion, and GetSelectedRadiologistFromSearchResultsList PACS methods to be configured
- No validation of report text format
- No automatic reportify/dereportify of fetched text
- No comparison with existing report text
- Overwrites existing FindingsText, ConclusionText, and ReportRadiologist without confirmation

**Future Enhancements**:
- Optional confirmation before overwriting existing report text
- Automatic dereportify of fetched text
- Report text comparison UI (diff view)
- Support for alternate getters (GetCurrentFindings2, GetCurrentConclusion2)
- Configurable field mapping (which JSON fields to update)

**Status**: ? **Implemented** (2025-01-21 - Updated 2025-01-21 to include radiologist)

**Cross-References**:
- Related Modules: GetStudyRemark, GetPatientRemark (similar data capture pattern)
- PACS Methods: `GetCurrentFindingsAsync()`, `GetCurrentConclusionAsync()`, `GetSelectedRadiologistFromSearchResultsAsync()` in `PacsService.cs`
- Property Sync: `MainViewModel.Editor.cs` (`FindingsText`, `ConclusionText`, `ReportRadiologist`)
- JSON Structure: `MainViewModel.Editor.UpdateCurrentReportJson()`
- Implementation: `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Commands.cs`

**Testing**:
- Configure GetReportedReport in Test automation pane
- Click Test button with PACS closed ¡æ should show error, sequence continues
- Click Test button with PACS open, no study selected ¡æ should return empty text
- Click Test button with study open ¡æ should populate FindingsText, ConclusionText, and ReportRadiologist
- Verify CurrentReportJson contains `findings`, `conclusion`, and `report_radiologist` fields
- Verify JSON updates trigger UI refresh in Findings/Conclusion editors
- Verify sequence continues to next module even if GetReportedReport fails
- Verify status shows total character count and radiologist name on success
- Verify radiologist field is empty if not available in PACS

**Example Automation Sequences**:

**Quick Import**:
```
Test Sequence:
1. GetReportedReport
2. ShowTestMessage (confirms completion)
```

**Full Import Workflow**:
```
New Study Sequence:
1. NewStudy (clear current report)
2. LockStudy (lock patient context)
3. GetStudyRemark (fetch study metadata)
4. GetPatientRemark (fetch patient metadata)
5. GetReportedReport (fetch report text + radiologist)
6. Reportify (apply formatting)
```

---

## Test Automation Pane (2025-01-21)

### FR-1147: Test Automation Pane in Settings

**Problem**: Users need a way to configure reusable test automation sequences that can be invoked from the main window's "Test" button, separate from NewStudy or other workflows.

**Requirement**: Add a new "Test" automation pane in Settings ¡æ Automation tab where users can configure automation module sequences that execute when the "Test" button is pressed in the main window.

**Behavior**:

1. **UI Location**
   - Settings window ¡æ Automation tab
   - New pane below "Shortcut: Send Report (reportified)" pane
   - Label: "Test"
   - Listbox name: `lstTest`

2. **Module Configuration**
   - Drag modules from "Available Modules" to "Test" pane
   - Modules execute sequentially when "Test" button pressed
   - All automation modules available (including GetUntilReportDateTime, ShowTestMessage, etc.)
   - Supports duplicates (same module can be added multiple times)

3. **Storage**
   - Persisted per PACS profile in `automation.json`
   - Path: `%AppData%/Wysg.Musm.Radium/Pacs/{pacs_key}/automation.json`
   - Property: `TestSequence` (comma-separated module list)

4. **Execution Flow**
   - User clicks "Test" button in main window
   - Reads `TestSequence` from current PACS automation.json
   - Executes modules sequentially via `RunModulesSequentially()`
   - Aborts on module failure (same as other automation sequences)
   - Shows status messages during execution

5. **Empty Sequence Behavior**
   - If no modules configured: shows "No Test sequence configured" error
   - Prevents execution if sequence is empty

**Use Cases**:

**PACS Integration Testing**:
```
Test sequence:
1. ShowTestMessage (verify UI responsiveness)
2. GetUntilReportDateTime (verify PACS connectivity)
3. ShowTestMessage (confirm data acquisition)
```

**Workflow Validation**:
```
Test sequence:
1. GetStudyRemark (fetch study remark)
2. GetPatientRemark (fetch patient remark)
3. ShowTestMessage (verify data capture)
```

**Custom Development Testing**:
```
Test sequence:
1. TestInvoke (invoke custom UI element)
2. MouseClick1 (test mouse automation)
3. Delay (wait for UI update)
4. ShowTestMessage (confirm completion)
```

**Status**: ? **Implemented** (2025-01-21)

**Cross-References**:
- Related Feature: FR-1146 (Test Button in Main Window)
- Related Feature: FR-1145 (GetUntilReportDateTime Module)
- Implementation: `apps\Wysg.Musm.Radium\ViewModels\SettingsViewModel.PacsProfiles.cs`
- UI: `apps\Wysg.Musm.Radium\Views\SettingsTabs\AutomationSettingsTab.xaml`

---

## Test Button in Main Window (2025-01-21)

### FR-1146: Rename "Test NewStudy Proc" to "Test"

**Problem**: The button label "Test NewStudy Proc" is overly specific and doesn't reflect the button's new purpose as a general-purpose test automation trigger.

**Requirement**: Rename the button from "Test NewStudy Proc" to "Test" and change its behavior to execute the Test automation sequence configured in Settings ¡æ Automation.

**Behavior**:

1. **Button Label**
   - Old: "Test NewStudy Proc"
   - New: "Test"

2. **Command Binding**
   - Still bound to `TestNewStudyProcedureCommand`
   - Command handler now runs Test automation sequence instead of NewStudy procedure directly

3. **Execution Flow**
   - Reads `TestSequence` from current PACS automation.json
   - Validates sequence is not empty
   - Executes modules sequentially
   - Shows "No Test sequence configured" if empty

4. **Location**
   - Main window ¡æ Current Report Editor Panel ¡æ Header toolbar
   - Between "Extract Phrases" and "Proofread" buttons

**Use Cases**:

**Quick PACS Connectivity Test**:
```
User clicks "Test" ¡æ runs configured test sequence (e.g., GetUntilReportDateTime) ¡æ verifies PACS is responsive
```

**Automation Development**:
```
Developer configures test sequence in Settings ¡æ clicks "Test" button repeatedly ¡æ validates module behavior without full workflow execution
```

**Status**: ? **Implemented** (2025-01-21)

**Cross-References**:
- Related Feature: FR-1147 (Test Automation Pane)
- Implementation: `apps\Wysg.Musm.Radium\Controls\CurrentReportEditorPanel.xaml`
- Command Handler: `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Commands.cs` (`OnRunTestAutomation`)

---

## GetUntilReportDateTime Module (2025-01-21)

### FR-1145: GetUntilReportDateTime Automation Module

**Problem**: The `GetSelectedReportDateTimeFromSearchResults` PACS method sometimes returns non-DateTime formatted strings during the transition period when a study is being opened in PACS. This causes automation to fail when DateTime parsing is expected.

**Requirement**: Add a new automation module `GetUntilReportDateTime` that repeatedly executes `GetSelectedReportDateTimeFromSearchResults` PACS method until the result is in valid DateTime format, with a maximum of 30 retries. If all retries fail, the procedure (automation pane) must abort and cannot proceed.

**Behavior**:

1. **Retry Logic**
   - Maximum attempts: 30
   - Delay between attempts: 200ms
   - Total maximum wait time: 6 seconds (30 ¡¿ 200ms)

2. **Success Condition**
   - Result is not null/whitespace
   - Result parses successfully to DateTime via `DateTime.TryParse()`
   - Example valid formats: "2024-01-21 14:30:00", "2024-01-21", "1/21/2024 2:30 PM"

3. **Failure Condition**
   - After 30 attempts, result still invalid or unparseable
   - PACS method throws exception
   - PACS method returns null/empty on all attempts

4. **Abort Behavior**
   - On failure: throws `InvalidOperationException` with message "GetUntilReportDateTime failed to acquire valid DateTime after 30 retries"
   - Exception caught by `RunModulesSequentially()`
   - Entire automation sequence aborts (no subsequent modules executed)
   - Status message: "Report DateTime acquisition failed - aborting procedure"

5. **Status Messages**
   - On success: "Report DateTime acquired: {result}"
   - On failure: "Report DateTime acquisition failed - aborting procedure"
   - Debug logging for each attempt with result value

**Use Cases**:

**Reliable Study Opening Workflow**:
```
Automation sequence:
1. ResultsListSetFocus (focus search results)
2. GetUntilReportDateTime (wait for valid report datetime - ensures study is fully loaded)
3. OpenStudy (safe to open now that datetime is valid)
```

**Retry Scenarios**:
```
Attempt 1: Result = "" (empty) ¡æ wait 200ms
Attempt 2: Result = "Loading..." ¡æ wait 200ms
Attempt 3: Result = "2024-01-21 14:30:00" (valid DateTime) ¡æ SUCCESS
```

**Failure Scenario**:
```
Attempts 1-30: All return invalid or empty values
Result: Module throws exception ¡æ entire automation sequence aborts
Status: "Report DateTime acquisition failed - aborting procedure"
Subsequent modules (e.g., OpenStudy) are not executed
```

**Implementation Details**:

1. **Module Registration**
   - Added to `AvailableModules` list in `SettingsViewModel`
   - Available in all automation panes (NewStudy, AddStudy, Test, etc.)

2. **Module Handler**
   - Method: `RunGetUntilReportDateTimeAsync()` in `MainViewModel.Commands.cs`
   - Uses existing `_pacs.GetSelectedReportDateTimeFromSearchResultsAsync()` method
   - Implements retry loop with `Task.Delay()` between attempts

3. **Abort Mechanism**
   - `RunModulesSequentially()` catches exception and aborts entire sequence
   - Status updated with error message
   - Changed behavior: was "Module failed", now "Module failed - procedure aborted"

**Error Handling**:

**Exception Propagation**:
```csharp
catch (Exception ex)
{
    Debug.WriteLine($"[Automation][GetUntilReportDateTime] EXCEPTION: {ex.GetType().Name} - {ex.Message}");
    SetStatus("Report DateTime acquisition error - aborting", true);
    throw; // Re-throw to abort the procedure sequence
}
```

**Sequence Abort**:
```csharp
catch (Exception ex)
{
    Debug.WriteLine("[Automation] Module '" + m + "' error: " + ex.Message);
    SetStatus($"Module '{m}' failed - procedure aborted", true);
    return; // ABORT entire sequence
}
```

**Limitations**:
- Fixed retry count (30 attempts, cannot be configured)
- Fixed delay (200ms between attempts, cannot be configured)
- No progress indication during retries (only shows final result)
- Assumes `GetSelectedReportDateTimeFromSearchResults` PACS method is configured

**Status**: ? **Implemented** (2025-01-21)

**Cross-References**:
- Related Feature: FR-1147 (Test Automation Pane - common use case)
- Related Feature: FR-1146 (Test Button - convenient way to test this module)
- PACS Method: `GetSelectedReportDateTimeFromSearchResultsAsync()` in `PacsService.cs`
- Implementation: `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Commands.cs`

**Testing**:
- Configure GetUntilReportDateTime in Test pane
- Click Test button with PACS closed ¡æ should fail after 30 retries
- Click Test button with PACS open, study not selected ¡æ should retry until selection changes
- Click Test button with study selected ¡æ should succeed on first or early attempt
- Verify subsequent modules do not execute when GetUntilReportDateTime fails
- Verify debug output shows each attempt and result value

**Future Enhancements**:
- Configurable retry count (e.g., MaxRetries property)
- Configurable delay interval (e.g., DelayMs property)
- Progress indication in status bar (e.g., "Attempt 5/30...")
- Timeout based on wall-clock time instead of attempt count
- Support for other format validation (e.g., numeric ID, specific regex pattern)

---

## AddPreviousStudy Comparison Append (2025-01-20)

### FR-1144: Append Simplified Study String to Comparison Field

**Problem**: When AddPreviousStudy automation module successfully adds a previous study, there is no automatic record of which previous study was added in the current report's Comparison field. Users had to manually type comparison information.

**Requirement**: After AddPreviousStudy module successfully adds a previous study, automatically append a simplified study string (format: "MODALITY YYYY-MM-DD") to the current report's `Report.comparison` JSON field.

**Behavior**:

1. **Trigger Condition**
   - Only executes after successful AddPreviousStudy completion
   - Runs before status message "Previous study added"
   - Does NOT execute if:
     - Patient mismatch occurs
     - Study data is invalid (missing studyname/datetime)
     - Study matches current study
     - Any error during add operation

2. **Simplified String Format**
   - Format: `{MODALITY} {YYYY-MM-DD}`
   - MODALITY: Extracted from studyname using `ExtractModality()` helper
   - Date: Study datetime formatted as ISO date (YYYY-MM-DD)
   - Examples:
     - "CT 2024-01-15"
     - "MR 2023-12-20"
     - "XR 2024-01-18"
     - "UNK 2024-01-10" (if modality cannot be determined)

3. **Modality Extraction**
   - Uses existing `ExtractModality()` method from `MainViewModel.ReportifyHelpers.cs`
   - Recognizes common modalities: CT, MRI/MR, XR, CR, DX, US, PET-CT/PETCT/PET, MAMMO/MMG, DXA, NM
   - Returns "UNK" if no modality found in studyname

4. **Append Logic**
   - If Comparison field is empty or whitespace: Set to simplified string
   - If Comparison field has content: Append with ", " separator
   - Examples:
     - Empty ¡æ "CT 2024-01-15"
     - "Prior CT" ¡æ "Prior CT, CT 2024-01-15"
     - "CT 2024-01-10" ¡æ "CT 2024-01-10, MR 2024-01-15"

5. **Comparison Property Update**
   - Sets `Comparison` property directly (triggers JSON sync)
   - Updates `Report.comparison` field in CurrentReportJson
   - Triggers header re-formatting (Comparison is part of formatted header)
   - Preserved across reportify/dereportify cycles

6. **Error Handling**
   - Wrapped in try-catch block
   - Errors logged to Debug output with "[AddPreviousStudyModule] Comparison append error: {message}"
   - Does NOT fail the entire AddPreviousStudy operation
   - Silent failure (no user-visible error message)

7. **Multiple Additions**
   - Running AddPreviousStudy multiple times appends multiple entries
   - Example after 3 previous studies added: "CT 2024-01-10, MR 2024-01-05, XR 2023-12-20"
   - Order reflects addition sequence (newest additions at end)

**Use Cases**:

**Single Previous Study**:
```
Before: Comparison = ""
Run: AddPreviousStudy (CT Chest, 2024-01-15)
After: Comparison = "CT 2024-01-15"
```

**Multiple Previous Studies**:
```
Before: Comparison = ""
Run: AddPreviousStudy (CT Chest, 2024-01-15)
After: Comparison = "CT 2024-01-15"

Run: AddPreviousStudy (MRI Brain, 2023-12-20)
After: Comparison = "CT 2024-01-15, MR 2023-12-20"
```

**Existing Comparison Text**:
```
Before: Comparison = "Prior study"
Run: AddPreviousStudy (XR Chest, 2024-01-18)
After: Comparison = "Prior study, XR 2024-01-18"
```

**Modality Extraction Examples**:
```
Studyname: "CT Chest without contrast" ¡æ "CT"
Studyname: "MRI Brain with Gd" ¡æ "MR"
Studyname: "CTA Neck" ¡æ "CT"
Studyname: "PET-CT Whole Body" ¡æ "PETCT"
Studyname: "Mammography bilateral" ¡æ "MAMMO"
Studyname: "Unknown study" ¡æ "UNK"
```

**JSON Synchronization**:
```json
{
  "findings": "...",
  "conclusion": "...",
  "comparison": "CT 2024-01-15, MR 2023-12-20",
  ...
}
```

**Limitations**:
- Modality extraction limited to recognized abbreviations (CT, MRI, XR, US, etc.)
- Unknown modalities show as "UNK"
- No duplicate detection (adding same study twice creates duplicate entries)
- No automatic sorting by date
- Manual comparison text preserved but not parsed
- Appending only (no automatic removal or replacement)

**Future Enhancements**:
- Configurable format template (e.g., "MODALITY on DATE")
- Duplicate detection before append
- Sort comparison entries by date
- Option to prepend instead of append
- Parse and preserve custom comparison notes
- Configurable separator (currently hardcoded to ", ")

**Status**: ? **Implemented** (2025-01-20)

**Cross-References**:
- Implementation: `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Commands.cs` (`RunAddPreviousStudyModuleAsync`)
- Related Feature: FR-511 (Add Previous Study Automation Module)
- Helper Method: `MainViewModel.ReportifyHelpers.ExtractModality()`
- Property: `MainViewModel.Comparison` (triggers JSON update and header refresh)

**Testing**:
- Verify comparison field empty ¡æ single entry added
- Verify comparison field with content ¡æ comma-separated append
- Verify modality extraction for common types (CT, MRI, XR, US, etc.)
- Verify unknown modality shows "UNK"
- Verify date formatting as YYYY-MM-DD
- Verify JSON `Report.comparison` updates
- Verify header displays updated comparison line
- Verify multiple AddPreviousStudy runs append correctly
- Verify error during comparison append does not fail AddPreviousStudy
- Verify no append when AddPreviousStudy aborts early (patient mismatch, invalid data)

---

## Global Phrase Word Limit in Completion (2025-01-20)

### FR-1143: Limit Global Phrases in Completion Window to 3 Words or Less

**Problem**: The completion window for editorFindings shows all global phrases, including very long multi-word medical terms. This clutters the completion list and makes it harder to find relevant short phrases.

**Requirement**: Filter global phrases (account_id IS NULL) shown in the completion window to only those with 3 words or less. This filter should NOT apply to:
- Account-specific (local) phrases
- Hotkeys
- Snippets

**Rationale**:
- Global phrases are shared across all accounts and often contain long medical terminology
- Most commonly used phrases for quick completion are short (1-3 words)
- Longer phrases are better accessed through other mechanisms (search, browser)
- Keeps completion window focused on frequently-used short terms

**Implementation**:

1. **Word Counting Logic**
   - Words counted by splitting on whitespace (space, tab, newline, carriage return)
   - Empty strings count as 0 words
   - Implemented as `CountWords()` helper method in `PhraseService`

2. **Filtering Location**
   - Applied in `GetGlobalPhrasesByPrefixAsync()` method
   - Filter: `CountWords(r.Text) <= 3`
   - Combined with existing prefix match and active status filters

3. **Combined List Behavior**
   - `GetCombinedPhrasesByPrefixAsync()` uses filtered global phrases
   - Account-specific phrases retrieved via `GetPhrasesByPrefixAccountAsync()` (no word limit)
   - Final list contains: filtered globals + unfiltered local phrases

4. **Scope of Filtering**
   - ? Applies to: Global phrases in completion window
   - ? Does NOT apply to: Account phrases, hotkeys, snippets
   - ? Does NOT apply to: Global phrase management UI, SNOMED browser

**Examples**:

**Filtered Out (> 3 words)**:
- "chronic obstructive pulmonary disease" (4 words)
- "diffuse large B-cell lymphoma" (4 words)
- "acute myocardial infarction with ST elevation" (6 words)

**Kept (<= 3 words)**:
- "normal" (1 word)
- "no acute abnormality" (3 words)
- "liver parenchyma" (2 words)

**Use Cases**:

**Quick Completion**:
```
User types "no" ?? completion shows:
- "no" (global, 1 word)
- "no acute abnormality" (global, 3 words)
- "no focal lesion" (global, 3 words)
- "no significant change from previous study" (account, 6 words - allowed because it's local)
```

**Long Phrases Still Accessible**:
```
User can still access "chronic obstructive pulmonary disease" via:
- SNOMED browser
- Global phrases management window
- Typing the full phrase manually
- Copy from previous report
```

**Status**: ? **Implemented** (2025-01-20)

**Cross-References**:
- Implementation: `apps\Wysg.Musm.Radium\Services\PhraseService.cs`
- Related Features: [SNOMED Browser](SNOMED_BROWSER_FEATURE_SUMMARY.md), Global Phrases Management

**Testing**:
- Verify completion window shows only short global phrases
- Verify local (account-specific) phrases show regardless of length
- Verify hotkeys and snippets unaffected
- Verify global phrase management windows show all phrases (no filter)

**Future Enhancements**:
- Make word limit configurable (currently hardcoded to 3)
- Add user preference for word limit
- Add character limit as alternative filter option
- Consider frequency-based filtering instead of word count

---

## SetClipboard Variable Support (2025-01-20)

### FR-1141: SetClipboard Operation Variable Support

**Problem**: SetClipboard operation forced Arg1 type to "String" on operation selection, preventing users from passing variable references from previous operations to clipboard.

**Requirement**: SetClipboard operation must support both String (literal text) and Var (variable reference) argument types.

**Behavior**:

1. **Arg1 Type Selection**
   - User can select either String or Var type for Arg1
   - Type selection persists when operation is selected
   - `OnProcOpChanged` handler only enables/disables arguments, does not force type

2. **String Type (Literal)**
   - User enters literal text in Arg1 Value field
   - Text is copied directly to Windows clipboard
   - Preview shows: "(clipboard set, N chars)" where N is character count

3. **Var Type (Variable Reference)**
   - User selects variable name (e.g., var1, var2) from Arg1 Value dropdown
   - Variable value from previous operation is resolved and copied to clipboard
   - Preview shows: "(clipboard set, N chars)" based on resolved variable content

4. **Error Handling**
   - Null text: "(null)"
   - Clipboard access error: "(error: {message})"

**Use Cases**:
- Copy text extracted from UI element: `GetText(element) ¡æ var1; SetClipboard(var1)`
- Copy field value from selected row: `GetValueFromSelection(list, "ID") ¡æ var1; SetClipboard(var1)`
- Copy static text: `SetClipboard("Fixed text to copy")`

**Status**: ? **Implemented** (2025-01-20)

**Cross-References**:
- Implementation: [Plan-active.md - SetClipboard Fix](Plan-active.md#change-log-addition-2025-01-20--setclipboard--trimstring-operation-fixes)
- Related Operations: SimulatePaste (uses clipboard content), GetText (common source for clipboard)

---

## TrimString Operation (2025-01-20)

### FR-1142: TrimString Operation for String Cleaning

**Problem**: No operation existed to remove specific substrings from the start or end of strings. Users needed to trim labels, prefixes, or unwanted text patterns from UI-extracted content.

**Requirement**: Add TrimString operation that removes all occurrences of a specified substring from the start and/or end of a source string.

**Operation Signature**:
- **Operation Name**: TrimString
- **Arg1**: Source string (Type: String or Var)
- **Arg2**: String to trim away from start/end (Type: String or Var)
- **Arg3**: Disabled
- **Output**: Cleaned string with Arg2 removed from start and end of Arg1

**Behavior**:

1. **Trim from Start and End Only**
   - Arg1 (source): " I am me "
   - Arg2 (trim): "I"
   - Result: " am me " (only removed "I" from start)
   - Uses repeated `StartsWith()` and `EndsWith()` checks

2. **Remove from Both Ends**
   - Arg1 (source): "aba"
   - Arg2 (trim): "a"
   - Result: "b" (removed "a" from both start and end)

3. **Empty Trim String**
   - If Arg2 is null or empty, returns Arg1 unchanged
   - Preview: Shows source string as-is

4. **Variable References**
   - Both Arg1 and Arg2 can reference variables from previous operations
   - Example: `GetText(label) ¡æ var1; TrimString(var1, "Label: ") ¡æ var2`

5. **Multiple Occurrences at Start/End**
   - Removes ALL consecutive occurrences from start and end
   - Example: "!!!text!!!" trimmed by "!" becomes "text"
   - Example: "aatext" trimmed by "a" becomes "text"

6. **Middle Occurrences Preserved**
   - Does NOT remove occurrences in the middle of the string
   - Example: "a test a" trimmed by "a" becomes " test " (middle "a" in "test" preserved)

7. **Case Sensitivity**
   - Trim operation is case-sensitive (uses `StringComparison.Ordinal`)
   - "ABC" will not trim "abc"

**Preview Format**:
- Success: Shows cleaned string
- Empty trim: Shows source string unchanged
- Example: " am me " (after trimming "I" from " I am me ")

**Use Cases**:

**Remove Prefix**:
```
GetValueFromSelection(ResultsList, "ID") ¡æ var1  // "ID: 12345"
TrimString(var1, "ID: ") ¡æ var2  // "12345"
```

**Clean Label Text**:
```
GetText(NameLabel) ¡æ var1  // "Patient Name: John Doe"
TrimString(var1, "Patient Name: ") ¡æ var2  // "John Doe"
```

**Remove Surrounding Characters**:
```
GetText(PriceField) ¡æ var1  // "***$99.99***"
TrimString(var1, "*") ¡æ var2  // "$99.99"
```

**Chain Multiple Trims**:
```
GetText(field) ¡æ var1  // "[PREFIX] Content [SUFFIX]"
TrimString(var1, "[PREFIX] ") ¡æ var2  // "Content [SUFFIX]"
TrimString(var2, " [SUFFIX]") ¡æ var3  // "Content"
```

**Limitations**:
- Only trims from start and end (use Replace for middle occurrences)
- Case-sensitive matching only
- No regex pattern matching (use Replace operation for regex)
- No character set trimming (e.g., cannot trim all whitespace characters - use Trim operation for that)

**Comparison with Similar Operations**:
- **Trim**: Removes whitespace from start/end only
- **TrimString**: Removes specific substring from start/end only (NEW)
- **Replace**: Removes/replaces all occurrences throughout the entire string

**Status**: ? **Implemented** (2025-01-20)

**Cross-References**:
- Implementation: [Plan-active.md - TrimString Implementation](Plan-active.md#change-log-addition-2025-01-20--setclipboard--trimstring-operation-fixes)
- Related Operations: 
  - `Trim` (trims whitespace from start/end)
  - `Replace` (more flexible pattern replacement with regex support)
  - `Split` (splits string into parts)

**Future Enhancements**:
- Add `TrimStart` / `TrimEnd` for prefix/suffix-only trimming
- Add `TrimCharacters` for character set removal
- Add case-insensitive trim option
- Add regex pattern trimming

---

## SetFocus Operation Retry Logic (2025-01-20)

### FR-1140: Add Retry Logic to SetFocus Operation in Custom Procedures

**Problem**: SetFocus operation in Custom Procedures works during test run (manual step-by-step execution) but fails during automated procedure module execution.

**Root Cause**: UI automation elements may not be immediately ready for focus operations, especially during automated procedure execution where operations happen in rapid succession.

**Solution**: Implement retry logic with configurable attempts and delays for SetFocus operation.

**Requirements**:

1. **Retry Configuration**
   - Maximum 3 attempts per SetFocus operation
   - 150ms delay between retry attempts
   - Applies to both test mode and procedure module execution

2. **Success Feedback**
   - First attempt success: Display "(focused)"
   - Retry success: Display "(focused after N attempts)" where N is attempt count
   - Helps users identify elements that need more time

3. **Error Feedback**
   - After all attempts fail: Display "(error after 3 attempts: [error message])"
   - Includes exception message for troubleshooting
   - Clear indication that retries were exhausted

4. **Consistent Implementation**
   - Apply same retry logic in SpyWindow (test mode)
   - Apply same retry logic in ProcedureExecutor (procedure module)
   - Ensure consistent behavior across both execution contexts

5. **Performance Considerations**
   - Maximum 300ms total delay on complete failure (2 retries ¡¿ 150ms)
   - No delay on first attempt success
   - Minimal impact on procedure execution time

**Behavior Examples**:

```
Scenario 1: First attempt succeeds
SetFocus on SearchResultsList ¡æ "(focused)"

Scenario 2: Second attempt succeeds
SetFocus on SearchResultsList ¡æ wait 150ms ¡æ "(focused after 2 attempts)"

Scenario 3: All attempts fail
SetFocus on SearchResultsList ¡æ wait 150ms ¡æ wait 150ms ¡æ "(error after 3 attempts: Element not found)"
```

**Status**: ? **Implemented** (2025-01-20)

**Cross-References**:
- Implementation: [Plan-active.md - SetFocus Retry Logic](Plan-active.md#change-log-addition-2025-01-20--setfocus-operation-retry-logic)
- Related Operations: Other timing-sensitive operations may benefit from similar retry logic (future enhancement)

---

## Foreign Text Sync & Caret Management (2025-01-19)

> **Archive Note**: Detailed specifications for FR-1100..FR-1136 have been archived.  
> **Full Details**: [archive/2025-Q1/Spec-2025-Q1-foreign-text-sync.md](archive/2025-Q1/Spec-2025-Q1-foreign-text-sync.md)

### Summary
Bidirectional text synchronization between Radium Findings editor and external textbox applications (e.g., Notepad) with automatic merge-on-disable and caret position preservation.

### Key Requirements (Recent Updates)
- **FR-1132**: Caret position preservation during merge (new_caret = old_caret + merged_length)
- **FR-1133**: Best-effort focus prevention when clearing foreign textbox
- **FR-1134**: FindingsCaretOffsetAdjustment property for merge coordination
- **FR-1135**: CaretOffsetAdjustment DP in MusmEditor for caret adjustment
- **FR-1136**: CaretOffsetAdjustment DP in EditorControl for binding flow

### Status
? **Implemented and Active** (2025-01-19)

### Cross-References
- Implementation: [Plan-2025-Q1-foreign-text-sync.md](archive/2025-Q1/Plan-2025-Q1-foreign-text-sync.md)
- Tasks: T1100-T1148 (all complete)

---

## Current Combination Quick Delete (2025-01-18)

### FR-1025: Double-Click to Remove from Current Combination
Enable users to quickly remove techniques from "Current Combination" ListBox by double-clicking items.

**Behavior**:
- Double-click on any item in Current Combination ¡æ removes immediately
- No confirmation dialog
- Updates SaveNewCombinationCommand enabled state
- GroupBox header includes hint: "(double-click to remove)"

---

### FR-1026: All Combinations Library ListBox
Display all technique combinations (regardless of studyname) in a new ListBox for reuse/modification.

**Features**:
- Loads all combinations from `v_technique_combination_display`
- Ordered by ID descending (newest first)
- Shows formatted display text (e.g., "axial T1, T2; coronal T1")
- Double-click to load into Current Combination

---

### FR-1027: Double-Click to Load Combination
Enable loading existing combinations into Current Combination for modification.

**Behavior**:
- Double-click on item in All Combinations ¡æ loads techniques into Current Combination
- Fetches combination items via repository
- Matches prefix/tech/suffix strings to get IDs
- Prevents duplicates during load
- Appends to end with sequential sequence_order
- Notifies SaveNewCombinationCommand after load

**Duplicate Prevention**: Skips techniques already present in Current Combination by exact (prefix_id, tech_id, suffix_id) match.

---

## Report Inputs Panel Layout (2025-01-18)

### FR-1081..FR-1086: Side-by-Side Row Layout with MinHeight Bindings
Restructured ReportInputsAndJsonPanel to use side-by-side row layout where each main textbox has a corresponding proofread textbox in the same vertical position.

**Key Changes**:
- Chief Complaint (PR) binds MinHeight to txtChiefComplaint.MinHeight (60px)
- Patient History (PR) binds MinHeight to txtPatientHistory.MinHeight (60px)
- Findings (PR) binds MinHeight to txtFindings.MinHeight (100px)
- Conclusion (PR) binds MinHeight to txtConclusion.MinHeight (100px)

**Benefits**:
- Natural Y-coordinate alignment via WPF Grid row mechanics
- No custom layout calculation required
- Dynamic height adjustments preserve alignment
- Simplified XAML with fewer converters

---

### FR-1090: Scroll Synchronization Between Main and Proofread
Added scroll synchronization to keep corresponding textboxes visible together when scrolling the proofread column.

**Implementation**:
- `OnProofreadScrollChanged` event handler
- `_isScrollSyncing` flag to prevent feedback loops
- One-way sync: proofread scroll affects main column
- Main column scrolls independently (no reverse sync)

---

### FR-1091: Reverse Layout Support
Maintained reverse layout feature for column swapping.

**Behavior**: When Reverse=true, JSON and Main/Proofread columns swap positions while maintaining alignment.

---

### FR-1093: No Custom Y-Coordinate Calculation
Eliminated need for custom behaviors or value converters to calculate Y-coordinate alignment.

**Rationale**: WPF Grid naturally aligns elements in the same row, making custom calculation unnecessary.

---

## Phrase-SNOMED Mapping Window (2025-01-15)

### FR-950: Pre-filled Search Text in Link Window
When opening the phrase-SNOMED link window, pre-populate the search textbox with the phrase text.

**Behavior**:
- `SearchText` property initialized from `phraseText` constructor parameter
- User can immediately press Enter to search without retyping
- Search box remains editable for custom searches

**Fix**: Resolves UX issue where users had to retype phrase text to search.

---

### FR-951: Map Button Enabled State Fix
Fixed Map button remaining disabled after concept selection by calling `NotifyCanExecuteChanged()` when `SelectedConcept` changes.

**Implementation**:
- Replaced `[ObservableProperty]` with manual property setter
- Calls `MapCommand.NotifyCanExecuteChanged()` on value change
- Ensures WPF re-evaluates button enabled state immediately

**Fix**: Resolves UX issue where button stayed disabled despite valid selection.

---

## Archived Specifications

The following specifications have been moved to archives for better document organization:

### 2024 Q4 Archives
- **FR-500..FR-547**: Study Technique Management, PACS Automation
- **FR-600..FR-681**: Multi-PACS Tenancy, Window Placement, Reportify
- **FR-700..FR-709**: Editor Phrase Highlighting
- **Location**: [archive/2024/Spec-2024-Q4.md](archive/2024/Spec-2024-Q4.md)

### 2025 Q1 Archives (Older Entries)
- **FR-900..FR-915**: Phrase-SNOMED Mapping Database Schema
- **FR-1000..FR-1024**: Technique Combination Management
- **Location**: [archive/2025-Q1/](archive/2025-Q1/)

---

## Document Maintenance

### Active Document Criteria
This document contains only:
- Features specified in the last 90 days (since 2025-01-01)
- Features currently under active development
- Features with pending implementation tasks

### Archival Policy
Features are moved to archives when:
- Implementation complete and stable for 90+ days
- No active tasks or pending work
- Documented in at least one release

### Finding Historical Requirements
1. Check [archive/README.md](archive/README.md) for index of all archives
2. Use Feature Domains Index to locate specific feature areas
3. Search archives by requirement ID (FR-XXX) or date range

---

*Document last trimmed: 2025-01-20*  
*Next review: 2025-04-20 (90 days)*
