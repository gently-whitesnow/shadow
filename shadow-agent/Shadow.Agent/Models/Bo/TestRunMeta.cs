using System;

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
    public int?    ProcessorCount{ get; init; }
    public DateTimeOffset? StartUtc    { get; init; }
    public DateTimeOffset? FinishUtc   { get; init; }
}