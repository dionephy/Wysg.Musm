# Quick Reference: Previous Study Modality & Comparison Update

**Date**: 2025-11-25  
**Type**: Quick Reference  
**Category**: User Reference  
**Status**: ? Active

---

## Summary

This guide provides quick reference information for users. For detailed implementation information, see the related plan and specification documents.

---

# Quick Reference: Previous Study Modality & Comparison Update

## Overview
Two enhancements to improve previous study handling:
1. Auto-update Comparison field when AddPreviousStudy runs
2. Extract modality from LOINC mapping for tab titles

---

## Key Features

### 1. Comparison Auto-Update
**When it runs:** After AddPreviousStudy automation completes  
**What it does:** Sets Comparison field to "{Modality} {Date}" of the previously selected study  
**Example:** "CT 2024-01-15"  
**Skip condition:** Current study is XR AND "Do not update header in XR" setting is enabled

### 2. LOINC-Based Modality
**Tab title format:** "{Modality} {StudyDate}"  
**Example:** "MR 2024-01-15" (was: "2024-01-15 MR")  
**Modality source:** LOINC part "Rad.Modality.Modality Type"  
**Fallback:** Simple prefix extraction if no LOINC mapping

---

## Implementation

### Files Changed
1. `MainViewModel.PreviousStudiesLoader.cs` - Added `ExtractModalityAsync` and `ExtractModalityFallback` methods
2. `MainViewModel.Commands.AddPreviousStudy.cs` - Already has `UpdateComparisonFromPreviousStudyAsync` (no changes needed)
3. `MainViewModel.cs` - Added `_studynameLoincRepo` field and constructor parameter
4. `App.xaml.cs` - Added `IStudynameLoincRepository` to DI registration

### Key Methods

#### ExtractModalityAsync
```csharp
private async Task<string> ExtractModalityAsync(string studyname)
{
    // 1. Query database for studyname
    // 2. Get mappings for studyname
    // 3. Find "Rad.Modality.Modality Type" part
    // 4. Return part name (e.g., "CT", "MR")
    // 5. Fallback if no mapping
}
```

#### ExtractModalityFallback
```csharp
private static string ExtractModalityFallback(string studyname)
{
    // Check common prefixes (CT, MR, US, XR, etc.)
    // Extract first word if no match
    // Return first 2-3 chars if no space
}
```

---

## Database Query

### LOINC Modality Lookup
```sql
SELECT p.part_name
FROM med.rad_studyname s
JOIN med.rad_studyname_loinc_part m ON m.studyname_id = s.id
JOIN loinc.part p ON p.part_number = m.part_number
WHERE s.studyname = @studyname
  AND p.part_type_name = 'Rad.Modality.Modality Type'
LIMIT 1;
```

---

## Testing Checklist

- [ ] LOINC-mapped studyname shows correct modality (e.g., "Brain MRI" → "MR")
- [ ] Non-mapped studyname uses fallback (e.g., "XYZ Study" → "XYZ")
- [ ] Tab title format is "{Modality} {Date}" (not "{Date} {Modality}")
- [ ] Comparison updates to previous study's modality/date
- [ ] XR restriction works (Comparison NOT updated when XR + setting enabled)

---

## Configuration

### Setting: Do Not Update Header in XR
- **Path:** Settings > Automation
- **Effect:** Skips Comparison update if current study is XR
- **Default:** false (updates enabled)

---

## Troubleshooting

### Modality shows "UNKNOWN"
**Cause:** No LOINC mapping AND fallback failed  
**Fix:** Add LOINC mapping for the studyname in StudynameLoincWindow

### Comparison not updating
**Possible causes:**
1. No previous study was selected before AddPreviousStudy ran
2. Current study is XR and setting is enabled
3. Check debug output for "[UpdateComparisonFromPreviousStudy]" messages

### Tab title still shows old format
**Cause:** Code uses cached previous study tabs  
**Fix:** Reload patient (which reloads previous studies)

---

## Performance Notes

- **Modality extraction:** +1 DB query per unique studyname (cached in memory during session)
- **Impact:** Negligible for typical usage (<10 previous studies per patient)
- **Optimization:** Could batch-load all modalities in one query if needed

---

## Quick Wins

### Add LOINC Mapping
1. Open StudynameLoincWindow (menu: LOINC Mapping)
2. Select studyname
3. Add part "Rad.Modality.Modality Type" with value (CT, MR, US, etc.)
4. Save
5. Reload patient to see updated modality in tab title

### Test Comparison Update
1. Load patient with multiple previous studies
2. Select a previous study (e.g., "CT 2023-01-01")
3. Run AddPreviousStudy automation
4. Check Comparison field = "CT 2023-01-01"

---

## Code References

### Modality Extraction
- **Primary:** `MainViewModel.PreviousStudiesLoader.cs:ExtractModalityAsync`
- **Fallback:** `MainViewModel.PreviousStudiesLoader.cs:ExtractModalityFallback`
- **Called from:** `MainViewModel.PreviousStudiesLoader.cs:LoadPreviousStudiesForPatientAsync`

### Comparison Update
- **Method:** `MainViewModel.Commands.AddPreviousStudy.cs:UpdateComparisonFromPreviousStudyAsync`
- **Called from:** `MainViewModel.Commands.AddPreviousStudy.cs:RunAddPreviousStudyModuleAsync`
- **Setting check:** `IRadiumLocalSettings.DoNotUpdateHeaderInXR`

---

**Last updated:** 2025-11-09  
**Status:** ? Implemented & build successful

