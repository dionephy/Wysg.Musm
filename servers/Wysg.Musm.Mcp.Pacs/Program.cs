using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows.Automation;

namespace Wysg.Musm.Mcp.Pacs;

public static class Program
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false
    };

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public static async Task Main()
    {
        // Ensure UTF-8 (no BOM) for predictable stdio behavior.
        Console.InputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        // Read line-delimited JSON-RPC messages until stdin closes.
        string? line;
        while ((line = await Console.In.ReadLineAsync()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;

                var method = root.TryGetProperty("method", out var m) ? m.GetString() : null;
                var hasId = root.TryGetProperty("id", out var idEl);

                // Notifications may not have id; ignore those safely.
                if (method is null)
                    continue;

                if (method == "initialize" && hasId)
                {
                    var id = CloneId(idEl);
                    WriteResponse(new
                    {
                        jsonrpc = "2.0",
                        id,
                        result = new
                        {
                            protocolVersion = "2024-11-05",
                            capabilities = new
                            {
                                tools = new { listChanged = false },
                                prompts = new { listChanged = false },
                                experimental = new { }
                            },
                            serverInfo = new { name = "wysg-mcp-pacs-dummy", version = "0.0.1" }
                        }
                    });
                    continue;
                }

                if (method == "tools/list" && hasId)
                {
                    var id = CloneId(idEl);
                    WriteResponse(new
                    {
                        jsonrpc = "2.0",
                        id,
                        result = new
                        {
                            tools = new object[]
                            {
                                new
                                {
                                    name = "open_worklist",
                                    description = "Dummy tool: simulates opening the worklist.",
                                    inputSchema = new
                                    {
                                        type = "object",
                                        properties = new { },
                                        required = Array.Empty<string>(),
                                        additionalProperties = false
                                    }
                                },
                                new
                                {
                                    name = "open_patient_number",
                                    description = "Dummy tool: simulates opening a patient by patient_number.",
                                    inputSchema = new
                                    {
                                        type = "object",
                                        properties = new
                                        {
                                            patient_number = new
                                            {
                                                type = "string",
                                                minLength = 1,
                                                description = "Patient number (MRN/ID)."
                                            }
                                        },
                                        required = new[] { "patient_number" },
                                        additionalProperties = false
                                    }
                                },
                                new
                                {
                                    name = "radium_health",
                                    description = "Calls the Radium API /health endpoint to verify connectivity.",
                                    inputSchema = new
                                    {
                                        type = "object",
                                        properties = new
                                        {
                                            base_url = new
                                            {
                                                type = "string",
                                                description = "Override Radium API base URL (defaults to env RADIUM_API_BASE_URL or http://localhost:5205)."
                                            }
                                        },
                                        required = Array.Empty<string>(),
                                        additionalProperties = false
                                    }
                                },
                                new
                                {
                                    name = "radium_test",
                                    description = "Invokes the Radium main window Test button (UI Automation).",
                                    inputSchema = new
                                    {
                                        type = "object",
                                        properties = new
                                        {
                                            process_name = new
                                            {
                                                type = "string",
                                                description = "Optional override for Radium process name (default: Wysg.Musm.Radium)."
                                            },
                                            button_name = new
                                            {
                                                type = "string",
                                                description = "Optional override for button name/label (default: Test)."
                                            }
                                        },
                                        required = Array.Empty<string>(),
                                        additionalProperties = false
                                    }
                                }
                            }
                        }
                    });
                    continue;
                }

                if (method == "tools/call" && hasId)
                {
                    var id = CloneId(idEl);

                    // params: { name: "...", arguments: {...} }
                    var @params = root.GetProperty("params");
                    var toolName = @params.GetProperty("name").GetString() ?? "";

                    if (toolName == "open_worklist")
                    {
                        WriteToolResult(id, "Omygod worklist is open");
                        continue;
                    }

                    if (toolName == "open_patient_number")
                    {
                        string? patientNumber = null;
                        if (@params.TryGetProperty("arguments", out var argsEl) &&
                            argsEl.ValueKind == JsonValueKind.Object &&
                            argsEl.TryGetProperty("patient_number", out var pnEl))
                        {
                            patientNumber = pnEl.GetString();
                        }

                        if (string.IsNullOrWhiteSpace(patientNumber))
                        {
                            WriteError(id, code: -32602, message: "Missing required argument: patient_number");
                            continue;
                        }

                        WriteToolResult(id, $"oh patient is open (patient_number={patientNumber})");
                        continue;
                    }

                    if (toolName == "radium_health")
                    {
                        string? baseUrlOverride = null;
                        if (@params.TryGetProperty("arguments", out var argsEl) &&
                            argsEl.ValueKind == JsonValueKind.Object &&
                            argsEl.TryGetProperty("base_url", out var buEl))
                        {
                            baseUrlOverride = buEl.GetString();
                        }

                        var result = await CheckRadiumHealthAsync(baseUrlOverride);
                        if (result.Success)
                        {
                            WriteToolResult(id, result.Message);
                        }
                        else
                        {
                            WriteError(id, result.ErrorCode ?? -32002, result.Message);
                        }

                        continue;
                    }

                    if (toolName == "radium_test")
                    {
                        string? processNameOverride = null;
                        string? buttonNameOverride = null;
                        if (@params.TryGetProperty("arguments", out var argsEl) &&
                            argsEl.ValueKind == JsonValueKind.Object)
                        {
                            if (argsEl.TryGetProperty("process_name", out var pnEl))
                            {
                                processNameOverride = pnEl.GetString();
                            }
                            if (argsEl.TryGetProperty("button_name", out var bnEl))
                            {
                                buttonNameOverride = bnEl.GetString();
                            }
                        }

                        var result = await InvokeRadiumTestButtonAsync(processNameOverride, buttonNameOverride);
                        if (result.Success)
                        {
                            WriteToolResult(id, result.Message);
                        }
                        else
                        {
                            WriteError(id, result.ErrorCode ?? -32005, result.Message);
                        }

                        continue;
                    }

                    WriteError(id, code: -32601, message: $"Unknown tool: {toolName}");
                    continue;
                }

                // Unknown method: if it's a request, respond with error; if notification, ignore.
                if (hasId)
                {
                    var id = CloneId(idEl);
                    WriteResponse(new
                    {
                        jsonrpc = "2.0",
                        id,
                        error = new { code = -32601, message = $"Method not found: {method}" }
                    });
                }
            }
            catch (Exception ex)
            {
                // Best effort: if parsing fails and no id, just ignore.
                // If you want, write diagnostics to stderr (NOT stdout).
                Console.Error.WriteLine($"[dummy-mcp] parse/handle error: {ex.Message}");
            }
        }
    }

    private static object CloneId(JsonElement idEl)
    {
        // JSON-RPC id can be number/string/null. We'll preserve its type.
        return idEl.ValueKind switch
        {
            JsonValueKind.Number when idEl.TryGetInt64(out var l) => l,
            JsonValueKind.Number => idEl.GetDouble(),
            JsonValueKind.String => idEl.GetString() ?? "",
            JsonValueKind.Null => null!,
            _ => idEl.ToString()
        };
    }

    private static void WriteToolResult(object id, string message)
    {
        WriteResponse(new
        {
            jsonrpc = "2.0",
            id,
            result = new
            {
                content = new object[]
                {
                    new { type = "text", text = message }
                },
                isError = false
            }
        });
    }

    private static void WriteError(object id, int code, string message)
    {
        WriteResponse(new
        {
            jsonrpc = "2.0",
            id,
            error = new { code, message }
        });
    }

    private static void WriteResponse(object payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonOpts);
        Console.Out.WriteLine(json);
        Console.Out.Flush();
    }

    private static async Task<RadiumCallResult> CheckRadiumHealthAsync(string? baseUrlOverride)
    {
        var baseUrl = string.IsNullOrWhiteSpace(baseUrlOverride)
            ? Environment.GetEnvironmentVariable("RADIUM_API_BASE_URL")
            : baseUrlOverride;

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "http://localhost:5205";
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            return new RadiumCallResult(false, $"Invalid Radium API base URL: '{baseUrl}'", -32602);
        }

        // Ensure we always hit /health on the configured server.
        var healthUri = new Uri(baseUri, "/health");

        try
        {
            using var response = await Http.GetAsync(healthUri);
            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var statusText = string.IsNullOrWhiteSpace(body) ? "(empty body)" : body.Trim();
                return new RadiumCallResult(true, $"Radium API reachable: {(int)response.StatusCode} {response.ReasonPhrase}; body: {statusText}");
            }

            var errorBody = string.IsNullOrWhiteSpace(body) ? "(empty body)" : body.Trim();
            return new RadiumCallResult(false, $"Radium API responded with {(int)response.StatusCode} {response.ReasonPhrase}; body: {errorBody}", -32004);
        }
        catch (Exception ex)
        {
            return new RadiumCallResult(false, $"Failed to reach Radium API at {healthUri}: {ex.Message}", -32003);
        }
    }

    private sealed record RadiumCallResult(bool Success, string Message, int? ErrorCode = null);

    private static Task<RadiumCallResult> InvokeRadiumTestButtonAsync(string? processNameOverride, string? buttonNameOverride)
    {
        return RunOnStaAsync(() => InvokeRadiumTestButtonInternal(processNameOverride, buttonNameOverride));
    }

    private static Task<RadiumCallResult> RunOnStaAsync(Func<RadiumCallResult> work)
    {
        var tcs = new TaskCompletionSource<RadiumCallResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                tcs.SetResult(work());
            }
            catch (Exception ex)
            {
                tcs.SetResult(new RadiumCallResult(false, $"radium_test failed: {ex.Message}", -32006));
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        return tcs.Task;
    }

    private static RadiumCallResult InvokeRadiumTestButtonInternal(string? processNameOverride, string? buttonNameOverride)
    {
        var preferredName = string.IsNullOrWhiteSpace(processNameOverride) ? "Wysg.Musm.Radium" : processNameOverride.Trim();

        var candidates = new List<Process>();
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                if (p.HasExited) continue;
                if (p.MainWindowHandle == IntPtr.Zero) continue;

                var procName = p.ProcessName;
                var title = p.MainWindowTitle ?? string.Empty;

                if (procName.Equals(preferredName, StringComparison.OrdinalIgnoreCase) ||
                    procName.Contains("Radium", StringComparison.OrdinalIgnoreCase) ||
                    title.Contains("Radium", StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add(p);
                }
            }
            catch
            {
                // ignore processes we can't inspect
            }
        }

        var proc = candidates
            .OrderByDescending(p => SafeStartTime(p))
            .FirstOrDefault();

        if (proc == null)
        {
            return new RadiumCallResult(false, "No running Radium process with a visible window was found.", -32001);
        }

        var hwnd = proc.MainWindowHandle;
        if (hwnd == IntPtr.Zero)
        {
            return new RadiumCallResult(false, "Radium process has no main window handle.", -32001);
        }

        try
        {
            ShowWindow(hwnd, SW_RESTORE);
            SetForegroundWindow(hwnd);
        }
        catch { }

        AutomationElement? root = null;
        try
        {
            root = AutomationElement.FromHandle(hwnd);
        }
        catch (Exception ex)
        {
            return new RadiumCallResult(false, $"Failed to access Radium window automation: {ex.Message}", -32006);
        }

        if (root == null)
        {
            return new RadiumCallResult(false, "Could not obtain automation root for Radium window.", -32006);
        }

        var namesToMatch = new List<string>();
        if (!string.IsNullOrWhiteSpace(buttonNameOverride))
        {
            namesToMatch.Add(buttonNameOverride.Trim());
        }
        namesToMatch.AddRange(new[] { "Test", "Run Test", "TEST" });

        var button = FindButton(root, namesToMatch);
        if (button == null)
        {
            // Fallback: any button whose name or automation id contains "test"
            var buttonCond = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button);
            var buttons = root.FindAll(TreeScope.Descendants, buttonCond);
            foreach (AutomationElement b in buttons)
            {
                try
                {
                    var name = b.Current.Name ?? string.Empty;
                    var aid = b.Current.AutomationId ?? string.Empty;
                    if (name.Contains("test", StringComparison.OrdinalIgnoreCase) ||
                        aid.Contains("test", StringComparison.OrdinalIgnoreCase))
                    {
                        button = b;
                        break;
                    }
                }
                catch
                {
                    // ignore elements we cannot read
                }
            }
        }

        if (button == null)
        {
            return new RadiumCallResult(false, "Test button not found in Radium window.", -32001);
        }

        try
        {
            if (button.TryGetCurrentPattern(InvokePattern.Pattern, out var invObj) && invObj is InvokePattern inv)
            {
                inv.Invoke();
                return new RadiumCallResult(true, "Radium Test button invoked.");
            }

            if (button.TryGetCurrentPattern(TogglePattern.Pattern, out var togObj) && togObj is TogglePattern tog)
            {
                tog.Toggle();
                return new RadiumCallResult(true, "Radium Test button toggled.");
            }

            return new RadiumCallResult(false, "Test button does not support Invoke/Toggle patterns.", -32004);
        }
        catch (Exception ex)
        {
            return new RadiumCallResult(false, $"Failed to invoke Radium Test button: {ex.Message}", -32005);
        }
    }

    private static AutomationElement? FindButton(AutomationElement root, IEnumerable<string> names)
    {
        var buttonCond = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button);

        foreach (var name in names.Where(n => !string.IsNullOrWhiteSpace(n)))
        {
            var nameCond = new PropertyCondition(AutomationElement.NameProperty, name, PropertyConditionFlags.IgnoreCase);
            var match = root.FindFirst(TreeScope.Descendants, new AndCondition(buttonCond, nameCond));
            if (match != null)
            {
                return match;
            }

            var idCond = new PropertyCondition(AutomationElement.AutomationIdProperty, name, PropertyConditionFlags.IgnoreCase);
            match = root.FindFirst(TreeScope.Descendants, new AndCondition(buttonCond, idCond));
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static DateTime SafeStartTime(Process p)
    {
        try { return p.StartTime; }
        catch { return DateTime.MinValue; }
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;
}
