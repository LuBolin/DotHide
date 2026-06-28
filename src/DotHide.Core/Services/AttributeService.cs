using System.Runtime.InteropServices;

namespace DotHide.Services;

public class AttributeResult
{
    public bool Success { get; set; } = true;
    public string Error { get; set; } = "";
    public FileAttributes OriginalAttributes { get; set; }
}

public static class AttributeService
{
    public static FileAttributes GetCurrentAttributes(string path) => File.GetAttributes(path);

    public static AttributeResult SetHidden(string path, bool hidden, bool useSystem)
    {
        try
        {
            if (!File.Exists(path) && !Directory.Exists(path))
                return new AttributeResult { Success = false, Error = $"Path not found: {path}" };

            var current = File.GetAttributes(path);
            var wasReadOnly = (current & FileAttributes.ReadOnly) != 0;

            if (wasReadOnly)
                File.SetAttributes(path, current & ~FileAttributes.ReadOnly);

            var updated = File.GetAttributes(path);
            if (hidden)
            {
                updated |= FileAttributes.Hidden;
                if (useSystem)
                    updated |= FileAttributes.System;
            }
            else
            {
                updated &= ~FileAttributes.Hidden;
                if (useSystem)
                    updated &= ~FileAttributes.System;
            }

            File.SetAttributes(path, updated);

            if (wasReadOnly)
                File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);

            NotifyAttributesChanged(path);
            return new AttributeResult { Success = true, OriginalAttributes = current };
        }
        catch (UnauthorizedAccessException)
        {
            return new AttributeResult { Success = false, Error = $"Access denied: {path}" };
        }
        catch (IOException ex)
        {
            return new AttributeResult { Success = false, Error = $"IO error for {path}: {ex.Message}" };
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return new AttributeResult { Success = false, Error = $"Invalid path {path}: {ex.Message}" };
        }
    }

    public static bool IsHidden(FileAttributes attrs) => (attrs & FileAttributes.Hidden) != 0;

    public static bool IsSystem(FileAttributes attrs) => (attrs & FileAttributes.System) != 0;

    public static bool IsDirectory(FileAttributes attrs) => (attrs & FileAttributes.Directory) != 0;

    public static void NotifyAttributesChanged(string path)
    {
        var flags = SHCNF_PATHW | SHCNF_FLUSHNOWAIT;
        SHChangeNotify(SHCNE_UPDATEITEM, flags, path, null);
        SHChangeNotify(SHCNE_ATTRIBUTES, flags, path, null);

        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(parent))
            SHChangeNotify(SHCNE_UPDATEDIR, flags, parent, null);
    }

    private const uint SHCNE_ATTRIBUTES = 0x00000800;
    private const uint SHCNE_UPDATEDIR = 0x00001000;
    private const uint SHCNE_UPDATEITEM = 0x00002000;
    private const uint SHCNF_PATHW = 0x0005;
    private const uint SHCNF_FLUSHNOWAIT = 0x2000;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern void SHChangeNotify(uint wEventId, uint uFlags, string? dwItem1, string? dwItem2);
}
