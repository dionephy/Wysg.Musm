# User Guide: Findings (PR) Diff Visualization

**Last Updated**: 2025-02-02

---

## Overview

The Findings (PR) textbox now shows **character-by-character differences** between your original findings and the AI-proofread version using color-coded inline highlighting.

---

## Visual Guide

### Color Code
- ? **Green background** = Text **added** by AI
- ? **Red background + strikethrough** = Text **deleted/removed** from original
- ? **Normal text** = **Unchanged** from original

---

## Example Use Cases

### 1. Capitalization Fix
```
Your original:     "no acute hemorrhage"
AI proofread:      "No acute hemorrhage"

What you see in Findings (PR):
[n?][N]o acute hemorrhage
red green
```
- **Red "n"** = Original lowercase "n" removed
- **Green "N"** = New uppercase "N" added
- Rest unchanged

### 2. Word Insertion
```
Your original:     "No acute hemorrhage"
AI proofread:      "No acute intracranial hemorrhage"

What you see:
No acute [intracranial ]hemorrhage
       ^^^^^^^^^^^^^^
 (green)
```
- Green highlight = "intracranial " was added

### 3. Punctuation Addition
```
Your original:     "Normal study"
AI proofread:      "Normal study."

What you see:
Normal study[.]
    ^^^
         (green)
```
- Green period = Punctuation was added

### 4. Complex Medical Edit
```
Your original:     "lungs clear bilaterally"
AI proofread:      "Lungs are clear bilaterally."

What you see:
[l?][L]ungs[ are] clear bilaterally[.]
red green green     green
```
- Capitalization change: "l" ¡æ "L"
- "are" added
- Period added

---

## How to Use

### Step 1: Type Findings
1. Type your findings in the **Findings** textbox (left column)
2. Can be raw, unformatted text

### Step 2: Generate Proofread
1. Click **"gen"** button next to "Findings (PR)" label
2. AI generates proofread version
3. Diff visualization appears automatically

### Step 3: Review Changes
1. Look at **Findings (PR)** textbox (right column)
2. Green highlights = additions
3. Red strikethroughs = deletions
4. Review each change

### Step 4: Accept or Regenerate
- **Accept**: If changes look good, proceed to send report
- **Regenerate**: Click "gen" again to get new proofread version
- **Manual edit**: Edit original findings and regenerate

---

## Tips & Best Practices

### ? Do
- Review diff before sending report
- Look for unintended changes (wrong terminology, changed meaning)
- Use diff to learn AI's proofreading patterns
- Regenerate if changes don't look right

### ? Don't
- Don't rely blindly on AI proofreading
- Don't ignore red strikethroughs (deletions may be significant)
- Don't send report without reviewing diff

---

## Common Scenarios

### Scenario: Too Many Changes
**Problem**: AI changed too much text, hard to review

**Solution**:
1. Edit original findings to be clearer
2. Regenerate proofread
3. If still too many changes, consider manual editing in JSON

### Scenario: Wrong Medical Term
**Problem**: AI replaced correct term with incorrect one

**Example**:
```
Original:  "hepatomegaly"
AI:        "enlarged liver"  (wrong - too general)
```

**Solution**:
1. Note the change (red strikethrough on "hepatomegaly")
2. Edit original to make term usage clearer
3. Regenerate, or manually edit in JSON

### Scenario: No Changes Needed
**Problem**: Original text was already perfect

**Behavior**:
```
Findings (PR) shows:  "No acute intracranial hemorrhage."
      (all normal text, no colors)
```
- If no highlighting appears, AI made no changes
- Original text was already good

---

## Technical Notes

### Read-Only Display
- Findings (PR) textbox is **read-only** (diff view only)
- Cannot edit directly in this textbox
- To edit proofread text:
  1. Edit original findings and regenerate, or
  2. Edit `findings_proofread` field in JSON panel

### Real-Time Updates
- Diff updates automatically when:
  - Original findings text changes
  - Proofread text is regenerated
  - JSON is edited manually

### Performance
- Fast for normal-sized findings (<1000 chars)
- Slight delay for very long findings (>10,000 chars)

---

## Troubleshooting

### Issue: No Colors Showing
**Cause**: Original and proofread are identical

**Solution**: This is normal - no changes were made

### Issue: Diff Looks Wrong
**Cause**: Character-level diff can look strange for large edits

**Example**:
```
Original:  "aaa bbb ccc"
Modified:  "xxx yyy zzz"

Diff may show many small red/green chunks
```

**Solution**: This is expected - diff shows exact character changes

### Issue: Textbox Looks Blank
**Cause**: Both original and proofread are empty

**Solution**: Type findings first, then generate proofread

---

## Keyboard Shortcuts

(Same as other textboxes in Radium)

- **Alt + Arrow Keys**: Navigate between textboxes
- **Alt + Down**: Move from Findings to Findings (PR)
- **Alt + Up**: Move from Findings (PR) back to Findings
- **Alt + Left/Right**: Move between columns

---

## Related Features

- **Proofread Toggle**: Shows proofread versions in editor
- **Reportified Toggle**: Shows formatted versions in editor
- **JSON Panel**: Shows raw JSON with all field values
- **Auto-generation buttons**: Generate proofread versions via LLM

---

## Frequently Asked Questions

### Q: Can I edit the diff view directly?
**A**: No, it's read-only. Edit original findings and regenerate, or edit JSON manually.

### Q: Why are some changes highlighted in both red and green?
**A**: Character-level diff shows exact changes. Example: "a" ¡æ "A" shows red "a" + green "A".

### Q: Can I hide the diff colors?
**A**: Not currently. Future enhancement may add toggle for plain text view.

### Q: Does this affect what gets sent to PACS?
**A**: No, diff is visualization only. Proofread text (without colors) is sent to PACS.

### Q: Can I use this for Conclusion (PR) too?
**A**: Not yet. Current implementation is Findings (PR) only. May be extended to other fields in future.

---

## Feedback

If you encounter issues or have suggestions:
1. Note the specific scenario (original text, proofread text, unexpected behavior)
2. Take a screenshot if possible
3. Report to development team

---

**Feature Status**: ? Production Ready  
**Last Updated**: 2025-02-02  
**Version**: 1.0
