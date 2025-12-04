# Enhancement: Rename Obsolete Built-in Modules and Update UI Styling (2025-12-04)

**Date**: 2025-12-04  
**Type**: Enhancement  
**Status**: ? Complete  
**Priority**: Normal

---

## Summary

Renamed several built-in automation modules to include an "(obs)" suffix to indicate they are obsolete/deprecated, and updated the UI to display these modules in grey instead of orange in the Automation settings tab.

## Changes

### Module Renames

| Old Name | New Name |
|----------|----------|
| `NewStudy(Obsolete)` | `NewStudy(obs)` |
| `AbortIfWorklistClosed` | `AbortIfWorklistClosed(obs)` |
| `GetStudyRemark` | `GetStudyRemark(obs)` |
| `GetPatientRemark` | `GetPatientRemark(obs)` |
| `AddPreviousStudy` | `AddPreviousStudy(obs)` |
| `OpenStudy` | `OpenStudy(obs)` |
| `SetCurrentInMainScreen` | `SetCurrentInMainScreen(obs)` |

### UI Changes

- Modules containing "(obs)" are now displayed in **grey** (#808080) instead of orange (#FFA000)
- This applies to both:
  - Available Modules list
  - Configured sequence panes (New Study, Send Report, etc.)

## Implementation Details

### Files Created

1. **`Converters/ObsoleteModuleColorConverter.cs`**
   - Returns grey color for modules containing "(obs)"
   - Returns orange color for normal built-in modules
   - Used in AutomationSettingsTab.xaml

### Files Modified

1. **`ViewModels/SettingsViewModel.cs`**
   - Updated `AvailableModules` list with renamed module names

2. **`ViewModels/MainViewModel.Commands.Automation.Core.cs`**
   - Updated module handlers to recognize both old and new names for backward compatibility
   - Example: Both `GetStudyRemark` and `GetStudyRemark(obs)` call the same handler

3. **`Converters/CustomModuleSyntaxConverter.cs`**
   - Updated to detect obsolete modules (containing "(obs)") and color them grey
   - Normal built-in modules remain orange

4. **`Views/SettingsTabs/AutomationSettingsTab.xaml`**
   - Added `ObsoleteModuleColorConverter` resource
   - Updated `OrderedModuleTemplate` and `LibraryModuleTemplate` to use the converter for text color

## Visual Example

### Before
```
[NewStudy(Obsolete)] - Orange
[GetStudyRemark]     - Orange
[AddPreviousStudy]   - Orange
```

### After
```
[NewStudy(obs)]           - Grey
[GetStudyRemark(obs)]     - Grey
[AddPreviousStudy(obs)]   - Grey
[SetStudyLocked]          - Orange (current module)
[FetchPreviousStudies]    - Orange (current module)
```

## Backward Compatibility

? **Full backward compatibility maintained**

Existing automation sequences using the old module names will continue to work:
- `NewStudy(Obsolete)` ¡æ Still works
- `GetStudyRemark` ¡æ Still works
- `AbortIfWorklistClosed` ¡æ Still works

The automation engine recognizes both old and new names for all renamed modules.

## Why These Modules Are Obsolete

These modules are marked obsolete because they have been replaced by newer, more modular alternatives:

| Obsolete Module | Recommended Alternative |
|-----------------|------------------------|
| `NewStudy(obs)` | Use combination of modular modules |
| `GetStudyRemark(obs)` | Use custom procedure with PACS getters |
| `GetPatientRemark(obs)` | Use custom procedure with PACS getters |
| `AddPreviousStudy(obs)` | Use `FetchPreviousStudies` + `InsertPreviousStudy` |
| `OpenStudy(obs)` | Use custom procedure with proper sequencing |
| `SetCurrentInMainScreen(obs)` | Use custom procedure with bookmarks |
| `AbortIfWorklistClosed(obs)` | Use `If` module with condition procedure |

## Color Legend in Automation Settings

| Color | Meaning |
|-------|---------|
| **Orange** (#FFA000) | Current/active built-in module |
| **Grey** (#808080) | Obsolete/deprecated module (still works) |
| **Green** (#6ABE30) | Property name in custom module |
| **Cyan** (#4EC9B0) | Bookmark/procedure name in custom module |

## Testing Checklist

- [x] All renamed modules appear correctly in Available Modules list
- [x] Grey color applied to modules with "(obs)" suffix
- [x] Orange color remains for current modules
- [x] Existing automation sequences with old names still work
- [x] New module names work when added to sequences
- [x] Build succeeds with no errors

---

**Build Status**: ? Success  
**Backward Compatible**: ? Yes  
**Ready for Use**: ? Complete
