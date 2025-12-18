# Fix: Header Format persistence and per-user automation storage
**Date**: 2025-12-18  
**Status**: Fixed  
**Build**: Success

## Issues
1. Header format template was saved locally only, not persisting to central DB - so changes were lost when re-opening settings
2. Automation, bookmarks, procedures, and custom modules were stored only per PACS, causing collisions between users and missing procedures (e.g., custom procedure `G3_WorklistVisible` not found when switching accounts)
3. Path resolution used captured values at login time instead of dynamically resolving based on current tenant context
4. **Root Cause Found**: When `AccountStoragePaths._tenantContext` was null, fallback path was `Radium/ui-procedures.json` which contained old procedures from October without `G3_WorklistVisible`
5. **Additional Root Cause**: `custom-modules.json` was only stored at root `Radium/` location, not per-account. The `CustomModuleStore` was not finding modules because it looked in `Accounts/{id}/Pacs/{key}/` which had no `custom-modules.json` file.

## Root Cause Analysis
1. **Header template**: `ApplyReportifyJson()` and `UpdateReportifyJson()` didn't include `header_format_template` field, so it was never saved/loaded from central DB
2. **Automation paths**: The fallback path when tenant context was null went to root `Radium/` directory instead of legacy `Pacs/default_pacs/` directory
3. **Tenant context null**: If `AccountStoragePaths.Initialize()` wasn't called or tenant context was garbage collected, procedures would load from wrong location
4. **Custom modules not migrated**: Unlike `ui-procedures.json` which had migration logic, `custom-modules.json` had NO migration - so per-account paths were empty

## Solution

### 1. Fixed Header Template Central Persistence
- Added `header_format_template` field to `UpdateReportifyJson()` for saving
- Added `GetStr()` helper and loading in `ApplyReportifyJson()` 
- Updated `Save()` to call `SaveReportifySettingsAsync()` for central persistence
- Constructor now prefers central settings over local fallback

### 2. Fixed AccountStoragePaths Fallback Logic
- Added auto-resolution of tenant context from DI if `_tenantContext` is null
- Changed fallback path from `Radium/` to `Radium/Pacs/default_pacs/` which contains the actual procedures
- Added more debug logging to trace path resolution issues

### 3. Fixed CustomModuleStore Migration
- **NEW**: Added migration logic to `CustomModuleStore.GetStorePath()` 
- If the per-account path doesn't exist, checks for legacy root location
- Copies from `Radium/custom-modules.json` to `Accounts/{id}/Pacs/{key}/custom-modules.json`
- Added debug logging to trace module loading and lookup

### 4. Created Centralized AccountStoragePaths Service
File `Services/AccountStoragePaths.cs`:
- Single source of truth for all per-account storage paths
- Dynamically resolves paths based on current `ITenantContext` values
- Auto-resolves tenant context from DI if not initialized
- Handles legacy file migration automatically
- Provides `GetProceduresPath()`, `GetBookmarksPath()`, `GetCustomModulesPath()`, `GetAutomationPath()`

### 5. Updated App.xaml.cs Login Flow
- Call `AccountStoragePaths.Initialize(tenant)` after login success
- Call `AccountStoragePaths.ConfigurePathOverrides()` to set up all services with dynamic resolution

### 6. Updated PACS Profile Switching
- `OnSelectedPacsProfileChanged` now calls `AccountStoragePaths.ConfigurePathOverrides()` instead of setting hardcoded paths

### 7. Added Debug Logging
- `ProcedureExecutor.Storage.cs`: Log path resolution and procedure loading
- `AccountStoragePaths.cs`: Log path resolution details and tenant context state
- `CustomModule.cs` (CustomModuleStore): Log path resolution, load counts, and module lookups

## Path Resolution Order
1. If `_tenantContext` is initialized: Use `Accounts/{accountId}/Pacs/{pacsKey}/`
2. If `_tenantContext` is null, try to resolve from DI
3. If still null: Fall back to legacy `Pacs/default_pacs/` (NOT root `Radium/`)

## Migration Logic
Both `ProcedureExecutor` and `CustomModuleStore` now handle migration:
- When a per-account file is requested but doesn't exist
- Check for legacy location at `Pacs/{pacsKey}/` or root `Radium/`
- Copy the file to the new per-account location

## File Locations
```
%APPDATA%/Wysg.Musm/Radium/
戍式式 ui-procedures.json          ∠ OLD root fallback (Oct 2025, incomplete)
戍式式 custom-modules.json         ∠ OLD root location (will be migrated)
戍式式 Accounts/
弛   戍式式 {accountId}/            ∠ Per-user scoping (PREFERRED)
弛   弛   戌式式 Pacs/
弛   弛       戌式式 {pacsKey}/
弛   弛           戍式式 ui-procedures.json   ∠ Contains G3_WorklistVisible
弛   弛           戍式式 custom-modules.json  ∠ Contains If not G3_WorklistVisible (migrated)
弛   弛           戍式式 bookmarks.json
弛   弛           戌式式 automation.json
戍式式 Pacs/                       ∠ Legacy (fallback when tenant context unavailable)
弛   戌式式 {pacsKey}/
弛       戌式式 ui-procedures.json  ∠ Contains G3_WorklistVisible
```

## Files Changed
- `Services/AccountStoragePaths.cs` (enhanced fallback + DI auto-resolve)
- `Services/ProcedureExecutor.Storage.cs` (debug logging)
- `Models/CustomModule.cs` (**NEW**: migration logic + debug logging)
- `App.xaml.cs` (use AccountStoragePaths)
- `Views/AutomationWindow.Procedures.Exec.cs` (use AccountStoragePaths)
- `ViewModels/SettingsViewModel.cs` (header template central persistence)
- `ViewModels/SettingsViewModel.PacsProfiles.cs` (use AccountStoragePaths for PACS switching)

## Verification
- Build: ? Success
- Automation paths now scoped by account ID
- Fallback to legacy `Pacs/default_pacs/` when tenant context unavailable
- **CustomModuleStore now migrates from legacy root location**
- Header template saved centrally via `SaveReportifySettingsAsync()`
