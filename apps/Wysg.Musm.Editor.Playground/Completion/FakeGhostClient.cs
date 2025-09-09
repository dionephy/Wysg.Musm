using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wysg.Musm.Editor.Ghosting;

namespace Wysg.Musm.Editor.Playground.Completion;

public sealed class FakeGhostClient : IGhostSuggestionClient
{
    public async Task<GhostResponse> SuggestAsync(GhostRequest request, CancellationToken ct)
    {
        await Task.Delay(300, ct); // simulate network/compute

        var lines = request.ReportText.Replace("\r", "").Split('\n');
        var suggestions = lines
            .Select((line, idx) =>
            {
                var t = line.Trim();
                if (string.IsNullOrEmpty(t)) return null;

                if (t.Contains("microangiopathy", System.StringComparison.OrdinalIgnoreCase))
                    return new GhostSuggestion(idx + 1, "Mild degree of microangiopathy in the bilateral cerebral white matters.");
                if (t.Contains("old infarction", System.StringComparison.OrdinalIgnoreCase))
                    return new GhostSuggestion(idx + 1, "An old infarction in the right frontal lobe.");
                //if (t.Contains("no other abnormality", System.StringComparison.OrdinalIgnoreCase))
                 //   return new GhostSuggestion(idx + 1, "Otherwise, no evidence of hemorrhage, mass, hydrocephalus, or significant atrophy.");
                // default paraphrase:
                return new GhostSuggestion(idx + 1, t[..System.Math.Min(40, t.Length)] + " ...");
            })
            .Where(x => x is not null)!
            .ToList()!;
        return new GhostResponse(suggestions);
    }
}
