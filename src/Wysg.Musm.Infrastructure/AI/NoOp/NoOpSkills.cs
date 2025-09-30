using System.Threading;
using System.Threading.Tasks;
using Wysg.Musm.Domain.AI;

namespace Wysg.Musm.Infrastructure.AI.NoOp;

// Minimal placeholder implementations (return inputs) until real LLM & RBM providers are added.
public sealed class NoOpStudyRemarkParser : IStudyRemarkParser
{
    public Task<StudyRemarkResult> ParseAsync(StudyRemarkInput input, CancellationToken ct = default) =>
        Task.FromResult(new StudyRemarkResult("", ""));
}

public sealed class NoOpPatientRemarkParser : IPatientRemarkParser
{
    public Task<PatientRemarkResult> ParseAsync(PatientRemarkInput input, CancellationToken ct = default) =>
        Task.FromResult(new PatientRemarkResult(""));
}

public sealed class NoOpConclusionGenerator : IConclusionGenerator
{
    public Task<string> GenerateConclusionAsync(string findings, CancellationToken ct = default) => Task.FromResult("");
}

public sealed class NoOpProofreader : IProofreader
{
    public Task<string> ProofreadAsync(string text, ReportSectionType section, CancellationToken ct = default) => Task.FromResult(text);
}

public sealed class NoOpLlmClient : ILLMClient
{
    public Task<LlmResponse> SendAsync(LlmRequest request, CancellationToken ct = default) => Task.FromResult(new LlmResponse(""));
}

public sealed class NoOpModelRouter : IModelRouter
{
    public LlmRequest BuildRequest(string skill, string userPrompt, ModelRoutingOptions? options = null) =>
        new LlmRequest("noop-model", "", userPrompt, Temperature: options?.Temperature ?? 0, MaxTokens: options?.MaxTokens);
}

public sealed class NoOpInferenceTelemetry : IInferenceTelemetry
{
    public void Record(string skill, string model, long durationMs, int? inTokens, int? outTokens, bool success, string? error = null) { }
}
