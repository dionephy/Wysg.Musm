using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Text.Json;
using Wysg.Musm.LlamaClient.Models;

namespace Wysg.Musm.LlamaClient.ViewModels;

public partial class McpConfigViewModel : ObservableObject
{
    private const string DefaultJson = """
{
  "mcp": {
    "servers": {
      "fetch": {
        "command": "uvx",
        "args": ["mcp-server-fetch"]
      },
      "filesystem": {
        "command": "npx",
        "args": ["-y", "@modelcontextprotocol/server-filesystem", "C:\\\\Users\\\\dhkim\\\\Downloads"]
      }
    }
  }
}
""";

    private readonly string _configPath = "mcp-config.json";
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private string _rawJson = string.Empty;
    public string RawJson
    {
        get => _rawJson;
        set => SetProperty(ref _rawJson, value);
    }

    public McpConfigViewModel()
    {
        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                RawJson = DefaultJson;
                File.WriteAllText(_configPath, RawJson);
                return;
            }

            RawJson = File.ReadAllText(_configPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load MCP config: {ex.Message}");
            RawJson = DefaultJson;
        }
    }

    private bool ValidateJson(string json, out string normalized)
    {
        normalized = string.Empty;
        try
        {
            var parsed = JsonSerializer.Deserialize<McpConfigFile>(json, _jsonOptions);
            if (parsed?.Mcp?.Servers == null || parsed.Mcp.Servers.Count == 0)
            {
                return false;
            }
            normalized = JsonSerializer.Serialize(parsed, _jsonOptions);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Invalid MCP config JSON: {ex.Message}");
            return false;
        }
    }

    private void SaveConfiguration()
    {
        if (!ValidateJson(RawJson, out var normalized))
        {
            return;
        }

        try
        {
            File.WriteAllText(_configPath, normalized);
            RawJson = normalized;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save MCP config: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Save()
    {
        SaveConfiguration();
    }

    [RelayCommand]
    private void AddServer()
    {
        RawJson = DefaultJson;
        SaveConfiguration();
    }

    [RelayCommand]
    private void RemoveServer()
    {
        // No-op for raw JSON editor; kept for command binding compatibility
    }
}