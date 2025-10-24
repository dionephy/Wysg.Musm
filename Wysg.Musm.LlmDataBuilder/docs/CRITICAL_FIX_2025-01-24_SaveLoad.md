# CRITICAL FIX: Records Emptying On Save (v1.3.5)

## ?? CRITICAL BUG FOUND AND FIXED

**Date**: 2025-01-24  
**Version**: 1.3.5  
**Severity**: CRITICAL - Data Loss Risk  
**Status**: ? FIXED

## Problem Description

When saving new records, **all previously saved records became empty** except the most recently saved one.

### What Users Experienced

```
Step 1: Save record with Input="test1", Output="test1" ¡æ ? Saved OK
Step 2: Save record with Input="test2", Output="test2" ¡æ ? Record 1 becomes EMPTY!
Step 3: Save record with Input="test3", Output="test3" ¡æ ? Records 1,2 become EMPTY!

Result in data.json:
[
  { "input": "", "output": "", ... },  ¡ç Lost data!
  { "input": "", "output": "", ... },  ¡ç Lost data!
  { "input": "test3", "output": "test3", ... }  ¡ç Only last record OK
]
```

## Root Cause

**Missing JsonSerializerOptions in BtnSave_Click**

The save method loads existing records before appending the new one, but it was using **default deserialization** which doesn't match the **camelCase JSON format** we save to.

### The Bug

```csharp
// BUGGY CODE (v1.3.4 and earlier)
if (File.Exists(dataPath))
{
    string existingJson = File.ReadAllText(dataPath);
    records = JsonSerializer.Deserialize<List<LlmDataRecord>>(existingJson);
    //                                                           ^^^ NO OPTIONS!
    // Result: Creates empty LlmDataRecord objects because:
    // - JSON has "input", "output" (camelCase)
    // - C# expects "Input", "Output" (PascalCase)
    // - Mismatch = defaults to empty strings
}
```

### What Happens

1. **Save Record 1**:
   ```json
   [{ "input": "test1", "output": "test1", "appliedPromptNumbers": ["1"] }]
   ```
   ? Works correctly

2. **Save Record 2**:
   - Load existing file ¡æ Deserialize WITHOUT options
   - JSON: `{"input": "test1"}` ¡æ C# object: `Input = "" ` (EMPTY!)
   - Append Record 2
   - Save: `[{empty}, {record2}]`
   ? Record 1 lost!

3. **Save Record 3**:
   - Load existing file ¡æ Deserialize WITHOUT options
   - Records 1,2 become empty
   - Append Record 3
   - Save: `[{empty}, {empty}, {record3}]`
   ? Records 1,2 lost!

## The Fix

**Added JsonSerializerOptions to all deserialization calls**

```csharp
// FIXED CODE (v1.3.5)
if (File.Exists(dataPath))
{
    string existingJson = File.ReadAllText(dataPath);
    
    // ? Added proper deserialization options
    var deserializeOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
    
    records = JsonSerializer.Deserialize<List<LlmDataRecord>>(
        existingJson, 
        deserializeOptions  // ¡ç Now matches save format!
    ) ?? new List<LlmDataRecord>();
}
```

## Files Fixed

? **MainWindow.xaml.cs** - 3 methods updated:
1. `BtnSave_Click()` - Load existing records properly
2. `UpdateRecordCount()` - Count records properly
3. `CleanupBlankRecords()` - Clean records properly

## How to Apply Fix

### For Developers

1. **Pull latest code** (v1.3.5)
2. **Rebuild solution**
3. **Restart application**
4. **Test**: Save multiple records ¡æ All should persist

### For Users

1. **Close application completely**
2. **Download/install v1.3.5**
3. **Use "Cleanup Blank Records" button** to remove empties
4. **Re-enter any lost data**
5. **Future saves will work correctly**

## Testing Verification

### Test 1: Multiple Saves
```
1. Delete data.json
2. Save: Input="A", Output="A"
3. Save: Input="B", Output="B"
4. Save: Input="C", Output="C"
5. Open data.json

Expected:
[
  { "input": "A", "output": "A", ... },  ?
  { "input": "B", "output": "B", ... },  ?
  { "input": "C", "output": "C", ... }   ?
]
```

### Test 2: Record Count
```
1. Save 3 records
2. Check status bar

Expected: "Records: 3"  ?
```

### Test 3: Data Browser
```
1. Save 3 records
2. Click "Browse Data"
3. Check DataGrid

Expected: All 3 records show full text  ?
```

### Test 4: Cleanup
```
1. Manually add blank record to JSON
2. Click "Cleanup Blank Records"

Expected: Removes only blank, keeps valid records  ?
```

## Why This Happened

**Timeline of Changes**:

- **v1.3.3**: Fixed DataBrowserWindow.LoadData() with JsonSerializerOptions
  - ? Data Browser started working
  - ? Forgot to update MainWindow methods

- **v1.3.4**: Added decimal prompt numbers support
  - Changed `List<int>` ¡æ `List<string>`
  - ? Bug still present from v1.3.3

- **v1.3.5**: Fixed all deserialization in MainWindow
  - ? Complete fix applied
  - ? All save/load operations now correct

## Impact Assessment

### Affected Versions
- ? v1.3.1 - v1.3.4: **Data loss on every append**

### Not Affected
- ? v1.3.5+: **Fixed completely**

### Data Loss Scope
- **First record**: Always OK (no previous data to corrupt)
- **Subsequent records**: Previous records become empty
- **Severity**: HIGH - Loses all previously saved work

## Prevention

### Code Review Lesson

When updating serialization options in one place (DataBrowserWindow), **must update ALL deserialization calls**:

```csharp
// Search for ALL occurrences:
JsonSerializer.Deserialize<List<LlmDataRecord>>(...)

// Ensure ALL have options:
JsonSerializer.Deserialize<List<LlmDataRecord>>(..., options)
```

### Added to Checklist

- [ ] Check all Deserialize calls when changing model
- [ ] Test save ¡æ load ¡æ save ¡æ load cycle
- [ ] Verify multi-record append scenarios
- [ ] Check record count after operations

## Status

? **FIXED IN v1.3.5**  
? **Build Successful**  
? **Tests Passing**  
? **Ready for Use**

---

**IMPORTANT**: Users on v1.3.1 - v1.3.4 should:
1. Upgrade to v1.3.5 immediately
2. Use "Cleanup Blank Records" to remove empties
3. Verify data integrity
4. Re-enter any lost records
