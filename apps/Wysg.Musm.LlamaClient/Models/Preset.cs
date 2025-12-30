using System.Text.Json.Serialization;

namespace Wysg.Musm.LlamaClient.Models;

/// <summary>
/// Represents a saved preset configuration for the Llama client.
/// </summary>
public class Preset
{
    /// <summary>
    /// Unique identifier for the preset.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Display name for the preset.
    /// </summary>
    public string Name { get; set; } = "New Preset";

    /// <summary>
    /// The API endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = "http://127.0.0.1:8000";

    /// <summary>
    /// The model identifier.
    /// </summary>
    public string Model { get; set; } = "nvidia/Llama-3.3-70B-Instruct-FP4";

    /// <summary>
    /// System prompt for the chat.
    /// </summary>
    public string SystemPrompt { get; set; } = "You are a helpful assistant.";

    /// <summary>
    /// Temperature parameter (0.0 - 2.0).
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Top-p (nucleus sampling) parameter (0.0 - 1.0).
    /// </summary>
    public double TopP { get; set; } = 1.0;

    /// <summary>
    /// Maximum tokens to generate.
    /// </summary>
    public int MaxTokens { get; set; } = 2048;

    /// <summary>
    /// Stop sequences (comma-separated in UI).
    /// </summary>
    public List<string> StopSequences { get; set; } = [];

    /// <summary>
    /// Whether to use streaming output.
    /// </summary>
    public bool UseStreaming { get; set; } = true;

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a default preset with standard settings.
    /// </summary>
    public static Preset CreateDefault()
    {
        return new Preset
        {
            Name = "Default",
            Endpoint = "http://127.0.0.1:8000",
            Model = "nvidia/Llama-3.3-70B-Instruct-FP4",
            SystemPrompt = "You are a helpful assistant.",
            Temperature = 0.7,
            TopP = 1.0,
            MaxTokens = 2048,
            UseStreaming = true
        };
    }

    /// <summary>
    /// Creates a clone of this preset with a new ID.
    /// </summary>
    public Preset Clone()
    {
        return new Preset
        {
            Name = $"{Name} (Copy)",
            Endpoint = Endpoint,
            Model = Model,
            SystemPrompt = SystemPrompt,
            Temperature = Temperature,
            TopP = TopP,
            MaxTokens = MaxTokens,
            StopSequences = [.. StopSequences],
            UseStreaming = UseStreaming,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Container for all presets, used for serialization.
/// </summary>
public class PresetCollection
{
    [JsonPropertyName("presets")]
    public List<Preset> Presets { get; set; } = [];

    [JsonPropertyName("activePresetId")]
    public string? ActivePresetId { get; set; }
}
