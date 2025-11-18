# Summary: PhraseColorizer Word Connector Fallback

## Quick Overview
Enhanced `PhraseColorizer` to intelligently handle compound words with connectors (`-`, `/`). When "word-word" isn't found as a complete phrase, it now splits and colors each part independently.

## Problem Solved
**Before:**
- "existing-missing" ¡æ entire phrase RED (even though "existing" is valid)

**After:**
- "existing-missing" ¡æ "existing" GRAY, "-" GRAY, "missing" RED
- "COVID-19" in phrase list ¡æ still works as complete phrase (no split)

## Key Changes
1. Added `WordConnectors` array: `{ '-', '/' }`
2. New method `SplitAndColorizeCompoundWord` splits on connectors
3. Fallback only triggers for missing phrases with connectors
4. Connector characters themselves treated as valid (not red)

## Examples
```
Input: "post-operative"
Phrases: ["post", "operative"]
Result: post(gray) -(gray) operative(gray)

Input: "post-operative"  
Phrases: ["post-operative"]
Result: post-operative(gray) [no split, matched as whole]

Input: "left-sided"
Phrases: ["left"]
Result: left(gray) -(gray) sided(RED)
```

## Benefits
- Reduces false positives (red highlighting on valid partial compounds)
- Maintains phrase list flexibility (don't need every compound variation)
- Works with medical terms: "post-operative", "pre-contrast", "N/A"
- Preserves existing exact-match behavior
- Zero performance impact (only processes missing compounds)

## Usage
No code changes required - automatic fallback behavior for all compound words.

## Files Modified
- `src\Wysg.Musm.Editor\Ui\PhraseColorizer.cs`

## Documentation
- Full spec: `ENHANCEMENT_2025-02-10_PhraseColorizerConnectorFallback.md`
