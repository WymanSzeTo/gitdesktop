namespace GitDesktop.Core.Execution;

/// <summary>
/// In-memory mock executor for use in tests.
/// </summary>
public sealed class MockGitExecutor : IGitExecutor
{
    private readonly Queue<GitResult> _results = new();
    private readonly List<(string WorkingDirectory, string Arguments)> _calls = new();

    public IReadOnlyList<(string WorkingDirectory, string Arguments)> Calls => _calls.AsReadOnly();

    public void Enqueue(GitResult result) => _results.Enqueue(result);
    public void EnqueueSuccess(string output = "") => _results.Enqueue(GitResult.Ok(output));
    public void EnqueueFailure(string error = "error", int exitCode = 1) => _results.Enqueue(GitResult.Fail(error, exitCode));

    public Task<GitResult> ExecuteAsync(string workingDirectory, string arguments, CancellationToken cancellationToken = default)
    {
        _calls.Add((workingDirectory, arguments));
        var result = _results.Count > 0 ? _results.Dequeue() : GitResult.Ok();
        return Task.FromResult(result);
    }

    public Task<GitResult> ExecuteWithInputAsync(string workingDirectory, string arguments, string input, CancellationToken cancellationToken = default)
    {
        _calls.Add((workingDirectory, arguments));
        var result = _results.Count > 0 ? _results.Dequeue() : GitResult.Ok();
        return Task.FromResult(result);
    }
}
