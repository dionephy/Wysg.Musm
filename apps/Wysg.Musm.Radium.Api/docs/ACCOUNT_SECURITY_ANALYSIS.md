# ?? Account Table Security Analysis & Solution

## ?? The Problem: Accounts Are Currently Exposed!

You correctly identified that the `app.account` table is accessed directly from the WPF app, which creates a **critical security vulnerability**.

---

## ?? Current Architecture (INSECURE!)

```
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛   WPF Client (Wysg.Musm.Radium)  弛
弛                                  弛
弛  AzureSqlCentralService:         弛
弛   - EnsureAccountAsync()   ?    弛
弛   - UpdateLastLoginAsync() ?    弛
弛   - GetSettings()          ?    弛
弛                                  弛
弛  Connection String stored in app!弛
弛  Full read/write access!    ?    弛
戌式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式戎
           弛
           弛 Direct SQL Connection
           弛 (Must be open to internet!)
           ⊿
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛   Azure SQL Database             弛
弛   (musmdb)                       弛
弛                                  弛
弛  ? app.account                  弛
弛  ? radium.phrase                弛
弛  ? radium.reportify_setting     弛
弛  ? radium.hotkey (API optional) 弛
弛  ? radium.snippet (API optional)弛
弛                                  弛
弛  Firewall: OPEN TO INTERNET! ?  弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
```

### Security Risks

1. **? Direct Account Manipulation**
   - WPF can create/modify ANY account
   - No Firebase validation on writes
   - No audit trail for account changes

2. **? Connection String Exposure**
   - Database credentials in WPF app
   - Could be extracted and misused
   - No rotation/revocation mechanism

3. **? Database Open to Internet**
   - Azure SQL must allow WPF client IPs
   - Broader attack surface
   - Potential data exfiltration

4. **? No Centralized Business Logic**
   - Account validation logic in client
   - Difficult to enforce policies
   - Inconsistent across platforms

---

## ? Recommended Architecture (SECURE!)

```
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛   WPF Client (Wysg.Musm.Radium)  弛
弛                                  弛
弛  RadiumApiClient:                弛
弛   - EnsureAccountAsync()   ?    弛
弛   - UpdateLastLoginAsync() ?    弛
弛   - GetPhrases()           ?    弛
弛   - Hotkeys/Snippets       ?    弛
弛                                  弛
弛  ? NO DB CONNECTION STRING!     弛
弛  ? Only Firebase ID token       弛
戌式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式戎
           弛
           弛 HTTPS + Firebase JWT
           弛 (Token validated on every request)
           ⊿
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛   Radium.Api                     弛
弛   (ASP.NET Core Web API)         弛
弛                                  弛
弛  FirebaseAuthenticationHandler:  弛
弛   ? Validates JWT signature     弛
弛   ? Checks expiration           弛
弛   ? Extracts uid, account_id    弛
弛                                  弛
弛  Controllers:                    弛
弛   - AccountsController     ?    弛
弛   - PhrasesController      ?    弛
弛   - HotkeysController      ?    弛
弛   - SnippetsController     ?    弛
弛                                  弛
弛  Business Logic:                 弛
弛   ? Validates Firebase claims   弛
弛   ? Enforces account ownership  弛
弛   ? Audit trail for all changes 弛
戌式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式戎
           弛
           弛 Private Connection
           弛 (Managed Identity or Static IP)
           ⊿
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛   Azure SQL Database             弛
弛   (musmdb)                       弛
弛                                  弛
弛  ? app.account                  弛
弛  ? radium.phrase                弛
弛  ? radium.hotkey                弛
弛  ? radium.snippet               弛
弛  ? radium.reportify_setting     弛
弛                                  弛
弛  Firewall: ? PRIVATE!           弛
弛  Only API can access             弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
```

---

## ?? What Needs to Move to API

### Currently NOT in API (Security Issue!)

| Feature | Current Access | Security Risk | Priority |
|---------|---------------|---------------|----------|
| **Accounts** | Direct DB ? | Can modify any account | ?? **CRITICAL** |
| **Phrases** | Direct DB ? | Large dataset exposure | ?? **HIGH** |
| **Settings** | Direct DB ? | User data exposure | ?? **HIGH** |

### Already in API (Optional)

| Feature | Current Status | Notes |
|---------|---------------|-------|
| **Hotkeys** | API available (opt-in via `USE_API=1`) | ? Can use API now |
| **Snippets** | API available (opt-in via `USE_API=1`) | ? Can use API now |

---

## ?? Implementation Plan

### Phase 1: Add Accounts API ? **IMMEDIATE**

#### 1.1 Create Accounts Controller

```csharp
// apps/Wysg.Musm.Radium.Api/Controllers/AccountsController.cs
[ApiController]
[Authorize] // Requires Firebase JWT
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    // POST /api/accounts/ensure
    // Ensures account exists and returns account + settings
    [HttpPost("ensure")]
    public async Task<ActionResult<EnsureAccountResponse>> EnsureAccount(
        [FromBody] EnsureAccountRequest request)
    {
        // 1. Validate Firebase JWT (already done by [Authorize])
        // 2. Extract uid from JWT claims
        // 3. Ensure uid matches request.Uid (security check!)
        // 4. Create/update account
        // 5. Load settings
        // 6. Update last_login_at
        // 7. Return account + settings
    }
    
    // POST /api/accounts/{accountId}/login
    // Updates last login timestamp
    [HttpPost("{accountId}/login")]
    public async Task<ActionResult> UpdateLastLogin(long accountId)
    {
        // 1. Validate Firebase JWT
        // 2. Ensure JWT uid matches this account (security!)
        // 3. Update last_login_at
    }
}
```

**Security Benefits**:
- ? Firebase JWT validated on every request
- ? Can only modify your own account (uid check)
- ? Audit trail in API logs
- ? No direct DB access from client

#### 1.2 Update WPF to Use Accounts API

```csharp
// apps/Wysg.Musm.Radium/Services/ApiAccountService.cs
public class ApiAccountService
{
    private readonly RadiumApiClient _apiClient;
    
    public async Task<(long accountId, string? settingsJson)> EnsureAccountAsync(
        string uid, string email, string displayName)
    {
        var request = new EnsureAccountRequest 
        { 
            Uid = uid, 
            Email = email, 
            DisplayName = displayName 
        };
        
        var response = await _apiClient.EnsureAccountAsync(request);
        return (response.Account.AccountId, response.SettingsJson);
    }
    
    public async Task UpdateLastLoginAsync(long accountId)
    {
        await _apiClient.UpdateLastLoginAsync(accountId);
    }
}
```

#### 1.3 Replace AzureSqlCentralService in SplashLoginViewModel

```csharp
// Before (insecure):
var accountId = await _central.EnsureAccountAsync(auth.UserId, auth.Email, auth.DisplayName);

// After (secure):
var (accountId, settingsJson) = await _accountService.EnsureAccountAsync(
    auth.UserId, auth.Email, auth.DisplayName);
_tenantContext.ReportifySettingsJson = settingsJson;
```

---

### Phase 2: Add Phrases API ? **HIGH PRIORITY**

#### 2.1 Create Phrases Controller

```csharp
[ApiController]
[Authorize]
[Route("api/accounts/{accountId}/[controller]")]
public class PhrasesController : ControllerBase
{
    // GET /api/accounts/1/phrases/bulk
    // Returns all phrases (global + account-specific) in one request
    [HttpGet("bulk")]
    public async Task<ActionResult<BulkPhrasesResponse>> GetBulk(long accountId)
    {
        // Returns:
        // - All global phrases (account_id IS NULL)
        // - All account phrases (account_id = accountId)
        // Client caches this locally for completion
    }
}
```

#### 2.2 Add API-side Caching

```csharp
// Memory cache for phrases (30min TTL)
public class PhraseCacheService
{
    public async Task<BulkPhrasesResponse> GetCachedBulkPhrasesAsync(long accountId)
    {
        return await _cache.GetOrCreateAsync($"phrases_bulk_{accountId}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            return await _phraseRepository.GetBulkPhrasesAsync(accountId);
        });
    }
}
```

**Performance**: First load slower, subsequent loads instant (cached)

---

### Phase 3: Lock Down Azure SQL ??

After all features moved to API:

```powershell
# Remove "Allow Azure services" rule
az sql server firewall-rule delete \
  --resource-group rg-wysg-musm \
  --server musm-server \
  --name "AllowAllWindowsAzureIps"

# Add API-only rule (using Managed Identity)
# Option 1: Use Managed Identity (recommended)
az sql server ad-admin set \
  --resource-group rg-wysg-musm \
  --server musm-server \
  --display-name <API_MANAGED_IDENTITY>

# Option 2: Whitelist API's static IP
az sql server firewall-rule create \
  --resource-group rg-wysg-musm \
  --server musm-server \
  --name "AllowRadiumApi" \
  --start-ip-address <API_STATIC_IP> \
  --end-ip-address <API_STATIC_IP>
```

**Result**: Database completely private! ?

---

## ?? Security Benefits Summary

### Before (Current - INSECURE)

| Aspect | Status | Risk Level |
|--------|--------|------------|
| **DB Credentials** | In WPF app | ?? **CRITICAL** |
| **Account Writes** | Unvalidated | ?? **CRITICAL** |
| **Database Firewall** | Open to internet | ?? **CRITICAL** |
| **Audit Trail** | None | ?? **HIGH** |
| **Token Validation** | Client-side only | ?? **HIGH** |

### After (Recommended - SECURE)

| Aspect | Status | Security Level |
|--------|--------|----------------|
| **DB Credentials** | Only in API | ? **SECURE** |
| **Account Writes** | API-validated | ? **SECURE** |
| **Database Firewall** | Private (API-only) | ? **SECURE** |
| **Audit Trail** | All API requests logged | ? **SECURE** |
| **Token Validation** | Server-side (Firebase) | ? **SECURE** |

---

## ?? Performance Considerations

### Accounts & Settings

- **Current**: ~200-500ms (direct SQL)
- **Via API**: ~300-700ms (HTTP overhead)
- **Impact**: Minimal (only on login)

### Phrases

- **Current**: ~500-1000ms (direct SQL, 10k+ phrases)
- **Via API (first load)**: ~800-1500ms (HTTP + serialization)
- **Via API (cached)**: ~100-200ms (API cache hit)
- **Impact**: First login slower, but overall better with caching

### Hotkeys & Snippets

- **Already implemented** ?
- Performance acceptable (few hundred items max)

---

## ? Recommended Implementation Order

1. **Week 1**: Accounts API (CRITICAL - closes biggest security hole)
   - Add AccountsController
   - Update SplashLoginViewModel
   - Remove AzureSqlCentralService

2. **Week 2**: Phrases API (HIGH - enables database lockdown)
   - Add PhrasesController with caching
   - Add bulk endpoints
   - Client-side caching in WPF

3. **Week 3**: Enable API Mode for Hotkeys/Snippets
   - Set `USE_API=1` by default
   - Test thoroughly
   - Remove direct DB code

4. **Week 4**: Lock Down Database
   - Remove WPF connection strings
   - Lock Azure SQL firewall
   - Enable Managed Identity
   - Production deployment

---

## ?? Bottom Line

**YES, the account table is a security problem!**

| What | Security Risk | Solution |
|------|--------------|----------|
| **Account Management** | ?? **CRITICAL** | Move to API immediately |
| **Phrases** | ?? **HIGH** | Move to API (enables DB lockdown) |
| **Hotkeys/Snippets** | ?? **MEDIUM** | Already have API (just switch to it) |

**Complete all 4 phases to achieve**:
- ? Zero database credentials in WPF
- ? Azure SQL completely private
- ? All writes validated by Firebase
- ? Full audit trail
- ? Ready for web/mobile expansion

---

## ?? Next Step

Should I implement the **Accounts API** first? This is the most critical security fix:

1. Create `AccountsController`
2. Create `IAccountRepository` & `AccountRepository`
3. Update `RadiumApiClient` with account methods
4. Replace `AzureSqlCentralService` in `SplashLoginViewModel`
5. Remove direct database access for accounts

This alone will close the biggest security hole! ??
