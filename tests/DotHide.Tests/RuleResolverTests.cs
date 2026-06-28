using DotHide.Models;
using DotHide.Services;

namespace DotHide.Tests;

public class RuleResolverTests
{
    [Fact]
    public void NoMatchingRules_ReturnsGlobalState()
    {
        var config = new AppConfig { GlobalState = RuleState.Hide };
        var result = RuleResolver.Resolve(@"C:\some\random\file.txt", config);
        Assert.Equal(RuleState.Hide, result.DesiredState);
        Assert.Equal(ResolutionSource.Global, result.Source);
    }

    [Fact]
    public void NameRule_BeatsGlobalState()
    {
        var config = new AppConfig { GlobalState = RuleState.Show };
        config.NameRules.Add(new NameRule { Name = ".obsidian", State = RuleState.Hide });
        var result = RuleResolver.Resolve(@"C:\Users\test\.obsidian", config);
        Assert.Equal(RuleState.Hide, result.DesiredState);
        Assert.Equal(ResolutionSource.Name, result.Source);
    }

    [Fact]
    public void GlobalException_IsOppositeOfGlobalState()
    {
        var config = new AppConfig
        {
            GlobalState = RuleState.Hide,
            GlobalExceptions = new List<string> { ".obsidian" }
        };

        var result = RuleResolver.Resolve(@"C:\Users\test\.obsidian", config);

        Assert.Equal(RuleState.Show, result.DesiredState);
        Assert.Equal(ResolutionSource.Global, result.Source);
        Assert.Contains("exception", result.SourceDescription, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FolderRule_BeatsNameRule()
    {
        var config = new AppConfig { GlobalState = RuleState.Hide };
        config.NameRules.Add(new NameRule { Name = ".obsidian", State = RuleState.Hide });
        config.FolderRules.Add(new FolderRule { Path = @"C:\A", State = RuleState.Show });
        var result = RuleResolver.Resolve(@"C:\A\.obsidian", config);
        Assert.Equal(RuleState.Show, result.DesiredState);
        Assert.Equal(ResolutionSource.Folder, result.Source);
    }

    [Fact]
    public void FolderException_IsOppositeOfFolderState()
    {
        var config = new AppConfig { GlobalState = RuleState.Hide };
        config.FolderRules.Add(new FolderRule
        {
            Path = @"C:\A",
            State = RuleState.Show,
            Exceptions = new List<string> { ".obsidian" }
        });

        var result = RuleResolver.Resolve(@"C:\A\.obsidian", config);

        Assert.Equal(RuleState.Hide, result.DesiredState);
        Assert.Equal(ResolutionSource.Folder, result.Source);
        Assert.Contains("exception", result.SourceDescription, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FolderException_BeatsGlobalException()
    {
        var config = new AppConfig
        {
            GlobalState = RuleState.Hide,
            GlobalExceptions = new List<string> { ".obsidian" }
        };
        config.FolderRules.Add(new FolderRule
        {
            Path = @"C:\A",
            State = RuleState.Show,
            Exceptions = new List<string> { ".obsidian" }
        });

        var result = RuleResolver.Resolve(@"C:\A\.obsidian", config);

        Assert.Equal(RuleState.Hide, result.DesiredState);
        Assert.Equal(ResolutionSource.Folder, result.Source);
    }

    [Fact]
    public void DeeperFolderRule_BeatsShallowerFolderRule()
    {
        var config = new AppConfig { GlobalState = RuleState.Show };
        config.FolderRules.Add(new FolderRule { Path = @"C:\A", State = RuleState.Show });
        config.FolderRules.Add(new FolderRule { Path = @"C:\A\B", State = RuleState.Hide });
        var result = RuleResolver.Resolve(@"C:\A\B\file.txt", config);
        Assert.Equal(RuleState.Hide, result.DesiredState);
        Assert.Equal(ResolutionSource.Folder, result.Source);
    }

    [Fact]
    public void ExactPathRule_BeatsFolderRule()
    {
        var config = new AppConfig { GlobalState = RuleState.Show };
        config.FolderRules.Add(new FolderRule { Path = @"C:\A", State = RuleState.Show });
        config.ExactPathRules.Add(new ExactPathRule { Path = @"C:\A\.obsidian", State = RuleState.Hide });
        var result = RuleResolver.Resolve(@"C:\A\.obsidian", config);
        Assert.Equal(RuleState.Hide, result.DesiredState);
        Assert.Equal(ResolutionSource.Exact, result.Source);
    }

    [Fact]
    public void ExactPathRule_CanMatchDirectory()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"dotHide_exact_{Guid.NewGuid()}");
        var folder = Path.Combine(tempRoot, ".obsidian");
        Directory.CreateDirectory(folder);
        try
        {
            var config = new AppConfig { GlobalState = RuleState.Show };
            config.FolderRules.Add(new FolderRule { Path = tempRoot, State = RuleState.Show });
            config.ExactPathRules.Add(new ExactPathRule { Path = folder, State = RuleState.Hide });

            var result = RuleResolver.Resolve(folder, config);

            Assert.Equal(RuleState.Hide, result.DesiredState);
            Assert.Equal(ResolutionSource.Exact, result.Source);
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void ExactPath_CaseInsensitiveMatch()
    {
        var config = new AppConfig { GlobalState = RuleState.Show };
        config.ExactPathRules.Add(new ExactPathRule { Path = @"C:\A\.Obsidian", State = RuleState.Hide });
        var result = RuleResolver.Resolve(@"c:\a\.obsidian", config);
        Assert.Equal(RuleState.Hide, result.DesiredState);
        Assert.Equal(ResolutionSource.Exact, result.Source);
    }

    [Fact]
    public void FolderAncestor_DoesNotMatchSiblingPrefix()
    {
        var config = new AppConfig { GlobalState = RuleState.Show };
        config.FolderRules.Add(new FolderRule { Path = @"C:\A", State = RuleState.Hide });
        var result = RuleResolver.Resolve(@"C:\ABC\file.txt", config);
        Assert.Equal(RuleState.Show, result.DesiredState);
        Assert.Equal(ResolutionSource.Global, result.Source);
    }
}
