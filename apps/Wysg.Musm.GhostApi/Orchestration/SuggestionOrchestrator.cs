using Wysg.Musm.GhostApi.Domain;
using Wysg.Musm.GhostApi.Services;

namespace Wysg.Musm.GhostApi.Orchestration;

public sealed class SuggestionOrchestrator
{
    private readonly IRuleEngine _rules;
    private readonly LlmClient _llm;
    private readonly Ranker _ranker;

    public SuggestionOrchestrator(IRuleEngine rules, LlmClient llm, Ranker ranker)
    {
        _rules = rules;
        _llm = llm;
        _ranker = ranker;
    }

    public async Task<SuggestResponse> SuggestAsync(SuggestRequest req, CancellationToken ct)
    {
        var lines = SplitLines(req.ReportText);
        var ruleCands = await _rules.GetCandidatesAsync(req, lines, ct);

        var perLineMax = Enumerable.Range(0, lines.Count)
            .Select(i => ruleCands.Where(c => c.LineIndex == i).Select(c => c.Score).DefaultIfEmpty(0.0).Max())
            .ToArray();

        const double ruleStrongThreshold = 0.72;
        var weak = new HashSet<int>(Enumerable.Range(0, lines.Count).Where(i => perLineMax[i] < ruleStrongThreshold));

        var llmCands = await _llm.GetCandidatesAsync(req, lines, weak, ct);


        var merged = _ranker.Merge(ruleCands, llmCands, req, lines.Count, req.MaxPerLine);

        return new SuggestResponse(merged, _llm.Model);
    }


    private static List<string> SplitLines(string text)
        => text.Replace("\r\n", "\n").Split('\n').ToList();
}
