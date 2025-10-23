# Diagnostic Guide: "Patient Numbers Look the Same But Don't Match"

## Problem

You see this in the log:
```
Patient number mismatch - automation aborted (PACS: '238098', Main: '238098')
```

They look identical, but the comparison fails!

## Root Cause

**Invisible characters** or **different character encodings** are present that you can't see visually.

## How to Diagnose

### Step 1: Check the Debug Log

Look for these lines in your debug output:

```
[ProcedureExecutor][PatientNumberMatch] PACS Patient Number (raw length): 6
[ProcedureExecutor][PatientNumberMatch] PACS Patient Number (raw char codes): '2':50,'3':51,'8':56,'0':48,'9':57,'8':56
[ProcedureExecutor][PatientNumberMatch] Main Patient Number (raw length): 6
[ProcedureExecutor][PatientNumberMatch] Main Patient Number (raw char codes): '2':50,'3':51,'8':56,'0':48,'9':57,'8':56
```

### Step 2: Compare the Character Codes

**If character codes are identical** ¡æ Comparison should succeed (this indicates a bug)

**If character codes differ** ¡æ You've found the issue!

## Common Issues & Solutions

### Issue 1: Different Lengths (Invisible Characters)

**Example**:
```
PACS Patient Number (raw length): 7
Main Patient Number (raw length): 6
```

**Diagnosis**: PACS has an extra invisible character (probably a space, tab, or zero-width character)

**Solution**: 
- Check your PACS data entry - is there a trailing space?
- Check if PACS is padding numbers with spaces
- The normalization should remove this, but double-check the debug log shows normalized lengths are equal

### Issue 2: Full-Width Characters (Asian/Unicode Numbers)

**Example**:
```
PACS: '£²':65298,'£³':65299  (full-width)
Main: '2':50,'3':51          (ASCII)
```

**Diagnosis**: PACS is using full-width unicode numbers (common in Japanese/Chinese systems)

**Characters**:
- Full-width: £°£±£²£³£´£µ£¶£·£¸£¹ (codes 65296-65305)
- ASCII: 0123456789 (codes 48-57)

**Solution**:
- Configure PACS to output ASCII numbers
- Or add full-width to ASCII conversion in normalization logic

### Issue 3: Non-Breaking Space vs Regular Space

**Example**:
```
PACS: ' ':160  (non-breaking space)
Main: ' ':32   (regular space)
```

**Diagnosis**: Different types of spaces

**Solution**: Normalization should remove all spaces

### Issue 4: Unicode Normalization Forms

**Example**:
```
PACS: Combining characters (NFD form)
Main: Precomposed characters (NFC form)
```

**Diagnosis**: Same visual character, different unicode representation

**Solution**: Apply unicode normalization (NFC or NFD) before comparison

## Reading Character Codes

### Common Character Codes Reference

| Character | Code | Type |
|-----------|------|------|
| '0'-'9' | 48-57 | ASCII digits |
| 'A'-'Z' | 65-90 | ASCII uppercase |
| 'a'-'z' | 97-122 | ASCII lowercase |
| ' ' (space) | 32 | Regular space |
| '\t' (tab) | 9 | Tab |
| '\n' (newline) | 10 | Line feed |
| '\r' | 13 | Carriage return |
| NBSP | 160 | Non-breaking space |
| ZWSP | 8203 | Zero-width space |
| '£°'-'£¹' | 65296-65305 | Full-width digits |

### How to Read the Log

**This log**:
```
'2':50,'3':51,'8':56,'0':48,'9':57,'8':56
```

**Means**:
- Character '2' has code 50 (ASCII digit 2) ?
- Character '3' has code 51 (ASCII digit 3) ?
- Character '8' has code 56 (ASCII digit 8) ?
- Character '0' has code 48 (ASCII digit 0) ?
- Character '9' has code 57 (ASCII digit 9) ?
- Character '8' has code 56 (ASCII digit 8) ?

All codes 48-57 = ASCII digits = CORRECT

**Problem example**:
```
'£²':65298,'£³':65299,'£¸':65304
```

Codes 65296-65305 = Full-width digits = WRONG (should be 48-57)

## Quick Fixes

### Fix 1: Trim Whitespace in PACS Getter

If PACS always returns numbers with trailing spaces, update the getter:

```csharp
// In ProcedureExecutor or PacsService
return pacsValue?.Trim();
```

### Fix 2: Add Full-Width to ASCII Conversion

Add this to normalization:

```csharp
string ConvertFullWidth(string s)
{
    var result = new char[s.Length];
    for (int i = 0; i < s.Length; i++)
    {
        if (s[i] >= 65296 && s[i] <= 65305) // Full-width 0-9
            result[i] = (char)(s[i] - 65248); // Convert to ASCII
        else
            result[i] = s[i];
    }
    return new string(result);
}
```

### Fix 3: Unicode Normalization

Add before comparison:

```csharp
using System.Text;
pacsValue = pacsValue?.Normalize(NormalizationForm.FormC);
mainValue = mainValue?.Normalize(NormalizationForm.FormC);
```

## What to Report

When asking for help, include:

1. **Raw lengths**: "PACS: 7 chars, Main: 6 chars"
2. **Character codes**: Copy the entire char codes line from both PACS and Main
3. **Context**: What PACS system are you using? What language/locale?

## Testing Your Fix

After applying a fix:

1. Restart Radium
2. Select the same patient in PACS
3. Run the automation again
4. Check debug log shows character codes now match
5. Verify comparison succeeds

## Contact Support If...

- Character codes are **identical** but comparison still fails (bug in comparison logic)
- You can't identify which characters are different
- You need help implementing a normalization fix
- The issue persists after trying all suggested fixes
