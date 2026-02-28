using System.Diagnostics;
using System.Text;

namespace GitDesktop.Core.Execution;

/// <summary>
/// Executes git commands by spawning the git process.
/// </summary>
public sealed class GitProcessExecutor : IGitExecutor
{
    private readonly string _gitExecutable;

    public GitProcessExecutor(string gitExecutable = "git")
    {
        _gitExecutable = gitExecutable;
    }

    public async Task<GitResult> ExecuteAsync(string workingDirectory, string arguments, CancellationToken cancellationToken = default)
    {
        return await RunAsync(workingDirectory, arguments, null, cancellationToken);
    }

    public async Task<GitResult> ExecuteWithInputAsync(string workingDirectory, string arguments, string input, CancellationToken cancellationToken = default)
    {
        return await RunAsync(workingDirectory, arguments, input, cancellationToken);
    }

    private async Task<GitResult> RunAsync(string workingDirectory, string arguments, string? input, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo(_gitExecutable, arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = input != null,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        psi.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";

        using var process = new Process { StartInfo = psi };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (input != null)
        {
            await process.StandardInput.WriteAsync(input);
            process.StandardInput.Close();
        }

        await process.WaitForExitAsync(cancellationToken);

        return new GitResult
        {
            ExitCode = process.ExitCode,
            Output = outputBuilder.ToString().TrimEnd(),
            Error = errorBuilder.ToString().TrimEnd(),
        };
    }
}
