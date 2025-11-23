# ?? Localhost Testing Guide - Quick Start

## ? Your API is Running!

```
Now listening on: http://localhost:5205
```

Perfect! Your API server is ready. Follow these steps to test it with the WPF client.

---

## ?? Step 1: Configure WPF App (1 minute)

Create `apps\Wysg.Musm.Radium\appsettings.Development.json`:

```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5205"
  }
}
```

**Important**: Use `http://` (not `https://`) since your API is running on HTTP.

---

## ?? Step 2: Test API Connection (30 seconds)

Open browser or use curl:

```bash
# Test health endpoint
curl http://localhost:5205/health
# Expected: "Healthy" or similar

# Test API info (if available)
curl http://localhost:5205/api
```

Or just open in browser: `http://localhost:5205/health`

---

## ?? Step 3: Set Firebase Token After Login

In your `SplashLoginViewModel` or wherever you handle login:

```csharp
// After successful login
var authResult = await _authService.SignInWithEmailPasswordAsync(email, password);

if (authResult.Success)
{
    // ? Set Firebase token on all API clients
    SetAuthTokenOnAllClients(authResult.IdToken);
    
    // Now continue with loading data...
}

private void SetAuthTokenOnAllClients(string firebaseIdToken)
{
    // Get all API clients and set token
    var clients = new[]
    {
        _serviceProvider.GetService<IUserSettingsApiClient>() as ApiClientBase,
        _serviceProvider.GetService<IPhrasesApiClient>() as ApiClientBase,
        _serviceProvider.GetService<IHotkeysApiClient>() as ApiClientBase,
        _serviceProvider.GetService<ISnippetsApiClient>() as ApiClientBase,
        _serviceProvider.GetService<ISnomedApiClient>() as ApiClientBase,
        _serviceProvider.GetService<IExportedReportsApiClient>() as ApiClientBase
    };

    foreach (var client in clients.Where(c => c != null))
    {
        client!.SetAuthToken(firebaseIdToken);
    }
    
    Debug.WriteLine($"[Auth] Token set on {clients.Count(c => c != null)} API clients");
}
```

---

## ?? Step 4: Simple Test (5 minutes)

Add this test method to verify the connection works:

```csharp
private async Task TestApiConnectionAsync()
{
    try
    {
        Debug.WriteLine("[Test] Testing API connection...");
        
        // Test 1: Check if API is reachable
        using var client = new HttpClient { BaseAddress = new Uri("http://localhost:5205") };
        var healthResponse = await client.GetAsync("/health");
        Debug.WriteLine($"[Test] Health check: {healthResponse.StatusCode}");
        
        if (!healthResponse.IsSuccessStatusCode)
        {
            Debug.WriteLine("[Test] ? API health check failed!");
            return;
        }
        
        // Test 2: Try getting user settings (requires auth)
        var accountId = _tenantContext.AccountId;
        if (accountId > 0)
        {
            var settingsApi = _serviceProvider.GetService<IUserSettingsApiClient>();
            var settings = await settingsApi.GetAsync(accountId);
            
            if (settings != null)
            {
                Debug.WriteLine($"[Test] ? Settings loaded: {settings.SettingsJson?.Length ?? 0} chars");
            }
            else
            {
                Debug.WriteLine("[Test] ?? No settings found (may be new account)");
            }
        }
        
        Debug.WriteLine("[Test] ? API connection test completed!");
    }
    catch (HttpRequestException ex)
    {
        Debug.WriteLine($"[Test] ? Connection error: {ex.Message}");
        Debug.WriteLine("[Test] Make sure API is running on http://localhost:5205");
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"[Test] ? Test failed: {ex.Message}");
    }
}
```

Call this after login:
```csharp
if (authResult.Success)
{
    SetAuthTokenOnAllClients(authResult.IdToken);
    await TestApiConnectionAsync(); // Add this line
}
```

---

## ?? What to Look For

### In Debug Output (Visual Studio Output window):

**? Success**:
```
[Auth] Token set on 6 API clients
[Test] Testing API connection...
[Test] Health check: OK
[Test] ? Settings loaded: 1234 chars
[Test] ? API connection test completed!
```

**? Common Errors**:

1. **Cannot connect**:
```
[Test] ? Connection error: No connection could be made
```
**Fix**: Make sure API is running (`dotnet run` in Wysg.Musm.Radium.Api)

2. **401 Unauthorized**:
```
[Test] ? API returned: Unauthorized (401)
```
**Fix**: Token not set. Check `SetAuthToken()` was called.

3. **Wrong URL**:
```
[Test] ? Connection error: Connection refused
```
**Fix**: Check `BaseUrl` in `appsettings.json` is `http://localhost:5205`

---

## ?? Testing Checklist

### Before Starting:
- [x] API running: `http://localhost:5205`
- [ ] `appsettings.Development.json` created with correct BaseUrl
- [ ] API clients registered in `App.xaml.cs` (without `IAuthService`)
- [ ] Firebase environment variables set

### During Login:
- [ ] Login succeeds
- [ ] `authResult.IdToken` is not null
- [ ] `SetAuthToken()` called on all clients
- [ ] Debug shows "Token set on 6 API clients"

### Test Each Feature:
- [ ] User Settings: Get works
- [ ] Phrases: List works
- [ ] Hotkeys: List works
- [ ] Snippets: List works

### Verify Logs:
- [ ] No connection errors
- [ ] No 401 errors
- [ ] API calls shown in Debug output

---

## ?? Quick Troubleshooting

### Problem: "Cannot connect to API"

**Check 1**: Is API running?
```bash
# In terminal
netstat -ano | findstr :5205
# Should show LISTENING
```

**Check 2**: Can you reach health endpoint?
```bash
curl http://localhost:5205/health
# Or open in browser
```

**Check 3**: Is BaseUrl correct?
```json
// In appsettings.json
"BaseUrl": "http://localhost:5205"  // ? Correct
"BaseUrl": "https://localhost:5205" // ? Wrong (no HTTPS)
"BaseUrl": "http://localhost:7001"  // ? Wrong port
```

### Problem: "401 Unauthorized"

**Fix**: Make sure token is set:
```csharp
// Verify token is set
var client = _serviceProvider.GetService<IUserSettingsApiClient>() as ApiClientBase;
var httpClient = client?.HttpClient;
var hasAuth = httpClient?.DefaultRequestHeaders.Authorization != null;
Debug.WriteLine($"Has authorization header: {hasAuth}");

// If false, call SetAuthToken again:
client?.SetAuthToken(authResult.IdToken);
```

### Problem: "Slow performance"

**Tip**: This is normal for first request. Subsequent calls should be fast.

```csharp
var sw = Stopwatch.StartNew();
var settings = await _settingsApi.GetAsync(accountId);
sw.Stop();
Debug.WriteLine($"API call took {sw.ElapsedMilliseconds}ms");
// First call: ~500-1000ms (cold start)
// Later calls: ~50-200ms (warm)
```

---

## ?? Success Indicators

You'll know everything is working when:

1. ? API responds to `/health`
2. ? Login completes without errors
3. ? Token is set on all clients
4. ? First API call succeeds (user settings, phrases, etc.)
5. ? No 401/403 errors in Debug output
6. ? Data loads correctly in UI

---

## ?? Next Steps

Once localhost testing works:

1. ? Test all features incrementally
2. ? Verify error handling
3. ? Check offline behavior (stop API, see what happens)
4. ? Monitor memory usage
5. ? Deploy to Azure when ready

---

## ?? Pro Tips

### Tip 1: Keep API Running
Use `dotnet watch run` instead of `dotnet run`:
```bash
cd apps\Wysg.Musm.Radium.Api
dotnet watch run
# Auto-restarts on code changes
```

### Tip 2: Use HTTP File for Testing
Create `test-wpf-integration.http`:
```http
### Test health
GET http://localhost:5205/health

### Test user settings (replace token)
GET http://localhost:5205/api/accounts/1/settings
Authorization: Bearer YOUR_FIREBASE_TOKEN_HERE

### Test phrases
GET http://localhost:5205/api/accounts/1/phrases
Authorization: Bearer YOUR_FIREBASE_TOKEN_HERE
```

### Tip 3: Enable Detailed Logging
In API's `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

---

## ?? Ready to Test!

Your API is running on **`http://localhost:5205`**. 

Just:
1. Update `appsettings.json` with the correct URL
2. Set Firebase token after login
3. Start testing!

**Good luck! ??**
