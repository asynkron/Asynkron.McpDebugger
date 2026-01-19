# Asynkron.McpDebugger

AI-controlled cooperative debugger via MCP (Model Context Protocol).

Add breakpoints to your C# code that pause execution until an AI (like Claude) resumes them. The AI can inspect the call stack, view source code context, and control program flow.

## How It Works

```
┌─────────────────┐     MCP (stdio)      ┌─────────────────────────┐
│   AI (Claude)   │ ◄──────────────────► │   mcpdebugger mcp       │
└─────────────────┘                       └───────────┬─────────────┘
                                                      │ HTTP
                                          ┌───────────▼─────────────┐
                                          │   mcpdebugger serve     │
                                          └───────────┬─────────────┘
                                                      │ HTTP
                                          ┌───────────▼─────────────┐
                                          │   Your Application      │
                                          │   with DebugBreak calls │
                                          └─────────────────────────┘
```

## Installation

### Install the CLI tool (global)

```bash
dotnet tool install -g Asynkron.McpDebugger
```

### Add the client library to your project

```bash
dotnet add package Asynkron.McpDebugger.Client
```

## Quick Start

### 1. Add breakpoints to your code

```csharp
using Asynkron.McpDebugger.Client;

public class MyService
{
    public async Task ProcessOrderAsync(Order order)
    {
        // Async breakpoint - doesn't block threadpool threads
        await DebugBreak.HereAsync();

        // Your code continues after AI resumes...
        await ValidateOrder(order);

        // Another breakpoint
        await DebugBreak.HereAsync();

        await ChargeCustomer(order);
    }
}
```

### 2. Start the debug server

```bash
mcpdebugger serve
```

### 3. Configure Claude Code

Add to `~/.claude/settings.json`:

```json
{
  "mcpServers": {
    "debugger": {
      "command": "mcpdebugger",
      "args": ["mcp"]
    }
  }
}
```

### 4. Run your application

Your app will pause at each `DebugBreak` call until the AI resumes it.

## API

### Client Library

```csharp
// Async breakpoint (recommended) - doesn't steal threadpool threads
await DebugBreak.HereAsync();

// Sync breakpoint - blocks the current thread
DebugBreak.Here();

// Configure server URL (default: http://localhost:5200)
DebugBreak.Configure("http://localhost:5200");

// Disable/enable breakpoints
DebugBreak.Disable();
DebugBreak.Enable();
```

### MCP Tools (available to AI)

| Tool | Description |
|------|-------------|
| `get_breakpoints` | List all active breakpoints |
| `get_context` | Get call stack and source code for a breakpoint |
| `resume` | Resume a specific breakpoint |
| `resume_all` | Resume all active breakpoints |

### HTTP API (for direct access)

```bash
# List active breakpoints
curl http://localhost:5200/status

# Resume a specific breakpoint
curl -X POST http://localhost:5200/resume/{breakpoint-id}

# Resume all breakpoints
curl -X POST http://localhost:5200/resume-all
```

## CLI Commands

```bash
mcpdebugger serve [--port 5200]   # Start the HTTP debug server
mcpdebugger mcp [--port 5200]     # Start the MCP server (for AI integration)
mcpdebugger --help                # Show help
```

## How the Breakpoints Work

The async breakpoint (`HereAsync`) uses `TaskCompletionSource` internally:
- Your code awaits an HTTP POST to the debug server
- The server holds the request until the AI calls `resume`
- No threadpool threads are blocked

The sync breakpoint (`Here`) simply blocks on the HTTP call:
- The calling thread is blocked until resume
- Use sparingly to avoid thread starvation

## Building from Source

```bash
git clone https://github.com/asynkron/Asynkron.McpDebugger.git
cd Asynkron.McpDebugger
dotnet build
```

## License

MIT
