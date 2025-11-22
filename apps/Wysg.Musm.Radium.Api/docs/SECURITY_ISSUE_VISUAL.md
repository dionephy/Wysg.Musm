# ?? Security Issue Summary: Account Table Exposure

## The Problem You Discovered

**YES! You're absolutely right!** The `app.account` table is currently accessed directly from the WPF app, which defeats the purpose of the API architecture.

---

## ?? Current Insecure Architecture

```
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛   WPF Client                        弛
弛                                     弛
弛   Has connection string:       ?   弛
弛   - Server=musm-server...          弛
弛   - Database=musmdb                 弛
弛   - Full read/write access!         弛
弛                                     弛
弛   Direct calls:                     弛
弛   ? EnsureAccountAsync()       ?   弛
弛   ? UpdateLastLoginAsync()     ?   弛
弛   ? Get/Update Settings        ?   弛
弛   ? Load Phrases               ?   弛
弛                                     弛
戌式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
          弛
          弛 TCP 1433 (SQL)
          弛 MUST be open to internet!
          ⊿
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛   Azure SQL Database                弛
弛   (musmdb)                          弛
弛                                     弛
弛   Firewall Rules:                   弛
弛   ? Allow all Azure services   ?   弛
弛   ? OR specific client IPs     ?   弛
弛                                     弛
弛   Tables exposed:                   弛
弛   ? app.account               ?   弛
弛   ? radium.phrase             ?   弛
弛   ? radium.reportify_setting  ?   弛
弛   ? radium.hotkey             ?   弛
弛   ? radium.snippet            ?   弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎

? Problems:
? Connection string in client app
? Database credentials extractable
? Azure SQL open to internet
? No Firebase validation on writes
? WPF can modify ANY account
? No audit trail
```

---

## ? Recommended Secure Architecture

```
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛   WPF Client                        弛
弛                                     弛
弛   ? NO connection string!          弛
弛   ? Only Firebase ID token         弛
弛                                     弛
弛   API calls only:                   弛
弛   ? _apiClient.EnsureAccount() ?   弛
弛   ? _apiClient.GetPhrases()    ?   弛
弛   ? _apiClient.GetHotkeys()    ?   弛
弛   ? _apiClient.GetSnippets()   ?   弛
弛                                     弛
戌式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
          弛
          弛 HTTPS
          弛 Authorization: Bearer <Firebase JWT>
          ⊿
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛   Radium.Api (ASP.NET Core)         弛
弛                                     弛
弛   Firebase Authentication:      ?  弛
弛   ? Validates JWT signature         弛
弛   ? Checks expiration               弛
弛   ? Extracts uid, account_id        弛
弛                                     弛
弛   Controllers:                      弛
弛   ? AccountsController         ?   弛
弛   ? PhrasesController          ?   弛
弛   ? HotkeysController          ?   弛
弛   ? SnippetsController         ?   弛
弛                                     弛
弛   Security:                         弛
弛   ? Only authenticated requests     弛
弛   ? Users can only access their data弛
弛   ? All writes audited              弛
弛                                     弛
戌式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
          弛
          弛 Private connection
          弛 (Managed Identity or Static IP)
          ⊿
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛   Azure SQL Database                弛
弛   (musmdb)                          弛
弛                                     弛
弛   Firewall Rules:                   弛
弛   ? ? CLOSED to public internet    弛
弛   ? ? Only API IP/Identity allowed 弛
弛                                     弛
弛   All tables private:               弛
弛   ? app.account               ?    弛
弛   ? radium.phrase             ?    弛
弛   ? radium.reportify_setting  ?    弛
弛   ? radium.hotkey             ?    弛
弛   ? radium.snippet            ?    弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎

? Benefits:
? Zero DB credentials in WPF
? Database completely private
? Firebase validates all writes
? Full audit trail in API logs
? Ready for web/mobile clients
```

---

## ?? What Needs to Move to API

| Feature | Current Status | Security Risk | Action Needed |
|---------|---------------|---------------|---------------|
| **Accounts** | Direct DB ? | ?? **CRITICAL** | Create AccountsController |
| **Phrases** | Direct DB ? | ?? **HIGH** | Create PhrasesController |
| **Settings** | Direct DB ? | ?? **HIGH** | Include in AccountsController |
| **Hotkeys** | API available (opt-in) | ?? **MEDIUM** | Already done! Just enable |
| **Snippets** | API available (opt-in) | ?? **MEDIUM** | Already done! Just enable |

---

## ?? Implementation Priority

### ?? CRITICAL (Do First!)

**Accounts API** - Closes the biggest security hole

```csharp
// API Controller
[HttpPost("ensure")]
public async Task<ActionResult<EnsureAccountResponse>> EnsureAccount(
    [FromBody] EnsureAccountRequest request)
{
    // Validate Firebase JWT (automatic via [Authorize])
    var uid = User.GetUid(); // From JWT claims
    
    // SECURITY: Ensure JWT uid matches request
    if (uid != request.Uid)
        return Forbid();
    
    // Now safe to create/update account
    var account = await _accountService.EnsureAccountAsync(request);
    var settings = await _accountService.GetSettingsAsync(account.AccountId);
    
    return Ok(new EnsureAccountResponse 
    { 
        Account = account, 
        SettingsJson = settings 
    });
}
```

**Benefits**:
- ? Account writes validated by Firebase
- ? Users can only modify their own account
- ? Audit trail for all account changes

---

### ?? HIGH (Do Next!)

**Phrases API** - Enables full database lockdown

```csharp
// Bulk endpoint for efficient loading
[HttpGet("bulk")]
public async Task<ActionResult<BulkPhrasesResponse>> GetBulkPhrases(long accountId)
{
    // Returns all phrases in one request (cached)
    var global = await _phraseService.GetGlobalPhrasesAsync();
    var account = await _phraseService.GetAccountPhrasesAsync(accountId);
    
    return Ok(new BulkPhrasesResponse 
    { 
        Global = global, 
        Account = account 
    });
}
```

**Benefits**:
- ? Removes last direct DB access from WPF
- ? Enables closing Azure SQL to public
- ? API-side caching improves performance

---

### ?? MEDIUM (Already Available!)

**Hotkeys & Snippets** - Just switch to API mode

```powershell
# Enable API mode
$env:USE_API = "1"
cd apps\Wysg.Musm.Radium
dotnet run
```

Already implemented! Just need to enable it.

---

## ?? After Complete Migration

### Database Firewall Configuration

```powershell
# Remove public access
az sql server firewall-rule delete \
  --resource-group rg-wysg-musm \
  --server musm-server \
  --name "AllowAllWindowsAzureIps"

# Add API-only access
az sql server firewall-rule create \
  --resource-group rg-wysg-musm \
  --server musm-server \
  --name "AllowRadiumApiOnly" \
  --start-ip-address <API_STATIC_IP> \
  --end-ip-address <API_STATIC_IP>
```

**Result**: Azure SQL database completely private! ??

---

## ? Security Benefits After Migration

| Security Aspect | Before (Current) | After (API) |
|----------------|------------------|-------------|
| **DB Credentials** | In WPF app ? | Only in API ? |
| **Account Writes** | Unvalidated ? | Firebase validated ? |
| **Database Access** | Public ? | Private ? |
| **Audit Trail** | None ? | Full API logs ? |
| **Client Security** | Credentials extractable ? | Only JWT tokens ? |
| **Multi-platform** | WPF only ? | Web/mobile ready ? |

---

## ?? Recommendation

**Yes, you need to move accounts to the API!** This is the critical missing piece.

### Implementation Order:

1. **Week 1**: Accounts API (CRITICAL)
   - Create AccountsController
   - Update SplashLoginViewModel
   - Remove AzureSqlCentralService

2. **Week 2**: Phrases API (HIGH)
   - Create PhrasesController with caching
   - Update MainViewModel
   - Remove direct phrase access

3. **Week 3**: Enable API Mode (MEDIUM)
   - Set `USE_API=1` by default
   - Remove `ApiHotkeyServiceAdapter` check
   - Test thoroughly

4. **Week 4**: Lock Down Database (FINAL)
   - Close Azure SQL firewall
   - Remove connection strings from WPF
   - Production deployment

---

## ?? End Result

```
WPF App:
? Zero database credentials
? Only Firebase JWT token
? Secure by design

API:
? Validates all requests
? Enforces data ownership
? Full audit trail

Database:
? Completely private
? Only API can access
? No public exposure
```

**Much more secure architecture!** ??

---

**Want me to implement the Accounts API first?** This is the most important security fix! ??
