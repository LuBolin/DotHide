using System.IO;
using DotHide.Models;
using DotHide.Services;
using Xunit;

namespace DotHide.Tests;

public class FileScannerTests
{
    [Fact]
    public void Scan_FindsDotPrefixedFolders()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"dotHide_scan_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempRoot);
        try
        {
            Directory.CreateDirectory(Path.Combine(tempRoot, ".obsidian"));
            Directory.CreateDirectory(Path.Combine(tempRoot, ".codex"));
            Directory.CreateDirectory(Path.Combine(tempRoot, "normal-folder"));
            File.WriteAllText(Path.Combine(tempRoot, "normal-file.txt"), "x");
            
            var config = AppConfig.CreateDefault();
            config.Roots = new List<string> { tempRoot };
            var results = FileScanner.Scan(config.Roots, config.ManagedItems, config);
            
            Assert.Equal(2, results.Count(r => r.Name.StartsWith(".")));
            Assert.Contains(results, r => r.Name == ".obsidian");
            Assert.Contains(results, r => r.Name == ".codex");
            Assert.DoesNotContain(results, r => r.Name == "normal-folder");
        }
        finally { Directory.Delete(tempRoot, true); }
    }

    [Fact]
    public void Scan_IncludesManagedItemsWithoutDotPrefix()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"dotHide_scan_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempRoot);
        try
        {
            Directory.CreateDirectory(Path.Combine(tempRoot, "normal-folder"));
            var config = AppConfig.CreateDefault();
            config.Roots = new List<string> { tempRoot };
            config.ManagedItems.Add(new ManagedItem { Path = Path.Combine(tempRoot, "normal-folder") });
            
            var results = FileScanner.Scan(config.Roots, config.ManagedItems, config);
            
            Assert.Contains(results, r => r.Name == "normal-folder");
        }
        finally { Directory.Delete(tempRoot, true); }
    }

    [Fact]
    public void Scan_IncludesNonDotGlobalExceptionNames()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"dotHide_scan_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempRoot);
        try
        {
            Directory.CreateDirectory(Path.Combine(tempRoot, "workspace"));
            var config = new AppConfig
            {
                Roots = new List<string> { tempRoot },
                GlobalExceptions = new List<string> { "workspace" }
            };

            var results = FileScanner.Scan(config);

            Assert.Contains(results, r => r.Name == "workspace");
        }
        finally { Directory.Delete(tempRoot, true); }
    }

    [Fact]
    public void Scan_AddsFolderConfigPathsAsScanRoots()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"dotHide_scan_{Guid.NewGuid()}");
        var configuredFolder = Path.Combine(tempRoot, "project");
        Directory.CreateDirectory(configuredFolder);
        try
        {
            Directory.CreateDirectory(Path.Combine(configuredFolder, ".obsidian"));
            var config = new AppConfig
            {
                Roots = new List<string>(),
                FolderRules = new List<FolderRule> { new() { Path = configuredFolder, State = RuleState.Hide } }
            };

            var results = FileScanner.Scan(config);

            Assert.Contains(results, r => r.Name == ".obsidian");
        }
        finally { Directory.Delete(tempRoot, true); }
    }

    [Fact]
    public void Scan_MissingRoot_SkipsWithoutCrash()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"dotHide_scan_{Guid.NewGuid()}");
        var missingRoot = Path.Combine(Path.GetTempPath(), $"dotHide_missing_root_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempRoot);
        try
        {
            Directory.CreateDirectory(Path.Combine(tempRoot, ".obsidian"));
            var config = new AppConfig
            {
                Roots = new List<string> { tempRoot, missingRoot }
            };

            var results = FileScanner.Scan(config);

            var result = Assert.Single(results);
            Assert.Equal(".obsidian", result.Name);
            Assert.Contains(FileScanner.LastScanErrors, error => error.Contains("Root folder not found"));
        }
        finally { Directory.Delete(tempRoot, true); }
    }

    [Fact]
    public void Scan_OnlyDirectChildren_NotRecursive()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"dotHide_scan_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempRoot);
        try
        {
            var subDir = Path.Combine(tempRoot, ".obsidian");
            Directory.CreateDirectory(subDir);
            Directory.CreateDirectory(Path.Combine(subDir, ".nested")); // should NOT be found
            
            var config = AppConfig.CreateDefault();
            config.Roots = new List<string> { tempRoot };
            var results = FileScanner.Scan(config.Roots, config.ManagedItems, config);
            
            Assert.Contains(results, r => r.Name == ".obsidian");
            Assert.DoesNotContain(results, r => r.Name == ".nested");
        }
        finally { Directory.Delete(tempRoot, true); }
    }

    [Fact]
    public void Scan_IncludesMissingManagedItemsWithoutCrash()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"dotHide_missing_{Guid.NewGuid()}", ".gone");
        var config = new AppConfig
        {
            Roots = new List<string>(),
            ManagedItems = new List<ManagedItem> { new() { Path = missing } }
        };

        var results = FileScanner.Scan(config);

        var result = Assert.Single(results);
        Assert.False(result.Exists);
        Assert.Equal(ItemType.Missing, result.Type);
        Assert.Equal(PendingAction.NoChange, result.PendingAction);
        Assert.Equal("Missing", result.Status);
    }

    [Fact]
    public void DeterminePendingAction_StrongerHideAddsSystemWhenAlreadyHidden()
    {
        var attrs = FileAttributes.Hidden;

        var action = FileScanner.DeterminePendingAction(attrs, RuleState.Hide, useSystemAttribute: true);

        Assert.Equal(PendingAction.Hide, action);
    }

    [Fact]
    public void DeterminePendingAction_StrongerShowRemovesSystemOnlyWhenRequested()
    {
        var attrs = FileAttributes.System;

        var withoutStronger = FileScanner.DeterminePendingAction(attrs, RuleState.Show, useSystemAttribute: false);
        var withStronger = FileScanner.DeterminePendingAction(attrs, RuleState.Show, useSystemAttribute: true);

        Assert.Equal(PendingAction.NoChange, withoutStronger);
        Assert.Equal(PendingAction.Show, withStronger);
    }
}
