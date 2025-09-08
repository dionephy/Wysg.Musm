using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Wysg.Musm.Editor.Ghosting;

public sealed class HttpGhostSuggestionClient : IGhostSuggestionClient
{
    private readonly HttpClient _http;
    private readonly string _endpoint; // e.g. "http://localhost:5111/suggest"

    public HttpGhostSuggestionClient(HttpClient http, string endpoint)
    {
        _http = http;
        _endpoint = endpoint.TrimEnd('/');
    }

    public async Task<GhostResponse> SuggestAsync(GhostRequest request, CancellationToken ct)
    {
        var resp = await _http.PostAsJsonAsync($"{_endpoint}", request, ct);
        resp.EnsureSuccessStatusCode();
        var payload = await resp.Content.ReadFromJsonAsync<GhostResponse>(cancellationToken: ct)
                      ?? new GhostResponse(Array.Empty<GhostSuggestion>());
        return payload;
    }
}

