using Wysg.Musm.GhostApi.Domain;

namespace Wysg.Musm.GhostApi.Services;

public sealed class Ranker
{
    public List<LineSuggestion> Merge(
        IEnumerable<Candidate> ruleCands,
        IEnumerable<Candidate> llmCands,
        SuggestRequest req,
        int lineCount,
        int maxPerLine)
    {
        var result = new List<LineSuggestion>();

        var byLine = ruleCands.Concat(llmCands)
            .GroupBy(c => c.LineIndex);

        foreach (var g in byLine)
        {
            var picked = g
                .OrderByDescending(c => c.Score)
                .ThenBy(c => c.Source == "rule" ? 0 : 1) // prefer rule tie-break
                .Take(maxPerLine);

            foreach (var p in picked)
            {
                result.Add(new LineSuggestion(
                    LineIndex: p.LineIndex,
                    Ghost: p.Text,
                    Confidence: Math.Round(p.Score, 2),
                    Source: p.Source));
            }
        }

        // Keep results sorted by line index
        result.Sort((a, b) => a.LineIndex.CompareTo(b.LineIndex));
        return result;
    }
}
