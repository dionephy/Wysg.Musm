# Test Script - After Critical Fix - 2025-01-16

## Pre-Requisites
- ? Build succeeded (0 errors)
- ? Application runs without crashes
- ? Debug Output window open (View ¡æ Output ¡æ Debug)

---

## Test 1: GetCurrentPatientNumber (SHOULD NOW WORK)

### Steps
1. **Start application** (F5 in Visual Studio)
2. **Login** and open MainWindow
3. **Load a study** (any study)
4. **Verify "Current Study" label** shows patient number (e.g., "132508 / ¼­Á¤±æ")
5. **Open Settings** ¡æ Automation ¡æ Spy
6. **Select PACS Method:** "Get banner patient number"
7. **Add Step 1:**
   - Operation: `GetCurrentPatientNumber`
   - (No arguments needed)
8. **Click "Run Procedure"**

### Expected Result
**Output shows patient number:**
```
132508
```

### Debug Output (Check Output Window)
```
[SpyWindow][GetCurrentPatientNumber] Starting operation
[SpyWindow][GetCurrentPatientNumber] MainWindow found
[SpyWindow][GetCurrentPatientNumber] SUCCESS: PatientNumber='132508'
```

### ? If You See Empty
**Debug shows:**
```
[SpyWindow][GetCurrentPatientNumber] FAIL: MainWindow.DataContext is (null)
```

**Cause:** MainWindow not yet loaded or DataContext not set

**Solution:** 
- Wait for MainWindow to fully load before testing
- Verify Current Study label shows data
- Try loading another study

---

## Test 2: GetCurrentStudyDateTime (SHOULD NOW WORK)

### Steps
1. **Same as Test 1** but select "Get banner study date time" PACS method
2. **Add Step 1:**
   - Operation: `GetCurrentStudyDateTime`
3. **Click "Run Procedure"**

### Expected Result
**Output shows datetime:**
```
2025-10-17 00:36:54
```

### Debug Output
```
[SpyWindow][GetCurrentStudyDateTime] SUCCESS: StudyDateTime='2025-10-17 00:36:54'
```

---

## Test 3: GetStudyRemark Module (PARTIAL - PatientNumber Fixed, Parsing Not Yet Fixed)

### Steps
1. **Configure automation sequence** with "GetStudyRemark" module
2. **Run New Study automation** (or any sequence with GetStudyRemark)
3. **Check Debug Output window**

### Expected Result (AFTER CRITICAL FIX)

**Patient Number NOW WORKS:**
```
[ProcedureExecutor][GetCurrentPatientNumber] Starting direct read
[ProcedureExecutor][GetCurrentPatientNumber] MainWindow found
[ProcedureExecutor][GetCurrentPatientNumber] SUCCESS: PatientNumber='132508'  ?
```

**But Study Remark Still HTML (Issue #3 - User Must Fix Procedure):**
```
[ProcedureExecutor][ExecuteInternal] Step 2 result: preview='1 parts'  ?? Not splitting
[Automation][GetStudyRemark] Raw result: 'http://192.168...'  ?? Still HTML
```

### To Fix Study Remark Parsing

**Follow instructions in [CRITICAL_FIX_MAINVIEWMODEL_DI.md](CRITICAL_FIX_MAINVIEWMODEL_DI.md)**

1. Open Settings ¡æ Automation ¡æ Spy
2. Select "Get current study remark"
3. Fix Step 2 separator: `&pinfo=`, index: 1
4. Fix Step 3 separator: `&pname=`, index: 0
5. Test procedure ¡æ Verify output shows parsed text
6. Save procedure
7. Re-run automation

**After fixing procedure, you should see:**
```
[ProcedureExecutor][ExecuteInternal] Step 2 result: preview='2 parts'  ? Split worked!
[Automation][GetStudyRemark] Raw result: '026Y,M,CT Brain...'  ? Parsed!
```

---

## Test 4: Full Automation Sequence

### Steps
1. **Configure New Study automation** with all modules:
   - GetStudyRemark
   - GetPatientRemark
   - InvokeOpenStudy
   - SetCurrentStudyInMainScreen
   - SetPreviousStudyInSubScreen
2. **Run automation**
3. **Check Debug Output** for each step

### Expected Results

**? PatientNumber acquisition:**
```
[ProcedureExecutor][GetCurrentPatientNumber] SUCCESS: PatientNumber='132508'
```

**?? StudyRemark (if procedure not fixed yet):**
```
[Automation][GetStudyRemark] Raw result: 'http://192.168...'
```

**?? PatientRemark (if procedure not fixed yet):**
```
[Automation][GetPatientRemark] Raw result: '<HTML>...'
```

---

## Debugging Tips

### How to Read Debug Output

**Look for these keywords:**
- `SUCCESS:` - Operation worked correctly
- `FAIL:` - Operation failed (check reason after colon)
- `EXCEPTION:` - Error occurred (check error type and message)
- `preview='1 parts'` - Split operation didn't find separator
- `preview='2 parts'` (or more) - Split operation worked!

### Common Issues

#### Issue: "MainWindow.DataContext is (null)"
**Cause:** Application not fully initialized

**Solution:**
- Wait for MainWindow to appear
- Load a study before testing
- Check Current Study label shows data

#### Issue: "preview='1 parts'" for all Split operations
**Cause:** Separator string doesn't match content

**Solution:**
- Check separator exactly matches URL/HTML structure
- Look for typos (case-sensitive!)
- Test separator manually in SpyWindow

#### Issue: "FAIL: MainViewModel is null"
**Cause:** This should NOT happen after the fix

**If you see this:**
1. Verify build succeeded
2. Clean and rebuild project
3. Restart Visual Studio
4. Re-run application

---

## Success Criteria

### ? Test 1 PASS Criteria
- Output shows actual patient number (not empty)
- Debug shows "SUCCESS: PatientNumber='...'"
- No "Constructor START" messages in debug

### ? Test 2 PASS Criteria
- Output shows actual study datetime
- Debug shows "SUCCESS: StudyDateTime='...'"

### ?? Test 3 PARTIAL PASS Criteria
- PatientNumber acquisition works (SUCCESS message)
- StudyRemark still HTML (expected - user must fix procedure)
- No "Constructor START" messages

### ? After Fixing Procedures
- All Split operations show "2 parts" or more
- StudyRemark shows parsed text only
- PatientRemark shows deduplicated diagnosis list

---

## Reporting Issues

**If GetCurrentPatientNumber still returns empty after fix:**

Please provide:
1. **Debug Output** (copy all `[ProcedureExecutor][GetCurrentPatientNumber]` lines)
2. **Current Study label** text (screenshot)
3. **MainWindow.DataContext type** (from debug output)
4. **Steps to reproduce**

**If Split operations still fail after fixing separators:**

Please provide:
1. **Debug Output** showing Step results
2. **Raw URL/HTML** from Step 1 output
3. **Separator strings** used in procedure
4. **Expected vs Actual** output

---

*Test Script Version: 1.0*  
*Date: 2025-01-16*  
*For Build: Critical Fix - MainViewModel DI*  
*Author: GitHub Copilot*
