# ?? LOCALHOST TESTING - VISUAL GUIDE

```
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛                   YOUR SETUP (Current Status)                 弛
戍式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛                                                               弛
弛  ? API Server Running                                       弛
弛     http://localhost:5205                                     弛
弛     Status: Listening                                         弛
弛                                                               弛
弛  ? API Clients Created                                      弛
弛     ? UserSettingsApiClient                                   弛
弛     ? PhrasesApiClient                                        弛
弛     ? HotkeysApiClient                                        弛
弛     ? SnippetsApiClient                                       弛
弛     ? SnomedApiClient                                         弛
弛     ? ExportedReportsApiClient                                弛
弛                                                               弛
弛  ? Documentation Complete                                   弛
弛     ? Registration guide (corrected)                          弛
弛     ? Testing guide (step-by-step)                            弛
弛     ? Troubleshooting guide                                   弛
弛                                                               弛
弛  ? TODO: Configure WPF App                                  弛
弛     ? Create appsettings.Development.json                     弛
弛     ? Register API clients in App.xaml.cs                     弛
弛     ? Set Firebase token after login                          弛
弛                                                               弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
```

---

## ?? 3-STEP TESTING PROCESS

```
忙式式式式式式式式式式式式式忖
弛   STEP 1    弛  Configure WPF (1 min)
弛   CONFIG    弛
戌式式式式式式成式式式式式式戎
       弛
       弛  Create appsettings.Development.json:
       弛  { "ApiSettings": { "BaseUrl": "http://localhost:5205" } }
       弛
       ∪
忙式式式式式式式式式式式式式忖
弛   STEP 2    弛  Register Clients (5 min)
弛  REGISTER   弛
戌式式式式式式成式式式式式式戎
       弛
       弛  In App.xaml.cs:
       弛  services.AddScoped<IUserSettingsApiClient>(...)
       弛  services.AddScoped<IPhrasesApiClient>(...)
       弛  ... (repeat for 4 more)
       弛
       ∪
忙式式式式式式式式式式式式式忖
弛   STEP 3    弛  Set Token & Test (2 min)
弛    TEST     弛
戌式式式式式式成式式式式式式戎
       弛
       弛  After login:
       弛  apiClient.SetAuthToken(authResult.IdToken)
       弛  var data = await apiClient.GetAsync(accountId)
       弛
       ∪
忙式式式式式式式式式式式式式忖
弛   SUCCESS   弛  ? API working!
戌式式式式式式式式式式式式式戎
```

---

## ?? REQUEST FLOW

### What Happens When You Call an API:

```
WPF Application
    弛
    弛 1. User logs in
    ∪
GoogleOAuthAuthService
    弛
    弛 2. Returns AuthResult with IdToken (JWT)
    ∪
SetAuthToken(authResult.IdToken)
    弛
    弛 3. Token stored in HttpClient.DefaultRequestHeaders
    ∪
await _phrasesApi.GetAllAsync(accountId)
    弛
    弛 4. HTTP GET with Authorization: Bearer <token>
    ∪
http://localhost:5205/api/accounts/1/phrases
    弛
    弛 5. FirebaseAuthenticationHandler validates token
    ∪
PhrasesController.GetAll()
    弛
    弛 6. Query database
    ∪
Return List<PhraseDto>
    弛
    弛 7. JSON response
    ∪
WPF displays data in UI
```

---

## ?? ARCHITECTURE DIAGRAM

```
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛                     WPF Application                          弛
弛  忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖  弛
弛  弛              ViewModels Layer                          弛  弛
弛  弛  ? PhraseManagementViewModel                           弛  弛
弛  弛  ? HotkeyManagementViewModel                           弛  弛
弛  戌式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎  弛
弛                  弛 Inject                                    弛
弛  忙式式式式式式式式式式式式式式式∪式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖  弛
弛  弛              API Clients Layer                         弛  弛
弛  弛  ? IPhrasesApiClient                                   弛  弛
弛  弛  ? IHotkeysApiClient                                   弛  弛
弛  弛  ? IUserSettingsApiClient                              弛  弛
弛  戌式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎  弛
弛                  弛 HTTP + Firebase JWT                       弛
戌式式式式式式式式式式式式式式式式式式托式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
                   弛
                   弛 HTTPS (or HTTP for localhost)
                   弛 Authorization: Bearer <firebase-token>
                   弛
忙式式式式式式式式式式式式式式式式式式∪式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛              API Server (localhost:5205)                      弛
弛  忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖  弛
弛  弛     FirebaseAuthenticationHandler                      弛  弛
弛  弛     (Validates JWT token)                              弛  弛
弛  戌式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎  弛
弛                  弛                                            弛
弛  忙式式式式式式式式式式式式式式式∪式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖  弛
弛  弛              Controllers                                弛  弛
弛  弛  ? PhrasesController                                    弛  弛
弛  弛  ? HotkeysController                                    弛  弛
弛  戌式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎  弛
弛                  弛                                            弛
弛  忙式式式式式式式式式式式式式式式∪式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖  弛
弛  弛              Services                                   弛  弛
弛  弛  ? PhraseService                                        弛  弛
弛  弛  ? HotkeyService                                        弛  弛
弛  戌式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎  弛
弛                  弛                                            弛
弛  忙式式式式式式式式式式式式式式式∪式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖  弛
弛  弛            Repositories                                 弛  弛
弛  弛  ? PhraseRepository                                     弛  弛
弛  弛  ? HotkeyRepository                                     弛  弛
弛  戌式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎  弛
弛                  弛 SQL Queries                                弛
戌式式式式式式式式式式式式式式式式式式托式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
                   弛
忙式式式式式式式式式式式式式式式式式式∪式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛           Azure SQL Database (musmdb)                         弛
弛  ? radium.phrase                                              弛
弛  ? radium.hotkey                                              弛
弛  ? radium.snippet                                             弛
弛  ? radium.user_setting                                        弛
弛  ? snomed.concept_cache                                       弛
弛  ? radium.exported_report                                     弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
```

---

## ?? TESTING SEQUENCE

### 1?? Pre-Test Checklist

```
﹤ API running?          ⊥ netstat -ano | findstr :5205
﹤ Health endpoint?      ⊥ curl http://localhost:5205/health
﹤ appsettings.json?     ⊥ Check BaseUrl = "http://localhost:5205"
﹤ Clients registered?   ⊥ Check App.xaml.cs RegisterApiClients()
﹤ Firebase vars set?    ⊥ Check FIREBASE_API_KEY, GOOGLE_OAUTH_CLIENT_ID
```

### 2?? Login Flow Test

```
﹤ Open WPF app
﹤ Click "Login" or "Sign in with Google"
﹤ Enter credentials
﹤ Verify login succeeds ⊥ Check Debug output for success message
﹤ Verify token set     ⊥ Look for "[Auth] Token set on 6 API clients"
```

### 3?? API Call Test

```
﹤ Navigate to Phrases screen
﹤ Should load phrases automatically
﹤ Check Debug output:
  ? "[PhrasesApiClient] Getting all phrases for account X"
  ? "[ApiClientBase] GET http://localhost:5205/api/accounts/X/phrases"
  ? Data appears in UI

﹤ Try adding a phrase
﹤ Check Debug output:
  ? "[PhrasesApiClient] Upserting phrase for account X"
  ? "[ApiClientBase] PUT http://localhost:5205/api/accounts/X/phrases"
  ? New phrase appears in UI
```

### 4?? Error Handling Test

```
﹤ Stop API server (Ctrl+C in API terminal)
﹤ Try loading phrases ⊥ Should see error in Debug output
﹤ Restart API server
﹤ Try again ⊥ Should work
```

---

## ?? DEBUG OUTPUT - What to Expect

### ? SUCCESS (Normal Operation)

```
[Auth] Firebase token obtained successfully
[Auth] Token set on 6 API clients
[PhrasesApiClient] Getting all phrases for account 1, activeOnly=True
[ApiClientBase] GET http://localhost:5205/api/accounts/1/phrases
[PhrasesApiClient] Retrieved 42 phrases
[UI] Phrases loaded and displayed
```

### ?? WARNING (No Settings Yet)

```
[UserSettingsApiClient] Getting settings for account 1
[ApiClientBase] GET http://localhost:5205/api/accounts/1/settings
[UserSettingsApiClient] Settings not found for account 1
[UI] Using default settings
```

### ? ERROR (API Not Running)

```
[PhrasesApiClient] Getting all phrases for account 1
[ApiClientBase] GET http://localhost:5205/api/accounts/1/phrases
[ApiClientBase] DELETE error for /api/accounts/1/phrases: No connection could be made...
[UI] Failed to load phrases
```

### ?? ERROR (Token Not Set)

```
[ApiClientBase] WARNING: No authorization header set. API calls may fail with 401.
[ApiClientBase] GET http://localhost:5205/api/accounts/1/phrases
[ApiClientBase] GET failed: Unauthorized (401)
[UI] Authentication required
```

---

## ?? SUCCESS INDICATORS

### You'll Know It's Working When:

```
? Login completes without errors
? Debug shows "Token set on 6 API clients"
? Phrases screen loads data
? Can add/edit/delete phrases
? No 401/403 errors
? API server logs show requests:
   info: Microsoft.AspNetCore.Hosting.Diagnostics[1]
         Request starting HTTP/1.1 GET http://localhost:5205/api/accounts/1/phrases
   info: Microsoft.AspNetCore.Routing.EndpointMiddleware[0]
         Executing endpoint 'PhrasesController.GetAll'
   info: Microsoft.AspNetCore.Routing.EndpointMiddleware[1]
         Executed endpoint 'PhrasesController.GetAll'
```

---

## ?? GETTING HELP

### If Something Doesn't Work:

**1. Check API Server Logs** (in API terminal)
   - Look for errors
   - Check if requests are arriving

**2. Check WPF Debug Output** (Visual Studio Output window)
   - Filter by "ApiClient" or "Auth"
   - Look for error messages

**3. Check Network** (Use browser or Postman)
   ```
   GET http://localhost:5205/health
   GET http://localhost:5205/api/accounts/1/phrases
   (Add: Authorization: Bearer <your-token>)
   ```

**4. Review Guides**:
   - `LOCALHOST_TESTING_GUIDE.md` - Detailed testing
   - `API_CLIENT_REGISTRATION_GUIDE.md` - Setup steps
   - `READY_FOR_TESTING.md` - Quick reference

---

## ?? FINAL CHECKLIST

Before considering it "done":

```
﹤ API running on localhost:5205
﹤ WPF app configured with correct BaseUrl
﹤ All 6 API clients registered
﹤ Login works and sets Firebase token
﹤ User settings can be loaded/saved
﹤ Phrases can be loaded/created/edited/deleted
﹤ Hotkeys work
﹤ Snippets work
﹤ No 401/403 errors
﹤ No connection errors
﹤ Debug output looks clean
```

When all checked ? ⊥ **Ready for production deployment!**

---

**You're all set! Start testing! ??**
