using DotHide.Models;
using DotHide.Services;

namespace DotHide;

public sealed class DotHideController
{
    public AppConfig Config { get; private set; }
    public IReadOnlyList<ScanResult> CurrentResults { get; private set; } = Array.Empty<ScanResult>();

    public DotHideController()
    {
        Config = AppConfig.Load();
    }

    public void Save() => Config.Save();

    public void SetGlobalState(RuleState state)
    {
        Config.GlobalState = state;
        Save();
    }

    public void SetUseSystemAttribute(bool enabled)
    {
        Config.UseSystemAttribute = enabled;
        Save();
    }

    public bool AddRoot(string path)
    {
        var normalized = PathHelper.NormalizePath(path);
        if (Config.Roots.Any(root => PathHelper.PathsEqual(root, normalized)))
            return false;

        Config.Roots.Add(normalized);
        Save();
        return true;
    }

    public bool AddDesktopRoot() => AddRoot(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));

    public void RemoveRootAt(int index)
    {
        if (index < 0 || index >= Config.Roots.Count)
            return;

        Config.Roots.RemoveAt(index);
        Save();
    }

    public IReadOnlyList<ScanResult> Scan()
    {
        CurrentResults = FileScanner.Scan(Config);
        return CurrentResults;
    }

    public OperationSummary ApplyChanges(IEnumerable<ScanResult> results)
    {
        var summary = ManagedItemStore.ApplyChanges(results.ToList(), Config);
        Save();
        return summary;
    }

    public OperationSummary RestoreAll()
    {
        var summary = ManagedItemStore.RestoreAll(Config);
        Save();
        return summary;
    }

    public string ConfigFolder => Path.GetDirectoryName(AppConfig.ConfigPath) ?? AppContext.BaseDirectory;
}
