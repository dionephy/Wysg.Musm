# WPF API Client Registration Guide

## Overview

This guide shows how to register all API clients in your WPF application's dependency injection container.

---

## Step 1: Update appsettings.json

Create or update `appsettings.json` in your Radium WPF project:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://your-api.azurewebsites.net",
    "TimeoutSeconds": 30,
    "EnableRetry": true,
    "MaxRetryAttempts": 3,
    "EnableOfflineCache": true,
    "CacheDurationMinutes": 5
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "System": "Warning"
    }
  }
}
```

**Development Override** (`appsettings.Development.json`):

```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5205"
  }
}
```

> **Note**: Your API is running on `http://localhost:5205` (not HTTPS). Update this if your API URL changes.

---

## Step 2: Register Services in App.xaml.cs

Update your `App.xaml.cs` to register all API clients:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Windows;
using Wysg.Musm.Radium.Configuration;
using Wysg.Musm.Radium.Services;
using Wysg.Musm.Radium.Services.ApiClients;

namespace Wysg.Musm.Radium
{
    public partial class App : Application
    {
        private IHost? _host;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    // Load configuration
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", 
                                     optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services, context.Configuration);
                })
                .Build();

            _host.Start();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Register configuration
            var apiSettings = configuration.GetSection("ApiSettings").Get<ApiSettings>() 
                           ?? new ApiSettings();
            services.AddSingleton(apiSettings);

            // Configure HttpClient
            services.AddHttpClient("RadiumApi", client =>
            {
                client.BaseAddress = new Uri(apiSettings.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(apiSettings.TimeoutSeconds);
                client.DefaultRequestHeaders.Add("User-Agent", "Wysg.Musm.Radium/1.0");
            });

            // Register API Clients
            RegisterApiClients(services, apiSettings.BaseUrl);

            // Register existing services
            services.AddSingleton<IAuthService, GoogleOAuthAuthService>();
            services.AddSingleton<ITenantContext, TenantContext>();

            // Replace old database services with API services
            services.AddScoped<IReportifySettingsService, ApiReportifySettingsService>();
            // Add more service replacements as you migrate...

            // Register windows
            services.AddTransient<MainWindow>();
        }

        private void RegisterApiClients(IServiceCollection services, string baseUrl)
        {
            // User Settings API Client
            services.AddScoped<IUserSettingsApiClient>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("RadiumApi");
                return new UserSettingsApiClient(httpClient, baseUrl);
            });

            // Phrases API Client
            services.AddScoped<IPhrasesApiClient>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("RadiumApi");
                return new PhrasesApiClient(httpClient, baseUrl);
            });

            // Hotkeys API Client
            services.AddScoped<IHotkeysApiClient>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("RadiumApi");
                return new HotkeysApiClient(httpClient, baseUrl);
            });

            // Snippets API Client
            services.AddScoped<ISnippetsApiClient>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("RadiumApi");
                return new SnippetsApiClient(httpClient, baseUrl);
            });

            // SNOMED API Client
            services.AddScoped<ISnomedApiClient>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("RadiumApi");
                return new SnomedApiClient(httpClient, baseUrl);
            });

            // Exported Reports API Client
            services.AddScoped<IExportedReportsApiClient>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("RadiumApi");
                return new ExportedReportsApiClient(httpClient, baseUrl);
            });
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host?.StopAsync().GetAwaiter().GetResult();
            _host?.Dispose();
            base.OnExit(e);
        }
    }
}
```

---

## Step 3: Update Existing Services

### Example: Migrate PhraseService

**Before** (Direct Database):
```csharp
public class PhraseService : IPhraseService
{
    private readonly ICentralDataSourceProvider _ds;

    public async Task<IReadOnlyList<string>> GetPhrasesForAccountAsync(long accountId)
    {
        await using var con = _ds.Central.CreateConnection();
        await con.OpenAsync();
        const string sql = "SELECT text FROM radium.phrase WHERE account_id = @aid";
        // ... direct SQL query
    }
}
```

**After** (API):
```csharp
public class ApiPhraseService : IPhraseService
{
    private readonly IPhrasesApiClient _apiClient;

    public ApiPhraseService(IPhrasesApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IReadOnlyList<string>> GetPhrasesForAccountAsync(long accountId)
    {
        var phrases = await _apiClient.GetAllAsync(accountId, activeOnly: true);
        return phrases.Select(p => p.Text).ToList();
    }
}
```

### Register the Replacement

In `ConfigureServices`:
```csharp
// Replace old service with API version
services.AddScoped<IPhraseService, ApiPhraseService>();
```

---

## Step 4: Add Offline Support (Optional)

Create a caching wrapper for offline scenarios:

```csharp
public class CachedPhrasesApiClient : IPhrasesApiClient
{
    private readonly IPhrasesApiClient _innerClient;
    private readonly Dictionary<long, List<PhraseDto>> _cache = new();
    private readonly object _lock = new();

    public CachedPhrasesApiClient(IPhrasesApiClient innerClient)
    {
        _innerClient = innerClient;
    }

    public async Task<List<PhraseDto>> GetAllAsync(long accountId, bool activeOnly = false)
    {
        try
        {
            // Try API first
            var phrases = await _innerClient.GetAllAsync(accountId, activeOnly);
            
            // Update cache
            lock (_lock)
            {
                _cache[accountId] = phrases;
            }
            
            return phrases;
        }
        catch (HttpRequestException)
        {
            // Fallback to cache if API fails
            lock (_lock)
            {
                if (_cache.TryGetValue(accountId, out var cached))
                {
                    Debug.WriteLine("[CachedPhrasesApiClient] Using cached data (offline)");
                    return cached;
                }
            }
            throw;
        }
    }

    // Implement other interface methods similarly...
}
```

Register the cached version:
```csharp
services.AddScoped<IPhrasesApiClient>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("RadiumApi");
    var innerClient = new PhrasesApiClient(httpClient, baseUrl);
    return new CachedPhrasesApiClient(innerClient); // Wrap with caching
});
```

---

## Step 5: Update ViewModels

Inject API clients into your ViewModels:

```csharp
public class PhraseManagementViewModel : ViewModelBase
{
    private readonly IPhrasesApiClient _phrasesApi;
    private readonly ITenantContext _tenantContext;

    public PhraseManagementViewModel(
        IPhrasesApiClient phrasesApi, 
        ITenantContext tenantContext)
    {
        _phrasesApi = phrasesApi;
        _tenantContext = tenantContext;
    }

    public async Task LoadPhrasesAsync()
    {
        var accountId = _tenantContext.AccountId; // ? Fixed: Use AccountId property
        if (accountId <= 0) return;

        try
        {
            var phrases = await _phrasesApi.GetAllAsync(accountId, activeOnly: true);
            Phrases = new ObservableCollection<PhraseDto>(phrases);
        }
        catch (Exception ex)
        {
            // Handle error (show message to user)
            Debug.WriteLine($"Error loading phrases: {ex.Message}");
        }
    }

    public async Task AddPhraseAsync(string text)
    {
        var accountId = _tenantContext.AccountId; // ? Fixed: Use AccountId property
        if (accountId <= 0) return;

        try
        {
            var phrase = await _phrasesApi.UpsertAsync(accountId, text);
            Phrases.Add(phrase);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error adding phrase: {ex.Message}");
        }
    }
}
```

---

## Step 6: Remove Old Dependencies

Once migration is complete:

1. **Remove NuGet packages**:
   ```bash
   dotnet remove package Npgsql
   dotnet remove package Microsoft.Data.SqlClient
   ```

2. **Remove connection strings** from `appsettings.json`

3. **Remove old services**:
   - `ICentralDataSourceProvider`
   - Direct database repositories
   - Connection string configurations

4. **Update project references** if needed

---

## Step 7: Testing

### Manual Testing
1. Start the API server
2. Start the WPF application
3. Verify authentication works
4. Test each feature (phrases, hotkeys, snippets, etc.)
5. Monitor Debug output for API calls

### Unit Testing
```csharp
[Fact]
public async Task GetPhrasesAsync_CallsApiClient()
{
    // Arrange
    var mockApiClient = new Mock<IPhrasesApiClient>();
    mockApiClient.Setup(x => x.GetAllAsync(1, true))
                 .ReturnsAsync(new List<PhraseDto> { new() { Text = "test" } });
    
    var service = new ApiPhraseService(mockApiClient.Object);

    // Act
    var result = await service.GetPhrasesForAccountAsync(1);

    // Assert
    Assert.Single(result);
    Assert.Equal("test", result[0]);
}
```

---

## Troubleshooting

### Issue: 401 Unauthorized
**Cause**: Firebase token not set or expired  
**Fix**: Set token after login using `SetAuthToken()`:
```csharp
// After successful login
if (authResult.Success)
{
    var apiClient = _serviceProvider.GetService<IUserSettingsApiClient>() as ApiClientBase;
    apiClient?.SetAuthToken(authResult.IdToken);
    // Repeat for all API clients
}
```

### Issue: Cannot connect to API
**Cause**: API URL incorrect or API not running  
**Fix**: 
1. Verify API is running: `http://localhost:5205/health`
2. Check `ApiSettings.BaseUrl` in `appsettings.json`
3. Ensure it's `http://localhost:5205` (not HTTPS)

### Issue: Slow performance
**Cause**: Network latency  
**Fix**: Implement caching as shown in Step 4

### Issue: API returns 403 Forbidden
**Cause**: User accessing wrong account  
**Fix**: Ensure account ID matches authenticated user

---

## Migration Checklist

- [ ] Created `appsettings.json` with API configuration
- [ ] Registered HttpClient in DI
- [ ] Registered all 6 API clients
- [ ] Migrated `ReportifySettingsService` to use API
- [ ] Updated ViewModels to inject API clients
- [ ] Tested authentication flow
- [ ] Tested each feature (phrases, hotkeys, etc.)
- [ ] Implemented offline caching (optional)
- [ ] Removed old database dependencies
- [ ] Removed connection strings
- [ ] Updated deployment scripts

---

## Success Criteria

? Application starts successfully  
? Firebase authentication works  
? All features work as before  
? No direct database connections  
? API calls visible in Debug output  
? Errors handled gracefully  

---

**Ready to migrate!** Follow steps 1-7 in order for a smooth transition. ??
