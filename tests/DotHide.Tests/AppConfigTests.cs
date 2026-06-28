using System.Text.Json;
using DotHide.Models;

namespace DotHide.Tests;

public class AppConfigTests
{
    private static string TestPath([System.Runtime.CompilerServices.CallerMemberName] string testName = "")
    {
        var dir = Path.Combine(Path.GetTempPath(), "DotHideTests", testName);
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "appsettings.json");
    }

    [Fact]
    public void CreateDefaultConfig_HasCorrectDefaults()
    {
        var config = AppConfig.CreateDefault();

        Assert.Equal(2, config.Version);
        Assert.Equal(RuleState.Hide, config.GlobalState);
        Assert.True(config.UseSystemAttribute);
        Assert.Single(config.Roots);
        Assert.Equal(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), config.Roots[0]);
        Assert.Empty(config.GlobalExceptions);
        Assert.Empty(config.NameRules);
        Assert.Empty(config.ManagedItems);
        Assert.Empty(config.ExactPathRules);
        Assert.Empty(config.FolderRules);
    }

    [Fact]
    public void RoundTrip_SerializeDeserialize_PreservesData()
    {
        var path = TestPath();
        try
        {
            var original = new AppConfig
            {
                Version = 2,
                GlobalState = RuleState.Show,
                UseSystemAttribute = true,
                GlobalExceptions = new List<string> { ".keep" },
                Roots = new List<string> { @"C:\Test", @"D:\Data" },
                FolderRules = new List<FolderRule>
                {
                    new() { Path = @"C:\Test\Hidden", State = RuleState.Hide, Exceptions = new List<string> { ".visible-here" } },
                    new() { Path = @"D:\Data\Visible", State = RuleState.Show }
                },
                NameRules = new List<NameRule>
                {
                    new() { Name = ".hidden", State = RuleState.Hide },
                    new() { Name = ".visible", State = RuleState.Show }
                },
                ExactPathRules = new List<ExactPathRule>
                {
                    new() { Path = @"C:\Exact\File.txt", State = RuleState.Hide }
                },
                ManagedItems = new List<ManagedItem>
                {
                    new() { Path = @"C:\Managed\Item", OriginalHidden = true, OriginalSystem = false, LastAppliedState = RuleState.Hide }
                }
            };
            original.Save(path);

            var deserialized = AppConfig.Load(path);

            Assert.NotNull(deserialized);
            Assert.Equal(original.Version, deserialized.Version);
            Assert.Equal(original.GlobalState, deserialized.GlobalState);
            Assert.Equal(original.UseSystemAttribute, deserialized.UseSystemAttribute);
            Assert.Equal(new[] { ".hidden", ".keep" }, deserialized.GlobalExceptions);
            Assert.Equal(original.Roots, deserialized.Roots);
            Assert.Equal(original.FolderRules.Count, deserialized.FolderRules.Count);
            Assert.Equal(original.NameRules.Count, deserialized.NameRules.Count);
            Assert.Equal(original.ExactPathRules.Count, deserialized.ExactPathRules.Count);
            Assert.Equal(original.ManagedItems.Count, deserialized.ManagedItems.Count);

            for (int i = 0; i < original.FolderRules.Count; i++)
            {
                Assert.Equal(original.FolderRules[i].Path, deserialized.FolderRules[i].Path);
                Assert.Equal(original.FolderRules[i].State, deserialized.FolderRules[i].State);
                Assert.Equal(original.FolderRules[i].Exceptions, deserialized.FolderRules[i].Exceptions);
            }
            for (int i = 0; i < original.NameRules.Count; i++)
            {
                Assert.Equal(original.NameRules[i].Name, deserialized.NameRules[i].Name);
                Assert.Equal(original.NameRules[i].State, deserialized.NameRules[i].State);
            }
            for (int i = 0; i < original.ExactPathRules.Count; i++)
            {
                Assert.Equal(original.ExactPathRules[i].Path, deserialized.ExactPathRules[i].Path);
                Assert.Equal(original.ExactPathRules[i].State, deserialized.ExactPathRules[i].State);
            }
            for (int i = 0; i < original.ManagedItems.Count; i++)
            {
                Assert.Equal(original.ManagedItems[i].Path, deserialized.ManagedItems[i].Path);
                Assert.Equal(original.ManagedItems[i].OriginalHidden, deserialized.ManagedItems[i].OriginalHidden);
                Assert.Equal(original.ManagedItems[i].OriginalSystem, deserialized.ManagedItems[i].OriginalSystem);
                Assert.Equal(original.ManagedItems[i].LastAppliedState, deserialized.ManagedItems[i].LastAppliedState);
            }
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Load_WhenFileMissing_CreatesDefaultAndSaves()
    {
        var path = TestPath();
        if (File.Exists(path)) File.Delete(path);

        try
        {
            var config = AppConfig.Load(path);
            Assert.NotNull(config);
            Assert.Equal(RuleState.Hide, config.GlobalState);
            Assert.True(File.Exists(path), "Load should create the config file when missing");

            var fileContent = File.ReadAllText(path);
            Assert.Contains("\"hide\"", fileContent);
            Assert.Contains("GlobalExceptions", fileContent);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Load_WhenFileExists_ReturnsParsedConfig()
    {
        var path = TestPath();
        if (File.Exists(path)) File.Delete(path);

        try
        {
            var original = AppConfig.CreateDefault();
            original.GlobalState = RuleState.Show;
            original.UseSystemAttribute = true;
            original.Save(path);

            var loaded = AppConfig.Load(path);

            Assert.NotNull(loaded);
            Assert.Equal(RuleState.Show, loaded.GlobalState);
            Assert.True(loaded.UseSystemAttribute);
            Assert.Empty(loaded.NameRules);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Load_WhenCorruptJson_CreatesFreshDefault()
    {
        var path = TestPath();
        if (File.Exists(path)) File.Delete(path);

        try
        {
            File.WriteAllText(path, "{ invalid json }");
            var config = AppConfig.Load(path);
            Assert.NotNull(config);
            Assert.Equal(RuleState.Hide, config.GlobalState);
            Assert.True(config.UseSystemAttribute);
            Assert.Equal(2, config.Version);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Load_WhenEmptyFile_CreatesFreshDefault()
    {
        var path = TestPath();
        if (File.Exists(path)) File.Delete(path);

        try
        {
            File.WriteAllText(path, "");
            var config = AppConfig.Load(path);
            Assert.NotNull(config);
            Assert.Equal(RuleState.Hide, config.GlobalState);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Save_CreatesFileWithReadableJson()
    {
        var path = TestPath();
        if (File.Exists(path)) File.Delete(path);

        try
        {
            var config = AppConfig.CreateDefault();
            config.GlobalState = RuleState.Show;
            config.Save(path);

            Assert.True(File.Exists(path));
            var content = File.ReadAllText(path);
            Assert.Contains("\"show\"", content);
            Assert.Contains("GlobalExceptions", content);
            Assert.True(IsValidJson(content));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Load_AcceptsLegacyPascalCaseStates()
    {
        var path = TestPath();
        if (File.Exists(path)) File.Delete(path);

        try
        {
            File.WriteAllText(path, """
            {
              "version": 1,
              "globalState": "Show",
              "useSystemAttribute": false,
              "roots": [],
              "folderRules": [],
              "nameRules": [
                { "name": ".obsidian", "state": "Hide" }
              ],
              "exactPathRules": [],
              "managedItems": []
            }
            """);

            var loaded = AppConfig.Load(path);

            Assert.Equal(RuleState.Show, loaded.GlobalState);
            Assert.Single(loaded.NameRules);
            Assert.Equal(RuleState.Hide, loaded.NameRules[0].State);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Load_MigratesVersion1ConfigToStrongerHide()
    {
        var path = TestPath();
        if (File.Exists(path)) File.Delete(path);

        try
        {
            File.WriteAllText(path, """
            {
              "version": 1,
              "globalState": "hide",
              "useSystemAttribute": false,
              "globalExceptions": [],
              "roots": [],
              "folderRules": [],
              "nameRules": [],
              "exactPathRules": [],
              "managedItems": []
            }
            """);

            var loaded = AppConfig.Load(path);

            Assert.Equal(2, loaded.Version);
            Assert.True(loaded.UseSystemAttribute);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Sanitize_NormalizesAndDeduplicatesExceptions()
    {
        var config = new AppConfig
        {
            GlobalExceptions = new List<string> { " .obsidian ", ".OBSIDIAN", ".codex" },
            FolderRules = new List<FolderRule>
            {
                new() { Path = @"C:\A", State = RuleState.Hide, Exceptions = new List<string> { ".git", ".GIT" } }
            }
        };

        config.Sanitize();

        Assert.Equal(new[] { ".codex", ".obsidian" }, config.GlobalExceptions);
        Assert.Equal(new[] { ".git" }, config.FolderRules[0].Exceptions);
    }

    [Fact]
    public void Sanitize_MigratesLegacyNameRulesToGlobalExceptionsWhenOppositeGlobal()
    {
        var config = new AppConfig
        {
            GlobalState = RuleState.Show,
            NameRules = new List<NameRule>
            {
                new() { Name = ".obsidian", State = RuleState.Hide },
                new() { Name = ".visible", State = RuleState.Show }
            }
        };

        config.Sanitize();

        Assert.Contains(".obsidian", config.GlobalExceptions);
        Assert.DoesNotContain(".visible", config.GlobalExceptions);
    }

    private static bool IsValidJson(string json)
    {
        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
