# API Integration Guide - LLM Server Connection

## Overview

This document outlines the planned implementation for the "Get Proto Result" feature, which will connect to LLM servers to generate prototype outputs automatically.

## Current Status

?? **Not Implemented** - The "Get Proto Result" button currently shows a placeholder message.

## Planned Architecture

### Flow Diagram

```
[User Input] ¡æ [Click "Get Proto Result"] ¡æ [HTTP Request to LLM API] 
    ¡æ [Receive Response] ¡æ [Populate Proto Output Field]
```

### Components to Implement

1. **Configuration Management**
   - Store API endpoint URL
   - Store API keys securely
   - Support multiple LLM providers (OpenAI, Azure OpenAI, custom)

2. **HTTP Client Service**
   - Async HTTP communication
   - Error handling and retries
   - Timeout management

3. **Response Parser**
   - Parse LLM API responses
   - Extract generated text
   - Handle different response formats

## Supported LLM Providers (Planned)

### 1. OpenAI API

**Endpoint**: `https://api.openai.com/v1/chat/completions`

**Request Format**:
```json
{
  "model": "gpt-4",
  "messages": [
    {
      "role": "system",
      "content": "[Content from prompt.txt]"
    },
    {
      "role": "user",
      "content": "[Content from Input field]"
    }
  ],
  "temperature": 0.7,
  "max_tokens": 1000
}
```

**Headers**:
```
Authorization: Bearer YOUR_API_KEY
Content-Type: application/json
```

### 2. Azure OpenAI

**Endpoint**: `https://{resource-name}.openai.azure.com/openai/deployments/{deployment-id}/chat/completions?api-version=2024-02-15-preview`

**Request Format**: Same as OpenAI API

**Headers**:
```
api-key: YOUR_AZURE_API_KEY
Content-Type: application/json
```

### 3. Custom LLM Endpoints

Support for custom endpoints with configurable:
- Base URL
- Authentication method
- Request/response format

## Implementation Plan

### Phase 1: Configuration UI

Create a settings window with:

```csharp
public class LlmSettings
{
    public string Provider { get; set; } // "OpenAI", "AzureOpenAI", "Custom"
    public string ApiEndpoint { get; set; }
    public string ApiKey { get; set; }
    public string Model { get; set; } // "gpt-4", "gpt-3.5-turbo", etc.
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 1000;
}
```

### Phase 2: HTTP Service

```csharp
public interface ILlmService
{
    Task<string> GetCompletionAsync(string prompt, string input, CancellationToken cancellationToken = default);
}

public class OpenAiService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly LlmSettings _settings;

    public async Task<string> GetCompletionAsync(string prompt, string input, CancellationToken cancellationToken = default)
    {
        // Build request
        var request = new
        {
            model = _settings.Model,
            messages = new[]
            {
                new { role = "system", content = prompt },
                new { role = "user", content = input }
            },
            temperature = _settings.Temperature,
            max_tokens = _settings.MaxTokens
        };

        // Send request
        var response = await _httpClient.PostAsJsonAsync(_settings.ApiEndpoint, request, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Parse response
        var result = await response.Content.ReadFromJsonAsync<OpenAiResponse>(cancellationToken);
        return result?.Choices?[0]?.Message?.Content ?? string.Empty;
    }
}

public class OpenAiResponse
{
    public Choice[] Choices { get; set; }
}

public class Choice
{
    public Message Message { get; set; }
}

public class Message
{
    public string Content { get; set; }
}
```

### Phase 3: UI Integration

Update the button click handler:

```csharp
private async void BtnGetProtoResult_Click(object sender, RoutedEventArgs e)
{
    try
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(txtInput.Text))
        {
            UpdateStatus("Error: Input is required", isError: true);
            return;
        }

        // Disable button during request
        btnGetProtoResult.IsEnabled = false;
        UpdateStatus("Requesting proto result from LLM...");

        // Get LLM service
        var llmService = GetLlmService(); // Factory method based on settings

        // Make request
        var protoResult = await llmService.GetCompletionAsync(
            txtPrompt.Text,
            txtInput.Text,
            CancellationToken.None
        );

        // Update UI
        txtProtoOutput.Text = protoResult;
        UpdateStatus("Proto result received successfully");
    }
    catch (HttpRequestException ex)
    {
        UpdateStatus($"Network error: {ex.Message}", isError: true);
        MessageBox.Show($"Failed to connect to LLM server:\n\n{ex.Message}", 
            "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
    catch (Exception ex)
    {
        UpdateStatus($"Error: {ex.Message}", isError: true);
        MessageBox.Show($"An error occurred:\n\n{ex.Message}", 
            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
    finally
    {
        btnGetProtoResult.IsEnabled = true;
    }
}
```

## Configuration Storage

### Option 1: App.config / appsettings.json

```json
{
  "LlmSettings": {
    "Provider": "OpenAI",
    "ApiEndpoint": "https://api.openai.com/v1/chat/completions",
    "Model": "gpt-4",
    "Temperature": 0.7,
    "MaxTokens": 1000
  }
}
```

**Note**: API keys should NOT be stored in plain text config files.

### Option 2: Windows Credential Manager

Use `CredentialManager` to securely store API keys:

```csharp
using System.Security.Cryptography;
using Windows.Security.Credentials;

public class SecureSettingsService
{
    private const string ResourceName = "LlmDataBuilder.ApiKey";

    public void SaveApiKey(string apiKey)
    {
        var credential = new PasswordCredential(ResourceName, "user", apiKey);
        var vault = new PasswordVault();
        vault.Add(credential);
    }

    public string GetApiKey()
    {
        var vault = new PasswordVault();
        try
        {
            var credential = vault.Retrieve(ResourceName, "user");
            credential.RetrievePassword();
            return credential.Password;
        }
        catch
        {
            return string.Empty;
        }
    }
}
```

### Option 3: User Settings File

Create `settings.json` in user's AppData folder:

```csharp
string settingsPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "LlmDataBuilder",
    "settings.json"
);
```

## Required NuGet Packages

```xml
<PackageReference Include="System.Net.Http.Json" Version="9.0.0" />
```

For Azure SDK integration (optional):
```xml
<PackageReference Include="Azure.AI.OpenAI" Version="2.0.0" />
```

## Error Handling

### Common Scenarios

1. **Network Timeout**
   - Show clear error message
   - Suggest checking internet connection
   - Allow retry

2. **Invalid API Key**
   - Detect 401/403 responses
   - Prompt user to check settings
   - Guide to settings configuration

3. **Rate Limiting**
   - Detect 429 responses
   - Show friendly message about API limits
   - Implement exponential backoff for retries

4. **Empty Response**
   - Handle cases where LLM returns no content
   - Show warning to user
   - Allow manual entry

### Example Error Handling

```csharp
catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
{
    UpdateStatus("Invalid API key", isError: true);
    MessageBox.Show("Your API key appears to be invalid. Please check your settings.", 
        "Authentication Error", MessageBoxButton.OK, MessageBoxImage.Error);
}
catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
{
    UpdateStatus("Rate limit exceeded", isError: true);
    MessageBox.Show("You've exceeded the API rate limit. Please wait and try again.", 
        "Rate Limit", MessageBoxButton.OK, MessageBoxImage.Warning);
}
```

## Testing Strategy

### Unit Tests

```csharp
[TestClass]
public class LlmServiceTests
{
    [TestMethod]
    public async Task GetCompletionAsync_ValidInput_ReturnsResult()
    {
        // Arrange
        var mockHttpClient = CreateMockHttpClient();
        var service = new OpenAiService(mockHttpClient, CreateTestSettings());

        // Act
        var result = await service.GetCompletionAsync("prompt", "input");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }
}
```

### Integration Tests

- Test against actual APIs (with test keys)
- Verify response parsing
- Test error scenarios
- Measure response times

## Security Considerations

### API Key Security

1. **Never commit API keys to source control**
2. **Use environment variables or secure storage**
3. **Implement key rotation support**
4. **Log API usage (but not keys)**

### Example: Environment Variable

```csharp
public class LlmSettings
{
    public string ApiKey => Environment.GetEnvironmentVariable("LLM_API_KEY") 
        ?? GetSecurelyStoredKey();
}
```

## Performance Optimization

### Caching

Consider caching LLM responses for identical inputs:

```csharp
private readonly Dictionary<string, string> _responseCache = new();

public async Task<string> GetCompletionAsync(string prompt, string input)
{
    string cacheKey = $"{prompt}:{input}".GetHashCode().ToString();
    
    if (_responseCache.TryGetValue(cacheKey, out string cached))
    {
        return cached;
    }

    string result = await CallLlmApiAsync(prompt, input);
    _responseCache[cacheKey] = result;
    return result;
}
```

### Async/Await Best Practices

- Use `ConfigureAwait(false)` for library code
- Implement proper cancellation token support
- Show progress indicators for long operations

## UI Enhancements

### Loading Indicator

```xaml
<ProgressBar x:Name="progressBar" 
             IsIndeterminate="True" 
             Visibility="Collapsed"
             Height="4"
             Margin="0,5"/>
```

```csharp
private async Task GetProtoResultWithProgressAsync()
{
    progressBar.Visibility = Visibility.Visible;
    try
    {
        // API call
    }
    finally
    {
        progressBar.Visibility = Visibility.Collapsed;
    }
}
```

### Cancellation Support

```xaml
<Button x:Name="btnCancelRequest" 
        Content="Cancel" 
        Click="BtnCancelRequest_Click"
        Visibility="Collapsed"/>
```

```csharp
private CancellationTokenSource _cts;

private async void BtnGetProtoResult_Click(object sender, RoutedEventArgs e)
{
    _cts = new CancellationTokenSource();
    btnCancelRequest.Visibility = Visibility.Visible;
    
    try
    {
        await GetProtoResultAsync(_cts.Token);
    }
    catch (OperationCanceledException)
    {
        UpdateStatus("Request cancelled");
    }
    finally
    {
        btnCancelRequest.Visibility = Visibility.Collapsed;
    }
}

private void BtnCancelRequest_Click(object sender, RoutedEventArgs e)
{
    _cts?.Cancel();
}
```

## Future Enhancements

1. **Batch Processing**: Generate proto outputs for multiple records
2. **Comparison View**: Side-by-side comparison of output vs protoOutput
3. **Model Selection**: Choose different models for different types of inputs
4. **Cost Tracking**: Monitor API usage and costs
5. **Local LLM Support**: Integration with local models (Ollama, llama.cpp)

## References

- [OpenAI API Documentation](https://platform.openai.com/docs/api-reference)
- [Azure OpenAI Service Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [System.Net.Http Best Practices](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines)

## Implementation Checklist

- [ ] Create LlmSettings class
- [ ] Implement configuration UI
- [ ] Create ILlmService interface
- [ ] Implement OpenAiService
- [ ] Implement AzureOpenAiService
- [ ] Add secure credential storage
- [ ] Update button click handler
- [ ] Add loading indicators
- [ ] Implement error handling
- [ ] Add cancellation support
- [ ] Write unit tests
- [ ] Perform integration testing
- [ ] Update user documentation

---

**Note**: This feature is planned for a future release. The current version provides the UI placeholder to prepare for this functionality.
