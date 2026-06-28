using DotHide.Models;

namespace DotHide.Services;

public static class RuleResolver
{
    public static ResolutionResult Resolve(string itemPath, AppConfig config)
    {
        var normalizedPath = PathHelper.NormalizePath(itemPath);

        var exactMatch = config.ExactPathRules.FirstOrDefault(r => PathHelper.PathsEqual(r.Path, normalizedPath));
        if (exactMatch is not null)
            return new ResolutionResult
            {
                DesiredState = exactMatch.State,
                Source = ResolutionSource.Exact,
                SourceDescription = $"Exact rule: {exactMatch.Path}"
            };

        var parentFolder = Directory.Exists(normalizedPath)
            ? normalizedPath
            : Path.GetDirectoryName(normalizedPath) ?? normalizedPath;

        var nearestFolder = config.FolderRules
            .Where(r => PathHelper.IsAncestorOrSame(r.Path, parentFolder))
            .OrderByDescending(r => PathHelper.NormalizePath(r.Path).Length)
            .FirstOrDefault();

        if (nearestFolder is not null)
        {
            if (NameMatches(Path.GetFileName(normalizedPath), nearestFolder.Exceptions))
                return new ResolutionResult
                {
                    DesiredState = Opposite(nearestFolder.State),
                    Source = ResolutionSource.Folder,
                    SourceDescription = $"Folder exception: {Path.GetFileName(normalizedPath)}"
                };

            return new ResolutionResult
            {
                DesiredState = nearestFolder.State,
                Source = ResolutionSource.Folder,
                SourceDescription = $"Folder rule: {nearestFolder.Path}"
            };
        }

        var baseName = Path.GetFileName(normalizedPath);
        if (NameMatches(baseName, config.GlobalExceptions))
            return new ResolutionResult
            {
                DesiredState = Opposite(config.GlobalState),
                Source = ResolutionSource.Global,
                SourceDescription = $"Global exception: {baseName}"
            };

        var nameMatch = config.NameRules.FirstOrDefault(r => string.Equals(r.Name, baseName, StringComparison.OrdinalIgnoreCase));
        if (nameMatch is not null)
            return new ResolutionResult
            {
                DesiredState = nameMatch.State,
                Source = ResolutionSource.Name,
                SourceDescription = $"Name rule: {nameMatch.Name}"
            };

        return new ResolutionResult
        {
            DesiredState = config.GlobalState,
            Source = ResolutionSource.Global,
            SourceDescription = "Global default"
        };
    }

    private static bool NameMatches(string name, IEnumerable<string> names) =>
        names.Any(candidate => string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase));

    private static RuleState Opposite(RuleState state) => state == RuleState.Hide ? RuleState.Show : RuleState.Hide;
}
