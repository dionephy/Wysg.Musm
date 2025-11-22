# ?? Quick Reference: WPF to API Integration

## ? Integration Status

| Component | Status | Notes |
|-----------|--------|-------|
| **API Client** | ? Done | Registered in DI |
| **Firebase Token** | ? Done | Auto-set on login |
| **Email Login** | ? Done | Token passed to API |
| **Google Login** | ? Done | Token passed to API |
| **Silent Refresh** | ? Done | Token passed to API |

---

## ?? Quick Start

### 1. Start API
```powershell
cd apps\Wysg.Musm.Radium.Api
dotnet run
```

### 2. Run WPF App
```powershell
cd apps\Wysg.Musm.Radium
dotnet run
```

### 3. Login
- Use any login method (Google/Email)
- Token is automatically set!

---

## ?? Using the API Client

### In ViewModels
```csharp
public MyViewModel(RadiumApiClient apiClient, ITenantContext tenant)
{
    _apiClient = apiClient;
    _tenant = tenant;
}

public async Task LoadDataAsync()
{
    // Get hotkeys
    var hotkeys = await _apiClient.GetHotkeysAsync(_tenant.TenantId);
    
    // Get snippets
    var snippets = await _apiClient.GetSnippetsAsync(_tenant.TenantId);
}
```

### Quick Test (Anywhere)
```csharp
var apiClient = ((App)Application.Current).Services
    .GetRequiredService<RadiumApiClient>();

var hotkeys = await apiClient.GetHotkeysAsync(1);
Debug.WriteLine($"Loaded {hotkeys.Count} hotkeys");
```

---

## ?? Configuration

### Development
Default: `http://localhost:5205` ?

### Production
Set environment variable:
```powershell
$env:RADIUM_API_URL = "https://your-api.azurewebsites.net"
```

---

## ?? Full Documentation

See: `WPF_API_INTEGRATION_COMPLETE.md`

---

**That's it! You're connected!** ??
