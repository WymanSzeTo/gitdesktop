namespace GitDesktop.Core.Execution;

/// <summary>
/// Represents the result of a git command execution.
/// </summary>
public sealed class GitResult
{
    public int ExitCode { get; init; }
    public string Output { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
    public bool Success => ExitCode == 0;

    public static GitResult Ok(string output = "") => new() { ExitCode = 0, Output = output };
    public static GitResult Fail(string error, int exitCode = 1) => new() { ExitCode = exitCode, Error = error };
}
