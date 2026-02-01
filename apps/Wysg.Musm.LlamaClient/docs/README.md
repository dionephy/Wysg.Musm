# Llama Client - Wysg.Musm

A WPF desktop application for interacting with local Llama models through an OpenAI-compatible API.

## Overview

The Llama Client provides a user-friendly interface for chatting with locally-hosted LLM models, specifically designed to work with the `nvidia/Llama-3.3-70B-Instruct-FP4` model running on a local vLLM server.

## Features

### 1. API Connection
- **Endpoint Configuration**: Configurable API endpoint (default: `http://127.0.0.1:8000`)
- **List Models**: Fetch available models from `/v1/models` endpoint
- **Connection Status**: Real-time connection status indicator

### 2. Chat Interface
- **System Prompt**: Configurable system prompt for the conversation
- **Chat History**: Visual chat history with user/assistant message bubbles
- **Selectable Messages**: Chat messages (user and assistant) are selectable for copy/paste
- **Streaming Output**: Real-time streaming responses with SSE (Server-Sent Events)
- **Response Timing**: Each assistant response shows the elapsed time from Send to completion
- **Enter to Send**: Enter (or Ctrl+Enter) sends the message; Shift+Enter inserts a newline

### 3. Parameters
| Parameter | Description | Range |
|-----------|-------------|-------|
| Temperature | Controls randomness | 0.0 - 2.0 |
| Top P | Nucleus sampling | 0.0 - 1.0 |
| Max Tokens | Maximum response length | 1 - 128000 |
| Stop Sequences | Custom stop tokens | Comma-separated |
| Streaming | Enable/disable streaming | On/Off |

### 4. Presets
- **Save**: Save current settings to selected preset
- **New**: Create a new preset from current settings
- **Duplicate**: Clone an existing preset
- **Delete**: Remove a preset (minimum one must remain)
- Presets are stored in `%LocalAppData%\Wysg.Musm.LlamaClient\presets.json`

### 5. Actions
- **Copy as JSON**: Copy the last API request as JSON to clipboard
- **Replay from Clipboard**: Load and replay a JSON request from clipboard
- **Clear Chat**: Clear the current conversation

### 6. MCP Support (Model Context Protocol)
- Infrastructure for MCP tool integration
- Tool calling support in API requests
- Placeholder for future MCP server connections

## Sample API Request

```json
{
  "model": "nvidia/Llama-3.3-70B-Instruct-FP4",
  "messages": [
    { "role": "system", "content": "You are a helpful assistant." },
    { "role": "user", "content": "San Francisco is a" }
  ],
  "temperature": 0.0,
  "max_tokens": 128,
  "stream": true
}
```

## Project Structure

```
apps/Wysg.Musm.LlamaClient/
戍式式 Models/
弛   戍式式 ChatModels.cs      # API request/response models
弛   戍式式 McpModels.cs       # MCP protocol models
弛   戌式式 Preset.cs          # Preset configuration model
戍式式 Services/
弛   戍式式 LlamaApiService.cs # HTTP client for API communication
弛   戍式式 McpService.cs      # MCP server management
弛   戌式式 PresetService.cs   # Preset save/load operations
戍式式 ViewModels/
弛   戌式式 MainViewModel.cs   # Main application ViewModel
戍式式 Converters.cs          # WPF value converters
戍式式 MainWindow.xaml        # Main window UI
戍式式 MainWindow.xaml.cs     # Main window code-behind
戍式式 App.xaml               # Application definition
戌式式 App.xaml.cs            # Application code-behind
```

## Requirements

- .NET 10.0 (Windows)
- Local LLM server with OpenAI-compatible API (e.g., vLLM, LocalAI, Ollama)

## Dependencies

- **CommunityToolkit.Mvvm** (8.4.0) - MVVM framework
- **System.Text.Json** (9.0.0) - JSON serialization

## Usage

1. **Start your local LLM server**
   ```bash
   # Example with vLLM
   python -m vllm.entrypoints.openai.api_server \
     --model nvidia/Llama-3.3-70B-Instruct-FP4 \
     --port 8000
   ```

2. **Launch the Llama Client**

3. **Click "List Models"** to verify connection

4. **Enter a message** and press Send or Ctrl+Enter

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Enter / Ctrl+Enter | Send message |
| Shift+Enter | Insert newline |

## Dark Theme

The application uses a Visual Studio-inspired dark theme:
- Background: `#1E1E1E`
- Panel: `#252526`
- Accent: `#007ACC`
- User messages: Blue (`#2D4F7C`)
- Assistant messages: Gray (`#3C3C3C`)

## Future Enhancements

- [ ] Full MCP server integration
- [ ] Tool calling with real tool execution
- [ ] Conversation export/import
- [ ] Multiple conversation tabs
- [ ] Token count display
- [ ] Custom themes

## Changelog

### 2026-01-31
- Chat responses now display elapsed time from Send to completion

### 2026-01-09
- Chat messages are now selectable for copying
- Enter sends messages; Shift+Enter inserts a newline (Ctrl+Enter still sends)

### 2025-12-30
- Initial implementation
- Chat interface with streaming support
- Preset management
- Copy as JSON / Replay functionality
- MCP infrastructure (placeholder)

## MCP Setup & Troubleshooting

1. Ensure Docker is available inside WSL and the image is present (or pullable):
   ```bash
   wsl docker run --rm mcp/fetch:latest --help
   ```
2. Create an `mcp-config.json` next to the executable (or edit via MCP Config window). Example for the fetch server running through WSL Docker:
   ```json
   {
     "servers": [
       {
         "name": "fetch",
         "transport": "stdio",
         "command": "mcp/fetch:latest",
         "args": ["--pull=missing"],
         "env": {
           "HTTP_PROXY": "",
           "HTTPS_PROXY": ""
         },
         "isEnabled": true
       }
     ]
   }
   ```
   - `command` is the Docker image name.
   - `args` are appended to `docker run --rm -i` (e.g., `--pull=missing`, `--network host`).
   - `env` entries are passed as `-e KEY=VALUE` to the container.
3. The client waits up to ~2 minutes for the MCP container to start (covers image pulls). STDERR from the container is echoed to the debug output for diagnosis.
4. If connection times out, verify Docker access from Windows/WSL, pull the image manually, and rerun.
5. MCP stdio servers expect one JSON object per line (no pretty-print). The client now sends newline-delimited JSON, but if testing manually use single-line JSON with a trailing `\n`.
5. For manual tests, send one JSON object per line to stdio servers. Example from Windows using uvx fetch server:
   ```bash
   printf '%s\n' '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"Test","version":"1.0"}}}' \
   | uvx mcp-server-fetch
   ```
   Pretty-printed multi-line JSON will be rejected by stdio servers expecting NDJSON.
   Note: uvx sets PYTHONIOENCODING=utf-8 automatically for Windows encoding issues.

## MCP configuration

The MCP config is edited as raw JSON (opened via **Configure MCP Servers**). Default content:

```json
{
  "mcp": {
    "servers": {
      "fetch": {
        "command": "uvx",
        "args": ["mcp-server-fetch"]
      },
      "filesystem": {
        "command": "npx",
        "args": ["-y", "@modelcontextprotocol/server-filesystem", "C:\\Users\\dhkim\\Downloads"]
      }
    }
  }
}
```

Each server entry supports `command`, `args`, and optional `env` (object). The client loads this file at startup; saving in the dialog rewrites it with the same structure.

### MCP command resolution
- On Windows the client invokes MCP servers via `cmd /c <command> <args>` so PATH and `.cmd/.bat` shims are honored (e.g., `uvx`, `npx`).
- If a command is not found, ensure it is installed and on PATH.

### MCP tool call arguments
- When a tool takes no parameters, the client now sends `"arguments": {}` instead of an empty string. If you invoke tools manually, ensure arguments is a JSON object (empty if none).

---

*Created: 2025-12-30*
*Project: Wysg.Musm.LlamaClient*
