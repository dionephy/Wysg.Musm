# Visual Guide: Side-by-Side Diff Viewer

**Last Updated**: 2025-11-25

---

## What It Looks Like

```
������������������������������������������������������������������������������������������������������������������������������������������������������
�� �� Show Changes (Side-by-Side) (character/word/line-level diff)        ��
������������������������������������������������������������������������������������������������������������������������������������������������������
��                 ��
��  Original (Left Panel)         ��  Modified (Right Panel)       ��
��  ???????????????????????     ��  ???????????????????????         ��
��             ��        ��
��  no acute hemorrhage      ��  No acute intracranial hemorrhage.   ��
��  ^^      ��  ^^     ^^^^^^^^^^^^  ^   ��
��  red (lowercase deleted)        ��  green (uppercase added)            ��
��    ��  green (word added)        ��
��       ��  green (period added)               ��
��              ��  ��
��  Line 2 deleted      ��  (empty - line removed)           ��
��  ^^^^^^^^^^^^^^ red    ��  gray placeholder             ��
�� ��             ��
��  (empty - line added)  ��  Line 3 added     ��
��  gray placeholder     ��  ^^^^^^^^^^^^^^^ green        ��
��         ��         ��
������������������������������������������������������������������������������������������������������������������������������������������������������
```

---

## Color Coding

### Line-Level (Subtle Background)
- **?? Red tint**: Entire line deleted (left panel)
- **?? Green tint**: Entire line added (right panel)
- **?? Yellow tint**: Line modified (both panels)
- **? Gray**: Placeholder for alignment

### Character/Word-Level (Bright Highlight)
- **?? Bright red + ~~strikethrough~~**: Specific text deleted (left panel)
- **?? Bright green**: Specific text added (right panel)
- **?? Yellow**: Text modified (both panels)

---

## Real Examples

### Example 1: Capitalization Fix
```
LEFT (Original)      �� RIGHT (Modified)
???????????????????????��???????????????????????
no acute hemorrhage   �� No acute hemorrhage
^^  �� ^^
?? red + strike      �� ?? green
```

### Example 2: Word Insertion
```
LEFT (Original)   �� RIGHT (Modified)
??????????????????????????????��??????????????????????????????
No acute hemorrhage  �� No acute intracranial hemorrhage
     ��     ?? green line tint
   ��       ^^^^^^^^^^^^
        ��  ?? bright green
```

### Example 3: Multi-Line Changes
```
LEFT (Original)      �� RIGHT (Modified)
???????????????????????��???????????????????????
Line 1: Normal    �� Line 1: Normal
       ��
Line 2: Deleted�� [gray placeholder]
?? red line tint   �� ? alignment
       ��
[gray placeholder]     �� Line 2: New finding
? alignment �� ?? green line tint
       ��        ^^^^^^^^^^^^^^^^^^^^
        ��          ?? bright green
Line 3: Normal    �� Line 3: Normal
```

---

## Interactive Features

### 1. Collapsible Panel
- **Collapsed (default)**: Only toggle button visible
- **Click ��**: Panel expands, showing side-by-side diff
- **Click again**: Panel collapses

### 2. Synchronized Scrolling
- Scroll left panel �� Right panel moves with it
- Scroll right panel �� Left panel moves with it
- Always shows same lines

### 3. Resizable Panels
- **Drag splitter** (middle bar) left/right
- Adjust panel widths to preference
- Maintains proportions

---

## How to Use

### Step 1: Type and Generate
1. Type findings in **Findings** textbox (left column)
2. Click **"gen"** button to generate proofread
3. Proofread appears in **Findings (PR)** textbox (right column)

### Step 2: View Diff
1. Click **"�� Show Changes"** toggle button
2. Side-by-side diff viewer expands
3. **Left panel**: Shows original with deletions (red)
4. **Right panel**: Shows modified with insertions (green)

### Step 3: Review Changes
1. **Line-level**: Subtle background colors show line changes
2. **Character-level**: Bright highlights show exact character changes
3. **Scroll**: Both panels scroll together for easy comparison

### Step 4: Edit or Accept
- **Accept**: If changes look good, proceed to send report
- **Edit**: Edit **Findings (PR)** textbox, diff updates automatically
- **Regenerate**: Click "gen" again for new proofread

### Step 5: Collapse
1. Click toggle button again to hide diff viewer
2. Saves screen space when not needed

---

## Keyboard Shortcuts

(Same as other panels)

- **Alt + Arrow Keys**: Navigate between textboxes
- **Alt + Down**: Move from Findings to Findings (PR)
- **Alt + Up**: Move from Findings (PR) back to Findings

---

## Tips & Best Practices

### ? Do
- Expand diff before sending report (quality check)
- Look for unintended changes in red (left panel)
- Verify new text in green (right panel) is correct
- Use synchronized scrolling to compare long texts
- Resize panels if one side is truncated

### ? Don't
- Don't ignore red deletions (may be important)
- Don't assume all green additions are improvements
- Don't send without reviewing diff
- Don't forget to collapse when done (saves space)

---

## Troubleshooting

### Issue: Panels Not Aligned
**Cause**: Different text lengths

**Solution**: Scroll to top, panels auto-align with placeholder lines

### Issue: Diff Not Updating
**Cause**: Toggle button not expanded

**Solution**: Click toggle button to expand diff viewer

### Issue: Can't Edit Diff Text
**Cause**: Diff viewer is read-only (by design)

**Solution**: Edit original textboxes (Findings or Findings PR), diff updates automatically

### Issue: Text Too Small
**Cause**: Default font size

**Solution**: Resize panels by dragging splitter, or edit font size in code

---

## Comparison with Other Tools

### Similar To:
- ? **DiffPlex GUI** - Same engine, same visual style
- ? **GitHub Diff Viewer** - Side-by-side layout
- ? **Visual Studio Code Diff** - Color-coded highlights
- ? **Beyond Compare** - Professional diff tool

### Advantages:
- ? **Integrated** - No need to export/import
- ? **Real-time** - Updates as you edit
- ? **Collapsible** - Doesn't clutter UI
- ? **Synchronized** - Scrolls together automatically

---

## Frequently Asked Questions

### Q: Why side-by-side instead of inline?
**A**: Side-by-side is clearer for comparing two versions. Inline mixes deletions and insertions, making it harder to see what changed.

### Q: Can I copy text from the diff viewer?
**A**: Yes! The panels are RichTextBoxes, you can select and copy text.

### Q: Can I edit text in the diff viewer?
**A**: No, it's read-only. Edit in the original textboxes above.

### Q: Why is the diff collapsed by default?
**A**: To save screen space. Most users don't need to see the diff constantly.

### Q: Can I have multiple diff viewers open?
**A**: Currently only Findings diff. Future enhancement: Conclusion diff.

### Q: Does this affect performance?
**A**: Minimal impact. DiffPlex is very fast (<20ms for typical findings).

---

**Feature Status**: ? Production Ready  
**Last Updated**: 2025-11-25  
**Version**: 1.0
