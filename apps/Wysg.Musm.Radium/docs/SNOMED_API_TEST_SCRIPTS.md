# ?? SNOMED API Test Scripts

## Quick Launch Scripts

### Test API Mode
```powershell
# test-api-mode.ps1
# Launches both API and WPF in API mode

# Terminal 1: Start API
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd apps\Wysg.Musm.Radium.Api; dotnet run"

# Wait for API to start
Start-Sleep -Seconds 5

# Terminal 2: Start WPF in API mode
$env:USE_API = "1"
$env:RADIUM_API_URL = "http://localhost:5205"
cd apps\Wysg.Musm.Radium
dotnet run
```

### Test Direct DB Mode
```powershell
# test-db-mode.ps1
# Launches WPF in direct DB mode (no API)

# Clear API env vars
Remove-Item Env:\USE_API -ErrorAction SilentlyContinue
Remove-Item Env:\RADIUM_API_URL -ErrorAction SilentlyContinue

cd apps\Wysg.Musm.Radium
dotnet run
```

---

## API Testing Scripts

### Start API Only
```powershell
# start-api.ps1
cd apps\Wysg.Musm.Radium.Api
Write-Host "?? Starting Radium API..." -ForegroundColor Green
Write-Host "API will be available at: http://localhost:5205" -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop" -ForegroundColor Yellow
dotnet run
```

### Test API Health
```powershell
# test-api-health.ps1
$apiUrl = "http://localhost:5205"

Write-Host "Testing API health..." -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "$apiUrl/health" -Method Get
    if ($response.StatusCode -eq 200) {
        Write-Host "? API is healthy!" -ForegroundColor Green
        Write-Host "Status: $($response.StatusCode)" -ForegroundColor Green
    }
} catch {
    Write-Host "? API is not responding" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Make sure API is running: cd apps\Wysg.Musm.Radium.Api; dotnet run" -ForegroundColor Yellow
}
```

---

## WPF Testing Scripts

### Start WPF in API Mode
```powershell
# start-wpf-api-mode.ps1
Write-Host "?? Starting WPF in API mode..." -ForegroundColor Green

# Set environment variables
$env:USE_API = "1"
$env:RADIUM_API_URL = "http://localhost:5205"

Write-Host "Environment:" -ForegroundColor Cyan
Write-Host "  USE_API = $env:USE_API" -ForegroundColor Gray
Write-Host "  RADIUM_API_URL = $env:RADIUM_API_URL" -ForegroundColor Gray

# Check if API is running
Write-Host "`nChecking if API is running..." -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "$env:RADIUM_API_URL/health" -Method Get -TimeoutSec 2
    Write-Host "? API is running" -ForegroundColor Green
} catch {
    Write-Host "??  WARNING: API is not responding!" -ForegroundColor Yellow
    Write-Host "   Start API first: cd apps\Wysg.Musm.Radium.Api; dotnet run" -ForegroundColor Yellow
}

Write-Host "`n?? Starting WPF app..." -ForegroundColor Green
cd apps\Wysg.Musm.Radium
dotnet run
```

### Start WPF in DB Mode
```powershell
# start-wpf-db-mode.ps1
Write-Host "?? Starting WPF in Direct DB mode..." -ForegroundColor Green

# Clear API environment variables
Remove-Item Env:\USE_API -ErrorAction SilentlyContinue
Remove-Item Env:\RADIUM_API_URL -ErrorAction SilentlyContinue

Write-Host "Environment:" -ForegroundColor Cyan
Write-Host "  USE_API = (not set - direct DB)" -ForegroundColor Gray

Write-Host "`n?? Starting WPF app..." -ForegroundColor Green
cd apps\Wysg.Musm.Radium
dotnet run
```

---

## SNOMED Testing Scripts

### Test SNOMED Endpoints
```powershell
# test-snomed-endpoints.ps1
$apiUrl = "http://localhost:5205"
$token = "YOUR_FIREBASE_TOKEN" # Get from WPF app after login

Write-Host "?? Testing SNOMED API Endpoints..." -ForegroundColor Green

# Test 1: Cache Concept
Write-Host "`n1. Testing POST /api/snomed/concepts" -ForegroundColor Cyan
$conceptBody = @{
    conceptId = 80891009
    conceptIdStr = "80891009"
    fsn = "Heart structure (body structure)"
    pt = "Heart structure"
    semanticTag = "body structure"
    active = $true
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$apiUrl/api/snomed/concepts" `
        -Method Post `
        -Headers @{ "Authorization" = "Bearer $token" } `
        -Body $conceptBody `
        -ContentType "application/json"
    Write-Host "? Concept cached successfully" -ForegroundColor Green
} catch {
    Write-Host "? Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Get Concept
Write-Host "`n2. Testing GET /api/snomed/concepts/80891009" -ForegroundColor Cyan
try {
    $concept = Invoke-RestMethod -Uri "$apiUrl/api/snomed/concepts/80891009" `
        -Method Get `
        -Headers @{ "Authorization" = "Bearer $token" }
    Write-Host "? Retrieved concept: $($concept.fsn)" -ForegroundColor Green
} catch {
    Write-Host "? Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Get Mappings (Batch)
Write-Host "`n3. Testing GET /api/snomed/mappings?phraseIds=1&phraseIds=2&phraseIds=3" -ForegroundColor Cyan
try {
    $mappings = Invoke-RestMethod -Uri "$apiUrl/api/snomed/mappings?phraseIds=1&phraseIds=2&phraseIds=3" `
        -Method Get `
        -Headers @{ "Authorization" = "Bearer $token" }
    Write-Host "? Retrieved $($mappings.Count) mappings" -ForegroundColor Green
    foreach ($key in $mappings.Keys | Select-Object -First 3) {
        $mapping = $mappings[$key]
        Write-Host "   - Phrase $key: $($mapping.semanticTag)" -ForegroundColor Gray
    }
} catch {
    Write-Host "? Failed: $($_.Exception.Message)" -ForegroundColor Red
}
```

---

## Diagnostic Scripts

### Check Configuration
```powershell
# check-config.ps1
Write-Host "?? Configuration Check" -ForegroundColor Green

Write-Host "`n1. Environment Variables:" -ForegroundColor Cyan
Write-Host "   USE_API = $env:USE_API" -ForegroundColor Gray
Write-Host "   RADIUM_API_URL = $env:RADIUM_API_URL" -ForegroundColor Gray
Write-Host "   RAD_DISABLE_PHRASE_PRELOAD = $env:RAD_DISABLE_PHRASE_PRELOAD" -ForegroundColor Gray

Write-Host "`n2. API Availability:" -ForegroundColor Cyan
$apiUrl = if ($env:RADIUM_API_URL) { $env:RADIUM_API_URL } else { "http://localhost:5205" }
try {
    $response = Invoke-WebRequest -Uri "$apiUrl/health" -Method Get -TimeoutSec 2
    Write-Host "   ? API is running at $apiUrl" -ForegroundColor Green
} catch {
    Write-Host "   ? API is not running" -ForegroundColor Red
}

Write-Host "`n3. Projects:" -ForegroundColor Cyan
$apiProject = Test-Path "apps\Wysg.Musm.Radium.Api\Wysg.Musm.Radium.Api.csproj"
$wpfProject = Test-Path "apps\Wysg.Musm.Radium\Wysg.Musm.Radium.csproj"
Write-Host "   API Project: $(if ($apiProject) { '? Found' } else { '? Not Found' })" -ForegroundColor $(if ($apiProject) { 'Green' } else { 'Red' })
Write-Host "   WPF Project: $(if ($wpfProject) { '? Found' } else { '? Not Found' })" -ForegroundColor $(if ($wpfProject) { 'Green' } else { 'Red' })

Write-Host "`n4. Database Connection:" -ForegroundColor Cyan
$appsettings = Get-Content "apps\Wysg.Musm.Radium.Api\appsettings.json" | ConvertFrom-Json
if ($appsettings.ConnectionStrings.DefaultConnection) {
    $connStr = $appsettings.ConnectionStrings.DefaultConnection
    if ($connStr -match "database.windows.net") {
        Write-Host "   ? Azure SQL connection configured" -ForegroundColor Green
    } else {
        Write-Host "   ??  PostgreSQL connection configured" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ? No connection string found" -ForegroundColor Red
}
```

### Monitor API Logs
```powershell
# monitor-api-logs.ps1
Write-Host "?? Monitoring API Logs..." -ForegroundColor Green
Write-Host "Press Ctrl+C to stop" -ForegroundColor Yellow

cd apps\Wysg.Musm.Radium.Api

# Start API and capture output
dotnet run | ForEach-Object {
    if ($_ -match "snomed") {
        Write-Host $_ -ForegroundColor Cyan
    } elseif ($_ -match "error|fail") {
        Write-Host $_ -ForegroundColor Red
    } elseif ($_ -match "success|200|204") {
        Write-Host $_ -ForegroundColor Green
    } else {
        Write-Host $_ -ForegroundColor Gray
    }
}
```

---

## Batch Testing Script

### Complete Test Suite
```powershell
# run-all-tests.ps1
Write-Host "?? Running Complete Test Suite" -ForegroundColor Green

# Step 1: Check configuration
Write-Host "`n?? Step 1: Checking configuration..." -ForegroundColor Cyan
& "$PSScriptRoot\check-config.ps1"

# Step 2: Start API
Write-Host "`n?? Step 2: Starting API..." -ForegroundColor Cyan
$apiJob = Start-Job -ScriptBlock { 
    cd $using:PWD\apps\Wysg.Musm.Radium.Api
    dotnet run 
}

# Wait for API to start
Write-Host "   Waiting for API to start..." -ForegroundColor Gray
Start-Sleep -Seconds 10

# Step 3: Test API health
Write-Host "`n?? Step 3: Testing API health..." -ForegroundColor Cyan
& "$PSScriptRoot\test-api-health.ps1"

# Step 4: Run WPF
Write-Host "`n?? Step 4: Starting WPF in API mode..." -ForegroundColor Cyan
$env:USE_API = "1"
$env:RADIUM_API_URL = "http://localhost:5205"

Write-Host "`n? Ready for manual testing!" -ForegroundColor Green
Write-Host "   1. Login with Google" -ForegroundColor Gray
Write-Host "   2. Verify phrases are colored" -ForegroundColor Gray
Write-Host "   3. Open Global Phrases window" -ForegroundColor Gray
Write-Host "   4. Test SNOMED operations" -ForegroundColor Gray

cd apps\Wysg.Musm.Radium
dotnet run

# Cleanup
Write-Host "`n?? Cleaning up..." -ForegroundColor Cyan
Stop-Job $apiJob
Remove-Job $apiJob
```

---

## Usage

### Save These Scripts

```powershell
# Save to: scripts/test-api-mode.ps1
# Save to: scripts/test-db-mode.ps1
# Save to: scripts/check-config.ps1
# etc.

# Make scripts folder
mkdir scripts -Force

# Run a script
.\scripts\test-api-mode.ps1
```

### Quick Commands

```powershell
# API Mode
$env:USE_API="1"; cd apps\Wysg.Musm.Radium; dotnet run

# DB Mode  
Remove-Item Env:\USE_API; cd apps\Wysg.Musm.Radium; dotnet run

# Check API
Invoke-WebRequest http://localhost:5205/health
```

---

## ?? Recommended Testing Flow

1. **Check Config**: `.\scripts\check-config.ps1`
2. **Start API**: `.\scripts\start-api.ps1` (Terminal 1)
3. **Test Health**: `.\scripts\test-api-health.ps1` (Terminal 2)
4. **Start WPF**: `.\scripts\start-wpf-api-mode.ps1` (Terminal 2)
5. **Manual Tests**: Login, verify coloring, test SNOMED operations
6. **Monitor Logs**: Watch Debug Output for API calls

---

**Copy these scripts to your project and customize as needed!** ??
