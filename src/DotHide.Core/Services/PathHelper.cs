namespace DotHide.Services;

public static class PathHelper
{
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "";

        var fullPath = Path.GetFullPath(path);
        var root = Path.GetPathRoot(fullPath);
        var trimmed = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.IsNullOrEmpty(trimmed) && !string.IsNullOrEmpty(root) ? root : trimmed;
    }

    public static bool PathsEqual(string left, string right) =>
        string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);

    public static bool IsAncestorOrSame(string ancestor, string child)
    {
        var normalizedAncestor = NormalizePath(ancestor);
        var normalizedChild = NormalizePath(child);

        if (string.Equals(normalizedAncestor, normalizedChild, StringComparison.OrdinalIgnoreCase))
            return true;

        var prefix = normalizedAncestor.EndsWith(Path.DirectorySeparatorChar)
            ? normalizedAncestor
            : normalizedAncestor + Path.DirectorySeparatorChar;

        return normalizedChild.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAncestorOf(string ancestor, string child) => IsAncestorOrSame(ancestor, child);

    public static bool NameEquals(string pathOrName, string expectedName)
    {
        var name = Path.GetFileName(pathOrName);
        return string.Equals(name, expectedName, StringComparison.OrdinalIgnoreCase);
    }
}
