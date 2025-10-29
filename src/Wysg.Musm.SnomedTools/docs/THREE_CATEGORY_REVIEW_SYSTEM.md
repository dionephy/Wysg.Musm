# Three-Category Candidate Review System

## Summary

Enhanced the Cache Review system to separate candidates into three distinct categories based on SNOMED semantic tags:
1. **Organism** - Biological organisms (bacteria, viruses, etc.)
2. **Substance** - Chemical substances, medications, etc.
3. **Other** - All other SNOMED concepts (body structures, findings, procedures, etc.)

Each category has its own display panel with independent accept/reject buttons.

## Implementation

### Category Detection

Categories are determined by extracting the semantic tag from the SNOMED FSN (Fully Specified Name):

```csharp
private CandidateCategory GetCandidateCategory(CachedCandidate candidate)
{
    var semanticTag = ExtractSemanticTag(candidate.ConceptFsn);
    
    if (semanticTag != null)
    {
        if (semanticTag.IndexOf("organism", StringComparison.OrdinalIgnoreCase) >= 0)
            return CandidateCategory.Organism;
        if (semanticTag.IndexOf("substance", StringComparison.OrdinalIgnoreCase) >= 0)
            return CandidateCategory.Substance;
    }
    
    return CandidateCategory.Other;
}

private static string? ExtractSemanticTag(string? fsn)
{
    // Extracts text in parentheses at end of FSN
    // Example: "Escherichia coli (organism)" ⊥ "organism"
    // Example: "Aspirin (substance)" ⊥ "substance"
    // Example: "Heart (body structure)" ⊥ "body structure" ⊥ Other category
}
```

### ViewModel Changes

**Three Separate Current Candidates:**
```csharp
public CachedCandidate? CurrentOrganismCandidate { get; private set; }
public CachedCandidate? CurrentSubstanceCandidate { get; private set; }
public CachedCandidate? CurrentOtherCandidate { get; private set; }
```

**Six Commands (2 per category):**
```csharp
// Organism
public IAsyncRelayCommand AcceptOrganismCommand { get; }
public IAsyncRelayCommand RejectOrganismCommand { get; }

// Substance
public IAsyncRelayCommand AcceptSubstanceCommand { get; }
public IAsyncRelayCommand RejectSubstanceCommand { get; }

// Other
public IAsyncRelayCommand AcceptOtherCommand { get; }
public IAsyncRelayCommand RejectOtherCommand { get; }
```

**Independent Processing:**
Each category's accept/reject operation only affects that category's current candidate:

```csharp
private async Task AcceptCandidateAsync(CachedCandidate? candidate, CandidateCategory category)
{
    if (candidate == null) return;
    
    // Mark as accepted in database
    await _cacheService.MarkAcceptedAsync(candidate.Id);
    AcceptedCount++;
    
    // Remove from pending list
    PendingCandidates.Remove(candidate);
    PendingCount--;
    
    // Load next candidate ONLY for this category
    await LoadNextCandidateForCategoryAsync(category);
}
```

### UI Layout

**Three-Column Layout:**
```
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛  Background Fetching Controls & Stats                       弛
戍式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛  Review Statistics (Pending, Accepted, Rejected, Saved)     弛
戍式式式式式式式式式式式式式成式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛  ORGANISM   弛  SUBSTANCE    弛  OTHER                        弛
弛  (Green)    弛  (Blue)       弛  (Orange)                     弛
弛             弛               弛                               弛
弛  Term:      弛  Term:        弛  Term:                        弛
弛  Bacteria   弛  Aspirin      弛  Heart                        弛
弛             弛               弛                               弛
弛  Concept:   弛  Concept:     弛  Concept:                     弛
弛  [12345]    弛  [67890]      弛  [11111]                      弛
弛  E. coli    弛  Aspirin      弛  Heart (body structure)       弛
弛  (organism) 弛  (substance)  弛                               弛
弛             弛               弛                               弛
弛  [Accept]   弛  [Accept]     弛  [Accept]                     弛
弛  [Reject]   弛  [Reject]     弛  [Reject]                     弛
戌式式式式式式式式式式式式式扛式式式式式式式式式式式式式式式扛式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
弛  [Save All Accepted to Database]                            弛
戍式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛  Status: Background: Page 10, cached 5 new, skipped 3       弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
```

**Visual Styling:**
- **Organism Panel**: Green border (#4CAF50)
- **Substance Panel**: Blue border (#2196F3)
- **Other Panel**: Orange border (#FF9800)
- Each panel has 2px colored border for clear visual separation
- Consistent layout across all three panels

### Auto-Refresh Behavior

When new candidates are cached by background fetcher:

```csharp
private void OnCandidateCached(object? sender, Services.CandidateCachedEventArgs e)
{
    // If any category slot is empty, auto-refresh
    if (CurrentOrganismCandidate == null || 
        CurrentSubstanceCandidate == null || 
        CurrentOtherCandidate == null)
    {
        await LoadNextCandidatesAsync();
    }
}
```

**Smart Loading:**
- Loads one candidate per category from pending list
- Each category maintains its own current candidate
- Accept/Reject only advances that specific category
- Empty categories show appropriate message

### Category Distribution Logic

```csharp
private async Task LoadNextCandidatesAsync()
{
    var pending = await _cacheService.GetPendingCandidatesAsync(100);
    
    // Separate by category
    var organisms = pending.Where(c => GetCandidateCategory(c) == Organism).ToList();
    var substances = pending.Where(c => GetCandidateCategory(c) == Substance).ToList();
    var others = pending.Where(c => GetCandidateCategory(c) == Other).ToList();
    
    // Set current candidates (only if null)
    if (CurrentOrganismCandidate == null)
        CurrentOrganismCandidate = organisms.FirstOrDefault();
    
    if (CurrentSubstanceCandidate == null)
        CurrentSubstanceCandidate = substances.FirstOrDefault();
    
    if (CurrentOtherCandidate == null)
        CurrentOtherCandidate = others.FirstOrDefault();
}
```

## User Workflow

### Scenario 1: Reviewing Mixed Categories

```
1. Start fetching with word count = 1
2. Background fetcher finds:
   - "bacteria" (organism)
   - "aspirin" (substance)
   - "heart" (body structure) ⊥ Other
3. All three panels populate automatically
4. User reviews:
   - Clicks Accept on Organism ⊥ "bacteria" saved, next organism loads
   - Clicks Reject on Substance ⊥ "aspirin" rejected, next substance loads
   - Clicks Accept on Other ⊥ "heart" saved, next other loads
5. Each category advances independently
```

### Scenario 2: Unbalanced Categories

```
1. Background fetcher finds mostly "Other" categories
2. Organism panel: Shows first organism (or empty if none found yet)
3. Substance panel: Shows first substance (or empty if none found yet)
4. Other panel: Shows first other concept (always populated)
5. User can accept/reject available categories
6. As fetcher continues, empty slots auto-populate when matches found
```

### Scenario 3: Empty Categories

```
忙式式式式式式式式式式式式式成式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式忖
弛  ORGANISM   弛  SUBSTANCE    弛  OTHER                弛
弛             弛               弛                       弛
弛  Term:      弛  (No sub-     弛  Term:                弛
弛  Bacteria   弛   stance      弛  Heart                弛
弛             弛   candidates) 弛                       弛
弛  [Accept]   弛               弛  [Accept]             弛
弛  [Reject]   弛  (Buttons     弛  [Reject]             弛
弛             弛   disabled)   弛                       弛
戌式式式式式式式式式式式式式扛式式式式式式式式式式式式式式式扛式式式式式式式式式式式式式式式式式式式式式式戎
```

## Benefits

### For Users:
? **Simultaneous review** of different category types  
? **Faster workflow** - can accept organisms while reviewing substances  
? **Visual categorization** - color-coded panels for quick identification  
? **Independent progress** - each category advances separately  
? **No category blocking** - empty category doesn't block others  

### For Quality Control:
? **Category-specific decisions** - organisms vs substances vs others  
? **Better context** - seeing category helps make informed decisions  
? **Balanced processing** - ensures all categories get reviewed  
? **Semantic awareness** - leverages SNOMED's semantic tag system  

### Technical:
? **Efficient loading** - single query categorizes into three streams  
? **Independent state** - each category maintains own current candidate  
? **Auto-refresh** - fills empty slots automatically  
? **Parallel review** - all three categories can be active simultaneously  

## Examples

### Organism Category Examples:
- "Escherichia coli (organism)"
- "Staphylococcus aureus (organism)"
- "Influenza virus (organism)"
- "Human (organism)"
- "Bacteria (organism)"

### Substance Category Examples:
- "Aspirin (substance)"
- "Penicillin (substance)"
- "Glucose (substance)"
- "Oxygen (substance)"
- "Water (substance)"

### Other Category Examples:
- "Heart (body structure)"
- "Myocardial infarction (disorder)"
- "Appendectomy (procedure)"
- "Blood pressure (observable entity)"
- "Fracture (finding)"

## Future Enhancements

Possible improvements:
1. **Category filtering** - Hide/show specific categories
2. **Category priorities** - Reorder panels based on user preference
3. **Batch operations** - Accept all in category at once
4. **Category statistics** - Show counts per category
5. **Custom categories** - User-defined semantic tag groupings
6. **Category export** - Export by category to separate files

---

**Implementation Date**: 2025-01-21  
**Status**: ? Complete & Working  
**Build Status**: ? Successful  
**Key Feature**: Three independent review streams for efficient categorized processing
