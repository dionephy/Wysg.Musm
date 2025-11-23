# ?? LOCALHOST TESTING - IMPLEMENTATION COMPLETE!

## ? What's Been Created

I've implemented a complete localhost testing infrastructure for your WPF application:

### 1. **Configuration File** ?
- `appsettings.Development.json` - Configured for `http://localhost:5205`

### 2. **Helper Services** ?
- `ApiTokenManager.cs` - Centralized token management
- `ApiTestService.cs` - Comprehensive API testing suite

### 3. **Example Code** ?
- `App.xaml.cs.example` - Complete DI configuration
- `LoginViewModel.example.cs` - Login integration example

### 4. **Test UI** ?
- `ApiTestWindow.xaml` - Standalone test window
- `ApiTestWindow.xaml.cs` - Test window code-behind

---

## ?? HOW TO USE (5 Minutes Setup)

### Step 1: Copy Configuration (30 seconds)

The file `appsettings.Development.json` is already created. Just verify it exists:

```
apps\Wysg.Musm.Radium\appsettings.Development.json
```

Should contain:
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5205"
  }
}
```

? **Done!** Configuration is ready.

---

### Step 2: Register Services in App.xaml.cs (2 minutes)

Open your **existing** `apps\Wysg.Musm.Radium\App.xaml.cs` and:

#### Option A: Copy from Example (Recommended)
Open `App.xaml.cs.example` and copy the relevant sections into your actual `App.xaml.cs`:

1. Copy the `RegisterApiClients` method
2. Copy the service registrations in `ConfigureServices`
3. Add these two lines:
   ```csharp
   services.AddSingleton<ApiTokenManager>();
   services.AddSingleton<ApiTestService>();
   ```

#### Option B: Manual Changes
Add to your existing `ConfigureServices` method:

```csharp
// Add these service registrations
services.AddSingleton<ApiTokenManager>();
services.AddSingleton<ApiTestService>();

// Register the 6 API clients (see App.xaml.cs.example for full code)
RegisterApiClients(services, apiSettings.BaseUrl);
```

Then add the `RegisterApiClients` method from the example file.

---

### Step 3: Update Login Flow (2 minutes)

Open your **existing login ViewModel** (e.g., `SplashLoginViewModel.cs`) and add:

#### 3a. Inject ApiTokenManager

```csharp
private readonly ApiTokenManager _apiTokenManager;

public YourLoginViewModel(..., ApiTokenManager apiTokenManager)
{
    // ... existing code
    _apiTokenManager = apiTokenManager;
}
```

#### 3b. Set Token After Successful Login

Find your login success handler and add:

```csharp
if (authResult.Success)
{
    // ? ADD THIS LINE
    _apiTokenManager.SetAuthToken(authResult.IdToken);
    
    // ... rest of your existing code
}
```

That's it! Token is now set on all 6 API clients automatically.

---

### Step 4: Test! (30 seconds)

#### Option A: Quick Test in Code

Add this to your login success handler (temporary, for testing):

```csharp
if (authResult.Success)
{
    _apiTokenManager.SetAuthToken(authResult.IdToken);
    
    // TEST: Verify API connection
    var testService = _serviceProvider.GetService<ApiTestService>();
    await testService.QuickTestAsync();
    // Check Debug output for results
}
```

#### Option B: Use Test Window (Recommended)

Add a menu item or button to open the test window:

```csharp
// In your MainWindow or SettingsWindow
private void OpenApiTestWindow()
{
    var testWindow = new ApiTestWindow();
    testWindow.Show();
}
```

Or add to your XAML:
```xml
<MenuItem Header="Tools">
    <MenuItem Header="API Connection Test" Click="OpenApiTestWindow_Click"/>
</MenuItem>
```

---

## ?? TESTING WORKFLOW

### 1. Start API Server

```bash
cd apps\Wysg.Musm.Radium.Api
dotnet run
```

Wait for:
```
Now listening on: http://localhost:5205
```

### 2. Start WPF Application

Press F5 in Visual Studio

### 3. Open Test Window

- Click **Tools ⊥ API Connection Test** (if you added the menu)
- Or temporarily call `new ApiTestWindow().Show()` in your code

### 4. Run Tests

#### Quick Test (No Login Required)
- Click **"Quick Test"** button
- Should see: ? API is healthy

#### Full Test (Login Required)
1. Close test window
2. Login to your app
3. Open test window again
4. Click **"Full Test Suite"**
5. Should see all 5 tests pass

---

## ?? WHAT THE TESTS CHECK

### Quick Test
- ? API is reachable at `http://localhost:5205`
- ? Health endpoint responds

### Full Test Suite
1. **Health Check** - API is running
2. **User Settings** - Can get/update settings
3. **Phrases** - Can list phrases
4. **Hotkeys** - Can list hotkeys
5. **Snippets** - Can list snippets

All tests log detailed output to:
- Test window (if using UI)
- Visual Studio Debug output
- Console (if running from terminal)

---

## ?? DEBUG OUTPUT EXAMPLES

### ? Success (Expected Output)

```
???????????????????????????????????????
[ApiTest] Starting comprehensive API tests
???????????????????????????????????????

[Test 1] Health Check
式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
? API is healthy: Healthy

[Test 2] User Settings API
式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
   Account ID: 1
? Settings retrieved: 1234 characters
   Rev: 5, Updated: 2025-01-23T10:30:00

[Test 3] Phrases API
式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
   Account ID: 1
? Phrases retrieved: 42 phrases
   Sample: 'right pneumothorax' (ID: 123)

[Test 4] Hotkeys API
式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
   Account ID: 1
? Hotkeys retrieved: 15 hotkeys
   Sample: '..rt' ⊥ 'right...'

[Test 5] Snippets API
式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
   Account ID: 1
? Snippets retrieved: 8 snippets
   Sample: '..chest' ⊥ 'Chest X-ray: {{findings}}...'

???????????????????????????????????????
[ApiTest] Tests completed: ? ALL PASSED
???????????????????????????????????????
```

### ? Error Examples

**API Not Running:**
```
[Test 1] Health Check
式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
? Cannot reach API: No connection could be made...
   Make sure API is running on http://localhost:5205
```

**Token Not Set:**
```
[Test 2] User Settings API
式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
? 401 Unauthorized - Token not set or expired
   Call ApiTokenManager.SetAuthToken() after login
```

---

## ?? INTEGRATION CHECKLIST

Use this checklist to track your implementation:

### Configuration
- [x] `appsettings.Development.json` created ?
- [ ] Verified API URL is `http://localhost:5205`

### App.xaml.cs
- [ ] `ApiTokenManager` registered
- [ ] `ApiTestService` registered
- [ ] All 6 API clients registered (UserSettings, Phrases, Hotkeys, Snippets, SNOMED, Reports)
- [ ] HttpClientFactory configured

### Login Flow
- [ ] `ApiTokenManager` injected into login ViewModel
- [ ] `SetAuthToken()` called after successful login
- [ ] Verified token is set (check Debug output)

### Testing
- [ ] API server starts successfully
- [ ] Quick test passes (health check)
- [ ] Can login successfully
- [ ] Full test suite passes
- [ ] All 5 tests show ?

---

## ?? USAGE EXAMPLES

### Example 1: Set Token on Login

```csharp
// In your SplashLoginViewModel or similar
private async Task OnLoginAsync(string email, string password)
{
    var authResult = await _authService.SignInWithEmailPasswordAsync(email, password);
    
    if (authResult.Success)
    {
        // ? Set token on all API clients
        _apiTokenManager.SetAuthToken(authResult.IdToken);
        
        // Now all API calls will work
        var phrases = await _phrasesApi.GetAllAsync(accountId);
    }
}
```

### Example 2: Run Tests Programmatically

```csharp
// Quick test (no login needed)
var testService = _serviceProvider.GetService<ApiTestService>();
var success = await testService.QuickTestAsync();

if (!success)
{
    MessageBox.Show("API not available!");
}
```

### Example 3: Verify Before Important Operation

```csharp
// Before syncing data
private async Task SyncDataAsync()
{
    var testService = _serviceProvider.GetService<ApiTestService>();
    var apiAvailable = await testService.QuickTestAsync();
    
    if (!apiAvailable)
    {
        MessageBox.Show("Cannot sync: API not available");
        return;
    }
    
    // Proceed with sync
    await DoSyncAsync();
}
```

---

## ?? TROUBLESHOOTING

### Problem: "ApiTokenManager not found"

**Solution**: Add to `App.xaml.cs`:
```csharp
services.AddSingleton<ApiTokenManager>();
services.AddSingleton<ApiTestService>();
```

### Problem: "Cannot connect to API"

**Solution**: 
1. Check API is running: `netstat -ano | findstr :5205`
2. Check URL in `appsettings.Development.json`
3. Try: `curl http://localhost:5205/health`

### Problem: "401 Unauthorized"

**Solution**: Token not set. Make sure you call:
```csharp
_apiTokenManager.SetAuthToken(authResult.IdToken);
```

### Problem: Tests fail with "Account ID not set"

**Solution**: Set tenant context before running tests:
```csharp
_tenantContext.AccountId = 1; // Your actual account ID
```

---

## ?? SUCCESS CRITERIA

You're ready for production when:

- ? API server starts without errors
- ? WPF app loads configuration correctly
- ? Quick test passes (health check)
- ? Login sets token successfully
- ? Full test suite passes (all 5 tests)
- ? Can load phrases, hotkeys, snippets from UI
- ? No 401/403 errors in logs
- ? API calls complete in reasonable time (<500ms)

---

## ?? WHAT'S NEXT

### Immediate (Now):
1. ? Copy API client registrations to `App.xaml.cs`
2. ? Update login flow to set token
3. ? Run quick test
4. ? Run full test suite

### Short Term (This Week):
1. Migrate more services to use API clients
2. Add error handling for offline scenarios
3. Implement data caching
4. Test with real user data

### Long Term (Next Week):
1. Remove old database code
2. Deploy API to Azure
3. Update production configuration
4. Test with multiple users

---

## ?? YOU'RE ALL SET!

Everything is implemented and ready to test:

? Configuration files created  
? Helper services implemented  
? Example code provided  
? Test UI ready  
? Documentation complete  

**Just follow the 4-step setup above and you'll be testing in 5 minutes!**

---

## ?? FILES CREATED

```
apps\Wysg.Musm.Radium\
戍式式 appsettings.Development.json          ∠ Configuration ?
戍式式 Services\
弛   戍式式 ApiTokenManager.cs                ∠ Token management ?
弛   戌式式 ApiTestService.cs                 ∠ Test suite ?
戍式式 Views\
弛   戍式式 ApiTestWindow.xaml                ∠ Test UI ?
弛   戌式式 ApiTestWindow.xaml.cs             ∠ Test logic ?
戍式式 App.xaml.cs.example                   ∠ DI example ?
戌式式 ViewModels\
    戌式式 LoginViewModel.example.cs         ∠ Login example ?
```

**All files are ready to use!** ??

---

**Need help with integration? Let me know which step you're on!**
