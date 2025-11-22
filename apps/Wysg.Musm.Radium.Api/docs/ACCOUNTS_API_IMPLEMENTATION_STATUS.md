# ?? Accounts API Implementation - COMPLETE!

## ? What Was Implemented

I've successfully implemented the **Accounts API** to close the critical security vulnerability!

---

## ?? Files Created

### API Side (Wysg.Musm.Radium.Api)

1. ? **`Repositories/IAccountRepository.cs`** - Account repository interface
2. ? **`Repositories/AccountRepository.cs`** - Azure SQL implementation
3. ? **`Controllers/AccountsController.cs`** - RESTful account endpoints
4. ? **Updated `Models/Dtos/AccountDto.cs`** - Added `EnsureAccountResponse`
5. ? **Updated `Program.cs`** - Registered account services

### Client Side (Wysg.Musm.Radium)

1. ? **Updated `Services/RadiumApiClient.cs`** - Added account methods & DTOs

---

## ?? Security Features Implemented

### 1. Firebase JWT Validation

All account operations require Firebase authentication:

```csharp
[ApiController]
[Authorize] // ก็ Requires valid Firebase JWT token
[Route("api/[controller]")]
public class AccountsController : ControllerBase
```

### 2. UID Ownership Enforcement

Users can only access their own account:

```csharp
// Get UID from Firebase JWT token
var tokenUid = User.GetUid();

// SECURITY: Ensure JWT UID matches requested UID
if (tokenUid != request.Uid)
{
    return Forbid("Cannot create/update account for different user");
}
```

### 3. Account Ownership Verification

All operations verify account belongs to authenticated user:

```csharp
var account = await _accountRepository.GetAccountByIdAsync(accountId);
if (account.Uid != tokenUid)
{
    return Forbid("Cannot access different user's account");
}
```

---

## ?? API Endpoints

### POST /api/accounts/ensure

Ensures account exists (creates or updates).

**Request:**
```json
{
  "uid": "firebase-user-id",
  "email": "user@example.com",
  "displayName": "John Doe"
}
```

**Response:**
```json
{
  "account": {
    "accountId": 123,
    "uid": "firebase-user-id",
    "email": "user@example.com",
    "displayName": "John Doe",
    "isActive": true,
    "createdAt": "2025-01-30T10:00:00Z",
    "lastLoginAt": "2025-01-30T10:00:00Z"
  },
  "settingsJson": "{...reportify settings...}"
}
```

**Security:**
- ? Requires Firebase JWT token
- ? UID in JWT must match UID in request
- ? Auto-updates last_login_at
- ? Returns settings in single call

---

### POST /api/accounts/{accountId}/login

Updates last login timestamp.

**Response:** 204 No Content

**Security:**
- ? Requires Firebase JWT token
- ? Verifies account belongs to authenticated user

---

### GET /api/accounts/{accountId}

Gets account information.

**Response:**
```json
{
  "accountId": 123,
  "uid": "firebase-user-id",
  "email": "user@example.com",
  "displayName": "John Doe",
  "isActive": true,
  "createdAt": "2025-01-30T10:00:00Z",
  "lastLoginAt": "2025-01-30T10:00:00Z"
}
```

**Security:**
- ? Requires Firebase JWT token
- ? Can only view own account

---

### GET /api/accounts/{accountId}/settings

Gets reportify settings JSON.

**Response:** Settings JSON string or 204 No Content if no settings.

**Security:**
- ? Requires Firebase JWT token
- ? Can only view own settings

---

### PUT /api/accounts/{accountId}/settings

Updates reportify settings JSON.

**Request:** Settings JSON string (in body)

**Response:** 204 No Content

**Security:**
- ? Requires Firebase JWT token
- ? Can only update own settings

---

## ?? Client Integration

### RadiumApiClient Methods Added

```csharp
// Ensure account exists and get settings
var response = await _apiClient.EnsureAccountAsync(new EnsureAccountRequest
{
    Uid = auth.UserId,
    Email = auth.Email,
    DisplayName = auth.DisplayName
});

// Update last login
await _apiClient.UpdateLastLoginAsync(accountId);

// Get account info
var account = await _apiClient.GetAccountAsync(accountId);

// Get/Update settings
var settings = await _apiClient.GetSettingsAsync(accountId);
await _apiClient.UpdateSettingsAsync(accountId, settingsJson);
```

---

## ?? **NEXT STEP: Update SplashLoginViewModel**

The API is ready, but **SplashLoginViewModel still uses AzureSqlCentralService** directly!

### Current Code (INSECURE):
```csharp
var combined = await _central.EnsureAccountAndGetSettingsAsync(...);
var accountId = combined.accountId;
await _central.UpdateLastLoginAsync(accountId);
```

### Needs to Change To (SECURE):
```csharp
var response = await _apiClient.EnsureAccountAsync(new EnsureAccountRequest
{
    Uid = refreshed.UserId,
    Email = _storage.Email ?? string.Empty,
    DisplayName = _storage.DisplayName ?? string.Empty
});

var accountId = response.Account.AccountId;
_tenantContext.ReportifySettingsJson = response.SettingsJson;
```

---

## ?? TODO: Complete the Migration

### 1. Update `SplashLoginViewModel.cs`

Replace ALL occurrences of `_central` with API calls:

**Lines to Update:**
- Line 117: `var combined = await _central.EnsureAccountAndGetSettingsAsync(...)`
- Line 164: `BackgroundTask.Run("LastLoginUpdate", () => _central.UpdateLastLoginAsync(...))`
- Line 218: `var accountId = await _central.EnsureAccountAsync(...)`
- Line 219: `await _central.UpdateLastLoginAsync(accountId)`
- Line 232: `var combined = await _central.EnsureAccountAndGetSettingsAsync(...)`
- Line 277: `var accountId = await _central.EnsureAccountAsync(...)`
- Line 278: `await _central.UpdateLastLoginAsync(accountId)`
- Line 291: `var combined = await _central.EnsureAccountAndGetSettingsAsync(...)`
- Line 368: `var (ok, msg) = await _central.TestConnectionAsync()`

**Remove:**
- `_central` field declaration
- `AzureSqlCentralService` from constructor
- `TestCentralCommand` implementation (can't test API from client)

### 2. Update `App.xaml.cs`

**Remove:**
```csharp
services.AddSingleton<AzureSqlCentralService>();
```

### 3. Test the Integration

```powershell
# Terminal 1: Start API
cd apps\Wysg.Musm.Radium.Api
dotnet run

# Terminal 2: Run WPF app
cd apps\Wysg.Musm.Radium
dotnet run
```

**Test scenarios:**
1. Login with Google OAuth
2. Login with Email/Password
3. Refresh token (close and reopen app)
4. Verify account created in database
5. Verify settings loaded correctly

---

## ?? Security Benefits After Complete Migration

| Aspect | Before | After |
|--------|--------|-------|
| **Account Writes** | Direct DB (unvalidated) ? | API-validated ? |
| **UID Verification** | Client-side only ? | Server-side (Firebase JWT) ? |
| **Audit Trail** | None ? | All requests logged ? |
| **Data Access** | Can modify any account ? | Only own account ? |
| **DB Credentials** | In WPF app ? | Only in API ? |

---

## ?? What's Left to Close Security Hole Completely

### Critical (For Account Security)
1. ? **Update SplashLoginViewModel** - Replace _central with API calls
2. ? **Remove AzureSqlCentralService** - No longer needed
3. ? **Test thoroughly** - Verify login flow works

### High Priority (For Complete DB Lockdown)
4. ?? **Add Phrases API** - Last direct DB access
5. ?? **Enable API mode by default** - Set `USE_API=1`
6. ?? **Lock Azure SQL firewall** - Close to public

---

## ?? Current Status

? **API Implementation: COMPLETE**
- Account repository created
- Account controller created
- Security features implemented
- Client methods added

? **Client Integration: IN PROGRESS**
- API client updated
- Login flow needs updating

?? **Database Lockdown: PENDING**
- Waiting for phrases API
- Then can close firewall

---

## ?? Next Action

**I need to complete the SplashLoginViewModel update!**

Should I:
1. **Update SplashLoginViewModel** to use the Accounts API? (Recommended)
2. **Create a migration guide** for you to do it manually?
3. **Show example code** for the key changes needed?

Let me know and I'll complete the account security migration! ??
