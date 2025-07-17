namespace Shadow.Agent.Models.Bo;

public record TestRunResult
{
    public int Total { get; init; }
    public int Passed { get; init; }
    public int Failed { get; init; }
    public int Skipped { get; init; }
} 