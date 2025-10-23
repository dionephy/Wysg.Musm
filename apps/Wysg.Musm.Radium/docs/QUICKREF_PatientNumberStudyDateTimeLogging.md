# Quick Reference: Patient Number and Study DateTime Mismatch Logging

## What Changed?

When automation aborts due to mismatches, you now see **exactly what values were compared** in both the status bar and debug logs.

## New Status Messages

### Before ?
```
Patient number mismatch - automation aborted
Study date/time mismatch - automation aborted
```

### After ?
```
Patient number mismatch - automation aborted (PACS: '12345-67', Main: '1234568')
Study date/time mismatch - automation aborted (PACS: '2025-01-22 14:30:00', Main: '2025-01-23 09:15:00')
```

## Where to Find Details

### 1. Status Bar (Always Visible)
- Shows mismatch immediately when automation aborts
- Displays both PACS and Main values side-by-side
- Red text indicates error

### 2. Debug Logs (Detailed)
Look for these log entries:
```
[ProcedureExecutor][PatientNumberMatch] PACS Patient Number (raw): '...'
[ProcedureExecutor][PatientNumberMatch] Main Patient Number (raw): '...'
[ProcedureExecutor][PatientNumberMatch] Result: false

[Automation][AbortIfPatientNumberNotMatch] MISMATCH - PACS: '...' Main: '...'
```

## What Gets Compared?

### Patient Numbers
- **Normalization**: Removes hyphens, spaces, and other special characters
- **Case**: Converted to uppercase
- **Example**: "12345-67" becomes "1234567" for comparison

### Study DateTime
- **Date Only**: Only the date portion is compared (time is ignored)
- **Example**: "2025-01-22 14:30:00" compared to "2025-01-22 09:15:00" = MATCH
- **Example**: "2025-01-22 14:30:00" compared to "2025-01-23 09:15:00" = MISMATCH

## Troubleshooting Guide

### Patient Number Mismatch

**Possible Causes**:
1. Wrong patient selected in PACS worklist
2. Wrong patient loaded in Radium
3. Patient number formatting differs between systems
4. Data entry error in one system

**How to Fix**:
1. Check status bar message to see both patient numbers
2. Verify which patient number is correct
3. Update PACS or Radium to match the correct patient
4. Re-run automation

### Study DateTime Mismatch

**Possible Causes**:
1. Wrong study date selected in PACS
2. Wrong study loaded in Radium
3. Study performed on different date than expected
4. Timezone differences

**How to Fix**:
1. Check status bar message to see both study datetimes
2. Verify which study date is correct
3. Select the correct study in PACS or Radium
4. Re-run automation

## When to Contact Support

Contact support if you see:
- Same patient numbers but still get mismatch error
- Same study dates but still get mismatch error
- Missing values in status bar (shows blank or "(null)")
- Unexpected normalization behavior

## Files Changed

For developers who want to review the code:
- `apps/Wysg.Musm.Radium/Services/ProcedureExecutor.cs` - Comparison logic with logging
- `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.cs` - Status messages

## Documentation

For more details, see:
- `CHANGELOG_2025-01-23_PatientNumberStudyDateTimeLogging.md` - Full changelog
- `IMPLEMENTATION_SUMMARY_2025-01-23_PatientNumberStudyDateTimeLogging.md` - Technical summary
