# Enhancement: PhraseColorizer Word Connector Fallback
**Date:** 2025-02-10  
**Component:** `src\Wysg.Musm.Editor\Ui\PhraseColorizer.cs`  
**Type:** Feature Enhancement

## Overview
Enhanced the `PhraseColorizer` to implement intelligent fallback behavior for compound words connected by hyphens (`-`) and forward slashes (`/`). When a compound phrase like "existing-word" is not found in the phrase list, the colorizer now splits it on the connector and colorizes each part independently.

## Motivation
Medical terminology often includes compound words with connectors (e.g., "COVID-19", "post-operative", "N/A", "pre/post-contrast"). Previously, if the exact compound phrase wasn't in the phrase list, the entire token would be marked red (missing), even if individual parts like "post" and "operative" existed separately. This created unnecessary visual noise and false negatives in phrase validation.

## Problem Statement
**Before this change:**
- "existingword-existingword" would appear RED if the exact compound phrase wasn't in the phrase list
- Even if both "existingword" parts existed separately in the phrase list
- This required maintaining every possible compound variation in the phrase list

**Desired behavior:**
1. First, check if "existingword-existingword" exists as a complete phrase
2. If found, colorize it as one unit (with appropriate semantic coloring if available)
3. If NOT found, split on the connector and check each part independently
4. Colorize each part according to whether it exists in the phrase list
5. Treat the connector character itself as valid (to avoid red highlighting)

## Changes Made

### 1. Added Word Connector Array
Defined the connectors that trigger fallback behavior:
```csharp
// Word connectors that trigger fallback behavior
private static readonly char[] WordConnectors = { '-', '/' };
```

### 2. New Helper Method: `ContainsWordConnector`
Simple utility to check if a token contains any word connectors:
```csharp
private static bool ContainsWordConnector(string token)
{
    return token.IndexOfAny(WordConnectors) >= 0;
}
```

### 3. New Helper Method: `SplitAndColorizeCompoundWord`
Core logic for splitting compound words and colorizing each part:
```csharp
private static IEnumerable<PhraseMatch> SplitAndColorizeCompoundWord(
    string token, int baseOffset, HashSet<string> set)
{
    int currentPos = 0;
    
    while (currentPos < token.Length)
    {
        int connectorPos = token.IndexOfAny(WordConnectors, currentPos);
        
        if (connectorPos == -1)
        {
            // No more connectors, handle remaining part
            string part = token.Substring(currentPos);
            if (part.Length > 0)
            {
                bool partExists = set.Contains(part);
                yield return new PhraseMatch(baseOffset + currentPos, part.Length, partExists, part);
            }
            break;
        }
        else
        {
            // Handle part before connector
            if (connectorPos > currentPos)
            {
                string part = token.Substring(currentPos, connectorPos - currentPos);
                bool partExists = set.Contains(part);
                yield return new PhraseMatch(baseOffset + currentPos, part.Length, partExists, part);
            }
            
            // Handle connector itself (treat as existing to avoid red highlighting)
            yield return new PhraseMatch(baseOffset + connectorPos, 1, true, token[connectorPos].ToString());
            
            currentPos = connectorPos + 1;
        }
    }
}
```

### 4. Updated `FindMatchesInLine` Method
Added fallback logic after primary matching:
```csharp
// Check if we need to apply fallback for compound words with connectors
if (!bestExists && !isNumberOrDate && ContainsWordConnector(tokenForMatching))
{
    // Try fallback: split on connectors and check each part
    foreach (var part in SplitAndColorizeCompoundWord(tokenForMatching, wordStart, set))
    {
        yield return part;
    }
}
else
{
    yield return new PhraseMatch(wordStart, matchLen, bestExists, tokenForMatching);
}
```

### 5. Updated XML Documentation
Updated the class summary to document the new behavior:
```csharp
/// - Compound words with connectors (-, /): Tries whole phrase first, then falls back to individual parts
```

## Technical Details

### Fallback Logic Flow
1. **Primary Check**: Token is checked as a complete phrase (e.g., "COVID-19")
2. **Connector Detection**: If not found and contains connectors, trigger fallback
3. **Split Process**: Split on each connector character (`-` or `/`)
4. **Part Validation**: Each part is checked independently against phrase list
5. **Connector Handling**: Connector characters are treated as existing (to avoid red highlighting)

### Example Processing

#### Example 1: Exact Match
- Input: `"COVID-19"`
- Phrase list contains: `["COVID-19"]`
- Result: Single match, colored as one unit

#### Example 2: Fallback Split
- Input: `"post-operative"`
- Phrase list contains: `["post", "operative"]` (but NOT "post-operative")
- Result: Three matches:
  - `"post"` (offset 0, length 4, exists=true)
  - `"-"` (offset 4, length 1, exists=true)
  - `"operative"` (offset 5, length 9, exists=true)

#### Example 3: Partial Match
- Input: `"existing-missing"`
- Phrase list contains: `["existing"]` (but NOT "missing")
- Result: Three matches:
  - `"existing"` (offset 0, length 8, exists=true, colored gray)
  - `"-"` (offset 8, length 1, exists=true, colored gray)
  - `"missing"` (offset 9, length 7, exists=false, colored RED)

#### Example 4: Multiple Connectors
- Input: `"pre/post-contrast"`
- Phrase list contains: `["pre", "post", "contrast"]`
- Result: Five matches:
  - `"pre"` (exists=true)
  - `"/"` (exists=true)
  - `"post"` (exists=true)
  - `"-"` (exists=true)
  - `"contrast"` (exists=true)

### Performance Considerations
- Fallback only triggers for missing phrases with connectors (minimal overhead)
- `IndexOfAny` is optimized for small character arrays
- String operations use efficient `Substring` methods
- No regex compilation overhead (uses simple character matching)
- Iterator pattern (`yield return`) provides lazy evaluation

## Example Behavior

### Before Enhancement
```
Input: "left-sided chest pain"
Phrase list: ["left", "chest", "pain"]
Result: "left-sided" is RED (entire compound marked as missing)
```

### After Enhancement
```
Input: "left-sided chest pain"
Phrase list: ["left", "chest", "pain"]
Result: 
  - "left" is GRAY (exists)
  - "-" is GRAY (connector, treated as valid)
  - "sided" is RED (missing)
  - "chest" is GRAY (exists)
  - "pain" is GRAY (exists)
```

### With Complete Compound Phrase
```
Input: "left-sided chest pain"
Phrase list: ["left-sided", "chest", "pain"]
Result: 
  - "left-sided" is GRAY (exists as complete phrase)
  - "chest" is GRAY (exists)
  - "pain" is GRAY (exists)
```

## Supported Connectors
Currently supports:
- **Hyphen** (`-`): e.g., "post-operative", "COVID-19", "X-ray"
- **Forward Slash** (`/`): e.g., "and/or", "pre/post", "N/A"

**Note:** The connector array can be easily extended to support additional characters if needed (e.g., backslash `\`, pipe `|`, etc.).

## Testing Recommendations
1. **Basic hyphen compounds:**
   - "post-operative" (split)
   - "COVID-19" (complete phrase)
   - "left-sided-anterior" (multiple hyphens)

2. **Forward slash compounds:**
   - "and/or"
   - "pre/post-contrast"
   - "N/A"

3. **Mixed connectors:**
   - "pre/post-operative"
   - "left-sided/right-sided"

4. **Edge cases:**
   - Leading/trailing connectors: "-word", "word-"
   - Empty parts: "word--word"
   - Single character parts: "a-b"

5. **Integration with existing features:**
   - Verify SNOMED semantic colorization still works
   - Test with numbers: "COVID-19" (should handle number correctly)
   - Test with dates: "2025-02-10" (should be treated as date, not split)
   - Test multi-word phrases with connectors: "left-sided chest wall"

6. **Performance testing:**
   - Large documents with many compound words
   - Documents with many missing compound words (triggers fallback frequently)

## Dependencies
- No new dependencies added
- Uses existing .NET string manipulation methods

## Backward Compatibility
This change is fully backward compatible:
- Existing exact phrase matches work as before
- Only adds fallback behavior for missing compound phrases
- No changes to API or public interfaces
- Visual rendering is enhanced, not replaced

## Related Issues
- **Enhancement:** Numbers and Dates - See `ENHANCEMENT_2025-02-09_PhraseColorizerNumbersAndDates.md`
- **Bug Fix:** Trailing Period Handling - See `BUGFIX_2025-02-09_PhraseColorizerTrailingPeriod.md`

## Future Considerations
- Could add configuration to enable/disable connector fallback
- Could make connector list configurable through constructor
- Could add support for additional connectors (backslash, pipe, underscore, etc.)
- Could implement smarter splitting (e.g., preserve common prefixes like "pre-", "post-", "non-")
- Could add statistics/logging for fallback usage to identify missing compound phrases

## Implementation Notes
- The connector character itself is always treated as "existing" to avoid red highlighting on valid punctuation
- Fallback only applies to missing phrases (optimization)
- Numbers and dates bypass the connector fallback (they're already treated specially)
- The split process handles edge cases like consecutive connectors or leading/trailing connectors
- Each part is validated independently against the phrase list
