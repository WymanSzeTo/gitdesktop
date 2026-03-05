using GitDesktop.App.ViewModels;
using GitDesktop.Core;
using GitDesktop.Core.Execution;

namespace GitDesktop.App.Tests;

/// <summary>
/// Unit tests for <see cref="FilesViewModel"/>.
/// </summary>
public class FilesViewModelTests
{
    [Fact]
    public async Task RefreshAsync_PopulatesAllFiles()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("README.md\nsrc/Program.cs\nsrc/Utils.cs\n");

        var vm = new FilesViewModel(new GitDesktopClient(mock), "/repo");
        await vm.RefreshAsync();

        Assert.Equal(3, vm.AllFiles.Count);
        Assert.Contains("README.md", vm.AllFiles);
        Assert.Contains("src/Program.cs", vm.AllFiles);
    }

    [Fact]
    public async Task FilterText_Set_FiltersFileList()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("README.md\nsrc/Program.cs\nsrc/Utils.cs\n");

        var vm = new FilesViewModel(new GitDesktopClient(mock), "/repo");
        await vm.RefreshAsync();

        vm.FilterText = "src";

        Assert.Equal(2, vm.FilteredFiles.Count);
        Assert.DoesNotContain("README.md", vm.FilteredFiles);
    }

    [Fact]
    public async Task FilterText_Empty_ShowsAllFiles()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("README.md\nsrc/Program.cs\n");

        var vm = new FilesViewModel(new GitDesktopClient(mock), "/repo");
        await vm.RefreshAsync();

        vm.FilterText = "src";
        vm.FilterText = string.Empty;

        Assert.Equal(2, vm.FilteredFiles.Count);
    }

    [Fact]
    public async Task RefreshAsync_GitFailure_LeavesListEmpty()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueFailure("fatal: not a git repo", 128);

        var vm = new FilesViewModel(new GitDesktopClient(mock), "/bad");
        await vm.RefreshAsync();

        Assert.Empty(vm.AllFiles);
        Assert.Empty(vm.FilteredFiles);
    }

    [Fact]
    public void FilterText_Set_RaisesPropertyChanged()
    {
        var mock = new MockGitExecutor();
        var vm   = new FilesViewModel(new GitDesktopClient(mock), "/repo");

        string? prop = null;
        vm.PropertyChanged += (_, e) => prop = e.PropertyName;

        vm.FilterText = "hello";

        Assert.Equal(nameof(FilesViewModel.FilterText), prop);
    }

    [Fact]
    public async Task SelectedFile_Set_LoadsContentLines()
    {
        var mock = new MockGitExecutor();
        // First: ls-files response; second: show HEAD:README.md response
        mock.EnqueueSuccess("README.md\n");
        mock.EnqueueSuccess("# My Project\nThis is a readme.\n");

        var vm = new FilesViewModel(new GitDesktopClient(mock), "/repo");
        await vm.RefreshAsync();

        await vm.LoadFileContentAsync("README.md");

        Assert.NotEmpty(vm.ContentLines);
        Assert.Equal("# My Project", vm.ContentLines[0].Content);
        Assert.Equal("Markdown", vm.DetectedLanguage);
    }

    [Fact]
    public async Task SelectedFile_SetToNull_ClearsContentLines()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("public class Foo { }");

        var vm = new FilesViewModel(new GitDesktopClient(mock), "/repo");
        await vm.LoadFileContentAsync("Foo.cs");
        Assert.NotEmpty(vm.ContentLines);

        await vm.LoadFileContentAsync(null);

        Assert.Empty(vm.ContentLines);
    }

    [Fact]
    public async Task LoadFileContentAsync_GitFailure_LeavesContentEmpty()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueFailure("fatal: Path not found", 128);

        var vm = new FilesViewModel(new GitDesktopClient(mock), "/repo");
        await vm.LoadFileContentAsync("missing.txt");

        Assert.Empty(vm.ContentLines);
    }

    [Fact]
    public async Task LoadFileContentAsync_GitFailure_FallsBackToDiskFile()
    {
        // Write a temporary file to the working tree so the fallback path can read it.
        var tempDir  = Path.Combine(Path.GetTempPath(), $"GitDesktop_FV_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var fileName = "new_file.txt";
        var content  = "hello from disk\nsecond line\n";
        await File.WriteAllTextAsync(Path.Combine(tempDir, fileName), content);

        try
        {
            var mock = new MockGitExecutor();
            // git show HEAD:new_file.txt fails (file not yet committed)
            mock.EnqueueFailure("fatal: Path 'new_file.txt' does not exist in 'HEAD'", 128);

            var vm = new FilesViewModel(new GitDesktopClient(mock), tempDir);
            await vm.LoadFileContentAsync(fileName);

            // The fallback should have populated ContentLines from the disk file.
            Assert.NotEmpty(vm.ContentLines);
            Assert.Equal("hello from disk", vm.ContentLines[0].Content);
            Assert.Equal("Plain text", vm.DetectedLanguage);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task LoadFileContentAsync_PathEscapesRepository_DoesNotReadOutsideRepo()
    {
        var repoDir = Path.Combine(Path.GetTempPath(), $"GitDesktop_FV_{Guid.NewGuid():N}");
        var outsideDir = Path.Combine(Path.GetTempPath(), $"GitDesktop_FV_{Guid.NewGuid():N}");
        Directory.CreateDirectory(repoDir);
        Directory.CreateDirectory(outsideDir);
        await File.WriteAllTextAsync(Path.Combine(outsideDir, "secret.txt"), "should-not-read");

        try
        {
            var mock = new MockGitExecutor();
            mock.EnqueueFailure("fatal: Path not found", 128);

            var vm = new FilesViewModel(new GitDesktopClient(mock), repoDir);
            await vm.LoadFileContentAsync(Path.Combine("..", Path.GetFileName(outsideDir), "secret.txt"));

            Assert.Empty(vm.ContentLines);
        }
        finally
        {
            Directory.Delete(repoDir, recursive: true);
            Directory.Delete(outsideDir, recursive: true);
        }
    }

    [Fact]
    public void ClassifyLine_CommentLine_ReturnsComment()
    {
        Assert.Equal(FileLineKind.Comment, FilesViewModel.ClassifyLine("// a comment", SourceLanguage.CSharp));
        Assert.Equal(FileLineKind.Comment, FilesViewModel.ClassifyLine("# shell comment", SourceLanguage.Shell));
        Assert.Equal(FileLineKind.Comment, FilesViewModel.ClassifyLine("/* block */", SourceLanguage.JavaScript));
        Assert.Equal(FileLineKind.Comment, FilesViewModel.ClassifyLine("-- SQL comment", SourceLanguage.Sql));
    }

    [Fact]
    public void ClassifyLine_KeywordLine_ReturnsKeyword()
    {
        Assert.Equal(FileLineKind.Keyword, FilesViewModel.ClassifyLine("public class Foo", SourceLanguage.CSharp));
        Assert.Equal(FileLineKind.Keyword, FilesViewModel.ClassifyLine("namespace MyApp", SourceLanguage.CSharp));
        Assert.Equal(FileLineKind.Keyword, FilesViewModel.ClassifyLine("return value;", SourceLanguage.JavaScript));
    }

    [Fact]
    public void ClassifyLine_CodeLine_ReturnsCode()
    {
        Assert.Equal(FileLineKind.Code, FilesViewModel.ClassifyLine("    x = 42;", SourceLanguage.CSharp));
        Assert.Equal(FileLineKind.Code, FilesViewModel.ClassifyLine("", SourceLanguage.CSharp));
    }

    [Fact]
    public void FileLineViewModel_CommentKind_HasSecondaryTextKey()
    {
        var vm = new FileLineViewModel("// comment", FileLineKind.Comment);
        Assert.Equal("ThemeSyntaxComment", vm.ForegroundKey);
    }

    [Fact]
    public void FileLineViewModel_KeywordKind_HasAccentColorKey()
    {
        var vm = new FileLineViewModel("public class", FileLineKind.Keyword);
        Assert.Equal("ThemeSyntaxKeyword", vm.ForegroundKey);
    }

    [Fact]
    public void FileLineViewModel_CodeKind_HasPrimaryTextKey()
    {
        var vm = new FileLineViewModel("    x = 1;", FileLineKind.Code);
        Assert.Equal("ThemePrimaryText", vm.ForegroundKey);
    }

    [Fact]
    public void SourceSyntaxClassifier_DetectLanguage_FromExtension()
    {
        Assert.Equal(SourceLanguage.CSharp, SourceSyntaxClassifier.DetectLanguage("Program.cs"));
        Assert.Equal(SourceLanguage.TypeScript, SourceSyntaxClassifier.DetectLanguage("main.ts"));
        Assert.Equal(SourceLanguage.Unknown, SourceSyntaxClassifier.DetectLanguage("README"));
    }
}
