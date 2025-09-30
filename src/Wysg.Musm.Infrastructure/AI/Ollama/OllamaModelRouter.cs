using Wysg.Musm.Domain.AI;

namespace Wysg.Musm.Infrastructure.AI.Ollama;

public sealed class OllamaModelRouter : IModelRouter
{
    private readonly OllamaOptions _opts;
    public OllamaModelRouter(OllamaOptions opts) => _opts = opts;

    public LlmRequest BuildRequest(string skill, string userPrompt, ModelRoutingOptions? options = null)
    {
        float temp = options?.Temperature ?? (skill.Contains("proofread") ? 0.1f : _opts.DefaultTemperature);
        int? max = options?.MaxTokens ?? _opts.DefaultMaxTokens;
        var system = skill switch
        {
            "study_remark_parser" => "Extract concise chief complaint and history preview as JSON.",
            "patient_remark_parser" => "Extract concise patient history as JSON.",
            "conclusion_generator" => "Write a radiology impression (no patient identifiers).",
            _ when skill.StartsWith("proofreader") => "Correct grammar; do not alter medical meaning.",
            _ => "You are a helpful assistant."
        };
        return new LlmRequest(
            Model: _opts.DefaultModel,
            SystemPrompt: system,
            UserPrompt: userPrompt,
            Temperature: temp,
            MaxTokens: max,
            ExpectJsonObject: skill.Contains("parser")
        );
    }
}
