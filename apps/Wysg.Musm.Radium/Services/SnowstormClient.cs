using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.Services
{
    /// <summary>
    /// Helper record for tracking concept info during term accumulation.
    /// </summary>
    internal sealed record ConceptInfo(long ConceptId, string ConceptIdStr, string Fsn, string? Pt, bool Active);

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
                $"{BaseUrl}/MAIN/description-search?term={Uri.EscapeDataString(query)}&activeFilter=true&conceptActive=true&groupByConcept=true&searchMode=STANDARD&limit={limit}",
                $"{BaseUrl}/MAIN/descriptions?term={Uri.EscapeDataString(query)}&active=true&conceptActive=true&groupByConcept=true&searchMode=STANDARD&limit={limit}"
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

        public async Task<(IReadOnlyList<SnomedConceptWithTerms> concepts, string? nextSearchAfter)> BrowseBySemanticTagAsync(
            string semanticTag, 
            int offset = 0, 
            int limit = 10, 
            string? searchAfterToken = null)
        {
            var list = new List<SnomedConceptWithTerms>();
            string? returnToken = null;
            
            // Use ECL (Expression Constraint Language) queries for proper semantic tag filtering
            string eclQuery;
            switch (semanticTag.ToLowerInvariant())
            {
                case "all":
                    eclQuery = "<<138875005"; // SNOMED CT Concept (all active concepts)
                    break;
                case "body structure":
                    eclQuery = "<<123037004"; // Body structure (body structure)
                    break;
                case "finding":
                    eclQuery = "<<404684003"; // Clinical finding (finding)
                    break;
                case "disorder":
                    eclQuery = "<<64572001"; // Disease (disorder)
                    break;
                case "procedure":
                    eclQuery = "<<71388002"; // Procedure (procedure)
                    break;
                case "observable entity":
                    eclQuery = "<<363787002"; // Observable entity (observable entity)
                    break;
                case "substance":
                    eclQuery = "<<105590001"; // Substance (substance)
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine($"[SnowstormClient] Unknown domain: {semanticTag}, using 'all'");
                    eclQuery = "<<138875005";
                    break;
            }

            System.Diagnostics.Debug.WriteLine($"[SnowstormClient] Fetching domain '{semanticTag}' using ECL: {eclQuery}");
            System.Diagnostics.Debug.WriteLine($"[SnowstormClient] Requested offset={offset}, limit={limit}");
            System.Diagnostics.Debug.WriteLine($"[SnowstormClient] searchAfterToken: {searchAfterToken ?? "(null - will fetch from beginning)"}");

            try
            {
                // If searchAfterToken is provided, use it directly for efficient pagination (Next button)
                // If not provided, we need to paginate from the beginning (Jump to page, Previous, or first load)
                
                if (!string.IsNullOrEmpty(searchAfterToken))
                {
                    // EFFICIENT PATH: Use provided token to jump directly to the page
                    System.Diagnostics.Debug.WriteLine("[SnowstormClient] Using cached searchAfter token for efficient pagination");
                    
                    var url = $"{BaseUrl}/MAIN/concepts?ecl={Uri.EscapeDataString(eclQuery)}&limit={limit}&activeFilter=true&searchAfter={Uri.EscapeDataString(searchAfterToken)}";
                    
                    System.Diagnostics.Debug.WriteLine($"[SnowstormClient] Fetching page: {url}");

                    using var req = new HttpRequestMessage(HttpMethod.Get, url);
                    using var resp = await _http.SendAsync(req).ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SnowstormClient] HTTP {resp.StatusCode}: {resp.ReasonPhrase}");
                        return (list, null);
                    }

                    using var content = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    using var doc = await JsonDocument.ParseAsync(content).ConfigureAwait(false);
                    
                    if (!doc.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                    {
                        System.Diagnostics.Debug.WriteLine("[SnowstormClient] No 'items' array in response");
                        return (list, null);
                    }

                    var pagedConcepts = new List<(string id, string fsn, string? pt, bool active)>();
                    foreach (var conceptItem in items.EnumerateArray())
                    {
                        try
                        {
                            var idStr = conceptItem.TryGetProperty("conceptId", out var idEl) ? (idEl.GetString() ?? string.Empty) : string.Empty;
                            if (string.IsNullOrEmpty(idStr))
                                continue;

                            string fsn = idStr;
                            if (conceptItem.TryGetProperty("fsn", out var fsnObj))
                            {
                                if (fsnObj.ValueKind == JsonValueKind.Object && fsnObj.TryGetProperty("term", out var fsnTerm))
                                    fsn = fsnTerm.GetString() ?? idStr;
                                else if (fsnObj.ValueKind == JsonValueKind.String)
                                    fsn = fsnObj.GetString() ?? idStr;
                            }

                            string? pt = null;
                            if (conceptItem.TryGetProperty("pt", out var ptObj))
                            {
                                if (ptObj.ValueKind == JsonValueKind.Object && ptObj.TryGetProperty("term", out var ptTerm))
                                    pt = ptTerm.GetString();
                                else if (ptObj.ValueKind == JsonValueKind.String)
                                    pt = ptObj.GetString();
                            }

                            bool active = conceptItem.TryGetProperty("active", out var actEl) && actEl.ValueKind == JsonValueKind.True || false;

                            pagedConcepts.Add((idStr, fsn, pt, active));
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SnowstormClient] Error processing concept: {ex.Message}");
                        }
                    }

                    // Get next searchAfter token
                    if (doc.RootElement.TryGetProperty("searchAfter", out var searchAfterEl))
                    {
                        returnToken = searchAfterEl.GetString();
                        System.Diagnostics.Debug.WriteLine($"[SnowstormClient] Next searchAfter token: {returnToken}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[SnowstormClient] No more results available (no searchAfter token)");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[SnowstormClient] Fetched {pagedConcepts.Count} concepts using cached token");

                    // Fetch descriptions for each concept
                    await FetchDescriptionsForConcepts(pagedConcepts, list);
                    
                    return (list, returnToken);
                }
                else
                {
                    // FALLBACK PATH: Paginate from beginning (for Jump to page, Previous, or initial load)
                    System.Diagnostics.Debug.WriteLine("[SnowstormClient] No token provided - paginating from beginning");
                    
                    var currentOffset = 0;
                    string? currentSearchAfter = null;
                    var allConcepts = new List<(string id, string fsn, string? pt, bool active)>();

                    // Keep fetching pages until we have enough concepts to satisfy offset + limit
                    while (currentOffset < offset + limit)
                    {
                        // Use non-browser endpoint for ECL queries (browser API doesn't support ECL properly)
                        var url = $"{BaseUrl}/MAIN/concepts?ecl={Uri.EscapeDataString(eclQuery)}&limit={limit}&activeFilter=true";
                        if (!string.IsNullOrEmpty(currentSearchAfter))
                        {
                            url += $"&searchAfter={Uri.EscapeDataString(currentSearchAfter)}";
                        }

                        System.Diagnostics.Debug.WriteLine($"[SnowstormClient] Fetching page: {url}");

                        using var req = new HttpRequestMessage(HttpMethod.Get, url);
                        using var resp = await _http.SendAsync(req).ConfigureAwait(false);
                        if (!resp.IsSuccessStatusCode)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SnowstormClient] HTTP {resp.StatusCode}: {resp.ReasonPhrase}");
                            break;
                        }

                        using var content = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
                        using var doc = await JsonDocument.ParseAsync(content).ConfigureAwait(false);
                        
                        if (!doc.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                        {
                            System.Diagnostics.Debug.WriteLine("[SnowstormClient] No 'items' array in response");
                            break;
                        }

                        var itemCount = 0;
                        foreach (var conceptItem in items.EnumerateArray())
                        {
                            itemCount++;
                            try
                            {
                                var idStr = conceptItem.TryGetProperty("conceptId", out var idEl) ? (idEl.GetString() ?? string.Empty) : string.Empty;
                                if (string.IsNullOrEmpty(idStr))
                                    continue;

                                // Non-browser API returns fsn and pt as objects with "term" property
                                string fsn = idStr;
                                if (conceptItem.TryGetProperty("fsn", out var fsnObj))
                                {
                                    if (fsnObj.ValueKind == JsonValueKind.Object && fsnObj.TryGetProperty("term", out var fsnTerm))
                                        fsn = fsnTerm.GetString() ?? idStr;
                                    else if (fsnObj.ValueKind == JsonValueKind.String)
                                        fsn = fsnObj.GetString() ?? idStr;
                                }

                                string? pt = null;
                                if (conceptItem.TryGetProperty("pt", out var ptObj))
                                {
                                    if (ptObj.ValueKind == JsonValueKind.Object && ptObj.TryGetProperty("term", out var ptTerm))
                                        pt = ptTerm.GetString();
                                    else if (ptObj.ValueKind == JsonValueKind.String)
                                        pt = ptObj.GetString();
                                }

                                bool active = conceptItem.TryGetProperty("active", out var actEl) && actEl.ValueKind == JsonValueKind.True || false;

                                allConcepts.Add((idStr, fsn, pt, active));
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[SnowstormClient] Error processing concept: {ex.Message}");
                            }
                        }

                        currentOffset += itemCount;
                        System.Diagnostics.Debug.WriteLine($"[SnowstormClient] Fetched {itemCount} concepts, total accumulated: {allConcepts.Count}, currentOffset: {currentOffset}");

                        // Check if there are more results
                        if (doc.RootElement.TryGetProperty("searchAfter", out var searchAfterEl))
                        {
                            currentSearchAfter = searchAfterEl.GetString();
                            System.Diagnostics.Debug.WriteLine($"[SnowstormClient] Next searchAfter token: {currentSearchAfter}");
                            
                            // Save the token for the NEXT page (current offset = next page start)
                            if (currentOffset == offset + limit)
                            {
                                returnToken = currentSearchAfter;
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[SnowstormClient] No more results available (no searchAfter token)");
                            break;
                        }

                        // If we got fewer items than requested, we've reached the end
                        if (itemCount < limit)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SnowstormClient] Reached end of results (got {itemCount} < {limit})");
                            break;
                        }
                    }

                    // Apply offset and limit to accumulated concepts
                    var pagedConcepts = allConcepts.Skip(offset).Take(limit).ToList();
                    System.Diagnostics.Debug.WriteLine($"[SnowstormClient] Returning {pagedConcepts.Count} concepts after offset={offset}, limit={limit}");

                    // Fetch descriptions for each concept
                    await FetchDescriptionsForConcepts(pagedConcepts, list);
                    
                    return (list, returnToken);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SnowstormClient] Error: {ex.Message}");
            }

            return (list, null);
        }

        /// <summary>
        /// Helper method to fetch all descriptions for a list of concepts.
        /// </summary>
        private async Task FetchDescriptionsForConcepts(
            List<(string id, string fsn, string? pt, bool active)> concepts,
            List<SnomedConceptWithTerms> resultList)
        {
            // For each concept, fetch ALL its descriptions by concept ID
            foreach (var (idStr, fsn, pt, active) in concepts)
            {
                try
                {
                    long id = 0; _ = long.TryParse(idStr, out id);
                    if (id == 0)
                        continue;

                    // Fetch ALL descriptions for this concept using concept ID
                    var descriptionsUrl = $"{BaseUrl}/browser/MAIN/descriptions?term={idStr}&conceptActive=true&limit=100";
                    
                    using var descReq = new HttpRequestMessage(HttpMethod.Get, descriptionsUrl);
                    using var descResp = await _http.SendAsync(descReq).ConfigureAwait(false);
                    if (!descResp.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SnowstormClient] Failed to fetch descriptions for {idStr}");
                        continue;
                    }

                    using var descContent = await descResp.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    using var descDoc = await JsonDocument.ParseAsync(descContent).ConfigureAwait(false);
                    
                    if (!descDoc.RootElement.TryGetProperty("items", out var descItems) || descItems.ValueKind != JsonValueKind.Array)
                        continue;

                    var terms = new List<SnomedTerm>();
                    foreach (var descItem in descItems.EnumerateArray())
                    {
                        var descTerm = descItem.TryGetProperty("term", out var termEl) ? termEl.GetString() : null;
                        if (string.IsNullOrEmpty(descTerm))
                            continue;

                        var termType = "Synonym";
                        if (descItem.TryGetProperty("type", out var typeEl))
                        {
                            var typeStr = typeEl.GetString();
                            if (typeStr == "FSN") 
                                termType = "FSN";
                            else if (typeStr == "SYNONYM" && descItem.TryGetProperty("acceptabilityMap", out var accMap))
                            {
                                foreach (var prop in accMap.EnumerateObject())
                                {
                                    if (prop.Value.GetString() == "PREFERRED")
                                    {
                                        termType = "PT";
                                        break;
                                    }
                                }
                            }
                        }

                        terms.Add(new SnomedTerm(descTerm, termType));
                    }

                    System.Diagnostics.Debug.WriteLine($"[SnowstormClient] Concept {idStr}: {fsn} has {terms.Count} terms");
                    resultList.Add(new SnomedConceptWithTerms(id, idStr, fsn, pt, active, DateTime.UtcNow, terms));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SnowstormClient] Error fetching descriptions for {idStr}: {ex.Message}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"[SnowstormClient] Successfully fetched {resultList.Count} concepts with all their terms");
        }
    }
}
