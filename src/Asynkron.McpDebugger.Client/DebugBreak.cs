using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace Asynkron.McpDebugger.Client;

/// <summary>
/// Cooperative debugger breakpoint API.
/// Call Here() or HereAsync() to pause execution until an AI resumes it via MCP.
/// </summary>
public static class DebugBreak
{
    private static string _serverUrl = "http://localhost:5200";
    private static bool _enabled = true;
    private static readonly HttpClient _httpClient = new();
    private static int _breakpointCounter;

    /// <summary>
    /// Configure the debug break behavior
    /// </summary>
    /// <param name="serverUrl">URL of the debugger server (default: http://localhost:5200)</param>
    /// <param name="enabled">Whether breakpoints are active (default: true)</param>
    public static void Configure(string serverUrl, bool enabled = true)
    {
        _serverUrl = serverUrl;
        _enabled = enabled;
    }

    /// <summary>
    /// Disable all breakpoints
    /// </summary>
    public static void Disable() => _enabled = false;

    /// <summary>
    /// Enable breakpoints
    /// </summary>
    public static void Enable() => _enabled = true;

    /// <summary>
    /// Synchronous breakpoint - blocks the current thread until resumed.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Here(
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int callerLine = 0,
        [CallerMemberName] string callerMember = "")
    {
        if (!_enabled)
        {
            return;
        }

        var context = CaptureContext(callerFile, callerLine, callerMember);

        try
        {
            // This will block until the server responds (when AI calls resume)
            var response = _httpClient.PostAsJsonAsync($"{_serverUrl}/break", context).Result;
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            // If we can't reach the server, just continue execution
            Console.Error.WriteLine($"[DebugBreak] Failed to connect to server: {ex.Message}");
        }
    }

    /// <summary>
    /// Asynchronous breakpoint - doesn't steal threadpool threads.
    /// Awaits until resumed by AI via MCP.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task HereAsync(
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int callerLine = 0,
        [CallerMemberName] string callerMember = "")
    {
        if (!_enabled)
        {
            return;
        }

        var context = CaptureContext(callerFile, callerLine, callerMember);

        try
        {
            // This will await until the server responds (when AI calls resume)
            var response = await _httpClient.PostAsJsonAsync($"{_serverUrl}/break", context);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            // If we can't reach the server, just continue execution
            Console.Error.WriteLine($"[DebugBreak] Failed to connect to server: {ex.Message}");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static BreakpointContext CaptureContext(string callerFile, int callerLine, string callerMember)
    {
        var id = $"bp-{Interlocked.Increment(ref _breakpointCounter):D4}-{DateTime.UtcNow:HHmmss}";

        // Capture stack trace with file info
        var stackTrace = new StackTrace(fNeedFileInfo: true);
        var frames = stackTrace.GetFrames();

        // Skip frames inside DebugBreak class
        var relevantFrames = frames
            .Skip(2) // Skip CaptureContext and Here/HereAsync
            .Where(f => f.GetMethod()?.DeclaringType != typeof(DebugBreak))
            .Select(f =>
            {
                var method = f.GetMethod();
                return new StackFrameInfo
                {
                    Method = method?.Name ?? "<unknown>",
                    Type = method?.DeclaringType?.FullName ?? "<unknown>",
                    File = f.GetFileName(),
                    Line = f.GetFileLineNumber(),
                    Column = f.GetFileColumnNumber()
                };
            })
            .ToArray();

        // Get the immediate caller's info from the stack (more accurate than caller attributes for nested calls)
        var callerFrame = relevantFrames.FirstOrDefault();
        var file = callerFrame?.File ?? callerFile;
        var line = callerFrame?.Line > 0 ? callerFrame.Line : callerLine;
        var column = callerFrame?.Column ?? 0;
        var method = callerFrame?.Method ?? callerMember;
        var type = callerFrame?.Type ?? "<unknown>";

        // Try to read source context
        var sourceContext = SourceReader.GetContext(file, line, contextLines: 5);

        return new BreakpointContext
        {
            Id = id,
            File = file,
            Line = line,
            Column = column,
            Method = method,
            Type = type,
            CallStack = relevantFrames,
            SourceLines = sourceContext?.Lines,
            SourceStartLine = sourceContext?.StartLine ?? 0,
            HitTime = DateTime.UtcNow
        };
    }
}
