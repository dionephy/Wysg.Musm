# Test Wysg.Musm.Radium.Api
# PowerShell test script for Windows

$baseUrl = "http://localhost:5205"
$accountId = 1

Write-Host "Testing Wysg.Musm.Radium.Api..." -ForegroundColor Cyan
Write-Host ""

# Test 1: Health Check
Write-Host "1. Testing Health Check..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/health"
    Write-Host "   ? Health Check: $($response.StatusCode)" -ForegroundColor Green
}
catch {
    Write-Host "   ? Health Check Failed: $_" -ForegroundColor Red
}
Write-Host ""

# Test 2: Get All Hotkeys
Write-Host "2. Getting All Hotkeys..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/accounts/$accountId/hotkeys"
    $hotkeys = $response.Content | ConvertFrom-Json
    Write-Host "   ? Found $($hotkeys.Count) hotkeys" -ForegroundColor Green
    if ($hotkeys.Count -gt 0) {
        Write-Host "   First hotkey: $($hotkeys[0].triggerText) -> $($hotkeys[0].expansionText)" -ForegroundColor Gray
    }
}
catch {
    Write-Host "   ? Get Hotkeys Failed: $_" -ForegroundColor Red
}
Write-Host ""

# Test 3: Create a Test Hotkey
Write-Host "3. Creating Test Hotkey..." -ForegroundColor Yellow
try {
    $body = @{
        triggerText = "pstest"
        expansionText = "PowerShell Test Expansion"
        description = "Created by test script"
        isActive = $true
    } | ConvertTo-Json

    $response = Invoke-WebRequest `
        -Uri "$baseUrl/api/accounts/$accountId/hotkeys" `
        -Method Put `
        -ContentType "application/json" `
        -Body $body

    $hotkey = $response.Content | ConvertFrom-Json
    Write-Host "   ? Created hotkey ID: $($hotkey.hotkeyId)" -ForegroundColor Green
    $createdId = $hotkey.hotkeyId
}
catch {
    Write-Host "   ? Create Hotkey Failed: $_" -ForegroundColor Red
    $createdId = $null
}
Write-Host ""

# Test 4: Get Single Hotkey (if created)
if ($createdId) {
    Write-Host "4. Getting Single Hotkey (ID: $createdId)..." -ForegroundColor Yellow
    try {
        $response = Invoke-WebRequest -Uri "$baseUrl/api/accounts/$accountId/hotkeys/$createdId"
        $hotkey = $response.Content | ConvertFrom-Json
        Write-Host "   ? Retrieved: $($hotkey.triggerText) -> $($hotkey.expansionText)" -ForegroundColor Green
    }
    catch {
        Write-Host "   ? Get Single Hotkey Failed: $_" -ForegroundColor Red
    }
    Write-Host ""

    # Test 5: Toggle Active Status
    Write-Host "5. Toggling Active Status..." -ForegroundColor Yellow
    try {
        $response = Invoke-WebRequest `
            -Uri "$baseUrl/api/accounts/$accountId/hotkeys/$createdId/toggle" `
            -Method Post

        $hotkey = $response.Content | ConvertFrom-Json
        Write-Host "   ? Toggled: isActive = $($hotkey.isActive)" -ForegroundColor Green
    }
    catch {
        Write-Host "   ? Toggle Failed: $_" -ForegroundColor Red
    }
    Write-Host ""

    # Test 6: Delete Hotkey
    Write-Host "6. Deleting Test Hotkey..." -ForegroundColor Yellow
    try {
        $response = Invoke-WebRequest `
            -Uri "$baseUrl/api/accounts/$accountId/hotkeys/$createdId" `
            -Method Delete

        Write-Host "   ? Deleted successfully" -ForegroundColor Green
    }
    catch {
        Write-Host "   ? Delete Failed: $_" -ForegroundColor Red
    }
    Write-Host ""
}

Write-Host "All tests completed!" -ForegroundColor Cyan
Write-Host ""
Write-Host "To view OpenAPI spec, visit:" -ForegroundColor Yellow
Write-Host "  $baseUrl/openapi/v1.json" -ForegroundColor Gray
