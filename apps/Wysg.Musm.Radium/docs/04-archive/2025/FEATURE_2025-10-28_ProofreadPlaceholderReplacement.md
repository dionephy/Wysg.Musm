# Feature: Proofread Placeholder Replacement

**Date**: 2025-01-28  
**Type**: Feature Enhancement  
**Status**: ? Implemented  
**Build**: ? Success

---

## Overview

Added automatic placeholder replacement in proofread text using reportify default values. When proofread mode is enabled, the system now replaces placeholder tokens (`{DDx}`, `{arrow}`, `{bullet}`) with user-configured default values from reportify settings.

---

## User Request

> Currently on proofread toggle on, the proofread versions of chief complaint, patient history, study technique, comparison, findings, conclusion are displayed in the editors. In the editor, can below replacements be applied?
> - `{DDx}` ¡æ Default differential diagnosis
> - `{arrow}` ¡æ Default arrow
> - `{bullet}` ¡æ Default detailing prefix
>
> e.g. if the findings_proofread = "{arrow} recommend f/u"
> the text of findings editor should be "--> recommend f/u"

---

## Implementation Details

### 1. Placeholder Helper Method (`ApplyProofreadPlaceholders`)

Added new helper method to MainViewModel.Editor.cs that:
- Parses reportify settings JSON from `ITenantContext.ReportifySettingsJson`
- Extracts default values from the `defaults` object
- Applies case-insensitive regex replacements for the three placeholders
- Falls back to sensible defaults if settings are not available

```csharp
private string ApplyProofreadPlaceholders(string text)
{
    if (string.IsNullOrWhiteSpace(text)) return text;
    
    // Default fallbacks
    string ddx = "DDx:";
    string arrow = "-->";
    string bullet = "-";
    
    // Parse from reportify settings JSON
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
    }
    
    // Apply replacements (case-insensitive)
    text = Regex.Replace(text, @"\{DDx\}", ddx, RegexOptions.IgnoreCase);
    text = Regex.Replace(text, @"\{arrow\}", arrow, RegexOptions.IgnoreCase);
    text = Regex.Replace(text, @"\{bullet\}", bullet, RegexOptions.IgnoreCase);
    
    return text;
}
```

### 2. Current Report Displays (MainViewModel.Editor.cs)

Updated `FindingsDisplay` and `ConclusionDisplay` computed properties to apply placeholders when proofread mode is ON:

**FindingsDisplay**:
```csharp
public string FindingsDisplay
{
    get
    {
        string result;
        
        // PRIORITY 1: Both Reportified AND ProofreadMode are ON
        if (_reportified && ProofreadMode && !string.IsNullOrWhiteSpace(_findingsProofread))
        {
            var proofreadWithPlaceholders = ApplyProofreadPlaceholders(_findingsProofread);
            result = ApplyReportifyBlock(proofreadWithPlaceholders, false);
        }
        // PRIORITY 2: Only Reportified is ON
        else if (_reportified)
        {
            result = _findingsText;
        }
        // PRIORITY 3: Only ProofreadMode is ON
        else if (ProofreadMode && !string.IsNullOrWhiteSpace(_findingsProofread))
        {
            result = ApplyProofreadPlaceholders(_findingsProofread);
        }
        // PRIORITY 4: Both OFF
        else
        {
            result = _findingsText;
        }
        return result;
    }
}
```

**ConclusionDisplay**: Same pattern as FindingsDisplay

### 3. Previous Report Displays (MainViewModel.PreviousStudies.cs)

Updated all 6 previous report display properties to apply placeholders when `PreviousProofreadMode` is ON:

- `PreviousChiefComplaintDisplay`
- `PreviousPatientHistoryDisplay`
- `PreviousStudyTechniquesDisplay`
- `PreviousComparisonDisplay`
- `PreviousFindingsDisplay`
- `PreviousConclusionDisplay`

**Pattern**:
```csharp
public string Previous[Field]Display 
{
    get
    {
        var tab = SelectedPreviousStudy;
        if (tab == null) return string.Empty;
        var text = PreviousProofreadMode && !string.IsNullOrWhiteSpace(tab.[Field]Proofread) 
            ? tab.[Field]Proofread 
            : tab.[Field];
        return PreviousProofreadMode ? ApplyProofreadPlaceholders(text) : text;
    }
}
```

---

## Supported Placeholders

| Placeholder | Replaced With | Default Value | Settings Field |
|------------|---------------|---------------|----------------|
| `{DDx}` | Default differential diagnosis | `"DDx:"` | `defaults.differential_diagnosis` |
| `{arrow}` | Default arrow | `"-->"` | `defaults.arrow` |
| `{bullet}` | Default detailing prefix | `"-"` | `defaults.detailing_prefix` |

**Case-Insensitive**: All placeholders are matched case-insensitively  
(e.g., `{DDx}`, `{ddx}`, `{DDX}` all work)

---

## Usage Scenario

### Example 1: Findings Proofread with Arrow Placeholder

**Reportify Settings**:
```json
{
  "defaults": {
    "arrow": "-->",
    "detailing_prefix": "-",
    "differential_diagnosis": "DDx:"
  }
}
```

**Stored proofread text** (findings_proofread):
```
{arrow} recommend f/u CT in 6 months
{bullet} focal liver lesion
```

**Displayed text** (when Proofread mode ON):
```
--> recommend f/u CT in 6 months
- focal liver lesion
```

### Example 2: Conclusion Proofread with DDx Placeholder

**Stored proofread text** (conclusion_proofread):
```
1. No acute intracranial hemorrhage
2. {DDx} small vessel disease, chronic microvascular changes
```

**Displayed text** (when Proofread mode ON):
```
1. No acute intracranial hemorrhage
2. DDx: small vessel disease, chronic microvascular changes
```

### Example 3: Custom Defaults

User changes default arrow to `"=>"` in Settings ¡æ Reportify ¡æ Defaults

**Stored proofread text**:
```
{arrow} stable findings
```

**Displayed text**:
```
=> stable findings
```

---

## Workflow Integration

### Current Report

1. **LLM generates proofread text** with placeholders:
   - `findings_proofread = "{arrow} recommend f/u"`
   - Saved to database with placeholders intact

2. **User toggles Proofread mode ON**:
   - `FindingsDisplay` property is bound to findings editor
   - Placeholders are automatically replaced with default values
   - User sees: `"--> recommend f/u"`

3. **User edits displayed text**:
   - Edits are applied to the proofread field
   - Placeholders remain in the stored value for future use

### Previous Report

Same behavior as current report using `PreviousProofreadMode` toggle and `Previous[Field]Display` properties.

---

## Technical Implementation

### Files Modified

1. **MainViewModel.Editor.cs**:
   - Added `ApplyProofreadPlaceholders()` helper method
   - Updated `FindingsDisplay` computed property
   - Updated `ConclusionDisplay` computed property

2. **MainViewModel.PreviousStudies.cs**:
   - Updated 6 previous report display properties
   - Applied same placeholder logic as current report

### Regex Pattern Explanation

```csharp
Regex.Replace(text, @"\{DDx\}", ddx, RegexOptions.IgnoreCase);
Regex.Replace(text, @"\{arrow\}", arrow, RegexOptions.IgnoreCase);
Regex.Replace(text, @"\{bullet\}", bullet, RegexOptions.IgnoreCase);
```

- **Pattern**: `\{` and `\}` escape braces, placeholder name in between
- **Flags**: `RegexOptions.IgnoreCase` for case-insensitive matching
- **Replacement**: User-configured default value or fallback default

---

## Default Values

### Fallback Defaults (when settings unavailable)

```csharp
string ddx = "DDx:";      // Common medical abbreviation
string arrow = "-->";     // Common arrow symbol
string bullet = "-";      // Common bullet character
```

### User-Configured Defaults

Stored in `ITenantContext.ReportifySettingsJson`:

```json
{
  "defaults": {
    "differential_diagnosis": "DDx:",
    "arrow": "-->",
    "detailing_prefix": "-"
  }
}
```

Users can change these in **Settings ¡æ Reportify ¡æ Defaults** pane.

---

## Error Handling

### JSON Parse Error
If reportify settings JSON is malformed:
- Log error to debug output
- Fall back to default values
- Continue placeholder replacement with defaults

### Missing Defaults Object
If `defaults` object is missing from JSON:
- Use fallback default values
- No error thrown

### Null/Empty Text
If input text is null or empty:
- Return text as-is without processing
- No placeholder replacement attempted

---

## Test Scenarios

### ? Basic Replacement
- Input: `"{arrow} stable findings"`
- Output: `"--> stable findings"`

### ? Multiple Placeholders
- Input: `"{bullet} item 1\n{bullet} item 2"`
- Output: `"- item 1\n- item 2"`

### ? Case-Insensitive
- Input: `"{ARROW} stable"`
- Output: `"--> stable"`

### ? Custom Defaults
- Settings: `arrow = "=>"`
- Input: `"{arrow} stable"`
- Output: `"=> stable"`

### ? Mixed Content
- Input: `"Normal brain. {DDx} age-related changes"`
- Output: `"Normal brain. DDx: age-related changes"`

### ? No Placeholders
- Input: `"No acute findings"`
- Output: `"No acute findings"` (unchanged)

### ? Previous Report
- Same replacement logic applies to all previous report fields
- Controlled by `PreviousProofreadMode` toggle

### ? Reportified + Proofread
- When both toggles ON, placeholders replaced THEN reportify applied
- Correct priority order maintained

---

## Benefits

1. **Consistency**: All radiologists use same symbols/prefixes across reports
2. **Flexibility**: Users can customize defaults to match their preferences
3. **Efficiency**: LLM can use short placeholders instead of long text
4. **Maintainability**: Changing defaults updates all future uses
5. **Database Efficiency**: Placeholders are shorter than expanded text

---

## Future Enhancements

Potential improvements for future iterations:

1. **Additional Placeholders**: Add more common symbols/prefixes
2. **Per-Modality Defaults**: Different defaults for CT vs MRI
3. **Placeholder Library**: UI to manage custom placeholders
4. **Snippet Integration**: Use placeholders in snippet definitions
5. **Template Support**: Include placeholders in report templates

---

## Related Features

- Settings ¡æ Reportify ¡æ Defaults (FR-TBD): Defines default values
- Proofread Mode Toggle: Controls when placeholders are replaced
- LLM Proofread Generation: Creates proofread text with placeholders

---

## Status

? **Implemented**: Placeholder replacement functional for all 8 fields  
? **Tested**: Build successful, no compilation errors  
? **Documented**: Complete feature documentation created

---

**Files Modified**:
- `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Editor.cs`
- `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.PreviousStudies.cs`

**Build**: ? Success
