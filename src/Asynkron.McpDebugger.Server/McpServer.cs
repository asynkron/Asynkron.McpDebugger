using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Asynkron.McpDebugger.Server;

/// <summary>
/// MCP (Model Context Protocol) server that exposes debugging tools to AI
/// </summary>
public class McpServer
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public McpServer(int port = 5200)
    {
        _baseUrl = $"http://localhost:{port}";
        _http = new HttpClient { BaseAddress = new Uri(_baseUrl) };
    }

    public async Task RunAsync(CancellationToken ct)
    {
        using var reader = new StreamReader(Console.OpenStandardInput());
        using var writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };

        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null)
            {
                break;
            }

            try
            {
                var request = JsonNode.Parse(line);
                var response = await HandleRequestAsync(request);
                if (response != null)
                {
                    await writer.WriteLineAsync(response.ToJsonString());
                }
            }
            catch (Exception ex)
            {
                var error = new JsonObject
                {
                    ["jsonrpc"] = "2.0",
                    ["error"] = new JsonObject
                    {
                        ["code"] = -32603,
                        ["message"] = ex.Message
                    }
                };
                await writer.WriteLineAsync(error.ToJsonString());
            }
        }
    }

    private async Task<JsonNode?> HandleRequestAsync(JsonNode? request)
    {
        if (request == null)
        {
            return null;
        }

        var method = request["method"]?.GetValue<string>();
        var id = request["id"];
        var @params = request["params"];

        JsonNode? result = method switch
        {
            "initialize" => HandleInitialize(),
            "tools/list" => HandleToolsList(),
            "tools/call" => await HandleToolCallAsync(@params),
            _ => null
        };

        // Notifications don't need responses
        if (result == null && method?.StartsWith("notifications/") == true)
        {
            return null;
        }

        if (result == null)
        {
            return new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id?.DeepClone(),
                ["error"] = new JsonObject
                {
                    ["code"] = -32601,
                    ["message"] = $"Unknown method: {method}"
                }
            };
        }

        return new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id?.DeepClone(),
            ["result"] = result
        };
    }

    private static JsonNode HandleInitialize()
    {
        return new JsonObject
        {
            ["protocolVersion"] = "2024-11-05",
            ["capabilities"] = new JsonObject
            {
                ["tools"] = new JsonObject()
            },
            ["serverInfo"] = new JsonObject
            {
                ["name"] = "mcpdebugger",
                ["version"] = "0.1.0"
            }
        };
    }

    private static JsonNode HandleToolsList()
    {
        return new JsonObject
        {
            ["tools"] = new JsonArray
            {
                new JsonObject
                {
                    ["name"] = "get_breakpoints",
                    ["description"] = "List all active breakpoints that are currently waiting to be resumed",
                    ["inputSchema"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject()
                    }
                },
                new JsonObject
                {
                    ["name"] = "get_context",
                    ["description"] = "Get detailed context for a breakpoint including call stack and source code",
                    ["inputSchema"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["id"] = new JsonObject
                            {
                                ["type"] = "string",
                                ["description"] = "Breakpoint ID"
                            }
                        },
                        ["required"] = new JsonArray { "id" }
                    }
                },
                new JsonObject
                {
                    ["name"] = "resume",
                    ["description"] = "Resume execution at a specific breakpoint, allowing the paused code to continue",
                    ["inputSchema"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["id"] = new JsonObject
                            {
                                ["type"] = "string",
                                ["description"] = "Breakpoint ID to resume"
                            }
                        },
                        ["required"] = new JsonArray { "id" }
                    }
                },
                new JsonObject
                {
                    ["name"] = "resume_all",
                    ["description"] = "Resume all active breakpoints at once",
                    ["inputSchema"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject()
                    }
                }
            }
        };
    }

    private async Task<JsonNode?> HandleToolCallAsync(JsonNode? @params)
    {
        var toolName = @params?["name"]?.GetValue<string>();
        var args = @params?["arguments"];

        try
        {
            var result = toolName switch
            {
                "get_breakpoints" => await CallGetBreakpointsAsync(),
                "get_context" => await CallGetContextAsync(args),
                "resume" => await CallResumeAsync(args),
                "resume_all" => await CallResumeAllAsync(),
                _ => throw new NotSupportedException($"Unknown tool: {toolName}")
            };

            return new JsonObject
            {
                ["content"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "text",
                        ["text"] = result
                    }
                }
            };
        }
        catch (HttpRequestException ex)
        {
            return new JsonObject
            {
                ["content"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "text",
                        ["text"] = $"Error: Could not connect to debug server at {_baseUrl}. Make sure 'mcpdebugger serve' is running.\n\nDetails: {ex.Message}"
                    }
                },
                ["isError"] = true
            };
        }
        catch (Exception ex)
        {
            return new JsonObject
            {
                ["content"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "text",
                        ["text"] = $"Error: {ex.Message}"
                    }
                },
                ["isError"] = true
            };
        }
    }

    private async Task<string> CallGetBreakpointsAsync()
    {
        var response = await _http.GetAsync("/status");
        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());

        var breakpoints = json?["breakpoints"]?.AsArray();
        var count = json?["activeBreakpoints"]?.GetValue<int>() ?? 0;

        if (count == 0)
        {
            return "No active breakpoints. The application is either not running or hasn't hit any DebugBreak calls yet.";
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Active breakpoints: {count}");
        sb.AppendLine();

        if (breakpoints != null)
        {
            foreach (var bp in breakpoints)
            {
                var id = bp?["id"]?.GetValue<string>() ?? "?";
                var file = bp?["file"]?.GetValue<string>() ?? "?";
                var line = bp?["line"]?.GetValue<int>() ?? 0;
                var method = bp?["method"]?.GetValue<string>() ?? "?";
                var type = bp?["type"]?.GetValue<string>() ?? "?";

                // Shorten file path for readability
                var shortFile = Path.GetFileName(file);

                sb.AppendLine(CultureInfo.InvariantCulture, $"  [{id}]");
                sb.AppendLine(CultureInfo.InvariantCulture, $"    Location: {type}.{method}");
                sb.AppendLine(CultureInfo.InvariantCulture, $"    File: {shortFile}:{line}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("Use get_context with a breakpoint ID to see source code and call stack.");
        sb.AppendLine("Use resume with a breakpoint ID to continue execution.");

        return sb.ToString();
    }

    private async Task<string> CallGetContextAsync(JsonNode? args)
    {
        var id = args?["id"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Breakpoint ID is required");
        }

        var response = await _http.GetAsync("/status");
        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());

        var breakpoints = json?["breakpoints"]?.AsArray();
        var bp = breakpoints?.FirstOrDefault(b => b?["id"]?.GetValue<string>() == id);

        if (bp == null)
        {
            return $"Breakpoint '{id}' not found. It may have already been resumed or the ID is incorrect.";
        }

        // For detailed context, we need the full breakpoint data
        // The /status endpoint only returns summary, so we'll display what we have
        // In a more complete implementation, we'd have a /context/{id} endpoint

        var file = bp["file"]?.GetValue<string>() ?? "Unknown";
        var line = bp["line"]?.GetValue<int>() ?? 0;
        var method = bp["method"]?.GetValue<string>() ?? "Unknown";
        var type = bp["type"]?.GetValue<string>() ?? "Unknown";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"=== Breakpoint {id} ===");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Location: {type}.{method}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"File: {file}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Line: {line}");
        sb.AppendLine();

        // Try to read source context from file (if accessible from server)
        try
        {
            if (File.Exists(file) && line > 0)
            {
                var lines = File.ReadAllLines(file);
                var startLine = Math.Max(0, line - 6);
                var endLine = Math.Min(lines.Length - 1, line + 4);

                sb.AppendLine("Source:");
                sb.AppendLine("```csharp");
                for (var i = startLine; i <= endLine; i++)
                {
                    var marker = (i == line - 1) ? " >> " : "    ";
                    sb.AppendLine(CultureInfo.InvariantCulture, $"{i + 1,4}{marker}{lines[i]}");
                }
                sb.AppendLine("```");
            }
        }
        catch
        {
            sb.AppendLine("(Source file not accessible from server)");
        }

        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Use 'resume' with id '{id}' to continue execution.");

        return sb.ToString();
    }

    private async Task<string> CallResumeAsync(JsonNode? args)
    {
        var id = args?["id"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Breakpoint ID is required");
        }

        var response = await _http.PostAsync($"/resume/{Uri.EscapeDataString(id)}", null);
        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());

        if (json?["error"] != null)
        {
            return $"Could not resume breakpoint: {json["error"]?.GetValue<string>()}";
        }

        return $"Breakpoint '{id}' resumed. Execution continues.";
    }

    private async Task<string> CallResumeAllAsync()
    {
        var response = await _http.PostAsync("/resume-all", null);
        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());

        var count = json?["resumed"]?.GetValue<int>() ?? 0;

        if (count == 0)
        {
            return "No active breakpoints to resume.";
        }

        return $"Resumed {count} breakpoint(s). All paused execution continues.";
    }
}
