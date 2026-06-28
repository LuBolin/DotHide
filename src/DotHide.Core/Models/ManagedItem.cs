namespace DotHide.Models;

public class ManagedItem
{
    public string Path { get; set; } = "";
    public bool OriginalHidden { get; set; }
    public bool OriginalSystem { get; set; }
    public RuleState LastAppliedState { get; set; }
}
