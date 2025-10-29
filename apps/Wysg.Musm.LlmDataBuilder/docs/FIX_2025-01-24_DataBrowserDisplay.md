# Fix: Data Browser Not Showing Text (v1.3.3)

## Problem

The Data Browser window was displaying record numbers but not showing the actual text content (Input, Output, ProtoOutput fields appeared blank) even though the data.json file contained valid data.

## Root Cause

**JSON Property Name Mismatch:**
- **Saving**: Used `JsonNamingPolicy.CamelCase` ¡æ properties saved as `input`, `output`, `protoOutput`
- **Loading**: Used default deserialization ¡æ expected `Input`, `Output`, `ProtoOutput` (PascalCase)
- **Result**: Deserialization created objects with default empty strings because property names didn't match

## Solution

Updated `DataBrowserWindow.LoadData()` to use the same naming policy as the save operation:

```csharp
// BEFORE (incorrect):
var records = JsonSerializer.Deserialize<List<LlmDataRecord>>(json);

// AFTER (correct):
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true
};
var records = JsonSerializer.Deserialize<List<LlmDataRecord>>(json, options);
```

## Testing

### Before Fix
1. Open Data Browser
2. Expected: See records with text content
3. Actual: Records showed as blank (only index numbers visible)

### After Fix
1. **Restart application** (close completely)
2. Open Data Browser
3. Result: ? All text content now displays correctly

### Test Steps
1. Save a record: Input="test input", Output="test output"
2. Click "Browse Data"
3. Verify DataGrid shows:
   - Column "#": 1
   - Column "Input": "test input"
   - Column "Output": "test output"
   - Column "Proto Output": (content if exists)
4. Select record
5. Verify detail panel shows full text

## Files Modified

1. ? `DataBrowserWindow.xaml.cs` - Added JsonSerializerOptions to LoadData()

## Version

- **Version**: 1.3.3
- **Date**: 2025-01-24
- **Type**: Bug Fix
- **Impact**: Critical - Data Browser now functional

## Related Issues

This was NOT related to the blank records issue (v1.3.1). The data was always being saved correctly, but the browser couldn't read it due to the property name mismatch.

---

**Status**: ? Fixed  
**Build**: ? Successful  
**Action Required**: Restart application to see fix
