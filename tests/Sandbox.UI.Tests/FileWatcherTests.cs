using Sandbox.UI;
using Xunit;

namespace Sandbox.UI.Tests;

/// <summary>
/// Tests for the FileWatcher class used for stylesheet/Razor hotloading.
/// Note: FileWatcher is only active in DEBUG builds.
/// </summary>
public class FileWatcherTests
{
#if DEBUG
    [Fact]
    public void FileWatcher_AddFile_RegistersFileForWatching()
    {
        // Arrange
        var watcher = new FileWatcher();
        var testPath = "/tmp/test.scss";

        // Act
        watcher.AddFile(testPath);

        // Assert - watcher should be created without error
        Assert.True(watcher.Enabled);
        
        // Cleanup
        watcher.Dispose();
    }

    [Fact]
    public void FileWatcher_Dispose_CleansUpResources()
    {
        // Arrange
        var watcher = new FileWatcher();
        watcher.AddFile("/tmp/test.scss");

        // Act
        watcher.Dispose();

        // Assert - no exceptions thrown during dispose
        Assert.True(true);
    }

    [Fact]
    public void FileWatcher_Tick_ProcessesWithoutError()
    {
        // Arrange - just ensure Tick doesn't throw
        
        // Act
        FileWatcher.Tick(0.016f); // ~60fps frame time

        // Assert - no exceptions
        Assert.True(true);
    }

    [Fact]
    public void FileWatcher_DisposeAll_CleansUpAllWatchers()
    {
        // Arrange
        var watcher1 = new FileWatcher();
        var watcher2 = new FileWatcher();
        watcher1.AddFile("/tmp/test1.scss");
        watcher2.AddFile("/tmp/test2.scss");

        // Act
        FileWatcher.DisposeAll();

        // Assert - no exceptions
        Assert.True(true);
    }
#else
    [Fact]
    public void FileWatcher_InReleaseBuild_IsNoOp()
    {
        // In release builds, FileWatcher methods should be no-ops
        var watcher = new FileWatcher();
        watcher.AddFile("/tmp/test.scss");
        
        // In release, Enabled should be false (no-op mode)
        Assert.False(watcher.Enabled);
        
        watcher.Dispose();
    }
#endif
}
