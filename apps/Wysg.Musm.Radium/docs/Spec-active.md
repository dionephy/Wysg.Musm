# Feature Specification: Radium - Active Features (Last 90 Days)

**Purpose**: This document contains active feature specifications from the last 90 days.  
**Archive Location**: [docs/archive/](archive/) contains historical specifications organized by quarter and feature domain.  
**Last Updated**: 2025-01-20

[?? View Archive Index](archive/README.md) | [??? View All Archives](archive/)

---

## Quick Navigation

### Recent Features (2025-01-01 onward)
- [FR-1143: Global Phrase Word Limit in Completion](#fr-1143-global-phrase-word-limit-in-completion-2025-01-20) - **NEW**
- [FR-1141: SetClipboard Variable Support](#fr-1141-setclipboard-variable-support-2025-01-20)
- [FR-1142: TrimString Operation](#fr-1142-trimstring-operation-2025-01-20)
- [FR-1140: SetFocus Operation Retry Logic](#setfocus-operation-retry-logic-2025-01-20)
- [FR-1100..FR-1136: Foreign Text Sync & Caret Management](#foreign-text-sync--caret-management-2025-01-19)
- [FR-1025..FR-1027: Current Combination Quick Delete](#current-combination-quick-delete-2025-01-18)
- [FR-1081..FR-1093: Report Inputs Panel Layout](#report-inputs-panel-layout-2025-01-18)
- [FR-950..FR-965: Phrase-SNOMED Mapping Window](#phrase-snomed-mapping-window-2025-01-15)

### Archived Features (2024 and earlier)
- **2024-Q4**: PACS Automation, Multi-PACS Tenancy, Study Techniques ¡æ [archive/2024/Spec-2024-Q4.md](archive/2024/Spec-2024-Q4.md)
- **2025-Q1**: Phrase Highlighting, Editor Enhancements ¡æ [archive/2025-Q1/](archive/2025-Q1/)

---

## Global Phrase Word Limit in Completion (2025-01-20)

### FR-1143: Limit Global Phrases in Completion Window to 3 Words or Less

**Problem**: The completion window for editorFindings shows all global phrases, including very long multi-word medical terms. This clutters the completion list and makes it harder to find relevant short phrases.

**Requirement**: Filter global phrases (account_id IS NULL) shown in the completion window to only those with 3 words or less. This filter should NOT apply to:
- Account-specific (local) phrases
- Hotkeys
- Snippets

**Rationale**:
- Global phrases are shared across all accounts and often contain long medical terminology
- Most commonly used phrases for quick completion are short (1-3 words)
- Longer phrases are better accessed through other mechanisms (search, browser)
- Keeps completion window focused on frequently-used short terms

**Implementation**:

1. **Word Counting Logic**
   - Words counted by splitting on whitespace (space, tab, newline, carriage return)
   - Empty strings count as 0 words
   - Implemented as `CountWords()` helper method in `PhraseService`

2. **Filtering Location**
   - Applied in `GetGlobalPhrasesByPrefixAsync()` method
   - Filter: `CountWords(r.Text) <= 3`
   - Combined with existing prefix match and active status filters

3. **Combined List Behavior**
   - `GetCombinedPhrasesByPrefixAsync()` uses filtered global phrases
   - Account-specific phrases retrieved via `GetPhrasesByPrefixAccountAsync()` (no word limit)
   - Final list contains: filtered globals + unfiltered local phrases

4. **Scope of Filtering**
   - ? Applies to: Global phrases in completion window
   - ? Does NOT apply to: Account phrases, hotkeys, snippets
   - ? Does NOT apply to: Global phrase management UI, SNOMED browser

**Examples**:

**Filtered Out (> 3 words)**:
- "chronic obstructive pulmonary disease" (4 words)
- "diffuse large B-cell lymphoma" (4 words)
- "acute myocardial infarction with ST elevation" (6 words)

**Kept (<= 3 words)**:
- "normal" (1 word)
- "no acute abnormality" (3 words)
- "liver parenchyma" (2 words)

**Use Cases**:

**Quick Completion**:
```
User types "no" ?? completion shows:
- "no" (global, 1 word)
- "no acute abnormality" (global, 3 words)
- "no focal lesion" (global, 3 words)
- "no significant change from previous study" (account, 6 words - allowed because it's local)
```

**Long Phrases Still Accessible**:
```
User can still access "chronic obstructive pulmonary disease" via:
- SNOMED browser
- Global phrases management window
- Typing the full phrase manually
- Copy from previous report
```

**Status**: ? **Implemented** (2025-01-20)

**Cross-References**:
- Implementation: `apps\Wysg.Musm.Radium\Services\PhraseService.cs`
- Related Features: [SNOMED Browser](SNOMED_BROWSER_FEATURE_SUMMARY.md), Global Phrases Management

**Testing**:
- Verify completion window shows only short global phrases
- Verify local (account-specific) phrases show regardless of length
- Verify hotkeys and snippets unaffected
- Verify global phrase management windows show all phrases (no filter)

**Future Enhancements**:
- Make word limit configurable (currently hardcoded to 3)
- Add user preference for word limit
- Add character limit as alternative filter option
- Consider frequency-based filtering instead of word count

---

## SetClipboard Variable Support (2025-01-20)

### FR-1141: SetClipboard Operation Variable Support

**Problem**: SetClipboard operation forced Arg1 type to "String" on operation selection, preventing users from passing variable references from previous operations to clipboard.

**Requirement**: SetClipboard operation must support both String (literal text) and Var (variable reference) argument types.

**Behavior**:

1. **Arg1 Type Selection**
   - User can select either String or Var type for Arg1
   - Type selection persists when operation is selected
   - `OnProcOpChanged` handler only enables/disables arguments, does not force type

2. **String Type (Literal)**
   - User enters literal text in Arg1 Value field
   - Text is copied directly to Windows clipboard
   - Preview shows: "(clipboard set, N chars)" where N is character count

3. **Var Type (Variable Reference)**
   - User selects variable name (e.g., var1, var2) from Arg1 Value dropdown
   - Variable value from previous operation is resolved and copied to clipboard
   - Preview shows: "(clipboard set, N chars)" based on resolved variable content

4. **Error Handling**
   - Null text: "(null)"
   - Clipboard access error: "(error: {message})"

**Use Cases**:
- Copy text extracted from UI element: `GetText(element) ¡æ var1; SetClipboard(var1)`
- Copy field value from selected row: `GetValueFromSelection(list, "ID") ¡æ var1; SetClipboard(var1)`
- Copy static text: `SetClipboard("Fixed text to copy")`

**Status**: ? **Implemented** (2025-01-20)

**Cross-References**:
- Implementation: [Plan-active.md - SetClipboard Fix](Plan-active.md#change-log-addition-2025-01-20--setclipboard--trimstring-operation-fixes)
- Related Operations: SimulatePaste (uses clipboard content), GetText (common source for clipboard)

---

## TrimString Operation (2025-01-20)

### FR-1142: TrimString Operation for String Cleaning

**Problem**: No operation existed to remove specific substrings from the start or end of strings. Users needed to trim labels, prefixes, or unwanted text patterns from UI-extracted content.

**Requirement**: Add TrimString operation that removes all occurrences of a specified substring from the start and/or end of a source string.

**Operation Signature**:
- **Operation Name**: TrimString
- **Arg1**: Source string (Type: String or Var)
- **Arg2**: String to trim away from start/end (Type: String or Var)
- **Arg3**: Disabled
- **Output**: Cleaned string with Arg2 removed from start and end of Arg1

**Behavior**:

1. **Trim from Start and End Only**
   - Arg1 (source): " I am me "
   - Arg2 (trim): "I"
   - Result: " am me " (only removed "I" from start)
   - Uses repeated `StartsWith()` and `EndsWith()` checks

2. **Remove from Both Ends**
   - Arg1 (source): "aba"
   - Arg2 (trim): "a"
   - Result: "b" (removed "a" from both start and end)

3. **Empty Trim String**
   - If Arg2 is null or empty, returns Arg1 unchanged
   - Preview: Shows source string as-is

4. **Variable References**
   - Both Arg1 and Arg2 can reference variables from previous operations
   - Example: `GetText(label) ¡æ var1; TrimString(var1, "Label: ") ¡æ var2`

5. **Multiple Occurrences at Start/End**
   - Removes ALL consecutive occurrences from start and end
   - Example: "!!!text!!!" trimmed by "!" becomes "text"
   - Example: "aatext" trimmed by "a" becomes "text"

6. **Middle Occurrences Preserved**
   - Does NOT remove occurrences in the middle of the string
   - Example: "a test a" trimmed by "a" becomes " test " (middle "a" in "test" preserved)

7. **Case Sensitivity**
   - Trim operation is case-sensitive (uses `StringComparison.Ordinal`)
   - "ABC" will not trim "abc"

**Preview Format**:
- Success: Shows cleaned string
- Empty trim: Shows source string unchanged
- Example: " am me " (after trimming "I" from " I am me ")

**Use Cases**:

**Remove Prefix**:
```
GetValueFromSelection(ResultsList, "ID") ¡æ var1  // "ID: 12345"
TrimString(var1, "ID: ") ¡æ var2  // "12345"
```

**Clean Label Text**:
```
GetText(NameLabel) ¡æ var1  // "Patient Name: John Doe"
TrimString(var1, "Patient Name: ") ¡æ var2  // "John Doe"
```

**Remove Surrounding Characters**:
```
GetText(PriceField) ¡æ var1  // "***$99.99***"
TrimString(var1, "*") ¡æ var2  // "$99.99"
```

**Chain Multiple Trims**:
```
GetText(field) ¡æ var1  // "[PREFIX] Content [SUFFIX]"
TrimString(var1, "[PREFIX] ") ¡æ var2  // "Content [SUFFIX]"
TrimString(var2, " [SUFFIX]") ¡æ var3  // "Content"
```

**Limitations**:
- Only trims from start and end (use Replace for middle occurrences)
- Case-sensitive matching only
- No regex pattern matching (use Replace operation for regex)
- No character set trimming (e.g., cannot trim all whitespace characters - use Trim operation for that)

**Comparison with Similar Operations**:
- **Trim**: Removes whitespace from start/end only
- **TrimString**: Removes specific substring from start/end only (NEW)
- **Replace**: Removes/replaces all occurrences throughout the entire string

**Status**: ? **Implemented** (2025-01-20)

**Cross-References**:
- Implementation: [Plan-active.md - TrimString Implementation](Plan-active.md#change-log-addition-2025-01-20--setclipboard--trimstring-operation-fixes)
- Related Operations: 
  - `Trim` (trims whitespace from start/end)
  - `Replace` (more flexible pattern replacement with regex support)
  - `Split` (splits string into parts)

**Future Enhancements**:
- Add `TrimStart` / `TrimEnd` for prefix/suffix-only trimming
- Add `TrimCharacters` for character set removal
- Add case-insensitive trim option
- Add regex pattern trimming

---

## SetFocus Operation Retry Logic (2025-01-20)

### FR-1140: Add Retry Logic to SetFocus Operation in Custom Procedures

**Problem**: SetFocus operation in Custom Procedures works during test run (manual step-by-step execution) but fails during automated procedure module execution.

**Root Cause**: UI automation elements may not be immediately ready for focus operations, especially during automated procedure execution where operations happen in rapid succession.

**Solution**: Implement retry logic with configurable attempts and delays for SetFocus operation.

**Requirements**:

1. **Retry Configuration**
   - Maximum 3 attempts per SetFocus operation
   - 150ms delay between retry attempts
   - Applies to both test mode and procedure module execution

2. **Success Feedback**
   - First attempt success: Display "(focused)"
   - Retry success: Display "(focused after N attempts)" where N is attempt count
   - Helps users identify elements that need more time

3. **Error Feedback**
   - After all attempts fail: Display "(error after 3 attempts: [error message])"
   - Includes exception message for troubleshooting
   - Clear indication that retries were exhausted

4. **Consistent Implementation**
   - Apply same retry logic in SpyWindow (test mode)
   - Apply same retry logic in ProcedureExecutor (procedure module)
   - Ensure consistent behavior across both execution contexts

5. **Performance Considerations**
   - Maximum 300ms total delay on complete failure (2 retries ¡¿ 150ms)
   - No delay on first attempt success
   - Minimal impact on procedure execution time

**Behavior Examples**:

```
Scenario 1: First attempt succeeds
SetFocus on SearchResultsList ¡æ "(focused)"

Scenario 2: Second attempt succeeds
SetFocus on SearchResultsList ¡æ wait 150ms ¡æ "(focused after 2 attempts)"

Scenario 3: All attempts fail
SetFocus on SearchResultsList ¡æ wait 150ms ¡æ wait 150ms ¡æ "(error after 3 attempts: Element not found)"
```

**Status**: ? **Implemented** (2025-01-20)

**Cross-References**:
- Implementation: [Plan-active.md - SetFocus Retry Logic](Plan-active.md#change-log-addition-2025-01-20--setfocus-operation-retry-logic)
- Related Operations: Other timing-sensitive operations may benefit from similar retry logic (future enhancement)

---

## Foreign Text Sync & Caret Management (2025-01-19)

> **Archive Note**: Detailed specifications for FR-1100..FR-1136 have been archived.  
> **Full Details**: [archive/2025-Q1/Spec-2025-Q1-foreign-text-sync.md](archive/2025-Q1/Spec-2025-Q1-foreign-text-sync.md)

### Summary
Bidirectional text synchronization between Radium Findings editor and external textbox applications (e.g., Notepad) with automatic merge-on-disable and caret position preservation.

### Key Requirements (Recent Updates)
- **FR-1132**: Caret position preservation during merge (new_caret = old_caret + merged_length)
- **FR-1133**: Best-effort focus prevention when clearing foreign textbox
- **FR-1134**: FindingsCaretOffsetAdjustment property for merge coordination
- **FR-1135**: CaretOffsetAdjustment DP in MusmEditor for caret adjustment
- **FR-1136**: CaretOffsetAdjustment DP in EditorControl for binding flow

### Status
? **Implemented and Active** (2025-01-19)

### Cross-References
- Implementation: [Plan-2025-Q1-foreign-text-sync.md](archive/2025-Q1/Plan-2025-Q1-foreign-text-sync.md)
- Tasks: T1100-T1148 (all complete)

---

## Current Combination Quick Delete (2025-01-18)

### FR-1025: Double-Click to Remove from Current Combination
Enable users to quickly remove techniques from "Current Combination" ListBox by double-clicking items.

**Behavior**:
- Double-click on any item in Current Combination ¡æ removes immediately
- No confirmation dialog
- Updates SaveNewCombinationCommand enabled state
- GroupBox header includes hint: "(double-click to remove)"

---

### FR-1026: All Combinations Library ListBox
Display all technique combinations (regardless of studyname) in a new ListBox for reuse/modification.

**Features**:
- Loads all combinations from `v_technique_combination_display`
- Ordered by ID descending (newest first)
- Shows formatted display text (e.g., "axial T1, T2; coronal T1")
- Double-click to load into Current Combination

---

### FR-1027: Double-Click to Load Combination
Enable loading existing combinations into Current Combination for modification.

**Behavior**:
- Double-click on item in All Combinations ¡æ loads techniques into Current Combination
- Fetches combination items via repository
- Matches prefix/tech/suffix strings to get IDs
- Prevents duplicates during load
- Appends to end with sequential sequence_order
- Notifies SaveNewCombinationCommand after load

**Duplicate Prevention**: Skips techniques already present in Current Combination by exact (prefix_id, tech_id, suffix_id) match.

---

## Report Inputs Panel Layout (2025-01-18)

### FR-1081..FR-1086: Side-by-Side Row Layout with MinHeight Bindings
Restructured ReportInputsAndJsonPanel to use side-by-side row layout where each main textbox has a corresponding proofread textbox in the same vertical position.

**Key Changes**:
- Chief Complaint (PR) binds MinHeight to txtChiefComplaint.MinHeight (60px)
- Patient History (PR) binds MinHeight to txtPatientHistory.MinHeight (60px)
- Findings (PR) binds MinHeight to txtFindings.MinHeight (100px)
- Conclusion (PR) binds MinHeight to txtConclusion.MinHeight (100px)

**Benefits**:
- Natural Y-coordinate alignment via WPF Grid row mechanics
- No custom layout calculation required
- Dynamic height adjustments preserve alignment
- Simplified XAML with fewer converters

---

### FR-1090: Scroll Synchronization Between Main and Proofread
Added scroll synchronization to keep corresponding textboxes visible together when scrolling the proofread column.

**Implementation**:
- `OnProofreadScrollChanged` event handler
- `_isScrollSyncing` flag to prevent feedback loops
- One-way sync: proofread scroll affects main column
- Main column scrolls independently (no reverse sync)

---

### FR-1091: Reverse Layout Support
Maintained reverse layout feature for column swapping.

**Behavior**: When Reverse=true, JSON and Main/Proofread columns swap positions while maintaining alignment.

---

### FR-1093: No Custom Y-Coordinate Calculation
Eliminated need for custom behaviors or value converters to calculate Y-coordinate alignment.

**Rationale**: WPF Grid naturally aligns elements in the same row, making custom calculation unnecessary.

---

## Phrase-SNOMED Mapping Window (2025-01-15)

### FR-950: Pre-filled Search Text in Link Window
When opening the phrase-SNOMED link window, pre-populate the search textbox with the phrase text.

**Behavior**:
- `SearchText` property initialized from `phraseText` constructor parameter
- User can immediately press Enter to search without retyping
- Search box remains editable for custom searches

**Fix**: Resolves UX issue where users had to retype phrase text to search.

---

### FR-951: Map Button Enabled State Fix
Fixed Map button remaining disabled after concept selection by calling `NotifyCanExecuteChanged()` when `SelectedConcept` changes.

**Implementation**:
- Replaced `[ObservableProperty]` with manual property setter
- Calls `MapCommand.NotifyCanExecuteChanged()` on value change
- Ensures WPF re-evaluates button enabled state immediately

**Fix**: Resolves UX issue where button stayed disabled despite valid selection.

---

## Archived Specifications

The following specifications have been moved to archives for better document organization:

### 2024 Q4 Archives
- **FR-500..FR-547**: Study Technique Management, PACS Automation
- **FR-600..FR-681**: Multi-PACS Tenancy, Window Placement, Reportify
- **FR-700..FR-709**: Editor Phrase Highlighting
- **Location**: [archive/2024/Spec-2024-Q4.md](archive/2024/Spec-2024-Q4.md)

### 2025 Q1 Archives (Older Entries)
- **FR-900..FR-915**: Phrase-SNOMED Mapping Database Schema
- **FR-1000..FR-1024**: Technique Combination Management
- **Location**: [archive/2025-Q1/](archive/2025-Q1/)

---

## Document Maintenance

### Active Document Criteria
This document contains only:
- Features specified in the last 90 days (since 2025-01-01)
- Features currently under active development
- Features with pending implementation tasks

### Archival Policy
Features are moved to archives when:
- Implementation complete and stable for 90+ days
- No active tasks or pending work
- Documented in at least one release

### Finding Historical Requirements
1. Check [archive/README.md](archive/README.md) for index of all archives
2. Use Feature Domains Index to locate specific feature areas
3. Search archives by requirement ID (FR-XXX) or date range

---

*Document last trimmed: 2025-01-20*  
*Next review: 2025-04-20 (90 days)*
