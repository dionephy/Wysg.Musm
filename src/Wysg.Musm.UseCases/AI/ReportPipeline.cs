using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Wysg.Musm.Domain.AI;

namespace Wysg.Musm.UseCases.AI;

// Orchestrator with resilience + telemetry wrappers (initial hardening step T305)
public sealed class ReportPipeline : IReportPipeline
{
    private readonly IStudyRemarkParser _studyRemark;
    private readonly IPatientRemarkParser _patientRemark;
    private readonly IConclusionGenerator _conclusionGen;
    private readonly IProofreader _proofreader;
    private readonly IInferenceTelemetry _telemetry;

    public ReportPipeline(
        IStudyRemarkParser studyRemark,
        IPatientRemarkParser patientRemark,
        IConclusionGenerator conclusionGen,
        IProofreader proofreader,
        IInferenceTelemetry telemetry)
    {
        _studyRemark = studyRemark;
        _patientRemark = patientRemark;
        _conclusionGen = conclusionGen;
        _proofreader = proofreader;
        _telemetry = telemetry;
    }

    public async Task<ReportState> RunCurrentStudyIntakeAsync(CurrentStudyContext ctx, CancellationToken ct = default)
    {
        var studyRes = new StudyRemarkResult("", "");
        if (!string.IsNullOrWhiteSpace(ctx.StudyRemark))
        {
            studyRes = await RunSkillAsync("study_remark_parser", () => _studyRemark.ParseAsync(new(ctx.StudyRemark!, ctx.StudyName, ctx.PatientInfo), ct), r => (ctx.StudyRemark!.Length, (r.ChiefComplaint + r.HistoryPreview).Length), new StudyRemarkResult("", ""), ct);
        }

        var patientRes = new PatientRemarkResult("");
        if (!string.IsNullOrWhiteSpace(ctx.PatientRemark))
        {
            patientRes = await RunSkillAsync("patient_remark_parser", () => _patientRemark.ParseAsync(new(ctx.PatientRemark!, ctx.StudyName, ctx.PatientInfo, studyRes.HistoryPreview), ct), r => (ctx.PatientRemark!.Length, r.History.Length), new PatientRemarkResult(""), ct);
        }

        var state = new ReportState(
            ctx.AccountId,
            Technique: "",
            ChiefComplaint: studyRes.ChiefComplaint,
            HistoryPreview: studyRes.HistoryPreview,
            ChiefComplaintProofread: "",
            History: patientRes.History,
            HistoryProofread: "",
            HeaderAndFindings: "",
            Conclusion: "",
            SplitIndex: null,
            Comparison: "",
            TechniqueProofread: "",
            ComparisonProofread: "",
            FindingsProofread: "",
            ConclusionProofread: "",
            Findings: "",
            ConclusionPreview: ""
        );

        return state;
    }

    public async Task<ReportState> PostProcessAsync(ReportState state, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(state.Findings) && string.IsNullOrWhiteSpace(state.ConclusionPreview))
        {
            var preview = await RunSkillAsync("conclusion_generator", () => _conclusionGen.GenerateConclusionAsync(state.Findings, ct), s => (state.Findings.Length, s.Length), string.Empty, ct);
            if (!string.IsNullOrEmpty(preview)) state = state.With(ConclusionPreview: preview);
        }

        if (!string.IsNullOrWhiteSpace(state.ChiefComplaint) && string.IsNullOrWhiteSpace(state.ChiefComplaintProofread))
        {
            var cc = await RunSkillAsync("proofreader_chiefcomplaint", () => _proofreader.ProofreadAsync(state.ChiefComplaint, ReportSectionType.ChiefComplaint, ct), s => (state.ChiefComplaint.Length, s.Length), string.Empty, ct);
            if (!string.IsNullOrEmpty(cc)) state = state.With(ChiefComplaintProofread: cc);
        }

        if (!string.IsNullOrWhiteSpace(state.History) && string.IsNullOrWhiteSpace(state.HistoryProofread))
        {
            var hist = await RunSkillAsync("proofreader_history", () => _proofreader.ProofreadAsync(state.History, ReportSectionType.History, ct), s => (state.History.Length, s.Length), string.Empty, ct);
            if (!string.IsNullOrEmpty(hist)) state = state.With(HistoryProofread: hist);
        }

        return state;
    }

    private async Task<T> RunSkillAsync<T>(string skill, Func<Task<T>> action, System.Func<T, (int inSize, int outSize)> sizeSelector, T fallback, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await action();
            sw.Stop();
            var (inSz, outSz) = sizeSelector(result);
            _telemetry.Record(skill, model: "n/a", durationMs: sw.ElapsedMilliseconds, inTokens: inSz, outTokens: outSz, success: true, error: null);
            return result;
        }
        catch (System.OperationCanceledException)
        {
            sw.Stop();
            _telemetry.Record(skill, model: "n/a", durationMs: sw.ElapsedMilliseconds, inTokens: null, outTokens: null, success: false, error: "canceled");
            throw;
        }
        catch (System.Exception ex)
        {
            sw.Stop();
            _telemetry.Record(skill, model: "n/a", durationMs: sw.ElapsedMilliseconds, inTokens: null, outTokens: null, success: false, error: ex.GetType().Name);
            return fallback; // non-fatal: upstream keeps previously populated fields
        }
    }
}
