namespace Shadow.Agent.Models.Bo;

public sealed record TestRunMeta
{
    public string? Scope { get; init; }
    public string? RunId { get; init; }
    public string? OsUser { get; init; }
    public string? Branch { get; init; }
    public string? Commit { get; init; }

    public string? MachineName   { get; init; }
    public string? OsPlatform    { get; init; }
    public string? OsArchitecture{ get; init; }
    public string?    ProcessorCount{ get; init; }
    public string? StartUtc    { get; init; }
    public string? FinishUtc   { get; init; }
}