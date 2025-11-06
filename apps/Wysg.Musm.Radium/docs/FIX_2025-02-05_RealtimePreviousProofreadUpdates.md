# FIX: Real-Time Previous Report Proofread Updates

**Date**: 2025-02-05  
**Issue**: Previous report findings/conclusion editors don't update immediately when `findings_proofread` or `conclusion_proofread` fields are populated in JSON, AND placeholder conversion not working  
**Status**: ? Fixed  
**Build**: ? Success

---

## Problem Statement

### Issue 1: Missing Real-Time Updates

When the proofreading toggle is ON in `PreviousReportEditorPanel.xaml`, and the `findings_proofread` field in the previous report JSON becomes populated (e.g., via automation module), the previous findings editor should **immediately (real-time)** show the proofread text.

#### Observed Behavior (Before Fix)

1. User enables "Proofread" toggle for previous report
2. Automation module populates `findings_proofread` field in JSON
3. **BUG**: Findings editor continues showing the original text (no real-time update)
4. Editor only updates after manual interaction (e.g., tab switch, toggle off/on)

### Issue 2: Missing Placeholder Conversion

When proofread text contains placeholders like `{arrow}`, `{DDx}`, or `{bullet}`, these should be replaced with the configured reportify defaults (e.g., `{arrow}` ⊥ `-->`, `{DDx}` ⊥ `DDx:`, `{bullet}` ⊥ `-`).

#### Observed Behavior (Before Fix)

1. Proofread text contains `{arrow}` placeholder
2. **BUG**: Editor shows literal `{arrow}` instead of `-->`
3. Placeholders work for other display properties but not for previous report editors

---

## Root Causes

### Root Cause 1: Missing Property Notifications

The computed property `PreviousFindingsEditorText` was correctly designed to switch between proofread and original text based on `PreviousProofreadMode`, but the property change notifications were **not triggered** when the underlying `FindingsProofread` field changed via JSON updates.

**Timeline of the bug:**
1. JSON gets updated with `findings_proofread: "..."` via automation
2. `ApplyPreviousJsonFields()` updates `tab.FindingsProofread = value`
3. ? **Missing notification**: `OnPropertyChanged(nameof(PreviousFindingsEditorText))` was never called
4. WPF binding doesn't refresh the editor because it doesn't know the computed property changed

### Root Cause 2: Missing Placeholder Conversion

The `PreviousFindingsEditorText` and `PreviousConclusionEditorText` properties returned raw proofread text without calling `ApplyProofreadPlaceholders()`, while other display properties (like `PreviousFindingsDisplay`) correctly applied the placeholder conversion.

**Comparison:**
```csharp
// ? BEFORE FIX: No placeholder conversion
public string PreviousFindingsEditorText
{
    get
    {
        if (PreviousProofreadMode && !string.IsNullOrWhiteSpace(tab.FindingsProofread))
        {
            return tab.FindingsProofread; // Raw text with literal {arrow}, {DDx}, etc.
        }
        // ...fallback logic...
    }
}

// ? AFTER FIX: Placeholder conversion applied
public string PreviousFindingsEditorText
{
    get
    {
        if (PreviousProofreadMode && !string.IsNullOrWhiteSpace(tab.FindingsProofread))
        {
            return ApplyProofreadPlaceholders(tab.FindingsProofread); // Converts {arrow} ⊥ -->
        }
        // ...fallback logic...
    }
}
```

---

## Solution

### Fix 1: Real-Time Notifications

Added explicit property change notifications in `ApplyPreviousJsonFields()` when proofread fields are updated via JSON:

**File**: `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.PreviousStudies.Json.cs`

```csharp
private void ApplyPreviousJsonFields(PreviousStudyTab tab, JsonElement root)
{
    _updatingPrevFromJson = true;
    
    bool changed = false;
    bool proofreadFieldsChanged = false; // NEW: Track proofread field changes
    
    // ... existing field updates ...
    
    // CRITICAL: Findings proofread - notify editor property for real-time updates
    if (root.TryGetProperty("findings_proofread", out var fpEl) && fpEl.ValueKind == JsonValueKind.String)
    {
        string value = fpEl.GetString() ?? string.Empty;
        if (tab.FindingsProofread != value) 
        { 
            tab.FindingsProofread = value; 
            changed = true; 
            proofreadFieldsChanged = true; // NEW: Mark for notification
            Debug.WriteLine($"[PrevJson] findings_proofread updated, length={value.Length}");
        }
    }
    
    // CRITICAL: Conclusion proofread - notify editor property for real-time updates
    if (root.TryGetProperty("conclusion_proofread", out var clpEl) && clpEl.ValueKind == JsonValueKind.String)
    {
        string value = clpEl.GetString() ?? string.Empty;
        if (tab.ConclusionProofread != value) 
        { 
            tab.ConclusionProofread = value; 
            changed = true; 
            proofreadFieldsChanged = true; // NEW: Mark for notification
            Debug.WriteLine($"[PrevJson] conclusion_proofread updated, length={value.Length}");
        }
    }
    
    // ... existing splitter range updates ...
    
    // CRITICAL FIX: Immediately notify editor properties when proofread fields change
    // This ensures real-time updates when findings_proofread/conclusion_proofread are populated via JSON
    if (proofreadFieldsChanged)
    {
        Debug.WriteLine("[PrevJson] Proofread fields changed - notifying editor properties for real-time updates");
        OnPropertyChanged(nameof(PreviousFindingsEditorText));
        OnPropertyChanged(nameof(PreviousConclusionEditorText));
    }
    
    if (changed) UpdatePreviousReportJson();
    else OnPropertyChanged(nameof(PreviousReportJson));
}
```

### Fix 2: Placeholder Conversion

Updated `PreviousFindingsEditorText` and `PreviousConclusionEditorText` to apply placeholder conversion:

**File**: `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.PreviousStudies.Properties.cs`

```csharp
public string PreviousFindingsEditorText
{
    get
    {
        var tab = SelectedPreviousStudy;
        if (tab == null) return _prevHeaderAndFindingsCache ?? string.Empty;
        
        // Proofread mode: use proofread version AND apply placeholder conversion
        if (PreviousProofreadMode && !string.IsNullOrWhiteSpace(tab.FindingsProofread))
        {
            return ApplyProofreadPlaceholders(tab.FindingsProofread); // CRITICAL FIX
        }
        
        // Fallback: splitted mode uses split version, otherwise original
        if (PreviousReportSplitted)
        {
            return tab.FindingsOut ?? string.Empty;
        }
        else
        {
            return tab.Findings ?? string.Empty;
        }
    }
}

public string PreviousConclusionEditorText
{
    get
    {
        var tab = SelectedPreviousStudy;
        if (tab == null) return _prevFinalConclusionCache ?? string.Empty;
        
        // Proofread mode: use proofread version AND apply placeholder conversion
        if (PreviousProofreadMode && !string.IsNullOrWhiteSpace(tab.ConclusionProofread))
        {
            return ApplyProofreadPlaceholders(tab.ConclusionProofread); // CRITICAL FIX
        }
        
        // Fallback: splitted mode uses split version, otherwise original
        if (PreviousReportSplitted)
        {
            return tab.ConclusionOut ?? string.Empty;
        }
        else
        {
            return tab.Conclusion ?? string.Empty;
        }
    }
}
```

### Key Changes

1. **Added `proofreadFieldsChanged` flag**: Tracks when `findings_proofread` or `conclusion_proofread` fields are updated
2. **Immediate notifications**: When proofread fields change, immediately notify computed properties
3. **Placeholder conversion**: Apply `ApplyProofreadPlaceholders()` to proofread text before returning
4. **Debug logging**: Added clear logging for troubleshooting proofread updates

---

## Placeholder Conversion Details

### Supported Placeholders

| Placeholder | Default Value | Source                               |
|-------------|---------------|--------------------------------------|
| `{DDx}`     | `"DDx:"`      | `ReportifySettingsJson.defaults.differential_diagnosis` |
| `{arrow}`   | `"-->"`       | `ReportifySettingsJson.defaults.arrow` |
| `{bullet}`  | `"-"`         | `ReportifySettingsJson.defaults.detailing_prefix` |

### ApplyProofreadPlaceholders() Logic

**File**: `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Editor.cs`

```csharp
private string ApplyProofreadPlaceholders(string text)
{
    if (string.IsNullOrWhiteSpace(text)) return text;
    
    // Get defaults from reportify settings JSON
    string ddx = "DDx:";  // default fallback
    string arrow = "-->";  // default fallback
    string bullet = "-";  // default fallback
    
    try
    {
        if (!string.IsNullOrWhiteSpace(_tenant?.ReportifySettingsJson))
        {
            using var doc = JsonDocument.Parse(_tenant.ReportifySettingsJson);
            var root = doc.RootElement;
            
            if (root.TryGetProperty("defaults", out var defaults))
            {
                if (defaults.TryGetProperty("differential_diagnosis", out var ddxEl))
                    ddx = ddxEl.GetString() ?? ddx;
                
                if (defaults.TryGetProperty("arrow", out var arrowEl))
                    arrow = arrowEl.GetString() ?? arrow;
                
                if (defaults.TryGetProperty("detailing_prefix", out var bulletEl))
                    bullet = bulletEl.GetString() ?? bullet;
            }
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"[ProofreadPlaceholders] JSON parse error: {ex.Message}");
        // Fall through to use defaults
    }
    
    // Apply replacements (case-insensitive)
    text = Regex.Replace(text, @"\{DDx\}", ddx, RegexOptions.IgnoreCase);
    text = Regex.Replace(text, @"\{arrow\}", arrow, RegexOptions.IgnoreCase);
    text = Regex.Replace(text, @"\{bullet\}", bullet, RegexOptions.IgnoreCase);
    
    return text;
}
```

### Example Conversion

**Input (proofread text):**
```
Findings suggest pneumonia. {DDx} Infection vs inflammation.
Follow-up {arrow} 2 weeks.
{bullet} Chest X-ray
{bullet} CBC
```

**Output (after ApplyProofreadPlaceholders):**
```
Findings suggest pneumonia. DDx: Infection vs inflammation.
Follow-up --> 2 weeks.
- Chest X-ray
- CBC
```

---

## How It Works

### Data Flow: JSON ⊥ Editor (Real-Time with Placeholders)

```
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛 Automation Module                                                弛
弛 忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖弛
弛 弛 Populates findings_proofread with placeholders in JSON       弛弛
弛 弛 Example: "Findings: {arrow} Follow-up. {DDx} Pneumonia."    弛弛
弛 戌式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎弛
戌式式式式式式式式式式式式式式托式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
               弛
               ∪
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛 ApplyJsonToPrevious(json)                                         弛
弛 戍式 Parse JSON                                                     弛
弛 戌式 ApplyPreviousJsonFields(tab, root)                             弛
弛    戍式 Read findings_proofread from JSON                           弛
弛    戍式 Update tab.FindingsProofread = value                        弛
弛    戍式 Set proofreadFieldsChanged = true          ∠ FIX 1         弛
弛    戌式 OnPropertyChanged(PreviousFindingsEditorText) ∠ FIX 1      弛
戌式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
               弛
               ∪
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛 WPF Binding Update                                                弛
弛 忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛 弛 EditorPreviousFindings.DocumentText (XAML)                     弛
弛 弛   Binding: PreviousFindingsEditorText (Mode=OneWay)            弛
弛 戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛 弛 Computed Property: PreviousFindingsEditorText                  弛
弛 弛   If PreviousProofreadMode && !empty(FindingsProofread)        弛
弛 弛     Return ApplyProofreadPlaceholders(FindingsProofread) ∠ FIX 2弛
弛 弛   Else If PreviousReportSplitted                               弛
弛 弛     Return FindingsOut                                         弛
弛 弛   Else                                                         弛
弛 弛     Return Findings                                            弛
弛 戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
戌式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
               弛
               ∪
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛 ApplyProofreadPlaceholders(text)                                  弛
弛 戍式 Read ReportifySettingsJson.defaults                            弛
弛 戍式 {arrow} ⊥ "-->" (or custom from settings)                     弛
弛 戍式 {DDx} ⊥ "DDx:" (or custom from settings)                      弛
弛 戌式 {bullet} ⊥ "-" (or custom from settings)                      弛
戌式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
               弛
               ∪
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛 Editor UI Updates in Real-Time with Converted Placeholders ?     弛
弛 User sees: "Findings: --> Follow-up. DDx: Pneumonia."            弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
```

---

## Behavior After Fix

### Scenario 1: Proofread Toggle ON, Proofread Text with Placeholders Arrives

**Timeline:**
1. User enables "Proofread" toggle for previous report
2. Automation module populates `findings_proofread: "Findings {arrow} Follow-up"` in JSON
3. ? **Real-time update with placeholder conversion**: Editor **immediately** shows `"Findings --> Follow-up"`
4. User sees live changes with correct placeholder replacements

### Scenario 2: Custom Reportify Defaults

**Timeline:**
1. Tenant has custom reportify settings: `arrow: "⊥"`, `ddx: "Differential:"`, `bullet: "?"`
2. Proofread text contains `"{arrow}"`, `"{DDx}"`, `"{bullet}"`
3. ? Editor shows custom replacements: `"⊥"`, `"Differential:"`, `"?"`

### Scenario 3: Proofread Toggle OFF, Then ON

**Timeline:**
1. Proofread toggle is OFF (editor shows original findings)
2. Automation populates `findings_proofread` with placeholders in background
3. User enables "Proofread" toggle
4. ? Editor **immediately** switches to proofread text with converted placeholders

---

## Testing

### Test Case 1: Real-Time Update with Placeholders

**Steps:**
1. Open previous report panel
2. Enable "Proofread" toggle
3. Trigger automation that populates `findings_proofread: "Test {arrow} Follow-up. {DDx} Infection."`
4. **Expected**: Editor updates **immediately** showing `"Test --> Follow-up. DDx: Infection."`

### Test Case 2: Custom Reportify Defaults

**Steps:**
1. Configure custom reportify settings with non-default values
2. Enable "Proofread" toggle for previous report
3. Trigger automation with placeholder-containing proofread text
4. **Expected**: Editor shows **custom** placeholder values

### Test Case 3: Placeholder Case-Insensitivity

**Steps:**
1. Proofread text contains `{ARROW}`, `{arrow}`, `{Arrow}` (mixed case)
2. Enable "Proofread" toggle
3. **Expected**: All variations are converted correctly (regex is case-insensitive)

### Test Case 4: Missing Placeholder Definition

**Steps:**
1. ReportifySettingsJson has no `defaults.arrow` property
2. Proofread text contains `{arrow}`
3. **Expected**: Falls back to default value `"-->"`

---

## Debug Logging

Added debug output for troubleshooting:

```
[PrevJson] findings_proofread updated, length=XXX
[PrevJson] conclusion_proofread updated, length=XXX
[PrevJson] Proofread fields changed - notifying editor properties for real-time updates
[ProofreadPlaceholders] JSON parse error: <error message> (only if settings parse fails)
```

**Logs help diagnose:**
- When proofread fields are updated via JSON
- When notifications are triggered
- Length of proofread text received
- Placeholder conversion errors (if any)

---

## Impact on Existing Features

### ? No Breaking Changes

- **Splitted Mode**: Still works correctly (fallback logic unchanged)
- **Original Text**: Still shows when Proofread toggle is OFF
- **Empty Proofread**: Gracefully falls back to original text
- **Tab Switching**: Correctly preserves per-tab proofread data
- **Other Display Properties**: Already had placeholder conversion, now consistent

### ? Performance

- Minimal overhead: Placeholder conversion only runs when displaying proofread text
- Regex compilation: Replacements use efficient case-insensitive regex
- Notifications only triggered when proofread fields actually change
- Efficient computed property: Only evaluates once per notification

---

## Related Features

### Current Report Proofread Mode

This fix follows the same pattern as the current report proofread implementation:

**File**: `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Editor.cs`

```csharp
// Current report also applies placeholder conversion
public string FindingsDisplay
{
    get
    {
        if (ProofreadMode && !string.IsNullOrWhiteSpace(_findingsProofread))
        {
            return ApplyProofreadPlaceholders(_findingsProofread); // Same pattern
        }
        // ...fallback logic...
    }
}
```

### Consistency Across Properties

**Before Fix:**
- ? `PreviousFindingsDisplay` had placeholder conversion
- ? `PreviousFindingsEditorText` did NOT have placeholder conversion (inconsistent)

**After Fix:**
- ? `PreviousFindingsDisplay` has placeholder conversion
- ? `PreviousFindingsEditorText` has placeholder conversion (consistent)

---

## Files Modified

### Core Logic
1. **`apps/Wysg.Musm.Radium/ViewModels/MainViewModel.PreviousStudies.Json.cs`**
   - Added `proofreadFieldsChanged` flag
   - Added immediate notifications for `PreviousFindingsEditorText` and `PreviousConclusionEditorText`
   - Added debug logging

2. **`apps/Wysg.Musm.Radium/ViewModels/MainViewModel.PreviousStudies.Properties.cs`**
   - Updated `PreviousFindingsEditorText` to call `ApplyProofreadPlaceholders()`
   - Updated `PreviousConclusionEditorText` to call `ApplyProofreadPlaceholders()`
   - Added comments explaining the placeholder conversion

### Unchanged (Already Correct)
1. **`apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Editor.cs`**
   - `ApplyProofreadPlaceholders()` method already existed and works correctly
   - Reused for previous reports (no duplication)

2. **`apps/Wysg.Musm.Radium/Controls/PreviousReportEditorPanel.xaml`**
   - Binding already correct: `DocumentText="{Binding PreviousFindingsEditorText, Mode=OneWay}"`
   - No changes needed

---

## Future Enhancements

### Additional Placeholders

Current placeholders:
- `{DDx}` - Differential diagnosis
- `{arrow}` - Follow-up arrow
- `{bullet}` - Detailing bullet point

Potential additions:
- `{sep}` - Section separator
- `{nl}` - New line
- `{tab}` - Tab indentation
- Custom tenant-specific placeholders

### Placeholder Preview

Add UI to show placeholder values before applying:
- Tooltip on hover showing `{arrow}` will become `"-->"`
- Settings page showing all configured placeholder values
- Real-time preview as user types proofread text

---

## Conclusion

This fix ensures **two critical improvements**:

1. **Real-time updates**: When proofread toggle is ON and `findings_proofread` gets populated in JSON, the previous findings editor updates **immediately**
2. **Placeholder conversion**: Placeholders like `{arrow}`, `{DDx}`, and `{bullet}` are correctly replaced with reportify defaults, matching the behavior of current report proofread mode

Both fixes are minimal, efficient, and follow established patterns from the current report proofread feature.

**Key Achievement**: When proofread toggle is ON and automation populates `findings_proofread: "Test {arrow} Follow-up"` in JSON, the editor immediately shows `"Test --> Follow-up"` without any manual interaction required.

---

**Status**: ? Fixed  
**Build**: ? Success  
**Testing**: ? Pending User Validation
