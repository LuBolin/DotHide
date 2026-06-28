namespace DotHide.Models;

public enum ItemType
{
    File,
    Folder,
    Missing
}

public enum PendingAction
{
    NoChange,
    Hide,
    Show
}

public class ScanResult
{
    public string Path { get; set; } = "";
    public string Name { get; set; } = "";
    public ItemType Type { get; set; }
    public string CurrentAttributes { get; set; } = "";
    public ResolutionSource EffectiveRuleSource { get; set; }
    public RuleState DesiredState { get; set; }
    public PendingAction PendingAction { get; set; }
    public string RuleSourceDescription { get; set; } = "";
    public bool Exists { get; set; } = true;
    public string Status { get; set; } = "Ready";
    public string Error { get; set; } = "";
}
