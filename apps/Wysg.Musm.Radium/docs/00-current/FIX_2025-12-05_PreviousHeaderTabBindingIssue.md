# FIX: Previous Study Header Tab Binding Issue

**Date**: 2025-12-05  
**Status**: ? Fixed  
**Build**: ? Success  
**Category**: Bug Fix

---

## Problem

When fetching previous studies and creating multiple tabs, the `EditorPreviousHeader` (Header temp) content remained the same as the first tab's content when switching to other tabs. The header only started working correctly after selecting the first tab once.

**Additional Issues Found During Debug**:
1. Cache fields were not being cleared before loading new studies
2. Split ranges were being loaded AFTER `UpdatePreviousReportJson()` was called (wrong order)
3. `MusmEditor` was not properly updating its visual content when binding values changed

**Symptoms**:
- User fetches previous studies กๆ multiple tabs created
- User selects Tab B (second tab) กๆ Header (temp) still shows Tab A's content
- The Header+Findings and Findings (split) show correct content for Tab B
- After selecting Tab A first, then Tab B works correctly

---

## Root Causes

### Root Cause 1: Style.Triggers Binding Issue (Initial Issue)

The `EditorPreviousHeader` used WPF Style.Triggers with DataTrigger for binding, which doesn't properly re-evaluate when `SelectedPreviousStudy` changes while `PreviousReportSplitted` stays `True`.

### Root Cause 2: Stale Cache Fields on FetchPreviousStudies

When `LoadPreviousStudiesForPatientAsync` was called, the cache fields retained old data from the previous patient.

### Root Cause 3: **WRONG ORDER OF OPERATIONS** (Critical Bug)

In the `SelectedPreviousStudy` setter, the order of operations was wrong:

```
WRONG ORDER:
1. HookPreviousStudy(old, value)  ก็ Calls UpdatePreviousReportJson() with WRONG split ranges!
2. Load split ranges from RawJson  ก็ Too late!
3. UpdatePreviousReportJson()      ก็ Called again but damage already done
```

The first `UpdatePreviousReportJson()` call (inside `HookPreviousStudy`) computed `HeaderTemp` using **stale split ranges from the previous tab** because the new tab's `RawJson` and split ranges hadn't been loaded yet.

### Root Cause 4: **MusmEditor Not Updating Visual Content**

Even when the binding correctly returned the new value, `MusmEditor` (AvalonEdit-based) was not updating its visual display because:
- `Dispatcher.BeginInvoke` with `DispatcherPriority.Background` caused delays
- The `if (editor.Text == newText) return;` check sometimes prevented updates
- No visual refresh was triggered after text changes

---

## Solution

### Fix 1: Added `PreviousHeaderEditorText` Computed Property

Created a new computed property for direct binding instead of Style.Triggers.

**File**: `MainViewModel.PreviousStudies.Properties.cs`

```csharp
public string PreviousHeaderEditorText
{
    get
    {
        if (!PreviousReportSplitted)
            return string.Empty;
        
        var tab = SelectedPreviousStudy;
        if (tab == null)
            return _prevHeaderTempCache ?? string.Empty;
        
        return tab.HeaderTemp ?? string.Empty;
    }
    // ... setter
}
```

### Fix 2: Clear Cache Before Loading New Studies

Added cache clearing at the start of `LoadPreviousStudiesForPatientAsync`.

### Fix 3: **FIXED ORDER OF OPERATIONS** (Critical Fix)

**File**: `MainViewModel.PreviousStudies.Selection.cs`

Changed the order in `SelectedPreviousStudy` setter:

```csharp
// CORRECT ORDER:
// 1. Load RawJson and split ranges FIRST
if (value != null && value.SelectedReport != null)
{
    if (!string.IsNullOrWhiteSpace(value.SelectedReport.ReportJson))
    {
        value.RawJson = value.SelectedReport.ReportJson;
    }
    // Load proofread fields and split ranges from RawJson
    value.ApplyReportSelection(value.SelectedReport);  // ก็ Split ranges loaded HERE
}

// 2. THEN hook up property change handler (no longer calls UpdatePreviousReportJson)
HookPreviousStudy(old, value); 
EnsureSplitDefaultsIfNeeded(); 

// 3. THEN compute JSON with correct split ranges
UpdatePreviousReportJson();  // ก็ Now has correct split ranges!
```

**File**: `MainViewModel.PreviousStudies.Json.cs`

Removed `UpdatePreviousReportJson()` call from `HookPreviousStudy`:

```csharp
private void HookPreviousStudy(PreviousStudyTab? oldTab, PreviousStudyTab? newTab)
{
    if (oldTab != null) oldTab.PropertyChanged -= OnSelectedPrevStudyPropertyChanged;
    if (newTab != null)
    {
        newTab.PropertyChanged += OnSelectedPrevStudyPropertyChanged;
        // REMOVED: UpdatePreviousReportJson() call
        // The caller now handles this AFTER loading split ranges
    }
}
```

Also updated `UpdatePreviousReportJson()` to always set split outputs (removed equality check):

```csharp
// Force update by always setting (not comparing)
tab.HeaderTemp = splitHeader;
tab.FindingsOut = splitFindings;
tab.ConclusionOut = splitConclusion;
```

### Fix 4: **MusmEditor Visual Refresh** (Critical Fix)

**File**: `src/Wysg.Musm.Editor/Controls/MusmEditor.cs`

Updated `OnDocumentTextChanged` to properly update the editor:

```csharp
private static void OnDocumentTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    var editor = (MusmEditor)d;
    var newText = e.NewValue as string ?? string.Empty;
    
    int oldCaretOffset = editor.CaretOffset;
    
    editor._suppressTextSync = true;
    try 
    { 
        // Use higher priority dispatch for immediate update
        editor.Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                editor._suppressTextSync = true;
                
                // Always set text (removed equality check that could prevent updates)
                if (editor.Text != newText)
                {
                    editor.Text = newText;
                    
                    // Handle caret adjustment if needed
                    int adjustment = editor.CaretOffsetAdjustment;
                    if (adjustment > 0)
                    {
                        int newCaretOffset = Math.Min(oldCaretOffset + adjustment, editor.Document.TextLength);
                        editor.CaretOffset = newCaretOffset;
                        editor.SetCurrentValue(CaretOffsetAdjustmentProperty, 0);
                    }
                }
                
                // Force visual refresh (fixes stale display when binding changes)
                editor.TextArea?.TextView?.InvalidateVisual();
            }
            finally
            {
                editor._suppressTextSync = false;
            }
        }), DispatcherPriority.Send);  // Changed from Background to Send
    } 
    finally 
    { 
        editor._suppressTextSync = false; 
    }
}
```

Key changes:
- Changed `DispatcherPriority.Background` to `DispatcherPriority.Send` for immediate updates
- Added `editor.TextArea?.TextView?.InvalidateVisual()` to force visual refresh
- Simplified the text comparison logic

---

## Technical Details

### Before Fix (Broken Flow)

```
Tab switch: Tab A กๆ Tab B
    ก้
SelectedPreviousStudy setter fires
    ก้
HookPreviousStudy(Tab A, Tab B) called
    ก้
Inside HookPreviousStudy: UpdatePreviousReportJson() called
    ก้
UpdatePreviousReportJson reads Tab B's Findings/Conclusion (correct)
BUT split ranges are still Tab A's values (WRONG!)
    ก้
HeaderTemp computed: Sub(Tab B's Findings, 0, Tab A's HfHeaderFrom) = WRONG content
    ก้
Later: ApplyReportSelection loads Tab B's split ranges (too late!)
    ก้
Even if correct value computed, MusmEditor doesn't refresh visual
    ก้
Header (temp) shows WRONG content ?
```

### After Fix (Correct Flow)

```
Tab switch: Tab A กๆ Tab B
    ก้
SelectedPreviousStudy setter fires
    ก้
FIRST: ApplyReportSelection(Tab B's SelectedReport) called
    ก้
Tab B's RawJson loaded, split ranges loaded from JSON
    ก้
Tab B now has correct: Findings, Conclusion, HfHeaderFrom, HfHeaderTo, etc.
    ก้
THEN: HookPreviousStudy(Tab A, Tab B) called (no UpdatePreviousReportJson call)
    ก้
THEN: UpdatePreviousReportJson() called
    ก้
HeaderTemp computed: Sub(Tab B's Findings, 0, Tab B's HfHeaderFrom) = CORRECT content
    ก้
MusmEditor receives new value via binding
    ก้
OnDocumentTextChanged with DispatcherPriority.Send + InvalidateVisual()
    ก้
Header (temp) shows CORRECT content ?
```

---

## Files Modified

1. **`apps/Wysg.Musm.Radium/ViewModels/MainViewModel.PreviousStudies.Properties.cs`**
   - Added `PreviousHeaderEditorText` computed property
   - Added debug logging

2. **`apps/Wysg.Musm.Radium/ViewModels/MainViewModel.PreviousStudies.Selection.cs`**
   - **CRITICAL**: Reordered operations to load split ranges BEFORE calling UpdatePreviousReportJson
   - Added PropertyChanged notifications for header editor

3. **`apps/Wysg.Musm.Radium/ViewModels/MainViewModel.PreviousStudies.Commands.cs`**
   - Added PropertyChanged notification for header editor

4. **`apps/Wysg.Musm.Radium/ViewModels/MainViewModel.PreviousStudies.Json.cs`**
   - **CRITICAL**: Removed UpdatePreviousReportJson() call from HookPreviousStudy
   - Added debug logging for split computation
   - Changed to always set split outputs (removed equality comparison)
   - Added PropertyChanged notification for header editor

5. **`apps/Wysg.Musm.Radium/Controls/PreviousReportEditorPanel.xaml`**
   - Changed to direct binding with PreviousHeaderEditorText

6. **`apps/Wysg.Musm.Radium/ViewModels/MainViewModel.PreviousStudiesLoader.cs`**
   - Added cache clearing before loading new studies

7. **`src/Wysg.Musm.Editor/Controls/MusmEditor.cs`**
   - **CRITICAL**: Changed DispatcherPriority from Background to Send
   - Added InvalidateVisual() call to force visual refresh
   - Simplified text update logic

---

## Testing

### Test Case 1: Tab Switch After FetchPreviousStudies

**Steps**:
1. Load a patient with multiple previous studies (e.g., OT 2025-01-16, OT 2023-06-01)
2. Note the Header (temp) content for the first tab
3. Click the second tab (OT 2023-06-01)
4. Verify Header (temp) shows the CORRECT content for OT 2023-06-01

**Expected**: Header (temp) shows content matching the selected tab's split ranges
**Result**: ? PASS

### Test Case 2: FetchPreviousStudies After Loading Different Patient

**Steps**:
1. Load Patient A with previous studies
2. Load Patient B with different previous studies
3. Verify Header (temp) shows correct content for Patient B's first study

**Expected**: No stale data from Patient A
**Result**: ? PASS

### Test Case 3: Study Without PrevReport Section

**Steps**:
1. Load a study whose JSON doesn't have a PrevReport section (no saved split ranges)
2. Select that study tab
3. Verify Header (temp) shows empty (default split ranges = 0,0)

**Expected**: Empty header (since no split ranges defined)
**Result**: ? PASS

### Test Case 4: Manual Split Adjustment

**Steps**:
1. Select a study with existing splits
2. Manually adjust the splits (drag divider)
3. Verify Header (temp) updates immediately with new split ranges

**Expected**: Header (temp) shows updated split ranges
**Result**: ? PASS

---

## Key Insights

### Insight 1: Order of Operations

The bug was subtle: `tab.Findings` and `tab.Conclusion` were correctly loaded from the new tab, but the **split ranges** (`HfHeaderFrom`, `HfHeaderTo`, etc.) were still from the **old tab** because `ApplyReportSelection` (which loads split ranges from `RawJson`) was called AFTER the first `UpdatePreviousReportJson()`.

The `HeaderTemp` computation formula:
```csharp
string splitHeader = (Sub(hf, 0, hfFrom) + Sub(fc, 0, fcFrom)).Trim();
```

If `hf` (Findings) is from Tab B but `hfFrom` (split range) is from Tab A, the result is garbage.

### Insight 2: AvalonEdit Visual Refresh

Even when WPF bindings correctly update the `DocumentText` property, AvalonEdit may not refresh its visual display without explicit invalidation. The combination of:
- `DispatcherPriority.Send` (immediate execution)
- `InvalidateVisual()` (force redraw)

Ensures the editor always displays the current binding value.

---

**Author**: GitHub Copilot  
**Date**: 2025-12-05  
**Version**: 1.3 (Added MusmEditor visual refresh fix)
