# Architecture Clarification & Login Fix

**Date**: 2025-11-25  
**Type**: Architecture  
**Category**: System Design  
**Status**: ? Active

---

## Summary

This document provides detailed information for developers and architects. For user-facing guides, see the user documentation section.

---

# Architecture Clarification & Login Fix

**Date:** 2025-11-11  
**Status:** CORRECTED

---

## ? Correct Architecture

### Database Layer

```
������������������������������������������������������������������������������������������������������������������������������
��                     WPF Application                          ��
��                  (Wysg.Musm.Radium)                         ��
������������������������������������������������������������������������������������������������������������������������������
                    ��                    ��
                    ��                    ��
        �������������������������妡������������������  ���������������妡����������������������
        ��   HTTP API Call     ��  ��  Direct Access   ��
        ��   (via RadiumApi    ��  ��  (via Npgsql)    ��
        ��    Client)          ��  ��                  ��
        ����������������������������������������������  ����������������������������������������
                    ��                    ��
                    ��                    ��
        �������������������������妡������������������  ���������������妡����������������������
        ��   Radium API        ��  ��  PostgreSQL      ��
        ��   (localhost:5205)  ��  ��  (Local DB)      ��
        ��                     ��  ��                  ��
        ��   ��                 ��  ��                  ��
        ��   Azure SQL         ��  ��  - Tenants       ��
        ��   (Central DB)      ��  ��  - Studies       ��
        ��                     ��  ��  - Reports       ��
        ��   - Accounts        ��  ��  - PACS data     ��
        ��   - Phrases         ��  ��                  ��
        ��   - Hotkeys         ��  ��                  ��
        ��   - Snippets        ��  ��                  ��
        ��   - Settings        ��  ��                  ��
        ����������������������������������������������  ����������������������������������������
```

### Key Principles

**Azure SQL (Central DB):**
- ? **Only accessed via API** (`Wysg.Musm.Radium.Api`)
- ? Contains **account-scoped** data
- ? Requires **API to be running** for authentication & account operations
- ? **Never accessed directly** by WPF app

**PostgreSQL (Local DB):**
- ? **Directly accessed** by WPF app (via Npgsql)
- ? Contains **tenant/PACS-scoped** data
- ? Works **without API** for local operations
- ? **Does not contain accounts**

---

## Previous Misunderstanding

### What I Initially Thought (WRONG ?)

```
WPF App �� AzureSqlCentralService �� Azure SQL
         (Direct database access)
```

I mistakenly tried to fix the login by having the WPF app connect directly to Azure SQL using `AzureSqlCentralService`.

### Reality (CORRECT ?)

```
WPF App �� RadiumApiClient �� Radium API �� AzureSqlCentralService �� Azure SQL
         (HTTP calls)       (REST API)    (Only used by API)
```

The WPF app should **never** access Azure SQL directly. `AzureSqlCentralService` is an **internal API component** only.

---

## The Real Problem

When you tried to log in with Google:

1. **Google authentication** succeeded (Firebase worked)
2. **API call** failed because:
   - The Radium API was **not running** (`localhost:5205`)
   - The error message was confusing: "local connection string not configured"
   - It should have said: "**Cannot connect to API**"

### Why the Error Was Confusing

The Settings window lets you configure:
- **Azure SQL connection string** �� Used by the **API** (not WPF app)
- **PostgreSQL connection string** �� Used by the **WPF app** (local data)

When you saved the Azure SQL connection string in Settings, you thought it would allow direct login. But actually:
- That connection string is **only used by the API**
- The WPF app **never reads or uses** that Azure SQL connection string
- The WPF app **only uses** the PostgreSQL connection string for local tenant data

---

## The Fix

### Changes Made

**File:** `apps\Wysg.Musm.Radium\ViewModels\SplashLoginViewModel.cs`

1. **Reverted to API-based login** for all authentication methods:
   - `InitializeAsync()` - Silent restore via API
   - `OnEmailLoginAsync()` - Email/password login via API
   - `OnGoogleLoginAsync()` - Google login via API

2. **Improved error messages** when API is not available:

```csharp
catch (HttpRequestException hre)
{
    ErrorMessage = "Cannot connect to server. Please ensure:\n" +
                   "1. The Radium API is running (apps/Wysg.Musm.Radium.Api)\n" +
                   "2. API URL is correct in configuration\n" +
                   "3. Network connection is available\n\n" +
                   $"Technical details: {hre.Message}";
}
catch (TaskCanceledException tce)
{
    ErrorMessage = "Server connection timeout. The API may not be running.\n" +
                   "Please start: apps/Wysg.Musm.Radium.Api";
}
```

3. **Added proper exception handling** to distinguish:
   - Network errors (`HttpRequestException`)
   - Timeout errors (`TaskCanceledException`)
   - User cancellation (`OperationCanceledException`)

---

## How to Use

### Development (With API)

**Terminal 1:** Start the API
```powershell
cd apps\Wysg.Musm.Radium.Api
dotnet run
```

**Terminal 2:** Start the WPF app
```powershell
cd apps\Wysg.Musm.Radium
dotnet run
```

### Development (Without API)

If you can't run the API, you'll see a clear error message:

```
Cannot connect to server. Please ensure:
1. The Radium API is running (apps/Wysg.Musm.Radium.Api)
2. API URL is correct in configuration
3. Network connection is available
```

This tells you **exactly** what's wrong and how to fix it.

---

## Configuration

### API Configuration

**File:** `apps\Wysg.Musm.Radium.Api\appsettings.development.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:musm-server.database.windows.net,1433;Initial Catalog=musmdb;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=\"Active Directory Default\";"
  }
}
```

This is the **Azure SQL connection string** used by the **API**.

### WPF App Configuration

**File:** `apps\Wysg.Musm.Radium\appsettings.Development.json`

```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5205"
  }
}
```

This tells the WPF app **where to find the API**.

### Local Settings (Encrypted)

**File:** `%LOCALAPPDATA%\Wysg.Musm\Radium\settings.dat` (DPAPI-encrypted)

Contains:
- **PostgreSQL connection string** (local tenant/PACS data)
- **API base URL** (can override appsettings)
- **User preferences** (automation sequences, hotkeys, etc.)

---

## Testing Checklist

### Scenario 1: Both API and WPF Running ?

1. Start Radium API: `cd apps\Wysg.Musm.Radium.Api && dotnet run`
2. Wait for "Now listening on: http://localhost:5205"
3. Start WPF app: `cd apps\Wysg.Musm.Radium && dotnet run`
4. Log in with Google or email/password
5. ? Should successfully log in

### Scenario 2: Only WPF Running (API Not Started) ??

1. Start WPF app without starting API
2. Try to log in with Google or email/password
3. ?? Should show clear error message:
   ```
   Cannot connect to server. Please ensure:
   1. The Radium API is running (apps/Wysg.Musm.Radium.Api)
   ...
   ```
4. ? Error message is helpful and tells you what to do

### Scenario 3: Silent Restore (Token Refresh) ?

1. Log in once with "Keep logged in" checked
2. Close app
3. Restart app (with API running)
4. ? Should silently restore session without showing login form

### Scenario 4: PostgreSQL Direct Access ?

Even without the API, the WPF app can still:
- ? Access local tenant data
- ? Access local studies/reports
- ? Use UI spy and automation features
- ? Cannot log in (requires API for accounts)
- ? Cannot sync phrases/hotkeys (requires API for central data)

---

## What Each Service Does

### `RadiumApiClient` (WPF App)
- **Purpose:** HTTP client for calling the Radium API
- **Used by:** Login, account management, phrases, hotkeys, snippets
- **Located in:** `apps\Wysg.Musm.Radium\Services\RadiumApiClient.cs`

### `AzureSqlCentralService` (API Only)
- **Purpose:** Direct Azure SQL access for account operations
- **Used by:** Radium API controllers (internal only)
- **Located in:** `apps\Wysg.Musm.Radium\Services\AzureSqlCentralService.cs`
- ?? **Should NEVER be used directly by WPF app**

### `CentralDataSourceProvider` (WPF App)
- **Purpose:** Provides Npgsql data sources for PostgreSQL
- **Used by:** Local tenant/study/report operations
- **Located in:** `apps\Wysg.Musm.Radium\Services\CentralDataSourceProvider.cs`
- ? **Used directly by WPF app for local data**

### `TenantRepository` (WPF App)
- **Purpose:** Manages local tenant (PACS profiles) in PostgreSQL
- **Used by:** After login to create/load tenant for current account
- **Located in:** `apps\Wysg.Musm.Radium\Services\TenantRepository.cs`
- ? **Uses PostgreSQL directly**

---

## Migration Path (Future)

When you want to deploy to production:

### Option A: Keep API + PostgreSQL Architecture
```
Production:
  - Deploy Radium API to Azure App Service
  - Use Azure SQL for central data (already configured)
  - Each radiologist has local PostgreSQL for tenant data
```

### Option B: API-Only (Remove PostgreSQL)
```
Future Enhancement:
  - Migrate tenant/study data to API endpoints
  - Store tenant data in Azure SQL instead of local PostgreSQL
  - WPF app becomes "thin client" calling API for everything
```

---

## Key Takeaways

1. ? **Azure SQL** = Central DB = **API-only access**
2. ? **PostgreSQL** = Local DB = **Direct WPF access**
3. ? Login **requires API** to be running
4. ? Local operations work **without API**
5. ? Settings window saves **two different connection strings**:
   - **CentralConnectionString** (Azure SQL) �� Not used by WPF, only by API
   - **LocalConnectionString** (PostgreSQL) �� Used by WPF for tenant data
6. ?? `AzureSqlCentralService` is **internal to API**, never call from WPF

---

**Status:** Architecture clarified ?  
**Login:** Now shows helpful error messages when API is not running ?  
**Documentation:** Updated to reflect correct architecture ?

**Next Steps:**
1. Start both API and WPF app for development
2. For production, deploy API to Azure
3. Consider migrating to API-only architecture in future

---

**Updated:** 2025-11-11  
**Author:** Radium Development Team

