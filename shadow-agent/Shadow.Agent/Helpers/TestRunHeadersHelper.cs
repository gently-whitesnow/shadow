using System.Linq;
using Microsoft.AspNetCore.Http;
using Shadow.Agent.Models.Bo;

namespace Shadow.Agent.Helpers;

public static class TestRunHeadersHelper
{
    /// <summary>
    /// Scope
// RunId
// OsUser
// Branch
// Commit
// MachineName
// OsPlatform
// OsArchitecture
// ProcessorCount
// StartUtc
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static TestRunMeta GetTestRunMeta(HttpRequest request)
    {
        var runId = request.Headers["Shadow-RunId"].FirstOrDefault();
        var scopeName = request.Headers["Shadow-Scope"].FirstOrDefault();
        var osUser = request.Headers["Shadow-OsUser"].FirstOrDefault();
        var branch = request.Headers["Shadow-Branch"].FirstOrDefault();
        var commit = request.Headers["Shadow-Commit"].FirstOrDefault();
        var machineName = request.Headers["Shadow-MachineName"].FirstOrDefault();
        var osPlatform = request.Headers["Shadow-OsPlatform"].FirstOrDefault();
        var osArchitecture = request.Headers["Shadow-OsArchitecture"].FirstOrDefault();
        var processorCount = request.Headers["Shadow-ProcessorCount"].FirstOrDefault();
        var startUtc = request.Headers["Shadow-StartUtc"].FirstOrDefault();
        var finishUtc = request.Headers["Shadow-FinishUtc"].FirstOrDefault();
        return new TestRunMeta
        {
            Scope = scopeName,
            RunId = runId,
            OsUser = osUser,
            Branch = branch,
            Commit = commit,
            MachineName = machineName,
            OsPlatform = osPlatform,
            OsArchitecture = osArchitecture,
            ProcessorCount = processorCount,
            StartUtc = startUtc,
            FinishUtc = finishUtc
        };
    }
}