# API Integration Guide

## Overview

The LLM Data Builder now includes **complete API integration** for automatically generating proto results from a proofreading service. This guide covers the implementation, configuration, and usage of the API features.

## ? Implementation Status

**Status**: FULLY IMPLEMENTED (v1.2.0)

The "Get Proto Result" feature is now functional and calls an external proofreading API to generate results automatically.

## API Endpoint

### Default Configuration
- **URL**: `http://192.168.111.79:8081`
- **Endpoint**: `/v1/evaluations`
- **Method**: POST
- **Authentication**: Bearer token
- **Default Token**: `local-dev-token`

### Request Format
```json
POST /v1/evaluations
Authorization: Bearer local-dev-token
Content-Type: application/json

{
  "prompt": "Proofread",
  "candidate_text": "The launch were sucessful"
}
```

### Response Format
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
  "latency_ms": 1200,
  "summary_metrics": {
    "tokens": 4,
    "timestamp": "2025-10-23T03:09:55.599824+00:00"
  },
  "failure_reason": null,
  "submitted_at": "2025-10-23T03:09:55.589433Z",
  "completed_at": "2025-10-23T03:09:55.600751Z"
}
```

## Configuration

### Configuration File: api_config.json

Create `api_config.json` in the application directory:

```json
{
  "apiUrl": "http://192.168.111.79:8081",
  "authToken": "local-dev-token",
  "defaultPrompt": "Proofread"
}
```

### Configuration Properties

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `apiUrl` | string | Base URL of the API | `http://192.168.111.79:8081` |
| `authToken` | string | Bearer token for authentication | `local-dev-token` |
| `defaultPrompt` | string | Default prompt text | `Proofread` |

### Setup Steps

1. **Copy sample file**:
   ```
   Copy api_config.json.sample to api_config.json
   ```

2. **Edit configuration**:
   - Update `apiUrl` if your API is at a different address
   - Update `authToken` with your authentication token
   - Optionally change `defaultPrompt`

3. **Restart application** to load the new configuration

### Configuration Without File

If `api_config.json` doesn't exist, the application uses default values:
- API URL: `http://192.168.111.79:8081`
- Auth Token: `local-dev-token`
- Default Prompt: `Proofread`

## Usage

### Basic Workflow

1. **Enter Input Text**
   - Type or paste text in the "Input" field
   - Example: "The launch were sucessful"

2. **Set Prompt**
   - Ensure the "Prompt" field contains your instruction
   - Default: "Proofread"
   - Can be changed to other prompts like "Fix grammar", "Improve clarity", etc.

3. **Click "Get Proto Result"**
   - Button validates input and prompt are not empty
   - Makes async API call to the configured endpoint
   - Shows "Calling API..." in status bar

4. **Review Results**
   - Proto Output field is populated with `proofread_text`
   - Status bar shows model name, latency, and issue count
   - If issues are found, a dialog displays details

5. **Use or Edit Result**
   - Copy Proto Output to Output field
   - Or manually edit Output based on Proto Output
   - Save the record

### Advanced Usage

#### Custom Prompts

The prompt field accepts any instruction text:

```
Examples:
- "Proofread"
- "Fix grammar and spelling"
- "Improve clarity and conciseness"
- "Make more professional"
- "Simplify for general audience"
```

The prompt is sent directly to the API as the `prompt` parameter.

#### Batch Processing

For multiple similar corrections:
1. Set the prompt once
2. Enter different inputs
3. Click "Get Proto Result" for each
4. Save each record

## Technical Implementation

### Architecture

```
MainWindow.xaml.cs
    ¡é uses
ProofreadApiService
    ¡é calls
HTTP API (/v1/evaluations)
    ¡é returns
ProofreadResponse
    ¡é populates
Proto Output Field
```

### Key Classes

#### 1. ProofreadApiService
**Location**: `Services/ProofreadApiService.cs`

Main service class for API communication:

```csharp
public class ProofreadApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;
    private readonly string _authToken;

    public async Task<ProofreadResponse?> GetProofreadResultAsync(
        string prompt, 
        string candidateText)
    {
        // Makes HTTP POST request
        // Handles serialization/deserialization
        // Manages authentication
    }
}
```

#### 2. ApiConfiguration
**Location**: `Services/ApiConfiguration.cs`

Configuration management:

```csharp
public class ApiConfiguration
{
    public string ApiUrl { get; set; }
    public string AuthToken { get; set; }
    public string DefaultPrompt { get; set; }

    public static ApiConfiguration Load(string workingDirectory);
    public void Save(string workingDirectory);
}
```

#### 3. Request/Response Models

**ProofreadRequest**:
```csharp
public class ProofreadRequest
{
    public string Prompt { get; set; }
    public string CandidateText { get; set; }
}
```

**ProofreadResponse**:
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

**ProofreadIssue**:
```csharp
public class ProofreadIssue
{
    public string Category { get; set; }
    public int SpanStart { get; set; }
    public int SpanEnd { get; set; }
    public string Suggestion { get; set; }
    public string Severity { get; set; }
    public double Confidence { get; set; }
}
```

### Event Handler

The button click handler in `MainWindow.xaml.cs`:

```csharp
private async void BtnGetProtoResult_Click(object sender, RoutedEventArgs e)
{
    // 1. Validate input and prompt
    if (string.IsNullOrWhiteSpace(txtInput.Text)) { ... }
    if (string.IsNullOrWhiteSpace(txtPrompt.Text)) { ... }

    // 2. Disable button during call
    btnGetProtoResult.IsEnabled = false;
    
    // 3. Call API
    var response = await _apiService.GetProofreadResultAsync(
        txtPrompt.Text,
        txtInput.Text
    );

    // 4. Handle response
    if (response?.Status == "completed")
    {
        txtProtoOutput.Text = response.ProofreadText;
        // Show issues if any
    }
    
    // 5. Re-enable button
    btnGetProtoResult.IsEnabled = true;
}
```

## Error Handling

### Validation Errors

**Empty Input**:
```
Error: Input cannot be empty
¡æ MessageBox: "Please enter an input value before getting proto result."
```

**Empty Prompt**:
```
Error: Prompt cannot be empty
¡æ MessageBox: "Please enter a prompt (e.g., 'Proofread')."
```

### Network Errors

**Connection Refused**:
```
Error: API call failed: No connection could be made...
¡æ MessageBox with troubleshooting steps:
  1. API server is running at http://...
  2. Network connectivity
  3. API configuration in api_config.json
```

**Authentication Failed** (401):
```
Error: API call failed: Unauthorized
¡æ Check authToken in api_config.json
```

**Timeout**:
```
Error: API call failed: The operation has timed out
¡æ Check network connectivity and API response time
```

### API Status Errors

**Non-Completed Status**:
```
Status: "failed" or "pending"
¡æ Status bar: "API returned status: failed"
¡æ Proto Output: "[API Error: {failure_reason}]"
```

## Testing

### Manual Testing

#### Test 1: Basic Functionality
```
1. Input: "The launch were sucessful"
2. Prompt: "Proofread"
3. Click "Get Proto Result"
4. Expected: Proto Output shows "The launch was successful"
5. Expected: Status shows "API Success! Model: nemotron-4-340b-instruct, Latency: ~1200ms"
```

#### Test 2: Error Handling
```
1. Stop API server
2. Input: "Test text"
3. Click "Get Proto Result"
4. Expected: Error dialog with troubleshooting tips
```

#### Test 3: Custom Prompt
```
1. Input: "hello world"
2. Prompt: "Capitalize"
3. Click "Get Proto Result"
4. Expected: Proto Output shows capitalized version
```

### PowerShell Testing

Test the API directly:

```powershell
$headers = @{
    "Authorization" = "Bearer local-dev-token"
    "Content-Type"  = "application/json"
}

$body = @{
    prompt = "Proofread"
    candidate_text = "The launch were sucessful"
} | ConvertTo-Json

Invoke-RestMethod `
    -Uri "http://192.168.111.79:8081/v1/evaluations" `
    -Method POST `
    -Headers $headers `
    -Body $body
```

Expected output:
```
id              : 0b68e2c1-6c08-414a-94f5-1375f122bbbf
status          : completed
proofread_text  : The launch was successful
issues          : {@{category=auto-correction; ...}}
model_name      : nemotron-4-340b-instruct
latency_ms      : 1200
```

## Troubleshooting

### Common Issues

#### 1. Connection Refused
**Symptom**: "No connection could be made because the target machine actively refused it"

**Solutions**:
- ? Verify API server is running
- ? Check API URL in `api_config.json`
- ? Test with PowerShell/curl
- ? Check firewall settings

#### 2. 401 Unauthorized
**Symptom**: "Unauthorized" or 401 status code

**Solutions**:
- ? Verify `authToken` in `api_config.json`
- ? Check token hasn't expired
- ? Test with PowerShell using same token

#### 3. Timeout
**Symptom**: "The operation has timed out"

**Solutions**:
- ? Check network connectivity
- ? Verify API is responsive (not overloaded)
- ? Consider increasing timeout (future enhancement)

#### 4. Invalid Response
**Symptom**: JSON parsing errors or unexpected format

**Solutions**:
- ? Verify API version compatibility
- ? Check API endpoint URL (should be `/v1/evaluations`)
- ? Test API with PowerShell to verify response format

#### 5. Button Stays Disabled
**Symptom**: Button doesn't re-enable after error

**Solutions**:
- ?? Known limitation - restart application
- ?? Will be fixed in future version with better error recovery

## Performance Considerations

### Latency
- Typical API call: 1000-1500ms
- UI remains responsive during call (async)
- Button disabled to prevent duplicate requests

### Network
- Single HTTP POST per request
- Request size: ~100-500 bytes
- Response size: ~500-2000 bytes

### Rate Limiting
- Currently no rate limiting in client
- Respect API server's rate limits
- Consider adding delays between batch requests

## Security Considerations

### Authentication Token
- Stored in plain text in `api_config.json`
- ?? Do not commit `api_config.json` to version control
- ?? Use `.gitignore` to exclude config file
- ? Sample file provided for reference

### Network Security
- HTTP by default (not HTTPS)
- ?? Token sent in clear text over network
- ?? Consider using HTTPS in production
- ?? Consider VPN for sensitive data

### Data Privacy
- Input text sent to external API
- ?? Do not send sensitive/confidential information
- ?? Review API provider's privacy policy
- ? All data stored locally in JSON files

## Future Enhancements

### Planned Features
- [ ] Retry logic with exponential backoff
- [ ] Configurable timeout values
- [ ] API response caching
- [ ] Multiple API provider support (OpenAI, Azure, etc.)
- [ ] Batch processing of multiple inputs
- [ ] API health check on startup
- [ ] HTTPS support
- [ ] Token encryption in config file
- [ ] Rate limiting in client
- [ ] Response time metrics and logging

### Provider Support (Future)

#### OpenAI API
```json
{
  "provider": "openai",
  "apiKey": "sk-...",
  "model": "gpt-4",
  "endpoint": "https://api.openai.com/v1/chat/completions"
}
```

#### Azure OpenAI
```json
{
  "provider": "azure",
  "apiKey": "...",
  "endpoint": "https://{resource}.openai.azure.com/",
  "deployment": "gpt-4"
}
```

## Code Examples

### Calling API from Code

```csharp
// Initialize service
var apiService = new ProofreadApiService(
    "http://192.168.111.79:8081",
    "local-dev-token"
);

// Make request
var response = await apiService.GetProofreadResultAsync(
    prompt: "Proofread",
    candidateText: "The launch were sucessful"
);

// Handle response
if (response?.Status == "completed")
{
    Console.WriteLine($"Result: {response.ProofreadText}");
    Console.WriteLine($"Issues: {response.Issues.Count}");
}
```

### Custom Error Handling

```csharp
try
{
    var response = await apiService.GetProofreadResultAsync(prompt, text);
    // ... handle response
}
catch (HttpRequestException ex)
{
    // Network error
    Console.WriteLine($"Network error: {ex.Message}");
}
catch (JsonException ex)
{
    // JSON parsing error
    Console.WriteLine($"Invalid response format: {ex.Message}");
}
catch (Exception ex)
{
    // Other errors
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

## Support

For issues with API integration:
1. Check this guide's troubleshooting section
2. Test API with PowerShell/curl
3. Review application logs (status bar messages)
4. Check `api_config.json` configuration
5. Refer to main project repository for updates

## Version History

- **v1.2.0**: Initial API integration implementation
- **v1.1.0**: Dark theme and always on top
- **v1.0.0**: Initial release

---

**Note**: This API integration is fully functional as of version 1.2.0. The implementation uses the nemotron-4-340b-instruct model via the configured endpoint.
