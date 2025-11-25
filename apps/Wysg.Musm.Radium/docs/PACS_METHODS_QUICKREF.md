# PACS Methods Quick Reference

## What Are PACS Methods?

PACS Methods are reusable automation actions that interact with your PACS system (e.g., INFINITT). They allow you to:
- Extract patient data from worklists
- Read report text fields
- Control PACS UI elements
- Automate repetitive workflows

## Where to Manage PACS Methods

**Location**: Tools ¡æ UI Spy ¡æ Custom Procedures section

## Quick Actions

### View All Methods
1. Open UI Spy
2. Look at "PACS Method" dropdown in Custom Procedures
3. See all available methods for current PACS profile

### Add New Method
1. Click **"+ Method"** button
2. Enter Display Name (shown in UI)
3. Enter Method Tag (used in code, alphanumeric + underscores)
4. Click OK

**Example**:
- Display Name: `Get Patient Phone Number`
- Method Tag: `GetPatientPhoneNumber`

### Edit Method
1. Select method from dropdown
2. Click **"Edit Method"** button
3. Modify name or tag
4. Click OK

**Note**: Built-in methods cannot be edited

### Delete Method
1. Select method from dropdown
2. Click **"Delete Method"** button
3. Confirm deletion

**Note**: Built-in methods cannot be deleted

## Configure Procedure Steps

After creating a custom method, you need to define what it does:

1. Select your method from dropdown
2. Click **"Add"** to add operations
3. Choose operation (GetText, SetValue, etc.)
4. Configure arguments (which UI element to interact with)
5. Click **"Set"** to test each step
6. Click **"Save"** to save all steps
7. Click **"Run"** to test complete procedure

## Built-In Methods (43 total)

You get these automatically for each PACS profile:

### Patient Data
- Get selected ID/name/sex/birth date/age
- Get patient remark
- Get patient number from banner

### Study Data
- Get selected study name/datetime/radiologist
- Get study remark
- Get report datetime

### Report Text
- Get current findings/conclusion
- Get findings/conclusion (variants)
- Clear report
- Send report

### Navigation
- Invoke open study
- Invoke open worklist
- Set focus on search results
- Set current/previous study views

### Validation
- Patient number match
- Study datetime match
- Worklist/report visibility checks

## Method Tag Rules

? **Valid Tags**
- Must start with a letter
- Can contain: letters, numbers, underscores
- Examples: `GetPatientAddress`, `Get_Study_ID`, `InvokeAction1`

? **Invalid Tags**
- Cannot start with number: `1GetPatient`
- Cannot contain spaces: `Get Patient`
- Cannot contain special chars: `Get-Patient`, `Get.Patient`

## Storage Location

Methods are saved per PACS profile:
```
%APPDATA%\Wysg.Musm\Radium\Pacs\{your_pacs_name}\pacs-methods.json
```

Each PACS profile has its own method list.

## Common Workflows

### Create Method for New Field
```
1. Find UI element with Pick button
2. Create bookmark for element
3. Create new PACS method (e.g., "Get Study Priority")
4. Add GetText operation with bookmark
5. Save and test
```

### Duplicate Method for Variation
```
1. Create new method with similar name
2. Load procedure from similar method
3. Modify operations as needed
4. Save as new method
```

### Organize Methods by Category
```
Use prefixes in method tags:
- Patient_*  (patient data methods)
- Study_*    (study data methods)
- Report_*   (report methods)
- Action_*   (UI action methods)
```

## Troubleshooting

### Method Not Appearing
- Check if PACS profile is selected correctly
- Try restarting UI Spy window
- Check Debug output for errors

### Cannot Edit/Delete Method
- This is a built-in method (protected)
- Create new custom method instead

### Procedure Not Saving
- Ensure method is selected in dropdown
- Check for empty operation rows
- Verify all arguments are configured

### Method Tag Already Exists
- Choose a unique tag name
- Or delete the existing method first

## Tips

?? **Naming Convention**: Use descriptive names that indicate what data is retrieved  
?? **Tag Format**: Use PascalCase for consistency (e.g., `GetPatientAddress`)  
?? **Testing**: Always test with "Run" button before using in automation  
?? **Documentation**: Add comments in procedure steps for complex logic  
?? **Backup**: Export/copy `pacs-methods.json` file before major changes  

## Integration with Automation

Custom methods work seamlessly with automation sequences:
- Defined in Settings ¡æ PACS tab ¡æ Automation section
- Use method tags in sequence strings
- Execute via MainViewModel automation logic

Example automation sequence:
```
GetPatientPhoneNumber,GetStudyPriority,PopulateReport
```

## Support

For detailed documentation, see:
- `FEATURE_2025-02-02_DynamicPacsMethods.md` (full implementation doc)
- UI Spy status bar for operation feedback
- Debug output window for detailed logs

---

**Last Updated**: 2025-02-02  
**Feature Status**: ? Production Ready
