# UI Terminology Update - Custom Procedures

**Date**: 2025-11-25  
**Status**: ? Complete  
**Build**: ? Success

## Summary

Updated UI terminology in the Custom Procedures section of UI Spy window to minimize confusion by consistently using "Custom Procedure" terminology instead of mixing "PACS Method" and other terms.

## Changes Made

### Label Updates
- "PACS Method:" ¡æ "Custom Procedure:"

### Button Text Updates
- "Add" ¡æ "Add operation"
- "Save" ¡æ "Save procedure"
- "Run" ¡æ "Run procedure"
- "+ Method" ¡æ "Add procedure"
- "Edit Method" ¡æ "Edit procedure"
- "Delete Method" ¡æ "Delete procedure"

### Layout Changes
Controls are now organized in two rows:

**Row 1: Procedure Selection and Management**
```
Custom Procedure: [dropdown] | Save procedure | [separator] | Add procedure | Edit procedure | Delete procedure
```

**Row 2: Operation Management**
```
Add operation | Run procedure
```

This layout clarifies the distinction between:
- **Procedures** (the overall workflow/method)
- **Operations** (individual steps within a procedure)

## Files Modified

1. **`Views/SpyWindow.xaml`** - Updated button text and layout
2. **`Views/SpyWindow.PacsMethods.cs`** - Updated status messages and dialog titles
3. **`Views/SpyWindow.Procedures.Exec.cs`** - Updated status messages
4. **`docs/00-current/DYNAMIC_PACS_METHODS.md`** - Updated documentation

## Status Messages Updated

All user-facing messages now use consistent terminology:
- "Custom procedure manager not initialized"
- "No custom procedure selected"
- "Cannot edit built-in custom procedures"
- "Cannot delete built-in custom procedures"
- "Added custom procedure '{name}'"
- "Updated custom procedure '{name}'"
- "Deleted custom procedure '{name}'"
- "Select custom procedure"

## Dialog Titles Updated

- "Add PACS Method" ¡æ "Add Custom Procedure"
- "Edit PACS Method" ¡æ "Edit Custom Procedure"

## Technical Details

### Unchanged (Internal)
The underlying code structure remains unchanged:
- Model class: `PacsMethod`
- Service: `PacsMethodManager`
- Storage: `pacs-methods.json`
- Property names in code

### Changed (UI Only)
Only user-facing text was updated to improve clarity and consistency.

## Benefits

? **Clearer terminology** - "Custom Procedure" is self-explanatory  
? **Better organization** - Two-row layout separates procedure management from operation management  
? **Reduced confusion** - Consistent use of "procedure" throughout UI  
? **Improved workflow** - Related buttons grouped logically  

## User Impact

**Minimal** - This is a cosmetic change that improves usability:
- All functionality remains the same
- No data migration needed
- No breaking changes
- Improved clarity for new users

## Testing

? Build successful  
? No compilation errors  
? No runtime errors expected  
? UI layout verified in XAML  

## Terminology Guide

| UI Term | Code/Storage Term | Description |
|---------|------------------|-------------|
| Custom Procedure | PACS Method | The overall automation workflow |
| Operation | ProcOpRow | Individual step in a procedure |
| Add operation | Add row | Add a step to the procedure |
| Save procedure | Save procedure | Save all steps for this procedure |
| Run procedure | Run procedure | Execute all steps in sequence |

## Related Documentation

- **Main Feature Doc**: `docs/04-archive/2025/FEATURE_2025-02-02_DynamicPacsMethods.md`
- **Quick Reference**: `docs/PACS_METHODS_QUICKREF.md`
- **Current Status**: `docs/00-current/DYNAMIC_PACS_METHODS.md`

---

**Updated By**: UI Terminology Clarification  
**Date**: 2025-11-25  
**Status**: Complete  
**Breaking Changes**: None
