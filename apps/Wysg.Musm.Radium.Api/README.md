# Wysg.Musm.Radium.Api

Production-ready Web API for the Wysg.Musm.Radium application.

## Architecture

This API follows clean architecture principles with clear separation of concerns:

```
戍式式 Controllers/          # Thin API controllers (HTTP layer)
戍式式 Services/             # Business logic layer
戍式式 Repositories/         # Data access layer (ADO.NET)
戍式式 Models/
弛   戌式式 Dtos/            # Data Transfer Objects
戌式式 Configuration/        # Configuration classes
```

## Project Structure

### Controllers
- **Thin controllers** - Handle HTTP concerns only (routing, validation, status codes)
- Return proper HTTP status codes (200, 400, 404, 500)
- Use standard RESTful conventions

### Services
- **Business logic** - Validation, orchestration, logging
- Independent of HTTP/database concerns
- Easy to unit test

### Repositories
- **Data access** - Pure ADO.NET (SqlConnection, SqlCommand, SqlDataReader)
- Connection management via factory pattern
- Prepared for easy migration to Azure Managed Identity

### DTOs
- **Simple C# types** - No logic, just data
- Separate request/response models

## Database Connection

### Development (Local SQL Server)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=central_db;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  }
}
```

### Production (Azure SQL)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=yourserver.database.windows.net;Database=central_db;User Id=yourusername;Password=yourpassword;Encrypt=True;"
  }
}
```

### Azure Managed Identity (Future)
To switch to Managed Identity, update `SqlConnectionFactory.cs`:
```csharp
var credential = new DefaultAzureCredential();
var token = await credential.GetTokenAsync(
    new TokenRequestContext(new[] { "https://database.windows.net/.default" }));
connection.AccessToken = token.Token;
```

## API Endpoints

### Hotkeys

#### Get All Hotkeys
```
GET /api/accounts/{accountId}/hotkeys
```

#### Get Single Hotkey
```
GET /api/accounts/{accountId}/hotkeys/{hotkeyId}
```

#### Create/Update Hotkey
```
PUT /api/accounts/{accountId}/hotkeys
Content-Type: application/json

{
  "triggerText": "mykey",
  "expansionText": "expanded text",
  "description": "optional description",
  "isActive": true
}
```

#### Toggle Hotkey Active Status
```
POST /api/accounts/{accountId}/hotkeys/{hotkeyId}/toggle
```

#### Delete Hotkey
```
DELETE /api/accounts/{accountId}/hotkeys/{hotkeyId}
```

## Running the API

### Development
```bash
cd apps/Wysg.Musm.Radium.Api
dotnet run
```

### Production
```bash
dotnet publish -c Release -o ./publish
cd publish
dotnet Wysg.Musm.Radium.Api.dll
```

## Swagger/OpenAPI

Available at: `https://localhost:<port>/swagger`

## Health Check

Available at: `https://localhost:<port>/health`

## Adding New Endpoints

Follow this pattern for new entities (e.g., Snippets):

1. **Create DTO** in `Models/Dtos/`
   ```csharp
   public sealed class SnippetDto { /* properties */ }
   public sealed class UpsertSnippetRequest { /* properties */ }
   ```

2. **Create Repository Interface** in `Repositories/`
   ```csharp
   public interface ISnippetRepository { /* methods */ }
   ```

3. **Implement Repository** in `Repositories/`
   ```csharp
   public sealed class SnippetRepository : ISnippetRepository
   {
       // ADO.NET implementation
   }
   ```

4. **Create Service Interface** in `Services/`
   ```csharp
   public interface ISnippetService { /* methods */ }
   ```

5. **Implement Service** in `Services/`
   ```csharp
   public sealed class SnippetService : ISnippetService
   {
       // Business logic
   }
   ```

6. **Create Controller** in `Controllers/`
   ```csharp
   [ApiController]
   [Route("api/accounts/{accountId}/[controller]")]
   public sealed class SnippetsController : ControllerBase
   {
       // HTTP endpoints
   }
   ```

7. **Register in Program.cs**
   ```csharp
   builder.Services.AddScoped<ISnippetRepository, SnippetRepository>();
   builder.Services.AddScoped<ISnippetService, SnippetService>();
   ```

## Security Considerations

### For Production:
1. **Use HTTPS only** (already configured)
2. **Add authentication** (JWT, OAuth2, etc.)
3. **Configure CORS properly** (restrict allowed origins)
4. **Use Azure Key Vault** for connection strings
5. **Enable rate limiting**
6. **Add API versioning**
7. **Implement request validation**

Example authentication setup:
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* config */ });
```

## Monitoring & Logging

The API uses built-in .NET logging. Configure in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Wysg.Musm.Radium.Api": "Debug"
    }
  }
}
```

For production, integrate with:
- **Application Insights** (Azure)
- **Serilog** (structured logging)
- **ELK Stack** (Elasticsearch, Logstash, Kibana)

## Deployment to Azure

### Azure App Service
```bash
az webapp create --resource-group myResourceGroup --plan myAppServicePlan --name myapi
az webapp deployment source config-zip --resource-group myResourceGroup --name myapi --src publish.zip
```

### Azure Container Apps
```bash
docker build -t myapi:latest .
az containerapp create --resource-group myResourceGroup --name myapi --image myapi:latest
```

## Testing

### Unit Tests (Example)
```csharp
public class HotkeyServiceTests
{
    [Fact]
    public async Task GetAllByAccountAsync_ValidAccountId_ReturnsHotkeys()
    {
        // Arrange
        var mockRepo = new Mock<IHotkeyRepository>();
        var service = new HotkeyService(mockRepo.Object, Mock.Of<ILogger<HotkeyService>>());
        
        // Act
        var result = await service.GetAllByAccountAsync(1);
        
        // Assert
        Assert.NotNull(result);
    }
}
```

### Integration Tests
```csharp
public class HotkeysControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HotkeysControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsSuccessStatusCode()
    {
        var response = await _client.GetAsync("/api/accounts/1/hotkeys");
        response.EnsureSuccessStatusCode();
    }
}
```

## Next Steps

1. ? **Hotkeys endpoint** - Complete
2. ? **Snippets endpoint** - Follow the same pattern
3. ? **Accounts endpoint** - User management
4. ? **Authentication** - JWT/OAuth2
5. ? **Azure Managed Identity** - Replace SQL credentials
6. ? **API versioning** - Support multiple versions
7. ? **Rate limiting** - Protect against abuse
8. ? **Caching** - Redis/Memory cache for performance
