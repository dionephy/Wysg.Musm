using System.Text.Json.Serialization;

namespace Wysg.Musm.LlamaClient.Models;

/// <summary>
/// Represents an MCP (Model Context Protocol) tool definition.
/// </summary>
public class McpTool
{
    /// <summary>
    /// Unique name of the tool.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of what the tool does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// JSON schema for the tool's input parameters.
    /// </summary>
    public McpToolInputSchema? InputSchema { get; set; }

    /// <summary>
    /// Whether the tool is enabled for use.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// The MCP server this tool belongs to.
    /// </summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// Converts to OpenAI-compatible tool format.
    /// </summary>
    public Tool ToOpenAiTool()
    {
        return new Tool
        {
            Type = "function",
            Function = new FunctionDefinition
            {
                Name = Name,
                Description = Description,
                Parameters = InputSchema?.ToJsonObject()
            }
        };
    }
}

/// <summary>
/// JSON Schema for MCP tool input.
/// </summary>
public class McpToolInputSchema
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    [JsonPropertyName("properties")]
    public Dictionary<string, McpToolProperty> Properties { get; set; } = [];

    [JsonPropertyName("required")]
    public List<string> Required { get; set; } = [];

    public object ToJsonObject()
    {
        return new
        {
            type = Type,
            properties = Properties.ToDictionary(
                p => p.Key,
                p => new { type = p.Value.Type, description = p.Value.Description }
            ),
            required = Required
        };
    }
}

/// <summary>
/// Property definition in MCP tool schema.
/// </summary>
public class McpToolProperty
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// MCP server configuration.
/// </summary>
public class McpServerConfig
{
    /// <summary>
    /// Display name for the server.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of transport (stdio, sse, etc.).
    /// </summary>
    public string Transport { get; set; } = "stdio";

    /// <summary>
    /// Command to execute for stdio transport.
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Arguments for the command.
    /// </summary>
    public List<string> Args { get; set; } = [];

    /// <summary>
    /// Environment variables to set.
    /// </summary>
    public Dictionary<string, string> Env { get; set; } = [];

    /// <summary>
    /// Whether the server is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Connection status.
    /// </summary>
    [JsonIgnore]
    public McpServerStatus Status { get; set; } = McpServerStatus.Disconnected;

    /// <summary>
    /// Available tools from this server.
    /// </summary>
    [JsonIgnore]
    public List<McpTool> Tools { get; set; } = [];

    [JsonIgnore]
    public string ArgsText
    {
        get => string.Join(" ", Args);
        set
        {
            Args = string.IsNullOrWhiteSpace(value)
                ? []
                : value.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }

    [JsonIgnore]
    public string EnvText
    {
        get => string.Join("\n", Env.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        set
        {
            Env.Clear();
            if (string.IsNullOrWhiteSpace(value)) return;
            var lines = value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split('=', 2, StringSplitOptions.TrimEntries);
                if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]))
                {
                    Env[parts[0]] = parts[1];
                }
            }
        }
    }
}

/// <summary>
/// MCP server connection status.
/// </summary>
public enum McpServerStatus
{
    Disconnected,
    Connecting,
    Connected,
    Error
}

/// <summary>
/// Container for MCP configuration.
/// </summary>
public class McpConfiguration
{
    [JsonPropertyName("servers")]
    public List<McpServerConfig> Servers { get; set; } = [];
}

/// <summary>
/// MCP tool execution result.
/// </summary>
public class McpToolResult
{
    public string ToolCallId { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Result { get; set; } = string.Empty;
    public string? Error { get; set; }
}

public class McpServerEntry
{
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    public List<string> Args { get; set; } = [];

    [JsonPropertyName("env")]
    public Dictionary<string, string>? Env { get; set; }
}

public class McpConfigFile
{
    [JsonPropertyName("mcp")]
    public McpConfigRoot? Mcp { get; set; }
}

public class McpConfigRoot
{
    [JsonPropertyName("servers")]
    public Dictionary<string, McpServerEntry> Servers { get; set; } = new();
}
