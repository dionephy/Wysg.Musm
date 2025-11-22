# ?? Production Deployment Checklist

## ? Pre-Deployment

### Configuration
- [ ] Update `appsettings.Production.json` with real Firebase Project ID
- [ ] Set `Firebase:ValidateToken` to `true`
- [ ] Configure Azure SQL connection string
- [ ] Update CORS origins with production domains
- [ ] Disable Swagger in production

### Security
- [ ] Verify Firebase authentication is enabled
- [ ] Test UID ownership checks work
- [ ] Confirm account access control works
- [ ] Review API logs for security warnings

### Testing
- [ ] Test account creation via API
- [ ] Test login flow (email + Google OAuth)
- [ ] Test settings get/update
- [ ] Verify WPF app connects to API
- [ ] Test error handling (invalid tokens, wrong UID)

---

## ?? Deployment Steps

### 1. Deploy API to Azure

#### Option A: Azure App Service
```powershell
# Login to Azure
az login

# Create resource group (if needed)
az group create --name rg-radium-api --location eastus

# Create App Service plan
az appservice plan create --name plan-radium-api --resource-group rg-radium-api --sku B1

# Create web app
az webapp create --name radium-api --resource-group rg-radium-api --plan plan-radium-api --runtime "DOTNET|10.0"

# Deploy the code
cd apps\Wysg.Musm.Radium.Api
dotnet publish -c Release
Compress-Archive -Path bin\Release\net10.0\publish\* -DestinationPath deploy.zip -Force
az webapp deployment source config-zip --resource-group rg-radium-api --name radium-api --src deploy.zip
```

#### Option B: Azure Container Apps
```powershell
# Build Docker image
docker build -t radium-api:latest -f apps\Wysg.Musm.Radium.Api\Dockerfile .

# Push to Azure Container Registry
az acr login --name yourregistry
docker tag radium-api:latest yourregistry.azurecr.io/radium-api:latest
docker push yourregistry.azurecr.io/radium-api:latest

# Deploy to Container Apps
az containerapp create \
  --name radium-api \
  --resource-group rg-radium-api \
  --environment env-radium \
  --image yourregistry.azurecr.io/radium-api:latest \
  --target-port 5205 \
  --ingress external
```

### 2. Configure Environment Variables

Set in Azure Portal > Configuration:
```
FIREBASE_PROJECT_ID=your-project-id
FIREBASE_VALIDATE_TOKEN=true
ASPNETCORE_ENVIRONMENT=Production
```

### 3. Update WPF App

Update `RadiumApiClient` base URL:
```csharp
// In App.xaml.cs or appsettings
var apiUrl = Environment.GetEnvironmentVariable("RADIUM_API_URL") 
    ?? "https://radium-api.azurewebsites.net";  // ¡ç Production URL
```

### 4. Test Production API

```powershell
# Test health endpoint
curl https://radium-api.azurewebsites.net/health

# Test with real Firebase token
$token = "YOUR_FIREBASE_ID_TOKEN"
$headers = @{ Authorization = "Bearer $token" }
Invoke-RestMethod -Uri "https://radium-api.azurewebsites.net/api/accounts/2" -Headers $headers
```

---

## ? Post-Deployment

### Verification
- [ ] API health endpoint returns 200 OK
- [ ] WPF app can login successfully
- [ ] Account creation works
- [ ] Settings get/update works
- [ ] Firebase JWT validation is active
- [ ] UID ownership checks are working

### Monitoring
- [ ] Set up Application Insights (optional)
- [ ] Configure alerts for 5xx errors
- [ ] Monitor API response times
- [ ] Review logs for security issues

### Database
- [ ] Verify accounts are being created correctly
- [ ] Check last_login_at is being updated
- [ ] Confirm settings are being saved

---

## ?? Security Verification

Test these scenarios to confirm security:

### Test 1: Cannot Create Account for Different User
```powershell
# Login as User A, try to create account for User B
$tokenA = "USER_A_TOKEN"
$bodyB = '{"uid":"USER_B_UID","email":"userb@example.com","displayName":"User B"}'
Invoke-RestMethod -Uri "https://radium-api.azurewebsites.net/api/accounts/ensure" `
  -Method POST -Headers @{Authorization="Bearer $tokenA"} -Body $bodyB -ContentType "application/json"

# Expected: 403 Forbidden
```

### Test 2: Cannot Access Another User's Account
```powershell
# Login as User A, try to access User B's account
$tokenA = "USER_A_TOKEN"
Invoke-RestMethod -Uri "https://radium-api.azurewebsites.net/api/accounts/USER_B_ACCOUNT_ID" `
  -Headers @{Authorization="Bearer $tokenA"}

# Expected: 403 Forbidden
```

### Test 3: Cannot Modify Another User's Settings
```powershell
# Login as User A, try to update User B's settings
$tokenA = "USER_A_TOKEN"
$settings = '"{\"theme\":\"dark\"}"'
Invoke-RestMethod -Uri "https://radium-api.azurewebsites.net/api/accounts/USER_B_ACCOUNT_ID/settings" `
  -Method PUT -Headers @{Authorization="Bearer $tokenA"} -Body $settings -ContentType "application/json"

# Expected: 403 Forbidden
```

---

## ?? Troubleshooting

### Issue: "Failed to validate Firebase token"
- **Cause**: Firebase validation enabled but token is invalid
- **Fix**: Ensure WPF app is sending valid Firebase ID tokens
- **Check**: `Firebase:ProjectId` matches your Firebase project

### Issue: "UID mismatch" errors
- **Cause**: Token UID doesn't match request UID
- **Fix**: Ensure WPF sends the same UID from Firebase token
- **Check**: JWT payload contains correct `user_id`

### Issue: Database connection fails
- **Cause**: Azure SQL firewall or connection string issue
- **Fix**: Add App Service IP to SQL firewall rules
- **Check**: Test connection from Azure portal

### Issue: CORS errors in browser
- **Cause**: WPF domain not in allowed origins
- **Fix**: Update `ApiSettings:CorsOrigins` in appsettings
- **Check**: Browser dev tools for CORS error details

---

## ?? Rollback Plan

If deployment fails:

1. **Keep API running** - Old WPF still works with direct DB
2. **Fix issues** - API is additive, doesn't break existing flow
3. **Redeploy** - Once fixed, deploy again
4. **No data loss** - API and direct DB both write to same tables

---

## ?? Support Contacts

- **Firebase Issues**: Firebase Console ¡æ Support
- **Azure Issues**: Azure Portal ¡æ Support + Feedback
- **API Issues**: Check Application Insights logs

---

## ? Deployment Complete!

Once all checklist items are ?:

1. **Announce to team** - API is live
2. **Monitor for 24 hours** - Watch for errors
3. **Document learnings** - Update deployment guide
4. **Plan next phase** - Hotkeys/Snippets/Phrases migration

**Congratulations! ??**
