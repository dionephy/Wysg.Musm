using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Wysg.Musm.Domain.AI;

namespace Wysg.Musm.Infrastructure.AI.Ollama;

// Minimal Ollama client (non-stream) calling /api/chat
public sealed class OllamaLlmClient : ILLMClient, IDisposable
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly OllamaOptions _opts;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _modelLocks = new();

    public OllamaLlmClient(IHttpClientFactory httpFactory, OllamaOptions opts)
    {
        _httpFactory = httpFactory;
        _opts = opts;
    }

    private SemaphoreSlim GetLock(string model) => _modelLocks.GetOrAdd(model, _ => new SemaphoreSlim(_opts.MaxParallel, _opts.MaxParallel));

    public async Task<LlmResponse> SendAsync(LlmRequest request, CancellationToken ct = default)
    {
        var model = string.IsNullOrWhiteSpace(request.Model) ? _opts.DefaultModel : request.Model;
        var gate = GetLock(model);
        if (!await gate.WaitAsync(_opts.QueueTimeoutMs, ct).ConfigureAwait(false))
            throw new TimeoutException("ollama queue saturated");
        try
        {
            var http = _httpFactory.CreateClient("ollama");
            var url = _opts.BaseUrl.TrimEnd('/') + "/api/chat";
            var payload = new
            {
                model = model,
                options = new
                {
                    temperature = request.Temperature,
                    num_predict = request.MaxTokens ?? _opts.DefaultMaxTokens
                },
                messages = new object[]
                {
                    new { role = "system", content = request.SystemPrompt },
                    new { role = "user", content = request.UserPrompt }
                },
                stream = false
            };
            using var msg = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, JsonOpts), Encoding.UTF8, "application/json")
            };
            using var resp = await http.SendAsync(msg, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            var root = doc.RootElement;
            string text = root.TryGetProperty("message", out var m) && m.TryGetProperty("content", out var c)
                ? c.GetString() ?? string.Empty
                : root.GetRawText();
            return new LlmResponse(text.Trim());
        }
        finally
        {
            gate.Release();
        }
    }

    public void Dispose()
    {
        foreach (var kv in _modelLocks)
            kv.Value.Dispose();
    }
}
