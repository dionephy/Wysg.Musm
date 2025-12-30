using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Wysg.Musm.LlamaClient.Models;

namespace Wysg.Musm.LlamaClient.Services;

/// <summary>
/// Service for managing MCP (Model Context Protocol) servers and tools.
/// This is a placeholder for future MCP implementation.
/// </summary>
public class McpService : IDisposable
{
    private readonly Dictionary<string, Process> _serverProcesses = [];
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly HttpClient _httpClient;

    public List<McpServerConfig> Servers { get; } = [];
    public List<McpTool> AllTools { get; } = [];

    public event EventHandler<McpServerConfig>? ServerStatusChanged;
    public event EventHandler<List<McpTool>>? ToolsUpdated;

    public McpService()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        // Register a built-in fetch tool for immediate use
        RegisterBuiltInFetchTool();
    }

    private void RegisterBuiltInFetchTool()
    {
        if (AllTools.Any(t => t.Name == "fetch"))
            return;

        var fetchTool = new McpTool
        {
            Name = "fetch",
            Description = "Perform HTTP requests (GET/POST/PUT/PATCH/DELETE)",
            ServerName = "local",
            IsEnabled = true,
            InputSchema = new McpToolInputSchema
            {
                Type = "object",
                Properties = new Dictionary<string, McpToolProperty>
                {
                    ["url"] = new() { Type = "string", Description = "Absolute URL to request" },
                    ["method"] = new() { Type = "string", Description = "HTTP method (GET,POST,PUT,PATCH,DELETE)" },
                    ["headers"] = new() { Type = "object", Description = "Optional headers as key/value" },
                    ["body"] = new() { Type = "string", Description = "Optional request body (for non-GET)" }
                },
                Required = ["url"]
            }
        };

        AllTools.Add(fetchTool);
        ToolsUpdated?.Invoke(this, AllTools);
    }

    /// <summary>
    /// Loads MCP configuration from file.
    /// </summary>
    public async Task LoadConfigurationAsync(string configPath)
    {
        if (!File.Exists(configPath))
            return;

        try
        {
            var json = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<McpConfiguration>(json, _jsonOptions);
            if (config?.Servers != null)
            {
                Servers.Clear();
                Servers.AddRange(config.Servers);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load MCP config: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves MCP configuration to file.
    /// </summary>
    public async Task SaveConfigurationAsync(string configPath)
    {
        var config = new McpConfiguration { Servers = Servers };
        var json = JsonSerializer.Serialize(config, _jsonOptions);
        await File.WriteAllTextAsync(configPath, json);
    }

    /// <summary>
    /// Connects to an MCP server (stdio transport).
    /// </summary>
    public async Task ConnectServerAsync(McpServerConfig server)
    {
        if (string.IsNullOrEmpty(server.Command))
            return;

        server.Status = McpServerStatus.Connecting;
        ServerStatusChanged?.Invoke(this, server);

        try
        {
            await Task.Delay(100); // Placeholder

            server.Status = McpServerStatus.Connected;
            server.Tools = GetMockTools(server.Name);

            UpdateAllTools();
            ServerStatusChanged?.Invoke(this, server);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to connect to MCP server {server.Name}: {ex.Message}");
            server.Status = McpServerStatus.Error;
            ServerStatusChanged?.Invoke(this, server);
        }
    }

    /// <summary>
    /// Disconnects from an MCP server.
    /// </summary>
    public void DisconnectServer(McpServerConfig server)
    {
        if (_serverProcesses.TryGetValue(server.Name, out var process))
        {
            try
            {
                process.Kill();
                process.Dispose();
            }
            catch { }

            _serverProcesses.Remove(server.Name);
        }

        server.Status = McpServerStatus.Disconnected;
        server.Tools.Clear();
        UpdateAllTools();
        ServerStatusChanged?.Invoke(this, server);
    }

    /// <summary>
    /// Executes a tool call. Currently supports built-in "fetch".
    /// </summary>
    public async Task<McpToolResult> ExecuteToolAsync(string toolName, string arguments)
    {
        if (string.Equals(toolName, "fetch", StringComparison.OrdinalIgnoreCase))
        {
            return await ExecuteFetchAsync(arguments);
        }

        // TODO: Implement actual MCP protocol execution for external tools
        await Task.Delay(100);
        return new McpToolResult
        {
            ToolName = toolName,
            Success = true,
            Result = $"{{\"result\": \"Tool {toolName} executed with arguments: {arguments}\"}}"
        };
    }

    private sealed class FetchArgs
    {
        public string? Url { get; set; }
        public string? Method { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
        public string? Body { get; set; }
    }

    private async Task<McpToolResult> ExecuteFetchAsync(string arguments)
    {
        FetchArgs? args = null;
        try
        {
            args = JsonSerializer.Deserialize<FetchArgs>(arguments, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            return new McpToolResult
            {
                ToolName = "fetch",
                Success = false,
                Error = $"Invalid arguments: {ex.Message}",
                Result = string.Empty
            };
        }

        if (string.IsNullOrWhiteSpace(args?.Url))
        {
            return new McpToolResult
            {
                ToolName = "fetch",
                Success = false,
                Error = "url is required",
                Result = string.Empty
            };
        }

        var method = string.IsNullOrWhiteSpace(args.Method) ? HttpMethod.Get : new HttpMethod(args.Method.ToUpperInvariant());
        using var request = new HttpRequestMessage(method, args.Url);

        if (args.Headers != null)
        {
            foreach (var kvp in args.Headers)
            {
                if (!request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value))
                {
                    request.Content ??= new StringContent(string.Empty);
                    request.Content.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                }
            }
        }

        if (!string.Equals(method.Method, HttpMethod.Get.Method, StringComparison.OrdinalIgnoreCase) && args.Body != null)
        {
            request.Content = new StringContent(args.Body, Encoding.UTF8, "application/json");
        }

        try
        {
            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            var resultJson = JsonSerializer.Serialize(new
            {
                status = (int)response.StatusCode,
                reason = response.ReasonPhrase,
                body
            }, _jsonOptions);

            return new McpToolResult
            {
                ToolName = "fetch",
                Success = response.IsSuccessStatusCode,
                Result = resultJson,
                Error = response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new McpToolResult
            {
                ToolName = "fetch",
                Success = false,
                Error = ex.Message,
                Result = string.Empty
            };
        }
    }

    /// <summary>
    /// Gets tools formatted for OpenAI API.
    /// </summary>
    public List<Tool> GetOpenAiTools()
    {
        return AllTools
            .Where(t => t.IsEnabled)
            .Select(t => t.ToOpenAiTool())
            .ToList();
    }

    private void UpdateAllTools()
    {
        AllTools.Clear();
        foreach (var server in Servers.Where(s => s.Status == McpServerStatus.Connected))
        {
            AllTools.AddRange(server.Tools);
        }

        // Ensure built-in fetch tool is always present
        RegisterBuiltInFetchTool();

        ToolsUpdated?.Invoke(this, AllTools);
    }

    private static List<McpTool> GetMockTools(string serverName)
    {
        // Mock tools for demonstration
        return
        [
            new McpTool
            {
                Name = $"{serverName}_search",
                Description = $"Search using {serverName}",
                ServerName = serverName,
                InputSchema = new McpToolInputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, McpToolProperty>
                    {
                        ["query"] = new() { Type = "string", Description = "Search query" }
                    },
                    Required = ["query"]
                }
            },
            new McpTool
            {
                Name = $"{serverName}_get",
                Description = $"Get data from {serverName}",
                ServerName = serverName,
                InputSchema = new McpToolInputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, McpToolProperty>
                    {
                        ["id"] = new() { Type = "string", Description = "Resource ID" }
                    },
                    Required = ["id"]
                }
            }
        ];
    }

    public void Dispose()
    {
        foreach (var process in _serverProcesses.Values)
        {
            try
            {
                process.Kill();
                process.Dispose();
            }
            catch { }
        }
        _serverProcesses.Clear();
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
