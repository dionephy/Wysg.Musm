# Dynamic PACS Methods Implementation

**Date**: 2025-02-02  
**Updated**: 2025-11-25 (UI terminology clarification)  
**Status**: ? Implemented and Ready

## Quick Summary

Custom Procedures in the UI Spy window are now dynamic. You can add, edit, and delete custom procedures without modifying code.

## What You Can Do Now

### In UI Spy ¡æ Custom Procedures

**Add New Custom Procedure**
- Click "Add procedure" button
- Enter display name and method tag
- Procedure becomes available immediately

**Edit Custom Procedure**
- Select procedure from dropdown
- Click "Edit procedure"
- Modify name or tag
- Built-in procedures cannot be edited

**Delete Custom Procedure**
- Select procedure from dropdown
- Click "Delete procedure"
- Confirm deletion
- Built-in procedures cannot be deleted

## UI Layout

The Custom Procedures section has two rows of controls:

**Row 1: Procedure Selection and Management**
- Custom Procedure: [dropdown]
- Save procedure
- [separator]
- Add procedure | Edit procedure | Delete procedure

**Row 2: Operation Management**
- Add operation | Run procedure

## Storage Location

Procedures are saved per PACS profile:
```
%APPDATA%\Wysg.Musm\Radium\Pacs\{pacsKey}\pacs-methods.json
```

## Built-In Methods

43 built-in procedures are automatically available:
- Patient data extraction (ID, name, sex, birth date, age)
- Study information (name, datetime, radiologist, remarks)
- Report text (findings, conclusion)
- UI actions (open study, send report, clear report)
- Validation (patient number match, study datetime match)
- And more...

## Documentation

**Full Details**: `docs/04-archive/2025/FEATURE_2025-02-02_DynamicPacsMethods.md`  
**Quick Reference**: `docs/PACS_METHODS_QUICKREF.md`  
**Summary**: `docs/DYNAMIC_PACS_METHODS_SUMMARY.md`

## Benefits

? No code changes for custom procedures  
? Per-PACS profile configuration  
? Built-in procedures protected  
? Easy to maintain and organize  

## Terminology Note

The UI uses "Custom Procedure" terminology to minimize confusion, while the underlying system still uses "PACS Method" in code and storage. Both terms refer to the same concept: reusable automation workflows for PACS interaction.

---

**Implementation**: Complete  
**Build Status**: ? Success  
**Ready for Use**: Yes
