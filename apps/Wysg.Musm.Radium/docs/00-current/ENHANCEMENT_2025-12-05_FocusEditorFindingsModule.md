# ENHANCEMENT: FocusEditorFindings Built-in Module

**Date**: 2025-12-05  
**Type**: Feature Enhancement  
**Status**: ? Complete  
**Build**: ? Success

---

## Overview

Added a new "FocusEditorFindings" built-in automation module that:
1. Brings the MainWindow to the front (restores from minimized if necessary)
2. Activates and focuses the MainWindow
3. Focuses the Findings editor (EditorFindings control)

This module is useful in automation sequences where you want to ensure the user's attention is directed to the Findings editor after performing other operations (e.g., after fetching data from PACS).

---

## User Story

**As a** radiologist using automation sequences  
**I want** a module that focuses the Findings editor  
**So that** I can quickly start typing after automation completes without manually clicking on the editor

---

## Usage

### In Automation Window ¡æ Automation Tab

1. Open the Automation Window
2. Go to the "Automation" tab
3. Find "FocusEditorFindings" in the "Built-in Modules" pane
4. Drag it into any automation sequence (e.g., New Study, Send Report, Shortcut sequences)

### Typical Use Cases

**Example 1: End of New Study Sequence**
```
NewStudy(obs) ¡æ FetchPreviousStudies ¡æ SetComparison ¡æ FocusEditorFindings
```
After the new study is loaded, focus is automatically placed on the Findings editor.

**Example 2: After Opening a Study from Worklist**
```
OpenStudy(obs) ¡æ FocusEditorFindings
```
Opens the study and immediately focuses the Findings editor for typing.

**Example 3: Combined with Other Actions**
```
OpenWorklist ¡æ ResultsListSetFocus ¡æ Delay ¡æ FocusEditorFindings
```
Opens worklist, focuses the results list, waits, then switches focus to the Findings editor.

---

## Implementation Details

### Files Modified

1. **`apps/Wysg.Musm.Radium/ViewModels/SettingsViewModel.cs`**
   - Added "FocusEditorFindings" to the `availableModules` collection

2. **`apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.Automation.Core.cs`**
   - Added module execution case in `RunModulesSequentially`
   - Added `RunFocusEditorFindingsAsync()` method

### Code Implementation

```csharp
/// <summary>
/// Built-in module: FocusEditorFindings
/// Brings the MainWindow to the front, activates it, and focuses the Findings editor.
/// </summary>
private async Task RunFocusEditorFindingsAsync()
{
    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
    {
        var mainWindow = System.Windows.Application.Current.MainWindow;
        if (mainWindow == null) return;
        
        // Restore from minimized if necessary
        if (mainWindow.WindowState == System.Windows.WindowState.Minimized)
            mainWindow.WindowState = System.Windows.WindowState.Normal;
        
        // Activate and focus window
        mainWindow.Activate();
        mainWindow.Focus();
        
        // Focus the EditorFindings control
        if (mainWindow is Views.MainWindow mw)
        {
            var gridCenter = mw.FindName("gridCenter") as Controls.CenterEditingArea;
            var editorFindings = gridCenter?.EditorFindings;
            var musmEditor = editorFindings?.FindName("Editor") as TextEditor;
            
            if (musmEditor != null)
            {
                musmEditor.Focus();
                musmEditor.TextArea?.Caret.BringCaretToView();
            }
            else
            {
                editorFindings?.Focus();
            }
        }
    });
    
    SetStatus("[FocusEditorFindings] Done.");
}
```

---

## Behavior

### Window Activation
- If MainWindow is minimized, it's restored to Normal state
- MainWindow.Activate() brings it to the foreground
- MainWindow.Focus() ensures keyboard focus is on the window

### Editor Focus
- Finds the `gridCenter` (CenterEditingArea) control
- Locates `EditorFindings` within gridCenter
- Finds the underlying AvalonEdit TextEditor named "Editor"
- Calls Focus() on the editor and brings the caret into view

### Error Handling
- If MainWindow is null, logs error and sets status
- If any control in the hierarchy is not found, gracefully falls back
- Exceptions are caught and logged with status message

---

## Debug Logging

The module outputs debug messages at each step:

```
[Automation][FocusEditorFindings] START
[Automation][FocusEditorFindings] MainWindow activated
[Automation][FocusEditorFindings] Focused MusmEditor
[Automation][FocusEditorFindings] COMPLETED
```

Or on error:
```
[Automation][FocusEditorFindings] MainWindow is null
[Automation][FocusEditorFindings] gridCenter not found in MainWindow
```

---

## Status Messages

- **Success**: `[FocusEditorFindings] Done.`
- **Error (no window)**: `[FocusEditorFindings] MainWindow not found.`
- **Error (exception)**: `[FocusEditorFindings] Error: <message>`

---

## Related Features

- **EditorAutofocusService**: Automatic focus on keystroke (configured in Settings ¡æ Keyboard)
- **SetCurrentInMainScreen**: Sets values in MainWindow fields (different purpose)
- **ResultsListSetFocus**: Focuses the results list in PACS (for navigation)

---

## Testing Scenarios

### Scenario 1: Basic Usage
1. Create automation sequence: `FocusEditorFindings`
2. Run the sequence
3. ? MainWindow is activated and Findings editor has focus

### Scenario 2: From Minimized Window
1. Minimize MainWindow
2. Run automation sequence with `FocusEditorFindings`
3. ? Window is restored and Findings editor has focus

### Scenario 3: In Sequence with Other Modules
1. Create sequence: `NewStudy(obs), FocusEditorFindings`
2. Run the sequence
3. ? After new study loads, Findings editor automatically has focus

### Scenario 4: Error Handling
1. Manually close MainWindow (unusual scenario)
2. Run sequence with `FocusEditorFindings`
3. ? Error message displayed, automation continues

---

## Future Enhancements

Potential improvements:
1. `FocusEditorConclusion` - Focus the Conclusion editor instead
2. `FocusEditorHeader` - Focus the Header editor
3. `FocusPreviousFindings` - Focus the Previous Findings editor
4. Parameterized `FocusEditor` with target argument

---

**Status**: ? Complete  
**Build**: ? Success  
**Virtual Reality Date**: 2025-12-05
