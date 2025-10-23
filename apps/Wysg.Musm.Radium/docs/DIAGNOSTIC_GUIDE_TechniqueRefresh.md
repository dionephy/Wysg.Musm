# Diagnostic Guide: Technique Refresh Not Working

**Issue**: Study techniques field doesn't refresh when Studyname LOINC Parts window closes  
**Date**: 2025-01-23  
**Status**: ?? Diagnostic Mode Enabled

---

## Quick Diagnosis Steps

### Step 1: Enable Debug Output
1. Open Visual Studio
2. Go to **View** ¡æ **Output**
3. Select **Debug** from the dropdown
4. Run the application in Debug mode (F5)

### Step 2: Test the Refresh
1. Open a study with a known studyname (e.g., "CT CHEST")
2. Note the current `study_techniques` value
3. Open **Studyname LOINC Parts** window
4. **Don't make any changes** (just testing the close event)
5. Close the window
6. Check the **Output** window for debug messages

---

## Expected Debug Output

When the window closes, you should see this sequence:

```
[StudynameLoincWindow] Window closed - refreshing study_techniques in MainViewModel
[MainViewModel.Techniques] RefreshStudyTechniqueFromDefaultAsync - START
[MainViewModel.Techniques] Current StudyName: 'CT CHEST'
[MainViewModel.Techniques] Getting studyname ID from repository...
[PG][Open#xxx][BEGIN][Primary] ...
[Repo][Call#xxx] GetStudynameIdByNameAsync 'CT CHEST' START
[Repo][Call#xxx] GetStudynameIdByNameAsync OK Id=12 Elapsed=15ms
[MainViewModel.Techniques] Studyname ID: 12
[MainViewModel.Techniques] Getting default combination...
[MainViewModel.Techniques] Default combination ID: 5, Display: 'CT + contrast'
[MainViewModel.Techniques] Getting combination items...
[MainViewModel.Techniques] Retrieved 2 combination items
[MainViewModel.Techniques]   Item: Prefix='', Tech='CT', Suffix='', Seq=1
[MainViewModel.Techniques]   Item: Prefix='', Tech='contrast', Suffix='', Seq=2
[MainViewModel.Techniques] Grouped display: 'CT + contrast'
[MainViewModel.Techniques] StudyTechniques updated: 'CT' -> 'CT + contrast'
[MainViewModel.Techniques] RefreshStudyTechniqueFromDefaultAsync - COMPLETED
[StudynameLoincWindow] Study_techniques refresh completed
```

---

## Common Issues and Solutions

### Issue 1: No Debug Output at All

**Symptom**: Nothing appears in the Output window when closing the window

**Possible Causes**:
1. Debug output not enabled
2. Event handler not attached
3. Application not running in Debug mode

**Solutions**:
1. ? **Enable Debug Output**:
   - Visual Studio ¡æ **Tools** ¡æ **Options**
   - **Debugging** ¡æ **General**
   - Ensure "Redirect all Output Window text to the Immediate Window" is **unchecked**

2. ? **Verify Event Handler**:
   - Check `StudynameLoincWindow.xaml.cs` constructor
   - Should contain: `Closed += OnWindowClosed;`

3. ? **Run in Debug Mode**:
   - Press **F5** (not Ctrl+F5)
   - Ensure debugger is attached

---

### Issue 2: "StudyName is empty - returning"

**Symptom**: 
```
[MainViewModel.Techniques] StudyName is empty - returning
```

**Cause**: No study loaded in MainViewModel

**Solution**:
1. ? Open a study first before testing
2. ? Verify `MainViewModel.StudyName` property is set
3. ? Check that study metadata was loaded correctly

**Verification**:
- Main window should show patient/study information
- Study name should be visible in the UI

---

### Issue 3: "No studyname ID found"

**Symptom**:
```
[MainViewModel.Techniques] No studyname ID found for 'CT CHEST' - returning
```

**Cause**: Studyname doesn't exist in database

**Solutions**:
1. ? **Create Studyname**:
   - Open Studyname LOINC Parts window
   - Click "Add Studyname"
   - Enter the studyname (e.g., "CT CHEST")
   - Save

2. ? **Check Database**:
   ```sql
   SELECT id, studyname FROM med.rad_studyname WHERE studyname = 'CT CHEST';
   ```

3. ? **Verify Tenant Context**:
   - Ensure `ITenantContext.TenantId` is correct
   - Check if studyname belongs to different tenant

---

### Issue 4: "No default combination found"

**Symptom**:
```
[MainViewModel.Techniques] No default combination found for studyname ID 12 - returning
```

**Cause**: Studyname has no default technique combination set

**Solutions**:
1. ? **Set Default Combination**:
   - Open Studyname LOINC Parts window
   - Select the studyname
   - Click "Manage Default Technique"
   - Build a combination
   - Click "Save as New Combination"
   - Click "Set Selected As Default"

2. ? **Verify in Database**:
   ```sql
   SELECT * FROM med.rad_studyname_technique_combination 
   WHERE studyname_id = 12 AND is_default = true;
   ```

3. ? **Check Multiple Defaults** (should be only one):
   ```sql
   SELECT COUNT(*) FROM med.rad_studyname_technique_combination 
   WHERE studyname_id = 12 AND is_default = true;
   ```
   - Should return **1**
   - If **0**: No default set
   - If **>1**: Data integrity issue (fix by setting one as default)

---

### Issue 5: "ITechniqueRepository is null"

**Symptom**:
```
[MainViewModel.Techniques] ITechniqueRepository is null - returning
```

**Cause**: Dependency injection not configured correctly

**Solutions**:
1. ? **Check DI Registration**:
   - Open `App.xaml.cs`
   - Verify `ITechniqueRepository` is registered in services
   - Example:
     ```csharp
     services.AddScoped<ITechniqueRepository, TechniqueRepository>();
     ```

2. ? **Restart Application**:
   - DI container changes require restart
   - Rebuild solution (Ctrl+Shift+B)
   - Run again (F5)

---

### Issue 6: Database Connection Error

**Symptom**:
```
[MainViewModel.Techniques] EXCEPTION in RefreshStudyTechniqueFromDefaultAsync:
[MainViewModel.Techniques]   Type: NpgsqlException
[MainViewModel.Techniques]   Message: Connection refused
```

**Cause**: PostgreSQL database not accessible

**Solutions**:
1. ? **Verify Database Running**:
   - Check PostgreSQL service is running
   - Default: `localhost:5432`

2. ? **Check Connection String**:
   - Open Settings window
   - Verify "Local Connection String" is correct
   - Example: `Host=127.0.0.1;Port=5432;Database=wysg_dev;Username=postgres;Password=***`

3. ? **Test Connection**:
   - Use pgAdmin or psql to connect manually
   - Verify you can query `med.rad_studyname` table

---

### Issue 7: "Grouped display is empty"

**Symptom**:
```
[MainViewModel.Techniques] Grouped display: ''
[MainViewModel.Techniques] Grouped display is empty - not updating StudyTechniques
```

**Cause**: Combination items retrieved but formatter returns empty string

**Solutions**:
1. ? **Check Combination Items**:
   - Look at the debug output for retrieved items
   - Should show at least one item with a non-empty `Tech` value

2. ? **Verify TechniqueFormatter**:
   - Check `TechniqueFormatter.BuildGroupedDisplay` implementation
   - Ensure it handles empty prefix/suffix correctly

3. ? **Database Verification**:
   ```sql
   SELECT i.sequence_order, tp.prefix_text, tt.tech_text, ts.suffix_text
   FROM med.rad_technique_combination_item i
   JOIN med.rad_technique t ON t.id = i.technique_id
   LEFT JOIN med.rad_technique_prefix tp ON tp.id = t.prefix_id
   JOIN med.rad_technique_tech tt ON tt.id = t.tech_id
   LEFT JOIN med.rad_technique_suffix ts ON ts.id = t.suffix_id
   WHERE i.combination_id = 5
   ORDER BY i.sequence_order;
   ```

---

## Advanced Diagnostics

### Check Event Handler Attachment

**Verify in Code**:
```csharp
// In StudynameLoincWindow.xaml.cs constructor
public StudynameLoincWindow(StudynameLoincViewModel vm)
{
    InitializeComponent();
    DataContext = vm;
    
    // This line MUST be present
    Closed += OnWindowClosed;  // ¡ç Check this exists
}
```

**Test at Runtime**:
1. Set breakpoint in `OnWindowClosed` method
2. Close the window
3. Breakpoint should hit
4. If not hit ¡æ Event not attached

---

### Check MainViewModel Instance

**Verify Singleton**:
```csharp
private async void OnWindowClosed(object? sender, EventArgs e)
{
    var app = (App)Application.Current;
    var mainVm = app.Services.GetRequiredService<MainViewModel>();
    
    // Add this debug line to verify instance
    Debug.WriteLine($"[StudynameLoincWindow] MainViewModel instance: {mainVm.GetHashCode()}");
    Debug.WriteLine($"[StudynameLoincWindow] Current StudyName: '{mainVm.StudyName}'");
}
```

**Expected**:
- Same hashcode every time (singleton)
- StudyName should match what's displayed in main window

---

### Monitor Property Changes

**Add Property Change Listener**:
```csharp
// In MainViewModel or test code
PropertyChanged += (s, e) =>
{
    if (e.PropertyName == nameof(StudyTechniques))
    {
        Debug.WriteLine($"[PropertyChanged] StudyTechniques changed to: '{StudyTechniques}'");
    }
};
```

**Expected**:
- PropertyChanged event fires when StudyTechniques is set
- UI should update automatically via binding

---

## Performance Checks

### Measure Refresh Time

Add timing to the refresh method:

```csharp
var sw = System.Diagnostics.Stopwatch.StartNew();
await mainVm.RefreshStudyTechniqueFromDefaultAsync();
sw.Stop();
Debug.WriteLine($"[StudynameLoincWindow] Refresh took {sw.ElapsedMilliseconds}ms");
```

**Expected**: 15-100ms (depending on database latency)  
**Concern**: >500ms indicates network/database issue

---

## Logging Levels

### Minimal Logging (Production)
- Only errors logged
- Silent success

### Standard Logging (Current)
- Key steps logged
- Errors with details

### Verbose Logging (Diagnostic Mode)
- Every step logged
- All values logged
- Database queries logged

**Current Mode**: Standard (can be enhanced if needed)

---

## Quick Test Script

**Copy-paste this into Debug Output window after closing the window:**

1. If you see "RefreshStudyTechniqueFromDefaultAsync - START" ¡æ Event handler working ?
2. If you see "StudyName is empty" ¡æ No study loaded ?
3. If you see "No studyname ID found" ¡æ Studyname not in database ?
4. If you see "No default combination found" ¡æ No default set ?
5. If you see "StudyTechniques updated" ¡æ Success! ?

---

## Support Checklist

Before reporting an issue, verify:

- [ ] Debug output enabled
- [ ] Running in Debug mode (F5)
- [ ] Study loaded in main window
- [ ] Studyname exists in database
- [ ] Default technique combination set
- [ ] Database connection working
- [ ] Event handler attached
- [ ] MainViewModel instance correct
- [ ] Output window shows expected debug messages

---

## Contact Information

**Issue Persistence**:
- If problem persists after following this guide
- Collect full debug output from Output window
- Note exact reproduction steps
- Include database state (studyname, combination details)

**Emergency Rollback**:
- Remove `Closed += OnWindowClosed;` line
- Remove `OnWindowClosed` method
- Rebuild and run

---

**Last Updated**: 2025-01-23  
**Diagnostic Version**: 1.0  
**Status**: ?? Active Diagnostics
