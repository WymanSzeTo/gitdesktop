using GitDesktop.Core.Execution;
using GitDesktop.Core.Services;

namespace GitDesktop.Core;

/// <summary>
/// Top-level client that aggregates all Git Desktop services.
/// Provides a single entry point for all git operations.
/// </summary>
public sealed class GitDesktopClient
{
    public IGitExecutor Executor { get; }

    public RepositoryService Repository { get; }
    public CommitService Commit { get; }
    public BranchService Branch { get; }
    public RemoteService Remote { get; }
    public HistoryService History { get; }
    public MergeRebaseService MergeRebase { get; }
    public WorkTreeSubmoduleService WorkTreeSubmodule { get; }
    public ConfigService Config { get; }
    public BisectService Bisect { get; }
    public HooksService Hooks { get; }
    public LfsService Lfs { get; }
    public AdvancedService Advanced { get; }

    public GitDesktopClient(IGitExecutor? executor = null)
    {
        Executor = executor ?? new GitProcessExecutor();
        Repository = new RepositoryService(Executor);
        Commit = new CommitService(Executor);
        Branch = new BranchService(Executor);
        Remote = new RemoteService(Executor);
        History = new HistoryService(Executor);
        MergeRebase = new MergeRebaseService(Executor);
        WorkTreeSubmodule = new WorkTreeSubmoduleService(Executor);
        Config = new ConfigService(Executor);
        Bisect = new BisectService(Executor);
        Hooks = new HooksService(Executor);
        Lfs = new LfsService(Executor);
        Advanced = new AdvancedService(Executor);
    }
}
