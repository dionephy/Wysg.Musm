# Enhancement: Conditional SendReport Procedure Based on Modality

**Date:** 2025-11-10  
**Type:** Enhancement  
**Status:** Completed

## Overview
Modified the SendReport automation module to conditionally use either "SendReport" or "SendReportWithoutHeader" custom procedure based on the current study's modality and the "Following modalities don't send header" setting.

## Changes Made

### 1. MainViewModel.Commands.Automation.cs
- **Modified:** `RunSendReportModuleWithRetryAsync()` method to determine which procedure to use before executing
- **Added:** `DetermineSendReportProcedureAsync()` method that:
  1. Extracts the modality from the current study using LOINC mapping
  2. Reads the ModalitiesNoHeaderUpdate setting
  3. Checks if the current modality is in the exclusion list
  4. Returns "SendReportWithoutHeader" if modality is excluded, otherwise "SendReport"

### 2. Logic Flow
```
SendReport Module Called
        ��
DetermineSendReportProcedureAsync()
        ��
Extract Current Study Modality (via LOINC mapping)
        ��
Read ModalitiesNoHeaderUpdate Setting
        ��
Check if Modality in Exclusion List?
    ���� Yes �� Use "SendReportWithoutHeader"
    ���� No  �� Use "SendReport"
        ��
Execute Selected Procedure with Retry Logic
```

## Configuration

### Settings Location
**Automation Tab �� "Following modalities don't send header" textbox**

### Format
Comma or semicolon-separated list of modalities (case-insensitive):
```
XR, CR, DX
```

### Example Scenario
- **Studyname:** "XR CHEST PA"
- **LOINC Modality Mapping:** "XR"
- **Setting Value:** "XR,CR"
- **Result:** Uses "SendReportWithoutHeader" procedure

## Technical Details

### Modality Extraction
- Uses `ExtractModalityAsync(StudyName)` method
- Queries LOINC part mapping for "Rad.Modality.Modality Type"
- Falls back to "OT" (Other) if no mapping found

### Setting Storage
- Stored in `IRadiumLocalSettings.ModalitiesNoHeaderUpdate`
- Persisted in local settings file (global, not PACS-scoped)
- Saved when user clicks Save in Settings �� Automation tab

### Comparison Logic
- Modalities converted to uppercase for comparison
- Whitespace trimmed from each modality in list
- Empty entries filtered out
- Uses HashSet for efficient lookup

## Benefits

1. **Flexibility:** Users can configure which modalities send reports without headers
2. **Automation:** No manual intervention needed when sending reports
3. **Consistency:** Same logic applies across all send operations
4. **Transparency:** Debug logs show which procedure is selected and why

## Fallback Behavior

If any error occurs during modality determination:
- Logs error to debug output
- Falls back to standard "SendReport" procedure
- Ensures send operation continues even if check fails

## Usage Example

### Setup
1. Open Settings �� Automation tab
2. Find "Following modalities don't send header" textbox
3. Enter modalities: `XR,CR,DX`
4. Click Save

### Runtime
When SendReport module runs:
```
[DetermineSendReportProcedure] Current study modality: 'XR', StudyName: 'XR CHEST PA'
[DetermineSendReportProcedure] ModalitiesNoHeaderUpdate setting: 'XR,CR,DX'
[DetermineSendReportProcedure] Excluded modalities: [XR, CR, DX]
[DetermineSendReportProcedure] Should send without header: True
[DetermineSendReportProcedure] Using SendReportWithoutHeader - modality 'XR' is in exclusion list
[SendReportModule] Using procedure: SendReportWithoutHeader
```

## Related Features
- FR-1190 to FR-1198: SendReport and SendReportWithoutHeader procedures
- ENHANCEMENT_2025-11-10_ModalitiesNoHeaderUpdate.md: Original setting implementation
- ENHANCEMENT_2025-11-10_SendReportWithoutHeader.md: SendReportWithoutHeader method addition

## Testing Checklist
- [x] Build succeeds without errors
- [ ] Manual test: XR study uses SendReportWithoutHeader
- [ ] Manual test: CT study uses SendReport
- [ ] Manual test: Empty setting uses SendReport  
- [ ] Manual test: Invalid modality uses SendReport (fallback)
- [ ] Manual test: Setting persists across restarts
