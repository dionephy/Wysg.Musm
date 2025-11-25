# Auto-Archive Documentation Script
# Moves documents to age-appropriate folders based on relative time periods

param(
    [string]$BasePath = ".",
    [switch]$DryRun = $false,
    [switch]$Verbose = $false
)

$ErrorActionPreference = "Stop"

# Color output functions
function Write-Success { param($msg) Write-Host $msg -ForegroundColor Green }
function Write-Info { param($msg) Write-Host $msg -ForegroundColor Cyan }
function Write-Warning { param($msg) Write-Host $msg -ForegroundColor Yellow }
function Write-Error { param($msg) Write-Host $msg -ForegroundColor Red }

Write-Info "===================================================="
Write-Info "Documentation Auto-Archive Script"
Write-Info "===================================================="
Write-Host ""

if ($DryRun) {
    Write-Warning "DRY RUN MODE - No files will be moved"
    Write-Host ""
}

# Get current date
$today = Get-Date

# Statistics
$stats = @{
    Analyzed = 0
    Moved = 0
    Skipped = 0
    Errors = 0
}

# Helper function to extract date from filename
function Get-DocumentDate {
    param([string]$filename)
    
    # Try to extract date from filename pattern: *_YYYY-MM-DD_*
    if ($filename -match '_(\d{4})-(\d{2})-(\d{2})_') {
        try {
            return [DateTime]::ParseExact($matches[0].Trim('_'), 'yyyy-MM-dd', $null)
        } catch {
            return $null
        }
    }
    
    # Try to extract from pattern: *_YYYY_MM_DD_*
    if ($filename -match '_(\d{4})_(\d{2})_(\d{2})_') {
        try {
            $dateStr = "$($matches[1])-$($matches[2])-$($matches[3])"
            return [DateTime]::ParseExact($dateStr, 'yyyy-MM-dd', $null)
        } catch {
            return $null
        }
    }
    
    return $null
}

# Helper function to calculate age in days
function Get-DocumentAge {
    param([DateTime]$docDate, [DateTime]$currentDate)
    return ($currentDate - $docDate).TotalDays
}

# Helper function to determine target folder
function Get-TargetFolder {
    param([double]$ageDays)
    
    if ($ageDays -le 6) {
        return "00-current"
    }
    elseif ($ageDays -le 13) {
        return "01-recent/week-1"
    }
    elseif ($ageDays -le 20) {
        return "01-recent/week-2"
    }
    elseif ($ageDays -le 27) {
        return "01-recent/week-3"
    }
    elseif ($ageDays -le 34) {
        return "01-recent/week-4"
    }
    elseif ($ageDays -le 64) {
        return "02-this-quarter/month-1"
    }
    elseif ($ageDays -le 90) {
        return "02-this-quarter/month-2"
    }
    elseif ($ageDays -le 180) {
        # Determine quarter based on document date
        $quarterYear = $docDate.Year
        $quarter = [Math]::Ceiling($docDate.Month / 3)
        return "03-last-quarter/$quarterYear-Q$quarter"
    }
    else {
        # Archive by year
        return "04-archive/$($docDate.Year)"
    }
}

# Get all markdown files in the docs directory (excluding subdirectories initially)
$docsPath = Join-Path $BasePath "."
$files = Get-ChildItem -Path $docsPath -Filter "*.md" -File | Where-Object {
    # Exclude files without dates and special files
    $_.Name -notmatch "^README" -and 
    $_.Name -notmatch "^INDEX" -and
    $_.Name -notmatch "^QUICKSTART" -and
    $_.Name -notmatch "^REORGANIZATION" -and
    $_.Name -notmatch "^PHASE_" -and
    $_.Name -notmatch "^RELATIVE_TIME" -and
    $_.Name -notmatch "^Spec-" -and
    $_.Name -notmatch "^Plan-" -and
    $_.Name -notmatch "^Tasks" -and
    $_.Name -match '_\d{4}[-_]\d{2}[-_]\d{2}_'
}

Write-Info "Found $($files.Count) dated documents to analyze"
Write-Host ""

# Process each file
foreach ($file in $files) {
    $stats.Analyzed++
    
    # Extract date from filename
    $docDate = Get-DocumentDate -filename $file.Name
    
    if ($null -eq $docDate) {
        Write-Warning "Could not extract date from: $($file.Name)"
        $stats.Skipped++
        continue
    }
    
    # Calculate age
    $ageDays = Get-DocumentAge -docDate $docDate -currentDate $today
    
    # Determine target folder
    $targetFolder = Get-TargetFolder -ageDays $ageDays
    $targetPath = Join-Path $BasePath $targetFolder
    
    # Check if file is already in correct location
    $currentFolder = Split-Path $file.FullName -Parent
    $currentFolderRelative = $currentFolder.Replace($BasePath, "").TrimStart('\', '/')
    
    if ($currentFolderRelative -eq $targetFolder -or $currentFolder -eq $targetPath) {
        if ($Verbose) {
            Write-Success "  Already in correct location: $($file.Name) ($targetFolder)"
        }
        $stats.Skipped++
        continue
    }
    
    # Create target directory if it doesn't exist
    if (-not (Test-Path $targetPath)) {
        if (-not $DryRun) {
            New-Item -ItemType Directory -Path $targetPath -Force | Out-Null
        }
        Write-Info "  Created directory: $targetFolder"
    }
    
    # Move file
    $targetFile = Join-Path $targetPath $file.Name
    
    if ($DryRun) {
        Write-Info "  [DRY RUN] Would move: $($file.Name)"
        Write-Info "    From: $currentFolderRelative"
        Write-Info "    To: $targetFolder"
        Write-Info "    Age: $([Math]::Round($ageDays, 0)) days"
    }
    else {
        try {
            Move-Item -Path $file.FullName -Destination $targetFile -Force
            Write-Success "  Moved: $($file.Name) ¡æ $targetFolder ($([Math]::Round($ageDays, 0)) days old)"
            $stats.Moved++
        }
        catch {
            Write-Error "  ERROR moving $($file.Name): $_"
            $stats.Errors++
        }
    }
}

# Summary
Write-Host ""
Write-Info "===================================================="
Write-Info "Auto-Archive Summary"
Write-Info "===================================================="
Write-Host ""
Write-Host "  Analyzed: $($stats.Analyzed) files" -ForegroundColor Cyan
Write-Host "  Moved: $($stats.Moved) files" -ForegroundColor Green
Write-Host "  Skipped: $($stats.Skipped) files" -ForegroundColor Yellow
Write-Host "  Errors: $($stats.Errors) files" -ForegroundColor Red
Write-Host ""

if ($DryRun) {
    Write-Warning "This was a DRY RUN - no files were actually moved"
    Write-Host "Run without -DryRun parameter to actually move files"
    Write-Host ""
}

Write-Info "Auto-archive complete!"
