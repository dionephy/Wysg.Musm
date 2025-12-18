# Settings Storage Architecture

## Date
2025-12-18

## Summary
Reorganized settings storage to ensure proper separation of concerns:
- **Network settings**: Global local storage (shared across all users/PACS)
- **PACS settings**: Per-tenant local storage (per user + PACS profile)
- **Keyboard settings**: Global local storage (per machine)
- **Report format settings**: Central Azure SQL storage (synced across devices)
- **Automation/Custom modules**: Per-tenant local storage (per user + PACS profile)

## Storage Locations

### 1. Global Local Settings (IRadiumLocalSettings)
**Location**: `%LOCALAPPDATA%\Wysg.Musm\Radium\settings.dat`
**Encrypted**: Yes (DPAPI)
**Scope**: Per machine, shared across all users and PACS profiles

Contains:
- `LocalConnectionString` - Local PostgreSQL connection
- `CentralConnectionString` - Azure SQL connection  
- `SnowstormBaseUrl` - SNOMED API URL
- `ModalitiesNoHeaderUpdate` - Global modality configuration
- `SessionBasedCacheBookmarks` - Cache configuration
- `MainWindowPlacement` - Window position/size
- Global hotkeys (`GlobalHotkeyOpenStudy`, `GlobalHotkeySendStudy`, `GlobalHotkeyToggleSyncText`)
- Editor autofocus settings

### 2. Per-Tenant Local Settings (PACS-scoped)
**Location**: `%APPDATA%\Wysg.Musm\Radium\Pacs\{pacsKey}\`
**Scope**: Per user + PACS profile

Files:
- `automation.json` - Automation sequences (NewStudy, AddStudy, SendReport, etc.)
- `ui-procedures.json` - Custom procedures
- `bookmarks.json` - UI Bookmarks (UiBookmarks)
- `custom-modules.json` - Custom modules (CustomModuleStore) **[NEW]**

### 3. Central Storage (Azure SQL - radium.user_setting)
**Table**: `radium.user_setting`
**Scope**: Per account (synced across devices)

Contains Reportify settings JSON:
- Text formatting options (blanks, capitalization, punctuation, etc.)
- Arrow/bullet spacing options
- Conclusion numbering options
- Header format template **[NEW]**
- Default symbols (arrow, bullet, DDx, etc.)

## Changes Made

### CustomModuleStore Path Override
Added `GetStorePathOverride` static property to enable per-tenant storage:

```csharp
// CustomModule.cs
public class CustomModuleStore
{
    public static Func<string>? GetStorePathOverride { get; set; }
    // ...
}
```

Set in `App.xaml.cs` and `SettingsViewModel.PacsProfiles.cs` when PACS profile changes:
```csharp
var customModulesPath = Path.Combine(baseDir, "custom-modules.json");
CustomModuleStore.GetStorePathOverride = () => customModulesPath;
```

### HeaderFormatTemplate Moved to Central Storage
Previously stored in local settings, now part of Reportify settings JSON:

```json
{
  "remove_excessive_blanks": true,
  "capitalize_sentence": true,
  // ... other reportify settings ...
  "header_format_template": "Clinical information: {Chief Complaint}\n- {Patient History Lines}\nTechniques: {Techniques}\nComparison: {Comparison}",
  "defaults": {
    "arrow": "-->",
    "conclusion_numbering": "1.",
    "detailing_prefix": "-",
    "differential_diagnosis": "DDx:"
  }
}
```

## File Changes

1. **apps/Wysg.Musm.Radium/Models/CustomModule.cs**
   - Added `GetStorePathOverride` static property to `CustomModuleStore`

2. **apps/Wysg.Musm.Radium/App.xaml.cs**
   - Set `CustomModuleStore.GetStorePathOverride` alongside bookmarks/procedures path

3. **apps/Wysg.Musm.Radium/ViewModels/SettingsViewModel.PacsProfiles.cs**
   - Set `CustomModuleStore.GetStorePathOverride` on PACS profile change

4. **apps/Wysg.Musm.Radium/ViewModels/SettingsViewModel.cs**
   - Added `header_format_template` to `UpdateReportifyJson()`
   - Added loading of `header_format_template` in `ApplyReportifyJson()`
   - Removed local storage of `HeaderFormatTemplate`

5. **apps/Wysg.Musm.Radium/ViewModels/SettingsViewModel.HeaderFormat.cs**
   - Updated to trigger `UpdateReportifyJson()` on property change

## Storage Diagram

```
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛                    SETTINGS STORAGE ARCHITECTURE                 弛
戍式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛                                                                  弛
弛  忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖  弛
弛  弛 GLOBAL LOCAL (settings.dat)                              弛  弛
弛  弛 - Network: Connection strings, API URLs                   弛  弛
弛  弛 - Keyboard: Global hotkeys, Editor autofocus             弛  弛
弛  弛 - Window: Position, size, always-on-top                  弛  弛
弛  弛 Scope: Per machine                                        弛  弛
弛  戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎  弛
弛                                                                  弛
弛  忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖  弛
弛  弛 PER-TENANT LOCAL (Pacs/{pacsKey}/)                       弛  弛
弛  弛 - automation.json: Module sequences                       弛  弛
弛  弛 - ui-procedures.json: Custom procedures                   弛  弛
弛  弛 - bookmarks.json: UI Bookmarks                           弛  弛
弛  弛 - custom-modules.json: Custom modules                     弛  弛
弛  弛 Scope: Per user + PACS profile                           弛  弛
弛  戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎  弛
弛                                                                  弛
弛  忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖  弛
弛  弛 CENTRAL (radium.user_setting)                            弛  弛
弛  弛 - Report format settings (Reportify options)              弛  弛
弛  弛 - Header format template                                  弛  弛
弛  弛 - Default symbols                                         弛  弛
弛  弛 Scope: Per account (synced across devices)               弛  弛
弛  戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎  弛
弛                                                                  弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
```

## Behavior Summary

| Setting Category | Storage Type | Sync Behavior |
|------------------|--------------|---------------|
| Network (connection strings) | Local global | Machine-specific, no sync |
| PACS profiles | Central DB (app.account + local tenant) | Synced (profiles), local (details) |
| Keyboard (hotkeys, autofocus) | Local global | Machine-specific, no sync |
| Report Format | Central DB (radium.user_setting) | Synced across devices |
| UI Bookmarks | Per-tenant local | No sync (per PACS profile) |
| Custom Procedures | Per-tenant local | No sync (per PACS profile) |
| Custom Modules | Per-tenant local | No sync (per PACS profile) |
| Automation Sequences | Per-tenant local | No sync (per PACS profile) |
