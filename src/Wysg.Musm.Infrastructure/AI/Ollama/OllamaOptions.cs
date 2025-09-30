namespace Wysg.Musm.Infrastructure.AI.Ollama;

public sealed class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434"; // default daemon
    public string DefaultModel { get; set; } = "llama3"; // adjust as needed
    public float DefaultTemperature { get; set; } = 0.2f;
    public int? DefaultMaxTokens { get; set; } = null; // let model decide if null
    public int MaxParallel { get; set; } = 2; // concurrency cap per model
    public int QueueTimeoutMs { get; set; } = 4000; // fail fast if saturated
}
