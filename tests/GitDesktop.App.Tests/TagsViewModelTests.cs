using GitDesktop.App.ViewModels;
using GitDesktop.Core;
using GitDesktop.Core.Execution;

namespace GitDesktop.App.Tests;

/// <summary>
/// Unit tests for <see cref="TagsViewModel"/>.
/// </summary>
public class TagsViewModelTests
{
    [Fact]
    public async Task RefreshAsync_PopulatesTags()
    {
        var mock = new MockGitExecutor();
        // Format: %(refname:short)|%(objecttype)|%(*objectname)|%(objectname)|%(taggername)|%(taggeremail)|%(taggerdate:iso)|%(contents:subject)
        mock.EnqueueSuccess("v1.0|commit||abc1234||||\nv2.0|tag|def5678||John|<john@test.com>|2024-01-01 12:00:00 +0000|Release 2.0\n");

        var vm = new TagsViewModel(new GitDesktopClient(mock), "/repo");
        await vm.RefreshAsync();

        Assert.Equal(2, vm.Tags.Count);
        Assert.Equal("v1.0", vm.Tags[0].Name);
        Assert.Equal("v2.0", vm.Tags[1].Name);
        Assert.True(vm.Tags[1].IsAnnotated);
    }

    [Fact]
    public async Task RefreshAsync_NoTags_EmptyCollection()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("");

        var vm = new TagsViewModel(new GitDesktopClient(mock), "/repo");
        await vm.RefreshAsync();

        Assert.Empty(vm.Tags);
    }

    [Fact]
    public void CreateLightweightTagCommand_CannotExecute_WhenNameEmpty()
    {
        var mock = new MockGitExecutor();
        var vm = new TagsViewModel(new GitDesktopClient(mock), "/repo");

        vm.NewTagName = string.Empty;

        Assert.False(vm.CreateLightweightTagCommand.CanExecute(null));
    }

    [Fact]
    public void CreateLightweightTagCommand_CanExecute_WhenNameProvided()
    {
        var mock = new MockGitExecutor();
        var vm = new TagsViewModel(new GitDesktopClient(mock), "/repo");

        vm.NewTagName = "v3.0";

        Assert.True(vm.CreateLightweightTagCommand.CanExecute(null));
    }

    [Fact]
    public void CreateAnnotatedTagCommand_CannotExecute_WhenMessageEmpty()
    {
        var mock = new MockGitExecutor();
        var vm = new TagsViewModel(new GitDesktopClient(mock), "/repo");

        vm.NewTagName = "v3.0";
        vm.NewTagMessage = string.Empty;

        Assert.False(vm.CreateAnnotatedTagCommand.CanExecute(null));
    }

    [Fact]
    public void CreateAnnotatedTagCommand_CanExecute_WhenNameAndMessageProvided()
    {
        var mock = new MockGitExecutor();
        var vm = new TagsViewModel(new GitDesktopClient(mock), "/repo");

        vm.NewTagName = "v3.0";
        vm.NewTagMessage = "Release 3.0";

        Assert.True(vm.CreateAnnotatedTagCommand.CanExecute(null));
    }

    [Fact]
    public void NewTagName_Set_RaisesPropertyChanged()
    {
        var mock = new MockGitExecutor();
        var vm = new TagsViewModel(new GitDesktopClient(mock), "/repo");

        string? changedProperty = null;
        vm.PropertyChanged += (_, e) => changedProperty = e.PropertyName;

        vm.NewTagName = "v1.0";

        Assert.Equal(nameof(TagsViewModel.NewTagName), changedProperty);
    }
}
