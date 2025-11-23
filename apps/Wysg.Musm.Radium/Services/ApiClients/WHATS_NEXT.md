# ?? WHAT'S NEXT - Testing & Migration Roadmap

## ? CURRENT STATUS: Ready for Testing!

**Build**: ? 網萄 撩奢  
**API**: ? Running on `http://localhost:5205`  
**Integration**: ? Complete  

---

## ?? IMMEDIATE NEXT STEPS (Next 30 Minutes)

### Step 1: Verify API Clients Work (5 min)

**Action**: Run WPF app and check Debug output

1. Press **F5** to start the app
2. Open **Debug Output Window** (View ⊥ Output ⊥ Show output from: Debug)
3. Look for these messages:

```
[DI] API Settings: BaseUrl=http://localhost:5205
[DI] Registering API clients with base URL: http://localhost:5205
[DI] ? All 6 API clients registered successfully
```

**? Success**: You see all 3 messages  
**? Problem**: Messages missing ⊥ Check `appsettings.Development.json` exists

---

### Step 2: Set Token on Login (10 min)

**Action**: Update your login ViewModel

#### Find Your Login ViewModel

Search for files containing "Login" and "Sign":
- `SplashLoginViewModel.cs`
- `LoginViewModel.cs`
- Or check `Views\SplashLoginWindow.xaml` DataContext

#### Add ApiTokenManager

```csharp
public class YourLoginViewModel
{
    private readonly IAuthService _authService;
    private readonly ITenantContext _tenantContext;
    private readonly ApiTokenManager _apiTokenManager; // ? ADD THIS
    
    public YourLoginViewModel(
        IAuthService authService,
        ITenantContext tenantContext,
        ApiTokenManager apiTokenManager) // ? ADD THIS
    {
        _authService = authService;
        _tenantContext = tenantContext;
        _apiTokenManager = apiTokenManager; // ? ADD THIS
    }
    
    private async Task OnLoginAsync()
    {
        var authResult = await _authService.SignInWithEmailPasswordAsync(email, password);
        
        if (authResult.Success)
        {
            // ? ADD THIS ONE LINE
            _apiTokenManager.SetAuthToken(authResult.IdToken);
            
            // ... rest of your login code
        }
    }
}
```

#### Test It

1. Login to your app
2. Check Debug output for:
```
[ApiTokenManager] ? Token set on 6/6 API clients
```

**? Success**: Token message appears  
**? Problem**: "ApiTokenManager not found" ⊥ Check constructor injection

---

### Step 3: Run API Test Window (15 min)

**Action**: Add test button and run tests

#### Option A: Temporary Test Button

Add to `MainWindow.xaml` (anywhere in your UI):
```xml
<Button Content="?? Test API" 
        Click="TestApi_Click"
        HorizontalAlignment="Right"
        VerticalAlignment="Top"
        Margin="10"/>
```

Add to `MainWindow.xaml.cs`:
```csharp
private void TestApi_Click(object sender, RoutedEventArgs e)
{
    var testWindow = new Views.ApiTestWindow();
    testWindow.Show();
}
```

#### Run Tests

1. **Start app** and login
2. **Click "?? Test API"** button
3. In test window, click **"Quick Test"**
   - Should see: ? API is healthy
4. Click **"Full Test Suite"**
   - Should see all 5 tests pass:
     - ? Health Check
     - ? User Settings API
     - ? Phrases API
     - ? Hotkeys API
     - ? Snippets API

**? Success**: All tests show ?  
**? Problem**: Tests fail ⊥ Check troubleshooting below

---

## ?? TROUBLESHOOTING GUIDE

### Problem 1: "ApiTokenManager not found" in constructor

**Cause**: DI not finding the service

**Solution**:
1. Check `App.xaml.cs` has:
   ```csharp
   services.AddSingleton<ApiTokenManager>();
   ```
2. Rebuild solution
3. Restart app

---

### Problem 2: "401 Unauthorized" in API tests

**Cause**: Token not set

**Solution**:
1. Make sure you **logged in first**
2. Check Debug output for:
   ```
   [ApiTokenManager] ? Token set on 6/6 API clients
   ```
3. If missing, token wasn't set in login flow

---

### Problem 3: "Cannot connect to API"

**Cause**: API not running

**Solution**:
1. Open terminal in API project:
   ```bash
   cd apps\Wysg.Musm.Radium.Api
   dotnet run
   ```
2. Wait for:
   ```
   Now listening on: http://localhost:5205
   ```
3. Test health endpoint:
   ```bash
   curl http://localhost:5205/health
   # Should return: Healthy
   ```

---

### Problem 4: Test window doesn't open

**Cause**: Build issue or wrong namespace

**Solution**:
1. Check `Views\ApiTestWindow.xaml` exists
2. Verify namespace in button click:
   ```csharp
   var testWindow = new Wysg.Musm.Radium.Views.ApiTestWindow();
   ```
3. Rebuild solution

---

## ?? TESTING DECISION TREE

```
Start Testing
    弛
    戍式⊥ API Running? 式式No式式⊥ Start API (dotnet run)
    弛        弛
    弛       Yes
    弛        弛
    戍式⊥ App Starts? 式式No式式⊥ Check build errors
    弛        弛
    弛       Yes
    弛        弛
    戍式⊥ See DI messages? 式式No式式⊥ Check App.xaml.cs
    弛        弛
    弛       Yes
    弛        弛
    戍式⊥ Can Login? 式式No式式⊥ Fix login first
    弛        弛
    弛       Yes
    弛        弛
    戍式⊥ Token Set? 式式No式式⊥ Add _apiTokenManager.SetAuthToken()
    弛        弛
    弛       Yes
    弛        弛
    戍式⊥ Quick Test Pass? 式式No式式⊥ Check API connection
    弛        弛
    弛       Yes
    弛        弛
    戌式⊥ Full Test Pass? 式式No式式⊥ Check token + login
             弛
            Yes
             弛
        ? READY FOR MIGRATION!
```

---

## ?? AFTER SUCCESSFUL TESTING

Once all tests pass, you're ready to:

### Phase 1: Migrate One Service (Day 1 - 2 hours)

**Recommended**: Start with User Settings

1. Find where `IReportifySettingsService` is used
2. Replace implementation with `ApiReportifySettingsService`
3. Test settings load/save works
4. Keep old code as backup

**Example**:
```csharp
// In App.xaml.cs - Already done!
services.AddScoped<IReportifySettingsService, ApiReportifySettingsService>();
```

**Test**: Load/save settings ⊥ verify no errors

---

### Phase 2: Migrate More Services (Week 1 - 5 hours)

**Order** (easiest to hardest):
1. ? User Settings (example already done)
2. Hotkeys
3. Snippets
4. Phrases
5. SNOMED

**Pattern for each**:
```csharp
// 1. Create adapter (if interface doesn't match exactly)
public class ApiHotkeyService : IHotkeyService
{
    private readonly IHotkeysApiClient _apiClient;
    private readonly ITenantContext _tenantContext;
    
    public async Task<List<Hotkey>> GetAllAsync()
    {
        var accountId = _tenantContext.AccountId;
        var dtos = await _apiClient.GetAllAsync(accountId);
        return dtos.Select(MapToModel).ToList();
    }
}

// 2. Register in DI
services.AddScoped<IHotkeyService, ApiHotkeyService>();

// 3. Test thoroughly
// 4. Remove old code when confident
```

---

### Phase 3: Clean Up (Week 2 - 2 hours)

**Goal**: Remove direct database dependencies

1. Search for `ICentralDataSourceProvider` usage
2. Replace with API clients
3. Remove packages:
   ```bash
   # Keep these for now (local DB may still be needed)
   # dotnet remove package Npgsql
   # dotnet remove package Microsoft.Data.SqlClient
   ```
4. Remove connection strings from `appsettings.json` (or keep as backup)

---

### Phase 4: Deploy to Azure (Week 2 - 3 hours)

**Goal**: Make API accessible from anywhere

1. **Create Azure App Service**
   ```bash
   az webapp create \
     --name radium-api \
     --resource-group YOUR_RG \
     --plan YOUR_PLAN \
     --runtime "DOTNETCORE:9.0"
   ```

2. **Deploy API**
   ```bash
   cd apps\Wysg.Musm.Radium.Api
   dotnet publish -c Release -o ./publish
   az webapp deployment source config-zip \
     --resource-group YOUR_RG \
     --name radium-api \
     --src publish.zip
   ```

3. **Update WPF appsettings.json**
   ```json
   {
     "ApiSettings": {
       "BaseUrl": "https://radium-api.azurewebsites.net"
     }
   }
   ```

4. **Test from WPF app**

---

## ?? RECOMMENDED TIMELINE

### This Week:
- **Day 1** (Today): Verify all tests pass ?
- **Day 2**: Migrate User Settings (test thoroughly)
- **Day 3**: Migrate Hotkeys + Snippets
- **Day 4**: Migrate Phrases
- **Day 5**: Migrate SNOMED

### Next Week:
- **Day 1-2**: Test all features with API
- **Day 3**: Remove old DB code
- **Day 4-5**: Deploy to Azure + test production

### Goal: **10 days to full production deployment**

---

## ?? SUCCESS CRITERIA

### Immediate (Today):
- [x] App builds successfully ?
- [ ] App starts without errors
- [ ] See DI registration messages
- [ ] Can login
- [ ] Token gets set
- [ ] Quick test passes
- [ ] Full test passes

### This Week:
- [ ] All services migrated to API
- [ ] No direct DB calls in WPF
- [ ] All features work via API
- [ ] No 401 errors
- [ ] Performance acceptable

### Next Week:
- [ ] API deployed to Azure
- [ ] WPF connects to production API
- [ ] Multiple users can use app
- [ ] Monitoring in place

---

## ?? PRO TIPS

### Tip 1: Keep Both Implementations During Migration

```csharp
// Register both old and new
services.AddScoped<IHotkeyService, AzureSqlHotkeyService>(); // Old (backup)
services.AddScoped<IHotkeyService, ApiHotkeyService>(); // New (active)

// Switch with feature flag
var useApi = Environment.GetEnvironmentVariable("USE_API") == "1";
if (useApi)
{
    services.AddScoped<IHotkeyService, ApiHotkeyService>();
}
else
{
    services.AddScoped<IHotkeyService, AzureSqlHotkeyService>();
}
```

### Tip 2: Test After Each Service Migration

Don't migrate everything at once. Test thoroughly after each service.

### Tip 3: Monitor API Performance

Add logging to see API response times:
```csharp
var sw = Stopwatch.StartNew();
var data = await _apiClient.GetAllAsync(accountId);
sw.Stop();
Debug.WriteLine($"API call took {sw.ElapsedMilliseconds}ms");
```

### Tip 4: Use Test Window Frequently

Keep the test window open during development. Run tests after changes.

---

## ?? GET HELP

### If Tests Fail:
1. Read error message in test window
2. Check Debug output
3. Review troubleshooting section above
4. Check documentation:
   - `IMPLEMENTATION_SUCCESS.md`
   - `LOCALHOST_TESTING_GUIDE.md`
   - `QUICKSTART.md`

### Common Issues Already Documented:
- ? API not running
- ? Token not set
- ? 401 Unauthorized
- ? Connection refused
- ? Service not registered

**All with solutions!**

---

## ?? YOU'RE READY!

**Current Status**:
- ? API running
- ? API clients built
- ? Integration complete
- ? Build successful
- ? Tests ready
- ? Documentation complete

**Next Action**:
1. **Run the app** (F5)
2. **Check Debug output**
3. **Login**
4. **Run tests**

**Time to First Test**: **5 minutes**  
**Time to Full Migration**: **5-10 days**  

---

**LET'S START TESTING! ??**

Open `IMPLEMENTATION_SUCCESS.md` for the complete testing guide!
