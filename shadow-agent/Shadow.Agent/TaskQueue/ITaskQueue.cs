using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shadow.Agent.TaskQueue;

public interface ITaskQueue
{

    ValueTask EnqueueAsync(Func<IServiceProvider, CancellationToken, Task> workItem);


    ValueTask EnqueueAsync(Func<CancellationToken, Task> workItem);


    ValueTask EnqueueAsync(Func<Task> workItem);
}