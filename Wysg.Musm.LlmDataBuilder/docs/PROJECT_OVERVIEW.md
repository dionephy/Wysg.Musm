# Wysg.Musm.LlmDataBuilder - Project Overview

## Project Information

**Name**: Wysg.Musm.LlmDataBuilder  
**Type**: WPF Desktop Application  
**Framework**: .NET 9.0  
**Language**: C# 13.0  
**Created**: 2025  
**Purpose**: Training data builder for Large Language Models

## Description

LLM Data Builder is a desktop utility application designed to streamline the creation and management of training datasets for Large Language Models. It provides a user-friendly interface for collecting structured input-output pairs along with metadata, making it easier for data scientists and ML engineers to build high-quality training datasets.

## Key Features

### Current Implementation (v1.0)

? **Dual-Panel Interface**
- Left panel: Data entry fields (input, output, proto output, prompt numbers)
- Right panel: Prompt template editor
- Clean, intuitive layout optimized for data entry workflows

? **JSON Data Storage**
- Structured JSON format with proper schema
- Append-only architecture preserves all historical data
- Pretty-printed JSON for readability
- Automatic file management

? **Prompt Management**
- Edit and save prompt templates
- Persistent across sessions
- Plain text format for easy external editing

? **Validation & Error Handling**
- Required field validation
- Format validation for numeric fields
- User-friendly error messages
- Comprehensive exception handling

? **User Experience**
- Status bar with operation feedback
- Record counter
- Confirmation dialogs
- Auto-clear on save
- Visual feedback for all actions

### Planned Features (Future Versions)

?? **LLM Server Integration**
- Connect to OpenAI API
- Azure OpenAI support
- Custom endpoint configuration
- Automatic proto output generation

?? **Advanced Data Management**
- Search and filter records
- Edit existing records
- Delete records
- Batch operations
- Export to multiple formats (CSV, JSONL)

?? **UI Enhancements**
- Dark mode
- Keyboard shortcuts
- Drag-and-drop import
- Split view for comparisons
- Syntax highlighting

## Technology Stack

### Core Technologies
- **.NET 9.0**: Latest .NET framework with performance improvements
- **WPF**: Windows Presentation Foundation for rich desktop UI
- **C# 13.0**: Latest language features
- **System.Text.Json**: High-performance JSON serialization

### NuGet Packages
- **Spectre.Console** (v0.52.0): Enhanced console output capabilities

### Development Tools
- Visual Studio 2022
- .NET SDK 9.0
- Git for version control

## Architecture

### Project Structure

```
Wysg.Musm.LlmDataBuilder/
戍式式 MainWindow.xaml           # UI layout and design
戍式式 MainWindow.xaml.cs        # Code-behind with business logic
戍式式 App.xaml                  # Application configuration
戍式式 App.xaml.cs              # Application startup and lifecycle
戍式式 docs/                     # Documentation
弛   戍式式 README.md            # Comprehensive documentation
弛   戍式式 QUICKSTART.md        # Quick start guide
弛   戍式式 DATA_SCHEMA.md       # JSON schema documentation
弛   戍式式 API_INTEGRATION.md   # Future LLM integration guide
弛   戌式式 PROJECT_OVERVIEW.md  # This file
戌式式 Wysg.Musm.LlmDataBuilder.csproj  # Project file
```

### Data Files (Generated at Runtime)

```
bin/Debug/net9.0-windows/
戍式式 data.json         # Training data records
戌式式 prompt.txt        # Prompt template
```

### Class Design

```csharp
// Main window class with UI logic
public partial class MainWindow : Window
{
    - Data management methods
    - File I/O operations
    - Validation logic
    - UI update methods
}

// Data model class
public class LlmDataRecord
{
    - Input: string
    - Output: string
    - ProtoOutput: string
    - AppliedPromptNumbers: List<int>
}
```

## Usage Workflow

### Basic Workflow

1. **Launch Application**
   - Application loads existing `prompt.txt`
   - Displays current record count from `data.json`

2. **Enter Data**
   - Fill in Input field (required)
   - Fill in Output field (required)
   - Optionally add Applied Prompt Numbers
   - Edit prompt if needed

3. **Save**
   - Click Save button
   - Data validated
   - Record appended to JSON
   - Prompt saved to file
   - Fields cleared automatically

4. **Repeat**
   - Continue adding records
   - Monitor progress via record count

### Advanced Workflow (Future)

1. **Generate Proto Output**
   - Enter input
   - Click "Get Proto Result"
   - LLM generates response
   - Proto output field populated automatically

2. **Compare and Adjust**
   - Review proto output
   - Modify output field as needed
   - Save with both outputs for comparison

## File Formats

### data.json Structure

```json
[
  {
    "input": "User question or prompt",
    "output": "Expected response",
    "protoOutput": "LLM-generated response (optional)",
    "appliedPromptNumbers": [1, 2, 3]
  }
]
```

### prompt.txt Structure

Plain text file containing the prompt template:

```
System: You are a helpful assistant...

Context: {context}
Question: {question}

Answer:
```

## Development Guidelines

### Code Style
- Follow C# naming conventions
- Use meaningful variable names
- Add XML documentation comments
- Keep methods focused and small
- Handle exceptions appropriately

### UI Design Principles
- Clean, uncluttered interface
- Consistent spacing and alignment
- Clear visual hierarchy
- Responsive to user actions
- Accessible error messages

### Testing Strategy
- Manual testing for UI workflows
- Validation testing for all input fields
- File I/O testing
- Error handling verification
- Cross-platform compatibility (Windows)

## Building and Running

### Prerequisites
- Windows 10 or later
- .NET 9.0 SDK installed
- Visual Studio 2022 (recommended) or VS Code

### Build Commands

```bash
# Restore packages
dotnet restore

# Build project
dotnet build

# Run application
dotnet run

# Publish for distribution
dotnet publish -c Release
```

### Output

Debug build: `bin\Debug\net9.0-windows\`  
Release build: `bin\Release\net9.0-windows\`

## Integration Points

### Current
- Local file system (JSON and TXT files)
- Windows clipboard (copy/paste)

### Planned
- OpenAI API
- Azure OpenAI Service
- Custom LLM endpoints
- Database backends (optional)
- Cloud storage (optional)

## Performance Considerations

### Current Implementation
- Synchronous file I/O (acceptable for small datasets)
- In-memory JSON deserialization
- No caching mechanism
- Single-threaded UI

### Future Optimizations
- Async file operations for large datasets
- Lazy loading for record management
- Caching for LLM responses
- Background threads for API calls
- Pagination for large datasets

## Security Considerations

### Current
- Files stored locally in application directory
- No authentication or encryption
- Plain text storage

### Future
- Secure API key storage (Windows Credential Manager)
- Optional data encryption
- Environment variable support
- Configuration file security

## Deployment

### Standalone Distribution
- Single executable with dependencies
- Framework-dependent or self-contained
- Windows-only currently

### Installation
- Copy to any directory
- No installation required
- Portable application

## Version History

### Version 1.0 (Current)
- Initial release
- Basic data entry and storage
- JSON and TXT file management
- Validation and error handling
- Complete documentation

### Planned Version 1.1
- LLM server integration
- Get Proto Result implementation
- API configuration UI
- Async operations

### Planned Version 2.0
- Advanced data management
- Search and filter
- Edit/delete records
- Multiple export formats
- UI enhancements

## Contributing

This project is part of the Wysg.Musm solution suite. Contributions should:
- Follow existing code style
- Include appropriate documentation
- Add tests where applicable
- Update relevant documentation

## Support and Resources

### Documentation
- [README.md](README.md) - Complete user guide
- [QUICKSTART.md](QUICKSTART.md) - Quick start guide
- [DATA_SCHEMA.md](DATA_SCHEMA.md) - JSON schema reference
- [API_INTEGRATION.md](API_INTEGRATION.md) - LLM integration guide

### Related Projects
- Wysg.Musm.Radium - Main application
- Other projects in Wysg.Musm solution

## License

Part of the Wysg.Musm project suite.

## Contact

For questions or support, refer to the main Wysg.Musm repository.

---

**Last Updated**: January 2025  
**Version**: 1.0  
**Status**: Production Ready
