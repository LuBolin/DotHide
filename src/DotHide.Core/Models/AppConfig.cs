using System.Text.Json;
using DotHide.Json;
using DotHide.Services;

namespace DotHide.Models;

public class AppConfig
{
    public int Version { get; set; } = 2;
    public RuleState GlobalState { get; set; } = RuleState.Hide;
    public bool UseSystemAttribute { get; set; }
    public List<string> GlobalExceptions { get; set; } = new();
    public List<string> Roots { get; set; } = new();
    public List<FolderRule> FolderRules { get; set; } = new();
    public List<NameRule> NameRules { get; set; } = new();
    public List<ExactPathRule> ExactPathRules { get; set; } = new();
    public List<ManagedItem> ManagedItems { get; set; } = new();

    public static string ConfigPath => Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    private static JsonSerializerOptions JsonOptions => new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new LowercaseRuleStateJsonConverter() }
    };

    public static AppConfig CreateDefault()
    {
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        return new AppConfig
        {
            Version = 2,
            GlobalState = RuleState.Hide,
            UseSystemAttribute = true,
            Roots = new List<string> { desktopPath }
        };
    }

    public static AppConfig Load(string? path = null)
    {
        path ??= ConfigPath;

        if (!File.Exists(path))
        {
            var defaultConfig = CreateDefault();
            defaultConfig.Save(path);
            return defaultConfig;
        }

        try
        {
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                var defaultConfig = CreateDefault();
                defaultConfig.Save(path);
                return defaultConfig;
            }

            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
            if (config is null)
            {
                var defaultConfig = CreateDefault();
                defaultConfig.Save(path);
                return defaultConfig;
            }

            config.Sanitize();
            return config;
        }
        catch (JsonException)
        {
            var defaultConfig = CreateDefault();
            defaultConfig.Save(path);
            return defaultConfig;
        }
    }

    public void Save() => Save(ConfigPath);

    public void Save(string path)
    {
        Sanitize();

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(path, json);
    }

    public void Sanitize()
    {
        if (Version < 2)
        {
            UseSystemAttribute = true;
            Version = 2;
        }

        Version = Version <= 0 ? 2 : Version;
        Roots = Roots.Where(p => !string.IsNullOrWhiteSpace(p)).Select(PathHelper.NormalizePath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        GlobalExceptions = NormalizeNames(GlobalExceptions);
        FolderRules = FolderRules.Where(r => !string.IsNullOrWhiteSpace(r.Path)).ToList();
        foreach (var folderRule in FolderRules)
            folderRule.Exceptions = NormalizeNames(folderRule.Exceptions);

        MigrateLegacyNameRules();
        NameRules = NameRules.Where(r => !string.IsNullOrWhiteSpace(r.Name)).ToList();
        ExactPathRules = ExactPathRules.Where(r => !string.IsNullOrWhiteSpace(r.Path)).ToList();
        ManagedItems = ManagedItems.Where(m => !string.IsNullOrWhiteSpace(m.Path)).ToList();
    }

    private void MigrateLegacyNameRules()
    {
        foreach (var legacyRule in NameRules.Where(r => r.State != GlobalState))
        {
            if (!GlobalExceptions.Contains(legacyRule.Name, StringComparer.OrdinalIgnoreCase))
                GlobalExceptions.Add(legacyRule.Name.Trim());
        }

        GlobalExceptions = NormalizeNames(GlobalExceptions);
    }

    private static List<string> NormalizeNames(IEnumerable<string> names) =>
        names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
