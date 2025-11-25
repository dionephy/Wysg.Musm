# USER GUIDE: Alt+Arrow Navigation Feature

**Feature**: Quick Navigation Between Text Fields  
**Version**: 1.0  
**Date**: 2025-01-30

---

## Overview

The Alt+Arrow navigation feature allows you to quickly move between related text fields using keyboard shortcuts, with the ability to copy selected text when navigating.

---

## Quick Start

### Moving Between Fields

1. Place your cursor in any supported text field
2. Press **Alt+Arrow key** to move to an adjacent field
3. The cursor will be positioned at the end of the target field

### Copying Text While Moving

1. **Select text** in the current field
2. Press **Alt+Arrow key** to move to an adjacent field
3. The selected text will be **copied to the end** of the target field
4. The cursor will move to the target field

---

## Supported Navigation Mappings

### Report Inputs Panel

| From Field | Press | To Field |
|-----------|-------|----------|
| **Study Remark** | Alt+Down �� | Chief Complaint |
| **Chief Complaint** | Alt+Up �� | Study Remark |
| **Chief Complaint** | Alt+Right �� | Chief Complaint (Proofread) |
| **Chief Complaint (Proofread)** | Alt+Left �� | Chief Complaint |

---

## Usage Examples

### Example 1: Quick Navigation

**Scenario**: You want to move from Study Remark to Chief Complaint

1. Click in the **Study Remark** field
2. Press **Alt+Down**
3. Focus moves to **Chief Complaint** field
4. Cursor is positioned at the end

### Example 2: Copying Template Text

**Scenario**: You have template text in Study Remark that you want to copy to Chief Complaint

1. In **Study Remark**, select the text you want to copy  
 (e.g., "Patient complains of chest pain")
2. Press **Alt+Down**
3. The selected text is appended to **Chief Complaint**
4. Focus moves to **Chief Complaint**
5. You can immediately continue typing

### Example 3: Moving Proofread Text Back

**Scenario**: You edited text in Chief Complaint Proofread and want to copy it back

1. In **Chief Complaint (Proofread)**, select the edited text
2. Press **Alt+Left**
3. The text is appended to **Chief Complaint** (original field)
4. Focus moves back to **Chief Complaint**

### Example 4: Combining Multiple Sections

**Scenario**: Building Chief Complaint from multiple sources

1. Select relevant text in **Study Remark**
2. Press **Alt+Down** �� Text copied to Chief Complaint
3. Select additional text in **Study Remark**
4. Press **Alt+Down** again �� Text appended with newline separator
5. Result: Chief Complaint contains both selections on separate lines

---

## Smart Text Appending

The feature intelligently handles text appending:

- **Empty target field**: Copied text is inserted directly
- **Non-empty target field**: A newline is added before the copied text
- **Multiple copies**: Each copy adds a new line for readability

**Example**:
```
Chief Complaint (before): "Patient complains of chest pain"
Copy from Study Remark: "Radiating to left arm"
Chief Complaint (after): 
    Patient complains of chest pain
    Radiating to left arm
```

---

## Tips and Best Practices

### ? DO:
- Use Alt+Arrow for quick field navigation during data entry
- Select specific phrases to copy between fields
- Use repeatedly to build comprehensive field content from multiple sources
- Combine with other keyboard shortcuts for efficient workflow

### ? DON'T:
- Don't worry about manually adding line breaks when appending
- Don't use mouse to switch between related fields
- Don't copy-paste manually when Alt+Arrow can do it faster

---

## Keyboard Shortcuts Summary

| Shortcut | Action |
|----------|--------|
| **Alt+Down** | Move/Copy to field below |
| **Alt+Up** | Move/Copy to field above |
| **Alt+Right** | Move/Copy to field on right |
| **Alt+Left** | Move/Copy to field on left |

**Remember**: If text is selected, it will be copied. If not, only focus moves.

---

## Troubleshooting

### The shortcut doesn't work
- Make sure you're pressing **Alt** key (not Ctrl or Shift)
- Ensure you're in a supported text field
- Check that the target field exists in the current view

### Text is copied but I didn't want to copy
- Make sure to **deselect** text before pressing Alt+Arrow
- Click once in the field to place cursor without selection

### I want to move without copying selected text
- Press **Escape** first to deselect
- Or click once to place cursor instead of selecting

---

## Future Enhancements

Planned improvements for future versions:
- Additional navigation mappings for more field pairs
- Support for EditorControl/MusmEditor fields
- Visual indicators showing available navigation targets
- User-configurable navigation mappings
- Cross-panel navigation support

---

## Related Features

- **Tab Navigation**: Use Tab/Shift+Tab for sequential field navigation
- **Mouse Navigation**: Click directly on any field
- **Global Phrase Completion**: Type to trigger phrase suggestions
- **Snippet Mode**: Use numbered snippet selection

---

## Need Help?

If you encounter issues or have suggestions:
1. Check this user guide for usage tips
2. Review the implementation documentation
3. Report issues with specific steps to reproduce

---

**Last Updated**: 2025-11-25  
**Document Version**: 1.0
