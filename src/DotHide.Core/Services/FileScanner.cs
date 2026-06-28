using DotHide.Models;

namespace DotHide.Services;

public static class FileScanner
{
    public static List<string> LastScanErrors { get; private set; } = new();

    public static List<ScanResult> Scan(AppConfig config) => Scan(config.Roots, config.ManagedItems, config);

    public static List<ScanResult> Scan(List<string> roots, List<ManagedItem> managedItems, AppConfig config)
    {
        var results = new List<ScanResult>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        LastScanErrors = new List<string>();

        var scanRoots = roots
            .Concat(config.FolderRules.Select(rule => rule.Path))
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var root in scanRoots)
        {
            var normalizedRoot = SafeNormalize(root);
            if (normalizedRoot is null || !Directory.Exists(normalizedRoot))
            {
                LastScanErrors.Add($"Root folder not found: {root}");
                continue;
            }

            try
            {
                foreach (var entry in Directory.EnumerateFileSystemEntries(normalizedRoot))
                {
                    var name = Path.GetFileName(entry);
                    if (!ShouldIncludeCandidate(name, normalizedRoot, config))
                        continue;

                    AddExistingResult(results, seen, entry, config);
                }
            }
            catch (UnauthorizedAccessException)
            {
                LastScanErrors.Add($"Access denied: {root}");
            }
            catch (DirectoryNotFoundException)
            {
                LastScanErrors.Add($"Root folder not found: {root}");
            }
            catch (PathTooLongException)
            {
                LastScanErrors.Add($"Path too long: {root}");
            }
            catch (IOException ex)
            {
                LastScanErrors.Add($"Unable to scan {root}: {ex.Message}");
            }
        }

        foreach (var item in managedItems)
            AddManagedResult(results, seen, item.Path, config);

        return results
            .OrderBy(r => r.Exists ? 0 : 1)
            .ThenBy(r => r.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static PendingAction DeterminePendingAction(FileAttributes attrs, RuleState desiredState, bool useSystemAttribute)
    {
        var hidden = AttributeService.IsHidden(attrs);
        var system = AttributeService.IsSystem(attrs);

        if (desiredState == RuleState.Hide)
        {
            if (!hidden || (useSystemAttribute && !system))
                return PendingAction.Hide;

            return PendingAction.NoChange;
        }

        if (hidden || (useSystemAttribute && system))
            return PendingAction.Show;

        return PendingAction.NoChange;
    }

    private static void AddManagedResult(List<ScanResult> results, HashSet<string> seen, string path, AppConfig config)
    {
        var normalized = SafeNormalize(path);
        if (normalized is null)
            return;

        if (File.Exists(normalized) || Directory.Exists(normalized))
            AddExistingResult(results, seen, normalized, config);
        else if (seen.Add(normalized))
            results.Add(CreateMissingResult(normalized, config));
    }

    private static bool ShouldIncludeCandidate(string name, string root, AppConfig config)
    {
        if (name.StartsWith(".", StringComparison.Ordinal))
            return true;

        if (config.GlobalExceptions.Any(candidate => string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase)))
            return true;

        return config.FolderRules
            .Where(rule => PathHelper.IsAncestorOrSame(rule.Path, root))
            .Any(rule => rule.Exceptions.Any(candidate => string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase)));
    }

    private static void AddExistingResult(List<ScanResult> results, HashSet<string> seen, string path, AppConfig config)
    {
        var normalized = PathHelper.NormalizePath(path);
        if (!seen.Add(normalized))
            return;

        try
        {
            var attrs = AttributeService.GetCurrentAttributes(normalized);
            var resolution = RuleResolver.Resolve(normalized, config);

            results.Add(new ScanResult
            {
                Path = normalized,
                Name = Path.GetFileName(normalized),
                Type = AttributeService.IsDirectory(attrs) ? ItemType.Folder : ItemType.File,
                CurrentAttributes = AttributeFormatter.Format(attrs),
                EffectiveRuleSource = resolution.Source,
                DesiredState = resolution.DesiredState,
                PendingAction = DeterminePendingAction(attrs, resolution.DesiredState, config.UseSystemAttribute),
                RuleSourceDescription = resolution.SourceDescription,
                Exists = true,
                Status = "Ready"
            });
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or PathTooLongException)
        {
            results.Add(new ScanResult
            {
                Path = normalized,
                Name = Path.GetFileName(normalized),
                Type = ItemType.Missing,
                CurrentAttributes = "Unavailable",
                PendingAction = PendingAction.NoChange,
                Exists = false,
                Status = "Error",
                Error = ex.Message
            });
        }
    }

    private static ScanResult CreateMissingResult(string path, AppConfig config)
    {
        var resolution = RuleResolver.Resolve(path, config);
        return new ScanResult
        {
            Path = path,
            Name = Path.GetFileName(path),
            Type = ItemType.Missing,
            CurrentAttributes = "Missing",
            EffectiveRuleSource = resolution.Source,
            DesiredState = resolution.DesiredState,
            PendingAction = PendingAction.NoChange,
            RuleSourceDescription = resolution.SourceDescription,
            Exists = false,
            Status = "Missing",
            Error = "Path does not exist"
        };
    }

    private static string? SafeNormalize(string path)
    {
        try
        {
            return PathHelper.NormalizePath(path);
        }
        catch
        {
            LastScanErrors.Add($"Invalid path: {path}");
            return null;
        }
    }
}

public static class AttributeFormatter
{
    public static string Format(FileAttributes attrs)
    {
        if (attrs == FileAttributes.Normal)
            return "Normal";

        var names = Enum.GetValues<FileAttributes>()
            .Where(value => value != 0 && attrs.HasFlag(value))
            .Select(value => value.ToString());

        return string.Join(", ", names);
    }
}
