# Radium Integration

This MCP server can call into Radium via HTTP and UI Automation.

## Tool: `radium_health`
- **Purpose:** Issues `GET /health` against the Radium API to confirm connectivity.
- **Arguments:**
  - `base_url` (optional, string): Overrides the Radium API base URL.
    - Resolution order:
      1. `base_url` argument (when provided)
      2. `RADIUM_API_BASE_URL` environment variable
      3. Fallback: `http://localhost:5205`
- **Response:** Returns the HTTP status and response body. Failures surface as JSON-RPC errors with MCP error codes.

## Tool: `radium_test`
- **Purpose:** Finds the running Radium main window and presses the `Test` button via Windows UI Automation.
- **Arguments:**
  - `process_name` (optional, string): Override for the Radium process name. Default: `Wysg.Musm.Radium` (any process whose name or window title contains `Radium` will be considered).
  - `button_name` (optional, string): Override for the button label/automation id. Default search order: provided name ¡æ `Test` ¡æ `Run Test` ¡æ `TEST` ¡æ any button containing `test` in name/automation id.
- **Behavior:**
  1. Locates the most recently started Radium process with a visible main window.
  2. Restores and foregrounds the window.
  3. Searches descendant UI Automation elements for the target button and invokes/toggles it.
- **Response:** Returns a success message on invoke, or a JSON-RPC error if the process/window/button cannot be found or invoked.

## Prerequisites
- Radium API must be running and reachable for `radium_health` (default ports: `http://localhost:5205`, `https://localhost:5206`).
- Network/firewall rules must allow the MCP server process to reach the Radium API for `radium_health`.
- `radium_test` requires the Radium desktop app to be running on Windows with an accessible main window.

## Usage Examples

### `radium_health`
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "radium_health",
    "arguments": {
      "base_url": "http://localhost:5205"
    }
  }
}
```

### `radium_test`
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "radium_test",
    "arguments": {
      "process_name": "Wysg.Musm.Radium",
      "button_name": "Test"
    }
  }
}
```

## Notes
- Keep stdout reserved for JSON-RPC responses; any diagnostics are emitted to stderr.
- HTTP timeouts are limited to 10 seconds to avoid hanging the MCP host.
- `radium_test` runs UI Automation on a dedicated STA thread and attempts to restore/foreground the Radium window before invocation.
