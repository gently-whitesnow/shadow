using System.IO;

namespace Shadow.Agent.Options;

public class AgentOptions
{
    public int MaxReportMbLimit { get; init; } = 500;

    public string TempDir { get; init; } = Path.GetTempPath();
}