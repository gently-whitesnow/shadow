using System.Linq;
using Microsoft.AspNetCore.Http;
using Shadow.Agent.Models.Bo;

namespace Shadow.Agent.Helpers;

public static class TestRunHeadersHelper
{
    public static TestRunMeta GetTestRunMeta(HttpRequest request)
    {
        var runId = request.Headers["Shadow-Run-Id"].FirstOrDefault();
        var scopeName = request.Headers["Shadow-Scope-Name"].FirstOrDefault();
        var osUser = request.Headers["Shadow-Os-User"].FirstOrDefault();
        var branch = request.Headers["Shadow-Branch"].FirstOrDefault();
        var commit = request.Headers["Shadow-Commit"].FirstOrDefault();
        return new TestRunMeta
        {
            Scope = scopeName,
            RunId = runId,
            OsUser = osUser,
            Branch = branch,
            Commit = commit
        };
    }
}