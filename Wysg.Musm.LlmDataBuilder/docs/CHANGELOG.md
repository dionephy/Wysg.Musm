# Changelog

## Version 1.3.3 - Data Browser Display Fix (2025-01-24)

### Bug Fix

#### Data Browser Not Showing Text Content

**Problem**: The Data Browser window displayed record numbers but all text fields (Input, Output, Proto Output) appeared blank, even though the data.json file contained valid data.

**Root Cause**: JSON property name mismatch between save and load operations:
- Save operation used `JsonNamingPolicy.CamelCase` (properties: `input`, `output`, `protoOutput`)
- Load operation used default deserialization (expected: `Input`, `Output`, `ProtoOutput`)
- Result: Deserialization succeeded but created objects with default empty strings

**Solution**: Updated `DataBrowserWindow.LoadData()` to use the same `JsonNamingPolicy.CamelCase` and `PropertyNameCaseInsensitive = true` options as the save operation.

**Impact**: Critical - Data Browser is now fully functional and displays all record content correctly.

### Technical Details

#### File Modified
- `DataBrowserWindow.xaml.cs` - Updated `LoadData()` method

#### Code Change
```csharp
// Added JsonSerializerOptions to match save format
var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true
};
var records = JsonSerializer.Deserialize<List<LlmDataRecord>>(json, options);
```

### User Impact

**Before Fix**:
- Data Browser showed record count
- DataGrid displayed only index numbers (#)
- Input/Output/Proto Output columns appeared blank
- Detail panel showed no content
- Users thought data wasn't being saved

**After Fix**:
- ? All record content displays correctly in DataGrid
- ? Detail panel shows full text
- ? Export functionality works as expected
- ? Delete functionality works as expected

### Testing Checklist

- [x] Open Data Browser ¡æ Shows record count
- [x] View DataGrid ¡æ Displays all text content
- [x] Select record ¡æ Detail panel populates
- [x] Export record ¡æ Creates valid JSON file
- [x] Delete record ¡æ Removes correctly
- [x] Refresh ¡æ Reloads data successfully

### Migration Notes

**No action required for existing users**:
- Data files remain unchanged (already in correct format)
- Just restart application to get the fix
- All existing records will immediately display correctly

---

## Version 1.3.2 - API Parameter Updates (2025-01-24)

### API Integration Updates

#### 1. Updated API Request Parameters
- **Added**: `language` parameter (default: "en")
- **Added**: `strictness` parameter (default: 4, range: 1-5)
- **Updated**: Default auth token changed from "local-dev-token" to "change-me"
- **Aligned**: Request format now matches actual API specification

#### 2. Enhanced API Configuration
- **New Settings in api_config.json**:
  - `language`: Language code for proofreading (e.g., "en", "ko")
  - `strictness`: Evaluation strictness level (1-5)
- **Updated Sample**: `api_config.json.sample` includes all new parameters
- **Backward Compatible**: Existing configs continue to work with defaults

### Technical Implementation

#### Updated Files
1. **Services/ProofreadApiService.cs**
   - Added `language` and `strictness` parameters to `GetProofreadResultAsync`
   - Updated `ProofreadRequest` model with new fields
   - Default auth token changed to "change-me"

2. **Services/ApiConfiguration.cs**
   - Added `Language` property (default: "en")
   - Added `Strictness` property (default: 4)
   - Updated `AuthToken` default to "change-me"

3. **MainWindow.xaml.cs**
   - Updated API call to pass `language` and `strictness` from config

4. **api_config.json.sample**
   - Added example values for new parameters
   - Updated auth token to "change-me"

### API Request Format

**Before:**
```json
{
  "prompt": "Proofread",
  "candidate_text": "The launch were sucessful"
}
```

**After:**
```json
{
  "prompt": "Proofread",
  "candidate_text": "The launch were sucessful",
  "language": "en",
  "strictness": 4
}
```

### Strictness Levels

The `strictness` parameter controls how thoroughly the API evaluates the text:

| Level | Description | Use Case |
|-------|-------------|----------|
| 1 | Very lenient | Quick checks, minimal corrections |
| 2 | Lenient | Basic grammar and spelling |
| 3 | Moderate | Standard proofreading |
| 4 | Strict | Thorough corrections (default) |
| 5 | Very strict | Maximum scrutiny, all issues |

### Configuration Example

**api_config.json:**
```json
{
  "apiUrl": "http://192.168.111.79:8081",
  "authToken": "change-me",
  "defaultPrompt": "Proofread",
  "language": "en",
  "strictness": 4
}
```

### curl Command Match

The API call now matches the actual curl command format:
```bash
curl -X POST http://192.168.111.79:8081/v1/evaluations \
    -H 'Authorization: Bearer change-me' \
    -H 'Content-Type: application/json' \
    -d '{
          "prompt": "{prompt text}",
          "candidate_text": "{input}",
          "language": "en",
          "strictness": 4
        }'
```

### Documentation Updates

#### Updated Files
1. **README.md**
   - Updated api_config.json section
   - Updated Configuring the API section with strictness levels
   - Updated API Call Example with new parameters
   - Updated Common API Issues table

2. **api_config.json.sample**
   - Added language and strictness examples

### Breaking Changes
None - All changes are backward compatible. If `language` or `strictness` are not in your config, the application will use sensible defaults.

### Migration Guide

**Existing Users:**
1. No action required - defaults will be used
2. Optional: Add `language` and `strictness` to your `api_config.json` for explicit control

**New Configuration Format:**
```json
{
  "apiUrl": "http://192.168.111.79:8081",
  "authToken": "change-me",
  "defaultPrompt": "Proofread",
  "language": "en",
  "strictness": 4
}
```

### Use Cases

#### Multi-Language Support
```json
{
  "language": "ko",  // Korean
  "strictness": 4
}
```

#### Lenient Proofreading
```json
{
  "language": "en",
  "strictness": 2  // Basic corrections only
}
```

#### Maximum Thoroughness
```json
{
  "language": "en",
  "strictness": 5  // Catch everything
}
```

---

## Version 1.3.1 - Blank Record Prevention and Cleanup (2025-01-24)

### Bug Fixes

#### 1. Enhanced Input Validation
- **Fixed**: Records with whitespace-only Input or Output could be saved
- **Solution**: Added automatic trimming of all text inputs before validation
- **Impact**: Prevents saving meaningless blank records
- **Validation now**:
  - Trims leading/trailing whitespace from all inputs
  - Validates that Input has actual content (not just spaces)
  - Validates that Output has actual content (not just spaces)
  - Focuses the problem field for user convenience

#### 2. Cleanup Blank Records Feature
- **New button**: "Cleanup Blank Records" (yellow border)
- **Functionality**: Removes all records with empty Input or Output
- **Safety**: Creates timestamped backup before making changes
- **Location**: Between "Browse Data" and "Get Proto Result" buttons
- **Confirmation**: Asks for user confirmation before cleanup
- **Summary**: Shows count of removed records and remaining records

### Technical Implementation

#### Updated Files
1. **MainWindow.xaml.cs**
   - Enhanced `BtnSave_Click`: Added trimming and improved validation
   - Added `BtnCleanup_Click`: Event handler for cleanup button
   - Added `CleanupBlankRecords()`: Core cleanup logic with backup

2. **MainWindow.xaml**
   - Added "Cleanup Blank Records" button to action button group
   - Added tooltip explaining the feature

### Data Safety

#### Automatic Backup
- Format: `data.backup.YYYYMMDDHHMMSS.json`
- Location: Same directory as `data.json`
- Created: Before any cleanup operation
- Contents: Complete copy of original data.json

#### Cleanup Criteria
Records are removed if:
- Input is null, empty, or contains only whitespace
- OR Output is null, empty, or contains only whitespace

Records are kept if:
- Both Input and Output contain actual text content
- Even if ProtoOutput is empty (allowed)

### User Experience

#### Validation Messages
**Before (weak validation)**:
- "Please enter an input value."

**After (enhanced validation)**:
- "Please enter an input value with actual content."
- Focuses the Input field automatically
- Prevents tab/enter from bypassing validation

#### Cleanup Flow
1. User clicks "Cleanup Blank Records"
2. Confirmation dialog explains the operation
3. User confirms with Yes/No
4. Backup is created automatically
5. Blank records are removed
6. Summary dialog shows results
7. Record count updates automatically

### Documentation Updates

#### Updated Files
1. **README.md**
   - Added Cleanup feature to Managing Data section
   - Updated Validation Rules section
   - Added troubleshooting for blank records

2. **UI_REFERENCE.md**
   - Added Cleanup button to layout diagram
   - Updated Buttons section with tooltip
   - Updated button count and positions

3. **FIX_2025-01-24_BlankRecords.md** (New)
   - Detailed diagnostic guide
   - Root cause analysis
   - Implementation steps
   - Testing checklist

### Use Cases

#### Preventing Future Blank Records
- User enters only spaces in Input ¡æ Rejected
- User enters only spaces in Output ¡æ Rejected
- User enters valid text with spaces ¡æ Trimmed automatically
- User copies text with extra whitespace ¡æ Trimmed automatically

#### Cleaning Up Existing Data
- User has 100 records, 50 are blank
- User clicks "Cleanup Blank Records"
- Backup created: `data.backup.20250124120000.json`
- 50 blank records removed
- 50 valid records remain
- User can restore from backup if needed

### Breaking Changes
None - All changes are enhancements and do not affect existing functionality.

### Known Limitations
- Cleanup removes entire record if Input OR Output is blank
- ProtoOutput can be blank (valid use case when not using API)
- Applied Prompt Numbers can be empty (optional field)

### Future Enhancements
- Batch edit functionality in Data Browser
- Import/merge from external files with validation
- Duplicate detection based on Input similarity
- Undo for cleanup operation (in addition to backup)

---

## Version 1.3.0 - Data Browser

### New Features

#### 1. Data Browser Window
- **Dedicated browsing interface**: New window for viewing and managing all saved records
- **DataGrid view**: Sortable, resizable columns with alternating row colors
- **Key columns**:
  - Index (#): Sequential record number
  - Input: Training input text
  - Output: Expected output text
  - Proto Output: LLM-generated output
  - Prompts: Applied prompt template numbers
- **Record details panel**: Expandable panel showing full content of selected record
- **Navigation**: Select records by clicking rows in the grid

#### 2. Data Management Features
- **Browse Data button**: Opens Data Browser from main window (blue border)
- **Export selected record**: Save individual records to separate JSON files
- **Delete selected record**: Permanently remove unwanted/incorrect records
- **Refresh**: Reload data from file
- **Record selection**: Single-selection mode with full-row highlighting
- **Status updates**: Real-time feedback for all operations

#### 3. UI Enhancements
- **Modal dialog**: Blocks main window while open
- **Always on Top**: Checkbox to keep browser window above other applications
- **Dark theme**: Consistent with main application
- **Record count**: Updates automatically in both windows
- **Window size**: 1400x800 pixels for better data viewing

### Technical Implementation

#### New Files
1. **DataBrowserWindow.xaml**
   - DataGrid with custom columns
   - Details panel with expandable section
   - Action buttons (Refresh, Export, Delete, Close)
   - Status bar with Always on Top checkbox

2. **DataBrowserWindow.xaml.cs**
   - ObservableCollection for reactive updates
   - LlmDataRecordViewModel for display logic
   - Export to JSON with SaveFileDialog
   - Delete with confirmation dialog
   - Auto-reindexing after deletion

#### Updated Files
1. **MainWindow.xaml**
   - Added "Browse Data" button to action button group
   - Positioned as leftmost button with blue border

2. **MainWindow.xaml.cs**
   - Added BtnBrowseData_Click event handler
   - Opens DataBrowserWindow as modal dialog
   - Refreshes record count after browser closes

### Data Browser Features

#### Viewing Records
- **Grid display**: All records shown in sortable table
- **Column headers**: Click to sort ascending/descending
- **Alternating colors**: Easier visual scanning
- **Full-row selection**: Click anywhere to select
- **Detail panel**: Expand to see full text of all fields

#### Exporting Records
- **Individual export**: Save selected record to separate file
- **File format**: JSON with proper formatting
- **Save dialog**: Choose location and filename
- **Default name**: `record_[index].json`
- **Supported formats**: .json (recommended), .txt, or custom extension

#### Deleting Records
- **Confirmation dialog**: Prevents accidental deletion
- **Immediate save**: File updated after deletion
- **Auto-reindex**: Remaining records renumbered sequentially
- **Cannot undo**: Permanent deletion with clear warning

#### File Operations
- **Auto-load**: Loads data.json on window open
- **Auto-save**: Saves after deletion
- **Proper formatting**: Maintains JSON indentation
- **camelCase**: Consistent property naming

### Documentation Updates

#### New Files
1. **DATA_BROWSER.md**
   - Complete user guide for Data Browser
   - Feature descriptions with examples
   - Use case scenarios
   - Troubleshooting guide
   - Visual design specifications
   - Future enhancement plans

#### Updated Files
1. **README.md**
   - Added Data Browser to Features section
   - Updated Managing Data section
   - Link to detailed documentation

2. **UI_REFERENCE.md**
   - Added Browse Data button to layout diagram
   - Updated button colors section
   - Added to tab order
   - Added Ctrl+B keyboard shortcut (future)

3. **CHANGELOG.md** (This file)
   - Comprehensive documentation of new feature

### User Experience

#### Workflow Integration
1. Create records in main window
2. Click "Browse Data" to review all records
3. Select record to view full details
4. Export specific records for sharing
5. Delete errors or duplicates
6. Close browser and continue data entry

#### Visual Feedback
- **Status messages**: All operations show status updates
- **Record count**: Visible in both windows
- **Selection highlighting**: Blue background for selected row
- **Error messages**: Clear descriptions with troubleshooting tips

### Data Model

#### LlmDataRecordViewModel
```csharp
public class LlmDataRecordViewModel
{
    public int Index { get; set; }
    public string Input { get; set; }
    public string Output { get; set; }
    public string ProtoOutput { get; set; }
    public List<int> AppliedPromptNumbers { get; set; }
    public string AppliedPromptNumbersDisplay { get; }
}
```

### Error Handling
- **Load errors**: Graceful handling of corrupted JSON
- **Export errors**: Clear messages about file permissions
- **Delete errors**: Prevents deletion if file is locked
- **Validation**: Ensures selection before export/delete

### Future Enhancements
- **Search/filter**: Find records by content
- **Bulk operations**: Select multiple records
- **Inline editing**: Edit records directly in grid
- **Column customization**: Show/hide, reorder columns
- **Export all**: Export entire dataset or filtered subset
- **Import**: Merge records from external files
- **Statistics**: Data quality metrics
- **Duplicate detection**: Identify similar records

### Breaking Changes
None - All changes are additions and do not affect existing functionality.

### Known Limitations
- Single selection mode (no multi-select)
- No inline editing (use main window to create new records)
- Delete is permanent (no undo)
- Export one record at a time

---

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
