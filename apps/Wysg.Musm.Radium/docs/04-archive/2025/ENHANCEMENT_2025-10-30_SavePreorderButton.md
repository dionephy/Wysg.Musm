# ENHANCEMENT: Save Preorder Button

**Date**: 2025-01-30  
**Feature**: Save Preorder button to capture findings text for preorder purposes  
**Status**: ? Completed  
**Build**: ? Success

---

## Overview

Added a "Save Preorder" button next to the "Extract Phrases" button in the main window. When clicked, the button saves the current findings text (raw, unreportified) to a new `findings_preorder` field in the current report JSON.

---

## User Story

**As a** radiologist  
**I want** to save a snapshot of my current findings text as a "preorder"  
**So that** I can preserve the initial draft before making further edits

---

## Implementation

### 1. New JSON Field

Added `findings_preorder` field to the current report JSON schema:
- **Location**: `CurrentReportJson` in `MainViewModel.Editor.cs`
- **Type**: String
- **Content**: Raw (unreportified) findings text at the time of save
- **Purpose**: Preserve initial draft of findings for reference

### 2. New Property

**File**: `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Editor.cs`

```csharp
private string _findingsPreorder = string.Empty; 
public string FindingsPreorder 
{ 
    get => _findingsPreorder; 
    set 
    { 
        if (SetProperty(ref _findingsPreorder, value ?? string.Empty))
        {
            Debug.WriteLine($"[Editor] FindingsPreorder updated: length={value?.Length ?? 0}");
            UpdateCurrentReportJson(); 
        }
    } 
}
```

### 3. JSON Serialization

**File**: `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Editor.cs`

```csharp
private void UpdateCurrentReportJson()
{
    var obj = new
    {
        // ...existing fields...
        
        // Preorder findings (saved via Save Preorder button)
        findings_preorder = _findingsPreorder,
        
        // ...rest of fields...
    };
    // ...
}
```

### 4. JSON Deserialization

**File**: `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Editor.cs`

```csharp
private void ApplyJsonToEditors(string json)
{
    // ...
    string newFindingsPreorder = root.TryGetProperty("findings_preorder", out var fpoEl) 
        ? (fpoEl.GetString() ?? string.Empty) 
        : string.Empty;
    
    _findingsPreorder = newFindingsPreorder; 
    OnPropertyChanged(nameof(FindingsPreorder));
    // ...
}
```

### 5. New Command

**File**: `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.cs`

```csharp
public ICommand SavePreorderCommand { get; private set; } = null!;

private void InitializeCommands()
{
    // ...
    SavePreorderCommand = new DelegateCommand(_ => OnSavePreorder());
}

private void OnSavePreorder()
{
    try
    {
        Debug.WriteLine("[SavePreorder] Saving current findings to findings_preorder JSON field");
        
        // Get the raw findings text (unreportified)
        var findingsText = RawFindingsText;
        
        if (string.IsNullOrWhiteSpace(findingsText))
        {
            SetStatus("No findings text available to save as preorder", true);
            Debug.WriteLine("[SavePreorder] Findings text is empty");
            return;
        }
        
        Debug.WriteLine($"[SavePreorder] Captured findings text: length={findingsText.Length} chars");
        
        // Save to FindingsPreorder property (which will trigger JSON update)
        FindingsPreorder = findingsText;
        
        SetStatus($"Pre-order findings saved ({findingsText.Length} chars)");
        Debug.WriteLine("[SavePreorder] Successfully saved to FindingsPreorder property");
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"[SavePreorder] Error: {ex.Message}");
        SetStatus("Save preorder operation failed", true);
    }
}
```

### 6. UI Button

**File**: `apps/Wysg.Musm.Radium/Controls/CurrentReportEditorPanel.xaml`

```xaml
<!-- Upper row: controls -->
<StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,0" VerticalAlignment="Center">
    <Button Content="New" Command="{Binding NewStudyCommand}" FontSize="12" Margin="2,2,0,2"/>
    <Button Content="Send Report Preview" Command="{Binding SendReportPreviewCommand}" Margin="2,2,0,2"/>
    <Button Content="Send Report" Command="{Binding SendReportCommand}" Margin="2,2,0,2"/>
    <Button Content="Extract Phrases" Click="OnExtractPhrasesClick" Margin="2,2,0,2"/>
    <Button Content="Save Preorder" Command="{Binding SavePreorderCommand}" Margin="2,2,0,2"/>  <!-- NEW -->
    <Button Content="Test" Command="{Binding TestNewStudyProcedureCommand}" Margin="2,2,0,2"/>
    <!-- ...toggles... -->
</StackPanel>
```

---

## Behavior

### When Button is Clicked

1. **Validation**: Checks if findings text is available (not empty)
2. **Capture**: Gets the current raw (unreportified) findings text using `RawFindingsText` property
3. **Save**: Updates `FindingsPreorder` property
4. **JSON Update**: Automatically triggers `UpdateCurrentReportJson()` to serialize to JSON
5. **Feedback**: Shows status message with character count

### Edge Cases

- **Empty Findings**: Shows error status "No findings text available to save as preorder"
- **Exception**: Shows error status "Save preorder operation failed"
- **Reportified Mode**: Uses raw text (not the formatted version) via `RawFindingsText` property

### Status Messages

- **Success**: `"Pre-order findings saved (123 chars)"` (shows character count)
- **Empty**: `"No findings text available to save as preorder"`
- **Error**: `"Save preorder operation failed"`

---

## Testing Scenarios

### Scenario 1: Normal Save
1. Type findings text in editor
2. Click "Save Preorder" button
3. ? Findings text saved to `findings_preorder` JSON field
4. ? Status shows "Pre-order findings saved (X chars)"

### Scenario 2: Reportified Mode
1. Type findings text in editor
2. Enable "Reportified" toggle
3. Click "Save Preorder" button
4. ? Raw (unreportified) text saved to `findings_preorder`
5. ? Status shows success message

### Scenario 3: Empty Findings
1. Clear findings editor
2. Click "Save Preorder" button
3. ? Status shows error "No findings text available to save as preorder"

### Scenario 4: JSON Round-Trip
1. Save preorder findings
2. Close and reopen study
3. ? `findings_preorder` field preserved in JSON

---

## Database Persistence

The `findings_preorder` field is:
- ? Included in `CurrentReportJson` property
- ? Saved to database via existing `SaveCurrentStudyToDB` automation module
- ? Round-tripped correctly when loading studies from database

---

## Button Placement

**Location**: Top toolbar row in CurrentReportEditorPanel  
**Position**: After "Extract Phrases" button, before "Test" button  
**Alignment**: Horizontal left-to-right flow with other action buttons

---

## Files Modified

1. `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.cs`
   - Added `SavePreorderCommand` property
   - Added `OnSavePreorder()` handler
   - Initialized command in `InitializeCommands()`

2. `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Editor.cs`
   - Added `FindingsPreorder` property with backing field
   - Updated `UpdateCurrentReportJson()` to include `findings_preorder` field
   - Updated `ApplyJsonToEditors()` to read `findings_preorder` field

3. `apps/Wysg.Musm.Radium/Controls/CurrentReportEditorPanel.xaml`
   - Added "Save Preorder" button with `SavePreorderCommand` binding

---

## JSON Schema

### Before
```json
{
  "findings": "raw findings text",
  "conclusion": "raw conclusion text",
  ...
}
```

### After
```json
{
  "findings": "raw findings text",
  "conclusion": "raw conclusion text",
  "findings_preorder": "saved preorder findings",
  ...
}
```

---

## Debugging

Added Debug.WriteLine statements for troubleshooting:
- `[SavePreorder] Saving current findings to findings_preorder JSON field`
- `[SavePreorder] Captured findings text: length=X chars`
- `[SavePreorder] Successfully saved to FindingsPreorder property`
- `[Editor] FindingsPreorder updated: length=X`

---

## Future Enhancements

Potential improvements:
1. Add "Load Preorder" button to restore saved findings
2. Show indicator when preorder exists (badge/icon)
3. Confirm overwrite if preorder already exists
4. Support multiple preorder versions (preorder_v1, preorder_v2, etc.)
5. Add timestamp to preorder metadata
6. Extend to save conclusion preorder as well

---

**Status**: ? Feature Complete  
**Build**: ? Success  
**Testing**: ? Pending User Validation
