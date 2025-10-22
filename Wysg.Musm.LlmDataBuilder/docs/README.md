# LLM Data Builder

## Overview

The LLM Data Builder is a desktop application designed to help create and manage training data for Large Language Models (LLMs). It provides a simple interface for collecting input-output pairs along with metadata, storing them in a structured JSON format.

## Features

### Current Features

- **Data Entry Interface**: Four dedicated fields for capturing training data:
  - **Input**: The prompt or question for the LLM
  - **Output**: The expected response or answer
  - **Proto Output**: Placeholder for LLM-generated responses (read-only)
  - **Applied Prompt Numbers**: Comma-separated list of prompt template IDs used

- **Prompt Management**: 
  - View and edit the master prompt template
  - Automatically saved to `prompt.txt` on each save operation
  - Persistent across application sessions

- **Data Persistence**:
  - Records saved to `data.json` in the application directory
  - JSON formatted with proper indentation for readability
  - Append-only model - new records are added without overwriting existing data

- **User Experience**:
  - Real-time record count display
  - Status messages for all operations
  - Input validation before saving
  - Clear functionality for data fields (prompt is preserved)
  - Confirmation dialogs for important actions

### Planned Features

- **Get Proto Result**: Integration with LLM server to generate prototype outputs
  - Currently shows placeholder message
  - Will be implemented in future versions

## File Structure

The application works with two main files in its working directory:

### data.json
```json
[
  {
    "input": "Example question or prompt",
    "output": "Expected answer or response",
    "protoOutput": "LLM-generated response (if available)",
    "appliedPromptNumbers": [1, 2, 3]
  }
]
```

### prompt.txt
Contains the master prompt template used for LLM interactions. This is a plain text file that can be edited directly in the application.

## Usage Guide

### Starting the Application

1. Launch the LLM Data Builder application
2. The application will automatically:
   - Create or load `prompt.txt` if it exists
   - Display the current record count from `data.json`

### Creating a New Record

1. **Enter Input**: Type the question, prompt, or input text in the "Input" field
2. **Enter Output**: Type the expected response or answer in the "Output" field
3. **Optional - Proto Output**: This field is read-only and will be populated when the "Get Proto Result" feature is implemented
4. **Optional - Applied Prompt Numbers**: Enter comma-separated numbers (e.g., `1,2,3`) to track which prompt templates were used
5. **Update Prompt** (if needed): Modify the prompt text in the right panel
6. **Click Save**: The data will be appended to `data.json` and `prompt.txt` will be updated

### Managing Data

- **Clear Data Fields**: Click the "Clear Data Fields" button to reset all data entry fields (excluding the prompt)
- **View Records**: The status bar shows the total number of records in `data.json`
- **Edit Prompt**: The prompt text box always shows the current content of `prompt.txt`

### Validation Rules

The application enforces the following validation rules:

1. **Input cannot be empty** - You must provide an input value
2. **Output cannot be empty** - You must provide an output value
3. **Applied Prompt Numbers must be valid integers** - If provided, they must be comma-separated numbers

## Technical Details

### Technology Stack

- **Framework**: .NET 9.0
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Language**: C# 13.0
- **JSON Serialization**: System.Text.Json

### Data Model

```csharp
public class LlmDataRecord
{
    public string Input { get; set; }
    public string Output { get; set; }
    public string ProtoOutput { get; set; }
    public List<int> AppliedPromptNumbers { get; set; }
}
```

### File Locations

By default, all files are stored in the application's base directory:
- `AppDomain.CurrentDomain.BaseDirectory`

This typically resolves to:
- **Debug**: `bin\Debug\net9.0-windows\`
- **Release**: `bin\Release\net9.0-windows\`

## Error Handling

The application includes comprehensive error handling:

- **File I/O errors**: Displayed in the status bar with error styling
- **Validation errors**: Shown via message boxes with clear instructions
- **JSON parsing errors**: Gracefully handled with fallback to empty dataset
- **User feedback**: All operations provide status updates

## Future Enhancements

### Planned for Next Version

1. **LLM Server Integration**
   - Implement the "Get Proto Result" functionality
   - Connect to OpenAI, Azure OpenAI, or custom LLM endpoints
   - Automatically populate `protoOutput` field

2. **Advanced Features**
   - Export data to different formats (CSV, JSONL)
   - Search and filter existing records
   - Edit/delete individual records
   - Batch operations
   - Template management for prompts
   - Configuration for custom LLM endpoints

3. **UI Improvements**
   - Dark mode support
   - Keyboard shortcuts
   - Drag-and-drop file import
   - Preview pane for JSON data

## Troubleshooting

### Problem: Files not saving

**Solution**: Check that the application has write permissions to its directory. Try running as administrator or change the working directory.

### Problem: JSON parsing errors

**Solution**: Ensure `data.json` is valid JSON. If corrupted, back it up and delete it to start fresh.

### Problem: Prompt not loading

**Solution**: Verify `prompt.txt` exists and is readable. The application will create a new empty file if it doesn't exist.

## Support

For issues, feature requests, or contributions, please refer to the main project repository.

## License

This application is part of the Wysg.Musm project suite.
