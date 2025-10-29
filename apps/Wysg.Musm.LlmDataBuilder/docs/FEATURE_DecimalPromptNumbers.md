# Feature: Decimal Prompt Numbers (v1.3.4)

## What Changed

The **Applied Prompt Numbers** field now accepts decimal numbers like `1.1`, `1.2`, `2.1` in addition to integers.

## Quick Examples

### Before (integers only)
```
1,2,3
```

### After (integers and decimals)
```
1,1.1,1.2,2,2.1,2.2,3
```

## Use Cases

### 1. Hierarchical Prompts
```
Prompt Numbers: 1, 1.1, 1.2, 1.3

Organization:
  1   - Main category
  1.1 - Subcategory A
  1.2 - Subcategory B
  1.3 - Subcategory C
```

### 2. Prompt Versioning
```
Prompt Numbers: 1.0, 1.1, 1.2, 2.0

Versions:
  1.0 - Initial prompt
  1.1 - First revision
  1.2 - Second revision
  2.0 - Major update
```

### 3. Template Sets
```
Prompt Numbers: 1, 1.1, 1.2, 2, 2.1

Templates:
  Set 1: Prompts 1, 1.1, 1.2
  Set 2: Prompts 2, 2.1
```

## Valid Formats

? **Integers**: `1,2,3`  
? **Decimals**: `1.1,1.2,1.3`  
? **Mixed**: `1,1.1,2,2.1,3`  
? **With spaces**: `1.1, 1.2, 1.3` (auto-trimmed)  
? **Multiple decimals**: `1.12,2.34`

? **Invalid**: `1.a`, `abc`, `1..2`

## Data Format

### JSON Storage
```json
{
  "appliedPromptNumbers": ["1", "1.1", "1.2", "2"]
}
```

Numbers are stored as **strings** to preserve decimal format.

### Display
Data Browser shows numbers exactly as entered:
- Input: `1,1.1,1.2`
- Display: `1, 1.1, 1.2`

## Backward Compatibility

? **Fully compatible** with existing integer-only data  
? **No migration required** - old data works as-is  
? **Auto-conversion** - integers automatically become strings

### Example Migration
```json
// Old format (still works)
{
  "appliedPromptNumbers": [1, 2, 3]
}

// New format (after save)
{
  "appliedPromptNumbers": ["1", "2", "3"]
}
```

## How to Use

1. **Enter numbers** in the Applied Prompt Numbers field
2. **Separate with commas**: `1,1.1,1.2`
3. **Spaces optional**: `1, 1.1, 1.2` (auto-trimmed)
4. **Click Save** - validation ensures all entries are valid numbers

## Validation

Each entry is checked with `decimal.TryParse`:
- Valid: Any number that C# can parse as decimal
- Invalid: Letters, symbols, malformed decimals

**Error message** if invalid:
```
Please enter valid comma-separated numbers.
Examples: 1,2,3 or 1.1,1.2,2.1
```

## Real-World Example

### Medical Report Prompts
```
Applied Prompt Numbers: 1, 1.1, 1.2, 2, 2.1, 2.2

Prompt Structure:
  1   - Standard format
  1.1 - Detailed clinical history
  1.2 - Concise clinical history
  2   - Technical description
  2.1 - With measurements
  2.2 - Without measurements
```

### LLM Fine-Tuning Templates
```
Applied Prompt Numbers: 1.0, 1.1, 1.2, 2.0

Template Evolution:
  1.0 - Baseline prompt
  1.1 - Added context
  1.2 - Refined phrasing
  2.0 - Complete redesign
```

## Benefits

? **Organization**: Group related prompts hierarchically  
? **Versioning**: Track prompt evolution  
? **Clarity**: See relationship between prompts  
? **Flexibility**: Mix integers and decimals as needed

## See Also

- [README.md](README.md) - Full documentation
- [CHANGELOG.md](CHANGELOG.md) - Version 1.3.4 details
- [DATA_SCHEMA.md](DATA_SCHEMA.md) - JSON schema documentation

---

**Version**: 1.3.4  
**Date**: 2025-01-24  
**Backward Compatible**: ? Yes  
**Migration Required**: ? No
