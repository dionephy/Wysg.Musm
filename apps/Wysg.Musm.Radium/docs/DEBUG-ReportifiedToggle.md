# DEBUG: Reportified Toggle Not Working

**Date**: 2025-01-24  
**Status**: ?? Debugging  
**Issue**: Reportified toggle only adds a space at the start instead of applying full transformation

---

## Debug Logging Added

Debug logging has been added to the following methods to trace the execution flow:

### 1. `FindingsDisplay` and `ConclusionDisplay` Properties
- Logs which branch is taken (Reportified=true, ProofreadMode=true, or both OFF)
- Logs the length of the returned string

**Example output**:
```
[FindingsDisplay] Reportified=true, returning _findingsText length=150
```

### 2. `Reportified` Property Setter
- Logs old and new values
- Logs when PropertyChanged is raised
- Logs when transformation is skipped

**Example output**:
```
[Editor.Reportified] Setter called: old=false, new=true
[Editor.Reportified] PropertyChanged raised, _reportified is now=true
[Editor.ToggleReportified] START: applying transformations for reportified=true
[Editor.ToggleReportified] END: transformations applied
```

### 3. `FindingsText` and `ConclusionText` Setters
- Now notify `FindingsDisplay` and `ConclusionDisplay` when values change
- This ensures XAML bindings update correctly

---

## Testing Steps

### Step 1: Enable Debug Output Window
1. In Visual Studio, go to **View ¡æ Output**
2. In the Output dropdown, select **Debug**

### Step 2: Test Reportified Toggle (Simple Case)

1. **Start the application**
2. **Enter raw text in Findings**:
   ```
   no acute intracranial hemorrhage
   no acute skull fracture
   ```
3. **Toggle Reportified ON**
4. **Check Output window** for debug messages:
   ```
   [Editor.Reportified] Setter called: old=false, new=true
   [Editor.ToggleReportified] START: applying transformations for reportified=true
   [Editor.ToggleReportified] END: transformations applied
   [FindingsDisplay] Reportified=true, returning _findingsText length=XXX
   ```
5. **Expected result in editor**:
   ```
   No acute intracranial hemorrhage.
   No acute skull fracture.
   ```

### Step 3: Test Reportified Toggle (Conclusion with Numbering)

1. **Enter raw text in Conclusion**:
   ```
   no acute intracranial hemorrhage
   no acute skull fracture
   normal study
   ```
2. **Toggle Reportified ON**
3. **Expected result** (if `NumberConclusionParagraphs = true`):
   ```
   1. No acute intracranial hemorrhage.
   2. No acute skull fracture.
   3. Normal study.
   ```

### Step 4: Test Priority Logic

**Test Case 1: Both Reportified and Proofread OFF**
- Expected: Shows raw text (editable)
- Debug output: `[FindingsDisplay] Both OFF, returning _findingsText`

**Test Case 2: Proofread ON, Reportified OFF**
- Expected: Shows proofread text (read-only)
- Debug output: `[FindingsDisplay] ProofreadMode=true only, returning _findingsProofread`

**Test Case 3: Reportified ON, Proofread OFF**
- Expected: Shows reportified(raw) text (read-only)
- Debug output: `[FindingsDisplay] Reportified=true only, returning _findingsText`

**Test Case 4: BOTH ON (NEW BEHAVIOR)**
- Expected: Shows **reportified(proofread)** text (read-only)
- Debug output: `[FindingsDisplay] BOTH ON, returning reportified(proofread)`
- Example:
  - Raw: `no acute findings`
  - Proofread: `No acute intracranial findings`
  - Display shows: `No acute intracranial findings.` ¡ç Reportified version of proofread text

---

## Common Issues and Solutions

### Issue 1: Only a Space is Added at Start

**Symptom**: When toggling Reportified ON, the text only gets a single space added at the beginning, no other transformations applied.

**Possible Causes**:
1. **Reportify config is not loaded** - Check if `_tenant.ReportifySettingsJson` is null
2. **All transformations are disabled** - Check reportify settings JSON
3. **Input text is empty or whitespace** - `ApplyReportifyBlock` returns empty string

**Debug Steps**:
1. Add breakpoint in `ApplyReportifyBlock` method
2. Check value of `cfg` - verify transformation flags are `true`
3. Step through the transformation loops to see which transformations are being applied
4. Check `lines2[i]` values after each transformation step

### Issue 2: Reportified Text Not Showing in Editor

**Symptom**: Toggle appears to be ON, but editor still shows raw text.

**Possible Causes**:
1. **XAML trigger not activating** - Binding issue
2. **Display property not notified** - PropertyChanged not raised
3. **Raw and reportified text are the same** - Transformation didn't change anything

**Debug Steps**:
1. Check Output window for `[FindingsDisplay]` messages
2. Verify which value is being returned (raw vs reportified)
3. Check if `_reportified` flag is actually `true` when display property is accessed
4. Use Snoop or Live Visual Tree to inspect XAML binding at runtime

### Issue 3: Reportified Text Not Saved to Database

**Symptom**: When saving while Reportified is ON, the formatted text is saved instead of raw.

**Status**: ? This is already FIXED

The `UpdateCurrentReportJson` method correctly uses raw values:
```csharp
header_and_findings = _reportified ? _rawFindings : (FindingsText ?? string.Empty),
final_conclusion = _reportified ? _rawConclusion : (ConclusionText ?? string.Empty),
```

---

## Reportify Settings

The reportify transformations are controlled by a JSON configuration. Check the current settings:

**Location**: `_tenant.ReportifySettingsJson`

**Default Settings**:
```json
{
  "remove_excessive_blanks": true,
  "remove_excessive_blank_lines": true,
  "capitalize_sentence": true,
  "ensure_trailing_period": true,
  "normalize_arrows": true,
  "normalize_bullets": true,
  "space_after_punctuation": true,
  "normalize_parentheses": true,
  "space_number_unit": true,
  "collapse_whitespace": true,
  "number_conclusion_paragraphs": true,
  "indent_continuation_lines": true,
  "number_conclusion_lines_on_one_paragraph": false,
  "capitalize_after_bullet_or_number": false,
  "defaults": {
    "arrow": "-->",
    "conclusion_numbering": "1.",
    "detailing_prefix": "-"
  }
}
```

**To check settings**:
1. Add breakpoint in `EnsureReportifyConfig` method
2. Inspect `cfg` object after parsing
3. Verify all boolean flags are `true` (default values)

---

## Next Steps

If the issue persists after reviewing debug output:

1. **Capture Debug Log**: Copy all debug output from toggling reportified ON
2. **Check Input Text**: Verify the raw text that's being transformed
3. **Check Transformation Result**: Add debug logging in `ApplyReportifyBlock` to see intermediate transformation steps
4. **Check XAML Binding**: Use Snoop to verify the `DocumentText` binding is actually switching to `FindingsDisplay`

---

## Related Files

- `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Editor.cs` - Display properties and Reportified toggle logic
- `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.ReportifyHelpers.cs` - Transformation implementation
- `apps\Wysg.Musm.Radium\Controls\CurrentReportEditorPanel.xaml` - XAML bindings with triggers
- `apps\Wysg.Musm.Radium\docs\FEATURE-ProofreadToggleBinding.md` - Feature documentation
- `apps\Wysg.Musm.Radium\docs\CRITICAL_FIX_2025-01-23_ReportifySavingWrongValues.md` - Previous reportify fix

---

**Status**: ?? Debug logging added, awaiting test results
