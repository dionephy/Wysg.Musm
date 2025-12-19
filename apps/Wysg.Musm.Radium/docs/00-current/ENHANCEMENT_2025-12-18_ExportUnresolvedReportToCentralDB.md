# Enhancement: Send Unresolved Report Text to Central DB (radium.exported_report)

**Date**: 2025-12-18  
**Type**: Enhancement  
**Status**: ? Complete  

---

## Overview

Enhanced the `InsertCurrentStudyReport` automation module to automatically detect and export reports containing unresolved phrases to the central database (`radium.exported_report` table) via the Radium API.

### What Are Unresolved Phrases?

Unresolved phrases are words in the Findings or Conclusion editors that are **not** in the phrase snapshot (would be colored **red** in the editor). These words represent potentially misspelled or unrecognized medical terminology that should be reviewed.

**Excluded from detection:**
- Numbers (integers and decimals like `12.5`)
- Dates (YYYY-MM-DD format like `2025-12-18`)
- Punctuation-only tokens

---

## Behavior

When the `InsertCurrentStudyReport` module runs:

1. **Phase 1**: Insert report to local DB (`med.rad_report`) - existing behavior unchanged
2. **Phase 2** (NEW): Check for unresolved phrases in Findings and Conclusion
   - If unresolved phrases detected ¡æ Export unreportified text to central DB via API
   - If no unresolved phrases ¡æ Skip export

### Status Messages

| Scenario | Status Message |
|----------|----------------|
| No unresolved phrases | *(no additional message - only local insert status)* |
| Export success | `InsertCurrentStudyReport: Exported unresolved text (id=123, 5 unresolved words)` |
| API unavailable | `InsertCurrentStudyReport: API not available for export` |
| Export failed | `InsertCurrentStudyReport: Export failed - {error message}` |

---

## Technical Implementation

### New Files

1. **`MainViewModel.UnresolvedPhrases.cs`** - Helper methods for phrase detection
   - `HasUnresolvedPhrases(text)` - Returns true if text contains red-colored words
   - `GetUnresolvedWords(text)` - Returns list of unresolved words
   - `GetUnreportifiedTextForExport()` - Returns formatted text for export

### Modified Files

1. **`InsertCurrentStudyReportProcedure.cs`**
   - Added dependencies: `IExportedReportsApiClient`, `ITenantContext`
   - Added `CheckAndExportUnresolvedPhrasesAsync()` method
   - Calls API to create exported report if unresolved phrases detected

2. **`App.xaml.cs`**
   - Updated DI registration to inject new dependencies

### API Endpoint Used

```http
POST /api/accounts/{accountId}/exported-reports
Content-Type: application/json

{
  "report": "[HEADER]\\n...\\n\\n[FINDINGS]\\n...\\n\\n[CONCLUSION]\\n...",
  "reportDateTime": "2025-12-18T10:30:00Z"
}
```

---

## Export Format

The exported text now includes only Findings and Conclusion (header is excluded):

```
[FINDINGS]
The patient shows signs of abnormal findings in the chest area.
Cardiac silhouette is normal.

[CONCLUSION]
No significant abnormality detected.
```

---

## Dependencies

- **IExportedReportsApiClient**: API client for exported reports operations
- **ITenantContext**: Provides current account ID for API calls
- **MainViewModel.CurrentPhraseSnapshot**: List of known phrases for comparison

---

## Error Handling

- Export failure is **non-fatal** - the local DB insert still succeeds
- Errors are logged to Debug output and shown in status bar
- Missing dependencies (API client, tenant context) cause graceful skip

---

## Configuration

No additional configuration required. The feature uses:
- Existing `IExportedReportsApiClient` registered in DI
- Existing `ITenantContext` for account identification
- Existing phrase snapshot from `MainViewModel`

---

## Testing

### Test Scenarios

1. **Report with unresolved phrases**
   - Type text with unknown words (red colored)
   - Run InsertCurrentStudyReport module
   - Verify: Report appears in `radium.exported_report` table

2. **Report with all resolved phrases**
   - Type only known medical terms (not red)
   - Run InsertCurrentStudyReport module
   - Verify: No export occurs

3. **Numbers and dates are ignored**
   - Type "12.5 mg on 2025-12-18"
   - Run InsertCurrentStudyReport module
   - Verify: Numbers and dates are not flagged as unresolved

4. **API unavailable**
   - Disconnect from API server
   - Run InsertCurrentStudyReport module
   - Verify: Local insert succeeds, export silently skipped

---

## Related Files

- `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.UnresolvedPhrases.cs` (NEW)
- `apps/Wysg.Musm.Radium/Services/Procedures/InsertCurrentStudyReportProcedure.cs`
- `apps/Wysg.Musm.Radium/Services/Procedures/IInsertCurrentStudyReportProcedure.cs`
- `apps/Wysg.Musm.Radium/App.xaml.cs`
- `apps/Wysg.Musm.Radium/Services/ApiClients/ExportedReportsApiClient.cs`
- `apps/Wysg.Musm.Radium.Api/Controllers/ExportedReportsController.cs`

---

## Database Table Reference

Central DB table: `radium.exported_report`

| Column | Type | Description |
|--------|------|-------------|
| id | bigint | Primary key (auto-generated) |
| account_id | bigint | User's account ID |
| report | text | Exported text content |
| report_datetime | datetime | Report timestamp |
| uploaded_at | datetime | Upload timestamp |
| is_resolved | bit | Whether report has been reviewed |

---

## Future Enhancements

1. **Batch review UI**: Window to review and resolve exported reports
2. **Auto-learn phrases**: Option to add resolved words to phrase list
3. **Statistics dashboard**: Track unresolved phrase frequency

---

*Implementation by GitHub Copilot - 2025-12-18*
