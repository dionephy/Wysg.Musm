# ENHANCEMENT: Web Browser Element Picker

**Date**: 2025-11-10  
**Type**: Feature Enhancement  
**Component**: UI Spy Window - OnPickWeb  
**Status**: ? Complete

---

## Overview

Added "Pick Web" button to AutomationWindow that captures web browser UI elements with automatic optimization for web stability. Works exactly like regular "Pick" button, but applies smart defaults to create robust bookmarks that work even when browser tab titles change.

---

## User Workflow

### Step-by-Step Process

1. **Click "Pick Web"** button in AutomationWindow toolbar
2. **Move mouse** to target web browser element (1500ms delay)
3. **Element captured** with automatic web optimization applied
4. **Click "Validate"** to verify the bookmark works
5. **Modify if needed** (optional manual adjustments in grid)
6. **Select predefined bookmark** from "Bookmark:" dropdown (e.g., "ReportText", "StudyList")
7. **Click "Save"** to map the captured tree to the selected bookmark

### Workflow Comparison

| Step | Regular "Pick" | "Pick Web" |
|------|---------------|-----------|
| 1. Capture | Standard defaults | Web-optimized defaults |
| 2. Display | Show in grid | Show in grid |
| 3. Validate | Click "Validate" | Click "Validate" |
| 4. Select Bookmark | Select from dropdown | Select from dropdown |
| 5. Save | Click "Save" button | Click "Save" button |
| **Auto-Save** | No | No |
| **Result** | Generic bookmark | Robust web bookmark |

---

## Automatic Web Optimization

### Browser Window Nodes (Levels 0-2)

**Disabled**:
- `UseName` = false (tab titles change)
- `UseAutomationId` = false (not stable for top-level windows)
- `UseIndex` = false (brittle)

**Enabled**:
- `UseClassName` = true (structural identifier like "Chrome_WidgetWin_1")
- `UseControlTypeId` = true (stable control type)

**Scope**: `Descendants` (fast hierarchical search)

### Web Content Nodes (Level 3+)

**Disabled**:
- `UseName` = false (dynamic content)
- `UseIndex` = false (brittle)

**Enabled**:
- `UseClassName` = true (CSS classes are stable)
- `UseControlTypeId` = true (HTML element types)
- `UseAutomationId` = true (best for web elements)

**Scope**: `Descendants` (fast hierarchical search)

---

## Why Web Optimization Matters

### Problem with Standard "Pick"

```
Browser tab: "ITR Worklist Report - ???? - Microsoft? Edge"
Standard Pick captures: UseName=true with exact title
User switches tabs or window title changes
Bookmark fails: "ITR Worklist Report - Microsoft Edge" ?? captured title ?
```

### Solution with "Pick Web"

```
Browser tab: "ITR Worklist Report - ???? - Microsoft? Edge"
Pick Web captures: UseName=false, UseClassName=true (Chrome_WidgetWin_1)
User switches tabs or window title changes
Bookmark succeeds: ClassName match, title ignored ?
```

---

## Example Usage

### Scenario: Mapping "ReportText" Bookmark to Web PACS

```
1. User clicks "Pick Web" button
   Status: "Pick Web arming... move mouse to web browser element (1500ms)"

2. User moves mouse to textarea in PACS web interface
   System captures element tree

3. Status updates: "Captured web element from 'msedge' (optimized for web stability)"
   Grid shows optimized chain:
   - Level 0-2: UseName=off, UseClassName=on, UseControlTypeId=on, Scope=Descendants
   - Level 3+: UseName=off, UseAutomationId=on, UseClassName=on, Scope=Descendants

4. User clicks "Validate" button
   Status: "Validate: found and highlighted (45 ms)" ?

5. User selects "ReportText" from Bookmark ComboBox

6. User clicks "Save" button
   Status: "Saved mapping for ReportText"
   
7. Bookmark is now mapped and saved to ui-bookmarks.json
```

---

## Technical Implementation

### Button in UI (AutomationWindow.xaml)

```xaml
<Button Content="Pick Web" Margin="6,0,0,0" Click="OnPickWeb" 
        Style="{StaticResource AutomationWindowButtonStyle}" 
        ToolTip="Pick element from web browser with web-optimized defaults"/>
```

### Handler Logic (AutomationWindow.Bookmarks.cs)

```csharp
private async void OnPickWeb(object sender, RoutedEventArgs e)
{
    // 1. Arm and wait for user to position mouse
    await Task.Delay(delay);
    
    // 2. Capture element tree (preferAutomationId=true for web)
    var (b, procName, msg) = CaptureUnderMouse(preferAutomationId: true);
    
    // 3. Apply web optimization to all nodes
    for (int i = 0; i < b.Chain.Count; i++)
    {
        var node = b.Chain[i];
        
        if (i < 3) // Browser chrome (structural matching)
        {
            node.UseName = false;           // Titles change
            node.UseClassName = true;       // Stable structure
            node.UseControlTypeId = true;   // Stable type
            node.UseAutomationId = false;   // Not useful at top level
            node.UseIndex = false;          // Brittle
        }
        else // Web content (AutomationId priority)
        {
            node.UseName = false;           // Dynamic text
            node.UseClassName = true;       // CSS classes
            node.UseControlTypeId = true;   // Element type
            node.UseAutomationId = true;    // Best web identifier
            node.UseIndex = false;          // Brittle
        }
        
        // Fast hierarchical search for all nodes
        if (i > 0) node.Scope = UiBookmarks.SearchScope.Descendants;
    }
    
    // 4. Display in grid for validation (NO AUTO-SAVE)
    Tag = b;
    ShowBookmarkDetails(b, "Captured web element (optimized for stability)");
    GridChain.ItemsSource = b.Chain;
}
```

### Key Design Decisions

1. **No Auto-Save** - Matches regular "Pick" workflow
2. **No Naming Dialog** - User selects from predefined bookmarks
3. **Optimization Applied Immediately** - Smart defaults reduce manual editing
4. **Validation First** - User must verify bookmark works before saving

---

## Status Messages

### During Capture
```
"Pick Web arming... move mouse to web browser element (1500ms)"
```

### After Capture
```
"Captured web element from 'msedge' (optimized for web stability). 
Select bookmark from dropdown and click Save to map."
```

### After Validation
```
"Validate: found and highlighted (45 ms)"  // Success
"Validate: not found (137 ms)"             // Failure
```

### After Save
```
"Saved mapping for ReportText"  // When bookmark selected from dropdown
```

---

## Benefits

1. **Robust Bookmarks** - Works with dynamic browser tab titles
2. **Faster Resolution** - Descendants scope improves search speed (~45ms)
3. **AutomationId Priority** - Uses best identifier for web elements
4. **No Index Dependency** - Eliminates brittle index-based matching
5. **Consistent Workflow** - Same validation/save process as regular "Pick"
6. **Manual Override** - User can modify optimization in grid if needed

---

## Browser Compatibility

| Browser | Process Name | Window Class | Status |
|---------|-------------|-------------|---------|
| **Microsoft Edge** | msedge | Chrome_WidgetWin_1 | ? Tested |
| **Google Chrome** | chrome | Chrome_WidgetWin_1 | ? Compatible |
| **Firefox** | firefox | MozillaWindowClass | ? Compatible |

---

## Validation Example

### Test Case: Edge with Dynamic Tab Title

**Initial Capture**:
```
Window: "ITR Worklist Report - ???? - Microsoft? Edge"
Element: textarea (AutomationId="job-report-view-report-text")
Optimization: Applied ?
Validation: Success (45 ms)
```

**After Tab Title Changes**:
```
Window: "ITR Worklist Report - Microsoft Edge" (Korean removed)
Step 1: UseName=false, ClassName='Chrome_WidgetWin_1' ?? Match ?
Step 2: UseName=false, ClassName='BrowserRootView' ?? Match ?
Step 3: ClassName='NonClientView' ?? Match ?
...
Step 14: AutomationId='job-report-view-report-text' ?? Match ?
Result: Found and highlighted (45 ms) ?
```

---

## Predefined Bookmarks for Web PACS

Common KnownControl bookmarks to map with "Pick Web":

| Bookmark | Purpose | Typical AutomationId |
|----------|---------|---------------------|
| `ReportText` | Report textarea | report-text, editor-content |
| `ReportText2` | Secondary report field | report-text-2 |
| `StudyList` | Study list grid | study-list, worklist |
| `SearchResultsList` | Search results | search-results |
| `SendButton` | Report send button | btn-send, submit-report |
| `CloseButton` | Close window button | btn-close |
| `StudyRemark` | Study remark field | study-remark |
| `PatientRemark` | Patient remark field | patient-remark |

---

## Differences from Regular "Pick"

| Feature | Regular "Pick" | "Pick Web" |
|---------|---------------|-----------|
| **UseName on browser windows** | Enabled by default | Disabled (robust) |
| **Scope on all nodes** | Children (slower) | Descendants (faster) |
| **AutomationId priority** | Standard | Enabled for web content |
| **UseIndex default** | Enabled | Disabled (less brittle) |
| **Optimization message** | Generic capture | "optimized for web stability" |

---

## Known Limitations

1. **Requires AutomationId** - Web content nodes rely on AutomationId (most modern web apps provide this)
2. **Browser Structure Changes** - Major browser updates may change internal structure (rare, affects both Pick and Pick Web)
3. **Shadow DOM** - Some web components may not be accessible via UIA
4. **No Window Title Storage** - Unlike original plan, doesn't save window context (not needed for predefined bookmarks)

---

## Files Modified

- `apps\Wysg.Musm.Radium\Views\AutomationWindow.xaml` - Added "Pick Web" button with tooltip
- `apps\Wysg.Musm.Radium\Views\AutomationWindow.Bookmarks.cs` - Implemented OnPickWeb handler with optimization logic

---

## Related Documentation

- `BUGFIX_2025-11-10_WebBrowserElementPickerRobustness.md` - Robustness optimization details and validation results
- `QUICKREF_2025-11-10_WebBrowserElementPicker.md` - Quick reference guide for users

---

**Status**: ? Complete  
**Workflow**: Pick Web ?? Validate ?? Select Bookmark ?? Save  
**Auto-Save**: No (user must select bookmark and click Save)  
**Optimization**: Automatic (smart defaults applied on capture)
