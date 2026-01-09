using System.Text;
using System.Text.Json;

namespace Wysg.Musm.Mcp.Pacs;

public static class Program
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false
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
}
