# Change: Removed Deprecated Azure Central DB Code

**Date:** 2025-12-26  
**Status:** Completed  
**Category:** Cleanup / Architecture

## Context
With all central data transactions now handled via the Radium API, the legacy direct-DB services for Azure SQL and PostgreSQL central databases are no longer needed. This change removes all deprecated central DB code to simplify the codebase and eliminate dead code.

## Removed Files
### Direct-DB Services
- `Services/AzureSqlCentralService.cs` - Direct Azure SQL account management
- `Services/AzureSqlPhraseService.cs` - Azure SQL phrase storage (replaced by API)
- `Services/AzureSqlHotkeyService.cs` - Azure SQL hotkey storage (replaced by API)
- `Services/AzureSqlSnippetService.cs` - Azure SQL snippet storage (replaced by API)
- `Services/AzureSqlReportifySettingsService.cs` - Azure SQL settings storage (replaced by API)
- `Services/AzureSqlSnomedMapService.cs` - Azure SQL SNOMED mapping (replaced by API)
- `Services/PhraseService.cs` - Legacy PostgreSQL phrase service
- `Services/ReportifySettingsService.cs` - Legacy PostgreSQL settings service
- `Services/CentralDataSourceProvider.cs` - Shared NpgsqlDataSource management

### Removed from Settings
- `CentralConnectionString` property removed from `IRadiumLocalSettings` and `RadiumLocalSettings`

## Modified Files
- `Services/IRadiumLocalSettings.cs` - Removed `CentralConnectionString` property
- `Services/RadiumLocalSettings.cs` - Removed `CentralConnectionString` implementation
- `App.xaml.cs` - Removed `USE_API` feature flag and direct-DB service registrations; all central services now use API adapters exclusively
- `ViewModels/MainViewModel.cs` - Removed `ICentralDataSourceProvider` field
- `Services/StudynameLoincRepository.cs` - Removed `CentralConnectionString` fallback (uses only local connection)

## Architecture After Change
All central data access (phrases, hotkeys, snippets, settings, SNOMED mappings) now flows exclusively through:
1. `RadiumApiClient` - HTTP client for Radium API
2. API adapter services in `Services/Adapters/`:
   - `ApiPhraseServiceAdapter`
   - `ApiHotkeyServiceAdapter`
   - `ApiSnippetServiceAdapter`
   - `ApiSnomedMapServiceAdapter`
3. `ApiReportifySettingsService` - Settings via `IUserSettingsApiClient`

Local data (PACS profiles, studies, reports, LOINC mappings) continues to use direct PostgreSQL via `LocalConnectionString`.

## Testing
1. Verify app starts without errors
2. Login and confirm phrases load via API
3. Verify hotkeys, snippets, and settings work
4. Confirm Network tab no longer shows Central Connection String field
5. `dotnet build` ¡æ success
