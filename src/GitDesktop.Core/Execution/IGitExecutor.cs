namespace GitDesktop.Core.Execution;

/// <summary>
/// Abstraction for executing git commands.
/// </summary>
public interface IGitExecutor
{
    /// <summary>
    /// Executes a git command in the given working directory.
    /// </summary>
    Task<GitResult> ExecuteAsync(string workingDirectory, string arguments, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a git command with standard input piped in.
    /// </summary>
    Task<GitResult> ExecuteWithInputAsync(string workingDirectory, string arguments, string input, CancellationToken cancellationToken = default);
}
