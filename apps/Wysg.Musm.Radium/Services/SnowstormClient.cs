using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Minimal Snowstorm REST client. Tries multiple compatible endpoints across Snowstorm versions
    /// and avoids throwing on HTTP errors (returns empty list instead).
    /// </summary>
    public sealed class SnowstormClient : ISnowstormClient
    {
        private readonly IRadiumLocalSettings _settings;
        private readonly HttpClient _http;
        public SnowstormClient(IRadiumLocalSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _http = new HttpClient();
        }

        private string BaseUrl
            => string.IsNullOrWhiteSpace(_settings.SnowstormBaseUrl)
               ? "https://snowstorm.ihtsdotools.org/snowstorm"
               : _settings.SnowstormBaseUrl.TrimEnd('/');

        public async Task<IReadOnlyList<SnomedConcept>> SearchConceptsAsync(string query, int limit = 50)
        {
            var list = new List<SnomedConcept>();
            if (string.IsNullOrWhiteSpace(query)) return list;

            // Candidate endpoints (newer first). Some deployments include /snomed-ct in base, some not.
            // We'll use whichever base the user provided and try both description-search and descriptions.
            var urls = new[]
            {
                $"{BaseUrl}/browser/MAIN/description-search?term={Uri.EscapeDataString(query)}&activeFilter=true&conceptActive=true&groupByConcept=true&searchMode=STANDARD&limit={limit}",
                $"{BaseUrl}/browser/MAIN/descriptions?term={Uri.EscapeDataString(query)}&active=true&conceptActive=true&groupByConcept=true&searchMode=STANDARD&limit={limit}"
            };

            foreach (var url in urls)
            {
                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, url);
                    using var resp = await _http.SendAsync(req).ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode) continue; // try next shape

                    using var content = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    using var doc = await JsonDocument.ParseAsync(content).ConfigureAwait(false);
                    if (!doc.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (var it in items.EnumerateArray())
                    {
                        // Prefer concept payload under "concept"; otherwise try to resolve from fields directly
                        var hasConcept = it.TryGetProperty("concept", out var concept);
                        var node = hasConcept ? concept : it;
                        try
                        {
                            var idStr = node.TryGetProperty("conceptId", out var idEl) ? (idEl.GetString() ?? string.Empty) : string.Empty;
                            if (string.IsNullOrEmpty(idStr) && it.TryGetProperty("concept", out var c2) && c2.TryGetProperty("conceptId", out var idEl2))
                                idStr = idEl2.GetString() ?? string.Empty;
                            long id = 0; _ = long.TryParse(idStr, out id);

                            string fsn = idStr;
                            if (node.TryGetProperty("fsn", out var fsnObj) && fsnObj.ValueKind == JsonValueKind.Object && fsnObj.TryGetProperty("term", out var fsnTerm))
                                fsn = fsnTerm.GetString() ?? idStr;
                            else if (node.TryGetProperty("fsn", out var fsnStr) && fsnStr.ValueKind == JsonValueKind.String)
                                fsn = fsnStr.GetString() ?? idStr;

                            string? pt = null;
                            if (node.TryGetProperty("pt", out var ptObj) && ptObj.ValueKind == JsonValueKind.Object && ptObj.TryGetProperty("term", out var ptTerm))
                                pt = ptTerm.GetString();
                            else if (node.TryGetProperty("pt", out var ptStr) && ptStr.ValueKind == JsonValueKind.String)
                                pt = ptStr.GetString();

                            bool active = node.TryGetProperty("active", out var actEl) && actEl.ValueKind == JsonValueKind.True || false;

                            if (id != 0)
                                list.Add(new SnomedConcept(id, idStr, fsn, pt, active, DateTime.UtcNow));
                        }
                        catch { /* skip malformed item */ }
                    }

                    if (list.Count > 0) return list; // success path
                }
                catch
                {
                    // ignore and try next endpoint; if all fail, return empty
                }
            }

            return list; // empty if no endpoint succeeded
        }
    }
}
