# LLM Data Builder

## Overview

The LLM Data Builder is a desktop application designed to help create and manage training data for Large Language Models (LLMs). It provides a simple interface for collecting input-output pairs along with metadata, storing them in a structured JSON format.

## Features

### Current Features

- **Dark Theme UI**: Modern dark theme for comfortable extended use

- **Always on Top**: Optional checkbox to keep the window above other applications

- **Data Browser**: Dedicated window for viewing, managing, and exporting saved records
  - View all records in a sortable data grid
  - Detailed view panel for full record content
  - Export individual records to separate JSON files
  - Delete unwanted or incorrect records
  - Real-time record count and status updates
  - See [Data Browser Documentation](DATA_BROWSER.md) for details

- **LLM API Integration**: Connect to proofreading API to generate prototype outputs
  - Calls external API with your input text and prompt
  - Automatically populates Proto Output field
  - Shows issues and suggestions found during proofreading
  - Configurable API endpoint and authentication

- **Data Entry Interface**: Four dedicated fields for capturing training data:
  - **Input**: The prompt or question for the LLM (used as candidate_text in API)
  - **Output**: The expected response or answer
  - **Proto Output**: LLM-generated responses from API (proofread_text)
  - **Applied Prompt Numbers**: Comma-separated list of prompt template IDs used

- **Prompt Management**: 
  - View and edit the master prompt template (used as prompt in API)
  - Automatically saved to `prompt.txt` on each save operation
  - Persistent across application sessions
  - Used as the "prompt" parameter when calling the API

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
  - Detailed API response information

## File Structure

The application works with three main files in its working directory:

### data.json
```json
[
  {
    "input": "The launch were sucessful",
    "output": "The launch was successful",
    "protoOutput": "The launch was successful",
    "appliedPromptNumbers": [1, 2]
  }
]
```

### prompt.txt
Contains the master prompt template used for API calls. This is sent as the "prompt" parameter to the proofreading API. Default value is "Proofread".

### api_config.json
Optional configuration file for API settings:
```json
{
  "apiUrl": "http://192.168.111.79:8081",
  "authToken": "change-me",
  "defaultPrompt": "Proofread",
  "language": "en",
  "strictness": 4
}
```

**Configuration Options:**
- `apiUrl`: Your API endpoint URL (default: http://192.168.111.79:8081)
- `authToken`: Bearer token for authentication (default: "change-me")
- `defaultPrompt`: Default prompt text (default: "Proofread")
- `language`: Language code for proofreading (default: "en")
- `strictness`: Strictness level 1-5 (default: 4, higher = more strict)

If this file doesn't exist, the application will use default values. Copy `api_config.json.sample` to `api_config.json` and customize as needed.

## Usage Guide

### Starting the Application

1. Launch the LLM Data Builder application
2. The application will automatically:
   - Create or load `prompt.txt` if it exists (default: "Proofread")
   - Load API configuration from `api_config.json` or use defaults
   - Display the current record count from `data.json`
   - Load with dark theme enabled by default

### Configuring the API

1. **Copy sample config**: Copy `api_config.json.sample` to `api_config.json`
2. **Edit settings**:
   - `apiUrl`: Your API endpoint (default: http://192.168.111.79:8081)
   - `authToken`: Your authentication token (default: "change-me")
   - `defaultPrompt`: Default prompt text (default: "Proofread")
   - `language`: Language code (default: "en")
   - `strictness`: Strictness level 1-5 (default: 4)
3. **Restart application** to apply changes

**Strictness Levels:**
- 1: Very lenient (minimal corrections)
- 2: Lenient (basic corrections)
- 3: Moderate (standard corrections)
- 4: Strict (thorough corrections) **ก็ Default**
- 5: Very strict (maximum corrections)

### Window Settings

- **Always on Top**: Check the "Always on Top" checkbox in the status bar to keep the window above all other windows. This is useful when referencing other applications while entering data.

### Using Get Proto Result

The "Get Proto Result" button calls an external API to proofread your input text:

1. **Enter Input**: Type your text in the "Input" field (this becomes `candidate_text` in the API)
2. **Set Prompt**: Ensure your prompt is set (e.g., "Proofread") - this becomes the `prompt` parameter
3. **Click "Get Proto Result"**: The button will:
   - Validate that Input and Prompt are not empty
   - Call the API at the configured endpoint
   - Display the proofread text in the "Proto Output" field
   - Show any issues or suggestions found
   - Display API response details in the status bar

**API Call Example:**
```json
POST http://192.168.111.79:8081/v1/evaluations
Headers: Authorization: Bearer change-me
        Content-Type: application/json
Body: {
  "prompt": "Proofread",
  "candidate_text": "The launch were sucessful",
  "language": "en",
  "strictness": 4
}

Response: {
  "proofread_text": "The launch was successful",
  "status": "completed",
  "issues": [...],
  "model_name": "nemotron-4-340b-instruct",
  "latency_ms": 1200
}
```

**Request Parameters:**
- `prompt`: The instruction for the model (e.g., "Proofread")
- `candidate_text`: The text to be evaluated/proofread
- `language`: Language code ("en" for English, "ko" for Korean, etc.)
- `strictness`: Evaluation strictness level (1-5)

### Creating a New Record

1. **Enter Input**: Type the question, prompt, or input text in the "Input" field
2. **(Optional) Get Proto Result**: Click "Get Proto Result" to auto-populate Proto Output
3. **Enter Output**: Type the expected response or answer in the "Output" field
4. **Review Proto Output**: Check the API-generated result (if you used Get Proto Result)
5. **Optional - Applied Prompt Numbers**: Enter comma-separated numbers (e.g., `1,2,3`) to track which prompt templates were used
6. **Update Prompt** (if needed): Modify the prompt text in the right panel
7. **Click Save**: The data will be appended to `data.json` and `prompt.txt` will be updated

### Managing Data

- **Browse Data**: Click the "Browse Data" button to open the Data Browser window
  - View all records in a sortable grid
  - View full details of any record
  - Export individual records
  - Delete unwanted records
  - See [Data Browser Documentation](DATA_BROWSER.md) for complete guide
- **Cleanup Blank Records**: Click the "Cleanup Blank Records" button to remove invalid records
  - Automatically removes records with empty Input or Output
  - Creates a timestamped backup before making changes
  - Shows summary of removed records
  - Safe operation with automatic backup
- **Clear Data Fields**: Click the "Clear Data Fields" button to reset all data entry fields (excluding the prompt)
- **View Records**: The status bar shows the total number of records in `data.json`
- **Edit Prompt**: The prompt text box always shows the current content of `prompt.txt`

### Validation Rules

The application enforces the following validation rules:

1. **Input cannot be empty or whitespace** - You must provide an input value with actual content (leading/trailing spaces are automatically trimmed)
2. **Output cannot be empty or whitespace** - You must provide an output value with actual content (leading/trailing spaces are automatically trimmed)
3. **Prompt cannot be empty** - Required when calling the API
4. **Applied Prompt Numbers must be valid integers** - If provided, they must be comma-separated numbers
5. **Automatic trimming** - All text inputs are automatically trimmed of leading/trailing whitespace before saving

## Technical Details

### Technology Stack

- **Framework**: .NET 9.0
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Language**: C# 13.0
- **JSON Serialization**: System.Text.Json
- **HTTP Client**: System.Net.Http (for API calls)

### API Integration

The application includes a complete API integration system:

- **ProofreadApiService**: Handles HTTP communication with the proofreading API
- **ApiConfiguration**: Manages API settings (URL, auth token, default prompt)
- **Request/Response Models**: Strongly-typed models for API communication
- **Async/Await**: Non-blocking API calls for smooth UI experience
- **Error Handling**: Comprehensive error messages for API failures

### API Response Model

```csharp
public class ProofreadResponse
{
    public string Id { get; set; }
    public string Status { get; set; }
    public string ProofreadText { get; set; }
    public List<ProofreadIssue> Issues { get; set; }
    public string ModelName { get; set; }
    public int LatencyMs { get; set; }
    // ... other properties
}
```

### Dark Theme

The application features a dark theme with the following color scheme:
- **Background**: `#1E1E1E` (Dark gray)
- **Surface**: `#252526` (Slightly lighter gray for controls)
- **Text**: `#CCCCCC` (Light gray)
- **Accent**: `#007ACC` (Blue)
- **Success**: `#73C991` (Green)
- **Warning**: `#CCA700` (Yellow)
- **Error**: `#F48771` (Red)

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
- **API errors**: Detailed error messages with troubleshooting tips
- **Network errors**: Clear indication of connection issues
- **User feedback**: All operations provide status updates

### Common API Issues

| Issue | Possible Cause | Solution |
|-------|---------------|----------|
| Connection refused | API server not running | Start the API server |
| 401 Unauthorized | Invalid auth token | Check `authToken` in `api_config.json` (should be "change-me" by default) |
| Timeout | Network issues | Check network connectivity and API URL |
| Invalid response | API version mismatch | Verify API endpoint and version |
| Invalid strictness | Value out of range | Ensure strictness is between 1 and 5 |

## Troubleshooting

### Problem: Files not saving

**Solution**: Check that the application has write permissions to its directory. Try running as administrator or change the working directory.

### Problem: JSON parsing errors

**Solution**: Ensure `data.json` is valid JSON. If corrupted, back it up and delete it to start fresh.

### Problem: Prompt not loading

**Solution**: Verify `prompt.txt` exists and is readable. The application will create a new file with default value if it doesn't exist.

### Problem: API not responding

**Solution**: 
1. Verify the API server is running at the configured URL
2. Check `api_config.json` for correct settings
3. Test the API with curl or PowerShell
4. Check firewall and network settings
5. Verify the auth token is correct

### Problem: Get Proto Result button disabled

**Solution**: The button is disabled during API calls. If it stays disabled, restart the application.

### Problem: Data file contains blank records

**Solution**: 
1. Click the "Cleanup Blank Records" button
2. Confirm the operation when prompted
3. A backup will be automatically created before cleanup
4. All records with empty Input or Output will be removed
5. Check the status bar for the cleanup summary

**Manual cleanup**:
1. Locate `data.json` in the application directory
2. Create a backup: `data.backup.json`
3. Edit the file and remove objects with empty `"input"` or `"output"` fields
4. Save and restart the application

## Support

For issues, feature requests, or contributions, please refer to the main project repository.

## License

This application is part of the Wysg.Musm project suite.
