# Split Operation Diagnostic Enhancement - 2025-10-19

## Summary
Added comprehensive debug logging to the Split operation in `ProcedureExecutor.cs` to diagnose why separators like `&pinfo=` and `&pname=` are not matching the URL content.

## Changes Made

### File: `apps\Wysg.Musm.Radium\Services\ProcedureExecutor.cs`

**Method**: `ExecuteRow` �� `case "Split"`

**Added Debug Logging**:
1. **Input diagnostics**:
   - Input length
   - Raw separator string with hex byte representation
   - Whether input contains the raw separator

2. **After unescape diagnostics**:
   - Unescaped separator string with hex byte representation
   - Whether input contains the unescaped separator

3. **Split result diagnostics**:
   - Number of parts after initial split
   - Number of parts after CRLF retry (if applicable)

## How to Use

### Test Procedure

1. **Run the application** and execute your "GetCurrentStudyRemark" procedure
2. **Check Debug Output window** for the new diagnostic messages
3. **Look for these patterns**:

```
[Split] Input length: 150
[Split] SepRaw: '&pinfo=' (length: 7, bytes: 26 70 69 6E 66 6F 3D)
[Split] Input contains separator: True
[Split] After unescape: '&pinfo=' (length: 7, bytes: 26 70 69 6E 66 6F 3D)
[Split] Input contains unescaped separator: True
[Split] Split result: 2 parts
```

### What to Look For

#### Case 1: Separator matches, split works (EXPECTED)
```
[Split] Input contains separator: True
[Split] Input contains unescaped separator: True
[Split] Split result: 2 parts  �� Should be > 1
```

#### Case 2: Separator doesn't match (CURRENT PROBLEM)
```
[Split] Input contains separator: False  �� Problem indicator
[Split] Input contains unescaped separator: False
[Split] Split result: 1 parts  �� What we're seeing now
```

#### Case 3: Hidden characters in separator
```
[Split] SepRaw: '&pinfo=' (length: 8, bytes: 26 70 69 6E 66 6F 3D 20)
                                            ^^                  ^^ �� Extra 0x20 (space)
```

### Expected Byte Sequences

**Correct `&pinfo=` separator**:
```
26 70 69 6E 66 6F 3D
 &  p  i  n  f  o  =
```

**Correct `&pname=` separator**:
```
26 70 6E 61 6D 65 3D
 &  p  n  a  m  e  =
```

## Diagnostic Questions to Answer

1. **Is the separator in the saved procedure file corrupted?**
   - Check: Does SepRaw byte sequence match expected values?
   - Look for extra bytes (trailing spaces, Unicode BOM, etc.)

2. **Is the input URL being modified before reaching Split?**
   - Check: Does Input length match expected URL length?
   - Look for encoding issues in GetText operation

3. **Is there a case sensitivity issue?**
   - Check: Are separator bytes uppercase when they should be lowercase?
   - Example: `&PINFO=` (50 49 4E 46 4F) vs `&pinfo=` (70 69 6E 66 6F)

## Next Steps Based on Output

### If bytes are correct but Contains() returns False:
- String encoding mismatch (UTF-8 vs UTF-16)
- Unicode normalization issue
- Hidden control characters

### If bytes show extra characters:
- Edit procedure in SpyWindow
- Delete and re-type the separator strings
- Save procedure and test again

### If separator is case-mismatched:
- URL uses lowercase `&pinfo=` but procedure has `&PINFO=`
- Make separator match URL case exactly

## Example Test Session

```
Run: GetCurrentStudyRemark procedure
Check Debug Output:

[ProcedureExecutor][ExecuteInternal] Step 2/3: Op='Split'
[Split] Input length: 150
[Split] SepRaw: '&pinfo=' (length: 7, bytes: 26 70 69 6E 66 6F 3D)
[Split] Input contains separator: True
[Split] After unescape: '&pinfo=' (length: 7, bytes: 26 70 69 6E 66 6F 3D)
[Split] Input contains unescaped separator: True
[Split] Split result: 2 parts
[ProcedureExecutor][ExecuteInternal] Step 2 result: preview='2 parts', value='...'
```

**Expected**: `preview='2 parts'` (separator found and split successful)
**Current**: `preview='1 parts'` (separator NOT found, no split occurred)

## Troubleshooting Checklist

- [ ] Run procedure and capture new debug output
- [ ] Compare SepRaw bytes with expected hex values
- [ ] Check if `Input contains separator` is True or False
- [ ] Verify separator string in SpyWindow matches URL exactly
- [ ] Delete/recreate procedure if corruption suspected
- [ ] Test with simple separator (e.g., `,`) to verify Split works

## Related Files
- `apps\Wysg.Musm.Radium\Services\ProcedureExecutor.cs` - Split operation implementation
- `apps\Wysg.Musm.Radium\Views\SpyWindow.Procedures.Exec.cs` - SpyWindow Split implementation
- `%AppData%\Wysg.Musm\Radium\Pacs\{pacs_key}\ui-procedures.json` - Stored procedures

## Contact
If diagnostic output shows unexpected values, please share:
1. Full debug output from `[Split]` lines
2. Screenshot of procedure configuration in SpyWindow
3. Sample URL text from Step 1 (GetText result)
