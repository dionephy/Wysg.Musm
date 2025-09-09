using System.Text.RegularExpressions;
using Wysg.Musm.GhostApi.Domain;

namespace Wysg.Musm.GhostApi.Services;

public interface IRuleEngine
{
    Task<List<Candidate>> GetCandidatesAsync(
        SuggestRequest req, List<string> lines, CancellationToken ct);
}

public sealed class RuleEngineService : IRuleEngine
{
    private static readonly Regex MicroRx = new(@"microangiopathy", RegexOptions.IgnoreCase);
    private static readonly Regex NoOtherRx = new(@"^no\s+other\s+abnormality\s*$", RegexOptions.IgnoreCase);
    private static readonly Regex NoAcuteRx = new(@"^no\s+acute\b", RegexOptions.IgnoreCase);

    public Task<List<Candidate>> GetCandidatesAsync(SuggestRequest req, List<string> lines, CancellationToken ct)
    {
        var list = new List<Candidate>();

        for (int i = 0; i < lines.Count; i++)
        {
            var raw = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(raw)) continue;

            if (MicroRx.IsMatch(raw))
            {
                list.Add(new Candidate(i,
                    "Mild degree of microangiopathy in bilateral cerebral white matter.",
                    0.78, "rule"));
            }
            if (NoOtherRx.IsMatch(raw))
            {
                list.Add(new Candidate(i,
                    "Otherwise, no acute intracranial hemorrhage, mass, or hydrocephalus.",
                    0.74, "rule"));
            }
            if (raw.Contains("old infarction", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(new Candidate(i,
                    "An old infarction in the right frontal lobe.",
                    0.70, "rule"));
            }

            if (NoAcuteRx.IsMatch(raw))
            {
                list.Add(new Candidate(i,
                    "No acute intracranial abnormality.", 0.85, "rule"));
            }
        }

        return Task.FromResult(list);
    }
}

public sealed record Candidate(int LineIndex, string Text, double Score, string Source);
