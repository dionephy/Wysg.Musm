# Feature: InsertCurrentStudy & InsertCurrentStudyReport Modules (2025-12-10)

**Date**: 2025-12-10  
**Type**: Feature  
**Status**: ? Complete  
**Priority**: High

---

## Summary
Added two new built-in automation modules that push the current study context into the local PostgreSQL database without routing through UI-bound flows:

| Module | Purpose | Required Variables |
|--------|---------|--------------------|
| `InsertCurrentStudy` | Ensures `med.rad_study` has a record for the active patient/study | Current Patient Number, Current Study Studyname, Current Study Datetime |
| `InsertCurrentStudyReport` | Inserts the active report into `med.rad_report` when it does not already exist | All `InsertCurrentStudy` variables + Current Study Report Datetime |

Both modules mirror the previously shipped `InsertPreviousStudy*` modules but source their data from the current (non-temp) globals, enabling users to persist in-progress studies/reports on demand.

## Behavior
### InsertCurrentStudy
1. Validates that **Current Patient Number**, **Current Study Studyname**, and **Current Study Datetime** are populated.
2. Calls `IRadStudyRepository.EnsureStudyAsync`, creating patient/studyname/study rows as needed.
3. Emits status text indicating the resulting study ID or validation failure.

### InsertCurrentStudyReport
1. Reuses the study validation/creation above and additionally requires **Current Study Report Datetime**.
2. Builds report JSON from `CurrentReportJson` (fallback to `Reported Header/Findings`, `Reported Conclusion`, `Report Radiologist`).
3. Calls `InsertReportIfNotExistsAsync` with `is_mine=true`, skipping duplicates.
4. Emits status updates for inserted vs. skipped rows.

## Files Changed
- `MainViewModel.*` partials: wiring, module handlers, new helper methods.
- `App.xaml.cs`: DI registrations for the new procedures.
- `SettingsViewModel.cs`: exposes the modules inside the automation palette.
- `Services/Procedures/*`: new interfaces + implementations for current study/report insertion.

## Validation Steps
- [x] Automation window lists both modules.
- [x] `InsertCurrentStudy` only runs when required globals are present.
- [x] `InsertCurrentStudyReport` skips when a matching report already exists.
- [x] Build succeeds (no warnings/errors).

## Usage Example
```
1. Set "Current Study Report Datetime" via GetUntilReportDateTime.
2. Run custom procedure that prepares Reportified text.
3. InsertCurrentStudy    # ensures study row
4. InsertCurrentStudyReport
```

This sequence inserts the in-progress report into `med.rad_report` without leaving the automation surface, allowing later retrieval by downstream workflows or external tooling.
