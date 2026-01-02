# FIX: Remove Remarks from Current Report Insert JSON (2026-01-02)

**Status**: ? Implemented

## Summary
`InsertCurrentStudyReport` no longer persists `study_remark`, `patient_remark`, or `findings_preorder` when saving the current report JSON to `med.rad_report`. These fields stay in `CurrentReportJson` for UI/workflows but are stripped before DB insert.

## Details
- Added transient-field stripping before persisting the JSON so remarks and preorder drafts are excluded from saved records.
- Existing PrevReport splitter injection remains unchanged.
- Fallback JSON (when `CurrentReportJson` is empty) never included the removed fields, ensuring consistent output.

## Files Updated
- `Services/Procedures/InsertCurrentStudyReportProcedure.cs`

## Validation
- Build: ? Pending
- Manual: InsertCurrentStudyReport now saves JSON without `study_remark`, `patient_remark`, `findings_preorder`; other fields and PrevReport splits remain.
