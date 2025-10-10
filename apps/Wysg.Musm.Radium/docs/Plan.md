# Implementation Plan: Radium Cumulative (Reporting Workflow + Editor + Mapping + PACS)

## Change Log Addition (2025-10-10 - Automation modules: Study/Patient Remarks)
- Added two automation modules to Settings → Automation:
  - GetStudyRemark: fetches PACS method "Get current study remark" and updates `study_remark` in current report JSON.
  - GetPatientRemark: fetches PACS method "Get current patient remark" and updates `patient_remark` in current report JSON.
- Extended `SettingsViewModel.AvailableModules` to include `GetStudyRemark` and `GetPatientRemark` so users can drag them into sequences.
- `MainViewModel`:
  - Added properties `StudyRemark`, `PatientRemark` and wired them into `CurrentReportJson` serialization.
  - In New Study automation executor, recognized the two modules and fetch via `PacsService.GetCurrentStudyRemarkAsync()` / `GetCurrentPatientRemarkAsync()` and set the properties.
- Remarks update status messages for quick visual feedback.

## Change Log Addition (2025-10-10 - Fix: Patient Remark bookmark)
- Introduced a distinct known control `PatientRemark` in `UiBookmarks.KnownControl` and exposed it in SpyWindow → Known controls combo.
- Reason: Previously only `StudyRemark` existed, leading to "Get current patient remark" procedures accidentally using the study remark bookmark.
- Result: Users can now map the patient remark UI element separately and reference it in the `GetCurrentPatientRemark` procedure.

## Change Log Addition (2025-10-10 - Auto-seed key procedures for Remarks)
- Implemented auto-seeding in `ProcedureExecutor`:
  - When `GetCurrentPatientRemark` has no saved procedure, the system creates and persists a default procedure with a single `GetText` step on Element=`PatientRemark`.
  - Similarly, if `GetCurrentStudyRemark` is missing, auto-seed with `GetText` on Element=`StudyRemark`.
- Effect: "GetPatientRemark" module always has a working key procedure by default and remains user-editable in SpyWindow.

## Change Log Addition (2025-10-10 - Enforcement of patient_remark source)
- Enforced that `patient_remark` is only populated from the `GetCurrentPatientRemark` procedure result by ignoring `patient_remark` edits in `CurrentReportJson` parsing; `StudyRemark` remains round-trippable.

## Change Log Addition (2025-10-10 - ProcedureExecutor GetHTML + Replace + early-return bug)
- Implemented `Replace` op and `GetHTML` op in the procedure executor for parity with the SpyWindow Custom Procedures UI.
- Fixed an early return in `ExecuteInternal` that prevented fallback auto-seeding and could lead to returning the previous step’s value when `GetHTML` was present.
- Registered `CodePagesEncodingProvider` and added light-weight charset handling to decode HTML using header/meta charsets.

## Approach
1) Surface modules in Automation library list (already bound).
2) Procedure-first design via `PacsService` that executes procedure tags with retry.
3) Auto-seed default procedures at first invocation when missing and persist to `ui-procedures.json`.
4) Distinct bookmarks `StudyRemark` and `PatientRemark` avoid cross-reading.
5) Enforce patient remark provenance: JSON Apply ignores `patient_remark` to prevent accidental overwrite.
6) Ensure `GetHTML` op executes and returns the fetched HTML instead of preserving prior step output.

## Notes
- After update, open Spy → map `PatientRemark` to the patient remark field.
- The system will create the default `GetCurrentPatientRemark` procedure on first run if none exists. You can modify it later in SpyWindow → Custom Procedures.

## Test Plan (Manual)
- Procedures grid:
  - Create steps: [Arg1 Var=var1] [Op=Replace] to form a URL in var2 (optional) → [Op=GetHTML, Arg1 Type=Var, Value=var2] → Verify final output shows HTML content.
  - Ensure Arg1 Type=Var references the correct prior variable name, e.g., `var2`.
- Verify fallback:
  - Delete `%APPDATA%\Wysg.Musm\Radium\ui-procedures.json`, run GetPatientRemark module; confirm `GetCurrentPatientRemark` is auto-created and persisted.
- Verify enforcement:
  - Edit `CurrentReportJson` to change `patient_remark`; check that the bound PatientRemark value does not change and is only set when the automation module runs.

## Risks / Mitigations
- If the bookmark for `PatientRemark` is not mapped, `GetCurrentPatientRemark` returns `(no element)`. Map once in SpyWindow.
- If `GetHTML` URL is not http/https or empty, the step returns `(no url)` to signal misconfiguration.

Backlog
- Wire Add Study sequence execution path to honor the modules (T507).

