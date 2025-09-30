using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Wysg.Musm.Domain.AI;

// Core low-level generic client (raw model invocation abstraction)
public interface ILLMClient
{
    Task<LlmResponse> SendAsync(LlmRequest request, CancellationToken ct = default);
}

public sealed record LlmRequest(
    string Model,
    string SystemPrompt,
    string UserPrompt,
    float Temperature = 0.2f,
    int? MaxTokens = null,
    bool ExpectJsonObject = false
);

public sealed record LlmResponse(string RawText);

// Routing abstraction (decides model id / parameters per skill)
public interface IModelRouter
{
    LlmRequest BuildRequest(string skill, string userPrompt, ModelRoutingOptions? options = null);
}

public sealed record ModelRoutingOptions(float? Temperature = null, int? MaxTokens = null);

// Simple text generation (free form)
public interface ITextGeneration
{
    Task<string> GenerateAsync(string prompt, CancellationToken ct = default);
}

// Structured extraction (request -> strongly typed response via JSON contract)
public interface IStructuredExtraction<TRequest, TResponse>
{
    Task<TResponse> ExtractAsync(TRequest input, CancellationToken ct = default);
}

public enum ReportSectionType
{
    ChiefComplaint,
    HistoryPreview,
    History,
    Technique,
    Comparison,
    Findings,
    Conclusion
}

public interface IProofreader
{
    Task<string> ProofreadAsync(string text, ReportSectionType section, CancellationToken ct = default);
}

public interface IHeaderSplitter
{
    Task<HeaderSplitResult> SplitAsync(string headerAndFindings, CancellationToken ct = default);
}

public interface IHeaderParser
{
    Task<HeaderParseResult> ParseAsync(string headerPortion, CancellationToken ct = default);
}

public interface IConclusionGenerator
{
    Task<string> GenerateConclusionAsync(string findings, CancellationToken ct = default);
}

public interface IStudyRemarkParser
{
    Task<StudyRemarkResult> ParseAsync(StudyRemarkInput input, CancellationToken ct = default);
}

public interface IPatientRemarkParser
{
    Task<PatientRemarkResult> ParseAsync(PatientRemarkInput input, CancellationToken ct = default);
}

// Telemetry (implemented in Infrastructure) for observability
public interface IInferenceTelemetry
{
    void Record(string skill, string model, long durationMs, int? inTokens, int? outTokens, bool success, string? error = null);
}

// Report pipeline orchestrator (high-level multi-stage)
public interface IReportPipeline
{
    Task<ReportState> RunCurrentStudyIntakeAsync(CurrentStudyContext ctx, CancellationToken ct = default);
    Task<ReportState> PostProcessAsync(ReportState state, CancellationToken ct = default);
}

// ==== Data contracts (records) ====

public sealed record StudyRemarkInput(string StudyRemark, string StudyName, string PatientInfo);
public sealed record StudyRemarkResult(string ChiefComplaint, string HistoryPreview);

public sealed record PatientRemarkInput(string PatientRemark, string StudyName, string PatientInfo, string HistoryPreview);
public sealed record PatientRemarkResult(string History);

public sealed record HeaderSplitResult(int SplitIndex, string HeaderPortion, string FindingsPortion);

public sealed record HeaderParseResult(
    string ChiefComplaint,
    string History,
    string Technique,
    string Comparison);

public sealed record ReportState(
    long AccountId,
    string Technique,
    string ChiefComplaint,
    string HistoryPreview,
    string ChiefComplaintProofread,
    string History,
    string HistoryProofread,
    string HeaderAndFindings,
    string Conclusion,
    int? SplitIndex,
    string Comparison,
    string TechniqueProofread,
    string ComparisonProofread,
    string FindingsProofread,
    string ConclusionProofread,
    string Findings,
    string ConclusionPreview
)
{
    public ReportState With(
        string? Technique = null,
        string? ChiefComplaint = null,
        string? HistoryPreview = null,
        string? ChiefComplaintProofread = null,
        string? History = null,
        string? HistoryProofread = null,
        string? HeaderAndFindings = null,
        string? Conclusion = null,
        int? SplitIndex = null,
        string? Comparison = null,
        string? TechniqueProofread = null,
        string? ComparisonProofread = null,
        string? FindingsProofread = null,
        string? ConclusionProofread = null,
        string? Findings = null,
        string? ConclusionPreview = null
    ) => this with
    {
        Technique = Technique ?? this.Technique,
        ChiefComplaint = ChiefComplaint ?? this.ChiefComplaint,
        HistoryPreview = HistoryPreview ?? this.HistoryPreview,
        ChiefComplaintProofread = ChiefComplaintProofread ?? this.ChiefComplaintProofread,
        History = History ?? this.History,
        HistoryProofread = HistoryProofread ?? this.HistoryProofread,
        HeaderAndFindings = HeaderAndFindings ?? this.HeaderAndFindings,
        Conclusion = Conclusion ?? this.Conclusion,
        SplitIndex = SplitIndex ?? this.SplitIndex,
        Comparison = Comparison ?? this.Comparison,
        TechniqueProofread = TechniqueProofread ?? this.TechniqueProofread,
        ComparisonProofread = ComparisonProofread ?? this.ComparisonProofread,
        FindingsProofread = FindingsProofread ?? this.FindingsProofread,
        ConclusionProofread = ConclusionProofread ?? this.ConclusionProofread,
        Findings = Findings ?? this.Findings,
        ConclusionPreview = ConclusionPreview ?? this.ConclusionPreview
    };
}

public sealed record CurrentStudyContext(long AccountId, string StudyName, string PatientInfo, string? StudyRemark, string? PatientRemark);
