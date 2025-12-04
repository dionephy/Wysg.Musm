# Enhancement: SetComparison Built-in Module (2025-12-04)

**Date**: 2025-12-04  
**Type**: Enhancement  
**Status**: ? Complete  
**Priority**: Normal

---

## Summary

Created a new built-in module "SetComparison" that sets the Comparison field using temporary variables ("Previous Study Studyname" and "Previous Study Datetime") set by custom procedures. The module extracts modality from the studyname via LOINC mapping and formats the comparison string as "{Modality} {Date}".

## User Request

> "In the Automation window -> Automation tab, i want a new built-in module 'SetComparison'. this sets comparison string using 'Previous Study Studyname' (for modality) and 'Previous Study Datetime' (for Datetime) among the fetched studies in 'PreviousReportEditorPanel.xaml'. you can refer to legacy code in 'MainViewModel.Commands.AddPreviousStudy.cs'"

## Implementation

### Files Modified

#### 1. `MainViewModel.Commands.Automation.Core.cs`
**Changes**:
- Added SetComparison module handler in `RunModulesSequentially` method

```csharp
else if (string.Equals(m, "SetComparison", StringComparison.OrdinalIgnoreCase)) 
{ 
    await RunSetComparisonAsync(); 
}
```

#### 2. `MainViewModel.Commands.Automation.Database.cs`
**Changes**:
- Added `RunSetComparisonAsync()` implementation method

**Key Features**:
- Validates that `TempPreviousStudyStudyname` and `TempPreviousStudyDatetime` are set
- Extracts modality from current study to check ModalitiesNoHeaderUpdate exclusion list
- Extracts modality from previous study studyname using `ExtractModalityAsync()` (LOINC mapping)
- Builds comparison string in format: "{Modality} {Date}"
- Updates `Comparison` property
- Provides comprehensive debug logging

**Logic Flow**:
```
1. Validate TempPreviousStudyStudyname is not empty
2. Validate TempPreviousStudyDatetime has value
3. Extract current study modality (for exclusion check)
4. Check if current modality is in ModalitiesNoHeaderUpdate list
   ¦¦¦¡> If YES: Skip update and return
5. Extract previous study modality from TempPreviousStudyStudyname
   ¦¦¦¡> Uses ExtractModalityAsync() (LOINC mapping)
   ¦¦¦¡> Falls back to "OT" if no LOINC mapping
6. Build comparison string: "{Modality} {Date}"
7. Set Comparison property
8. Status message shows success
```

#### 3. `SettingsViewModel.cs`
**Changes**:
- Added "SetComparison" to `AvailableModules` list (positioned after FetchPreviousStudies)

```csharp
private ObservableCollection<string> availableModules = new(new[] { 
    ..., "FetchPreviousStudies", "SetComparison", "GetUntilReportDateTime", ...
});
```

## Behavior

### Basic Usage (After FetchPreviousStudies or Custom Procedure)

```
Custom Procedure:
1. Set "Previous Study Studyname" = "CT Brain"
2. Set "Previous Study Datetime" = 2025-11-15 14:30:00

Automation Sequence: SetComparison

Result:
- Modality extracted from "CT Brain" via LOINC mapping (e.g., "CT")
- Comparison field set to "CT 2025-11-15"
- Status: "Comparison set: CT 2025-11-15 (XXX ms)"
```

### With FetchPreviousStudies Module

```
Custom Procedure:
1. Set "Previous Study Studyname" = "MRI Brain with contrast"
2. Set "Previous Study Datetime" = 2025-10-20 10:00:00

Automation Sequence:
1. FetchPreviousStudies (loads and selects study)
2. SetComparison (sets comparison field)

Result:
- FetchPreviousStudies loads all studies
- Selects "MRI Brain with contrast" (2025-10-20)
- SetComparison extracts modality "MR" via LOINC
- Comparison field = "MR 2025-10-20"
```

### Respecting ModalitiesNoHeaderUpdate Setting

```
Scenario: Current study is XR, ModalitiesNoHeaderUpdate includes "XR"

Custom Procedure:
1. Set "Previous Study Studyname" = "CT Chest"
2. Set "Previous Study Datetime" = 2025-11-01

Automation Sequence: SetComparison

Result:
- Current modality: "XR"
- Check ModalitiesNoHeaderUpdate: "XR" is in exclusion list
- Skip update
- Status: "Comparison not updated (modality 'XR' excluded by settings)"
```

## Temporary Variables Used

The module requires these temporary variables to be set by custom procedures:

| Variable | Type | Required | Purpose |
|----------|------|----------|---------|
| `TempPreviousStudyStudyname` | string | Yes | Studyname to extract modality from |
| `TempPreviousStudyDatetime` | DateTime? | Yes | Date to use in comparison string |

These variables are properties in `MainViewModel.CurrentStudy.cs`:
```csharp
public string? TempPreviousStudyStudyname { get; set; }
public DateTime? TempPreviousStudyDatetime { get; set; }
```

## Modality Extraction

The module uses `ExtractModalityAsync()` from `MainViewModel.PreviousStudiesLoader.cs` which:

1. **First tries LOINC mapping**:
   - Queries `med.rad_studyname_loinc_part` table
   - Looks for "Rad.Modality.Modality Type" part
   - Returns part name (e.g., "CT", "MR", "US")

2. **Falls back to "OT"** if no LOINC mapping exists:
   - Returns "OT" (Other) to indicate unmapped studyname
   - User should add LOINC mapping for proper modality

### Example LOINC Query
```sql
SELECT p.part_name
FROM med.rad_studyname s
JOIN med.rad_studyname_loinc_part m ON m.studyname_id = s.id
JOIN loinc.part p ON p.part_number = m.part_number
WHERE s.studyname = 'CT Brain'
  AND p.part_type_name = 'Rad.Modality.Modality Type';
-- Result: 'CT'
```

## Comparison with Related Features

| Feature | SetComparison | AddPreviousStudy Comparison Update | EditComparisonWindow |
|---------|---------------|-------------------------------------|----------------------|
| Data Source | Temp variables | Selected previous study | Manual selection |
| When Run | On-demand (module) | Automatic after AddPreviousStudy | Manual UI |
| Requires Study | No (uses variables) | Yes (SelectedPreviousStudy) | Yes (PreviousStudies) |
| Modality | LOINC extraction | From study tab | From study tabs |
| Format | "{Modality} {Date}" | "{Modality} {Date}" | Custom |
| Use Case | Programmatic | Automatic workflow | Manual editing |

## Use Cases

### Use Case 1: Set Comparison Without Loading Studies

```
Scenario: User has studyname and datetime from external source
Custom Procedure:
1. GetText(ExternalField) ¡æ "CT Chest"
2. Set "Previous Study Studyname" = var1
3. Set "Previous Study Datetime" = 2025-11-01

Automation Sequence: SetComparison

Result: Comparison = "CT 2025-11-01" (without loading any previous studies)
```

### Use Case 2: Use with FetchPreviousStudies

```
Scenario: Fetch studies then set comparison based on specific study
Custom Procedure:
1. Set "Previous Study Studyname" = "MRI Brain"
2. Set "Previous Study Datetime" = 2025-10-15
3. Set "Previous Study Report Datetime" = 2025-10-15 15:00:00

Automation Sequence:
1. FetchPreviousStudies (loads and selects study)
2. SetComparison (sets comparison field)

Result:
- Studies loaded and selected
- Comparison = "MR 2025-10-15"
```

### Use Case 3: Batch Processing with Multiple Comparisons

```
Scenario: Process multiple studies with different comparison values
Custom Procedure (loop):
1. Read next studyname from list ¡æ "CT Chest"
2. Set "Previous Study Studyname" = var1
3. Set "Previous Study Datetime" = currentDate - 30 days
4. SetComparison
5. Process current study
6. Repeat

Result: Each study gets appropriate comparison based on calculated date
```

## Validation Logic

### Error 1: Missing Studyname
```
Condition: TempPreviousStudyStudyname is null or whitespace
Action: Abort with error status
Status: "SetComparison: Previous Study Studyname is required"
```

### Error 2: Missing Datetime
```
Condition: TempPreviousStudyDatetime is null
Action: Abort with error status
Status: "SetComparison: Previous Study Datetime is required"
```

### Skip 3: Excluded Modality
```
Condition: Current study modality is in ModalitiesNoHeaderUpdate list
Action: Skip update with info status
Status: "Comparison not updated (modality '{modality}' excluded by settings)"
```

## Debug Logging

```
[SetComparison] ===== START =====
[SetComparison] Using studyname: 'CT Brain'
[SetComparison] Using datetime: 2025-11-15 14:30:00
[SetComparison] Current study modality: 'XR', StudyName: 'XR Chest'
[SetComparison] ModalitiesNoHeaderUpdate setting: 'XR,MG'
[SetComparison] Excluded modalities: [XR, MG]
[SetComparison] Should skip update: false
[SetComparison] Previous study modality: 'CT'
[SetComparison] Built comparison text: 'CT 2025-11-15'
[SetComparison] Updated Comparison property: 'CT 2025-11-15'
[SetComparison] ===== END: SUCCESS ===== (12 ms)
```

## Error Handling

### Error 1: Empty Studyname
```csharp
if (string.IsNullOrWhiteSpace(TempPreviousStudyStudyname))
{
    SetStatus("SetComparison: Previous Study Studyname is required", true);
    return;
}
```

### Error 2: Null Datetime
```csharp
if (!TempPreviousStudyDatetime.HasValue)
{
    SetStatus("SetComparison: Previous Study Datetime is required", true);
    return;
}
```

### Error 3: Modality Exclusion Check
```csharp
if (shouldSkipUpdate)
{
    SetStatus($"Comparison not updated (modality '{currentModality}' excluded by settings)");
    return;
}
```

### Error 4: General Exception
```csharp
catch (Exception ex)
{
    SetStatus($"SetComparison error: {ex.Message}", true);
}
```

## Performance

- **Execution Time**: ~5-50ms (depends on LOINC query cache)
- **Memory**: No additional overhead
- **Database**: 1 query if modality not cached, 0 if cached
- **LOINC Query**: Cached per studyname during session

## Testing Checklist

**Prerequisites**:
- [ ] PostgreSQL local database running with LOINC mappings
- [ ] Custom procedures can set temp variables
- [ ] ModalitiesNoHeaderUpdate setting configured (optional)

**Test Case 1: Basic Set Comparison**
1. Set TempPreviousStudyStudyname = "CT Brain"
2. Set TempPreviousStudyDatetime = 2025-11-15 14:30:00
3. Run "SetComparison" module
4. Verify Comparison field = "CT 2025-11-15"

**Test Case 2: With LOINC Mapping**
1. Add LOINC mapping: "Brain MRI" ¡æ "Rad.Modality.Modality Type" = "MR"
2. Set TempPreviousStudyStudyname = "Brain MRI"
3. Set TempPreviousStudyDatetime = 2025-10-20
4. Run "SetComparison" module
5. Verify Comparison field = "MR 2025-10-20"

**Test Case 3: Without LOINC Mapping**
1. Set TempPreviousStudyStudyname = "Unknown Study Name"
2. Set TempPreviousStudyDatetime = 2025-11-01
3. Run "SetComparison" module
4. Verify Comparison field = "OT 2025-11-01" (OT = Other)

**Test Case 4: Excluded Modality (XR)**
1. Set current study modality = "XR"
2. Add "XR" to ModalitiesNoHeaderUpdate setting
3. Set temp variables
4. Run "SetComparison" module
5. Verify Comparison field NOT updated
6. Verify status shows "excluded by settings"

**Test Case 5: Missing Studyname**
1. Clear TempPreviousStudyStudyname
2. Set TempPreviousStudyDatetime = 2025-11-01
3. Run "SetComparison" module
4. Verify error status: "Previous Study Studyname is required"

**Test Case 6: Missing Datetime**
1. Set TempPreviousStudyStudyname = "CT Chest"
2. Clear TempPreviousStudyDatetime
3. Run "SetComparison" module
4. Verify error status: "Previous Study Datetime is required"

**Test Case 7: With FetchPreviousStudies**
1. Set all three temp variables (studyname, datetime, report datetime)
2. Run "FetchPreviousStudies" module
3. Run "SetComparison" module
4. Verify Comparison field set correctly
5. Verify selected study visible in UI

## Configuration

### ModalitiesNoHeaderUpdate Setting
- **Location**: Settings ¡æ General
- **Format**: Comma or semicolon separated modality codes
- **Example**: "XR,MG,NM"
- **Effect**: Skips comparison update if current study modality is in this list
- **Use Case**: Prevent automatic comparison updates for certain modalities

## Benefits

### For Users
- **Programmatic Control**: Set comparison via automation without UI interaction
- **Flexible**: Works with any studyname/datetime from any source
- **LOINC Integration**: Uses standard LOINC modality mappings
- **Exclusion Support**: Respects modality exclusion settings

### For Developers
- **Reusability**: Module can be called from any automation sequence
- **Testability**: Can test independently with known temp variables
- **Maintainability**: Uses existing `ExtractModalityAsync()` logic
- **Extensibility**: Easy to add more formatting options

## Related Modules

- **FetchPreviousStudies**: Loads previous studies and can set temp variables
- **AddPreviousStudy**: Automatically updates comparison using selected study
- **EditComparisonWindow**: Manual UI for editing comparison field

## Legacy Code Reference

The implementation is based on `UpdateComparisonFromPreviousStudyAsync()` from `MainViewModel.Commands.AddPreviousStudy.cs`, which:
- Extracts modality from studyname using LOINC mapping
- Checks ModalitiesNoHeaderUpdate exclusion list
- Formats comparison as "{Modality} {Date}"
- Updates Comparison property

**Key Differences**:
- AddPreviousStudy: Uses `SelectedPreviousStudy` (in-memory study object)
- SetComparison: Uses temp variables (can work without loaded studies)

## Future Enhancements

Potential improvements:
1. Add parameter to specify comparison format (e.g., include studyname)
2. Add support for multiple previous studies (comma-separated)
3. Add validation against fetched previous studies
4. Add option to append vs overwrite comparison field
5. Add support for custom modality aliases

## Known Limitations

1. **Temp Variable Dependency**: Requires custom procedures to set temp variables
2. **LOINC Coverage**: Modality extraction returns "OT" if no LOINC mapping exists
3. **Single Comparison**: Only sets one comparison value (not multiple)
4. **Overwrite Only**: Always overwrites Comparison field (no append option)

## Notes

- The module does NOT load previous studies (use FetchPreviousStudies for that)
- The module does NOT validate that the study exists in database
- The temp variables persist until cleared or overwritten
- LOINC modality extraction is cached per studyname during session

## Files Summary

### Created
- `apps/Wysg.Musm.Radium/docs/00-current/ENHANCEMENT_2025-12-04_SetComparison.md` (this document)

### Modified
- `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.Automation.Core.cs` - Added module handler
- `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.Automation.Database.cs` - Added implementation
- `apps/Wysg.Musm.Radium/ViewModels/SettingsViewModel.cs` - Added to AvailableModules

## Build Status

? **Build Successful** - No errors, no warnings

---

**Implementation Date**: 2025-12-04  
**Build Status**: ? Success  
**Ready for Use**: ? Complete

---

*New built-in module successfully implemented. Users can now set comparison field programmatically using temp variables with LOINC-based modality extraction.*
