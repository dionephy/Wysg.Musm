# ? QUICK START - 5 Minute Setup

## Your API is running on http://localhost:5205 ?

---

## ?? Step 1: Add to App.xaml.cs (2 min)

Open `apps\Wysg.Musm.Radium\App.xaml.cs` and add these services to `ConfigureServices`:

```csharp
// ADD THESE TWO LINES:
services.AddSingleton<ApiTokenManager>();
services.AddSingleton<ApiTestService>();

// ADD THIS METHOD CALL:
RegisterApiClients(services, apiSettings.BaseUrl);
```

Then copy the `RegisterApiClients` method from `App.xaml.cs.example`.

---

## ?? Step 2: Update Login (2 min)

Open your login ViewModel and:

### 2a. Inject ApiTokenManager

```csharp
private readonly ApiTokenManager _apiTokenManager;

// In constructor:
public YourLoginViewModel(..., ApiTokenManager apiTokenManager)
{
    _apiTokenManager = apiTokenManager;
}
```

### 2b. Set Token After Login

```csharp
if (authResult.Success)
{
    // ADD THIS ONE LINE:
    _apiTokenManager.SetAuthToken(authResult.IdToken);
    
    // ... rest of your code
}
```

---

## ?? Step 3: Test! (1 min)

### Quick Test (In Code)

Add temporarily to your login success:

```csharp
if (authResult.Success)
{
    _apiTokenManager.SetAuthToken(authResult.IdToken);
    
    // TEST API:
    var test = _serviceProvider.GetService<ApiTestService>();
    await test.QuickTestAsync();
    // Check Debug output!
}
```

### Or Use Test Window

Add this method to your MainWindow:

```csharp
private void OpenApiTest()
{
    var window = new Views.ApiTestWindow();
    window.Show();
}
```

Then call it from a button or menu.

---

## ? Expected Debug Output

```
[ApiTokenManager] ? Token set on 6/6 API clients
[ApiTest] Running quick health check...
[Test 1] Health Check
? API is healthy: Healthy
[ApiTest] ? Quick test passed
```

---

## ?? Done!

If you see the output above, **your API integration is working!**

---

## ?? If Something's Wrong

### Error: "Cannot connect"
¡æ Make sure API is running: `dotnet run` in `apps\Wysg.Musm.Radium.Api`

### Error: "401 Unauthorized"  
¡æ Token not set. Check `SetAuthToken()` was called.

### Error: "Service not registered"
¡æ Add to App.xaml.cs:
```csharp
services.AddSingleton<ApiTokenManager>();
services.AddSingleton<ApiTestService>();
```

---

## ?? Full Documentation

- **Setup**: `IMPLEMENTATION_COMPLETE.md` (step-by-step)
- **Testing**: `LOCALHOST_TESTING_GUIDE.md` (detailed tests)
- **Examples**: `App.xaml.cs.example`, `LoginViewModel.example.cs`

---

## ?? Pro Tip

After login, open the test window and click "Full Test Suite" to verify all APIs work!

---

**Ready to test? Start with Step 1! ??**
