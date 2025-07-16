using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shadow.Agent.TaskQueue;

public static class TaskQueueExtensions
{
    public static ValueTask EnqueueAsync(this ITaskQueue taskQueue, Func<IServiceProvider, CancellationToken, Task> workItem, Action<Exception, string?, object[]> onError, string? errorMessage = null, params object[] messageParams) =>
        taskQueue.EnqueueAsync((sp, ct) => workItem(sp, ct)
            .ContinueWith(t =>
            {
                if (t is { IsFaulted: true, Exception: { } exception })
                {
                    onError.Invoke(exception, errorMessage, messageParams);
                }
            }, CancellationToken.None));


    public static ValueTask EnqueueAsync(this ITaskQueue taskQueue, Func<CancellationToken, Task> workItem, Action<Exception, string?, object[]> onError, string? errorMessage = null, params object[] messageParams) =>
        EnqueueAsync(taskQueue, (_, ct) => workItem(ct), onError, errorMessage, messageParams);


    public static ValueTask EnqueueAsync(this ITaskQueue taskQueue, Func<Task> workItem, Action<Exception, string?, object[]> onError, string? errorMessage = null, params object[] messageParams) =>
        EnqueueAsync(taskQueue, (_, _) => workItem(), onError, errorMessage, messageParams);
}
