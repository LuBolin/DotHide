using DotHide.Models;
using DotHide.Services;
using Xunit;

namespace DotHide.Tests;

public class ManagedItemStoreTests
{
    [Fact]
    public void RecordOriginal_FirstTime_RecordsAttributes()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dotHide_store_{Guid.NewGuid()}");
        Directory.CreateDirectory(path);
        var config = new AppConfig();
        try
        {
            ManagedItemStore.RecordOriginal(path, config);
            Assert.Single(config.ManagedItems);
            Assert.Equal(path, config.ManagedItems[0].Path, ignoreCase: true);
            Assert.False(config.ManagedItems[0].OriginalHidden); // normal folder isn't hidden
        }
        finally { Directory.Delete(path, true); }
    }

    [Fact]
    public void RecordOriginal_SecondTime_DoesNotOverwrite()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dotHide_store_{Guid.NewGuid()}");
        Directory.CreateDirectory(path);
        var config = new AppConfig();
        try
        {
            // First: hide it
            File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden);
            ManagedItemStore.RecordOriginal(path, config);
            Assert.Single(config.ManagedItems);
            Assert.True(config.ManagedItems[0].OriginalHidden); // recorded while hidden
            
            // Second: show it, then record again (should NOT overwrite original)
            File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.Hidden);
            ManagedItemStore.RecordOriginal(path, config);
            Assert.Single(config.ManagedItems);
            // OriginalHidden should still be true (recorded first time when hidden)
            Assert.True(config.ManagedItems[0].OriginalHidden);
        }
        finally { Directory.Delete(path, true); }
    }

    [Fact]
    public void RestoreAll_RestoresOriginalAttributes()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dotHide_store_{Guid.NewGuid()}");
        Directory.CreateDirectory(path);
        var config = new AppConfig();
        try
        {
            var origAttrs = File.GetAttributes(path);
            ManagedItemStore.RecordOriginal(path, config);
            
            // Change attributes
            File.SetAttributes(path, FileAttributes.Hidden | FileAttributes.System);
            Assert.Single(config.ManagedItems);
            
            // Restore
            var summary = ManagedItemStore.RestoreAll(config);
            Assert.Equal(1, summary.RestoredCount);
            
            var attrs = File.GetAttributes(path);
            Assert.False((attrs & FileAttributes.Hidden) != 0);
        }
        finally { Directory.Delete(path, true); }
    }

    [Fact]
    public void RestoreAll_PreservesUnrelatedAttributes()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dotHide_store_{Guid.NewGuid()}.txt");
        File.WriteAllText(path, "test");
        var config = new AppConfig();
        try
        {
            File.SetAttributes(path, FileAttributes.ReadOnly);
            ManagedItemStore.RecordOriginal(path, config);

            File.SetAttributes(path, FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.System);
            var summary = ManagedItemStore.RestoreAll(config);

            Assert.Equal(1, summary.RestoredCount);
            var attrs = File.GetAttributes(path);
            Assert.True((attrs & FileAttributes.ReadOnly) != 0);
            Assert.False((attrs & FileAttributes.Hidden) != 0);
            Assert.False((attrs & FileAttributes.System) != 0);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void RestoreAll_MissingPath_SkipsWithoutCrash()
    {
        var config = new AppConfig();
        config.ManagedItems.Add(new ManagedItem { Path = @"C:\nonexistent_path_xyz123" });
        var summary = ManagedItemStore.RestoreAll(config);
        Assert.Equal(1, summary.SkippedCount);
        Assert.Equal(0, summary.FailedCount);
    }

    [Fact]
    public void ApplyChanges_RecordsOriginalsAndApplies()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"dotHide_store_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempRoot);
        var dotFolder = Path.Combine(tempRoot, ".testfolder");
        Directory.CreateDirectory(dotFolder);
        var config = new AppConfig();
        try
        {
            var results = new List<ScanResult>
            {
                new() { Path = dotFolder, Name = ".testfolder", PendingAction = PendingAction.Hide }
            };
            var summary = ManagedItemStore.ApplyChanges(results, config);
            Assert.Equal(1, summary.HiddenCount);
            Assert.Single(config.ManagedItems);
            var attrs = File.GetAttributes(dotFolder);
            Assert.True((attrs & FileAttributes.Hidden) != 0);
        }
        finally { Directory.Delete(tempRoot, true); }
    }

    [Fact]
    public void ApplyChanges_FolderRuleHide_UsesStrongerModeForFolders()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"dotHide_store_{Guid.NewGuid()}");
        var dotFolder = Path.Combine(tempRoot, ".obsidian");
        Directory.CreateDirectory(dotFolder);
        var config = new AppConfig
        {
            UseSystemAttribute = true,
            Roots = new List<string> { tempRoot },
            FolderRules = new List<FolderRule> { new() { Path = tempRoot, State = RuleState.Hide } }
        };

        try
        {
            var result = Assert.Single(FileScanner.Scan(config));
            Assert.Equal(PendingAction.Hide, result.PendingAction);

            ManagedItemStore.ApplyChanges(new List<ScanResult> { result }, config);

            var attrs = File.GetAttributes(dotFolder);
            Assert.True((attrs & FileAttributes.Hidden) != 0);
            Assert.True((attrs & FileAttributes.System) != 0);
        }
        finally
        {
            if (Directory.Exists(dotFolder))
                File.SetAttributes(dotFolder, FileAttributes.Normal);
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void ApplyChanges_FolderExceptionShow_UsesStrongerModeForFolders()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"dotHide_store_{Guid.NewGuid()}");
        var dotFolder = Path.Combine(tempRoot, ".obsidian");
        Directory.CreateDirectory(dotFolder);
        File.SetAttributes(dotFolder, FileAttributes.Hidden | FileAttributes.System | FileAttributes.Directory);
        var config = new AppConfig
        {
            UseSystemAttribute = true,
            Roots = new List<string> { tempRoot },
            FolderRules = new List<FolderRule>
            {
                new() { Path = tempRoot, State = RuleState.Hide, Exceptions = new List<string> { ".obsidian" } }
            }
        };

        try
        {
            var result = Assert.Single(FileScanner.Scan(config));
            Assert.Equal(RuleState.Show, result.DesiredState);
            Assert.Equal(PendingAction.Show, result.PendingAction);

            ManagedItemStore.ApplyChanges(new List<ScanResult> { result }, config);

            var attrs = File.GetAttributes(dotFolder);
            Assert.False((attrs & FileAttributes.Hidden) != 0);
            Assert.False((attrs & FileAttributes.System) != 0);
        }
        finally
        {
            if (Directory.Exists(dotFolder))
                File.SetAttributes(dotFolder, FileAttributes.Normal);
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, true);
        }
    }
}
