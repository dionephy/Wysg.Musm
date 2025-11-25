# ENHANCEMENT: Phrase Extraction Window Update

**Date**: 2025-11-06  
**Feature**: Updated Extract Phrases feature with duplicate editor and selected text display  
**Status**: ? Completed  
**Build**: ? Success

---

## Overview

Updated the "Extract Phrases" feature to:
1. Open as a singleton window (only one instance at a time)
2. Display a duplicate of the main editor showing concatenated header, findings, and conclusion
3. Show proofread versions with fallback to raw content (same logic as main window)
4. Display selected text from the editor in a dedicated TextBox below the editor
5. **Apply same dark mode styling and phrase coloring as main window editors**

---

## User Story

**As a** radiologist  
**I want** to extract phrases from a comprehensive view of my report with the same visual styling  
**So that** I can select specific portions to extract without switching between separate sections while seeing phrase highlights

---

## Implementation

### 1. Singleton Window Pattern

**File**: `apps/Wysg.Musm.Radium/Views/PhraseExtractionWindow.xaml.cs`

```csharp
private static PhraseExtractionWindow? _instance;

public static PhraseExtractionWindow GetOrCreateInstance()
{
    if (_instance == null || !_instance.IsLoaded)
    {
        _instance = new PhraseExtractionWindow();
    }
    return _instance;
}

private void OnClosed(object? sender, System.EventArgs e)
{
    _instance = null;
}
```

**Behavior**:
- Only one phrase extraction window can be open at a time
- Clicking "Extract Phrases" again activates the existing window instead of opening a new one
- Window instance is cleared when closed

### 2. Proofread Fallback Logic

**File**: `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Editor.cs`

Added new helper method `GetProofreadOrRawSections()`:

```csharp
public (string header, string findings, string conclusion) GetProofreadOrRawSections()
{
    bool proofreadMode = false;
    try
    {
        var prop = this.GetType().GetProperty("ProofreadMode");
        if (prop != null)
        {
            proofreadMode = (bool)(prop.GetValue(this) ?? false);
        }
    }
    catch { }
    
    // Header: use HeaderDisplay if proofread mode and any proofread component exists
    if (proofreadMode && (!string.IsNullOrWhiteSpace(_chiefComplaintProofread) || 
        !string.IsNullOrWhiteSpace(_patientHistoryProofread) ||
        !string.IsNullOrWhiteSpace(_studyTechniquesProofread) || 
        !string.IsNullOrWhiteSpace(_comparisonProofread)))
    {
        header = HeaderDisplay;
    }
    else
    {
        var (h, _, _) = GetDereportifiedSections();
        header = h;
    }
    
    // Findings: use proofread if available and mode ON, otherwise raw
    if (proofreadMode && !string.IsNullOrWhiteSpace(_findingsProofread))
    {
        findings = _findingsProofread;
    }
    else
    {
        var (_, f, _) = GetDereportifiedSections();
        findings = f;
    }
    
    // Conclusion: use proofread if available and mode ON, otherwise raw
    if (proofreadMode && !string.IsNullOrWhiteSpace(_conclusionProofread))
    {
        conclusion = _conclusionProofread;
    }
    else
    {
        var (_, _, c) = GetDereportifiedSections();
        conclusion = c;
    }
    
    return (header, findings, conclusion);
}
```

**Fallback Priority**:
1. **ProofreadMode ON + Proofread text exists**: Use proofread version
2. **ProofreadMode ON + Proofread text empty**: Fall back to raw/dereportified version
3. **ProofreadMode OFF**: Use raw/dereportified version

### 3. ViewModel Updates

**File**: `apps/Wysg.Musm.Radium/ViewModels/PhraseExtractionViewModel.cs`

Added two new properties:

```csharp
// Combined editor text (header + findings + conclusion, proofread or raw)
private string _editorText = string.Empty;
public string EditorText
{
    get => _editorText;
    set => Set(ref _editorText, value ?? string.Empty);
}

// Selected text from duplicate editor (bound to TextBox)
private string _selectedText = string.Empty;
public string SelectedText
{
    get => _selectedText;
    set => Set(ref _selectedText, value ?? string.Empty);
}
```

Updated `LoadFromDeReportified()` to populate `EditorText`:

```csharp
public void LoadFromDeReportified(string header, string findings, string conclusion)
{
    Lines.Clear();
    foreach (var s in SplitLinesAndDereportify(header)) Lines.Add(s);
    foreach (var s in SplitLinesAndDereportify(findings)) Lines.Add(s);
    foreach (var s in SplitLinesAndDereportify(conclusion)) Lines.Add(s);

    // NEW: Set editor text to concatenation of header, findings, conclusion (with double newlines as separator)
    var parts = new System.Collections.Generic.List<string>();
    if (!string.IsNullOrWhiteSpace(header)) parts.Add(header);
    if (!string.IsNullOrWhiteSpace(findings)) parts.Add(findings);
    if (!string.IsNullOrWhiteSpace(conclusion)) parts.Add(conclusion);
    EditorText = string.Join("\n\n", parts);
}
```

### 4. UI Layout with Dark Mode Styling

**File**: `apps/Wysg.Musm.Radium/Views/PhraseExtractionWindow.xaml`

Replaced complex extraction UI with simple layout using **dark mode colors matching main window**:

```xaml
<Window Background="#111" Foreground="#d0d0d0" FontFamily="Consolas">
    <Window.Resources>
        <SolidColorBrush x:Key="PanelBrush" Color="#1E1E1E"/>
        <Style TargetType="Button">
            <Setter Property="Background" Value="#2D2D30"/>
            <Setter Property="Foreground" Value="#C8C8C8"/>
            <Setter Property="BorderBrush" Value="#3D3D40"/>
        </Style>
        <Style TargetType="TextBox">
            <Setter Property="Background" Value="#1E1E1E"/>
            <Setter Property="Foreground" Value="#D0D0D0"/>
            <Setter Property="BorderBrush" Value="#2D2D30"/>
        </Style>
    </Window.Resources>

    <!-- Duplicate Editor with phrase coloring -->
    <editor:EditorControl x:Name="DuplicateEditor"
                         DocumentText="{Binding EditorText, Mode=TwoWay}"
                         Background="#1E1E1E"
                         Foreground="#D0D0D0"
                         FontFamily="Consolas"
                         FontSize="14"
                         PhraseSnapshot="{Binding CurrentPhraseSnapshot}"
                         PhraseSemanticTags="{Binding PhraseSemanticTags}"/>
</xaml>
```

**Dark Mode Colors**:
- Window background: `#111` (very dark gray)
- Editor background: `#1E1E1E` (dark gray)
- Foreground text: `#D0D0D0` (light gray)
- Button background: `#2D2D30` (medium dark gray)
- Borders: `#2D2D30` (medium dark gray)

### 5. Phrase Coloring Integration

**File**: `apps/Wysg.Musm.Radium/Views/PhraseExtractionWindow.xaml.cs`

Added phrase data copying in `OnWindowLoaded()`:

```csharp
private void OnWindowLoaded(object sender, RoutedEventArgs e)
{
    // Setup selection change monitoring
    var editor = DuplicateEditor.FindName("Editor") as ICSharpCode.AvalonEdit.TextEditor;
    if (editor != null && DataContext is PhraseExtractionViewModel vm)
    {
        editor.TextArea.SelectionChanged += (s, ev) =>
        {
            vm.SelectedText = editor.SelectedText ?? string.Empty;
        };
    }

    // Copy phrase snapshot and semantic tags from MainViewModel
    // This enables phrase coloring in the duplicate editor
    try
    {
        var mainWindow = Application.Current.MainWindow;
        if (mainWindow?.DataContext is MainViewModel mainVM && DataContext is PhraseExtractionViewModel extractVM)
        {
            var currentPhraseSnapshotProp = mainVM.GetType().GetProperty("CurrentPhraseSnapshot");
            var phraseSemanticTagsProp = mainVM.GetType().GetProperty("PhraseSemanticTags");
            
            if (currentPhraseSnapshotProp != null)
            {
                var snapshot = currentPhraseSnapshotProp.GetValue(mainVM) as IReadOnlyList<string>;
                if (snapshot != null)
                {
                    DuplicateEditor.PhraseSnapshot = snapshot;
                }
            }
            
            if (phraseSemanticTagsProp != null)
            {
                var semanticTags = phraseSemanticTagsProp.GetValue(mainVM) as IReadOnlyDictionary<string, string?>;
                if (semanticTags != null)
                {
                    DuplicateEditor.PhraseSemanticTags = semanticTags;
                }
            }
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"[PhraseExtractionWindow] Failed to copy phrase data: {ex.Message}");
    }
}
```

**How Phrase Coloring Works**:
1. `CurrentPhraseSnapshot`: List of all phrases (global + account) for syntax highlighting
2. `PhraseSemanticTags`: Dictionary mapping phrases to SNOMED semantic tags for color-coded highlighting
3. EditorControl uses `PhraseColorizer` (line transformer) to apply foreground colors based on semantic tags
4. Same coloring logic as main window editors for consistency

**Semantic Tag Colors** (from main window):
- Body structure phrases: Blue/Cyan color
- Clinical finding phrases: Red/Orange color
- Procedure phrases: Green color
- Other semantic types: Default colors

### 6. Selection Change Handling

**File**: `apps/Wysg.Musm.Radium/Views/PhraseExtractionWindow.xaml.cs`

Hooked into AvalonEdit's `TextArea.SelectionChanged` event:

```csharp
editor.TextArea.SelectionChanged += (s, ev) =>
{
    vm.SelectedText = editor.SelectedText ?? string.Empty;
};
```

### 7. MainWindow Integration

**File**: `apps/Wysg.Musm.Radium/Views/MainWindow.xaml.cs`

Updated `OnExtractPhrases()` to use singleton and new helper method:

```csharp
private void OnExtractPhrases(object sender, RoutedEventArgs e)
{
    try
    {
        if (DataContext is not MainViewModel vm) return;
        var app = (App)Application.Current;
        
        var vmExtract = app.Services.GetService<PhraseExtractionViewModel>();
        if (vmExtract == null)
        {
            MessageBox.Show("Phrase extraction service not available.", "Extract Phrases", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        // Get or create singleton window instance
        var win = PhraseExtractionWindow.GetOrCreateInstance();
        win.Owner = this;
        win.DataContext = vmExtract;
        
        // Get content with proofread fallback mechanism (uses new helper method)
        var (header, findings, conclusion) = vm.GetProofreadOrRawSections();
        vmExtract.LoadFromDeReportified(header, findings, conclusion);
        
        // Show window (activate if already open)
        if (!win.IsVisible)
        {
            win.Show();
        }
        else
        {
            win.Activate();
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Extraction window error: {ex.Message}", "Extract Phrases", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

---

## Behavior Examples

### Example 1: Dark Mode Consistency

**State**:
- Main window has dark mode (#1E1E1E background, #D0D0D0 text)
- Phrase Extraction window opened

**Result**:
- Window shows same dark colors as main window
- Editor has #1E1E1E background, #D0D0D0 foreground
- Buttons and TextBoxes use matching dark theme colors

### Example 2: Phrase Coloring

**State**:
- Main window has phrases loaded with SNOMED semantic tags
- Text contains "heart" (body structure), "normal" (clinical finding)

**Result**:
- "heart" appears in blue/cyan color (body structure)
- "normal" appears in red/orange color (clinical finding)
- Same coloring as main window editors

### Example 3: Proofread Mode ON, Proofread Text Available

**State**:
- Proofread toggle: ON
- `findings_proofread`: "Proofread findings text"
- `conclusion_proofread`: "Proofread conclusion text"

**Result**:
- Editor shows: Header (formatted from proofread components) + Proofread findings + Proofread conclusion
- Phrases highlighted with semantic tag colors

### Example 4: Selection Behavior

**Action**:
1. User opens phrase extraction window
2. User selects text in the duplicate editor (e.g., selects a phrase with blue color)

**Result**:
- Selected text immediately appears in the TextBox below the editor
- Updates in real-time as selection changes
- Selected phrase retains its color while selected

---

## Files Modified

1. **apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Editor.cs**
   - Added `GetProofreadOrRawSections()` method with fallback logic

2. **apps/Wysg.Musm.Radium/ViewModels/PhraseExtractionViewModel.cs**
   - Added `EditorText` property
   - Added `SelectedText` property
   - Updated `LoadFromDeReportified()` to populate EditorText

3. **apps/Wysg.Musm.Radium/Views/PhraseExtractionWindow.xaml**
   - Replaced complex UI with simple editor + selected text layout
   - **Added dark mode styling (colors matching main window)**
   - **Added `PhraseSnapshot` and `PhraseSemanticTags` bindings for phrase coloring**
   - Added EditorControl binding to `EditorText`
   - Added TextBox binding to `SelectedText`

4. **apps/Wysg.Musm.Radium/Views/PhraseExtractionWindow.xaml.cs**
   - Added singleton pattern with `GetOrCreateInstance()`
   - **Added phrase data copying in `OnWindowLoaded()` to enable phrase coloring**
   - Added selection change event handling
   - Added `OnClosed()` to clear singleton instance

5. **apps/Wysg.Musm.Radium/Views/MainWindow.xaml.cs**
   - Updated `OnExtractPhrases()` to use singleton window
   - Updated to call `GetProofreadOrRawSections()` for content

---

## Testing Scenarios

### Test 1: Dark Mode Styling

**Steps**:
1. Open main window (verify dark mode)
2. Click "Extract Phrases" button
3. Compare window colors

**Expected**: Phrase Extraction window has same dark colors as main window  
**Result**: ? Pass

### Test 2: Phrase Coloring

**Steps**:
1. Load phrases with SNOMED mappings in main window
2. Type text with phrases (e.g., "heart", "normal", "CT scan")
3. Click "Extract Phrases" button
4. Observe duplicate editor

**Expected**: Phrases show same colors as main window editors (body structure=blue, finding=red, procedure=green)  
**Result**: ? Pass

### Test 3: Singleton Behavior

**Steps**:
1. Click "Extract Phrases" button
2. Leave window open
3. Click "Extract Phrases" button again

**Expected**: Existing window activates (does not create new window)  
**Result**: ? Pass

### Test 4: Proofread Content Display

**Steps**:
1. Enable "Proofread" toggle in main window
2. Run proofread automation (populate proofread fields)
3. Click "Extract Phrases" button

**Expected**: Editor shows proofread versions with phrase colors  
**Result**: ? Pass

### Test 5: Selection Display with Colored Phrases

**Steps**:
1. Open phrase extraction window
2. Select text containing colored phrase (e.g., blue "heart")
3. Observe TextBox below editor

**Expected**: Selected text appears in TextBox immediately (color not shown in TextBox, just text)  
**Result**: ? Pass

---

## Impact

### User Experience
- ? **Unified view** - all report sections in one editor for easier phrase selection
- ? **Smart content fallback** - automatically shows best available content (proofread or raw)
- ? **Singleton behavior** - prevents multiple extraction windows cluttering the screen
- ? **Real-time selection** - immediate feedback for selected text
- ? **Consistent dark mode** - matches main window visual style
- ? **Phrase highlighting** - same semantic tag-based colors as main window editors

### Code Quality
- ? **Reusable helper method** - `GetProofreadOrRawSections()` centralizes fallback logic
- ? **Clean separation** - ViewModel handles data, View handles UI events
- ? **Consistent behavior** - uses same proofread fallback logic as main window
- ? **Consistent styling** - uses same dark theme colors as main window
- ? **Phrase coloring reuse** - leverages existing `PhraseColorizer` from EditorControl
- ? **Maintainable** - simple layout with clear data bindings

---

## Technical Details

### Phrase Coloring Architecture

The phrase coloring feature uses:
1. **PhraseColorizer** (line transformer in EditorControl) - applies foreground colors
2. **PhraseSnapshot** - list of all phrases for syntax highlighting
3. **PhraseSemanticTags** - dictionary mapping phrase �� SNOMED semantic tag
4. **Semantic tag �� color mapping** - different colors for different medical concept types

### Dark Mode Color Palette

| Element | Color | Hex |
|---------|-------|-----|
| Window background | Very dark gray | #111 |
| Editor background | Dark gray | #1E1E1E |
| Text foreground | Light gray | #D0D0D0 |
| Button background | Medium dark gray | #2D2D30 |
| Button foreground | Light gray | #C8C8C8 |
| Border | Medium dark gray | #2D2D30 |
| Textbox background | Dark gray | #1E1E1E |

These colors match the main window's dark theme for visual consistency.

---

## Related Features

- Works with FEATURE_2025-01-28_PreviousReportProofreadModeWithFallback.md (proofread fallback logic)
- Extends existing phrase extraction functionality
- Uses existing EditorControl component for consistency
- Leverages existing PhraseColorizer for semantic tag-based highlighting
- Matches main window dark theme styling

---

**Status**: ? Implemented and Verified  
**Build**: ? Success  
**Deployed**: Ready for production
