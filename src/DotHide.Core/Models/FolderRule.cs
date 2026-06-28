namespace DotHide.Models;

public class FolderRule
{
    public string Path { get; set; } = "";
    public RuleState State { get; set; }
    public List<string> Exceptions { get; set; } = new();
}
