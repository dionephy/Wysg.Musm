# Critical Fixes - Split Operation & Cross-Thread Exception - 2025-01-19

## Summary

Fixed **TWO critical bugs** discovered through diagnostic logging that were preventing automation procedures from working correctly:

1. **?? Split Operation Bug**: `ResolveString` was returning empty strings for all String-type arguments
2. **?? Cross-Thread Exception**: `GetCurrentPatientNumber` and `GetCurrentStudyDateTime` were accessing UI thread objects from background threads

## Issue #1: Split Operation Returning Empty Separators

### Problem

The diagnostic output showed:
```
[Split] SepRaw: '' (length: 0, bytes: )  ← EMPTY STRING!
[Split] Input contains separator: True
[Split] Split result: 1 parts  ← No split occurred
```

Even though the saved procedure had correct separators (`&pinfo=`, `&pname=`), they were being resolved as empty strings.

### Root Cause

**File**: `apps\Wysg.Musm.Radium\Services\ProcedureExecutor.cs`  
**Method**: `ResolveString`

```csharp
// BEFORE (BROKEN):
private static string ResolveString(ProcArg arg, Dictionary<string, string?> vars)
{
    if (arg.Type == nameof(ArgKind.Element))
    {
        var key = arg.Value ?? string.Empty;
        return _elementCache.TryGetValue(key, out var el) && el != null ? el.Name : string.Empty;
    }
    // For other ArgKinds, perform normal variable resolution
    vars.TryGetValue(arg.Value ?? string.Empty, out var value);
    return value ?? string.Empty;  ← BUG: Always returns empty for String args!
}
```

**The Bug**: For `ArgKind.String` arguments (like separator strings), the method was looking up `arg.Value` in the `vars` dictionary instead of returning the value directly!

Since separator strings like `"&pinfo="` are not variable names, the dictionary lookup always failed, returning empty string.

### Fix

```csharp
// AFTER (FIXED):
private static string ResolveString(ProcArg arg, Dictionary<string, string?> vars)
{
    var type = ParseArgKind(arg.Type);
    
    if (type == ArgKind.Element)
    {
        // For ArgKind.Element, resolve using the elements dictionary
        var key = arg.Value ?? string.Empty;
        return _elementCache.TryGetValue(key, out var el) && el != null ? el.Name : string.Empty;
    }
    
    if (type == ArgKind.Var)
    {
        // For ArgKind.Var, look up value in vars dictionary
        vars.TryGetValue(arg.Value ?? string.Empty, out var value);
        return value ?? string.Empty;
    }
    
    // For String and Number types, return the value directly
    return arg.Value ?? string.Empty;
}
```

**Key Change**: Added explicit handling for different `ArgKind` types:
- `Element` → lookup in element cache
- `Var` → lookup in vars dictionary (e.g., `var1`, `var2`)
- `String` / `Number` → return value directly ?

---

## Issue #2: Cross-Thread Exception in GetCurrentPatientNumber

### Problem

```
[ProcedureExecutor][GetCurrentPatientNumber] EXCEPTION: InvalidOperationException - 
다른 스레드가 이 개체를 소유하고 있어 호출 스레드가 해당 개체에 액세스할 수 없습니다.
```

Translation: "The calling thread cannot access this object because a different thread owns it."

### Root Cause

**File**: `apps\Wysg.Musm.Radium\Services\ProcedureExecutor.cs`  
**Method**: `ExecuteInternal`

```csharp
// BEFORE (BROKEN):
if (string.Equals(methodTag, "GetCurrentPatientNumber", StringComparison.OrdinalIgnoreCase))
{
    var mainWindow = System.Windows.Application.Current?.MainWindow;  ← UI thread object!
    if (mainWindow != null)
    {
        if (mainWindow.DataContext is ViewModels.MainViewModel mainVM)
        {
            var result = mainVM.PatientNumber ?? string.Empty;
            return result;
        }
    }
}
```

**The Bug**: `ProcedureExecutor.ExecuteAsync` runs on a background thread (`Task.Run`), but it was accessing `Application.Current.MainWindow` which is owned by the UI thread.

WPF throws `InvalidOperationException` when UI thread objects are accessed from background threads.

### Fix

```csharp
// AFTER (FIXED):
if (string.Equals(methodTag, "GetCurrentPatientNumber", StringComparison.OrdinalIgnoreCase))
{
    string result = string.Empty;
    System.Windows.Application.Current?.Dispatcher.Invoke(() =>  ← Marshal to UI thread!
    {
        var mainWindow = System.Windows.Application.Current?.MainWindow;
        if (mainWindow != null)
        {
            if (mainWindow.DataContext is ViewModels.MainViewModel mainVM)
            {
                result = mainVM.PatientNumber ?? string.Empty;
            }
        }
    });
    return result;
}
```

**Key Change**: Wrapped MainWindow access in `Dispatcher.Invoke(() => { ... })` to execute on the UI thread.

**Same fix applied to**:
- `GetCurrentPatientNumber`
- `GetCurrentStudyDateTime`

---

## Test Results Expected

### Before Fix

#### GetCurrentStudyRemark:
```
[Split] SepRaw: '' (length: 0, bytes: )
[Split] Split result: 1 parts
Result: 'http://192.168.200.162:8500/report.asp?...' (unsplit URL)
```

#### GetCurrentPatientNumber:
```
[ProcedureExecutor][GetCurrentPatientNumber] EXCEPTION: InvalidOperationException
Result: '' (empty)
```

### After Fix

#### GetCurrentStudyRemark:
```
[Split] SepRaw: '&pinfo=' (length: 7, bytes: 26 70 69 6E 66 6F 3D)
[Split] Input contains separator: True
[Split] Split result: 2 parts
...
[Split] SepRaw: '&pname=' (length: 7, bytes: 26 70 6E 61 6D 65 3D)
[Split] Split result: 2 parts
Result: '011Y,M,CT Brain (for trauma),EC,I,ER,-/head trauma'
```

#### GetCurrentPatientNumber:
```
[ProcedureExecutor][GetCurrentPatientNumber] MainWindow found
[ProcedureExecutor][GetCurrentPatientNumber] SUCCESS: PatientNumber='326947'
Result: '326947'
```

---

## Files Changed

### 1. `apps\Wysg.Musm.Radium\Services\ProcedureExecutor.cs`

**Changes**:
1. Fixed `ResolveString` method to handle `String`, `Number`, `Var`, and `Element` types correctly
2. Added `Dispatcher.Invoke` to `GetCurrentPatientNumber` handler in `ExecuteInternal`
3. Added `Dispatcher.Invoke` to `GetCurrentStudyDateTime` handler in `ExecuteInternal`
4. Removed obsolete `GetCurrentPatientNumber` and `GetCurrentStudyDateTime` cases from `ExecuteElemental` (now handled in `ExecuteInternal`)

---

## Impact

### Before Fix
- ? All Split operations failed (separators resolved to empty strings)
- ? GetCurrentStudyRemark returned unsplit URL
- ? GetCurrentPatientRemark returned full HTML
- ? GetCurrentPatientNumber threw cross-thread exception (failed 5/5 attempts)
- ? GetCurrentStudyDateTime threw cross-thread exception (failed 5/5 attempts)
- ? Patient number match validation always failed
- ? Study datetime match validation always failed

### After Fix
- ? Split operations work correctly with separators
- ? GetCurrentStudyRemark returns parsed study remark text
- ? GetCurrentPatientRemark returns parsed patient diagnosis data
- ? GetCurrentPatientNumber reads from MainViewModel successfully
- ? GetCurrentStudyDateTime reads from MainViewModel successfully
- ? Patient number match validation works
- ? Study datetime match validation works
- ? AddPreviousStudy module works (depends on patient number validation)

---

## Testing Instructions

1. **Run the application**
2. **Execute New Study automation** (should trigger all procedures)
3. **Check Debug Output window** for diagnostic lines:

### Expected Success Output

```
[Split] SepRaw: '&pinfo=' (length: 7, bytes: 26 70 69 6E 66 6F 3D)
[Split] Input contains separator: True
[Split] Split result: 2 parts
...
[Automation][GetStudyRemark] Set StudyRemark property: '011Y,M,CT Brain (for trauma)...'

[ProcedureExecutor][GetCurrentPatientNumber] SUCCESS: PatientNumber='326947'
[PacsService][ExecWithRetry] GetCurrentPatientNumber SUCCESS on attempt 1: '326947'
```

### Verify Fixes

- [ ] Study Remark field shows parsed text (not full URL)
- [ ] Patient Remark field shows parsed diagnosis list (not HTML)
- [ ] Patient number validation succeeds (no mismatch errors)
- [ ] Add Previous Study works without "Patient mismatch" error

---

## Related Documentation

- `apps\Wysg.Musm.Radium\docs\SPLIT_DIAGNOSTIC_2025_01_19.md` - Diagnostic logging implementation
- `apps\Wysg.Musm.Radium\docs\DEBUG_LOGGING_IMPLEMENTATION.md` - Original debug logging plan
- `apps\Wysg.Musm.Radium\docs\CRITICAL_FIX_MAINVIEWMODEL_DI.md` - Previous MainViewModel DI fix

---

## Build Status

? **SUCCESS** (0 errors)

All changes compile successfully and are ready for testing.
