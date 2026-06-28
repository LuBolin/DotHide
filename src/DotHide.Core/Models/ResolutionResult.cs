namespace DotHide.Models;

public enum ResolutionSource
{
    Exact,
    Folder,
    Name,
    Global
}

public class ResolutionResult
{
    public RuleState DesiredState { get; set; }
    public ResolutionSource Source { get; set; }
    public string SourceDescription { get; set; } = "";
}
