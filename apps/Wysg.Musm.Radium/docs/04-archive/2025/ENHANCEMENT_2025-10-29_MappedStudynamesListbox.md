# Enhancement: Mapped Studynames Listbox in StudynameLoincWindow
**Date**: 2025-01-29  
**Feature**: Mapped Studynames Quick Copy  
**Type**: Enhancement  
**Status**: Complete ?

---

## Overview

Added two new listboxes below the playbook suggestions in the StudynameLoincWindow that allow users to:
1. See all studynames that have LOINC part mappings
2. View the mapped parts when a studyname is selected
3. Copy all parts and their sequence orders to the current studyname by double-clicking

---

## User Story

**As a** radiologist configuring LOINC mappings  
**I want to** quickly copy LOINC part mappings from existing studynames  
**So that** I can save time when creating similar studyname mappings

---

## UI Layout

### Before
```
����������������������������������������������������������������������������������������������������������������������
�� Preview                                                 ��
����������������������������������������������������������������������������������������������������������������������
�� [SelectedParts ListBox]                                 ��
����������������������������������������������������������������������������������������������������������������������
�� Playbook Suggestions                                    ��
����������������������������������������������������������������������������������������������������������������������
�� Playbook        �� Playbook Parts                        ��
�� Matches         ��                                       ��
����������������������������������������������������������������������������������������������������������������������
```

### After
```
����������������������������������������������������������������������������������������������������������������������
�� Preview                                                 ��
����������������������������������������������������������������������������������������������������������������������
�� [SelectedParts ListBox]                                 ��
����������������������������������������������������������������������������������������������������������������������
�� Playbook Suggestions                                    ��
����������������������������������������������������������������������������������������������������������������������
�� Playbook        �� Playbook Parts                        ��
�� Matches         ��                                       ��
����������������������������������������������������������������������������������������������������������������������
�� Mapped Studynames                                       ��
����������������������������������������������������������������������������������������������������������������������
�� Mapped          �� Mapped Parts                          ��
�� Studynames      ��                                       ��
�� (lstMapped-     �� (shows sequence + part display)       ��
�� Studynames)     ��                                       ��
����������������������������������������������������������������������������������������������������������������������
```

---

## Features

### 1. Mapped Studynames List
- **Location**: Bottom-left panel
- **Name**: `lstMappedStudynames`
- **Content**: Displays all studynames that have at least one LOINC part mapping
- **Sorting**: Alphabetically by studyname
- **Behavior**: 
  - Auto-loads when window opens
  - Refreshes after saving mappings

### 2. Mapped Parts Display
- **Location**: Bottom-right panel
- **Content**: Shows all parts mapped to the selected studyname
- **Format**: `[SequenceOrder] [PartDisplay]`
- **Example**: 
  ```
  A Chest
  B X-ray
  C Lateral view
  ```

### 3. Double-Click Copy
- **Action**: Double-click on a studyname in `lstMappedStudynames`
- **Result**: 
  - **Clears** all current parts in the Preview (SelectedParts)
  - **Copies** all parts from the selected mapped studyname
  - **Preserves** sequence orders
- **Use Case**: Quickly replicate LOINC mappings from similar studynames

---

## Technical Implementation

### Database Changes

#### New Repository Method
```csharp
// Interface: IStudynameLoincRepository
Task<IReadOnlyList<StudynameRow>> GetMappedStudynamesAsync();
```

#### Implementation
```sql
-- PostgreSQL query (tenant-aware)
SELECT DISTINCT s.id, s.studyname 
FROM med.rad_studyname s
INNER JOIN med.rad_studyname_loinc_part m ON m.studyname_id = s.id
WHERE s.tenant_id = @tid  -- if tenant mode
ORDER BY s.studyname
```

### ViewModel Changes

#### New Properties
```csharp
public ObservableCollection<StudynameItem> MappedStudynames { get; }
public StudynameItem? SelectedMappedStudyname { get; set; }
public ObservableCollection<MappingPreviewItem> MappedStudynameParts { get; }
```

#### New Methods
```csharp
private async Task LoadMappedStudynamesAsync()
private async Task LoadMappedStudynamePartsAsync(StudynameItem? item)
```

#### Modified Methods
```csharp
public async Task LoadAsync()
    // Added: await LoadMappedStudynamesAsync();

private async Task SaveAsync()
    // Added: await LoadMappedStudynamesAsync();
    // Reason: Refresh list after saving new mappings
```

### XAML Changes

#### New Grid Layout
```xaml
<Grid Grid.Row="1">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>       <!-- Header -->
        <RowDefinition Height="*"/>          <!-- Playbook -->
        <RowDefinition Height="6"/>          <!-- Splitter -->
        <RowDefinition Height="Auto"/>       <!-- Header -->
        <RowDefinition Height="*"/>          <!-- Mapped -->
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
    
    <!-- Playbook Suggestions (rows 0-1) -->
    <!-- Mapped Studynames (rows 3-4) -->
</Grid>
```

### Code-Behind Changes

#### New Event Handler
```csharp
private async void OnMappedStudynameDoubleClick(object sender, MouseButtonEventArgs e)
{
    // 1. Load parts if different studyname selected
    // 2. Wait for parts to load (defensive loop)
    // 3. Clear current SelectedParts
    // 4. Copy all parts with sequence orders
}
```

---

## Usage Example

### Scenario: Copy MRI Brain mappings to MRI Head

1. **Open Window**: Select "MRI Head" in left panel
2. **View Mapped**: Scroll to "Mapped Studynames" section
3. **Find Similar**: Locate "MRI Brain" in lstMappedStudynames
4. **Preview**: Click "MRI Brain" to see its parts in right panel
   ```
   A Brain
   B MRI
   C Axial
   ```
5. **Copy**: Double-click "MRI Brain"
6. **Result**: All parts copied to Preview with sequence orders
7. **Customize**: Modify as needed (e.g., change "Brain" to "Head")
8. **Save**: Click "Save" button

---

## Data Flow

```
����������������������������������������������������������������������������������������������������������������������
�� Window Load                                             ��
����������������������������������������������������������������������������������������������������������������������
�� 1. LoadAsync()                                          ��
��    ������ GetStudynamesAsync()                             ��
��    ������ GetMappedStudynamesAsync()  �� NEW                ��
����������������������������������������������������������������������������������������������������������������������

����������������������������������������������������������������������������������������������������������������������
�� User Selects Mapped Studyname                           ��
����������������������������������������������������������������������������������������������������������������������
�� 1. SelectedMappedStudyname property setter              ��
�� 2. LoadMappedStudynamePartsAsync()  �� NEW               ��
��    ������ GetPartsAsync()                                  ��
��    ������ GetMappingsAsync(studynameId)                    ��
����������������������������������������������������������������������������������������������������������������������

����������������������������������������������������������������������������������������������������������������������
�� User Double-Clicks Mapped Studyname                     ��
����������������������������������������������������������������������������������������������������������������������
�� 1. OnMappedStudynameDoubleClick()  �� NEW                ��
�� 2. Wait for parts to load (async)                       ��
�� 3. SelectedParts.Clear()                                ��
�� 4. Copy all MappedStudynameParts �� SelectedParts        ��
����������������������������������������������������������������������������������������������������������������������

����������������������������������������������������������������������������������������������������������������������
�� User Saves Mappings                                     ��
����������������������������������������������������������������������������������������������������������������������
�� 1. SaveAsync()                                          ��
�� 2. SaveMappingsAsync()                                  ��
�� 3. LoadMappedStudynamesAsync()  �� Refresh list          ��
����������������������������������������������������������������������������������������������������������������������
```

---

## Files Modified

### Backend
- `apps\Wysg.Musm.Radium\Services\IStudynameLoincRepository.cs` - Added GetMappedStudynamesAsync()
- `apps\Wysg.Musm.Radium\Services\StudynameLoincRepository.cs` - Implemented GetMappedStudynamesAsync()

### ViewModel
- `apps\Wysg.Musm.Radium\ViewModels\StudynameLoincViewModel.cs`
  - Added properties: MappedStudynames, SelectedMappedStudyname, MappedStudynameParts
  - Added methods: LoadMappedStudynamesAsync(), LoadMappedStudynamePartsAsync()
  - Modified methods: LoadAsync(), SaveAsync()

### View
- `apps\Wysg.Musm.Radium\Views\StudynameLoincWindow.xaml`
  - Reorganized right panel into 2x2 grid
  - Added "Mapped Studynames" section with two listboxes
  - Added headers for clarity

### Code-Behind
- `apps\Wysg.Musm.Radium\Views\StudynameLoincWindow.xaml.cs`
  - Added event handler: OnMappedStudynameDoubleClick()

---

## Testing

### Manual Test Cases

#### TC1: Mapped List Population
1. Open StudynameLoincWindow
2. Scroll to "Mapped Studynames" section
3. **Expected**: List shows only studynames with mappings
4. **Expected**: List is alphabetically sorted

#### TC2: Parts Display
1. Click on a studyname in lstMappedStudynames
2. **Expected**: Right panel shows all parts with sequence orders
3. **Expected**: Format is "[Seq] [PartName]"

#### TC3: Double-Click Copy
1. Select a studyname in left panel (e.g., "MRI Head")
2. Add some parts to Preview
3. Double-click a different studyname in lstMappedStudynames
4. **Expected**: Preview cleared
5. **Expected**: All parts from selected studyname copied
6. **Expected**: Sequence orders preserved

#### TC4: Save Refresh
1. Create new mappings for a studyname
2. Click "Save"
3. **Expected**: Studyname appears in lstMappedStudynames (if not already there)

#### TC5: Empty State
1. Database with no mappings
2. Open window
3. **Expected**: lstMappedStudynames is empty
4. **Expected**: No errors

---

## Benefits

1. **Time Savings**: Copy existing mappings instead of rebuilding from scratch
2. **Consistency**: Reuse validated mapping patterns
3. **Discoverability**: See what mappings already exist
4. **Learning Tool**: New users can examine existing mappings
5. **Template System**: Use common studynames as templates

---

## Future Enhancements

### Potential Improvements
- **Search/Filter**: Add search box for mapped studynames
- **Comparison**: Show diff between current and selected mapping
- **Merge Mode**: Add instead of replace when copying
- **Export**: Export mapping as template
- **Statistics**: Show part usage frequency

---

## Related Features

- **Playbook Matches**: Suggests LOINC codes based on current parts
- **Common Parts**: Displays frequently used parts
- **Default Technique**: Links studyname to technique combinations

---

## Performance Considerations

- Mapped studynames query uses `DISTINCT` and `INNER JOIN` - efficient with proper indexing
- List loads once on window open and refreshes only after save
- Parts load on-demand when studyname selected
- No pagination needed - typically < 100 mapped studynames

---

## Database Schema

### Tables Used
```sql
-- Studynames
med.rad_studyname (id, studyname, tenant_id)

-- Mappings
med.rad_studyname_loinc_part (studyname_id, part_number, part_sequence_order)

-- Parts
loinc.part (part_number, part_name, part_type_name)
```

### Indexes Recommended
```sql
-- For efficient mapped studyname query
CREATE INDEX idx_mapping_studyname ON med.rad_studyname_loinc_part(studyname_id);
CREATE INDEX idx_studyname_tenant ON med.rad_studyname(tenant_id, studyname);
```

---

## Compatibility

- **.NET Version**: Compatible with .NET 8 and .NET 9
- **Database**: PostgreSQL 12+
- **Multi-Tenancy**: Fully tenant-aware
- **Backwards Compatible**: Yes - new feature, no breaking changes

---

## Rollout

- **Risk**: Low
- **Deployment**: Can be deployed independently
- **Migration**: None required
- **Training**: None required (intuitive UI)

---

**Last Updated**: 2025-11-25  
**Implemented By**: GitHub Copilot  
**Reviewed By**: [Pending]

