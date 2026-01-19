using Asynkron.McpDebugger.Client;

namespace Asynkron.McpDebugger.Server;

/// <summary>
/// HTTP server that handles client breakpoint connections
/// </summary>
public class DebugServer
{
    private readonly BreakpointStore _store;

    public DebugServer(BreakpointStore store)
    {
        _store = store;
    }

    public async Task RunAsync(int port, CancellationToken ct)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenLocalhost(port);
        });

        var app = builder.Build();

        // POST /break - Client hits breakpoint, waits for resume
        app.MapPost("/break", async (BreakpointContext context) =>
        {
            Console.WriteLine($"[Breakpoint] {context.Id} hit at {context.Type}.{context.Method} ({context.File}:{context.Line})");

            // Add to store and wait for resume signal
            await _store.Add(context);

            Console.WriteLine($"[Breakpoint] {context.Id} resumed");
            return Results.Ok(new { resumed = true });
        });

        // GET /status - Health check and list active breakpoints
        app.MapGet("/status", () =>
        {
            var breakpoints = _store.GetAll();
            return Results.Ok(new
            {
                status = "running",
                activeBreakpoints = breakpoints.Length,
                breakpoints = breakpoints.Select(b => new
                {
                    b.Id,
                    b.File,
                    b.Line,
                    b.Column,
                    b.Method,
                    b.Type,
                    b.HitTime,
                    b.SourceStartLine,
                    b.SourceLines,
                    b.CallStack
                })
            });
        });

        // POST /resume/{id} - Resume a specific breakpoint (for direct HTTP access)
        app.MapPost("/resume/{id}", (string id) =>
        {
            if (_store.TryResume(id))
            {
                return Results.Ok(new { resumed = true, id });
            }
            return Results.NotFound(new { error = $"Breakpoint '{id}' not found" });
        });

        // POST /resume-all - Resume all breakpoints (for direct HTTP access)
        app.MapPost("/resume-all", () =>
        {
            var count = _store.ResumeAll();
            return Results.Ok(new { resumed = count });
        });

        Console.WriteLine($"[DebugServer] Listening on http://localhost:{port}");
        await app.RunAsync(ct);
    }
}
