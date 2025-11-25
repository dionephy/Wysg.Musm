# Feature: SNOMED Browser Auto-Collapse for Structural and Saved Terms

**Date**: 2025-01-27  
**Component**: SNOMED CT Browser Window  
**Type**: Feature Enhancement  
**Status**: Completed

---

## Summary

Added automatic collapse functionality to the SNOMED CT Browser that hides term lists for concepts that meet specific criteria:
1. All synonyms contain structural terms (yellow background - "structure", "entire", or "(")
2. At least one term is already saved as a phrase (red background concept)

This improves usability by reducing visual clutter and allowing users to focus on concepts that haven't been added yet or contain useful clinical terms.

---

## Problem

The SNOMED CT Browser displayed all concepts in an expanded state, which caused:
- Visual clutter when browsing concepts with only structural/anatomical terms
- Difficulty identifying concepts that haven't been mapped yet
- Unnecessary scrolling through concepts already saved as phrases
- Slower visual scanning to find relevant unmapped concepts

Users requested the ability to automatically hide:
- Concepts where all synonyms are yellow (structural terms like "Structure of X", "Entire X")
- Concepts with red background (indicating at least one term is already saved)

---

## Solution

### Changes Made

#### 1. **New ViewModel Property: `IsExpanded`**
Added to `SnomedConceptViewModel`:
```csharp
public bool IsExpanded { get; set; } = true;
```

- Controls visibility of the terms list for each concept
- Initially `true` (expanded) but determined during initialization
- Can be manually toggled by users via expand/collapse button

#### 2. **Auto-Collapse Logic**

##### DetermineInitialExpansionState()
Automatically collapses concepts meeting these criteria:

**Criteria 1: All Synonyms Are Structural**
- Checks all synonym terms (not FSN or PT)
- Identifies structural terms containing:
  - "structure" (case-insensitive)
  - "entire" (case-insensitive)
  - "(" character
- If ALL synonyms match, concept is collapsed
- Example: "Structure of kidney (body structure)" ¡æ Collapsed

**Criteria 2: Has Existing Phrases (Red Background)**
- Monitored through `HasExistingPhrases` property
- Updated when term existence checks complete
- If any term is marked as already added ¡æ concept collapses
- This happens asynchronously after page load

#### 3. **UI Components**

**Expand/Collapse Toggle Button**
- Added to concept header (left of concept ID)
- Shows "¢º" when collapsed, "¡å" when expanded
- Clickable to manually toggle expansion state
- Tooltip: "Click to expand/collapse terms"

**Converters Created**:
1. `ExpanderArrowConverter`: Converts bool ¡æ arrow symbol (¢º/¡å)
2. `ExpandedToVisibilityConverter`: Converts bool ¡æ Visibility (Collapsed/Visible)

#### 4. **Toggle Command**
```csharp
ToggleExpandCommand = new RelayCommand<SnomedConceptViewModel>(
    conceptVm => 
    {
        if (conceptVm != null)
        {
            conceptVm.IsExpanded = !conceptVm.IsExpanded;
        }
    }
);
```

Allows users to manually expand/collapse any concept regardless of auto-collapse status.

---

## Files Modified

### ViewModel Layer

**`apps\Wysg.Musm.Radium\ViewModels\SnomedBrowserViewModel.cs`**

#### SnomedBrowserViewModel Changes:
- Added `ToggleExpandCommand` property
- Initialized command in constructor

#### SnomedConceptViewModel Changes:
- Added `IsExpanded` property
- Added `DetermineInitialExpansionState()` method
- Added `IsStructuralTerm()` helper method
- Modified `UpdateHasExistingPhrases()` to auto-collapse on existing phrases
- Auto-collapse happens at two points:
  1. During initialization (for structural terms)
  2. After async existence checks complete (for saved phrases)

### Converter Layer

**`apps\Wysg.Musm.Radium\Converters\ExpanderArrowConverter.cs`** (New)
- Converts `IsExpanded` bool to arrow character
- `true` ¡æ "¡å" (expanded/down arrow)
- `false` ¡æ "¢º" (collapsed/right arrow)

**`apps\Wysg.Musm.Radium\Converters\ExpandedToVisibilityConverter.cs`** (New)
- Converts `IsExpanded` bool to WPF Visibility
- `true` ¡æ `Visibility.Visible`
- `false` ¡æ `Visibility.Collapsed`

### View Layer

**`apps\Wysg.Musm.Radium\Views\SnomedBrowserWindow.xaml`**

#### Resources Section:
- Added `ExpanderArrowConverter` resource
- Added `ExpandedToVisibilityConverter` resource

#### Concept Header:
- Added new `Grid.Column="0"` for expand/collapse button
- Button binds to `IsExpanded` via `ExpanderArrowConverter`
- Button command binds to `ToggleExpandCommand` with concept as parameter
- Shifted existing columns (ID, FSN, tag) right by one column

#### Terms List:
- Added `Visibility` binding to `IsExpanded` via `ExpandedToVisibilityConverter`
- Terms are hidden (`Collapsed`) when `IsExpanded=false`
- Terms are shown (`Visible`) when `IsExpanded=true`

---

## User Impact

### Positive Changes

? **Reduced Clutter**: Structural-only concepts are hidden by default  
? **Focus on Unmapped**: Easier to identify concepts that haven't been added  
? **Visual Hierarchy**: Red backgrounds (saved) are collapsed, reducing noise  
? **Manual Control**: Users can expand/collapse any concept as needed  
? **Performance**: No performance impact - uses simple boolean visibility  
? **Intuitive Icons**: Standard ¢º/¡å arrows match common UI patterns  

### Behavior Changes

- **First Load**: Some concepts appear collapsed (yellow or red background)
- **Async Updates**: Concepts may collapse a moment after loading (as existence checks complete)
- **Manual Override**: User can expand any collapsed concept by clicking arrow
- **State Persists**: Expansion state persists until page navigation or domain change

### No Breaking Changes

- All existing functionality preserved
- Concepts without structural terms or saved phrases remain expanded
- User can manually expand any concept
- No data loss or functionality removed

---

## Technical Details

### Collapse Decision Logic

```plaintext
Load Concept ¡æ Check Synonyms
    ¡é
All Synonyms Structural? 
    YES ¡æ Collapse (Yellow)
    NO  ¡æ Keep Expanded ¡æ Async Existence Checks
              ¡é
         Any Term Saved?
              YES ¡æ Collapse (Red)
              NO  ¡æ Keep Expanded
```

### Structural Term Detection

A term is considered "structural" if it contains:
- "structure" (case-insensitive) - e.g., "Structure of kidney"
- "entire" (case-insensitive) - e.g., "Entire kidney"  
- "(" character - e.g., "Kidney (body structure)"

**Why only synonyms?**
- FSN (Fully Specified Name) and PT (Preferred Term) are always shown in header
- Only synonyms are evaluated for structural term detection
- This prevents collapsing concepts with useful PT but structural synonyms

### Red Background Collapse

- Monitored through `HasExistingPhrases` property
- Triggers when ANY term in concept is marked `IsAdded=true`
- Happens asynchronously after page load
- User may see brief "flash" as concept collapses post-load
- This is expected behavior due to async database checks

---

## Testing Recommendations

When testing, verify:

### Auto-Collapse Behavior
1. ? Concepts with only structural synonyms collapse on load
2. ? Concepts with saved phrases collapse after async check
3. ? Concepts with neither remain expanded
4. ? Mixed concepts (some structural, some clinical) remain expanded

### Manual Toggle
5. ? Clicking arrow expands collapsed concept
6. ? Clicking arrow collapses expanded concept
7. ? Arrow icon changes (¢º ¡ê ¡å)
8. ? Terms appear/disappear smoothly

### Edge Cases
9. ? Empty concept (no synonyms) remains expanded
10. ? Concept with only FSN/PT (no synonyms) remains expanded
11. ? After adding a term, concept collapses (red background)
12. ? Domain change resets expansion states

### Visual Verification
13. ? Structural terms have yellow background (existing feature)
14. ? Saved concepts have red background (existing feature)
15. ? Collapsed concepts show header only
16. ? Expanded concepts show full term list

---

## Example Scenarios

### Scenario 1: Structural Concept
```
Concept: [123456] Structure of kidney (body structure)
Synonyms:
  - "Structure of kidney"          [Yellow]
  - "Entire kidney"                [Yellow]
  - "Kidney structure (substance)" [Yellow]

Result: AUTO-COLLAPSED (all synonyms structural)
```

### Scenario 2: Clinical Concept
```
Concept: [654321] Acute kidney injury (disorder)
Synonyms:
  - "Acute renal failure"   [White]
  - "AKI"                   [White]
  - "ARF"                   [White]

Result: EXPANDED (useful clinical terms)
```

### Scenario 3: Mixed Concept
```
Concept: [789012] Kidney disease (disorder)
Synonyms:
  - "Renal disease"              [White]
  - "Kidney disorder"            [White]
  - "Structure of diseased kidney" [Yellow]

Result: EXPANDED (not all synonyms structural)
```

### Scenario 4: Saved Concept
```
Concept: [456789] Chronic kidney disease (disorder)
Terms:
  - FSN: "Chronic kidney disease (disorder)"
  - PT: "Chronic kidney disease"  [Red - Already Added]
  - Synonym: "CKD"                [Red - Already Added]

Result: AUTO-COLLAPSED (has saved phrases)
```

---

## Known Limitations

1. **Async Collapse "Flash"**
   - Concepts may appear expanded briefly before collapsing
   - Occurs when existence checks complete
   - Visual flash is minor and acceptable trade-off for accuracy

2. **State Not Persisted**
   - Manual expansion state lost on page navigation
   - Lost on domain change
   - This is by design for consistency

3. **No "Expand All" / "Collapse All"**
   - Future enhancement consideration
   - Would require additional UI controls
   - Current implementation prioritizes simplicity

4. **Only Checks Synonyms**
   - FSN and PT not evaluated for structural terms
   - This is intentional - these are always shown in header
   - Edge case: PT structural but synonyms clinical ¡æ remains expanded

---

## Future Enhancements

Potential future improvements:

- **Expand/Collapse All Button**: Global toggle for all concepts on page
- **Remember State Per Page**: Persist expansion state during session
- **Visual Indicator**: Show "(X terms hidden)" when collapsed
- **Filter Options**: "Hide all structural" or "Hide all saved" checkboxes
- **Statistics**: Show count of collapsed vs expanded concepts
- **Smart Expand**: Expand first 3-5 concepts automatically for context

---

## Configuration

No configuration required. Feature works out-of-box with existing data.

Optional customization (requires code change):
- Structural term keywords can be modified in `IsStructuralTerm()` method
- Initial `IsExpanded` default can be changed (currently `true`)
- Collapse criteria can be adjusted in `DetermineInitialExpansionState()`

---

## Related Components

- `SnomedConceptViewModel.IsExpanded` - Controls expansion state
- `SynonymStructureHighlightConverter` - Yellow background for structural terms
- `HasExistingPhrases` - Red background for saved concepts
- `IsPhraseExistsAsync()` - Async existence checking
- `CheckExistenceAsync()` - Term-level existence validation

---

## Deployment Notes

- ? No database changes required
- ? No configuration changes required
- ? No breaking changes to existing code
- ? Backward compatible with all existing features
- ? Can be deployed immediately
- ? No user training required (intuitive UI)

---

## Conclusion

This feature significantly improves the SNOMED CT Browser user experience by automatically hiding less relevant concepts (structural-only or already-saved), while preserving full manual control for users who want to expand and review any concept. The implementation is clean, performant, and follows established UI patterns with expand/collapse arrows.

Users can now focus on mapping new clinical terms without being distracted by structural anatomical concepts or concepts they've already processed, while still maintaining the ability to review everything when needed.
