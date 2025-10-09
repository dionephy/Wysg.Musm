# Snippet Syntax Reference Guide

## Overview
This document provides a quick reference for creating snippet templates with placeholder syntax according to the implementation in Wysg.Musm.Radium.

## Basic Syntax

### 1. Plain Text Snippet (No Placeholders)
```
Trigger: naa
Template: No acute abnormality identified.
```
**Behavior:** Inserts text directly without entering snippet mode.

---

## Placeholder Modes

### Mode 0: Free Text Placeholder
**Syntax:** `${label}`

**Example:**
```
Template: The patient has ${condition} with ${severity} symptoms.
```

**Behavior:**
- User can type any text
- `Tab` keeps typed text and moves to next placeholder
- Early exit (Enter/Esc before Tab) inserts `[ ]` as fallback

**Use Case:** Open-ended text input where the user provides custom content.

---

### Mode 1: Single Choice (Immediate Selection)
**Syntax:** `${1^label=key1^text1|key2^text2|key3^text3}`

**Example:**
```
Template: Impression: ${1^result=a^No acute abnormality|b^Acute infarction|c^Hemorrhage}.
```

**Behavior:**
- Pressing a **single key** (`a`, `b`, or `c`) immediately replaces placeholder with corresponding text
- No need to press `Tab` for mode 1
- Early exit defaults to **first option**

**Use Case:** Quick selection from a small set of common findings.

**Popup Display:**
```
a: No acute abnormality
b: Acute infarction
c: Hemorrhage
```

---

### Mode 2: Multiple Choice (Toggle Selection)
**Syntax:** `${2^label^option1^option2=key1^text1|key2^text2|key3^text3}`

**Options:**
- `or` ¡æ Join with "or" (e.g., "A, B, or C")
- `and` ¡æ Join with "and" (e.g., "A, B, and C")
- `bilateral` ¡æ Flag for future bilateral processing

**Example (with "or" joiner):**
```
Template: Restricted diffusion in the ${2^site^or=li^left insula|ri^right insula|lc^left corona radiata}.
```

**Behavior:**
- Press **Space** or matching **key** to toggle selection (adds checkmark ?)
- Multiple options can be selected
- `Tab` joins selected options with specified joiner
- Early exit inserts **all options** joined

**Use Case:** Selecting multiple applicable findings or anatomical locations.

**Popup Display (after pressing `li` and `lc`):**
```
? li: left insula
ri: right insula
? lc: left corona radiata
```

**Result after Tab:**
```
Restricted diffusion in the left insula, or left corona radiata.
```

---

### Mode 3: Single Replace (Multi-Char Key)
**Syntax:** `${3^label=key1^text1|key2^text2|key3^text3}`

**Example:**
```
Template: Impression: ${3^finding=naa^No acute abnormality|ich^Intracranial hemorrhage|inf^Acute infarction}.
```

**Behavior:**
- User types **multi-character key** (e.g., "ich")
- Keys accumulate in buffer until `Tab` or `Enter`
- Popup updates to highlight matching option as user types
- `Tab` replaces placeholder with matched text
- Early exit defaults to **first option**

**Use Case:** Longer trigger codes for detailed findings (e.g., medical abbreviations).

**Popup Display (while typing "i", then "c", then "h"):**
```
naa: No acute abnormality
ich: Intracranial hemorrhage  ¡ç highlighted after "ich"
inf: Acute infarction
```

---

## Special Placeholders (Macros)

### Date Macro
**Syntax:** `${date}`

**Behavior:** Auto-fills with current date in `yyyy-MM-dd` format.

**Example:**
```
Template: Study date: ${date}
Result: Study date: 2025-01-10
```

---

### Number Macro
**Syntax:** `${number}`

**Behavior:** Auto-fills with `0`.

**Example:**
```
Template: Lesion count: ${number}
Result: Lesion count: 0
```

---

## Navigation & Control Keys

| Key | Behavior |
|-----|----------|
| **Tab** | Complete current placeholder and move to next; exit if none |
| **Enter** | Apply fallback replacement (free: `[ ]`, choice: first/all) and exit to next line |
| **Escape** | Apply fallback replacement and move caret to end of snippet |
| **Up/Down** | Navigate popup options (choice modes) |
| **Space** | Toggle current option (mode 2 only) |
| **Letter/Digit** | Mode 1: immediate select; Mode 2: toggle; Mode 3: accumulate |
| **Arrow keys, Home, End** | **Blocked** (caret locked within current placeholder) |

---

## Mode 2 Joiner Examples

### "or" Joiner (Default)
```
Template: ${2^symptoms^or=h^headache|n^nausea|d^dizziness}
Selections: h, d
Result: headache, or dizziness
```

### "and" Joiner
```
Template: ${2^features^and=e^edema|h^hemorrhage|i^infarction}
Selections: e, h
Result: edema, and hemorrhage
```

### No Explicit Joiner (Defaults to "and")
```
Template: ${2^findings=m^mass|c^calcification}
Selections: m, c
Result: mass, and calcification
```

---

## Complete Example: Multi-Placeholder Snippet

**Trigger:** `dwi-multi`

**Template:**
```
MRI brain DWI sequence shows restricted diffusion in the ${2^location^or=li^left insula|ri^right insula|th^thalamus|bs^brainstem} compatible with ${1^etiology=a^acute infarction|s^subacute infarction|c^cytotoxic edema}. Size: approximately ${size} mm. Additional findings: ${comments}.
```

**Workflow:**
1. User types `dwi-multi` and accepts from completion
2. Snippet expands with first placeholder highlighted: `[location]`
3. Popup shows location options; user toggles `li` and `th`
4. User presses `Tab` ¡æ text becomes: `left insula, or thalamus`
5. Next placeholder highlighted: `[etiology]`
6. User presses `a` ¡æ immediately replaced with: `acute infarction`
7. Next placeholder: `[size]` (free text)
8. User types: `8` then `Tab`
9. Next placeholder: `[comments]` (free text)
10. User types: `No mass effect` then `Tab`
11. Snippet mode exits; final text:
```
MRI brain DWI sequence shows restricted diffusion in the left insula, or thalamus compatible with acute infarction. Size: approximately 8 mm. Additional findings: No mass effect.
```

---

## AST Structure (Auto-Generated)

When saving a snippet, the system automatically generates a JSON AST for fast runtime parsing:

**Example Template:**
```
${1^fruit=a^apple|b^banana|3^pear}
```

**Generated AST:**
```json
{
  "version": 1,
  "placeholders": [
    {
      "mode": 1,
      "label": "fruit",
      "tabstop": 1,
      "options": [
        {"key": "a", "text": "apple"},
        {"key": "b", "text": "banana"},
        {"key": "3", "text": "pear"}
      ],
      "joiner": null,
      "bilateral": null
    }
  ]
}
```

**Mode 2 with Options:**
```
${2^drinks^or^bilateral=c^cola|j^juice}
```

**Generated AST:**
```json
{
  "version": 1,
  "placeholders": [
    {
      "mode": 2,
      "label": "drinks",
      "tabstop": 2,
      "options": [
        {"key": "c", "text": "cola"},
        {"key": "j", "text": "juice"}
      ],
      "joiner": "or",
      "bilateral": true
    }
  ]
}
```

---

## Best Practices

### 1. Choose Appropriate Mode
- **Mode 1:** Quick single selection from 2-5 common options
- **Mode 2:** Multiple applicable findings/locations
- **Mode 3:** Longer trigger codes (3+ chars) for specific terminology
- **Free text:** Open-ended descriptions

### 2. Key Design
- Mode 1: Single letter or digit (e.g., `a`, `b`, `1`, `2`)
- Mode 2: Single letter for quick toggle (e.g., `l`, `r` for left/right)
- Mode 3: Short abbreviations (2-4 chars, e.g., `naa`, `ich`, `inf`)

### 3. Option Ordering
- Place most common options first (mode 1 defaults to first, mode 2 shows all)
- Keep option count reasonable (<10 per placeholder)

### 4. Joiner Selection
- Use `or` for differential findings
- Use `and` for concurrent features
- Omit joiner for default "and" behavior

### 5. Description Field
- Provide clear, concise description for UI display
- Example: "DWI positive findings with location options"

---

## Troubleshooting

### Issue: Placeholder not expanding
**Solution:** Check that trigger text doesn't conflict with existing hotkeys or common words.

### Issue: Wrong option selected
**Solution:** Verify key uniqueness within each placeholder; mode 1 uses single keys, mode 3 uses multi-char keys.

### Issue: Joiner not working
**Solution:** Ensure mode 2 syntax with `^or` or `^and` after label and before `=`.

### Issue: Caret escaping placeholder
**Solution:** This should not happen; if it does, report as bug (caret lock is enforced).

---

## Quick Reference Table

| Mode | Syntax Pattern | Key Type | Selection | Fallback |
|------|---------------|----------|-----------|----------|
| 0 (Free) | `${label}` | N/A | User types | `[ ]` |
| 1 (Single) | `${1^label=k^text\|...}` | Single char | Immediate | First option |
| 2 (Multi) | `${2^label^opt=k^text\|...}` | Single char | Toggle | All options |
| 3 (Replace) | `${3^label=kk^text\|...}` | Multi-char | On Tab | First option |

---

## Resources

- **Implementation:** `docs\SnippetRuntimeImplementationSummary.md`
- **Logic Spec:** `apps\Wysg.Musm.Radium\docs\snippet_logic.md`
- **Database Schema:** `db\migrations\20251010_add_snippet_table.sql`
- **Service Interface:** `apps\Wysg.Musm.Radium\Services\ISnippetService.cs`
- **Editor Handler:** `src\Wysg.Musm.Editor\Snippets\SnippetInputHandler.cs`

---

**Last Updated:** 2025-01-10
**Version:** 1.0
