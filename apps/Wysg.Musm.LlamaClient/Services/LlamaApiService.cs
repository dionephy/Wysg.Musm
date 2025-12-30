using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Wysg.Musm.LlamaClient.Models;

namespace Wysg.Musm.LlamaClient.Services;

/// <summary>
/// Service for communicating with the Llama API (OpenAI-compatible).
/// </summary>
public class LlamaApiService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private CancellationTokenSource? _currentCts;

    public string BaseUrl { get; set; } = "http://127.0.0.1:8000";

    public LlamaApiService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10) // Long timeout for streaming
        };

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Lists available models from the API.
    /// </summary>
    public async Task<ModelsResponse?> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl.TrimEnd('/')}/v1/models";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<ModelsResponse>(json, _jsonOptions);
    }

    /// <summary>
    /// Sends a chat completion request without streaming.
    /// </summary>
    public async Task<ChatCompletionResponse?> ChatAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        request.Stream = false;
        var url = $"{BaseUrl.TrimEnd('/')}/v1/chat/completions";

        var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {responseJson}");
        }

        return JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson, _jsonOptions);
    }

    /// <summary>
    /// Sends a chat completion request with streaming, yielding chunks as they arrive.
    /// </summary>
    public async IAsyncEnumerable<ChatCompletionChunk> ChatStreamAsync(
        ChatCompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _currentCts?.Cancel();
        _currentCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _currentCts.Token;

        request.Stream = true;
        var url = $"{BaseUrl.TrimEnd('/')}/v1/chat/completions";

        var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };

        using var response = await _httpClient.SendAsync(
            requestMessage,
            HttpCompletionOption.ResponseHeadersRead,
            token);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(token);
            throw new HttpRequestException($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {errorBody}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(token);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !token.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(token);

            if (string.IsNullOrEmpty(line))
                continue;

            if (!line.StartsWith("data: "))
                continue;

            var data = line["data: ".Length..];

            if (data == "[DONE]")
                yield break;

            ChatCompletionChunk? chunk = null;
            try
            {
                chunk = JsonSerializer.Deserialize<ChatCompletionChunk>(data, _jsonOptions);
            }
            catch (JsonException)
            {
                // Skip malformed chunks
                continue;
            }

            if (chunk != null)
                yield return chunk;
        }
    }

    /// <summary>
    /// Cancels any ongoing streaming request.
    /// </summary>
    public void CancelCurrentRequest()
    {
        _currentCts?.Cancel();
    }

    /// <summary>
    /// Checks if the API is reachable.
    /// </summary>
    public async Task<bool> IsApiAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{BaseUrl.TrimEnd('/')}/v1/models";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Serializes a request to JSON for "Copy as JSON" feature.
    /// </summary>
    public string SerializeRequest(ChatCompletionRequest request)
    {
        return JsonSerializer.Serialize(request, _jsonOptions);
    }

    /// <summary>
    /// Deserializes a JSON request for "Replay" feature.
    /// </summary>
    public ChatCompletionRequest? DeserializeRequest(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ChatCompletionRequest>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        _currentCts?.Cancel();
        _currentCts?.Dispose();
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
