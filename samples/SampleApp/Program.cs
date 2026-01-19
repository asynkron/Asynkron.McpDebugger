using Asynkron.McpDebugger.Client;

Console.WriteLine("=== MCP Debugger Sample Application ===");
Console.WriteLine();
Console.WriteLine("This app demonstrates cooperative breakpoints controlled by AI via MCP.");
Console.WriteLine("Make sure 'mcpdebugger serve' is running before starting this app.");
Console.WriteLine();

// Optional: Configure the server URL (default is http://localhost:5200)
// DebugBreak.Configure("http://localhost:5200");

Console.WriteLine("Starting execution...");
Console.WriteLine();

// Sync breakpoint example
Console.WriteLine("1. About to hit a synchronous breakpoint...");
DebugBreak.Here();
Console.WriteLine("   Resumed from sync breakpoint!");
Console.WriteLine();

// Async breakpoint example
Console.WriteLine("2. About to hit an asynchronous breakpoint...");
await DebugBreak.HereAsync();
Console.WriteLine("   Resumed from async breakpoint!");
Console.WriteLine();

// Nested method example
Console.WriteLine("3. Calling a method with a breakpoint inside...");
await DoSomeWorkAsync();
Console.WriteLine();

// Loop with breakpoints
Console.WriteLine("4. Running a loop with breakpoints...");
for (var i = 1; i <= 3; i++)
{
    Console.WriteLine($"   Iteration {i}: hitting breakpoint...");
    await DebugBreak.HereAsync();
    Console.WriteLine($"   Iteration {i}: resumed!");
}
Console.WriteLine();

Console.WriteLine("=== All breakpoints completed! ===");

static async Task DoSomeWorkAsync()
{
    Console.WriteLine("   Inside DoSomeWorkAsync - about to hit breakpoint...");
    await DebugBreak.HereAsync();
    Console.WriteLine("   DoSomeWorkAsync - resumed and completing!");
}
