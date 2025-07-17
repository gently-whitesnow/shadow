using System.Collections.Generic;
using System.Threading.Tasks;
using Shadow.Agent.Models.Bo;
using Shadow.Agent.Models.DbModels;
using Shadow.Agent.TaskQueue;

namespace Shadow.Agent.Services;

public sealed class ResultConsumerProvider(
        IEnumerable<IResultConsumer> resultConsumers,
        ITaskQueue taskQueue)
{
    public async Task SendResultAsync(ScopeDbModel scope, TestRunMeta meta, TestRunResult result)
    {
        foreach (var consumer in resultConsumers)
        {
            await taskQueue.EnqueueAsync(async () =>
            {
                await consumer.SendResultAsync(scope, meta, result);
            });
        }
    }
}