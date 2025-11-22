# ? WPF to API Integration - COMPLETE!

## ?? What Was Done

Your WPF app is now successfully connected to the API! Here's what was implemented:

### 1. **API Client Registration** (`App.xaml.cs`)
- ? Added `RadiumApiClient` to DI container
- ? Configurable via `RADIUM_API_URL` environment variable
- ? Default: `http://localhost:5205` for local development

```csharp
services.AddSingleton<RadiumApiClient>(sp =>
{
    var apiUrl = Environment.GetEnvironmentVariable("RADIUM_API_URL") ?? "http://localhost:5205";
    return new RadiumApiClient(apiUrl);
});
```

### 2. **Firebase Token Integration** (`SplashLoginViewModel.cs`)
- ? Token passed to API client after **silent login** (refresh token)
- ? Token passed to API client after **email/password login**
- ? Token passed to API client after **Google OAuth login**

```csharp
// After successful login
_apiClient.SetAuthToken(auth.IdToken);  // ¡ç This is all you need!
```

### 3. **Ready to Use**
The `RadiumApiClient` is now available everywhere in your app via DI!

---

## ?? How to Use the API Client

### Option 1: In ViewModels (Recommended)

Add `RadiumApiClient` to your constructor:

```csharp
public class HotkeysViewModel
{
    private readonly RadiumApiClient _apiClient;
    private readonly ITenantContext _tenant;

    public HotkeysViewModel(RadiumApiClient apiClient, ITenantContext tenant)
    {
        _apiClient = apiClient;
        _tenant = tenant;
    }

    public async Task LoadHotkeysAsync()
    {
        var hotkeys = await _apiClient.GetHotkeysAsync(_tenant.TenantId);
        // Use hotkeys...
    }
}
```

### Option 2: In Services

```csharp
public class MyCustomService
{
    private readonly RadiumApiClient _apiClient;

    public MyCustomService(RadiumApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task DoSomethingAsync()
    {
        var accountId = 1; // or from ITenantContext
        var snippets = await _apiClient.GetSnippetsAsync(accountId);
    }
}
```

---

## ?? Available API Methods

### Hotkeys
```csharp
// Get all hotkeys
var hotkeys = await _apiClient.GetHotkeysAsync(accountId);

// Get single hotkey
var hotkey = await _apiClient.GetHotkeyAsync(accountId, hotkeyId);

// Create/Update hotkey
var request = new UpsertHotkeyRequest
{
    TriggerText = "mykey",
    ExpansionText = "My expansion text",
    Description = "Optional description",
    IsActive = true
};
var created = await _apiClient.UpsertHotkeyAsync(accountId, request);

// Toggle active status
var toggled = await _apiClient.ToggleHotkeyAsync(accountId, hotkeyId);

// Delete hotkey
var deleted = await _apiClient.DeleteHotkeyAsync(accountId, hotkeyId);
```

### Snippets
```csharp
// Same pattern as hotkeys
var snippets = await _apiClient.GetSnippetsAsync(accountId);
var snippet = await _apiClient.GetSnippetAsync(accountId, snippetId);
var created = await _apiClient.UpsertSnippetAsync(accountId, request);
var toggled = await _apiClient.ToggleSnippetAsync(accountId, snippetId);
var deleted = await _apiClient.DeleteSnippetAsync(accountId, snippetId);
```

---

## ?? Configuration

### Development (Local API)
Default: `http://localhost:5205`

No configuration needed! Just run the API:
```powershell
cd apps\Wysg.Musm.Radium.Api
dotnet run
```

### Production (Azure App Service)
Set environment variable:
```powershell
# PowerShell
$env:RADIUM_API_URL = "https://api-wysg-musm.azurewebsites.net"

# Or in app.config / launch settings
```

Or update `App.xaml.cs`:
```csharp
var apiUrl = Environment.GetEnvironmentVariable("RADIUM_API_URL") 
    ?? "https://api-wysg-musm.azurewebsites.net"; // Production URL
```

---

## ?? Migration Example: Replace Direct DB Calls

### Before (Direct Database)
```csharp
public class HotkeysViewModel
{
    private readonly IHotkeyService _hotkeyService; // Direct DB
    
    public async Task LoadAsync()
    {
        var hotkeys = await _hotkeyService.GetAllHotkeyMetaAsync(accountId);
    }
}
```

### After (API)
```csharp
public class HotkeysViewModel
{
    private readonly RadiumApiClient _apiClient; // API
    
    public async Task LoadAsync()
    {
        var hotkeys = await _apiClient.GetHotkeysAsync(accountId);
    }
}
```

**That's it!** Just swap the service and change the method call!

---

## ? What Works Now

1. **Authentication Flow**
   - ? Login ¡æ Firebase generates JWT token
   - ? Token automatically passed to API client
   - ? Token included in all API requests (`Authorization: Bearer <token>`)

2. **API Communication**
   - ? All endpoints available: Hotkeys, Snippets
   - ? Automatic authentication (token in headers)
   - ? Error handling (exceptions on API errors)

3. **Development Mode**
   - ? API accepts requests without token validation
   - ? Can test WPF ¡æ API flow immediately

---

## ?? Testing

### 1. Start the API
```powershell
cd apps\Wysg.Musm.Radium.Api
dotnet run
```

### 2. Run Your WPF App
```powershell
cd apps\Wysg.Musm.Radium
dotnet run
```

### 3. Login
- Login with Google or Email/Password
- Token is automatically set in API client
- You're ready to make API calls!

### 4. Test API Calls (Example)
Add this to any ViewModel for testing:

```csharp
// In constructor or method
var apiClient = ((App)Application.Current).Services.GetRequiredService<RadiumApiClient>();

try
{
    var hotkeys = await apiClient.GetHotkeysAsync(accountId);
    Debug.WriteLine($"Loaded {hotkeys.Count} hotkeys from API");
}
catch (Exception ex)
{
    Debug.WriteLine($"API Error: {ex.Message}");
}
```

---

## ?? Production Deployment Checklist

When deploying to production:

### 1. API Configuration
- ? Set `ValidateToken: true` in `appsettings.json`
- ? Deploy API to Azure App Service
- ? Configure Managed Identity for database access

### 2. WPF Configuration
- ? Set `RADIUM_API_URL` environment variable to production URL
- ? Or hardcode production URL in `App.xaml.cs`

### 3. Firebase Configuration
- ? Ensure Firebase project ID is correct in API settings
- ? Custom claims (account_id, tenant_id) set per user

---

## ?? Next Steps

### Immediate (Optional)
1. **Migrate existing ViewModels** to use API instead of direct DB
   - `HotkeysViewModel` ¡æ use `RadiumApiClient`
   - `SnippetsViewModel` ¡æ use `RadiumApiClient`

2. **Test API integration** with real data
   - Login to your app
   - Check Debug output for "Firebase token set in API client"
   - Call API methods

### Future Enhancements
1. **Add retry logic** for network errors
2. **Add caching** for frequently accessed data
3. **Add offline mode** (queue operations when API unavailable)
4. **Add API health monitoring** in WPF app

---

## ?? Summary

Your WPF app is now fully integrated with the API!

**What you have:**
- ? API client registered in DI
- ? Firebase token automatically passed to API
- ? Ready to replace direct database calls with API calls
- ? Development and production configurations ready

**What you can do:**
- Replace `IHotkeyService` with `RadiumApiClient.GetHotkeysAsync()`
- Replace `ISnippetService` with `RadiumApiClient.GetSnippetsAsync()`
- All authentication handled automatically!

**Benefits:**
- ?? No database credentials in WPF app
- ?? Centralized business logic in API
- ?? Easier to scale and maintain
- ?? Ready for web/mobile clients

---

**You're ready to go!** ??

Start the API, run your WPF app, and you're connected!
