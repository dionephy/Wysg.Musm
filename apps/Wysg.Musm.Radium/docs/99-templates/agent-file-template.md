# Agent Instructions Template

**Date**: 2025-11-11  
**Type**: Template  
**Category**: Documentation  
**Status**: ? Active Template

---

## Summary

This template provides a standardized structure for creating AI agent instruction files (e.g., `.github/copilot-instructions.md` for GitHub Copilot). Agent files help AI assistants understand your project's technologies, patterns, and conventions to provide better code suggestions.

---

## Purpose

### Why Use This Template?
- **Context for AI** - Helps AI understand your project structure and tech stack
- **Consistent Suggestions** - AI provides suggestions that match your conventions
- **Auto-Updated** - Can be regenerated from implementation plans
- **Token Efficient** - Keeps file under 150 lines for optimal AI processing
- **Manual Override** - Supports manual additions that won't be overwritten

### When to Use
- Setting up AI-assisted development for your project
- After completing implementation plans (to extract tech stack)
- When onboarding new AI tools (Copilot, Claude, etc.)
- Updating after major technology changes

---

## Template Structure

### Metadata Block
```markdown
# [PROJECT NAME] Development Guidelines

**Auto-generated**: Yes/No  
**Last updated**: [DATE]  
**Source**: Implementation plans in `/specs/`  
**Manual additions**: Between markers
```

---

## Section 1: Active Technologies

**Purpose**: List all technologies currently used in the project

**Format**:
```markdown
## Active Technologies

### Languages
- C# 12 (.NET 8)
- XAML (WPF)
- SQL (Azure SQL, T-SQL)

### Frameworks & Libraries
- **WPF** - Desktop UI framework
- **AvalonEdit** - Text editor component
- **xUnit** - Testing framework
- **Moq** - Mocking library
- **Azure SDK** - Cloud services integration

### Data & Storage
- **Azure SQL Database** - Primary data store
- **SQLite** - Local caching
- **Entity Framework Core** - ORM (if applicable)

### Tools & Build
- **Visual Studio 2022** - Primary IDE
- **Git** - Version control
- **.editorconfig** - Code style enforcement
```

**Example** (Radium Project):
```markdown
## Active Technologies

### Languages
- C# 12 (.NET 8, .NET 9, .NET 10)
- XAML (WPF)
- SQL (Azure SQL, T-SQL)
- PowerShell (automation scripts)

### Frameworks & Libraries
- **WPF** - Desktop UI framework (Windows 10+)
- **AvalonEdit 6.3** - Syntax-highlighting text editor
- **xUnit 2.6** - Unit testing framework
- **Moq 4.20** - Mocking for tests
- **Azure.Identity** - Azure authentication
- **Microsoft.Data.SqlClient** - Azure SQL connectivity
- **Newtonsoft.Json** - JSON serialization
- **DiffPlex** - Text diffing library

### Data & Storage
- **Azure SQL Database** - Phrases, SNOMED mappings, settings
- **SQLite** - Local phrase cache
- **JSON files** - Report data persistence

### Tools & Build
- **Visual Studio 2022** - Primary IDE
- **Git + GitHub** - Version control
- **.editorconfig** - C# conventions
- **PowerShell** - Build and deployment scripts
```

---

## Section 2: Project Structure

**Purpose**: Show the actual directory layout

**Format**:
```markdown
## Project Structure

### Source Code
```
apps/
������ Wysg.Musm.Radium/                    # Main WPF application
��   ������ ViewModels/                      # MVVM ViewModels
��   ������ Views/                           # XAML windows
��   ������ Controls/                        # Custom WPF controls
��   ������ Services/                        # Business logic services
��   ������ Models/                          # Data models
��   ������ Resources/                       # Images, styles, fonts
������ Wysg.Musm.Radium.Api/                # Backend API
    ������ Controllers/                     # API controllers
    ������ Services/                        # API services
    ������ Models/                          # API models

src/
������ Wysg.Musm.Domain/                    # Domain models (shared)
������ Wysg.Musm.Infrastructure/            # Data access (shared)
������ Wysg.Musm.Editor/                    # Editor components (shared)

tests/
������ Wysg.Musm.Tests/                     # Unit and integration tests
```

### Documentation
```
apps/Wysg.Musm.Radium/docs/
������ 00-current/                          # Active work (last 7 days)
������ 01-recent/                           # Recent work (last month)
������ 04-archive/                          # Historical docs
������ 11-architecture/                     # Architecture docs
������ 12-guides/                           # User & developer guides
������ 99-templates/                        # Document templates
```
```

---

## Section 3: Development Commands

**Purpose**: Common commands for building, testing, and running

**Format**:
```markdown
## Development Commands

### Build
```bash
# Build solution
dotnet build Wysg.Musm.sln

# Build specific project
dotnet build apps/Wysg.Musm.Radium/Wysg.Musm.Radium.csproj

# Clean and rebuild
dotnet clean && dotnet build
```

### Test
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Wysg.Musm.Tests/Wysg.Musm.Tests.csproj

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Run
```bash
# Run WPF application
dotnet run --project apps/Wysg.Musm.Radium/Wysg.Musm.Radium.csproj

# Run API
dotnet run --project apps/Wysg.Musm.Radium.Api/Wysg.Musm.Radium.Api.csproj
```

### Database
```bash
# Create migration (if using EF Core)
dotnet ef migrations add MigrationName --project src/Wysg.Musm.Infrastructure

# Update database
dotnet ef database update --project src/Wysg.Musm.Infrastructure
```
```

---

## Section 4: Code Style & Conventions

**Purpose**: Coding standards specific to this project

**Format**:
```markdown
## Code Style & Conventions

### C# Conventions
- **Naming**:
  - PascalCase for classes, methods, properties
  - camelCase for private fields (with `_` prefix)
  - UPPER_CASE for constants
- **Async**: Use `Async` suffix for async methods
- **Nullability**: Enable nullable reference types
- **File scoped namespaces**: Use file-scoped namespace declarations

### XAML Conventions
- **Naming**:
  - PascalCase for controls (e.g., `SubmitButton`, `UserNameTextBox`)
  - Prefix: Button��`Btn`, TextBox��`Txt`, ComboBox��`Cmb`
- **Layout**: Use Grid for complex layouts, StackPanel for simple
- **Binding**: Prefer MVVM with INotifyPropertyChanged

### Testing Conventions
- **Test naming**: `MethodName_Scenario_ExpectedResult`
  - Example: `GetStudies_ValidPatientId_ReturnsStudyList`
- **Arrange-Act-Assert**: Use AAA pattern
- **Mock naming**: `mock{InterfaceName}` (e.g., `mockRepository`)

### Documentation
- **XML comments**: Required for public APIs
- **Inline comments**: Use `//` for complex logic explanation
- **TODOs**: Format as `// TODO(username): description`
```

**Example**:
```csharp
/// <summary>
/// Retrieves all previous studies for a given patient.
/// </summary>
/// <param name="patientId">The unique patient identifier.</param>
/// <returns>A list of previous studies ordered by date (newest first).</returns>
public async Task<List<PreviousStudy>> GetStudiesForPatientAsync(string patientId)
{
    if (string.IsNullOrWhiteSpace(patientId))
        throw new ArgumentException("Patient ID cannot be null or empty", nameof(patientId));

    // Query cached studies first for performance
    var cachedStudies = await _cache.GetStudiesAsync(patientId);
    if (cachedStudies.Any())
        return cachedStudies;

    // Fall back to database if cache miss
    return await _repository.GetStudiesAsync(patientId);
}
```
```

---

## Section 5: Architecture Patterns

**Purpose**: Key architectural decisions and patterns

**Format**:
```markdown
## Architecture Patterns

### MVVM (Model-View-ViewModel)
- **Views**: XAML files, minimal code-behind
- **ViewModels**: Implement INotifyPropertyChanged, handle UI logic
- **Models**: Plain data classes, no UI concerns

### Dependency Injection
- Services registered in `App.xaml.cs` or `Startup.cs`
- Constructor injection preferred
- Avoid service locator pattern

### Async/Await
- All I/O operations async (database, network, file)
- Use `ConfigureAwait(false)` for library code
- Async all the way (no `Task.Result` or `.Wait()`)

### Error Handling
- Use exceptions for exceptional cases
- Validate inputs early
- Log exceptions with structured logging
```

---

## Section 6: Recent Changes

**Purpose**: Track recent features to help AI understand evolving codebase

**Format**:
```markdown
## Recent Changes

### 2025-11-11 - Previous Study Feature
**Added**:
- `PreviousStudyService` for loading historical studies
- `PreviousStudiesPanel` WPF control
- Azure SQL queries for study retrieval
- Caching layer with SQLite

**Tech**: C#, WPF, Azure SQL, SQLite

### 2025-11-10 - Web Automation Element Picker
**Added**:
- `WebElementPicker` for browser automation
- UI Automation API integration
- Bookmark persistence

**Tech**: C#, Windows UI Automation API

### 2025-11-09 - Dark Theme Enhancement
**Added**:
- Dark theme styles for TabControl
- Custom ResourceDictionary for colors
- Theme switching support

**Tech**: WPF, XAML, ResourceDictionary
```

**Note**: Keep only last 3-5 changes to stay under token limit

---

## Section 7: Manual Additions

**Purpose**: Preserve custom instructions that shouldn't be auto-generated

**Format**:
```markdown
<!-- MANUAL ADDITIONS START -->

## Project-Specific Rules
- Always use `RadiumLocalSettings` for application settings
- Never hardcode Azure connection strings
- Use `EditorAutofocusService` for editor focus management
- Previous study data must be cached locally for offline support

## Common Pitfalls
- **Cross-thread UI updates**: Use `Dispatcher.Invoke()` in WPF
- **AvalonEdit**: Set `Document.Text` carefully to avoid undo stack issues
- **Azure SQL**: Always use parameterized queries (never string concatenation)

<!-- MANUAL ADDITIONS END -->
```

**Important**: Anything between `<!-- MANUAL ADDITIONS START/END -->` markers is preserved during auto-regeneration.

---

## Auto-Generation Script

### PowerShell Script Example
```powershell
# update-agent-context.ps1
# Auto-generates agent file from implementation plans

param(
    [string]$AgentType = "copilot",  # copilot, claude, gemini, etc.
    [string]$SpecsPath = "./specs",
    [string]$OutputPath = ".github/copilot-instructions.md"
)

# Extract technologies from all plan.md files
$technologies = Get-ChildItem -Path $SpecsPath -Filter "plan.md" -Recurse | 
    ForEach-Object { 
        # Parse Technical Context section
        # Extract language, frameworks, dependencies
    }

# Generate agent file
$content = @"
# Project Development Guidelines

**Auto-generated**: Yes
**Last updated**: $(Get-Date -Format "yyyy-MM-dd")

## Active Technologies
$($technologies -join "`n")

<!-- MANUAL ADDITIONS START -->
[Preserve existing content]
<!-- MANUAL ADDITIONS END -->
"@

$content | Out-File $OutputPath -Encoding UTF8
```

---

## Usage Examples

### For GitHub Copilot
**File**: `.github/copilot-instructions.md`
**Token Limit**: ~150 lines (optimal)

### For Claude Code (Cursor/Windsurf)
**File**: `CLAUDE.md` or `.cursorrules`
**Token Limit**: ~200 lines

### For Gemini CLI
**File**: `GEMINI.md`
**Token Limit**: ~150 lines

### For Qwen Code
**File**: `QWEN.md`
**Token Limit**: ~150 lines

### Generic (Multi-Agent)
**File**: `AGENTS.md`
**Token Limit**: ~150 lines

---

## Related Documents

- [Spec Template](spec-template.md) - For feature specifications
- [Plan Template](plan-template.md) - For implementation plans
- [Tasks Template](tasks-template.md) - For task tracking

---

## Tips & Best Practices

### Do's ?
- ? Keep file under 150 lines for optimal AI token usage
- ? Update after major technology changes
- ? Use markers for manual additions
- ? Include only ACTIVE technologies (not planned)
- ? Provide concrete examples

### Don'ts ?
- ? Don't include every possible technology
- ? Don't duplicate information from other docs
- ? Don't forget to preserve manual additions during regeneration
- ? Don't exceed 200 lines (AI token limit)

---

## Changelog

### 2025-11-11 - Standardization
- Added metadata block and summary
- Added comprehensive examples
- Added auto-generation script example
- Added multi-agent support
- Improved structure and formatting

---

**Last Updated**: 2025-11-25  
**Template Version**: 2.0  
**Maintained By**: Documentation Team
