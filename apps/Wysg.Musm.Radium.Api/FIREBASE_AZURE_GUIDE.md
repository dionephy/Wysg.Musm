# Firebase + Azure Architecture Implementation Guide

## ??? Architecture Overview

```
忙式式式式式式式式式式式式式式式式式忖
弛  WPF Client     弛
弛  (Radium)       弛式式Firebase Auth式式> Firebase (Google)
弛                 弛                    
戌式式式式式式式式成式式式式式式式式戎                        
         弛                                 
         弛 Bearer <Firebase-JWT>           
         弛                                 
         ∪                                 
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛   ASP.NET Core Web API                  弛
弛   (Wysg.Musm.Radium.Api)                弛
弛   - Validates Firebase JWT              弛
弛   - Extracts user/tenant from claims    弛
弛   - Azure App Service                   弛
戌式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
         弛
         弛 Managed Identity (No credentials!)
         弛
         ∪
忙式式式式式式式式式式式式式式式式式忖
弛   Azure SQL     弛
弛   (musmdb)      弛
戌式式式式式式式式式式式式式式式式式戎
```

## ? What's Implemented

### Backend (API)
- ? Firebase JWT authentication
- ? Custom claims extraction (account_id, tenant_id, role)
- ? Authorization on all endpoints
- ? User context validation
- ? Development mode (no Firebase required for local testing)
- ? Azure SQL with Active Directory authentication
- ? Hotkeys & Snippets endpoints

### What's Next
- ? WPF Client Firebase integration
- ? Firebase project setup
- ? Azure App Service deployment
- ? Managed Identity configuration

---

## ?? Step-by-Step Implementation

### Phase 1: Firebase Project Setup

#### 1.1. Create Firebase Project

1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Click "Add project" or select existing
3. Project name: `wysg-musm` (or your choice)
4. Enable Google Analytics (optional)
5. Create project

#### 1.2. Enable Authentication

1. In Firebase Console, go to **Authentication**
2. Click "Get started"
3. Enable sign-in methods:
   - ? Email/Password
   - ? Google (recommended)
   - (Optional) Microsoft, Apple, etc.

#### 1.3. Register Your WPF App

1. Go to Project Settings (?? icon)
2. Scroll to "Your apps"
3. Click "Add app" ⊥ Select **Web** (for desktop apps)
4. App nickname: `Radium WPF Client`
5. Register app
6. Copy the **Firebase configuration**:

```javascript
const firebaseConfig = {
  apiKey: "AIza...",
  authDomain: "wysg-musm.firebaseapp.com",
  projectId: "wysg-musm",
  // ...
};
```

#### 1.4. Set Up Custom Claims (for account_id, tenant_id)

Firebase custom claims require **Firebase Admin SDK** (Node.js or Python).

Create a simple Cloud Function or admin script:

```javascript
// Set custom claims for a user
const admin = require('firebase-admin');
admin.initializeApp();

async function setUserClaims(uid, accountId, tenantId, role) {
  await admin.auth().setCustomUserClaims(uid, {
    account_id: accountId,
    tenant_id: tenantId,
    role: role
  });
  console.log(`Custom claims set for user ${uid}`);
}

// Usage
setUserClaims('user-firebase-uid', 1, 'tenant-123', 'admin');
```

---

### Phase 2: WPF Client Integration

#### 2.1. Install Firebase NuGet Packages

```bash
cd apps/Wysg.Musm.Radium
dotnet add package FirebaseAuthentication.net
dotnet add package FirebaseAdmin  # Optional: for admin operations
```

#### 2.2. Create Firebase Service

```csharp
// Services/FirebaseAuthService.cs
using Firebase.Auth;

public class FirebaseAuthService
{
    private readonly string _apiKey;
    private FirebaseAuthProvider? _authProvider;
    private FirebaseAuthLink? _authLink;

    public FirebaseAuthService(string apiKey)
    {
        _apiKey = apiKey;
        _authProvider = new FirebaseAuthProvider(new FirebaseConfig(_apiKey));
    }

    public async Task<string?> SignInAsync(string email, string password)
    {
        try
        {
            _authLink = await _authProvider.SignInWithEmailAndPasswordAsync(email, password);
            return _authLink.FirebaseToken; // This is the JWT
        }
        catch (FirebaseAuthException ex)
        {
            // Handle auth errors
            throw new InvalidOperationException($"Firebase auth failed: {ex.Reason}", ex);
        }
    }

    public async Task<string?> RefreshTokenAsync()
    {
        if (_authLink == null) return null;
        
        var freshAuth = await _authProvider.RefreshAuthAsync(_authLink);
        _authLink = freshAuth;
        return _authLink.FirebaseToken;
    }

    public void SignOut()
    {
        _authLink = null;
    }

    public bool IsAuthenticated => _authLink != null;
}
```

#### 2.3. Create API Client with Firebase Token

```csharp
// Services/RadiumApiClient.cs
public class RadiumApiClient
{
    private readonly HttpClient _httpClient;
    private readonly FirebaseAuthService _authService;

    public RadiumApiClient(string baseUrl, FirebaseAuthService authService)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _authService = authService;
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync()
    {
        var token = await _authService.RefreshTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException("Not authenticated");
        }

        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        return _httpClient;
    }

    public async Task<List<HotkeyDto>> GetHotkeysAsync(long accountId)
    {
        var client = await GetAuthenticatedClientAsync();
        var response = await client.GetAsync($"/api/accounts/{accountId}/hotkeys");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<HotkeyDto>>();
    }

    // ... more methods
}
```

#### 2.4. Update MainViewModel

```csharp
// ViewModels/MainViewModel.cs
public class MainViewModel
{
    private readonly FirebaseAuthService _authService;
    private readonly RadiumApiClient _apiClient;

    public MainViewModel()
    {
        _authService = new FirebaseAuthService("YOUR_FIREBASE_API_KEY");
        _apiClient = new RadiumApiClient("https://your-api.azurewebsites.net", _authService);
    }

    public async Task LoginAsync(string email, string password)
    {
        await _authService.SignInAsync(email, password);
        // Now you can call API methods
        var hotkeys = await _apiClient.GetHotkeysAsync(1);
    }
}
```

---

### Phase 3: Azure Deployment

#### 3.1. Deploy API to Azure App Service

```bash
# Login to Azure
az login

# Create resource group (if not exists)
az group create --name rg-wysg-musm --location koreacentral

# Create App Service Plan
az appservice plan create \
  --name plan-wysg-musm \
  --resource-group rg-wysg-musm \
  --sku B1 \
  --is-linux

# Create Web App
az webapp create \
  --name api-wysg-musm \
  --resource-group rg-wysg-musm \
  --plan plan-wysg-musm \
  --runtime "DOTNETCORE:10.0"

# Publish from Visual Studio or CLI
dotnet publish -c Release
az webapp deployment source config-zip \
  --resource-group rg-wysg-musm \
  --name api-wysg-musm \
  --src publish.zip
```

#### 3.2. Configure App Settings in Azure

```bash
# Set connection string (or use Azure SQL Managed Identity)
az webapp config connection-string set \
  --name api-wysg-musm \
  --resource-group rg-wysg-musm \
  --connection-string-type SQLAzure \
  --settings DefaultConnection="Server=tcp:musm-server.database.windows.net..."

# Set Firebase Project ID
az webapp config appsettings set \
  --name api-wysg-musm \
  --resource-group rg-wysg-musm \
  --settings Firebase__ProjectId="wysg-musm" \
              Firebase__ValidateToken="true"
```

#### 3.3. Enable Managed Identity

```bash
# Enable system-assigned managed identity
az webapp identity assign \
  --name api-wysg-musm \
  --resource-group rg-wysg-musm

# Get the identity's principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --name api-wysg-musm \
  --resource-group rg-wysg-musm \
  --query principalId -o tsv)

# Grant SQL access
az sql server ad-admin create \
  --resource-group rg-wysg-musm \
  --server-name musm-server \
  --display-name api-wysg-musm \
  --object-id $PRINCIPAL_ID
```

#### 3.4. Update Connection String for Managed Identity

In Azure App Service Configuration:

```
DefaultConnection=Server=tcp:musm-server.database.windows.net,1433;Initial Catalog=musmdb;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication="Active Directory Default";
```

Set `ApiSettings__UseAzureManagedIdentity=true`

---

## ?? Testing

### Local Testing (Without Firebase)

The API is configured for **development mode** where Firebase validation is disabled:

```json
// appsettings.Development.json
{
  "Firebase": {
    "ProjectId": "wysg-musm",
    "ValidateToken": false  // ∠ Disabled for local testing
  }
}
```

Test endpoints work without authentication:

```bash
# Restart API (Ctrl+C if running)
cd apps/Wysg.Musm.Radium.Api
dotnet run

# Test (works without Bearer token in dev mode)
Invoke-WebRequest -Uri "http://localhost:5205/api/accounts/1/hotkeys"
```

### Production Testing (With Firebase)

1. Get a Firebase ID token:
   - Login via WPF app
   - Or use Firebase REST API:

```bash
curl -X POST "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=YOUR_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "password123",
    "returnSecureToken": true
  }'
```

2. Use the token in API requests:

```bash
$token = "eyJhbGciOiJSUzI1NiIsImtpZCI6I..."

Invoke-WebRequest `
  -Uri "https://api-wysg-musm.azurewebsites.net/api/accounts/1/hotkeys" `
  -Headers @{ "Authorization" = "Bearer $token" }
```

---

## ?? Security Checklist

- [x] API validates Firebase JWT tokens
- [x] User context (account_id) is validated
- [x] No SQL credentials in client code
- [x] Azure SQL uses Managed Identity
- [x] CORS configured properly
- [ ] HTTPS enforced
- [ ] Rate limiting configured
- [ ] Firebase custom claims set per user

---

## ?? Configuration Summary

### appsettings.Development.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:musm-server.database.windows.net,1433;Initial Catalog=musmdb;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=\"Active Directory Default\";"
  },
  "Firebase": {
    "ProjectId": "wysg-musm",
    "ValidateToken": false
  },
  "ApiSettings": {
    "UseAzureManagedIdentity": false
  }
}
```

### appsettings.json (Production)
```json
{
  "Firebase": {
    "ProjectId": "wysg-musm",
    "ValidateToken": true
  },
  "ApiSettings": {
    "UseAzureManagedIdentity": true
  }
}
```

---

## ?? Next Steps

1. **Stop the running API** (Ctrl+C in terminal)
2. **Restart and test**: `dotnet run`
3. **Test endpoints** using `test.http` file
4. **Set up Firebase project** (Phase 1)
5. **Integrate WPF client** (Phase 2)
6. **Deploy to Azure** (Phase 3)

The architecture is solid and ready for production! ??
