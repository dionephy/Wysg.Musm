# Enhancement: Edit Comparison Window

**Date**: 2025-01-29  
**Type**: Enhancement  
**Status**: ? Implemented

---

## Overview

Added a new "Edit Comparison" feature that allows users to manage which previous studies are included in the comparison field. The feature opens a dedicated window where users can add/remove previous studies and optionally add LOINC mappings for studynames that don't have them.

---

## User Story

**As a radiologist**, I want to easily manage which previous studies are included in my comparison field, so that I can accurately document which prior imaging I am comparing against in my current report.

---

## Features

### 1. Edit Comparison Window
- **Access**: Click "Edit Comparison" button in the top grid (next to "Edit Study Technique")
- **Requirement**: Patient must be locked and have previous studies available
- **Layout**: Two-column interface with available studies on left and selected studies on right

### 2. Previous Studies Management
- **Available Studies List**: Shows all previous studies for the current patient
  - Displays in format: "{Modality} {Date}" (e.g., "CT 2024-01-15")
  - Sorted by study date (most recent first)
  - "Add" button to select for comparison
  - "Map LOINC" button appears if studyname has no LOINC mapping (orange warning button)
  
- **Selected Studies List**: Shows studies currently in comparison
  - Same display format as available list
  - "Remove" button to deselect from comparison
  - Order preserved based on original study dates

### 3. LOINC Mapping Integration
- Studies without LOINC mappings are flagged with an orange "Map LOINC" button
- Clicking the button opens the Studyname-LOINC mapping window pre-selected to that studyname
- Allows users to add modality mappings on-the-fly without leaving the comparison editor
- LOINC map status checked asynchronously after window opens

### 4. Comparison String Generation
- Automatically generates comparison string as: "{Modality} {Date}, {Modality} {Date}"
- Updates in real-time as studies are added/removed
- Sorted by study date (most recent first)
- Example: "CT 2024-01-15, MR 2024-01-10"

### 5. Live Preview
- Read-only textbox shows the generated comparison string
- Updates immediately when selection changes
- Final string is applied to current report's Comparison field when user clicks OK

---

## User Interface

### Window Layout
```
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛  Edit Comparison                                                     弛
戍式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛                                                                       弛
弛  忙式式式式式式式式式式式式式式式式式式式式式式式式式忖   ?   忙式式式式式式式式式式式式式式式式式式式式式式式式式忖      弛
弛  弛 Available Previous      弛        弛 Selected for           弛      弛
弛  弛 Studies                 弛        弛 Comparison             弛      弛
弛  戍式式式式式式式式式式式式式式式式式式式式式式式式式扣        戍式式式式式式式式式式式式式式式式式式式式式式式式式扣      弛
弛  弛 CT 2024-01-15 [Add]     弛        弛 MR 2024-01-10 [Remove] 弛      弛
弛  弛 [Map LOINC]             弛        弛                         弛      弛
弛  戍式式式式式式式式式式式式式式式式式式式式式式式式式扣        戌式式式式式式式式式式式式式式式式式式式式式式式式式戎      弛
弛  弛 US 2023-12-20 [Add]     弛                                         弛
弛  戌式式式式式式式式式式式式式式式式式式式式式式式式式戎                                         弛
弛                                                                       弛
弛  Comparison String Preview:                                          弛
弛  忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖        弛
弛  弛 MR 2024-01-10                                            弛        弛
弛  戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎        弛
弛                                                                       弛
弛  Info: Studies without LOINC maps show a 'Map LOINC' button...       弛
弛                                                                       弛
弛                                                [OK] [Cancel]          弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
```

### Visual Design
- **Dark Theme**: Consistent with main application (#1E1E1E background)
- **Button Colors**:
  - Add: Blue (#0E639C)
  - Remove: Red (#DC2626)
  - Map LOINC: Orange (#D97706) - warning color to draw attention
- **Layout**: Responsive two-column grid with centered arrow icon
- **Size**: 800x600 pixels, resizable

---

## Implementation Details

### New Files Created

#### 1. `EditComparisonViewModel.cs`
**Location**: `apps\Wysg.Musm.Radium\ViewModels\EditComparisonViewModel.cs`

**Key Features**:
- Loads existing previous studies from MainViewModel
- Parses current comparison string to determine selected studies
- Maintains two collections: `AvailableStudies` and `SelectedStudies`
- Async LOINC map checking via `IStudynameLoincRepository`
- Real-time comparison string generation

**Properties**:
```csharp
public ObservableCollection<ComparisonStudyItem> AvailableStudies { get; }
public ObservableCollection<ComparisonStudyItem> SelectedStudies { get; }
public string ComparisonString { get; set; }
```

**Commands**:
```csharp
public ICommand AddStudyCommand { get; }         // Add study to selected list
public ICommand RemoveStudyCommand { get; }      // Remove study from selected list
public ICommand OpenStudynameMapCommand { get; } // Open LOINC mapping window
```

**Study Item Model**:
```csharp
public class ComparisonStudyItem
{
    public DateTime StudyDateTime { get; set; }
    public string Modality { get; set; }
    public string Studyname { get; set; }
    public string DisplayText { get; set; }  // "{Modality} {Date}"
    public bool IsSelected { get; set; }
    public bool HasLoincMap { get; set; }
}
```

#### 2. `EditComparisonWindow.xaml`
**Location**: `apps\Wysg.Musm.Radium\Views\EditComparisonWindow.xaml`

**Features**:
- Two-column layout with GridSplitter
- Dark theme styling for all controls
- Custom button styles with hover effects
- `InvertedBooleanToVisibilityConverter` for conditional UI

#### 3. `EditComparisonWindow.xaml.cs`
**Location**: `apps\Wysg.Musm.Radium\Views\EditComparisonWindow.xaml.cs`

**Key Methods**:
```csharp
public static string? Open(...)  // Opens window and returns updated comparison string
private void OnOkClick(...)      // Confirms changes
private void OnCancelClick(...)  // Discards changes
```

### Modified Files

#### 1. `MainViewModel.Commands.cs`
**Changes**:
- Added `EditComparisonCommand` property
- Initialized command in `InitializeCommands()`
- Implemented `OnEditComparison()` handler
  - Validates patient is loaded and has previous studies
  - Opens `EditComparisonWindow.Open()` with current context
  - Updates `Comparison` property if user clicks OK

**Command Definition**:
```csharp
public ICommand EditComparisonCommand { get; private set; } = null!;
// Enabled only when PatientLocked is true
EditComparisonCommand = new DelegateCommand(_ => OnEditComparison(), _ => PatientLocked);
```

#### 2. `ReportInputsAndJsonPanel.xaml`
**Changes**:
- Bound "Edit Comparison" button to `EditComparisonCommand`
- Button located in Row 0, Column 0 (top grid header area)

---

## Technical Flow

### Opening the Window
1. User clicks "Edit Comparison" button (enabled when PatientLocked=true)
2. `MainViewModel.OnEditComparison()` validates:
   - Patient number is not empty
   - Previous studies collection has items
3. Creates `EditComparisonViewModel` with:
   - Patient context (number, name, sex)
   - List of existing `PreviousStudyTab` objects
   - Current comparison string
4. Opens `EditComparisonWindow` as modal dialog

### Loading Studies
1. ViewModel iterates through `PreviousStudies` collection
2. Creates `ComparisonStudyItem` for each study with:
   - `StudyDateTime` from tab
   - `Modality` from tab (extracted from studyname)
   - `Studyname` from selected report
   - `DisplayText` formatted as "{Modality} {Date}"
3. Parses current comparison string to identify selected studies
4. Marks matching studies as `IsSelected=true` and adds to `SelectedStudies`
5. Asynchronously checks LOINC maps for all unique studynames

### Parsing Comparison String
```csharp
// Input: "CT 2024-01-15, MR 2024-01-10"
// Splits by comma, extracts modality and date from each entry
// Creates HashSet of "Modality|Date" keys for fast lookup
// Example: {"CT|2024-01-15", "MR|2024-01-10"}
```

### LOINC Map Checking
```csharp
// 1. Get unique studynames from AvailableStudies
// 2. Query IStudynameLoincRepository.GetMappedStudynamesAsync()
// 3. Create HashSet of mapped studynames
// 4. Update each ComparisonStudyItem.HasLoincMap property
// 5. UI shows/hides "Map LOINC" button based on this flag
```

### Adding a Study
1. User clicks "Add" button next to a study
2. `AddStudyCommand` executes
3. Sets `study.IsSelected = true`
4. Adds study to `SelectedStudies` collection
5. Calls `UpdateComparisonString()` to regenerate

### Removing a Study
1. User clicks "Remove" button next to a study
2. `RemoveStudyCommand` executes
3. Sets `study.IsSelected = false`
4. Removes study from `SelectedStudies` collection
5. Calls `UpdateComparisonString()` to regenerate

### Mapping LOINC
1. User clicks "Map LOINC" button (only visible when `HasLoincMap=false`)
2. `OpenStudynameMapCommand` executes
3. Opens `StudynameLoincWindow.Open(studyname)` pre-selected
4. User can add LOINC parts and save
5. Returns to Edit Comparison window (LOINC status not re-checked automatically)

### Generating Comparison String
```csharp
// 1. Order SelectedStudies by StudyDateTime (descending)
// 2. Format each as "{Modality} {Date:yyyy-MM-dd}"
// 3. Join with ", " separator
// Example: "CT 2024-01-15, MR 2024-01-10, US 2023-12-20"
```

### Saving Changes
1. User clicks "OK" button
2. `DialogResult = true`
3. Window closes
4. `MainViewModel.OnEditComparison()` receives new comparison string
5. Updates `Comparison` property (triggers header rebuild)
6. Sets status: "Comparison updated"

---

## Dependencies

### NuGet Packages
- None (uses existing Radium dependencies)

### Internal Dependencies
- `IStudynameLoincRepository` - For LOINC map checking
- `MainViewModel.PreviousStudyTab` - Source of previous studies data
- `Views.StudynameLoincWindow` - For adding LOINC mappings
- `MainViewModel.ExtractModality()` - For extracting modality from studyname

---

## Usage Scenarios

### Scenario 1: Standard Comparison Setup
```
1. User opens patient with 3 previous studies:
   - CT 2024-01-15
   - MR 2024-01-10
   - US 2023-12-20

2. User clicks "Edit Comparison"

3. Window shows:
   Available: All 3 studies
   Selected: None (initially empty)

4. User adds CT and MR studies

5. Comparison string: "CT 2024-01-15, MR 2024-01-10"

6. User clicks OK

7. Comparison field in report updated
```

### Scenario 2: Adding LOINC Mapping Mid-Workflow
```
1. User opens Edit Comparison window

2. Sees "MRI HEAD" study with orange "Map LOINC" button

3. Clicks "Map LOINC"

4. Studyname-LOINC window opens pre-selected to "MRI HEAD"

5. User adds LOINC parts for MR modality mapping

6. Saves and closes LOINC window

7. Returns to Edit Comparison window

8. Continues selecting studies for comparison
```

### Scenario 3: Updating Existing Comparison
```
1. Current report has: "CT 2024-01-15, MR 2024-01-10"

2. User clicks "Edit Comparison"

3. Window shows:
   Available: All studies
   Selected: CT and MR (parsed from comparison string)

4. User removes MR study

5. Comparison string updates: "CT 2024-01-15"

6. User adds US study

7. Final comparison: "CT 2024-01-15, US 2023-12-20"

8. User clicks OK
```

---

## Error Handling

### No Patient Loaded
```
Condition: PatientNumber is null or empty
Action: Show status message "No patient loaded - cannot edit comparison"
UI: Button remains disabled (PatientLocked=false)
```

### No Previous Studies
```
Condition: PreviousStudies collection is empty
Action: Show status message "No previous studies available for this patient"
UI: Window does not open
```

### LOINC Map Check Failure
```
Condition: Exception during GetMappedStudynamesAsync()
Action: Log error to Debug output
UI: All studies show "Map LOINC" button (fail-safe)
```

### Comparison String Parsing Failure
```
Condition: Invalid format in existing comparison string
Action: Empty HashSet (no studies pre-selected)
UI: User can still select studies manually
```

---

## Testing

### Manual Testing Checklist
- [ ] Window opens when Edit Comparison button clicked (PatientLocked=true)
- [ ] All previous studies appear in Available list
- [ ] Studies are sorted by date (most recent first)
- [ ] Existing comparison string is parsed correctly (studies pre-selected)
- [ ] Add button moves study to Selected list
- [ ] Remove button moves study back to Available list
- [ ] Comparison string updates in real-time
- [ ] Map LOINC button appears only for unmapped studynames
- [ ] Map LOINC button opens StudynameLoincWindow with correct studyname
- [ ] OK button updates Comparison field in main report
- [ ] Cancel button discards changes
- [ ] Window disabled when PatientLocked=false
- [ ] Window disabled when no previous studies exist
- [ ] Format is "{Modality} {Date}" (e.g., "CT 2024-01-15")

### Edge Cases
- [ ] Patient with no previous studies
- [ ] Patient with 10+ previous studies
- [ ] All studies already selected
- [ ] No studies selected (empty comparison)
- [ ] Studyname with no modality pattern (shows "OT" - Other)
- [ ] Multiple studies on same date
- [ ] Studies with same modality but different dates

---

## Known Limitations

### 1. LOINC Map Status Not Auto-Refreshed
After mapping a studyname via "Map LOINC" button, the orange button doesn't automatically hide. User must close and reopen the Edit Comparison window to see updated status.

**Workaround**: Refresh window or check LOINC status manually.

**Future**: Add refresh mechanism or subscribe to LOINC repository change events.

### 2. No Manual Comparison Text Editing
The comparison string is generated automatically from selected studies. Users cannot manually type arbitrary comparison text.

**Rationale**: Ensures consistency between selected studies and comparison string.

**Future**: Consider adding a "Custom" option for free-text comparison.

### 3. Modality Extraction Limited
Modality extraction relies on regex pattern matching in studyname. Uncommon modalities may not be recognized correctly.

**Current Fallback**: Unrecognized modalities default to "OT" (Other).

**Future**: Enhance modality extraction or allow manual modality selection.

---

## Future Enhancements

### 1. Drag-and-Drop Reordering
Allow users to reorder selected studies by dragging them in the list (affects comparison string order).

### 2. Study Details Preview
Show additional study metadata on hover or selection:
- Study description
- Body part examined
- Number of images
- Reporting radiologist

### 3. Bulk Selection
Add "Select All" / "Clear All" buttons for quick management.

### 4. Study Filtering
Add search/filter box to find specific studies by modality, date range, or description.

### 5. Comparison Templates
Save and load common comparison configurations (e.g., "Chest CT + prior Chest CTs").

### 6. Auto-Select Recent Studies
Option to automatically select N most recent studies of specific modality.

---

## Cross-References

### Related Features
- **StudynameLoincWindow**: LOINC mapping interface
- **PreviousStudiesLoader**: Source of previous study data
- **ExtractModality**: Modality extraction logic (in `MainViewModel.ReportifyHelpers.cs`)

### Related Commands
- `EditStudyTechniqueCommand`: Edit study technique combinations
- `AddStudyCommand`: Add previous study from automation

### Database Schema
- `med.rad_study`: Study records (source of previous studies)
- `med.rad_report`: Report records (metadata for each report)
- `med.studyname_loinc_part`: LOINC mappings (checked for Map button)

---

## Build Verification

? Build Status: **Success**  
? Compilation Errors: **None**  
? New Files: 3  
? Modified Files: 2  
? Lines Added: ~400 lines

### Files Created
1. `apps\Wysg.Musm.Radium\ViewModels\EditComparisonViewModel.cs` (200 lines)
2. `apps\Wysg.Musm.Radium\Views\EditComparisonWindow.xaml` (180 lines)
3. `apps\Wysg.Musm.Radium\Views\EditComparisonWindow.xaml.cs` (70 lines)

### Files Modified
1. `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Commands.cs` (+40 lines)
2. `apps\Wysg.Musm.Radium\Controls\ReportInputsAndJsonPanel.xaml` (+1 line)

---

*Enhancement implemented by GitHub Copilot on 2025-01-29*
