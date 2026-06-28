using DotHide.Services;
using Xunit;

namespace DotHide.Tests;

public class PathHelperTests
{
    [Fact]
    public void NormalizePath_TrimsTrailingSlash()
    {
        var result = PathHelper.NormalizePath(@"C:\Users\test\");
        Assert.Equal(@"C:\Users\test", result);
    }

    [Fact]
    public void NormalizePath_TrimsTrailingBackslash()
    {
        var result = PathHelper.NormalizePath(@"C:\Users\test\\");
        Assert.Equal(@"C:\Users\test", result);
    }

    [Fact]
    public void NormalizePath_ConvertsRelativeToAbsolute()
    {
        var result = PathHelper.NormalizePath(@"test\file.txt");
        Assert.True(Path.IsPathFullyQualified(result));
    }

    [Fact]
    public void IsAncestorOf_DirectChild_ReturnsTrue()
    {
        Assert.True(PathHelper.IsAncestorOf(@"C:\A", @"C:\A\file.txt"));
    }

    [Fact]
    public void IsAncestorOf_DeepChild_ReturnsTrue()
    {
        Assert.True(PathHelper.IsAncestorOf(@"C:\A", @"C:\A\B\C\file.txt"));
    }

    [Fact]
    public void IsAncestorOf_SiblingPrefix_ReturnsFalse()
    {
        // C:\ABC must NOT match as child of C:\A
        Assert.False(PathHelper.IsAncestorOf(@"C:\A", @"C:\ABC\file.txt"));
    }

    [Fact]
    public void IsAncestorOf_SamePath_ReturnsTrue()
    {
        Assert.True(PathHelper.IsAncestorOf(@"C:\A", @"C:\A"));
    }

    [Fact]
    public void IsAncestorOf_NotChild_ReturnsFalse()
    {
        Assert.False(PathHelper.IsAncestorOf(@"C:\A", @"D:\file.txt"));
    }

    [Fact]
    public void PathsEqual_IsCaseInsensitive()
    {
        Assert.True(PathHelper.PathsEqual(@"C:\A\.Obsidian", @"c:\a\.obsidian"));
    }
}
