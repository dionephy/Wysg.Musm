# File Split Summary: MainViewModel.Commands.cs

**Date**: 2025-02-09  
**Original File Size**: ~1800 lines  
**Status**: ? Split complete - ?? Build error needs fixing

---

## New File Structure

The large `MainViewModel.Commands.cs` file has been split into **5 smaller, focused files**:

### 1. **MainViewModel.Commands.Init.cs** (~150 lines)
**Purpose**: Command initialization and UI toggle properties

**Contains**:
- All `ICommand` property declarations
- UI mode toggle properties (`ProofreadMode`, `PreviousProofreadMode`, etc.)
- Auto-generation toggle properties
- `PatientLocked` state management
- `InitializeCommands()` method

### 2. **MainViewModel.Commands.Handlers.cs** (~270 lines)
**Purpose**: User-initiated command handlers (button clicks, UI actions)

**Contains**:
- `OnSendReportPreview()`, `OnSendReport()`
- `OnRunAddStudyAutomation()`, `OnRunTestAutomation()`, `OnNewStudy()`
- `RunOpenStudyShortcut()`, `RunSendReportShortcut()`
- `OnSelectPrevious()`, `OnGenerateField()`
- `OnEditStudyTechnique()`, `OnEditComparison()`
- `OnSavePreorder()`, `OnSavePreviousStudyToDB()`

### 3. **MainViewModel.Commands.Automation.cs** (~800 lines)
**Purpose**: Automation module execution

**Contains**:
- `RunModulesSequentially()` - Main automation dispatcher
- `RunNewStudyProcedureAsync()`
- `AcquireStudyRemarkAsync()`, `AcquirePatientRemarkAsync()`
- `RemoveDuplicateLinesInPatientRemark()`, `ExtractAngleBracketContent()`
- `RunOpenStudyAsync()`, `RunGetUntilReportDateTimeAsync()`, `RunGetReportedReportAsync()`
- `RunSetCurrentInMainScreenAsync()`, `RunOpenWorklistAsync()`, `RunResultsListSetFocusAsync()`
- `RunSendReportAsync()`, `RunSendReportModuleWithRetryAsync()`
- `RunSaveCurrentStudyToDBAsync()`, `RunSavePreviousStudyToDBAsync()`

### 4. **MainViewModel.Commands.AddPreviousStudy.cs** (~450 lines)
**Purpose**: AddPreviousStudy automation module (complex, self-contained feature)

**Contains**:
- `RunAddPreviousStudyModuleAsync()` - Main AddPreviousStudy logic with:
  - Patient validation
  - Study metadata reading from PACS
  - Current study validation check
  - Duplicate detection (study + report datetime)
  - Database persistence
  - Previous study loading
- `UpdateComparisonFromPreviousStudyAsync()` - Auto-fill Comparison field

### 5. **MainViewModel.Commands.Helpers.cs** (~100 lines)
**Purpose**: Helper classes and automation configuration

**Contains**:
- `AutomationSettings` class (deserialization model)
- `GetAutomationSequenceForCurrentPacs()` - Read automation sequences from JSON
- `GetAutomationFilePath()`, `SanitizeFileName()` - File path helpers
- `DelegateCommand` class - ICommand implementation

---

## Build Error

**Error**: `CS0103: '_local' 이름이 현재 컨텍스트에 없습니다.`  
**Location**: `MainViewModel.Commands.AddPreviousStudy.cs` lines 406, 408

**Cause**: The `_local` field (type `IRadiumLocalSettings`) is defined in the main `MainViewModel.cs` file and is not accessible in the partial class files because it's a private field.

**Solution Options**:

1. **Change `_local` to `protected`** in `MainViewModel.cs`:
   ```csharp
   protected readonly IRadiumLocalSettings? _local;
   ```

2. **Or add a property accessor** in `MainViewModel.cs`:
   ```csharp
   protected IRadiumLocalSettings? LocalSettings => _local;
   ```
   Then update `UpdateComparisonFromPreviousStudyAsync` to use `LocalSettings` instead of `_local`.

---

## Benefits of Split Structure

? **Improved Maintainability**: Each file has a clear, single purpose  
? **Easier Navigation**: Find specific functionality quickly  
? **Better Code Review**: Smaller, focused diffs  
? **Reduced Merge Conflicts**: Changes isolated to relevant files  
? **Logical Grouping**: Related methods stay together

---

## File Organization Logic

```
MainViewModel.Commands.Init.cs
├─ Properties (commands + toggles)
└─ Initialization

MainViewModel.Commands.Handlers.cs
├─ User Actions (button clicks)
└─ Shortcut dispatchers

MainViewModel.Commands.Automation.cs
├─ Module dispatcher (RunModulesSequentially)
├─ Individual automation modules
└─ Helper automation tasks

MainViewModel.Commands.AddPreviousStudy.cs
├─ AddPreviousStudy (complex feature)
└─ UpdateComparison (related helper)

MainViewModel.Commands.Helpers.cs
├─ Configuration models
├─ File path helpers
└─ Command infrastructure
```

---

## Migration Notes

- **No Breaking Changes**: All public interfaces remain the same
- **Partial Classes**: All files are `public partial class MainViewModel`
- **Namespace**: All files in `Wysg.Musm.Radium.ViewModels`
- **Dependencies**: Each file references fields/properties/methods from other partial class files
- **Access Modifiers**: All methods maintain their original visibility

---

## Next Steps

1. ? **Fix `_local` access issue** (change to protected or add property)
2. ? **Verify build succeeds**
3. ? **Test automation sequences** (New Study, Add Study, Send Report)
4. ? **Test AddPreviousStudy module** with comparison update
5. ? **Document in changelog**

---

## Related Files Modified

- ? **Removed**: `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.cs`
- ? **Created**: 5 new partial class files (Init, Handlers, Automation, AddPreviousStudy, Helpers)
- ?? **Needs Update**: `apps/Wysg.Musm.Radium/ViewModels/MainViewModel.cs` (change `_local` access)
