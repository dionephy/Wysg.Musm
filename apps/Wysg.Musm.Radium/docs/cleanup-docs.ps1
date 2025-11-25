# Cleanup Temporary and Unneeded Documentation Files
# Date: 2025-11-25

$ErrorActionPreference = "Stop"

Write-Host "=== Documentation Cleanup ===" -ForegroundColor Cyan
Write-Host "Identifying temporary and unneeded files..." -ForegroundColor Yellow
Write-Host ""

$basePath = "apps\Wysg.Musm.Radium\docs"

# Categories of files to clean up
$cleanupCategories = @{
    "Backup Files (-OLD.md)" = Get-ChildItem -Path $basePath -Filter "*-OLD.md" -Recurse
    "Temporary Scripts (standardize-*.ps1)" = Get-ChildItem -Path $basePath -Filter "standardize-*.ps1" -Recurse
    "Date Update Scripts (update-*.ps1)" = Get-ChildItem -Path $basePath -Filter "update-*-dates.ps1" -Recurse
    "Analysis Scripts (analyze-*.ps1)" = Get-ChildItem -Path $basePath -Filter "analyze-*.ps1" -Recurse
    "Execution Scripts (execute-*.ps1)" = Get-ChildItem -Path $basePath -Filter "execute-*.ps1" -Recurse
    "Organization Scripts (organize-*.ps1, create-*.ps1)" = @(
        Get-ChildItem -Path $basePath -Filter "organize-*.ps1" -Recurse
        Get-ChildItem -Path $basePath -Filter "create-*.ps1" -Recurse
    )
    "Duplicate Phase Reports" = @(
        Get-ChildItem -Path $basePath -Filter "PHASE_*_PROGRESS.md" -Recurse
        Get-ChildItem -Path $basePath -Filter "PHASE_*_SUMMARY.md" -Recurse
    )
    "Old Planning Documents" = @(
        Get-ChildItem -Path $basePath -Filter "REORGANIZATION_*.md" -Recurse
        Get-ChildItem -Path $basePath -Filter "CONTENT_STANDARDIZATION_*.md" -Recurse
        Get-ChildItem -Path $basePath -Filter "PHASED_IMPLEMENTATION_*.md" -Recurse
        Get-ChildItem -Path $basePath -Filter "IMPLEMENTATION_*.md" -Recurse
        Get-ChildItem -Path $basePath -Filter "ALL_PHASES_*.md" -Recurse
        Get-ChildItem -Path $basePath -Filter "FINAL_COMPLETION_*.md" -Recurse
    )
    "Duplicate READMEs" = @(
        Get-ChildItem -Path $basePath -Filter "README_NEW.md" -Recurse
        Get-ChildItem -Path $basePath -Filter "README_RELATIVE_TIME.md" -Recurse
    )
}

$totalFiles = 0
$totalSize = 0

# Display what will be deleted
Write-Host "Files identified for cleanup:" -ForegroundColor Yellow
Write-Host ""

foreach ($category in $cleanupCategories.Keys | Sort-Object) {
    $files = $cleanupCategories[$category]
    if ($files.Count -gt 0) {
        Write-Host "[$category] - $($files.Count) files" -ForegroundColor Cyan
        $categorySize = ($files | Measure-Object -Property Length -Sum).Sum
        $totalFiles += $files.Count
        $totalSize += $categorySize
        
        foreach ($file in $files | Select-Object -First 5) {
            $size = "{0:N2}" -f ($file.Length / 1KB)
            Write-Host "  - $($file.Name) ($size KB)" -ForegroundColor Gray
        }
        
        if ($files.Count -gt 5) {
            Write-Host "  ... and $($files.Count - 5) more files" -ForegroundColor DarkGray
        }
        Write-Host ""
    }
}

Write-Host "Total: $totalFiles files, $("{0:N2}" -f ($totalSize / 1KB)) KB" -ForegroundColor Yellow
Write-Host ""

# Ask for confirmation
$response = Read-Host "Delete these files? (Y/N)"

if ($response -eq "Y" -or $response -eq "y") {
    Write-Host ""
    Write-Host "Deleting files..." -ForegroundColor Green
    
    $deletedCount = 0
    $errorCount = 0
    
    foreach ($category in $cleanupCategories.Keys) {
        $files = $cleanupCategories[$category]
        foreach ($file in $files) {
            try {
                Remove-Item -Path $file.FullName -Force
                $deletedCount++
                Write-Host "  ? Deleted: $($file.Name)" -ForegroundColor Green
            } catch {
                Write-Host "  ? Error deleting: $($file.Name) - $($_.Exception.Message)" -ForegroundColor Red
                $errorCount++
            }
        }
    }
    
    Write-Host ""
    Write-Host "=== Cleanup Complete ===" -ForegroundColor Cyan
    Write-Host "Deleted: $deletedCount files" -ForegroundColor Green
    Write-Host "Errors: $errorCount files" -ForegroundColor $(if ($errorCount -gt 0) { "Red" } else { "Green" })
    
} else {
    Write-Host ""
    Write-Host "Cleanup cancelled." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Essential files kept:" -ForegroundColor Cyan
Write-Host "  ? FINAL_MASTER_SUMMARY.md - Complete project summary" -ForegroundColor Green
Write-Host "  ? PHASE_*_COMPLETE.md - Phase completion reports" -ForegroundColor Green
Write-Host "  ? README.md - Main documentation index" -ForegroundColor Green
Write-Host "  ? All standardized .md files" -ForegroundColor Green
