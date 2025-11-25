# FIX: Global Phrase Highlighting Filter Issue

**Date**: 2025-01-29  
**Type**: Bug Fix  
**Severity**: Medium  
**Component**: Phrase System, Syntax Highlighting  

## Problem Description

Global phrases with more than 3 words were not appearing in:
1. **Completion window** - Expected behavior (by design)
2. **Semantic highlighting** - **BUG** (should show all phrases)

### Example
The phrase "vein of calf" (3 words) exists in the global phrase database but:
- ? Does NOT appear in completion window (correct - 3-word filter for completion)
- ? Does NOT get syntax highlighting (bug - should highlight all phrases)

### Root Cause

In `MainViewModel.Phrases.cs`, the `LoadPhrasesAsync` method was using `GetCombinedPhrasesAsync` for populating the syntax highlighting snapshot. This method filters global phrases to 3 words or less for completion window optimization.

```csharp
// BEFORE (incorrect):
var list = await _phrases.GetCombinedPhrasesAsync(accountId).ConfigureAwait(false);
CurrentPhraseSnapshot = list ?? Array.Empty<string>();
```

The `GetCombinedPhrasesAsync` method in `PhraseService.cs`:
- Calls `GetGlobalPhrasesAsync()` which filters to 3 words or less
- This filtered list was being used for BOTH completion AND syntax highlighting
- Result: Long phrases like "vein of calf" were excluded from highlighting

## Solution

Changed `LoadPhrasesAsync` to use the dedicated unfiltered method `GetAllPhrasesForHighlightingAsync`:

```csharp
// AFTER (correct):
var list = await _phrases.GetAllPhrasesForHighlightingAsync(accountId).ConfigureAwait(false);
CurrentPhraseSnapshot = list ?? Array.Empty<string>();
```

### Key Points

1. **Completion Window**: Uses `GetCombinedPhrasesByPrefixAsync` which has built-in 3-word filter
   - Optimizes performance by limiting dropdown size
   - Focuses on short, frequently-used phrases

2. **Syntax Highlighting**: Uses `GetAllPhrasesForHighlightingAsync` which returns ALL phrases
   - No word count filter
   - Shows semantic colors for all mapped phrases regardless of length
   - Example: "anterior descending branch of left coronary artery" (8 words) now highlights correctly

## Implementation Details

### Modified Files

1. **apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Phrases.cs**
   - Changed `LoadPhrasesAsync` to use `GetAllPhrasesForHighlightingAsync`
   - Added debug logging to track loaded phrase counts

### PhraseService Methods

| Method | Purpose | Filter | Usage |
|--------|---------|--------|-------|
| `GetGlobalPhrasesAsync()` | Get global phrases for display | ��3 words | List views |
| `GetCombinedPhrasesAsync()` | Combined list for caching | Global ��3 words, Account all | Initial cache |
| `GetCombinedPhrasesByPrefixAsync()` | Completion window | Global ��3 words, Account all | Dropdown |
| `GetAllPhrasesForHighlightingAsync()` | Syntax highlighting | **None** | Editor rendering |

## Testing

### Verification Steps

1. Open Radium application
2. Ensure database has global phrases with >3 words (e.g., "vein of calf", "anterior descending branch")
3. Type one of these phrases in the editor
4. **Expected Results**:
   - ? Phrase highlights with appropriate color (SNOMED semantic tag or default gray)
   - ? Phrase does NOT appear in completion dropdown (by design)

### Test Cases

#### Test 1: Short Phrase (��3 words)
- Phrase: "heart" (1 word)
- Completion: ? Shows in dropdown
- Highlighting: ? Shows color

#### Test 2: Medium Phrase (3 words)
- Phrase: "vein of calf" (3 words)  
- Completion: ? Shows in dropdown
- Highlighting: ? Shows color

#### Test 3: Long Phrase (>3 words)
- Phrase: "anterior descending branch of left coronary artery" (8 words)
- Completion: ? Does NOT show in dropdown (by design)
- Highlighting: ? Shows color (FIXED)

## Performance Impact

- **Minimal**: Syntax highlighting already processes visible text only
- `GetAllPhrasesForHighlightingAsync` loads all phrases once at startup
- In-memory HashSet lookup remains O(1)
- No impact on completion window performance (still uses filtered list)

## Related Code

### PhraseService.cs Implementation

```csharp
public async Task<IReadOnlyList<string>> GetAllPhrasesForHighlightingAsync(long accountId)
{
    // Get ALL global phrases from _states (unfiltered)
    var globalState = _states.GetOrAdd(GLOBAL_KEY, _ => new AccountPhraseState(null));
    if (globalState.ById.Count == 0 && !globalState.Loading)
    {
        try { await LoadGlobalPhrasesAsync(globalState).ConfigureAwait(false); } 
        catch { return Array.Empty<string>(); }
    }
    
    // Get ALL account phrases
    var accountState = _states.GetOrAdd(accountId, id => new AccountPhraseState(id));
    if (accountState.ById.Count == 0 && !accountState.Loading)
    {
        try { await LoadSmallSetAsync(accountState).ConfigureAwait(false); } 
        catch { return Array.Empty<string>(); }
    }
    
    // Combine WITHOUT filtering - syntax highlighting needs all phrases
    var globalPhrases = globalState.ById.Values.Where(r => r.Active).Select(r => r.Text);
    var accountPhrases = accountState.ById.Values.Where(r => r.Active).Select(r => r.Text);
    
    var combined = new HashSet<string>(accountPhrases, StringComparer.OrdinalIgnoreCase);
    foreach (var global in globalPhrases)
        combined.Add(global);
    
    return combined.OrderBy(t => t).ToList();
}
```

## Future Considerations

### Completion Window Filter Adjustments

If users request longer phrases in completion:
1. Increase `CountWords` threshold from 3 to 4 or 5
2. Or add user preference setting for word limit
3. Located in `PhraseService.GetGlobalPhrasesAsync()` and `GetGlobalPhrasesByPrefixAsync()`

### Semantic Highlighting Performance

With 1000+ global phrases:
- Current implementation is efficient (visible lines only)
- If performance issues arise, consider:
  - Phrase length filtering (e.g., max 10 words)
  - Phrase frequency scoring (highlight common phrases only)
  - Lazy loading based on viewport

## Documentation Updates

Updated references:
- ? This document (new)
- ?? TODO: Update `phrase-highlighting-usage.md` to clarify filtering behavior
- ?? TODO: Update developer onboarding docs with phrase filtering architecture

## Changelog

```
[2025-01-29] FIX: Global phrases >3 words now highlight correctly in editor
  - Changed MainViewModel.Phrases.cs to use GetAllPhrasesForHighlightingAsync
  - Completion window still filters to 3 words (no change)
  - Syntax highlighting now shows ALL phrases regardless of word count
  - Example: "vein of calf" now highlights with body structure color
```

## Related Issues

- FR-completion-filter-2025-10-20: Original completion filter implementation
- FR-274, T385: Combined phrases (global + account) support
- FR-709: SNOMED CT color coding for phrases

## Author Notes

This was a subtle bug caused by reusing a filtered dataset for two different purposes:
1. Completion window (should be filtered for UX)
2. Syntax highlighting (should be unfiltered for completeness)

The fix properly separates these concerns by using dedicated methods for each use case.
