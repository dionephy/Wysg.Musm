# Enhancement: Default Differential Diagnosis Field

**Date**: 2025-01-28  
**Type**: Feature Enhancement  
**Status**: ? Implemented  
**Build**: ? Success

---

## Overview

Added a new configurable field **"Default differential diagnosis"** to the Settings ⊥ Reportify ⊥ Defaults section. This field allows users to set a default prefix for differential diagnosis sections in their reports (e.g., "DDx:", "Differential:", etc.).

---

## User Request

> In settings -> reportify -> defaults pane, I want additional textbox "Default differential diagnosis" defaultly "DDx:"

---

## Implementation Details

### 1. ViewModel Changes (`SettingsViewModel.cs`)

#### Added Property
```csharp
private string _defaultDifferentialDiagnosis = "DDx:"; 
public string DefaultDifferentialDiagnosis 
{ 
    get => _defaultDifferentialDiagnosis; 
    set 
    { 
        if (SetProperty(ref _defaultDifferentialDiagnosis, value)) 
            UpdateReportifyJson(); 
    } 
}
```

#### Updated JSON Serialization (`UpdateReportifyJson`)
```csharp
defaults = new
{
    arrow = DefaultArrow,
    conclusion_numbering = DefaultConclusionNumbering,
    detailing_prefix = DefaultDetailingPrefix,
    differential_diagnosis = DefaultDifferentialDiagnosis  // NEW
}
```

#### Updated JSON Deserialization (`ApplyReportifyJson`)
```csharp
DefaultDifferentialDiagnosis = GetDef("differential_diagnosis", DefaultDifferentialDiagnosis);
```

---

### 2. UI Changes (`ReportifySettingsTab.xaml`)

Added a fourth row to the "Defaults" section:

```xaml
<Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>  <!-- NEW -->
</Grid.RowDefinitions>

<!-- NEW ROW -->
<TextBlock Grid.Row="3" Grid.Column="0" Text="Default differential diagnosis" VerticalAlignment="Center"/>
<TextBox Grid.Row="3" Grid.Column="1" Margin="4,2,0,2" Text="{Binding DefaultDifferentialDiagnosis}"/>
```

---

## Settings JSON Structure

The new field is stored in the `defaults` object of the reportify settings JSON:

```json
{
  "remove_excessive_blanks": true,
  "remove_excessive_blank_lines": true,
  // ... other settings ...
  "defaults": {
    "arrow": "-->",
    "conclusion_numbering": "1.",
    "detailing_prefix": "-",
    "differential_diagnosis": "DDx:"  // NEW
  }
}
```

---

## User Interface

### Location
**Settings Window ⊥ Reportify Tab ⊥ Defaults Section**

### Layout
```
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛 Defaults                                        弛
戍式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛 Default arrow                      [    -->   ] 弛
弛 Default conclusion numbering       [    1.    ] 弛
弛 Default detailing prefix           [    -     ] 弛
弛 Default differential diagnosis     [    DDx:  ] 弛 ∠ NEW
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
```

---

## Default Value

**"DDx:"**  
- Commonly used medical abbreviation for "Differential Diagnosis"
- Includes colon for immediate use in reports
- Can be customized to user preference (e.g., "Differential:", "DD:", etc.)

---

## Persistence

### Storage
- Saved to PostgreSQL database in `radium.reportify_setting` table
- Stored as part of the `settings_json` JSONB column
- Associated with the user's account ID

### Scope
- **Account-specific**: Each account has its own reportify settings
- **Persists across sessions**: Settings are loaded on application startup
- **Synchronized**: Changes are immediately saved to the database

---

## Usage Scenario

### Example Use Case
A radiologist frequently writes differential diagnoses and wants a consistent prefix:

**Before**:
Manually types "DDx:" every time

**After**:
1. Open Settings ⊥ Reportify ⊥ Defaults
2. Set "Default differential diagnosis" to "DDx:"
3. The application can now use this default when needed (future feature)

---

## Future Enhancements

This field sets the foundation for future features such as:
- **Auto-insertion**: Automatically insert "DDx:" when user creates a differential diagnosis section
- **Snippet integration**: Use this prefix in differential diagnosis snippets
- **Template support**: Include in report templates for consistency

---

## Files Modified

1. **ViewModel**
   - `apps/Wysg.Musm.Radium/ViewModels/SettingsViewModel.cs`
     - Added `DefaultDifferentialDiagnosis` property
     - Updated `UpdateReportifyJson()` method
     - Updated `ApplyReportifyJson()` method

2. **UI**
   - `apps/Wysg.Musm.Radium/Views/SettingsTabs/ReportifySettingsTab.xaml`
     - Added row to Defaults grid
     - Added TextBlock label
     - Added TextBox binding

---

## Testing

### Manual Test Steps
1. ? Open Settings ⊥ Reportify tab
2. ? Expand "Defaults" section
3. ? Verify "Default differential diagnosis" field is visible
4. ? Verify default value is "DDx:"
5. ? Change value to "Differential:"
6. ? Click "Save Settings"
7. ? Restart application
8. ? Verify setting persisted

### Verification
```json
// Settings JSON after save should include:
{
  "defaults": {
    "differential_diagnosis": "Differential:"
  }
}
```

---

## Database Schema

No database migration required. The setting is stored in the existing JSONB column:

```sql
-- radium.reportify_setting table
-- settings_json column (JSONB) now can contain:
{
  "defaults": {
    "differential_diagnosis": "DDx:"
  }
}
```

---

## Backward Compatibility

? **Fully backward compatible**
- Existing settings JSON without this field will use default value "DDx:"
- Old reportify settings continue to work
- New field is optional and doesn't affect existing functionality

---

## Build Status

? **Compilation**: Success (no errors)  
? **Dependencies**: All resolved  
? **Integration**: No conflicts

---

## Summary

**What Changed**:
- Added new configurable field for default differential diagnosis prefix
- Located in Settings ⊥ Reportify ⊥ Defaults pane
- Default value: "DDx:"
- Persists to database with other reportify settings

**Impact**:
- ? Improved user customization
- ? Foundation for future automation features
- ? Consistent with existing settings pattern
- ? No breaking changes

---

**Status**: ? Implemented and Tested  
**Build**: ? Success
