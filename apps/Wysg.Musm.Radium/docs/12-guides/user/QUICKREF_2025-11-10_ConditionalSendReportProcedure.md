# Quick Reference: Conditional SendReport Procedure

**Date**: 2025-11-25  
**Type**: Quick Reference  
**Category**: User Reference  
**Status**: ? Active

---

## Summary

This guide provides quick reference information for users. For detailed implementation information, see the related plan and specification documents.

---

# Quick Reference: Conditional SendReport Procedure

## Feature
SendReport automation module now automatically selects the appropriate custom procedure based on study modality.

## Configuration
**Location:** Settings → Automation Tab → "Following modalities don't send header"

**Format:** Comma-separated list of modalities
```
XR, CR, DX, MG
```

## How It Works

| Condition | Procedure Used |
|-----------|----------------|
| Modality in exclusion list | SendReportWithoutHeader |
| Modality NOT in list | SendReport |
| No modality found | SendReport (fallback) |
| Error during check | SendReport (fallback) |

## Example Scenarios

### Scenario 1: XR Study
- **Study:** "XR CHEST PA"
- **Modality:** XR (from LOINC)
- **Setting:** "XR,CR"
- **Result:** Uses **SendReportWithoutHeader** ?

### Scenario 2: CT Study
- **Study:** "CT BRAIN WITHOUT CONTRAST"
- **Modality:** CT (from LOINC)
- **Setting:** "XR,CR"
- **Result:** Uses **SendReport** (standard)

### Scenario 3: Empty Setting
- **Study:** Any study
- **Setting:** (empty)
- **Result:** Uses **SendReport** (standard)

## Debug Information
Status bar shows which procedure is being used:
```
Using send without header (modality 'XR' configured for header-less send)
```

## Common Modalities
| Modality | Description |
|----------|-------------|
| XR | X-Ray / Plain Film |
| CR | Computed Radiography |
| DX | Digital X-Ray |
| CT | Computed Tomography |
| MR | Magnetic Resonance |
| US | Ultrasound |
| MG | Mammography |
| NM | Nuclear Medicine |
| PT | PET Scan |

## Troubleshooting

### Problem: Wrong procedure used
**Check:**
1. Modality extracted correctly? (see debug logs)
2. Setting value correct? (Settings → Automation)
3. Modality case matches? (case-insensitive, but check spelling)

### Problem: Always uses standard SendReport
**Possible causes:**
1. Setting is empty
2. Modality not in LOINC mapping (shows as "OT")
3. Modality not in exclusion list

### Problem: Error during send
**Fallback behavior:**
- Always falls back to standard SendReport on error
- Check debug logs for error details
- Send operation continues with fallback

## Tips

1. **Use Consistent Format:** Separate with commas, spaces optional
2. **Case Insensitive:** "XR", "xr", "Xr" all work the same
3. **Verify LOINC:** Ensure studies have LOINC modality mapping
4. **Test First:** Test with one modality before adding more
5. **Check Logs:** Debug logs show exact decision-making process

## Related Documentation
- ENHANCEMENT_2025-11-10_ConditionalSendReportProcedure.md
- ENHANCEMENT_2025-11-10_SendReportWithoutHeader.md
- QUICKREF_2025-11-10_SendReportWithoutHeader.md

