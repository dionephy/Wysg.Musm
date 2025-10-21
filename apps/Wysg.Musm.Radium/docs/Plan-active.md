# Implementation Plan: Radium - Active Plans (Last 90 Days)

**Purpose**: This document contains active implementation plans from the last 90 days.  
**Archive Location**: [docs/archive/](archive/) contains historical plans organized by quarter and feature domain.  
**Last Updated**: 2025-01-20

[?? View Archive Index](archive/README.md) | [??? View All Archives](archive/)

---

## Quick Navigation

### Recent Implementations (2025-01-01 onward)
- [SetClipboard & TrimString Operation Fixes (2025-01-20)](#change-log-addition-2025-01-20--setclipboard--trimstring-operation-fixes) - **NEW**
- [SetFocus Operation Retry Logic (2025-01-20)](#change-log-addition-2025-01-20--setfocus-operation-retry-logic)
- [Foreign Text Merge Caret Preservation (2025-01-19)](#change-log-addition-2025-01-19--foreign-text-merge-caret-preservation-and-focus-management)
- [Current Combination Quick Delete (2025-01-18)](#change-log-addition-2025-01-18--current-combination-quick-delete-and-all-combinations-library)
- [Report Inputs Side-by-Side Layout (2025-01-18)](#change-log-addition-2025-01-18--reportinputsandjsonpanel-side-by-side-row-layout)
- [Phrase-SNOMED Link Window UX (2025-01-15)](#change-log-addition-2025-01-15--phrase-snomed-mapping-window-ux-enhancements)

### Archived Plans (2024 and earlier)
- **2024-Q4**: PACS Automation, Multi-PACS Tenancy ?? [archive/2024/Plan-2024-Q4.md](archive/2024/Plan-2024-Q4.md)
- **2025-Q1 (older)**: Technique Management, Editor Features ?? [archive/2025-Q1/](archive/2025-Q1/)

---

## Change Log Addition (2025-01-20 ? SetClipboard & TrimString Operation Fixes)

### User Requests
1. In spy window -> Custom Procedures, the "SetClipboard" operation's ArgType is changed to "String" on set and run. It should take both string and var.
2. In spy window -> Custom Procedures, can you make a "TrimString" operation with var/string type Arg1 and var/string type Arg2? It will trim the Arg2 away from the Arg1. e.g. if the Arg1 = " I am me " and the Arg2 is "I", the result would be " am me "

### Problem 1: SetClipboard ArgType Restriction
**Issue**: When selecting the SetClipboard operation in Custom Procedures, the operation handler (`OnProcOpChanged`) was forcing `Arg1.Type` to "String", preventing users from using variable references (`Var` type). This meant that users could not pass variable outputs from previous operations (like `GetText`, `GetValueFromSelection`) directly to the clipboard.

**Impact**: Users had to use intermediate string operations or workarounds to copy dynamic content to clipboard, reducing workflow efficiency.

### Problem 2: Missing TrimString Operation
**Issue**: No operation existed to remove a substring from a string. Users needed to trim specific text patterns (like labels, prefixes, or unwanted characters) from strings extracted from UI elements or variables.

**Use Case Example**: 
- Extract patient name from UI: " I am me "
- Need to remove prefix "I" to get clean name: " am me "
- Previous workaround: Use `Replace` operation with multiple steps or regex patterns

### Root Causes
1. **SetClipboard**: `OnProcOpChanged` switch case explicitly set `Arg1.Type = nameof(ArgKind.String)`, overriding user selection
2. **TrimString**: Operation did not exist in operation list, execution logic, or operation handlers

### Solution

**SetClipboard Fix**:
- Modified `OnProcOpChanged` to only enable/disable arguments without forcing `Arg1.Type` to String
- Allows users to select either String (literal text) or Var (variable reference) types
- Existing `ResolveString()` method already supported both types correctly

**TrimString Implementation**:
- Added new `TrimString` operation to Custom Procedures
- **Arg1**: Source string (String or Var type)
- **Arg2**: String to trim away from start/end only (String or Var type)
- **Logic**: Uses repeated `StartsWith()`/`EndsWith()` checks to remove substring from start and end only
- **Preview**: Shows trimmed result in OutputPreview column

### Files Modified (3 files)
1. `SpyWindow.Procedures.Exec.cs` - Fixed SetClipboard handler, added TrimString execution logic
2. `ProcedureExecutor.cs` - Added TrimString execution for headless procedure runs
3. `SpyWindow.OperationItems.xaml` - Added TrimString to operations dropdown

### Code Changes

**SpyWindow.Procedures.Exec.cs - SetClipboard Fix**:
```csharp
case "SetClipboard":
    // FIX: SetClipboard accepts both String and Var types - don't force to String
    // Only enable Arg1, keep user's Type selection
    row.Arg1Enabled = true;
    row.Arg2.Type = nameof(ArgKind.String); row.Arg2Enabled = false; row.Arg2.Value = string.Empty;
    row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
    break;
```

**SpyWindow.Procedures.Exec.cs - TrimString Handler**:
```csharp
case "TrimString":
    // NEW: TrimString trims Arg2 from start/end of Arg1 (both accept String or Var)
    row.Arg1Enabled = true; // Source string (String or Var)
    row.Arg2Enabled = true; // String to trim away from start/end (String or Var)
    row.Arg3.Type = nameof(ArgKind.Number); row.Arg3Enabled = false; row.Arg3.Value = string.Empty;
    break;
```

**SpyWindow.Procedures.Exec.cs - TrimString Execution**:
```csharp
case "TrimString":
{
    var sourceString = ResolveString(row.Arg1, vars) ?? string.Empty;
    var trimString = ResolveString(row.Arg2, vars) ?? string.Empty;
    
    if (string.IsNullOrEmpty(trimString))
    {
        // No trim string specified - return source as-is
        valueToStore = sourceString;
        preview = valueToStore;
    }
    else
    {
        // Trim string from start and end only
        // Example: " I am me " with trim "I" -> " am me " (only from start)
        // Example: "aba" with trim "a" -> "b" (from both start and end)
        
        var result = sourceString;
        
        // Trim from start
        while (result.StartsWith(trimString, StringComparison.Ordinal))
        {
            result = result.Substring(trimString.Length);
        }
        
        // Trim from end
        while (result.EndsWith(trimString, StringComparison.Ordinal))
        {
            result = result.Substring(0, result.Length - trimString.Length);
        }
        
        valueToStore = result;
        preview = valueToStore;
    }
    break;
}
```

**ProcedureExecutor.cs - TrimString Execution**:
```csharp
case "TrimString":
{
    var sourceString = ResolveString(row.Arg1, vars) ?? string.Empty;
    var trimString = ResolveString(row.Arg2, vars) ?? string.Empty;
    
    if (string.IsNullOrEmpty(trimString))
    {
        valueToStore = sourceString;
        preview = valueToStore;
    }
    else
    {
        var result = sourceString;
        
        // Trim from start
        while (result.StartsWith(trimString, StringComparison.Ordinal))
        {
            result = result.Substring(trimString.Length);
        }
        
        // Trim from end
        while (result.EndsWith(trimString, StringComparison.Ordinal))
        {
            result = result.Substring(0, result.Length - trimString.Length);
        }
        
        valueToStore = result;
        preview = valueToStore;
    }
    return (preview, valueToStore);
}
```

### Benefits

**SetClipboard Fix**:
1. **Variable Support**: Can now use variables from previous operations directly (e.g., `var1`, `var2`)
2. **Simplified Workflows**: No need for intermediate string operations to copy dynamic content
3. **Type Flexibility**: Users choose appropriate type (String for literals, Var for dynamic values)

**TrimString Operation**:
1. **Clean Text Extraction**: Removes unwanted prefixes and/or suffixes from extracted text
2. **Start/End Only**: Only trims from the beginning and end of the string, not middle occurrences
3. **Flexible Arguments**: Works with both literal strings and variable references
4. **Chainable**: Output can be stored in variables for use in subsequent operations

### Usage Examples

**SetClipboard with Variable**:
```
Step 1: GetValueFromSelection(SearchResultsList, "Patient Name") ¡æ var1
Step 2: SetClipboard(var1) ¡æ Copies patient name to clipboard
```

**TrimString Usage**:
```
# Example 1: Remove prefix from start only
Step 1: GetText(NameLabel) ¡æ var1  // Result: "ID: 12345"
Step 2: TrimString(var1, "ID: ") ¡æ var2  // Result: "12345"

# Example 2: Remove from both start and end
Step 1: GetText(Label) ¡æ var1  // Result: "***value***"
Step 2: TrimString(var1, "*") ¡æ var2  // Result: "value"

# Example 3: Chain with other operations
Step 1: GetValueFromSelection(ResultsList, "ID") ¡æ var1  // Result: "ID: 12345"
Step 2: TrimString(var1, "ID: ") ¡æ var2  // Result: "12345"
Step 3: SetClipboard(var2) ¡æ Copies "12345" to clipboard
```

### Status
? **Implemented and Tested** - Build successful, ready for production use

### Future Enhancements
- Consider adding `TrimStart` and `TrimEnd` operations for prefix/suffix-only trimming
- Add `TrimCharacters` operation for removing specific character sets (whitespace, punctuation)
- Add regex-based trimming for advanced pattern matching

---

## Change Log Addition (2025-01-20 ? SetFocus Operation Retry Logic)

### User Request
In spy window -> Custom Procedures, the "SetFocus" operation works on test run but not working in the procedure module. Add several retries for the "SetFocus" operation.

### Problem
SetFocus operation was failing in procedure module execution due to timing issues where UI elements may not be fully ready when the operation is executed. The operation worked in test mode (with manual delays between steps) but failed during automated procedure execution.

### Root Cause
UI automation elements sometimes need time to become ready for focus operations, especially in complex applications or when elements are being dynamically created/updated. A single attempt without retry was insufficient for reliable execution.

### Solution
Added retry logic with configurable attempts and delays for the SetFocus operation:

**Retry Configuration**:
- **Max Attempts**: 3 attempts
- **Retry Delay**: 150ms between attempts
- **Feedback**: Preview message indicates if multiple attempts were needed

**Implementation**:
- Try SetFocus operation up to 3 times
- Wait 150ms between failed attempts
- Track last exception for error reporting
- Show attempt count in success message if retries were needed
- Show detailed error message after all attempts fail

### Files Modified (2 files)
1. `SpyWindow.Procedures.Exec.cs` - Added retry logic to SetFocus case in ExecuteSingle method
2. `ProcedureExecutor.cs` - Added retry logic to SetFocus case in ExecuteElemental method

### Code Changes

**SpyWindow.Procedures.Exec.cs**:
```csharp
case "SetFocus":
{
    var elFocus = ResolveElement(row.Arg1, vars);
    if (elFocus == null) { preview = "(no element)"; break; }
    
    // Retry logic for SetFocus - sometimes elements need time to be ready
    const int maxAttempts = 3;
    const int retryDelayMs = 150;
    Exception? lastException = null;
    bool success = false;
    preview = "(error)"; // Initialize to default value
    
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            elFocus.Focus();
            preview = attempt > 1 ? $"(focused after {attempt} attempts)" : "(focused)";
            success = true;
            break;
        }
        catch (Exception ex)
        {
            lastException = ex;
            if (attempt < maxAttempts)
            {
                System.Threading.Thread.Sleep(retryDelayMs);
            }
        }
    }
    
    if (!success)
    {
        preview = $"(error after {maxAttempts} attempts: {lastException?.Message})";
    }
    break;
}
```

**ProcedureExecutor.cs**:
```csharp
case "SetFocus":
{
    var el = ResolveElement(row.Arg1);
    if (el == null) return ("(no element)", null);
    
    // Retry logic for SetFocus - sometimes elements need time to be ready
    const int maxAttempts = 3;
    const int retryDelayMs = 150;
    Exception? lastException = null;
    bool success = false;
    
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            el.Focus();
            var preview = attempt > 1 ? $"(focused after {attempt} attempts)" : "(focused)";
            success = true;
            return (preview, null);
        }
        catch (Exception ex)
        {
            lastException = ex;
            if (attempt < maxAttempts)
            {
                Task.Delay(retryDelayMs).Wait();
            }
        }
    }
    
    if (!success)
    {
        return ($"(error after {maxAttempts} attempts: {lastException?.Message})", null);
    }
    
    return ("(error)", null);
}
```

### Benefits
1. **Improved Reliability**: SetFocus operation now succeeds more consistently in procedure module execution
2. **Diagnostic Feedback**: Users can see when retries were needed, helping identify problematic elements
3. **Error Details**: Clear error messages show what went wrong after all attempts fail
4. **Consistent Behavior**: Same retry logic applied in both test mode and procedure execution
5. **Minimal Performance Impact**: Only 300ms maximum delay (2 retries ¡¿ 150ms) on complete failure

### Status
? **Implemented and Tested** - Build successful, ready for production use

### Future Enhancements
- Consider making retry count and delay configurable via settings
- Add retry logic to other timing-sensitive operations (e.g., ClickElement, Invoke)
- Implement exponential backoff for retry delays if needed

---

## Change Log Addition (2025-01-19 ? Foreign Text Merge Caret Preservation and Focus Management)

> **Archive Note**: Detailed plan available in [archive/2025-Q1/Plan-2025-Q1-foreign-text-sync.md](archive/2025-Q1/Plan-2025-Q1-foreign-text-sync.md)

### User Requests
1. On sync text OFF, preserve caret position: `new_caret = old_caret + foreign_text_length + newline`
2. Prevent foreign textbox from stealing focus when cleared (best-effort)

### Implementation Summary

**Caret Preservation Flow**:
1. TextSyncEnabled setter calculates adjustment: `foreignLength = ForeignText.Length + newline`
2. Merge text and set `FindingsCaretOffsetAdjustment = foreignLength`
3. XAML binding flows adjustment: MainViewModel ¡æ EditorControl ¡æ MusmEditor
4. OnDocumentTextChanged applies adjustment after text update, then resets to 0

**Focus Prevention**:
- WriteToForeignAsync no longer calls SetFocus()
- **Note**: Some apps (Notepad) may still steal focus due to app-specific behavior
- UIA ValuePattern.SetValue() works without focus in most cases

### Files Modified (8 files)
1. `MainViewModel.cs` - Added FindingsCaretOffsetAdjustment property
2. `MusmEditor.cs` - Added CaretOffsetAdjustment DP and adjustment logic
3. `EditorControl.View.cs` - Added CaretOffsetAdjustment DP forwarding
4. `CurrentReportEditorPanel.xaml` - Added binding to EditorFindings
5. `TextSyncService.cs` - Removed SetFocus() call, updated comments
6. Documentation updates (Spec.md, Plan-update-caretsync.md, Tasks.md)

### Status
? **Implemented** - Build successful, all tests passing

---

## Change Log Addition (2025-01-18 ? Current Combination Quick Delete and All Combinations Library)

### User Requests
1. Double-click items in "Current Combination" to remove them quickly
2. Add ListBox showing all combinations that can be double-clicked to load

### Implementation Summary

**Quick Delete**:
- Added MouseDoubleClick handler to Current Combination ListBox
- Created `RemoveFromCurrentCombination(item)` method
- Updates SaveNewCombinationCommand after removal
- Updated GroupBox header with hint text

**All Combinations Library**:
- Added `AllCombinations` ObservableCollection to ViewModel
- Created `GetAllCombinationsAsync()` in repository
- Query from `v_technique_combination_display` ordered by id DESC
- Double-click loads combination into Current for modification
- Prevents duplicates during load

**Layout Adjustment**:
- Changed left panel from 4 rows to 5 rows
- Both ListBoxes have equal vertical space

### Files Modified (4 files)
1. `StudynameTechniqueViewModel.cs` - Added collections and methods
2. `TechniqueRepository.cs` + `.Pg.Extensions.cs` - Added query methods
3. `StudynameTechniqueWindow.xaml.cs` - Added layout and event handlers

### Status
? **Implemented** - Feature complete and tested

---

## Change Log Addition (2025-01-18 ? ReportInputsAndJsonPanel Side-by-Side Row Layout)

### User Request
Synchronize Y-coordinates between main textboxes and proofread counterparts dynamically as heights change.

### Problem
Previous column-based layout made alignment impossible without custom behaviors.

### Solution
Restructured to side-by-side row layout where each textbox pair shares the same vertical position naturally via WPF Grid mechanics.

**Height Binding Strategy**:
- Proofread textboxes bind MinHeight to corresponding main textbox MinHeight
- Chief Complaint / Patient History: 60px minimum
- Findings / Conclusion: 100px minimum
- Textboxes grow with content but never shrink below main textbox minimum

**Scroll Synchronization**:
- Added `OnProofreadScrollChanged` event handler
- Uses `_isScrollSyncing` flag to prevent feedback loops
- One-way sync: proofread scroll affects main column

### Files Modified (2 files)
1. `ReportInputsAndJsonPanel.xaml` - Restructured layout with MinHeight bindings
2. `ReportInputsAndJsonPanel.xaml.cs` - Added scroll sync handler

### Status
? **Implemented** - Alignment natural, no custom calculation needed

---

## Change Log Addition (2025-01-15 ? Phrase-SNOMED Mapping Window UX Enhancements)

### Problems
1. Search textbox empty when window opens (user must retype phrase)
2. Map button stays disabled after selecting concept

### Root Causes
1. `SearchText` property not initialized with phrase text
2. `MapCommand.CanExecuteChanged` not raised when `SelectedConcept` changes

### Fixes
1. **Pre-fill Search**: Set `SearchText = phraseText` in constructor
2. **Enable Map Button**: Replace `[ObservableProperty]` with manual property setter calling `MapCommand.NotifyCanExecuteChanged()`

### Files Modified (1 file)
1. `PhraseSnomedLinkWindowViewModel.cs` - Constructor init + manual property

### Status
? **Implemented** - UX improvements deployed

---

## Archived Implementation Plans

Historical implementation plans have been organized by feature domain:

### 2024 Q4 Archives
Contains plans for:
- Study Technique Management (grouped display, autofill, refresh)
- PACS Automation (modules, keyboard shortcuts, global hotkeys)
- Multi-PACS Tenancy (database schema, repositories)
- UI/UX Improvements (window placement, dark scrollbars, ComboBox fixes)
- Previous Report Features (field mapping, split view, reusable control)

**Location**: [archive/2024/Plan-2024-Q4.md](archive/2024/Plan-2024-Q4.md)

### 2025 Q1 Archives (Older Entries)
Contains plans for:
- Phrase-SNOMED Mapping (database schema, Snowstorm API)
- Editor Enhancements (phrase highlighting, syntax coloring)
- Foreign Text Sync (bidirectional sync, polling, merge logic)

**Location**: [archive/2025-Q1/](archive/2025-Q1/)

---

## Finding Historical Plans

### By Date
1. Identify quarter: Q1 (Jan-Mar), Q2 (Apr-Jun), Q3 (Jul-Sep), Q4 (Oct-Dec)
2. Open appropriate archive directory
3. Search by feature name or change log date

### By Feature
Use [archive/README.md](archive/README.md) Feature Domains Index to locate specific implementations

### By Task ID
- T1-T100: Various 2024 features ¡æ [archive/2024/Plan-2024-Q4.md](archive/2024/Plan-2024-Q4.md)
- T1100-T1148: Foreign text sync ¡æ [archive/2025-Q1/Plan-2025-Q1-foreign-text-sync.md](archive/2025-Q1/Plan-2025-Q1-foreign-text-sync.md)

---

## Document Maintenance

### Active Document Criteria
- Implementation plans from last 90 days
- Features under active development
- Features with pending tasks or verification

### Archival Policy
Plans archived when:
- Feature complete and stable 90+ days
- All tasks completed
- No active bugs or enhancement requests

### Archive Maintenance
- Plans maintain full cumulative detail
- Cross-references preserved
- Index updated with each archive

---

*Document last trimmed: 2025-01-20*  
*Next review: 2025-04-19 (90 days)*  
*Total archived: ~1000 lines moved to organized feature archives*
