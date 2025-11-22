# Connecting WPF App to API

## Quick Integration Guide

### Step 1: Add API Client to Your DI Container

In your `App.xaml.cs` or wherever you set up services:

```csharp
// Add this to your service registration
var apiClient = new RadiumApiClient("http://localhost:5205"); // or your production URL
services.AddSingleton(apiClient);
```

### Step 2: Use in MainViewModel (or wherever you handle login)

```csharp
public class MainViewModel
{
    private readonly GoogleOAuthAuthService _authService;
    private readonly RadiumApiClient _apiClient;
    
    public MainViewModel(GoogleOAuthAuthService authService, RadiumApiClient apiClient)
    {
        _authService = authService;
        _apiClient = apiClient;
    }
    
    public async Task LoginAsync()
    {
        // Your existing login code
        var result = await _authService.SignInWithGoogleAsync();
        
        if (result.Success)
        {
            // ? NEW: Set the Firebase token in the API client
            _apiClient.SetAuthToken(result.IdToken);  // This is the JWT!
            
            // Now you can call the API
            var hotkeys = await _apiClient.GetHotkeysAsync(accountId);
            
            // Continue with your existing code...
        }
    }
    
    public async Task LogoutAsync()
    {
        // Your existing logout code
        await _authService.SignOutAsync();
        
        // ? NEW: Clear the token
        _apiClient.ClearAuthToken();
    }
}
```

### Step 3: Replace Direct Database Calls

**Before (direct database access):**
```csharp
var service = new AzureSqlHotkeyService(_settings);
var hotkeys = await service.GetAllHotkeyMetaAsync(accountId);
```

**After (API access):**
```csharp
var hotkeys = await _apiClient.GetHotkeysAsync(accountId);
```

---

## Configuration

### Development (Local API)
```csharp
var apiClient = new RadiumApiClient("http://localhost:5205");
```

### Production (Azure App Service)
```csharp
var apiClient = new RadiumApiClient("https://api-wysg-musm.azurewebsites.net");
```

Or use app settings:
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5205"
  }
}
```

---

## Testing Without Firebase Token (Development)

Since your API has `ValidateToken: false` in development:

```csharp
// In development, you can test without setting a token
var apiClient = new RadiumApiClient("http://localhost:5205");
// No need to call SetAuthToken() - API accepts requests without token
var hotkeys = await apiClient.GetHotkeysAsync(1);
```

---

## Example: Migrating Hotkey Management

### Before (Direct DB)
```csharp
public class HotkeysViewModel
{
    private readonly IHotkeyService _hotkeyService; // Direct DB service
    
    public async Task LoadHotkeysAsync()
    {
        var hotkeys = await _hotkeyService.GetAllHotkeyMetaAsync(accountId);
    }
}
```

### After (API)
```csharp
public class HotkeysViewModel
{
    private readonly RadiumApiClient _apiClient; // API client
    
    public async Task LoadHotkeysAsync()
    {
        var hotkeys = await _apiClient.GetHotkeysAsync(accountId);
    }
}
```

---

## Benefits

? **No database credentials in WPF app**  
? **Firebase handles authentication** (you already have this!)  
? **Easy to scale** - Add web/mobile clients later  
? **Centralized business logic** - All in API  
? **Secure** - Database behind API firewall  

---

## Important Notes

1. **Token Refresh**: Your `GoogleOAuthAuthService` already has `RefreshAsync()` - use it to refresh the token when it expires (typically 1 hour)

2. **Error Handling**: The API client throws exceptions on errors - catch them in your ViewModels

3. **Development vs Production**: 
   - Development: API accepts requests without token
   - Production: Set `ValidateToken: true` and API requires valid Firebase JWT
