# Fix: InsertCurrentStudyReport writes PrevReport split metadata

**Date:** 2025-12-31  
**Status:** Completed  
**Category:** Bug Fix

## Problem
The InsertCurrentStudyReport procedure saved the current report JSON without the `PrevReport` split metadata. Local DB rows created from this path lost header/findings splitter positions, so reopening the report could not reconstruct header temp vs. findings/conclusion segments.

## Solution
Before inserting the report, the procedure now rewrites the JSON to include a `PrevReport` block. It uses the length of `Temp Header` for `header_and_findings_header_splitter_*`, the length of `header_and_findings` for `header_and_findings_conclusion_splitter_*`, and sets all `final_conclusion_*` splitters to `0`. Any existing `PrevReport` section is replaced with these values.

## Files Changed
- `apps/Wysg.Musm.Radium/Services/Procedures/InsertCurrentStudyReportProcedure.cs`

## Testing
1. Prepare a current report with header/temp header and findings populated.
2. Run the InsertCurrentStudyReport module.
3. Inspect the saved report JSON (local DB) and confirm it contains:
   - `PrevReport.header_and_findings_header_splitter_from`/`to` equal to the Temp Header length.
   - `PrevReport.header_and_findings_conclusion_splitter_from`/`to` equal to the header_and_findings length.
   - `PrevReport.final_conclusion_*` splitters all `0`.
