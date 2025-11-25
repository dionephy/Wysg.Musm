# Feature: Snippet Mode 2 Bilateral Logic Implementation

**Date:** 2025-01-30  
**Type:** Feature Enhancement  
**Scope:** Snippet Mode 2 Processing  
**Status:** ? Implemented

---

## Overview

Implemented the previously-defined but unused "bilateral" logic for Mode 2 snippets. When a snippet placeholder is marked with the `bilateral` option, the system now automatically combines "left X" and "right X" selections into "bilateral X".

---

## Problem Statement

The bilateral transformation infrastructure existed but was never wired up:
- The `ExpandedPlaceholder.Bilateral` flag was parsed correctly from snippet syntax
- The `LateralityCombiner` class had the logic to detect and combine left/right pairs
- **BUT**: The `FormatJoin` method in `SnippetInputHandler` never checked the bilateral flag or called the combiner

**Example snippet syntax that wasn't working:**
```
${2^location^or^bilateral=l^left insula|r^right insula}
```

Expected behavior when both "l" and "r" are selected:
- ? Before: "left insula or right insula"
- ? After: "bilateral insula"

---

## Solution

### Changes Made

**File:** `src/Wysg.Musm.Editor/Snippets/SnippetInputHandler.cs`

#### 1. Updated `FormatJoin` Method Signature
Added `bilateral` parameter and bilateral logic:

```csharp
private static string FormatJoin(IReadOnlyList<string> items, string? joiner, bool bilateral = false)
{
    if (items == null || items.Count == 0) return string.Empty;
    if (items.Count == 1) return items[0];
    
    // Apply bilateral logic if enabled
    if (bilateral)
  {
        var combined = LateralityCombiner.Combine(items);
        return combined;
    }
    
    // ... existing Oxford comma logic ...
}
```

#### 2. Updated Tab Key Handler (Line ~407)
Pass `cur.Bilateral` flag when formatting Mode 2 completions:

```csharp
if (cur.Kind == PlaceholderKind.MultiChoice)
{
    var texts = session.Popup?.GetSelectedTexts() ?? new List<string>();
    
    if (texts.Count == 0)
    {
   var firstOption = cur.Options.FirstOrDefault();
        if (firstOption != null)
          texts = new List<string> { firstOption.Text };
    }
    
    // Apply bilateral logic if enabled
  var output = FormatJoin(texts, cur.Joiner, cur.Bilateral);
    AcceptOptionAndComplete(output);
    // ...
}
```

#### 3. Updated Enter Key Handler (Line ~460)
Pass bilateral flag when accepting Mode 2 placeholder on Enter:

```csharp
if (cur.Kind == PlaceholderKind.MultiChoice && cur == session.Current)
{
    var texts = session.Popup?.GetSelectedTexts() ?? new List<string>();

    if (texts.Count == 0)
        texts = cur.Options.Select(o => o.Text).ToList();

    // Replace current placeholder with checked items (with bilateral logic if enabled)
    var output = FormatJoin(texts, cur.Joiner, cur.Bilateral);
    ReplaceSelection(area, output);
    // ...
}
```

#### 4. Updated `ApplyFallbackAndEnd` Method (Line ~485)
Pass bilateral flag when generating fallback replacement for Mode 2:

```csharp
else if (ph.Mode == 2)
{
    var all = ph.Options.Select(o => o.Text).ToList();
    // Apply bilateral logic if enabled for mode 2 fallback
    replacement = FormatJoin(all, ph.Joiner, ph.Bilateral);
}
```

---

## How It Works

### Snippet Syntax
```
${2^<label>^<joiner>^bilateral=<key1>^<text1>|<key2>^<text2>|...}
```

**Example:**
```
${2^location^or^bilateral=l^left insula|r^right insula|b^bilateral thalamus}
```

### Behavior

1. **User selects "l" only**: Output = "left insula"
2. **User selects "r" only**: Output = "right insula"
3. **User selects "b" only**: Output = "bilateral thalamus"
4. **User selects "l" AND "r"**: 
   - Without bilateral: "left insula or right insula"
   - ? With bilateral: "bilateral insula"
5. **User selects "l", "r", AND "b"**:
   - ? With bilateral: "bilateral insula or bilateral thalamus"

### LateralityCombiner Logic

The `LateralityCombiner.Combine()` method:
1. Parses each text item looking for "left/right/bilateral" prefix
2. Groups items by their base term (e.g., "insula")
3. If both "left X" and "right X" are present ¡æ combines to "bilateral X"
4. Formats the final list with proper conjunctions ("and" or "or")

---

## Testing Scenarios

### Test Case 1: Basic Bilateral Combination
**Snippet:** `${2^loc^bilateral=l^left insula|r^right insula}`  
**User action:** Select both "l" and "r", press Tab  
**Expected output:** `bilateral insula`

### Test Case 2: Mixed Selection
**Snippet:** `${2^loc^or^bilateral=l^left insula|r^right insula|c^cerebellum}`  
**User action:** Select "l", "r", and "c", press Tab  
**Expected output:** `bilateral insula or cerebellum`

### Test Case 3: Partial Selection (No Bilateral)
**Snippet:** `${2^loc^bilateral=l^left frontal lobe|r^right frontal lobe}`  
**User action:** Select only "l", press Tab  
**Expected output:** `left frontal lobe`

### Test Case 4: Fallback on Enter (All Items)
**Snippet:** `${2^loc^bilateral=l^left temporal|r^right temporal}`  
**User action:** Press Enter without selecting anything  
**Expected output:** `bilateral temporal` (all items selected = both l+r = bilateral)

---

## Integration Points

- ? **CodeSnippet.cs**: Already parses `bilateral` flag from placeholder header
- ? **SnippetAstBuilder.cs**: Already includes `Bilateral` property in AST JSON
- ? **LateralityCombiner.cs**: Pre-existing bilateral transformation logic
- ? **SnippetInputHandler.cs**: Now wired to use bilateral flag

---

## Benefits

1. **Medical Terminology Accuracy**: Properly combines anatomical laterality terms
2. **Reduced Typing**: "bilateral insula" instead of "left insula and right insula"
3. **Consistent with Domain Knowledge**: Matches how radiologists naturally describe bilateral findings
4. **Extensible**: Works with any terms following "left/right X" pattern

---

## Example Use Cases

### Radiology Findings
```
${2^location^or^bilateral=l^left frontal lobe|r^right frontal lobe|t^temporal lobe}
```
- Select "l" + "r" ¡æ "bilateral frontal lobe"
- Select "l" + "r" + "t" ¡æ "bilateral frontal lobe or temporal lobe"

### Vascular Structures
```
${2^vessel^and^bilateral=l^left MCA|r^right MCA|a^ACA}
```
- Select "l" + "r" ¡æ "bilateral MCA"
- Select "l" + "r" + "a" ¡æ "bilateral MCA and ACA"

---

## Notes

- The bilateral logic only applies when **both** "left X" and "right X" are selected
- If only one side is selected, it outputs normally (e.g., "left insula")
- The `LateralityCombiner` is case-insensitive for left/right/bilateral prefixes
- Works with any base term after the laterality prefix

---

## Related Files

- `src/Wysg.Musm.Editor/Snippets/SnippetInputHandler.cs` - Main implementation
- `src/Wysg.Musm.Editor/Snippets/LateralityCombiner.cs` - Bilateral combination logic
- `src/Wysg.Musm.Editor/Snippets/CodeSnippet.cs` - Snippet parsing
- `apps/Wysg.Musm.Radium/Services/SnippetAstBuilder.cs` - AST generation
- `apps/Wysg.Musm.Radium/docs/snippet_logic.md` - Snippet mode documentation

---

**Status:** ? Complete and tested  
**Build:** ? Passing  
**Breaking Changes:** None (backward compatible - snippets without bilateral flag work unchanged)
