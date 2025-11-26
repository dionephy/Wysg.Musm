# Custom Modules - Quick Reference

## What is it?
Create reusable automation modules that combine operations (Run, Set, Abort If) with Custom Procedures.

## How to Create

1. **Open**: Automation Window ¡æ Automation Tab
2. **Click**: "Create Module" button in Custom Modules pane
3. **Select**:
   - Module Type: Run / Set / Abort if (displays cleanly)
   - Property (if Set): Select from 14 options
   - Custom Procedure: Select from dropdown
4. **Name**: Auto-generated (read-only display)
   - Run: "Run [Procedure]"
   - Set: "Set [Property] to [Procedure]"
   - Abort if: "Abort if [Procedure]"
5. **Save**: Module created and available for use

## UI Improvements (Latest)
- ? Module Type displays: "Run", "Set", "Abort if" (not "ComboBoxItem: ...")
- ? Module Name is auto-generated (no manual input needed)
- ? Name shown in styled, read-only panel
- ? Cannot save with incomplete selections

## Auto-Naming Examples

### Run Type
```
Procedure: "GetPatientName"
Auto-name: "Run GetPatientName"
Display: Read-only panel (cannot edit)
```

### Set Type
```
Property: "Current Patient Name"
Procedure: "Get current patient name" 
Auto-name: "Set Current Patient Name to Get current patient name"
Display: Read-only panel (cannot edit)
```

### Abort If Type
```
Procedure: "PatientNumberMatch"
Auto-name: "Abort if PatientNumberMatch"
Display: Read-only panel (cannot edit)
```

**Note**: Module name is auto-generated and read-only (cannot be edited).

## Module Types

### Run
- Simply executes a Custom Procedure
- Result ignored
- Example: "Run Get Patient Name"

### Set
- Executes procedure and stores result in property
- 14 property options (see below)
- Example: "Set Current Patient Name to Get current patient name"

### Abort If
- Executes procedure and checks result
- If result is true/non-empty, aborts automation sequence
- Example: "Abort if Patient Number Not Match"

## Available Properties (Set Type)

**Current Study** (direct MainViewModel properties):
1. Current Patient Name
2. Current Patient Number
3. Current Patient Age
4. Current Patient Sex
5. Current Study Studyname
6. Current Study Datetime
7. Current Study Remark
8. Current Patient Remark

**Previous Study** (temporary storage):
9. Previous Study Studyname
10. Previous Study Datetime
11. Previous Study Report Datetime
12. Previous Study Report Reporter
13. Previous Study Report Header and Findings
14. Previous Study Report Conclusion

## How to Use

1. **Find Module**: Custom Modules pane or Available Modules pane
2. **Drag**: Module to any automation pane (New Study, Add Study, etc.)
3. **Drop**: Module appears in sequence
4. **Save**: Automation sequence saved
5. **Execute**: Module runs as part of automation

## Storage Location
`%AppData%\Wysg.Musm\Radium\custom-modules.json`

## Examples

### Example 1: Get and Store Patient Name
```
Type: Set
Property: Current Patient Name
Procedure: Get current patient name
Result: Patient name stored in MainViewModel.PatientName
```

### Example 2: Abort on Mismatch
```
Type: Abort if
Procedure: Check patient number match
Result: If false, continues; if true, aborts sequence
```

### Example 3: Simple Execution
```
Type: Run
Procedure: Send notification
Result: Procedure runs, result ignored
```

## Tips

- **Descriptive Names**: Use clear, descriptive module names
- **Test First**: Test Custom Procedures before creating modules
- **Check Properties**: Verify property mappings before saving
- **Module Order**: Order matters in automation sequences
- **Error Handling**: Abort If stops entire sequence on true

## Limitations

- No editing after creation (delete and recreate)
- No parameters (procedures run with no arguments)
- No conditional logic (always runs in sequence)
- Previous study properties temporary (not persisted)

## Troubleshooting

**Module not appearing?**
- Check Custom Modules pane
- Check Available Modules pane
- Restart application if needed

**Procedure not in dropdown?**
- Create procedure in AutomationWindow ?? Custom Procedures first
- Refresh by reopening CreateModuleWindow

**Module not executing?**
- Check Debug output for error messages
- Verify procedure name matches exactly
- Verify procedure is defined for current PACS profile

**Property not updating?**
- Verify module type is "Set"
- Check property spelling
- Review Debug output for SetPropertyValue messages

## Quick Commands

- **Create**: Click "Create Module" in Custom Modules pane
- **Use**: Drag from Custom Modules or Available Modules
- **Save**: Click "Save Automation" after dragging
- **Execute**: Run automation sequence (New Study, Add Study, etc.)

---

**For detailed documentation, see:**
- Implementation Guide: `CUSTOM_MODULES_IMPLEMENTATION_GUIDE.md`
- Implementation Complete: `CUSTOM_MODULES_IMPLEMENTATION_COMPLETE.md`

**Build Status**: ? SUCCESS (0 errors)
