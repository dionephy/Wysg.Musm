# Changelog

## Version 1.2.0 - API Integration for Proto Results

### New Features

#### 1. LLM API Integration
- **Complete API Client**: Implemented HTTP client for proofreading API
- **API Configuration**: Support for customizable API settings via `api_config.json`
- **Async Implementation**: Non-blocking API calls for smooth user experience
- **Request/Response Models**: Strongly-typed models for API communication
  - `ProofreadRequest`: Sends prompt and candidate_text
  - `ProofreadResponse`: Receives proofread_text, issues, and metadata
  - `ProofreadIssue`: Details about corrections and suggestions

#### 2. Get Proto Result Button
- **Functional Implementation**: Button now calls external API
- **Input Validation**: Ensures Input and Prompt fields are not empty
- **Status Feedback**: Real-time status updates during API calls
- **Error Handling**: Comprehensive error messages with troubleshooting tips
- **Button State Management**: Disables during API call to prevent duplicate requests
- **Issue Display**: Shows proofreading issues and suggestions in a dialog

#### 3. API Configuration System
- **Configuration File**: `api_config.json` for storing API settings
- **Default Values**: Works out-of-box with sensible defaults
- **Customizable Settings**:
  - `apiUrl`: API endpoint (default: http://192.168.111.79:8081)
  - `authToken`: Bearer token for authentication (default: local-dev-token)
  - `defaultPrompt`: Default prompt text (default: "Proofread")
- **Sample File**: Includes `api_config.json.sample` for easy setup

### API Integration Details

#### How It Works
1. User enters text in the "Input" field
2. User sets the prompt (e.g., "Proofread") in the Prompt field
3. Click "Get Proto Result"
4. Application sends HTTP POST to `/v1/evaluations`:
   ```json
   {
     "prompt": "Proofread",
     "candidate_text": "The launch were sucessful"
   }
   ```
5. API responds with proofread text and issues
6. Proto Output field is populated with the result

#### API Response Handling
- **Success**: Displays proofread text in Proto Output field
- **Issues Found**: Shows dialog with all corrections and suggestions
- **Status Display**: Shows model name, latency, and issue count
- **Failure Handling**: Clear error messages with troubleshooting steps

### Technical Implementation

#### New Files
1. **Services/ProofreadApiService.cs**
   - HTTP client wrapper for API calls
   - JSON serialization/deserialization
   - Authorization header management
   - Exception handling and retries

2. **Services/ApiConfiguration.cs**
   - Configuration loading and saving
   - Default value management
   - JSON persistence

3. **api_config.json.sample**
   - Sample configuration file
   - Documentation for settings

#### Updated Files
1. **MainWindow.xaml.cs**
   - Added API service initialization
   - Implemented async API call in button handler
   - Enhanced validation for API requirements
   - Added detailed error handling and user feedback

### API Example

**Request:**
```http
POST http://192.168.111.79:8081/v1/evaluations
Authorization: Bearer local-dev-token
Content-Type: application/json

{
  "prompt": "Proofread",
  "candidate_text": "The launch were sucessful"
}
```

**Response:**
```json
{
  "id": "0b68e2c1-6c08-414a-94f5-1375f122bbbf",
  "status": "completed",
  "proofread_text": "The launch was successful",
  "issues": [
    {
      "category": "auto-correction",
      "span_start": 0,
      "span_end": 25,
      "suggestion": "The launch was successful",
      "severity": "medium",
      "confidence": 0.75
    }
  ],
  "model_name": "nemotron-4-340b-instruct",
  "model_version": "2025.09.1",
  "latency_ms": 1200
}
```

### User Experience Improvements

#### Status Messages
- "Calling API..." - During API request
- "API Success! Model: {name}, Latency: {ms}ms" - On success
- "API returned status: {status}" - On non-completed status
- "Error: {message}" - On failure

#### Error Messages
Comprehensive error dialogs include:
- Error description
- Troubleshooting steps
- Configuration verification checklist
- Network connectivity tips

#### Validation
- Input field must not be empty
- Prompt field must not be empty
- Clear error messages guide the user

### Documentation Updates

#### Updated Files
1. **README.md**
   - Added API Integration section
   - Documented Get Proto Result workflow
   - Added API configuration guide
   - Included troubleshooting for API issues
   - Added API response model documentation

2. **QUICKSTART.md**
   - Added Get Proto Result instructions
   - Documented API configuration steps

3. **CHANGELOG.md** (This file)
   - Comprehensive documentation of API features

### Configuration Example

**api_config.json:**
```json
{
  "apiUrl": "http://192.168.111.79:8081",
  "authToken": "local-dev-token",
  "defaultPrompt": "Proofread"
}
```

### Dependencies
- **System.Net.Http**: For HTTP client functionality
- **System.Text.Json**: For JSON serialization (already in use)

### Breaking Changes
None - All changes are additions and do not affect existing functionality.

### Known Limitations
- API URL and token must be configured before first use
- No retry logic for failed API calls (single attempt)
- Network timeout is system default (100 seconds)
- No caching of API results

### Future Enhancements
- Retry logic with exponential backoff
- Configurable timeout values
- API response caching
- Multiple API provider support
- Batch processing of multiple inputs
- API health check on startup

---

## Version 1.1.0 - Dark Theme & Always on Top

### New Features

#### 1. Dark Theme
- **Complete UI Overhaul**: Application now features a modern dark theme
- **Color Palette**:
  - Background: `#1E1E1E` (Dark gray)
  - Surface: `#252526` (Controls background)
  - Text: `#CCCCCC` (Light gray)
  - Borders: `#3E3E42` (Subtle borders)
  - Accent: `#007ACC` (Blue)
  - Success: `#73C991` (Green)
  - Warning: `#CCA700` (Yellow)
  - Error: `#F48771` (Red)
- **Benefits**:
  - Reduced eye strain during extended use
  - Better visibility in low-light environments
  - Modern, professional appearance
  - Consistent with popular development tools

#### 2. Always on Top Feature
- **Location**: Checkbox in the status bar (top-right corner)
- **Functionality**: Keep the application window above all other windows
- **Use Cases**:
  - Reference other applications while entering data
  - Multi-tasking across multiple monitors
  - Keeping the window visible during workflow
- **Feedback**: Status bar message when toggled on/off

### UI Improvements

#### Button Styling
- Dark themed buttons with colored borders
- Hover effects: Background changes to `#3E3E42`
- Press effects: Background changes to accent color `#007ACC`
- Visual hierarchy maintained through border colors:
  - **Save**: Green border (`#73C991`)
  - **Clear**: Red border (`#F48771`)
  - **Get Proto Result**: Yellow border (`#CCA700`)

#### Text Box Styling
- Dark background (`#252526`)
- Light text (`#CCCCCC`)
- Subtle borders (`#3E3E42`)
- Light-colored caret for visibility

#### Status Bar
- Dark surface background (`#252526`)
- Light text for readability
- Error text in red (`#F48771`)
- Always on Top checkbox integrated seamlessly

### Documentation Updates

#### Updated Files
1. **README.md**
   - Added dark theme section
   - Documented Always on Top feature
   - Updated UI Improvements section
   - Added color scheme details

2. **UI_REFERENCE.md**
   - Complete dark theme color palette
   - Always on Top checkbox documentation
   - Updated visual states table
   - Added keyboard navigation for checkbox
   - Updated button states and interactions

3. **QUICKSTART.md**
   - Mentioned dark theme in launch section
   - Added Always on Top to key features
   - Updated button guide with visual cues
   - Added window controls section

4. **CHANGELOG.md** (New)
   - Comprehensive list of changes
   - Version history tracking

### Technical Changes

#### MainWindow.xaml
- Added comprehensive dark theme resource dictionary
- Defined color brushes for consistency
- Updated all control styles for dark theme
- Added custom button template with hover/press states
- Added Always on Top checkbox to status bar
- Reorganized status bar with Grid layout

#### MainWindow.xaml.cs
- Added `ChkAlwaysOnTop_Checked` event handler
- Added `ChkAlwaysOnTop_Unchecked` event handler
- Updated status text colors to match dark theme
- Maintained all existing functionality

### Compatibility
- **.NET Version**: 9.0 (unchanged)
- **C# Version**: 13.0 (unchanged)
- **Framework**: WPF (unchanged)
- **Breaking Changes**: None - all existing functionality preserved

### Future Enhancements
- Theme toggle (light/dark mode selection)
- Remember Always on Top preference
- Additional keyboard shortcuts
- Theme customization options

---

## Version 1.0.0 - Initial Release

### Features
- Data entry interface for LLM training data
- Prompt management
- JSON data persistence
- Input validation
- Record counting
- Clear functionality
