using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Wysg.Musm.Editor.Playground.Net;

public sealed class GhostClient
{
    private readonly HttpClient _http;

    public GhostClient(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _http.Timeout = TimeSpan.FromSeconds(5);
    }

    public record StudyHeader(string? Clinical, string? PriorStudyDate);
    public record StudyInfo(string[] StudyName, string StudyDate);
    public record SuggestRequest(
        string ReportText, string PatientSex, int PatientAge,
        StudyHeader StudyHeader, StudyInfo StudyInfo,
        int TopK = 3, int MaxPerLine = 1, int LatencyBudgetMs = 800,
        string? StudyHeaderText = null);

    public record LineSuggestion(int LineIndex, string Ghost, double Confidence, string Source);
    public record ModelInfo(string Name, string Quant);
    public record SuggestResponse(List<LineSuggestion> Lines, ModelInfo Model);

    public async Task<SuggestResponse?> SuggestAsync(SuggestRequest req, CancellationToken ct)
    {
        using var resp = await _http.PostAsJsonAsync("Ghost/suggest", req, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<SuggestResponse>(cancellationToken: ct);
    }
}
