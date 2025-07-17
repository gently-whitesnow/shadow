using System.Collections.Generic;
using System.Threading.Tasks;
using Shadow.Agent.DA;
using Shadow.Agent.Models.Bo;
using Shadow.Agent.Models.DbModels;

namespace Shadow.Agent.Services;

public sealed class MessengerService(
    IEnumerable<IMessengerClient> clients) : IResultConsumer
{
    public async Task SendResultAsync(ScopeDbModel scope, TestRunMeta meta, TestRunResult result)
    {
        var text = "test";
        foreach (var client in clients)
        {
            await client.SendMessageAsync(scope.MessengerChannelId, text);
        }
    }

    private string GetMessage(TestRunMeta meta, TestRunResult result)
    {
        return $"Run {meta.RunId} processed OK";
    }
}