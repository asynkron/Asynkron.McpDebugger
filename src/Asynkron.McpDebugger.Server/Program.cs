using Asynkron.McpDebugger.Server;

var cliArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();

if (cliArgs.Length == 0)
{
    PrintUsage();
    return 1;
}

var command = cliArgs[0].ToLowerInvariant();
var port = GetPortArg(cliArgs) ?? 5200;

switch (command)
{
    case "serve":
        return await RunServeAsync(port);

    case "mcp":
        return await RunMcpAsync(port);

    case "--help":
    case "-h":
    case "help":
        PrintUsage();
        return 0;

    default:
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintUsage();
        return 1;
}

static void PrintUsage()
{
    Console.WriteLine("""
        mcpdebugger - AI-controlled cooperative debugger via MCP

        Usage:
          mcpdebugger serve [--port <port>]   Start the HTTP debug server (default port: 5200)
          mcpdebugger mcp [--port <port>]     Start the MCP server (talks to debug server)
          mcpdebugger --help                  Show this help message

        Typical usage:
          1. Run 'mcpdebugger serve' in one terminal
          2. Configure Claude Code to use 'mcpdebugger mcp' as an MCP server
          3. Add DebugBreak.Here() calls to your C# code
          4. Run your application
          5. Use Claude Code to inspect and resume breakpoints

        MCP Configuration (add to Claude Code settings):
          {
            "mcpServers": {
              "debugger": {
                "command": "mcpdebugger",
                "args": ["mcp"]
              }
            }
          }
        """);
}

static int? GetPortArg(string[] args)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i] == "--port" && int.TryParse(args[i + 1], out var port))
        {
            return port;
        }
    }
    return null;
}

static async Task<int> RunServeAsync(int port)
{
    var store = new BreakpointStore();
    var server = new DebugServer(store);

    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    try
    {
        await server.RunAsync(port, cts.Token);
        return 0;
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("\n[DebugServer] Shutting down...");
        return 0;
    }
}

static async Task<int> RunMcpAsync(int port)
{
    var server = new McpServer(port);

    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    try
    {
        await server.RunAsync(cts.Token);
        return 0;
    }
    catch (OperationCanceledException)
    {
        return 0;
    }
}
