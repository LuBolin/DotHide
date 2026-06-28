using DotHide.Services;
using Xunit;

namespace DotHide.Tests;

public class AttributeServiceTests
{
    [Fact]
    public void SetHidden_AddsHiddenAttribute()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dotHide_attr_{Guid.NewGuid()}.txt");
        File.WriteAllText(path, "test");
        try
        {
            AttributeService.SetHidden(path, true, false);
            var attrs = File.GetAttributes(path);
            Assert.True((attrs & FileAttributes.Hidden) != 0);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void SetHidden_Show_RemovesHiddenAttribute()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dotHide_attr_{Guid.NewGuid()}.txt");
        File.WriteAllText(path, "test");
        File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden);
        try
        {
            AttributeService.SetHidden(path, false, false);
            var attrs = File.GetAttributes(path);
            Assert.False((attrs & FileAttributes.Hidden) != 0);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void SetHidden_StrongerMode_AddsHiddenAndSystem()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dotHide_attr_{Guid.NewGuid()}.txt");
        File.WriteAllText(path, "test");
        try
        {
            AttributeService.SetHidden(path, true, true);
            var attrs = File.GetAttributes(path);
            Assert.True((attrs & FileAttributes.Hidden) != 0);
            Assert.True((attrs & FileAttributes.System) != 0);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void SetHidden_StrongerShow_RemovesBothAttributes()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dotHide_attr_{Guid.NewGuid()}.txt");
        File.WriteAllText(path, "test");
        File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden | FileAttributes.System);
        try
        {
            AttributeService.SetHidden(path, false, true);
            var attrs = File.GetAttributes(path);
            Assert.False((attrs & FileAttributes.Hidden) != 0);
            Assert.False((attrs & FileAttributes.System) != 0);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void SetHidden_MissingPath_ReturnsError()
    {
        var result = AttributeService.SetHidden(@"C:\nonexistent_path_xyz.txt", true, false);
        Assert.False(result.Success);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SetHidden_ReadOnlyFile_HandlesGracefully()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dotHide_attr_{Guid.NewGuid()}.txt");
        File.WriteAllText(path, "test");
        File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);
        try
        {
            AttributeService.SetHidden(path, true, false);
            var attrs = File.GetAttributes(path);
            Assert.True((attrs & FileAttributes.Hidden) != 0);
            Assert.True((attrs & FileAttributes.ReadOnly) != 0); // ReadOnly preserved
        }
        finally
        {
            var attrs = File.GetAttributes(path);
            attrs &= ~FileAttributes.ReadOnly;
            File.SetAttributes(path, attrs);
            File.Delete(path);
        }
    }
}