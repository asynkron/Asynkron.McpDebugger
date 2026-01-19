using System.Collections.Concurrent;
using Asynkron.McpDebugger.Client;

namespace Asynkron.McpDebugger.Server;

/// <summary>
/// Thread-safe in-memory store for active breakpoints
/// </summary>
public class BreakpointStore
{
    private readonly ConcurrentDictionary<string, BreakpointEntry> _breakpoints = new();

    public record BreakpointEntry(BreakpointContext Context, TaskCompletionSource<bool> Tcs);

    /// <summary>
    /// Add a new breakpoint and return a task that completes when resumed
    /// </summary>
    public Task Add(BreakpointContext context)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entry = new BreakpointEntry(context, tcs);

        _breakpoints[context.Id] = entry;

        return tcs.Task;
    }

    /// <summary>
    /// Resume a specific breakpoint
    /// </summary>
    /// <returns>True if the breakpoint existed and was resumed</returns>
    public bool TryResume(string id)
    {
        if (_breakpoints.TryRemove(id, out var entry))
        {
            entry.Tcs.TrySetResult(true);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Resume all active breakpoints
    /// </summary>
    /// <returns>Number of breakpoints resumed</returns>
    public int ResumeAll()
    {
        var count = 0;
        foreach (var kvp in _breakpoints.ToArray())
        {
            if (_breakpoints.TryRemove(kvp.Key, out var entry))
            {
                entry.Tcs.TrySetResult(true);
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Get all active breakpoints
    /// </summary>
    public BreakpointContext[] GetAll()
    {
        return _breakpoints.Values.Select(e => e.Context).ToArray();
    }

    /// <summary>
    /// Get a specific breakpoint by ID
    /// </summary>
    public BreakpointContext? Get(string id)
    {
        return _breakpoints.TryGetValue(id, out var entry) ? entry.Context : null;
    }

    /// <summary>
    /// Number of active breakpoints
    /// </summary>
    public int Count => _breakpoints.Count;
}
