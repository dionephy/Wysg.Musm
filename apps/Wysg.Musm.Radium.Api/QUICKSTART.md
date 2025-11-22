# Wysg.Musm.Radium.Api - Quick Start Guide

## Prerequisites

- .NET 10 SDK
- SQL Server (local or Azure SQL)
- Your existing Azure SQL database with the `radium` schema

## Step 1: Configure Connection String

Edit `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:musm-server.database.windows.net,1433;Initial Catalog=musmdb;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=\"Active Directory Default\";"
  }
}
```

### For Azure SQL with Active Directory Default (Recommended):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:musm-server.database.windows.net,1433;Initial Catalog=musmdb;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=\"Active Directory Default\";"
  }
}
```

### For Azure SQL with SQL Authentication:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=yourserver.database.windows.net;Database=central_db;User Id=yourusername;Password=yourpassword;Encrypt=True;"
  }
}
```

## Step 2: Run the API

```bash
cd apps/Wysg.Musm.Radium.Api
dotnet run
```

The API will start on:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`

## Step 3: View OpenAPI Documentation

.NET 10 uses built-in OpenAPI support. Open your browser to:
```
https://localhost:5001/openapi/v1.json
```

To get a UI for testing, you can use:
- **Swagger UI**: Use online Swagger Editor at https://editor.swagger.io/ and paste the OpenAPI JSON
- **Bruno/Insomnia/Postman**: Import the OpenAPI spec
- **Visual Studio**: Use the built-in HTTP file testing

## Step 4: Test Endpoints

### PowerShell (Windows):

```powershell
# Test health check
Invoke-WebRequest -Uri "https://localhost:5001/health" -SkipCertificateCheck

# Get all hotkeys
Invoke-WebRequest -Uri "https://localhost:5001/api/accounts/1/hotkeys" -SkipCertificateCheck

# Create a hotkey
$body = @{
    triggerText = "test"
    expansionText = "This is a test expansion"
    description = "Test hotkey"
    isActive = $true
} | ConvertTo-Json

Invoke-WebRequest -Uri "https://localhost:5001/api/accounts/1/hotkeys" `
    -Method Put `
    -ContentType "application/json" `
    -Body $body `
    -SkipCertificateCheck
```

### Bash/Linux:

```bash
# Test health check
curl https://localhost:5001/health -k

# Get all hotkeys
curl https://localhost:5001/api/accounts/1/hotkeys -k

# Create a hotkey
curl -X PUT "https://localhost:5001/api/accounts/1/hotkeys" \
  -H "Content-Type: application/json" \
  -d '{
    "triggerText": "test",
    "expansionText": "This is a test expansion",
    "description": "Test hotkey",
    "isActive": true
  }' -k

# Toggle hotkey
curl -X POST "https://localhost:5001/api/accounts/1/hotkeys/123/toggle" -k

# Delete hotkey
curl -X DELETE "https://localhost:5001/api/accounts/1/hotkeys/123" -k
```

## Step 5: Update Your WPF App

Update your `Wysg.Musm.Radium` app to call the API instead of direct database access:

```csharp
// Before (direct database access)
var service = new AzureSqlHotkeyService(settings);
var hotkeys = await service.GetAllHotkeyMetaAsync(accountId);

// After (API access)
var client = new HttpClient { BaseAddress = new Uri("https://localhost:5001") };
var response = await client.GetAsync($"/api/accounts/{accountId}/hotkeys");
var hotkeys = await response.Content.ReadFromJsonAsync<List<HotkeyDto>>();
```

## Next Steps: Add More Endpoints

### 1. Snippets

Following the same pattern as Hotkeys:

1. Create `SnippetDto` and `UpsertSnippetRequest` in `Models/Dtos/` ? (already done)
2. Create `ISnippetRepository` and `SnippetRepository` in `Repositories/`
3. Create `ISnippetService` and `SnippetService` in `Services/`
4. Create `SnippetsController` in `Controllers/`
5. Register in `Program.cs`

Example controller route:
```
GET    /api/accounts/{accountId}/snippets
GET    /api/accounts/{accountId}/snippets/{snippetId}
PUT    /api/accounts/{accountId}/snippets
POST   /api/accounts/{accountId}/snippets/{snippetId}/toggle
DELETE /api/accounts/{accountId}/snippets/{snippetId}
```

### 2. Accounts

```
GET  /api/accounts/{accountId}
POST /api/accounts
PUT  /api/accounts/{accountId}
```

## Production Deployment

### Option 1: Azure App Service

```bash
# Build and publish
dotnet publish -c Release -o ./publish

# Deploy to Azure
az webapp deployment source config-zip \
  --resource-group YOUR_RG \
  --name YOUR_APP_NAME \
  --src publish.zip
```

### Option 2: Azure Container Apps

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Wysg.Musm.Radium.Api.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Wysg.Musm.Radium.Api.dll"]
```

```bash
# Build and deploy
docker build -t radium-api .
az acr build --registry YOUR_ACR --image radium-api:latest .
az containerapp create \
  --name radium-api \
  --resource-group YOUR_RG \
  --image YOUR_ACR.azurecr.io/radium-api:latest
```

## Security: Azure Managed Identity (Future)

When ready to remove SQL credentials:

1. Enable Managed Identity on your Azure App Service/Container App
2. Grant the identity access to Azure SQL
3. Update `SqlConnectionFactory.cs`:

```csharp
using Azure.Identity;
using Azure.Core;

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;
    private readonly bool _useManagedIdentity;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string not configured");
        
        _useManagedIdentity = configuration
            .GetValue<bool>("ApiSettings:UseAzureManagedIdentity");
    }

    public async Task<SqlConnection> CreateConnectionAsync()
    {
        var connection = new SqlConnection(_connectionString);
        
        if (_useManagedIdentity)
        {
            var credential = new DefaultAzureCredential();
            var token = await credential.GetTokenAsync(
                new TokenRequestContext(
                    new[] { "https://database.windows.net/.default" }));
            
            connection.AccessToken = token.Token;
        }
        
        return connection;
    }
}
```

## Monitoring

Add Application Insights:

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

## Troubleshooting

### Connection Issues

Check your connection string:
```bash
dotnet run --environment Development
# Look for any connection errors in console
```

### Port Already in Use

Change ports in `Properties/launchSettings.json`:
```json
{
  "applicationUrl": "https://localhost:7001;http://localhost:5001"
}
```

### CORS Issues

Update CORS policy in `Program.cs`:
```csharp
policy.WithOrigins("https://yourdomain.com")
      .AllowAnyMethod()
      .AllowAnyHeader();
```

### OpenAPI/Swagger UI

.NET 10 uses built-in OpenAPI support. The OpenAPI spec is available at:
- `/openapi/v1.json` - OpenAPI JSON specification

To get a visual UI, you can:
1. Use the online Swagger Editor: https://editor.swagger.io/
2. Use tools like Bruno, Insomnia, or Postman
3. Add Scalar or other UI packages if needed

## Support

For issues or questions, refer to the main README.md
