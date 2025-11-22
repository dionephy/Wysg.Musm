# Quick diagnostic script for API mode testing
# Save as: test-api-mode-diagnostic.ps1

Write-Host "?????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?  Radium API Mode - Diagnostic Script         ?" -ForegroundColor Cyan
Write-Host "?????????????????????????????????????????????????" -ForegroundColor Cyan

# Step 1: Check API
Write-Host "`n[1/5] Checking API..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest http://localhost:5205/health -TimeoutSec 2 -UseBasicParsing
    Write-Host "  ? API is running (HTTP $($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "  ? API is NOT running!" -ForegroundColor Red
    Write-Host "  ¡æ Start API: cd apps\Wysg.Musm.Radium.Api; dotnet run" -ForegroundColor Yellow
    Write-Host "`nPress any key to exit..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}

# Step 2: Check environment variables
Write-Host "`n[2/5] Checking environment variables..." -ForegroundColor Yellow
$useApi = $env:USE_API
$apiUrl = $env:RADIUM_API_URL

if ($useApi -eq "1") {
    Write-Host "  ? USE_API = $useApi" -ForegroundColor Green
} else {
    Write-Host "  ??  USE_API = $useApi (not set to '1')" -ForegroundColor Yellow
    Write-Host "  ¡æ Set with: `$env:USE_API = `"1`"" -ForegroundColor Gray
}

if ($apiUrl) {
    Write-Host "  ? RADIUM_API_URL = $apiUrl" -ForegroundColor Green
} else {
    Write-Host "  ??  RADIUM_API_URL = (not set, will use default)" -ForegroundColor Gray
}

# Step 3: Check project files
Write-Host "`n[3/5] Checking project files..." -ForegroundColor Yellow
$apiProject = Test-Path "apps\Wysg.Musm.Radium.Api\Wysg.Musm.Radium.Api.csproj"
$wpfProject = Test-Path "apps\Wysg.Musm.Radium\Wysg.Musm.Radium.csproj"

if ($apiProject) {
    Write-Host "  ? API project found" -ForegroundColor Green
} else {
    Write-Host "  ? API project NOT found" -ForegroundColor Red
}

if ($wpfProject) {
    Write-Host "  ? WPF project found" -ForegroundColor Green
} else {
    Write-Host "  ? WPF project NOT found" -ForegroundColor Red
}

# Step 4: Check if we're in the right directory
Write-Host "`n[4/5] Checking current directory..." -ForegroundColor Yellow
$currentDir = Get-Location
Write-Host "  Current: $currentDir" -ForegroundColor Gray

if ($currentDir -match "Wysg\.Musm\\apps\\Wysg\.Musm\.Radium") {
    Write-Host "  ? Already in WPF project directory" -ForegroundColor Green
} elseif (Test-Path "apps\Wysg.Musm.Radium") {
    Write-Host "  ??  In solution root (OK)" -ForegroundColor Gray
    Write-Host "  ¡æ Will cd to WPF project" -ForegroundColor Gray
} else {
    Write-Host "  ??  Not in expected directory" -ForegroundColor Yellow
}

# Step 5: Summary
Write-Host "`n[5/5] Summary" -ForegroundColor Yellow
Write-Host "  ????????????????????????????????????????" -ForegroundColor Gray

$allGood = $true

if ($response.StatusCode -ne 200) {
    Write-Host "  ? API not running" -ForegroundColor Red
    $allGood = $false
}

if ($useApi -ne "1") {
    Write-Host "  ??  USE_API not set to '1'" -ForegroundColor Yellow
    $allGood = $false
}

if (-not $wpfProject) {
    Write-Host "  ? WPF project not found" -ForegroundColor Red
    $allGood = $false
}

if ($allGood) {
    Write-Host "  ? All checks passed!" -ForegroundColor Green
    Write-Host "`n?????????????????????????????????????????????????" -ForegroundColor Green
    Write-Host "?  Ready to start WPF in API mode!             ?" -ForegroundColor Green
    Write-Host "?????????????????????????????????????????????????" -ForegroundColor Green
    
    Write-Host "`nNext steps:" -ForegroundColor Cyan
    Write-Host "  1. Make sure you're running this IN VISUAL STUDIO or" -ForegroundColor Gray
    Write-Host "     have DebugView running to see debug output" -ForegroundColor Gray
    Write-Host "  2. Press Enter to start WPF app..." -ForegroundColor Yellow
    $null = Read-Host
    
    # Set env vars if not set
    if ($useApi -ne "1") {
        $env:USE_API = "1"
        Write-Host "  ¡æ Set USE_API = 1" -ForegroundColor Gray
    }
    
    if (-not $apiUrl) {
        $env:RADIUM_API_URL = "http://localhost:5205"
        Write-Host "  ¡æ Set RADIUM_API_URL = http://localhost:5205" -ForegroundColor Gray
    }
    
    # Navigate to WPF project if needed
    if (-not ($currentDir -match "Wysg\.Musm\\apps\\Wysg\.Musm\.Radium")) {
        if (Test-Path "apps\Wysg.Musm.Radium") {
            Set-Location apps\Wysg.Musm.Radium
            Write-Host "  ¡æ Changed to WPF project directory" -ForegroundColor Gray
        }
    }
    
    Write-Host "`n?? Starting WPF app..." -ForegroundColor Cyan
    Write-Host "   Watch Visual Studio Output window (Ctrl+Alt+O)" -ForegroundColor Yellow
    Write-Host "   Look for: [DI] API Mode: ENABLED" -ForegroundColor Yellow
    Write-Host "`n" -ForegroundColor Gray
    
    dotnet run
} else {
    Write-Host "`n?????????????????????????????????????????????????" -ForegroundColor Red
    Write-Host "?  ? Some checks failed!                       ?" -ForegroundColor Red
    Write-Host "?????????????????????????????????????????????????" -ForegroundColor Red
    
    Write-Host "`nPlease fix the issues above and try again." -ForegroundColor Yellow
    Write-Host "`nPress any key to exit..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}
