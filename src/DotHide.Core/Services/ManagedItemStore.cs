using DotHide.Models;

namespace DotHide.Services;

public class OperationSummary
{
    public int HiddenCount { get; set; }
    public int ShownCount { get; set; }
    public int UnchangedCount { get; set; }
    public int FailedCount { get; set; }
    public int RestoredCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public static class ManagedItemStore
{
    public static void RecordOriginal(string path, AppConfig config)
    {
        var normalized = PathHelper.NormalizePath(path);
        if (config.ManagedItems.Any(m => PathHelper.PathsEqual(m.Path, normalized)))
            return;

        var attrs = AttributeService.GetCurrentAttributes(normalized);
        config.ManagedItems.Add(new ManagedItem
        {
            Path = normalized,
            OriginalHidden = AttributeService.IsHidden(attrs),
            OriginalSystem = AttributeService.IsSystem(attrs),
            LastAppliedState = AttributeService.IsHidden(attrs) ? RuleState.Hide : RuleState.Show
        });
    }

    public static OperationSummary ApplyChanges(List<ScanResult> results, AppConfig config)
    {
        var summary = new OperationSummary();

        foreach (var result in results)
        {
            if (result.PendingAction == PendingAction.NoChange)
            {
                if (result.Exists && (File.Exists(result.Path) || Directory.Exists(result.Path)))
                    AttributeService.NotifyAttributesChanged(result.Path);

                summary.UnchangedCount++;
                continue;
            }

            if (!result.Exists || (!File.Exists(result.Path) && !Directory.Exists(result.Path)))
            {
                summary.Errors.Add($"Missing: {result.Path}");
                summary.FailedCount++;
                continue;
            }

            try
            {
                RecordOriginal(result.Path, config);
                var hide = result.PendingAction == PendingAction.Hide;
                var attrResult = AttributeService.SetHidden(result.Path, hide, config.UseSystemAttribute);

                if (!attrResult.Success)
                {
                    summary.Errors.Add(attrResult.Error);
                    summary.FailedCount++;
                    continue;
                }

                var managed = config.ManagedItems.FirstOrDefault(m => PathHelper.PathsEqual(m.Path, result.Path));
                if (managed is not null)
                    managed.LastAppliedState = hide ? RuleState.Hide : RuleState.Show;

                if (hide)
                    summary.HiddenCount++;
                else
                    summary.ShownCount++;
            }
            catch (Exception ex)
            {
                summary.Errors.Add($"Error processing {result.Path}: {ex.Message}");
                summary.FailedCount++;
            }
        }

        return summary;
    }

    public static OperationSummary RestoreAll(AppConfig config)
    {
        var summary = new OperationSummary();
        var restoredItems = new List<ManagedItem>();

        foreach (var item in config.ManagedItems.ToList())
        {
            if (!File.Exists(item.Path) && !Directory.Exists(item.Path))
            {
                summary.Warnings.Add($"Missing: {item.Path}");
                summary.SkippedCount++;
                continue;
            }

            try
            {
                var current = AttributeService.GetCurrentAttributes(item.Path);
                var target = current;

                target = item.OriginalHidden ? target | FileAttributes.Hidden : target & ~FileAttributes.Hidden;
                target = item.OriginalSystem ? target | FileAttributes.System : target & ~FileAttributes.System;

                File.SetAttributes(item.Path, target);
                AttributeService.NotifyAttributesChanged(item.Path);
                restoredItems.Add(item);
                summary.RestoredCount++;
            }
            catch (Exception ex)
            {
                summary.Errors.Add($"Failed to restore {item.Path}: {ex.Message}");
                summary.FailedCount++;
            }
        }

        foreach (var item in restoredItems)
            config.ManagedItems.Remove(item);

        return summary;
    }
}
