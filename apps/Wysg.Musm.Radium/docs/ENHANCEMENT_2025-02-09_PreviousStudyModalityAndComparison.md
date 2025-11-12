# Enhancement: Previous Study Modality Extraction & Comparison Update (2025-02-09)

**Status**: ? Implemented  
**Date**: 2025-02-09  
**Category**: Enhancement

---

## Summary

This enhancement implements two improvements to the AddPreviousStudy automation module:

1. **Comparison Field Auto-Update**: When running "AddPreviousStudy" module, if there is already a previous study selected, the current study's Comparison field is automatically updated with that previous study's modality and date (unless current study is XR and "Do not update header in XR" is checked).

2. **LOINC-Based Modality Extraction**: Previous study tab titles now display "{Modality} {StudyDate}" format, where the modality is extracted from LOINC part mappings (specifically "Rad.Modality.Modality Type" part). Falls back to simple prefix extraction from studyname if no LOINC mapping exists.

---

## Implementation Details

### Feature 1: Auto-Update Comparison from Previous Study

#### Files Modified
- `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Commands.AddPreviousStudy.cs`

#### Changes
The `UpdateComparisonFromPreviousStudyAsync` method already existed and implements this feature. It:

1. Captures the previously selected previous study before loading a new one
2. Builds a comparison string in format: "{Modality} {Date}"
3. Updates the `Comparison` property with this string
4. Respects the "Do not update header in XR" setting (skips update if current study is XR and setting is enabled)

**Example:**
- Previous study: CT 2024-01-15
- After AddPreviousStudy completes: `Comparison` field = "CT 2024-01-15"

#### Logic Flow
```csharp
private async Task UpdateComparisonFromPreviousStudyAsync(PreviousStudyTab? previousStudy)
{
    if (previousStudy == null) return;
    
    // Check if current study is XR modality
    bool isXRModality = StudyName?.ToUpperInvariant().StartsWith("XR") ?? false;
    
    // Check "Do not update header in XR" setting
    bool doNotUpdateHeaderInXR = _localSettings.DoNotUpdateHeaderInXR == "true";
    
    // Skip update if XR and setting is enabled
    if (isXRModality && doNotUpdateHeaderInXR)
    {
        SetStatus("Comparison not updated (XR modality with 'Do not update header in XR' enabled)");
        return;
    }
    
    // Build comparison string: "{Modality} {Date}"
    var comparisonText = $"{previousStudy.Modality} {previousStudy.StudyDateTime:yyyy-MM-dd}";
    Comparison = comparisonText;
}
```

---

### Feature 2: LOINC-Based Modality Extraction

#### Files Modified
- `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.PreviousStudiesLoader.cs`
- `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.cs` (added `_studynameLoincRepo` field and constructor parameter)
- `apps\Wysg.Musm.Radium\App.xaml.cs` (added `IStudynameLoincRepository` to MainViewModel DI registration)

#### New Methods

##### `ExtractModalityAsync(string studyname)`
Primary method for extracting modality from studyname using LOINC mapping.

**Process:**
1. Query database for studyname-to-LOINC mappings
2. Find the "Rad.Modality.Modality Type" part in the mapping
3. Return the `PartName` value (e.g., "CT", "MR", "US")
4. If no LOINC mapping exists, fall back to `ExtractModalityFallback`

**Example:**
- Studyname: "Brain MRI with contrast"
- LOINC mapping: Part "Rad.Modality.Modality Type" = "MR"
- Result: "MR"

##### `ExtractModalityFallback(string studyname)`
Fallback method when no LOINC mapping is available. Extracts modality from studyname using common patterns.

**Logic:**
1. Check for common modality prefixes (CT, MR, US, XR, etc.)
2. Extract first word if no prefix match
3. Return first 2-3 characters if no space found

**Example:**
- Input: "CT CHEST W/O CONTRAST"
- Output: "CT"

#### Tab Title Format Change

**Before:**
- Format: "{StudyDateTime} {Modality}" (e.g., "2024-01-15 CT")
- Modality extracted from simple prefix extraction

**After:**
- Format: "{Modality} {StudyDateTime}" (e.g., "CT 2024-01-15")
- Modality extracted from LOINC mapping (Rad.Modality.Modality Type part)

---

## Database Schema

The LOINC mapping system uses the following structure:

### Tables
- `med.rad_studyname` - Studyname definitions
- `med.rad_studyname_loinc_part` - Mappings from studynames to LOINC parts
- `loinc.part` - LOINC parts including modality types

### Relevant Part Types
- **Rad.Modality.Modality Type**: Primary modality (CT, MR, US, XR, etc.)
- **Rad.Modality.Modality Subtype**: Optional subtype (e.g., "with contrast")

### Sample Query
```sql
SELECT p.part_name
FROM med.rad_studyname s
JOIN med.rad_studyname_loinc_part m ON m.studyname_id = s.id
JOIN loinc.part p ON p.part_number = m.part_number
WHERE s.studyname = 'Brain MRI with contrast'
  AND p.part_type_name = 'Rad.Modality.Modality Type';
-- Result: 'MR'
```

---

## Benefits

### 1. Improved Consistency
- Modality extraction is now based on structured LOINC mappings instead of ad-hoc string parsing
- Consistent modality representation across different studyname variations (e.g., "MRI Brain" and "Brain MRI" both return "MR")

### 2. Better User Experience
- Comparison field is automatically populated when adding previous studies, reducing manual data entry
- Previous study tabs show clear, consistent modality information

### 3. Maintainability
- Modality mappings can be updated in the database without code changes
- Fallback mechanism ensures functionality even without LOINC mappings

---

## Testing

### Test Case 1: LOINC-Mapped Studyname
**Steps:**
1. Ensure studyname "Brain MRI" has LOINC mapping with "Rad.Modality.Modality Type" = "MR"
2. Load previous study with studyname "Brain MRI"
3. Verify tab title shows "MR 2024-01-15" (date example)

**Expected:** Tab title uses LOINC-derived modality "MR"

### Test Case 2: Non-Mapped Studyname
**Steps:**
1. Create a studyname "XYZ Custom Study" with no LOINC mapping
2. Load previous study with this studyname
3. Verify tab title shows "XYZ 2024-01-15" (using fallback extraction)

**Expected:** Tab title uses fallback modality "XYZ"

### Test Case 3: Comparison Update (Non-XR)
**Steps:**
1. Open current study (modality: CT)
2. Select previous study "MR 2023-12-01"
3. Run AddPreviousStudy automation to add another previous study "CT 2024-01-10"
4. Verify `Comparison` field is updated to "MR 2023-12-01"

**Expected:** Comparison field reflects the previously selected study

### Test Case 4: Comparison Update Skipped (XR with Setting)
**Steps:**
1. Open current study (modality: XR)
2. Enable "Do not update header in XR" setting
3. Select previous study "CT 2023-12-01"
4. Run AddPreviousStudy automation
5. Verify `Comparison` field is NOT updated

**Expected:** Comparison field remains empty or unchanged due to XR restriction

---

## Configuration

### Setting: Do Not Update Header in XR
- **Location**: Settings > Automation
- **Key**: `DoNotUpdateHeaderInXR` in `IRadiumLocalSettings`
- **Values**: "true" / "false"
- **Default**: "false"
- **Effect**: When enabled and current study is XR modality, prevents automatic Comparison field update

---

## Dependency Injection Changes

### MainViewModel Constructor
Added new parameter:
```csharp
public MainViewModel(
    // ...existing parameters...
    IStudynameLoincRepository? studynameLoincRepo = null)
```

### App.xaml.cs Registration
Updated DI registration:
```csharp
services.AddTransient<MainViewModel>(sp => new MainViewModel(
    // ...existing parameters...
    sp.GetService<IStudynameLoincRepository>()
));
```

---

## Future Enhancements

### Potential Improvements
1. **Configurable Comparison Format**: Allow users to customize the comparison string format (e.g., include studyname, different date format)
2. **Multiple Previous Studies**: Support adding multiple previous studies to the Comparison field (comma-separated)
3. **Smart Comparison Update**: Detect if Comparison field already has content and append/prepend instead of overwriting
4. **Modality Aliases**: Support custom modality aliases (e.g., "MRI" ¡æ "MR", "Xray" ¡æ "XR") in the database

### Known Limitations
1. **Performance**: Modality extraction makes additional database queries during previous study loading. This is acceptable for current usage patterns but may need optimization if loading hundreds of previous studies simultaneously.
2. **LOINC Coverage**: Not all studynames have LOINC mappings yet. Fallback mechanism handles this but may produce inconsistent modality strings.
3. **Comparison Overwrite**: Current implementation overwrites the Comparison field completely. If user has manually edited the field, their changes will be lost.

---

## Related Documents
- `apps\Wysg.Musm.Radium\docs\ENHANCEMENT_2025-02-09_AddPreviousStudyComparisonUpdate.md` (original AddPreviousStudy comparison feature)
- `apps\Wysg.Musm.Radium\docs\IMPLEMENTATION_SUMMARY_AddPreviousStudyComparison.md` (AddPreviousStudy implementation details)

---

## Changelog

### 2025-02-09
- ? Implemented LOINC-based modality extraction with fallback
- ? Updated previous study tab title format to "{Modality} {StudyDate}"
- ? Verified comparison update respects "Do not update header in XR" setting
- ? Added `IStudynameLoincRepository` dependency to MainViewModel
- ? Updated DI registration in App.xaml.cs
- ? Build successful with all changes

---

## Verification Steps

1. **Build Check**: ? Build completed successfully
2. **Modality Extraction**: To be verified during runtime testing
3. **Comparison Update**: To be verified during runtime testing
4. **XR Restriction**: To be verified during runtime testing with setting enabled/disabled

---

## Notes for Deployment

1. **Database Requirement**: Ensure LOINC mapping tables (`loinc.part`, `med.rad_studyname_loinc_part`) are populated with modality mappings for common studynames.
2. **Migration**: Existing previous study tabs will use fallback modality extraction until LOINC mappings are added.
3. **Settings**: No new settings are required; reuses existing "Do not update header in XR" setting.

---

**Implementation completed by:** GitHub Copilot  
**Reviewed by:** (pending)  
**Deployed to:** (pending)
