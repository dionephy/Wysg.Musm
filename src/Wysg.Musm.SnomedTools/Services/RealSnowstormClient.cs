using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using Wysg.Musm.SnomedTools.Abstractions;

namespace Wysg.Musm.SnomedTools.Services
{
    /// <summary>
    /// Real SNOMED CT Snowstorm client implementation for standalone app.
    /// </summary>
    public sealed class RealSnowstormClient : ISnowstormClient
    {
        private readonly SnomedToolsLocalSettings _settings;
        private readonly HttpClient _http;

        public RealSnowstormClient(SnomedToolsLocalSettings settings)
     {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
       _http = new HttpClient();
     }

        private string BaseUrl
            => string.IsNullOrWhiteSpace(_settings.SnowstormBaseUrl)
           ? "https://snowstorm.ihtsdotools.org/snowstorm"
   : _settings.SnowstormBaseUrl.TrimEnd('/');

        public async Task<(IReadOnlyList<SnomedConceptWithTerms> concepts, string? nextSearchAfter)> BrowseBySemanticTagAsync(
            string semanticTag,
      int offset = 0,
          int limit = 10,
    string? searchAfterToken = null)
        {
 var list = new List<SnomedConceptWithTerms>();
      string? returnToken = null;

            // Use ECL (Expression Constraint Language) queries
    string eclQuery;
   switch (semanticTag.ToLowerInvariant())
            {
         case "all":
  eclQuery = "<<138875005"; // SNOMED CT Concept
       break;
 case "body structure":
   eclQuery = "<<123037004";
              break;
       case "finding":
     eclQuery = "<<404684003";
 break;
          case "disorder":
             eclQuery = "<<64572001";
        break;
       case "procedure":
        eclQuery = "<<71388002";
    break;
                case "observable entity":
eclQuery = "<<363787002";
                 break;
             case "substance":
         eclQuery = "<<105590001";
   break;
      default:
          System.Diagnostics.Debug.WriteLine($"[RealSnowstormClient] Unknown domain: {semanticTag}, using 'all'");
        eclQuery = "<<138875005";
         break;
          }

            System.Diagnostics.Debug.WriteLine($"[RealSnowstormClient] Fetching domain '{semanticTag}' using ECL: {eclQuery}");
    System.Diagnostics.Debug.WriteLine($"[RealSnowstormClient] Requested offset={offset}, limit={limit}");

       try
    {
           var url = $"{BaseUrl}/MAIN/concepts?ecl={Uri.EscapeDataString(eclQuery)}&limit={limit}&activeFilter=true";
       if (!string.IsNullOrEmpty(searchAfterToken))
     {
      url += $"&searchAfter={Uri.EscapeDataString(searchAfterToken)}";
                }

            System.Diagnostics.Debug.WriteLine($"[RealSnowstormClient] Fetching: {url}");

 using var req = new HttpRequestMessage(HttpMethod.Get, url);
     using var resp = await _http.SendAsync(req).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
   System.Diagnostics.Debug.WriteLine($"[RealSnowstormClient] HTTP {resp.StatusCode}: {resp.ReasonPhrase}");
             return (list, null);
                }

        using var content = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
    using var doc = await JsonDocument.ParseAsync(content).ConfigureAwait(false);

  if (!doc.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
{
             System.Diagnostics.Debug.WriteLine("[RealSnowstormClient] No 'items' array in response");
   return (list, null);
   }

     var concepts = new List<(string id, string fsn, string? pt, bool active)>();
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

       concepts.Add((idStr, fsn, pt, active));
      }
              catch (Exception ex)
          {
            System.Diagnostics.Debug.WriteLine($"[RealSnowstormClient] Error processing concept: {ex.Message}");
                    }
     }

    // Get next searchAfter token
      if (doc.RootElement.TryGetProperty("searchAfter", out var searchAfterEl))
   {
            returnToken = searchAfterEl.GetString();
System.Diagnostics.Debug.WriteLine($"[RealSnowstormClient] Next searchAfter token: {returnToken}");
                }

       System.Diagnostics.Debug.WriteLine($"[RealSnowstormClient] Fetched {concepts.Count} concepts");

           // Fetch descriptions for each concept
       foreach (var (idStr, fsn, pt, active) in concepts)
                {
           try
    {
           long id = 0;
         _ = long.TryParse(idStr, out id);
   if (id == 0)
            continue;

   var descriptionsUrl = $"{BaseUrl}/browser/MAIN/descriptions?term={idStr}&conceptActive=true&limit=100";

         using var descReq = new HttpRequestMessage(HttpMethod.Get, descriptionsUrl);
    using var descResp = await _http.SendAsync(descReq).ConfigureAwait(false);
if (!descResp.IsSuccessStatusCode)
       {
                System.Diagnostics.Debug.WriteLine($"[RealSnowstormClient] Failed to fetch descriptions for {idStr}");
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

     System.Diagnostics.Debug.WriteLine($"[RealSnowstormClient] Concept {idStr}: {fsn} has {terms.Count} terms");
       list.Add(new SnomedConceptWithTerms(id, idStr, fsn, pt, active, DateTime.UtcNow, terms));
          }
  catch (Exception ex)
          {
       System.Diagnostics.Debug.WriteLine($"[RealSnowstormClient] Error fetching descriptions for {idStr}: {ex.Message}");
      }
         }

            System.Diagnostics.Debug.WriteLine($"[RealSnowstormClient] Successfully fetched {list.Count} concepts with all their terms");
       }
            catch (Exception ex)
     {
            System.Diagnostics.Debug.WriteLine($"[RealSnowstormClient] Error: {ex.Message}");
            }

         return (list, returnToken);
   }
    }
}
