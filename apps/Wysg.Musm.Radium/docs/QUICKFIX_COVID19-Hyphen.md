# Quick Fix: COVID-19 Not Highlighting Correctly

**Issue**: "COVID-19" shows as red (missing phrase) even though it's saved in the phrase database.

**Cause**: The hyphen in "COVID-19" was being treated as a word separator instead of part of the word.

**Fix Date**: 2025-11-02

---

## What Changed?

The editor now treats **hyphens as part of words** when matching phrases. This means:

? **"COVID-19"** now matches correctly (not split into "COVID" and "19")  
? **"T-cell"** matches as a single word  
? **"follow-up"** matches as a single word  
? **"non-small-cell lung cancer"** matches as a complete phrase  

---

## How to Test

1. **Add "COVID-19" to your phrase database** (if not already added):
   - Go to Settings �� Global Phrases
   - Click "Add Phrase"
   - Type "COVID-19"
   - Click "Save"

2. **Type "COVID-19" in the editor**:
 - It should highlight in **gray** (or SNOMED color if mapped)
   - It should **NOT** be red anymore

3. **Try other hyphenated terms**:
   - "T-cell lymphoma"
   - "X-ray"
   - "follow-up examination"

---

## What If It Still Shows Red?

1. **Check if the phrase is saved**:
   - Go to Settings �� Global Phrases
   - Search for "COVID-19"
   - Make sure "Active" is checked

2. **Refresh the phrase cache**:
   - In the editor, press `Ctrl+Shift+R` (or restart the app)
   - The phrase list reloads from the database

3. **Check exact spelling**:
   - "COVID-19" �� "COVID19" (no hyphen)
   - "COVID-19" �� "COVID -19" (space before hyphen)
   - Case doesn't matter: "covid-19" = "COVID-19" ?

---

## Technical Notes

### What Changed in the Code?
- **File**: `src\Wysg.Musm.Editor\Ui\PhraseHighlightRenderer.cs`
- **Change**: Hyphens now included in word character check
- **Impact**: All hyphenated medical terms now work correctly

### Does This Affect Performance?
No - the change is minimal and adds no performance overhead.

### Will Old Phrases Break?
No - phrases without hyphens work exactly the same as before.

---

## Examples

| Before Fix | After Fix |
|-----------|-----------|
| "COVID-19" �� **RED** ? | "COVID-19" �� **GRAY** ? |
| "T-cell" �� **RED** ? | "T-cell" �� **GRAY** ? |
| "follow-up" �� **RED** ? | "follow-up" �� **GRAY** ? |
| "X-ray" �� **RED** ? | "X-ray" �� **GRAY** ? |

---

## Questions?

### Q: What about apostrophes (e.g., "patient's")?
**A**: Apostrophes are still treated as delimiters. "patient's" will be tokenized as "patient" + "s". This is intentional for medical accuracy.

### Q: What about slashes (e.g., "and/or")?
**A**: Slashes are still delimiters. "and/or" will be tokenized as "and" + "or".

### Q: Can I add "COVID - 19" with spaces?
**A**: Yes, but it won't match "COVID-19". The system treats them as different phrases:
- "COVID-19" (with hyphen, no spaces)
- "COVID - 19" (three separate words)

### Q: Does this work for Korean phrases?
**A**: Yes - the fix applies to all languages. Hyphens in Korean medical terms (if any) will also work correctly.

---

## Support

If you still see issues after applying this fix:
1. Check the documentation: `docs\BUGFIX_2025-11-02_COVID19-Hyphen.md`
2. Run the verification tests: `docs\Tasks.md` (V470-V480)
3. Contact support with details about which phrase is not highlighting

---

**Version**: Fixed in build dated 2025-11-02 or later  
**Affected Component**: Phrase-based syntax highlighting  
**Severity**: Low (cosmetic issue, no data loss)  
**Status**: ? Fixed and verified
