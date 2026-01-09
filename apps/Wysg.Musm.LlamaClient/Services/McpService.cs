using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using System.ComponentModel;
using Wysg.Musm.LlamaClient.Models;

namespace Wysg.Musm.LlamaClient.Services;

public class McpService : IDisposable
{
    private static readonly TimeSpan DefaultRequestTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan DefaultConnectTimeout = TimeSpan.FromSeconds(120);

    private readonly Dictionary<string, Process> _serverProcesses = [];
    private readonly Dictionary<string, BlockingCollection<string>> _stdoutQueues = [];
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
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        // RegisterBuiltInFetchTool(); // Temporarily disabled to test MCP fetch
    }

    /// <summary>
    /// Loads MCP configuration from file.
    /// </summary>
    public async Task LoadConfigurationAsync(string configPath)
    {
        Debug.WriteLine($"[MCP] Loading config from: {configPath}");
        if (!File.Exists(configPath))
        {
            Debug.WriteLine($"[MCP] Config file not found: {configPath}");
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(configPath);

            // Try new nested format
            var configFile = JsonSerializer.Deserialize<McpConfigFile>(json, _jsonOptions);
            if (configFile?.Mcp?.Servers != null && configFile.Mcp.Servers.Count > 0)
            {
                Servers.Clear();
                foreach (var kvp in configFile.Mcp.Servers)
                {
                    Servers.Add(new McpServerConfig
                    {
                        Name = kvp.Key,
                        Command = kvp.Value.Command,
                        Args = kvp.Value.Args ?? [],
                        Env = kvp.Value.Env ?? new(),
                        IsEnabled = true
                    });
                }
                Debug.WriteLine($"[MCP] Loaded {Servers.Count} servers (nested mcp format)");
                return;
            }

            // Fallback to legacy list format
            var legacyConfig = JsonSerializer.Deserialize<McpConfiguration>(json, _jsonOptions);
            if (legacyConfig?.Servers != null)
            {
                Servers.Clear();
                Servers.AddRange(legacyConfig.Servers);
                Debug.WriteLine($"[MCP] Loaded {legacyConfig.Servers.Count} servers (legacy list format)");
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
        var fileConfig = new McpConfigFile
        {
            Mcp = new McpConfigRoot
            {
                Servers = Servers.ToDictionary(
                    s => s.Name,
                    s => new McpServerEntry
                    {
                        Command = s.Command,
                        Args = s.Args ?? [],
                        Env = s.Env.Count == 0 ? null : new Dictionary<string, string>(s.Env)
                    })
            }
        };

        var json = JsonSerializer.Serialize(fileConfig, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
        await File.WriteAllTextAsync(configPath, json);
    }

    /// <summary>
    /// Connects to an MCP server (stdio transport).
    /// </summary>
    public async Task ConnectServerAsync(McpServerConfig server)
    {
        Debug.WriteLine($"[MCP] ConnectServerAsync start: name='{server.Name}', command='{server.Command}'");
        if (!server.IsEnabled)
        {
            Debug.WriteLine($"[MCP] Server {server.Name} disabled; skipping.");
            return;
        }

        if (string.IsNullOrWhiteSpace(server.Command))
        {
            Debug.WriteLine("[MCP] Command is empty. Skipping connection.");
            server.Status = McpServerStatus.Error;
            ServerStatusChanged?.Invoke(this, server);
            return;
        }

        server.Status = McpServerStatus.Connecting;
        ServerStatusChanged?.Invoke(this, server);

        Process? process = null;
        var stderrBuffer = new StringBuilder();
        var stdoutQueue = new BlockingCollection<string>(new ConcurrentQueue<string>());
        using var connectCts = new CancellationTokenSource(DefaultConnectTimeout);

        try
        {
            await Task.Delay(100, connectCts.Token);
            var startInfo = CreateProcessStartInfo(server);

            process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            var fullCommand = $"{startInfo.FileName} {string.Join(" ", startInfo.ArgumentList)}";
            Debug.WriteLine($"[MCP] Starting process: {fullCommand}");

            try
            {
                process.Start();
            }
            catch (Win32Exception w32)
            {
                Debug.WriteLine($"[MCP] Failed to start process for {server.Name}: {w32.Message}");
                server.Status = McpServerStatus.Error;
                ServerStatusChanged?.Invoke(this, server);
                return;
            }

            process.StandardInput.AutoFlush = true;

            Debug.WriteLine($"[MCP] Process started, ID: {process.Id}, HasExited: {process.HasExited}");
            _serverProcesses[server.Name] = process;
            _stdoutQueues[server.Name] = stdoutQueue;

            // Use synchronous reading in dedicated threads - more reliable for subprocess I/O
            var stdoutThread = new Thread(() =>
            {
                Debug.WriteLine("[MCP] Started stdout reader thread");
                try
                {
                    using var reader = process.StandardOutput;
                    while (true)
                    {
                        var line = reader.ReadLine();
                        if (line == null)
                        {
                            Debug.WriteLine("[MCP] STDOUT: EOF reached");
                            break;
                        }
                        Debug.WriteLine($"[MCP] STDOUT: {line}");
                        stdoutQueue.Add(line);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MCP] Error in stdout reader: {ex.Message}");
                }
                Debug.WriteLine("[MCP] Stdout reader thread ended");
            })
            {
                IsBackground = true,
                Name = "MCP-Stdout-Reader"
            };
            stdoutThread.Start();

            var stderrThread = new Thread(() =>
            {
                Debug.WriteLine("[MCP] Started stderr reader thread");
                try
                {
                    using var reader = process.StandardError;
                    while (true)
                    {
                        var line = reader.ReadLine();
                        if (line == null)
                        {
                            Debug.WriteLine("[MCP] STDERR: EOF reached");
                            break;
                        }
                        Debug.WriteLine($"[MCP] STDERR: {line}");
                        if (stderrBuffer.Length < 4000)
                        {
                            stderrBuffer.AppendLine(line);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MCP] Error in stderr reader: {ex.Message}");
                }
                Debug.WriteLine("[MCP] Stderr reader thread ended");
            })
            {
                IsBackground = true,
                Name = "MCP-Stderr-Reader"
            };
            stderrThread.Start();

            // Wait for container to start
            Debug.WriteLine("[MCP] Waiting for container to start...");
            await Task.Delay(2000, connectCts.Token);

            if (process.HasExited)
            {
                throw new Exception($"MCP server process exited early with code {process.ExitCode}. STDERR: {stderrBuffer}");
            }

            Debug.WriteLine($"[MCP] Initializing MCP protocol for {server.Name}");
            await InitializeMcpProtocolAsync(process, stdoutQueue, server, connectCts.Token);

            server.Status = McpServerStatus.Connected;
            Debug.WriteLine($"[MCP] Getting tools from {server.Name}");
            server.Tools = await GetServerToolsAsync(process, stdoutQueue, connectCts.Token);
            UpdateAllTools();
            ServerStatusChanged?.Invoke(this, server);
            Debug.WriteLine($"[MCP] Connected to {server.Name} with {server.Tools.Count} tools");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MCP] Failed to connect to {server.Name}: {ex.Message}");
            server.Status = McpServerStatus.Error;
            ServerStatusChanged?.Invoke(this, server);

            if (process != null)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                    process.Dispose();
                }
                catch { }
            }
        }
    }

    private static ProcessStartInfo CreateProcessStartInfo(McpServerConfig server)
    {
#if WINDOWS
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Environment.CurrentDirectory
        };

        psi.ArgumentList.Add("/c");
        psi.ArgumentList.Add(server.Command);
        if (server.Args != null && server.Args.Count > 0)
        {
            foreach (var arg in server.Args)
            {
                psi.ArgumentList.Add(arg);
            }
        }
#else
        var psi = new ProcessStartInfo
        {
            FileName = server.Command,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Environment.CurrentDirectory
        };

        if (server.Args != null && server.Args.Count > 0)
        {
            foreach (var arg in server.Args)
            {
                psi.ArgumentList.Add(arg);
            }
        }
#endif

        if (server.Env != null)
        {
            foreach (var kvp in server.Env)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Key))
                {
                    psi.EnvironmentVariables[kvp.Key] = kvp.Value ?? string.Empty;
                }
            }
        }
#if WINDOWS
        // If PATH is missing npx, append common Node.js install locations
        if (string.Equals(server.Command, "npx", StringComparison.OrdinalIgnoreCase))
        {
            var path = psi.EnvironmentVariables["PATH"] ?? string.Empty;
            var nodePaths = new List<string>();
            var pf = Environment.GetEnvironmentVariable("ProgramFiles") ?? string.Empty;
            var pf86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)") ?? string.Empty;
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!string.IsNullOrWhiteSpace(pf)) nodePaths.Add(System.IO.Path.Combine(pf, "nodejs"));
            if (!string.IsNullOrWhiteSpace(pf86)) nodePaths.Add(System.IO.Path.Combine(pf86, "nodejs"));
            if (!string.IsNullOrWhiteSpace(appData)) nodePaths.Add(System.IO.Path.Combine(appData, "npm"));
            foreach (var np in nodePaths)
            {
                if (!string.IsNullOrWhiteSpace(np) && !path.Contains(np, StringComparison.OrdinalIgnoreCase))
                {
                    path = string.IsNullOrEmpty(path) ? np : $"{np};{path}";
                }
            }
            psi.EnvironmentVariables["PATH"] = path;
        }
#endif

        return psi;
    }

    /// <summary>
    /// Connects all configured MCP servers.
    /// </summary>
    public async Task ConnectAllServersAsync()
    {
        Debug.WriteLine($"[MCP] Connecting {Servers.Count} servers");
        foreach (var server in Servers)
        {
            Debug.WriteLine($"[MCP] Server {server.Name} status: {server.Status}");
            if (server.Status == McpServerStatus.Disconnected)
            {
                await ConnectServerAsync(server);
            }
        }
    }

    /// <summary>
    /// Disconnects from an MCP server.
    /// </summary>
    public void DisconnectServer(McpServerConfig server)
    {
        if (_serverProcesses.TryGetValue(server.Name, out var process))
        {
            try { process.Kill(); process.Dispose(); } catch { }
            _serverProcesses.Remove(server.Name);
        }
        if (_stdoutQueues.TryGetValue(server.Name, out var queue))
        {
            queue.Dispose();
            _stdoutQueues.Remove(server.Name);
        }

        server.Status = McpServerStatus.Disconnected;
        server.Tools.Clear();
        UpdateAllTools();
        ServerStatusChanged?.Invoke(this, server);
    }

    /// <summary>
    /// Executes a tool call. Supports built-in "fetch" and external MCP tools.
    /// </summary>
    public async Task<McpToolResult> ExecuteToolAsync(string toolName, string arguments)
    {
        var server = Servers.FirstOrDefault(s => s.Tools.Any(t => t.Name == toolName));
        if (server != null && _serverProcesses.TryGetValue(server.Name, out var process) && _stdoutQueues.TryGetValue(server.Name, out var queue))
        {
            Debug.WriteLine($"[MCP] Executing external tool '{toolName}' with args: {arguments}");
            return await ExecuteMcpToolAsync(process, queue, toolName, arguments);
        }

        Debug.WriteLine($"[MCP] Tool '{toolName}' not found, using fallback");
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
            args = JsonSerializer.Deserialize<FetchArgs>(arguments, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            return new McpToolResult { ToolName = "fetch", Success = false, Error = $"Invalid arguments: {ex.Message}" };
        }

        if (string.IsNullOrWhiteSpace(args?.Url))
        {
            return new McpToolResult { ToolName = "fetch", Success = false, Error = "url is required" };
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
            var resultJson = JsonSerializer.Serialize(new { status = (int)response.StatusCode, reason = response.ReasonPhrase, body }, _jsonOptions);

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
            return new McpToolResult { ToolName = "fetch", Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Gets tools formatted for OpenAI API.
    /// </summary>
    public List<Tool> GetOpenAiTools()
    {
        return AllTools.Where(t => t.IsEnabled).Select(t => t.ToOpenAiTool()).ToList();
    }

    private void UpdateAllTools()
    {
        AllTools.Clear();
        foreach (var server in Servers.Where(s => s.Status == McpServerStatus.Connected))
        {
            AllTools.AddRange(server.Tools);
        }
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
            }
        ];
    }

    private async Task<McpToolResult> ExecuteMcpToolAsync(Process process, BlockingCollection<string> stdoutQueue, string toolName, string arguments)
    {
        JsonElement argsElement;
        if (string.IsNullOrWhiteSpace(arguments))
        {
            using var emptyDoc = JsonDocument.Parse("{}");
            argsElement = emptyDoc.RootElement.Clone();
        }
        else
        {
            using var argsDoc = JsonDocument.Parse(arguments);
            argsElement = argsDoc.RootElement.Clone();
        }

        var callRequest = new
        {
            jsonrpc = "2.0",
            id = 3,
            method = "tools/call",
            @params = new { name = toolName, arguments = argsElement }
        };

        Debug.WriteLine($"[MCP] Sending tools/call request for '{toolName}'");
        var response = await SendMcpRequestAsync(process, stdoutQueue, callRequest);
        if (response?.RootElement.TryGetProperty("error", out var errorProp) == true)
        {
            var errorMsg = errorProp.GetProperty("message").GetString();
            Debug.WriteLine($"[MCP] Tool call error: {errorMsg}");
            return new McpToolResult { ToolName = toolName, Success = false, Error = errorMsg };
        }

        if (response?.RootElement.TryGetProperty("result", out var result) == true)
        {
            var resultJson = result.GetRawText();
            Debug.WriteLine($"[MCP] Tool call success: {resultJson}");
            return new McpToolResult { ToolName = toolName, Success = true, Result = resultJson };
        }

        Debug.WriteLine("[MCP] Tool call failed: no result");
        return new McpToolResult { ToolName = toolName, Success = false, Error = "No result" };
    }

    private async Task InitializeMcpProtocolAsync(Process process, BlockingCollection<string> stdoutQueue, McpServerConfig server, CancellationToken token)
    {
        // Send initialize request
        var initRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new { name = "LlamaClient", version = "1.0.0" }
            }
        };

        var response = await SendMcpRequestAsync(process, stdoutQueue, initRequest, token);
        if (response == null)
        {
            throw new Exception("MCP initialization failed: no response received");
        }
        if (response.RootElement.TryGetProperty("error", out var errorProp))
        {
            var errorMsg = errorProp.GetProperty("message").GetString();
            throw new Exception($"MCP initialization failed: {errorMsg}");
        }

        Debug.WriteLine("[MCP] Initialize response received, sending initialized notification");
        var initializedNotification = new { jsonrpc = "2.0", method = "notifications/initialized" };
        await SendMcpNotificationAsync(process, initializedNotification, token);
    }

    private async Task<List<McpTool>> GetServerToolsAsync(Process process, BlockingCollection<string> stdoutQueue, CancellationToken token)
    {
        var listRequest = new { jsonrpc = "2.0", id = 2, method = "tools/list" };

        Debug.WriteLine("[MCP] Requesting tool list");
        var response = await SendMcpRequestAsync(process, stdoutQueue, listRequest, token);
        if (response?.RootElement.TryGetProperty("error", out _) == true)
        {
            Debug.WriteLine("[MCP] Failed to list tools, using fallback");
            return GetMockTools("unknown");
        }

        var tools = new List<McpTool>();
        if (response?.RootElement.TryGetProperty("result", out var result) == true && result.TryGetProperty("tools", out var toolsArray))
        {
            Debug.WriteLine($"[MCP] Tools array has {toolsArray.GetArrayLength()} items");
            foreach (var tool in toolsArray.EnumerateArray())
            {
                var name = tool.GetProperty("name").GetString();
                var description = tool.GetProperty("description").GetString();
                var inputSchema = tool.GetProperty("inputSchema");

                tools.Add(new McpTool
                {
                    Name = name,
                    Description = description,
                    ServerName = "docker",
                    InputSchema = JsonSerializer.Deserialize<McpToolInputSchema>(inputSchema.GetRawText(), _jsonOptions),
                    IsEnabled = true
                });
            }
        }
        else
        {
            Debug.WriteLine("[MCP] No tools in response");
        }

        Debug.WriteLine($"[MCP] Retrieved {tools.Count} tools from server");
        return tools;
    }

    private async Task<JsonDocument?> WaitForJsonResponseAsync(BlockingCollection<string> stdoutQueue, CancellationToken token)
    {
        Debug.WriteLine("[MCP] Waiting for JSON response from queue...");
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (stdoutQueue.TryTake(out var line, 100, token))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    Debug.WriteLine($"[MCP] Got line from queue: {line}");
                    try
                    {
                        return JsonDocument.Parse(line);
                    }
                    catch (JsonException ex)
                    {
                        Debug.WriteLine($"[MCP] Failed to parse as JSON: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        Debug.WriteLine("[MCP] WaitForJsonResponseAsync returning null (cancelled or timeout)");
        return null;
    }

    private async Task<JsonDocument?> SendMcpRequestAsync(Process process, BlockingCollection<string> stdoutQueue, object request, CancellationToken token = default)
    {
        if (process.HasExited)
        {
            Debug.WriteLine("[MCP] Process has exited");
            throw new Exception("MCP server process has exited");
        }

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        Debug.WriteLine($"[MCP] Sending request: {json}");

        CancellationToken effectiveToken = token;
        CancellationTokenSource? timeoutCts = null;
        if (!effectiveToken.CanBeCanceled)
        {
            timeoutCts = new CancellationTokenSource(DefaultRequestTimeout);
            effectiveToken = timeoutCts.Token;
        }

        try
        {
            Debug.WriteLine("[MCP] Writing to StandardInput (raw UTF-8, no BOM)...");
            var jsonBytes = Encoding.UTF8.GetBytes(json + "\n");
            await process.StandardInput.BaseStream.WriteAsync(jsonBytes, effectiveToken);
            await process.StandardInput.BaseStream.FlushAsync(effectiveToken);
            Debug.WriteLine("[MCP] Request written and flushed successfully");

            return await WaitForJsonResponseAsync(stdoutQueue, effectiveToken);
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("[MCP] Timeout waiting for response");
            throw new Exception("Timeout waiting for MCP response");
        }
        finally
        {
            timeoutCts?.Dispose();
        }
    }

    private Task SendMcpNotificationAsync(Process process, object notification, CancellationToken token = default)
    {
        var json = JsonSerializer.Serialize(notification, _jsonOptions);
        Debug.WriteLine($"[MCP] Sending notification: {json}");

        var jsonBytes = Encoding.UTF8.GetBytes(json + "\n");
        process.StandardInput.BaseStream.Write(jsonBytes, 0, jsonBytes.Length);
        process.StandardInput.BaseStream.Flush();
        Debug.WriteLine("[MCP] Notification written and flushed");

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        foreach (var process in _serverProcesses.Values)
        {
            try { process.Kill(); process.Dispose(); } catch { }
        }
        _serverProcesses.Clear();
        foreach (var queue in _stdoutQueues.Values)
        {
            try { queue.Dispose(); } catch { }
        }
        _stdoutQueues.Clear();
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
