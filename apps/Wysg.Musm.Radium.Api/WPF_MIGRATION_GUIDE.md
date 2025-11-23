# WPF Client Migration Guide - Database to API

## Overview

This guide shows how to migrate the Wysg.Musm.Radium WPF application from direct database access to secure API calls.

---

## Architecture Change

### Before (INSECURE)
```
WPF Desktop Client
    бщ Direct PostgreSQL/SQL Connection
    бщ (Exposed credentials)
Azure SQL Database
```

### After (SECURE)
```
WPF Desktop Client
    бщ HTTPS + Firebase JWT Token
API Server (Authentication, Validation, Logging)
    бщ Parameterized SQL Queries
Azure SQL Database
```

---

## Step-by-Step Migration

### Phase 1: Create API Client Base Class

**File**: `apps/Wysg.Musm.Radium/Services/ApiClients/ApiClientBase.cs`

```csharp
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.Services.ApiClients
{
    public abstract class ApiClientBase
    {
        protected readonly HttpClient HttpClient;
        protected readonly IAuthService AuthService;
        protected readonly string BaseUrl;

        protected ApiClientBase(HttpClient httpClient, IAuthService authService, string baseUrl)
        {
            HttpClient = httpClient;
            AuthService = authService;
            BaseUrl = baseUrl.TrimEnd('/');
        }

        protected async Task<HttpClient> GetAuthenticatedClientAsync()
        {
            var token = await AuthService.GetFirebaseTokenAsync();
            HttpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
            return HttpClient;
        }

        protected async Task<T?> GetAsync<T>(string endpoint)
        {
            var client = await GetAuthenticatedClientAsync();
            var response = await client.GetAsync($"{BaseUrl}{endpoint}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }

        protected async Task<T?> PostAsync<T>(string endpoint, object data)
        {
            var client = await GetAuthenticatedClientAsync();
            var response = await client.PostAsJsonAsync($"{BaseUrl}{endpoint}", data);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }

        protected async Task<T?> PutAsync<T>(string endpoint, object data)
        {
            var client = await GetAuthenticatedClientAsync();
            var response = await client.PutAsJsonAsync($"{BaseUrl}{endpoint}", data);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }

        protected async Task DeleteAsync(string endpoint)
        {
            var client = await GetAuthenticatedClientAsync();
            var response = await client.DeleteAsync($"{BaseUrl}{endpoint}");
            response.EnsureSuccessStatusCode();
        }
    }
}
```

---

### Phase 2: Create Specific API Clients

#### User Settings API Client

**File**: `apps/Wysg.Musm.Radium/Services/ApiClients/UserSettingsApiClient.cs`

```csharp
using System.Threading.Tasks;
using Wysg.Musm.Radium.Api.Models.Dtos;

namespace Wysg.Musm.Radium.Services.ApiClients
{
    public interface IUserSettingsApiClient
    {
        Task<UserSettingDto?> GetAsync(long accountId);
        Task<UserSettingDto> UpsertAsync(long accountId, string settingsJson);
        Task DeleteAsync(long accountId);
    }

    public class UserSettingsApiClient : ApiClientBase, IUserSettingsApiClient
    {
        public UserSettingsApiClient(HttpClient httpClient, IAuthService authService, string baseUrl)
            : base(httpClient, authService, baseUrl)
        {
        }

        public async Task<UserSettingDto?> GetAsync(long accountId)
        {
            return await GetAsync<UserSettingDto>($"/api/accounts/{accountId}/settings");
        }

        public async Task<UserSettingDto> UpsertAsync(long accountId, string settingsJson)
        {
            var request = new { SettingsJson = settingsJson };
            return await PutAsync<UserSettingDto>($"/api/accounts/{accountId}/settings", request);
        }

        public async Task DeleteAsync(long accountId)
        {
            await DeleteAsync($"/api/accounts/{accountId}/settings");
        }
    }
}
```

#### Phrases API Client

**File**: `apps/Wysg.Musm.Radium/Services/ApiClients/PhrasesApiClient.cs`

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Wysg.Musm.Radium.Api.Models.Dtos;

namespace Wysg.Musm.Radium.Services.ApiClients
{
    public interface IPhrasesApiClient
    {
        Task<List<PhraseDto>> GetAllAsync(long accountId, bool activeOnly = false);
        Task<PhraseDto?> GetByIdAsync(long accountId, long phraseId);
        Task<List<PhraseDto>> SearchAsync(long accountId, string? query, bool activeOnly, int maxResults);
        Task<PhraseDto> UpsertAsync(long accountId, string text, bool active);
        Task ToggleActiveAsync(long accountId, long phraseId);
        Task DeleteAsync(long accountId, long phraseId);
    }

    public class PhrasesApiClient : ApiClientBase, IPhrasesApiClient
    {
        public PhrasesApiClient(HttpClient httpClient, IAuthService authService, string baseUrl)
            : base(httpClient, authService, baseUrl)
        {
        }

        public async Task<List<PhraseDto>> GetAllAsync(long accountId, bool activeOnly = false)
        {
            var endpoint = $"/api/accounts/{accountId}/phrases?activeOnly={activeOnly}";
            return await GetAsync<List<PhraseDto>>(endpoint) ?? new List<PhraseDto>();
        }

        public async Task<PhraseDto?> GetByIdAsync(long accountId, long phraseId)
        {
            return await GetAsync<PhraseDto>($"/api/accounts/{accountId}/phrases/{phraseId}");
        }

        public async Task<List<PhraseDto>> SearchAsync(long accountId, string? query, bool activeOnly, int maxResults)
        {
            var endpoint = $"/api/accounts/{accountId}/phrases/search?query={query}&activeOnly={activeOnly}&maxResults={maxResults}";
            return await GetAsync<List<PhraseDto>>(endpoint) ?? new List<PhraseDto>();
        }

        public async Task<PhraseDto> UpsertAsync(long accountId, string text, bool active)
        {
            var request = new { Text = text, Active = active };
            return await PutAsync<PhraseDto>($"/api/accounts/{accountId}/phrases", request);
        }

        public async Task ToggleActiveAsync(long accountId, long phraseId)
        {
            await PostAsync<object>($"/api/accounts/{accountId}/phrases/{phraseId}/toggle", new { });
        }

        public async Task DeleteAsync(long accountId, long phraseId)
        {
            await DeleteAsync($"/api/accounts/{accountId}/phrases/{phraseId}");
        }
    }
}
```

#### Exported Reports API Client

**File**: `apps/Wysg.Musm.Radium/Services/ApiClients/ExportedReportsApiClient.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wysg.Musm.Radium.Api.Models.Dtos;

namespace Wysg.Musm.Radium.Services.ApiClients
{
    public interface IExportedReportsApiClient
    {
        Task<PaginatedExportedReportsResponse> GetAllAsync(
            long accountId, bool unresolvedOnly = false, int page = 1, int pageSize = 50);
        Task<ExportedReportDto?> GetByIdAsync(long accountId, long id);
        Task<ExportedReportDto> CreateAsync(long accountId, string report, DateTime reportDateTime);
        Task MarkResolvedAsync(long accountId, long id);
        Task DeleteAsync(long accountId, long id);
        Task<ExportedReportStatsDto> GetStatsAsync(long accountId);
    }

    public class ExportedReportsApiClient : ApiClientBase, IExportedReportsApiClient
    {
        public ExportedReportsApiClient(HttpClient httpClient, IAuthService authService, string baseUrl)
            : base(httpClient, authService, baseUrl)
        {
        }

        public async Task<PaginatedExportedReportsResponse> GetAllAsync(
            long accountId, bool unresolvedOnly = false, int page = 1, int pageSize = 50)
        {
            var endpoint = $"/api/accounts/{accountId}/exported-reports?unresolvedOnly={unresolvedOnly}&page={page}&pageSize={pageSize}";
            return await GetAsync<PaginatedExportedReportsResponse>(endpoint) 
                   ?? new PaginatedExportedReportsResponse();
        }

        public async Task<ExportedReportDto?> GetByIdAsync(long accountId, long id)
        {
            return await GetAsync<ExportedReportDto>($"/api/accounts/{accountId}/exported-reports/{id}");
        }

        public async Task<ExportedReportDto> CreateAsync(long accountId, string report, DateTime reportDateTime)
        {
            var request = new { Report = report, ReportDateTime = reportDateTime };
            return await PostAsync<ExportedReportDto>($"/api/accounts/{accountId}/exported-reports", request);
        }

        public async Task MarkResolvedAsync(long accountId, long id)
        {
            await PostAsync<object>($"/api/accounts/{accountId}/exported-reports/{id}/resolve", new { });
        }

        public async Task DeleteAsync(long accountId, long id)
        {
            await DeleteAsync($"/api/accounts/{accountId}/exported-reports/{id}");
        }

        public async Task<ExportedReportStatsDto> GetStatsAsync(long accountId)
        {
            return await GetAsync<ExportedReportStatsDto>($"/api/accounts/{accountId}/exported-reports/stats")
                   ?? new ExportedReportStatsDto();
        }
    }
}
```

---

### Phase 3: Register API Clients in DI

**File**: `apps/Wysg.Musm.Radium/App.xaml.cs`

Update the `ConfigureServices` method:

```csharp
private void ConfigureServices(IServiceCollection services)
{
    // Configuration
    var apiBaseUrl = Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001";
    
    // HttpClient with timeout
    services.AddHttpClient("RadiumApi", client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    });

    // Register API clients
    services.AddScoped<IUserSettingsApiClient>(sp =>
    {
        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("RadiumApi");
        var authService = sp.GetRequiredService<IAuthService>();
        return new UserSettingsApiClient(httpClient, authService, apiBaseUrl);
    });

    services.AddScoped<IPhrasesApiClient>(sp =>
    {
        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("RadiumApi");
        var authService = sp.GetRequiredService<IAuthService>();
        return new PhrasesApiClient(httpClient, authService, apiBaseUrl);
    });

    services.AddScoped<IExportedReportsApiClient>(sp =>
    {
        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("RadiumApi");
        var authService = sp.GetRequiredService<IAuthService>();
        return new ExportedReportsApiClient(httpClient, authService, apiBaseUrl);
    });

    // Remove old direct database services
    // services.AddSingleton<IReportifySettingsService, ReportifySettingsService>(); // ? REMOVE
    
    // Keep existing services
    services.AddSingleton<IAuthService, GoogleOAuthAuthService>();
    services.AddSingleton<ITenantContext, TenantContext>();
    // ... other services
}
```

---

### Phase 4: Update appsettings.json

**File**: `apps/Wysg.Musm.Radium/appsettings.json`

```json
{
  "ApiSettings": {
    "BaseUrl": "https://your-production-api.azurewebsites.net"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

**Development Override** - `appsettings.Development.json`:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7001"
  }
}
```

---

### Phase 5: Replace Service Implementations

#### Example: Update ReportifySettingsService

**Before** (Direct Database):
```csharp
public class ReportifySettingsService : IReportifySettingsService
{
    private readonly ICentralDataSourceProvider _ds;

    public async Task<string?> GetSettingsJsonAsync(long accountId)
    {
        await using var con = _ds.Central.CreateConnection();
        await con.OpenAsync();
        const string sql = "SELECT settings_json::text FROM radium.user_setting WHERE account_id=@aid";
        await using var cmd = new NpgsqlCommand(sql, con);
        cmd.Parameters.AddWithValue("aid", accountId);
        var obj = await cmd.ExecuteScalarAsync();
        return obj as string;
    }
}
```

**After** (API):
```csharp
public class ReportifySettingsService : IReportifySettingsService
{
    private readonly IUserSettingsApiClient _apiClient;

    public ReportifySettingsService(IUserSettingsApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<string?> GetSettingsJsonAsync(long accountId)
    {
        var settings = await _apiClient.GetAsync(accountId);
        return settings?.SettingsJson;
    }
}
```

---

### Phase 6: Handle Offline Scenarios

Add retry logic and offline caching:

```csharp
public class ResilientUserSettingsService : IReportifySettingsService
{
    private readonly IUserSettingsApiClient _apiClient;
    private readonly ILogger<ResilientUserSettingsService> _logger;
    private readonly Dictionary<long, string> _cache = new();

    public async Task<string?> GetSettingsJsonAsync(long accountId)
    {
        try
        {
            var settings = await _apiClient.GetAsync(accountId);
            if (settings != null)
            {
                _cache[accountId] = settings.SettingsJson;
                return settings.SettingsJson;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "API request failed, using cached settings");
            if (_cache.TryGetValue(accountId, out var cached))
            {
                return cached;
            }
        }

        return null;
    }
}
```

---

## Migration Checklist

### Pre-Migration
- [ ] Deploy API to production
- [ ] Verify API health endpoint accessible
- [ ] Test Firebase authentication flow
- [ ] Backup current WPF configuration

### Migration Steps
1. [ ] Create `ApiClients` folder structure
2. [ ] Implement `ApiClientBase.cs`
3. [ ] Implement specific API clients (6 clients)
4. [ ] Update `App.xaml.cs` DI registration
5. [ ] Add `appsettings.json` API configuration
6. [ ] Replace direct database services with API clients
7. [ ] Test each feature thoroughly
8. [ ] Remove old database connection code

### Post-Migration
- [ ] Remove PostgreSQL/SQL connection strings from config
- [ ] Remove `ICentralDataSourceProvider` dependency
- [ ] Remove `Npgsql` / `Microsoft.Data.SqlClient` NuGet packages
- [ ] Update deployment scripts
- [ ] Monitor API logs for errors

---

## Testing Strategy

### Unit Tests
Test API clients with mocked HttpClient:

```csharp
[Fact]
public async Task GetSettingsAsync_ReturnsSettings()
{
    // Arrange
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.When("*/api/accounts/1/settings")
            .Respond("application/json", "{\"accountId\":1,\"settingsJson\":\"{}\"}");
    
    var client = mockHttp.ToHttpClient();
    var apiClient = new UserSettingsApiClient(client, mockAuthService, "https://test.com");

    // Act
    var result = await apiClient.GetAsync(1);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(1, result.AccountId);
}
```

### Integration Tests
Test against real API (development environment):

```csharp
[Fact]
public async Task EndToEnd_CreateAndRetrieveReport()
{
    var client = new ExportedReportsApiClient(httpClient, authService, "https://localhost:7001");
    
    // Create
    var created = await client.CreateAsync(accountId, "Test report", DateTime.UtcNow);
    Assert.NotNull(created);
    
    // Retrieve
    var retrieved = await client.GetByIdAsync(accountId, created.Id);
    Assert.Equal(created.Id, retrieved.Id);
    
    // Cleanup
    await client.DeleteAsync(accountId, created.Id);
}
```

---

## Performance Considerations

### Caching Strategy
```csharp
public class CachedPhrasesService
{
    private readonly IPhrasesApiClient _apiClient;
    private readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    public async Task<List<PhraseDto>> GetPhrasesAsync(long accountId)
    {
        var cacheKey = $"phrases_{accountId}";
        
        if (_cache.TryGetValue(cacheKey, out List<PhraseDto> cached))
        {
            return cached;
        }

        var phrases = await _apiClient.GetAllAsync(accountId);
        
        _cache.Set(cacheKey, phrases, TimeSpan.FromMinutes(5));
        
        return phrases;
    }
}
```

### Batch Operations
For large datasets, use pagination:

```csharp
public async Task<List<ExportedReportDto>> GetAllReportsAsync(long accountId)
{
    var allReports = new List<ExportedReportDto>();
    int page = 1;
    const int pageSize = 100;

    while (true)
    {
        var response = await _apiClient.GetAllAsync(accountId, false, page, pageSize);
        allReports.AddRange(response.Data);

        if (page >= response.Pagination.TotalPages)
            break;

        page++;
    }

    return allReports;
}
```

---

## Troubleshooting

### Common Issues

#### 1. 401 Unauthorized
**Cause**: Firebase token expired or invalid  
**Solution**: Implement token refresh:
```csharp
if (response.StatusCode == HttpStatusCode.Unauthorized)
{
    await _authService.RefreshTokenAsync();
    // Retry request
}
```

#### 2. 403 Forbidden
**Cause**: User accessing wrong account  
**Solution**: Verify account ID matches authenticated user

#### 3. Network Timeout
**Cause**: API server not responding  
**Solution**: Increase timeout, add retry logic

---

## Rollback Plan

If migration causes issues:

1. Revert to previous WPF version
2. Re-enable direct database services
3. Restore connection strings
4. Keep API running (no harm)

---

## Success Metrics

After migration, monitor:
- ? API response times < 1 second
- ? Zero authentication failures
- ? No direct database connections from WPF
- ? All features working as before

---

**Estimated Migration Time**: 1-2 days for full WPF client migration

**Ready to start?** Begin with Phase 1 (ApiClientBase) and test incrementally!
