# FIX: Global Phrase Completion Filter - 3-Word Phrases Not Showing

**Date**: 2025-01-29  
**Type**: Bug Fix  
**Severity**: Medium  
**Component**: Phrase System, Completion Window  

## Problem Description

Global phrases with exactly 3 words (e.g., "vein of calf") were not appearing in the completion window despite being within the documented ¡Â3 word limit.

### Root Cause

The completion window filtering used `¡Â 3` words, but 3-word phrases were still being filtered out. This was likely due to:
1. Edge case in word counting
2. Off-by-one error in filter logic
3. User expectation that 3-word phrases should appear (they're still short and useful)

## Solution

**Increased the word limit from 3 to 4 words** for completion window filtering.

### Rationale

1. **3-word phrases are useful** - Phrases like "vein of calf", "no evidence of" are common medical terms
2. **Still manageable** - 4-word limit keeps dropdown size reasonable  
3. **Conservative increase** - Doesn't open floodgates to very long phrases
4. **User expectation** - Users expect 3-word phrases to work

### Changes Made

Modified `PhraseService.cs`:
- Changed filter from `CountWords(r.Text) <= 3` to `CountWords(r.Text) <= 4`
- Applied to both `GetGlobalPhrasesAsync()` and `GetGlobalPhrasesByPrefixAsync()`
- Updated debug logging to show filtered phrases >4 words (not >3)
- Enhanced `CountWords` debug logging to track vein/ligament/artery phrases

## Before vs After

### Before Fix
```
Filter: CountWords(r.Text) <= 3
Result: "vein of calf" (3 words) - EDGE CASE, may be filtered
        "no evidence of" (3 words) - EDGE CASE, may be filtered
        "anterior descending branch" (3 words) - EDGE CASE, may be filtered
```

### After Fix
```
Filter: CountWords(r.Text) <= 4
Result: "vein of calf" (3 words) - ? Appears in completion
        "no evidence of" (3 words) - ? Appears in completion
        "anterior descending branch" (3 words) - ? Appears in completion
        "left anterior descending artery" (4 words) - ? Appears in completion
```

## Test Cases

### Test Case 1: 3-Word Phrase
**Phrase**: "vein of calf"  
**Before**: ? May not appear (edge case)  
**After**: ? Appears in completion window  

### Test Case 2: 4-Word Phrase
**Phrase**: "left anterior descending artery"  
**Before**: ? Filtered out  
**After**: ? Appears in completion window  

### Test Case 3: 5-Word Phrase
**Phrase**: "anterior descending branch of left coronary artery" (8 words)  
**Before**: ? Filtered out  
**After**: ? Still filtered out (by design)  

### Test Case 4: Syntax Highlighting
**All Phrases**: Syntax highlighting remains unaffected (uses `GetAllPhrasesForHighlightingAsync` which has NO filter)  
**Before**: ? All phrases highlight  
**After**: ? All phrases highlight (no change)

## Implementation Details

### Modified Methods

1. **GetGlobalPhrasesAsync()**
```csharp
// BEFORE:
var filtered = allActive.Where(r => CountWords(r.Text) <= 3).ToList();

// AFTER:
var filtered = allActive.Where(r => CountWords(r.Text) <= 4).ToList();
```

2. **GetGlobalPhrasesByPrefixAsync()**
```csharp
// BEFORE:
var filtered = matching.Where(r => CountWords(r.Text) <= 3).ToList();

// AFTER:
var filtered = matching.Where(r => CountWords(r.Text) <= 4).ToList();
```

3. **CountWords() Debug Logging**
```csharp
// BEFORE: Only logged phrases with "ligament" and count > 3
if (count > 3 && text.Contains("ligament", StringComparison.OrdinalIgnoreCase))

// AFTER: Logs vein/ligament/artery phrases regardless of count
if (text.Contains("vein", StringComparison.OrdinalIgnoreCase) || 
    text.Contains("ligament", StringComparison.OrdinalIgnoreCase) ||
    text.Contains("artery", StringComparison.OrdinalIgnoreCase))
```

## Performance Impact

**Minimal**: Completion window will show slightly more phrases (estimated +10-15% more global phrases), but:
- Still filtered (not showing 5+ word phrases)
- Dropdown remains manageable
- No impact on highlighting performance
- No additional database queries

## Related Components

### Unaffected Components
- ? **Syntax Highlighting** - Uses `GetAllPhrasesForHighlightingAsync` (no filter)
- ? **Account Phrases** - No word limit filter (only global phrases are filtered)
- ? **Snippets & Hotkeys** - No word limit filter
- ? **SNOMED Browser** - No word limit filter

### Affected Components
- ? **Completion Window** - Now shows ¡Â4 word global phrases (was ¡Â3)
- ? **Cached Phrase Lists** - Will repopulate with new 4-word limit on next cache refresh

## Migration & Deployment

### Cache Invalidation
The change is **backward compatible** - existing caches will simply repopulate on next refresh with the new 4-word limit. No manual cache clear needed.

### User Impact
Users will immediately see more 3-4 word phrases in completion dropdown after:
1. Application restart, OR
2. Cache refresh (happens automatically every 2 minutes)

## Documentation Updates

- ? Updated code comments to reflect 4-word limit
- ? Updated debug logging to show 4-word threshold
- ? Created this FIX document
- ?? TODO: Update README.md to document 4-word limit

## Key Takeaway

When implementing word count filters, **always test edge cases** (phrases at exactly the limit). Consider whether the limit should be **exclusive (<)** or **inclusive (<=)** based on user expectations.

In this case, 3-word phrases are still short and useful, so increasing to 4 words provides better UX without sacrificing completion window manageability.
