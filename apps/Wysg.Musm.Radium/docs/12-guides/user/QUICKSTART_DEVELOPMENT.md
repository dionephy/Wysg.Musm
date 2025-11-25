# Quick Reference: Radium Development Setup

**Date**: 2025-11-25  
**Type**: Quick Start Guide  
**Category**: Getting Started  
**Status**: ? Active

---

## Summary

This guide provides quick reference information for users. For detailed implementation information, see the related plan and specification documents.

---

# Quick Reference: Radium Development Setup

**Last Updated:** 2025-11-11

---

## Starting the Application (Development)

### ? Correct Way (Both Services)

**Terminal 1 - Start API:**
```powershell
cd apps\Wysg.Musm.Radium.Api
dotnet run
```
Wait for: `Now listening on: http://localhost:5205`

**Terminal 2 - Start WPF App:**
```powershell
cd apps\Wysg.Musm.Radium
dotnet run
```

### ?? What Happens Without API

If you start only the WPF app without the API:

```
? Google login fails with:
   "Cannot connect to server. Please ensure:
    1. The Radium API is running..."

? Email/password login fails (same message)

? Silent restore fails (falls back to login form)

? Local operations work (UI spy, automation, viewing local data)
```

---

## Architecture Quick Reference

### Azure SQL (Central DB)
- **Access:** Via API **ONLY**
- **Contains:** Accounts, phrases, hotkeys, snippets, settings
- **Connection String:** Configured in `apps\Wysg.Musm.Radium.Api\appsettings.development.json`
- **Used By:** Radium API (`AzureSqlCentralService`)

### PostgreSQL (Local DB)
- **Access:** Direct from WPF app
- **Contains:** Tenants, studies, reports, PACS profiles
- **Connection String:** Configured in Settings window → saved to encrypted local storage
- **Used By:** WPF app (`CentralDataSourceProvider`, `TenantRepository`)

---

## Common Issues & Solutions

### Issue: "Cannot connect to server" during login

**Cause:** Radium API is not running

**Solution:**
```powershell
cd apps\Wysg.Musm.Radium.Api
dotnet run
```

### Issue: API shows connection error to Azure SQL

**Cause:** Azure SQL connection string not configured or invalid

**Solution:** Edit `apps\Wysg.Musm.Radium.Api\appsettings.development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:musm-server.database.windows.net,1433;Initial Catalog=musmdb;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=\"Active Directory Default\";"
  }
}
```

### Issue: Local operations fail (tenant/study data)

**Cause:** PostgreSQL connection string not configured

**Solution:** 
1. Open WPF app
2. Click Settings (?) in login window
3. Configure "Local Connection String" (PostgreSQL)
4. Click "Test Local" to verify
5. Click "Save"

---

## Configuration Files

### API Configuration
**File:** `apps\Wysg.Musm.Radium.Api\appsettings.development.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "<Azure SQL connection string>"
  },
  "ApiSettings": {
    "UseAzureManagedIdentity": false
  },
  "Firebase": {
    "ProjectId": "wysg-musm-4baf9",
    "ValidateToken": false
  }
}
```

### WPF App Configuration
**File:** `apps\Wysg.Musm.Radium\appsettings.Development.json`
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5205",
    "EnableApi": true
  }
}
```

### Local Settings (Encrypted)
**Location:** `%LOCALAPPDATA%\Wysg.Musm\Radium\settings.dat`  
**Format:** DPAPI-encrypted key-value pairs

Contains:
- `local` = PostgreSQL connection string (tenant/study data)
- `central` = (Not used by WPF, only by API)
- User preferences (automation, hotkeys, etc.)

---

## Testing Checklist

Before reporting a login issue, verify:

- [ ] **API is running** on http://localhost:5205
  ```powershell
  # Test manually:
  Invoke-WebRequest http://localhost:5205/health
  # Should return: "Healthy" or similar
  ```

- [ ] **API can reach Azure SQL**
  - Check API logs for connection errors
  - Verify authentication (Azure AD or SQL auth)

- [ ] **WPF app can reach API**
  ```powershell
  # From WPF login window:
  Click "Test Central" button
  # Should show: "API connection OK!"
  ```

- [ ] **WPF app can reach PostgreSQL** (for local data)
  - Open Settings from login window
  - Configure Local Connection String
  - Click "Test Local"
  - Should show: "? Connected"

---

## Environment Variables (Optional)

### Override API URL
```powershell
$env:RADIUM_API_URL = "https://your-production-api.azurewebsites.net"
```

### Use API vs Direct DB (for development)
```powershell
$env:USE_API = "1"  # Use API for phrases/hotkeys/snippets
$env:USE_API = "0"  # Use direct Azure SQL (not recommended)
```

### Enable PostgreSQL Tracing
```powershell
$env:RAD_TRACE_PG = "1"  # Verbose Npgsql logging
```

### Disable Phrase Preload (faster startup)
```powershell
$env:RAD_DISABLE_PHRASE_PRELOAD = "1"
```

---

## Ports Used

| Service | Port | Protocol | Purpose |
|---------|------|----------|---------|
| Radium API | 5205 | HTTP | REST API for central data (dev) |
| Radium API | 5206 | HTTPS | REST API (production) |
| PostgreSQL | 5432 | TCP | Local tenant/study database |
| Azure SQL | 1433 | TCP | Central accounts database (via API) |
| Firebase | 443 | HTTPS | Authentication (Google OAuth) |

---

## Visual Studio Tips

### Run Both Projects Simultaneously

1. Right-click solution → **Properties**
2. Select **Multiple startup projects**
3. Set both to **Start**:
   - `Wysg.Musm.Radium.Api` → **Start**
   - `Wysg.Musm.Radium` → **Start**
4. Click **OK**
5. Press **F5** to debug both

### Debug API Calls

Add breakpoint in:
- `apps\Wysg.Musm.Radium\Services\RadiumApiClient.cs` (WPF side)
- `apps\Wysg.Musm.Radium.Api\Controllers\*.cs` (API side)

---

## Production Deployment

### Azure App Service (API)
```bash
cd apps/Wysg.Musm.Radium.Api
dotnet publish -c Release -o ./publish
az webapp deployment source config-zip \
  --resource-group YOUR_RG \
  --name radium-api \
  --src publish.zip
```

### WPF App (Desktop)
```bash
cd apps/Wysg.Musm.Radium
dotnet publish -c Release -r win-x64 --self-contained false
# Output: bin\Release\net10.0-windows\win-x64\publish
# Create installer or distribute folder
```

---

## Support

For issues:
1. Check this guide first
2. Verify API is running (`http://localhost:5205/health`)
3. Check API logs for errors
4. Review architecture: `ARCHITECTURE_CLARIFICATION_2025-11-11.md`

---

**Quick Links:**
- [Architecture Clarification](ARCHITECTURE_CLARIFICATION_2025-11-11.md)
- [Google Login Bugfix](BUGFIX_2025-11-11_GoogleLoginConnectionStringError.md)
- [API Documentation](../Wysg.Musm.Radium.Api/QUICKSTART.md)

