# Bug Fix: Google Login "Local Connection String Not Configured" Error

**Date:** 2025-11-11
**Status:** ~~FIXED~~ **CORRECTED (See ARCHITECTURE_CLARIFICATION_2025-11-11.md)**
**Issue:** Google login fails with "local connection string not configured" error even when Azure SQL connection string is properly saved in Settings

---

## ?? IMPORTANT CORRECTION

**This document contained an incorrect fix.** The actual architecture is:

- **Azure SQL (Central DB)** �� Accessed **ONLY via API** (not directly by WPF app)
- **PostgreSQL (Local DB)** �� Accessed **directly by WPF app**

The original fix in this document tried to make the WPF app access Azure SQL directly, which violates the architecture.

**Please see:** `ARCHITECTURE_CLARIFICATION_2025-11-11.md` for the correct fix and architecture explanation.

---

## Original Problem (Still Valid)

When logging in with Google in the SplashWindow, users encountered an error:

> "Google log in error, local connection string not configured"

This occurred even though the Azure SQL connection string was properly saved in the local settings via the Settings dialog.

## Root Cause (Clarified)

The app was calling the Radium API at `http://localhost:5205` during login, but:
1. The API was **not running** (development-only)
2. The error message was misleading - it should have said "API not available" instead of "local connection string not configured"

## Correct Solution

**DO NOT** access Azure SQL directly from the WPF app. Instead:

1. **Start the API** before running the WPF app:
   ```powershell
   # Terminal 1
   cd apps/Wysg.Musm.Radium.Api
   dotnet run
   
   # Terminal 2
   cd apps/Wysg.Musm.Radium
   dotnet run
   ```

2. **Improved error messages** now tell you when the API is not running:
   ```
   Cannot connect to server. Please ensure:
   1. The Radium API is running (apps/Wysg.Musm.Radium.Api)
   2. API URL is correct in configuration
   3. Network connection is available
   ```

## Files Modified (Corrected)

- `apps\Wysg.Musm.Radium\ViewModels\SplashLoginViewModel.cs`
  - Kept API-based login (reverted incorrect direct Azure SQL access)
  - Added better error messages for API connection failures
  - Separated error handling for network vs timeout vs cancellation

## What Was Wrong With Original Fix

The original fix tried to:
1. Create `AzureSqlCentralService` in the WPF app
2. Access Azure SQL directly during login
3. Bypass the API entirely

This was incorrect because:
- `AzureSqlCentralService` is an **internal API component**
- Azure SQL should **only be accessed via the API**
- The architecture requires separation between central (API-managed) and local (WPF-managed) data

## Correct Architecture

```
������������������������������������������������������������������������������������������������������������������������������
��                     WPF Application                          ��
��                  (Wysg.Musm.Radium)                         ��
������������������������������������������������������������������������������������������������������������������������������
                    ��                    ��
                    ��                    ��
        �������������������������妡������������������  ���������������妡����������������������
        ��   HTTP API Call     ��  ��  Direct Access   ��
        ��   (RadiumApiClient) ��  ��  (Npgsql)        ��
        ����������������������������������������������  ����������������������������������������
                    ��                    ��
        �������������������������妡������������������  ���������������妡����������������������
        ��   Radium API        ��  ��  PostgreSQL      ��
        ��   (localhost:5205)  ��  ��  (Local DB)      ��
        ��   ��                 ��  ��                  ��
        ��   Azure SQL         ��  ��  - Tenants       ��
        ��   (Central DB)      ��  ��  - Studies       ��
        ��   - Accounts ?     ��  ��  - Reports       ��
        ��   - Phrases ?      ��  ��  - PACS data     ��
        ����������������������������������������������  ����������������������������������������
```

---

**For complete details, see:** `apps\Wysg.Musm.Radium\docs\ARCHITECTURE_CLARIFICATION_2025-11-11.md`

**Result:** Error messages now clearly indicate when API is not running, guiding users to start the API before logging in. ??
