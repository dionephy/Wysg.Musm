# Wysg.Musm.SnomedTools

A reusable WPF library for SNOMED-CT terminology tools and utilities.

## Overview

This library provides WPF windows and ViewModels for working with SNOMED-CT clinical terminology. It's designed to be framework-agnostic and can be integrated into any WPF application that needs SNOMED-CT functionality.

## Standalone Test Mode

**NEW**: The project can now be run as a standalone application for testing and development!

### Running Standalone

1. Set `Wysg.Musm.SnomedTools` as the startup project in Visual Studio
2. Press F5 to run
3. The test window will launch with two modes:
   - **Mock Mode**: Sample SNOMED concepts (no database/network required)
   - **Real Mode**: Connect to actual Azure SQL and Snowstorm API

### Configuring Real Services

To use real SNOMED CT data and database:

1. Click **"? Settings"** in the test window
2. Configure:
   - **Azure SQL Connection String**: Your Azure SQL database connection
     - Example: `Server=tcp:yourserver.database.windows.net,1433;Initial Catalog=musmdb;Encrypt=True;Authentication=Active Directory Default;`
   - **Snowstorm Base URL**: SNOMED CT terminology server endpoint
     - Example: `https://snowstorm.ihtsdotools.org/snowstorm`
3. Test connections to verify configuration
4. Settings are automatically saved to: `%LocalAppData%\Wysg.Musm.SnomedTools\settings.json`

### Service Modes

#### Mock Mode (Default)
- Pre-configured mock services (no database required)
- Sample SNOMED concepts for testing (Heart, MI, etc.)
- All UI functionality available
- Perfect for rapid UI/UX development iteration

#### Real Mode
- Connects to actual Azure SQL database
- Fetches real SNOMED CT data from Snowstorm API
- Full production-like behavior
- Requires valid connection configuration

**Features in Test Mode:**
- Configurable service backend (Mock vs Real)
- Connection testing before use
- Persistent configuration storage
- All UI functionality available

### Mock Services

The standalone mode includes built-in mock implementations:
- `MockSnowstormClient` - Returns sample SNOMED concepts (Heart, MI, etc.)
- `MockPhraseService` - Simulates phrase creation/retrieval
- `MockSnomedMapService` - Simulates concept caching and mapping

### Real Services

When configured, real service implementations include:
- `RealSnowstormClient` - Actual SNOMED CT Snowstorm API client
- `RealPhraseService` - Azure SQL phrase management
- `RealSnomedMapService` - Azure SQL SNOMED mapping service

See `TestWindow.xaml.cs` for implementation details.

## Components

### SNOMED Word Count Importer

A tool for systematically importing SNOMED-CT concepts as phrases, filtered by word count.

**Features:**
- Search across all SNOMED domains
- Filter synonyms by word count (1-10 words)
- Add concepts as active or inactive phrases
- Session persistence (resume imports across app restarts)
- Real-time statistics tracking
- Automatic pagination through large result sets

**Usage in Production:**

```csharp
// 1. Implement the required service interfaces
public class MySnowstormClient : ISnowstormClient { ... }
public class MyPhraseService : IPhraseService { ... }
public class MySnomedMapService : ISnomedMapService { ... }

// 2. Create the ViewModel
var viewModel = new SnomedWordCountImporterViewModel(
    snowstormClient,
    phraseService,
    snomedMapService,
    sessionFilePath: "path/to/session.json"  // Optional
);

// 3. Create and show the window
var window = new SnomedWordCountImporterWindow(viewModel)
{
    Owner = parentWindow  // Optional
};
window.ShowDialog();
```

## Dependencies

### Required NuGet Packages
- **CommunityToolkit.Mvvm** (8.2.2+) - For MVVM commands and helpers
- **Microsoft.Data.SqlClient** (5.2.0+) - For Azure SQL connectivity
- **System.Text.Json** (9.0.0+) - For JSON configuration

### Service Abstractions

The library defines service interfaces that must be implemented by the consuming application:

1. **ISnowstormClient** - SNOMED-CT terminology server integration
   - `BrowseBySemanticTagAsync()` - Fetch concepts with pagination

2. **IPhraseService** - Phrase management
   - `GetAllGlobalPhraseMetaAsync()` - List existing phrases
   - `UpsertPhraseAsync()` - Create/update phrases
   - `RefreshGlobalPhrasesAsync()` - Refresh phrase cache

3. **ISnomedMapService** - Phrase-to-SNOMED mapping
   - `CacheConceptAsync()` - Store SNOMED concepts locally
   - `MapPhraseAsync()` - Link phrases to SNOMED concepts

## Integration Examples

### Radium Application

```csharp
// In GlobalPhrasesSettingsTab.xaml.cs
private void OnOpenWordCountImporterClick(object sender, RoutedEventArgs e)
{
    var snowstormClient = app.Services.GetService<ISnowstormClient>();
    var phraseService = app.Services.GetService<IPhraseService>();
    var snomedMapService = app.Services.GetService<ISnomedMapService>();

    var vm = new SnomedWordCountImporterViewModel(
        snowstormClient,
        phraseService,
        snomedMapService
    );

    var window = new SnomedWordCountImporterWindow(vm)
    {
     Owner = Window.GetWindow(this)
    };

    window.ShowDialog();
}
```

## Session Persistence

The Word Count Importer automatically saves progress to disk:
- **Default location**: `%LocalAppData%\Wysg.Musm.SnomedTools\snomed_wordcount_session.json`
- **Custom location**: Pass `sessionFilePath` parameter to ViewModel constructor

Session includes:
- Current word count target
- Pagination state (`searchAfter` token)
- Queued candidates
- Statistics (Added, Ignored, Total)

## Project Structure

```
src/Wysg.Musm.SnomedTools/
戍式式 Abstractions/     # Service interfaces
弛   戍式式 ISnowstormClient.cs
弛   戍式式 IPhraseService.cs
弛   戌式式 ISnomedMapService.cs
戍式式 Services/   # Real service implementations
弛   戍式式 RealSnowstormClient.cs
弛   戍式式 RealPhraseService.cs
弛   戌式式 RealSnomedMapService.cs
戍式式 ViewModels/         # MVVM ViewModels
弛   戌式式 SnomedWordCountImporterViewModel.cs
戍式式 Views/        # WPF Windows
弛   戍式式 SnomedWordCountImporterWindow.xaml
弛   戌式式 SnomedWordCountImporterWindow.xaml.cs
戍式式 App.xaml            # Standalone app entry point
戍式式 App.xaml.cs
戍式式 TestWindow.xaml     # Standalone test window
戍式式 TestWindow.xaml.cs  # Mock/Real service switching & test UI
戍式式 SettingsWindow.xaml # Configuration UI
戍式式 SettingsWindow.xaml.cs
戍式式 SnomedToolsLocalSettings.cs  # Configuration persistence
戌式式 Wysg.Musm.SnomedTools.csproj
```

## Target Framework

- **.NET 9.0 (windows)**
- **C# 13.0**
- **WPF** enabled
- **OutputType**: WinExe (runnable application)

## Development Workflow

### Testing New Features
1. Make changes to ViewModels or Views
2. Run `Wysg.Musm.SnomedTools` standalone to test
3. Choose Mock Mode for quick UI iteration OR Real Mode for integration testing
4. Configure real services in Settings if needed
5. Iterate quickly without running full Radium app
6. Once stable, integrate with Radium via adapters

### Adding New Mock Data
Edit `TestWindow.xaml.cs` mock service implementations to add:
- More sample SNOMED concepts
- Custom test scenarios
- Edge cases for validation

### Configuration Storage
Settings are stored in JSON format at:
- **Location**: `%LocalAppData%\Wysg.Musm.SnomedTools\settings.json`
- **Format**: Plain JSON with connection strings and API endpoints
- **Security**: Supports Azure AD passwordless authentication for SQL

## License

See main repository LICENSE file.

## Contributing

This library is part of the Wysg.Musm medical software suite. Contributions should follow the main repository guidelines.
