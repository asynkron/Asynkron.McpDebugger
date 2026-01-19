using System.Text.Json.Serialization;

namespace Asynkron.McpDebugger.Client;

/// <summary>
/// Information about a single stack frame
/// </summary>
public record StackFrameInfo
{
    [JsonPropertyName("method")]
    public required string Method { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("file")]
    public string? File { get; init; }

    [JsonPropertyName("line")]
    public int Line { get; init; }

    [JsonPropertyName("column")]
    public int Column { get; init; }
}

/// <summary>
/// Full context sent to the debugger server when a breakpoint is hit
/// </summary>
public record BreakpointContext
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("file")]
    public string? File { get; init; }

    [JsonPropertyName("line")]
    public int Line { get; init; }

    [JsonPropertyName("column")]
    public int Column { get; init; }

    [JsonPropertyName("method")]
    public required string Method { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("callStack")]
    public required StackFrameInfo[] CallStack { get; init; }

    [JsonPropertyName("sourceLines")]
    public string[]? SourceLines { get; init; }

    [JsonPropertyName("sourceStartLine")]
    public int SourceStartLine { get; init; }

    [JsonPropertyName("hitTime")]
    public DateTime HitTime { get; init; }
}
