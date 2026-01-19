namespace Asynkron.McpDebugger.Client;

/// <summary>
/// Utility for reading source code context from files
/// </summary>
public static class SourceReader
{
    /// <summary>
    /// Gets lines around a specific line number from a source file
    /// </summary>
    /// <param name="filePath">Path to the source file</param>
    /// <param name="line">1-based line number to center on</param>
    /// <param name="contextLines">Number of lines to include before and after</param>
    /// <returns>The source lines and the 1-based starting line number, or null if file not found</returns>
    public static (string[] Lines, int StartLine)? GetContext(string? filePath, int line, int contextLines = 5)
    {
        if (string.IsNullOrEmpty(filePath) || line <= 0)
        {
            return null;
        }

        try
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var allLines = File.ReadAllLines(filePath);

            if (allLines.Length == 0)
            {
                return null;
            }

            // Convert to 0-based index
            var lineIndex = line - 1;

            // Calculate range
            var startIndex = Math.Max(0, lineIndex - contextLines);
            var endIndex = Math.Min(allLines.Length - 1, lineIndex + contextLines);

            var count = endIndex - startIndex + 1;
            var lines = new string[count];
            Array.Copy(allLines, startIndex, lines, 0, count);

            // Return 1-based start line
            return (lines, startIndex + 1);
        }
        catch
        {
            // File read errors - return null gracefully
            return null;
        }
    }
}
