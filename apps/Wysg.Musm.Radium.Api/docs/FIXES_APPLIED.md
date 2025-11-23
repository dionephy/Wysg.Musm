# ?? Fixes Applied - Session Restore & Database Issues

## Date: 2025-01-23

---

## ? Issue #1: Port 7001 Connection Error

### Problem
```
Session restore failed: 대상 컴퓨터에서 연결을 거부했으므로 연결하지 못했습니다. (localhost:7001)
```

### Root Cause
The `ApiSettings.cs` file had a hardcoded default value pointing to the wrong port:
```csharp
public string BaseUrl { get; set; } = "https://localhost:7001"; // ? Wrong
```

### Solution Applied
**File:** `apps\Wysg.Musm.Radium\Configuration\ApiSettings.cs`

**Change:**
```csharp
// ? BEFORE
public string BaseUrl { get; set; } = "https://localhost:7001";

// ? AFTER
public string BaseUrl { get; set; } = "http://localhost:5205";
```

### Additional Steps Taken
1. Cleared environment variable `RADIUM_API_URL`
2. Deleted cached settings file: `%LOCALAPPDATA%\Wysg.Musm\Radium\settings.dat`

---

## ? Issue #2: Invalid Table Name Error

### Problem
```
Microsoft.Data.SqlClient.SqlException (0x80131904): 
Invalid object name 'radium.reportify_setting'.
```

### Root Cause
The Azure SQL database had the table renamed from `radium.reportify_setting` to `radium.user_setting`, but the API code was still referencing the old table name.

### Solution Applied
**File:** `apps\Wysg.Musm.Radium.Api\Repositories\AccountRepository.cs`

**Changes:**

#### 1. GetReportifySettingsAsync Method (Line ~204)
```csharp
// ? BEFORE
const string sql = @"
    SELECT settings_json
    FROM radium.reportify_setting
    WHERE account_id = @accountId";

// ? AFTER
const string sql = @"
    SELECT settings_json
    FROM radium.user_setting
    WHERE account_id = @accountId";
```

#### 2. UpsertReportifySettingsAsync Method (Line ~222)
```csharp
// ? BEFORE
const string sql = @"
    MERGE radium.reportify_setting AS target
    USING (SELECT @accountId AS account_id, @settingsJson AS settings_json) AS source
    ON (target.account_id = source.account_id)
    ...

// ? AFTER
const string sql = @"
    MERGE radium.user_setting AS target
    USING (SELECT @accountId AS account_id, @settingsJson AS settings_json) AS source
    ON (target.account_id = source.account_id)
    ...
```

### Verification
? Confirmed `UserSettingRepository.cs` already uses correct table name
? Confirmed no other references to `reportify_setting` in API project
? Build successful

---

## ?? Testing Steps

### 1. Restart API
```powershell
cd apps\Wysg.Musm.Radium.Api
dotnet run
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5205
```

### 2. Restart WPF App
```powershell
cd apps\Wysg.Musm.Radium
dotnet run
```

### 3. Test Login Flow
1. Open WPF app
2. Click "Log in (Google)" or use email/password
3. Should successfully authenticate without errors

### 4. Verify API Calls
Check API logs for successful requests:
```
info: Wysg.Musm.Radium.Api.Repositories.AccountRepository[0]
      Updated existing account 1 for UID ...
info: Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker[105]
      Executed action ... in XXXms
```

---

## ?? Files Modified

| File | Changes | Status |
|------|---------|--------|
| `apps\Wysg.Musm.Radium\Configuration\ApiSettings.cs` | Changed default port 7001 → 5205 | ? Fixed |
| `apps\Wysg.Musm.Radium.Api\Repositories\AccountRepository.cs` | Updated table name in 2 methods | ? Fixed |

---

## ?? Expected Behavior After Fixes

### Before
- ? Connection refused on port 7001
- ? 500 Internal Server Error on login
- ? Invalid table name exception

### After
- ? Connects to correct port (5205)
- ? Successful login with Google OAuth
- ? User settings loaded correctly
- ? Session restored without errors

---

## ?? Related Documentation

- **Table Migration Script:** `apps\Wysg.Musm.Radium\docs\db\migrations\20250122_rename_reportify_setting_to_user_setting.sql`
- **PostgreSQL Migration:** `apps\Wysg.Musm.Radium\docs\db\migrations\20250122_rename_reportify_setting_to_user_setting_postgres.sql`
- **Migration README:** `apps\Wysg.Musm.Radium\docs\db\migrations\20250122_RENAME_TABLE_README.md`

---

## ? Status: RESOLVED

Both issues have been fixed and the application should now work correctly.

**Next Steps:**
1. Test the login flow
2. Verify API calls in logs
3. Confirm session restoration works
4. Test user settings CRUD operations

---

**Last Updated:** 2025-01-23  
**Build Status:** ? Success  
**Test Status:** ?? Ready for Testing
