# Quick Reference: PhraseColorizer Connector Fallback

## What It Does
Splits compound words on `-` and `/` when the complete phrase isn't found, then colors each part individually.

## Decision Flow
```
1. Check if "word-word" exists in phrase list
   戍式 YES ⊥ Color as one phrase (done)
   戌式 NO  ⊥ Contains connector?
            戍式 YES ⊥ Split on connector
            弛        戍式 Check "word" ⊥ color accordingly
            弛        戍式 Check "-" ⊥ color gray (always valid)
            弛        戌式 Check "word" ⊥ color accordingly
            戌式 NO  ⊥ Color as one phrase (red if missing)
```

## Supported Connectors
- Hyphen: `-`
- Forward slash: `/`

## Code Location
**File:** `src\Wysg.Musm.Editor\Ui\PhraseColorizer.cs`

**Key Methods:**
- `ContainsWordConnector(string token)` - Checks for connectors
- `SplitAndColorizeCompoundWord(string, int, HashSet)` - Splits and validates parts
- `FindMatchesInLine(string, HashSet)` - Main logic with fallback trigger

## Examples

### Example 1: Split Fallback
```
Phrase: "post-operative"
List: ["post", "operative"]
Result: "post" (gray) + "-" (gray) + "operative" (gray)
```

### Example 2: Exact Match (No Split)
```
Phrase: "post-operative"
List: ["post-operative"]
Result: "post-operative" (gray, as one unit)
```

### Example 3: Partial Match
```
Phrase: "left-sided"
List: ["left"]
Result: "left" (gray) + "-" (gray) + "sided" (RED)
```

### Example 4: Multiple Connectors
```
Phrase: "pre/post-contrast"
List: ["pre", "post", "contrast"]
Result: "pre" + "/" + "post" + "-" + "contrast" (all gray)
```

### Example 5: Number Dates (Special Case)
```
Phrase: "2025-02-10"
List: []
Result: "2025-02-10" (gray, treated as date - NO split)
```

## When Fallback Triggers
? Phrase NOT in list + contains connector ⊥ **SPLIT**
? "existing-missing" ⊥ Split and color parts
? Phrase IS in list ⊥ No split
? Number/date format ⊥ No split (handled by date/number logic)

## Performance
- Only processes MISSING phrases with connectors
- No overhead for existing phrases
- No overhead for phrases without connectors
- Efficient string operations (no regex)

## Testing Checklist
- [ ] "post-operative" with both parts in list
- [ ] "left-sided" with only "left" in list
- [ ] "COVID-19" as exact phrase in list
- [ ] "and/or" with forward slash
- [ ] "pre/post-contrast" with multiple connectors
- [ ] "2025-02-10" date format (should NOT split)
- [ ] Edge cases: "-word", "word-", "word--word"

## Common Use Cases
- Medical prefixes: "post-operative", "pre-contrast", "non-specific"
- Compound terms: "left-sided", "right-upper", "antero-posterior"  
- Alternatives: "and/or", "yes/no", "N/A"
- Medical codes: "COVID-19", "H1N1", "HER2-positive"

## Configuration
Currently hardcoded connectors: `{ '-', '/' }`

To add more connectors, modify:
```csharp
private static readonly char[] WordConnectors = { '-', '/', '\\', '_' };
```

## Integration
Works seamlessly with:
- SNOMED semantic tag coloring
- Number/date recognition
- Multi-word phrase matching
- Trailing period handling
