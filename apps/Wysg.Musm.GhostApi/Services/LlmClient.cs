using Wysg.Musm.GhostApi.Domain;

namespace Wysg.Musm.GhostApi.Services;

public sealed class LlmClient
{
    public ModelInfo Model { get; } = new("llama-3.1-8b-instruct-lora-musm-v1", "fp8");

    // For now: produce a single conservative alternative only for "weak" lines.
    public Task<List<Candidate>> GetCandidatesAsync(
        SuggestRequest req, List<string> lines, HashSet<int> target, CancellationToken ct)
    {
        var list = new List<Candidate>();
        foreach (var i in target)
        {
            var src = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(src)) continue;

            // Trivial “LLM-like” rewrite just so you can test merge/rank:
            string ghost = src switch
            {
                _ when src.StartsWith("no ", StringComparison.OrdinalIgnoreCase)
                    => "No acute intracranial abnormality.",
                _ when src.Length < 8
                    => $"Finding related to \"{src}\" is not clearly identified.",
                _ => char.ToUpper(src[0]) + src.Substring(1).TrimEnd('.') + "."
            };

            list.Add(new Candidate(i, ghost, 0.68, "llm"));
        }
        return Task.FromResult(list);
    }
}
