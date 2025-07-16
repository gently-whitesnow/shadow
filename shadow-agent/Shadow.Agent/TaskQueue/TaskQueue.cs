using System.Diagnostics;
using System.Threading.Channels;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Shadow.Agent.TaskQueue;

public class TaskQueue(ILogger<TaskQueue> logger, IServiceProvider serviceProvider) 
    : BackgroundService, ITaskQueue
{
    private readonly Channel<TaskContext> _queue = Channel.CreateUnbounded<TaskContext>();

    private record TaskContext(Func<IServiceProvider, CancellationToken, Task> WorkItem, Activity Activity);

    public ValueTask EnqueueAsync(Func<IServiceProvider, CancellationToken, Task> workItem)
    {
        var nestedActivity = new Activity($"{nameof(TaskQueue)}.{nameof(ExecuteAsync)}").Start();

        return _queue.Writer.WriteAsync(new TaskContext(workItem, nestedActivity));
    }

    public ValueTask EnqueueAsync(Func<CancellationToken, Task> workItem) => EnqueueAsync((_, token) => workItem(token));

    public ValueTask EnqueueAsync(Func<Task> workItem) => EnqueueAsync((_, _) => workItem());

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Parallel
        .ForEachAsync(_queue.Reader.ReadAllAsync(stoppingToken), new ParallelOptions
            {
                CancellationToken = stoppingToken,
                MaxDegreeOfParallelism = TaskScheduler.Current.MaximumConcurrencyLevel,
            },
            async (context, ct) =>
            {
                using var act = context.Activity;
                Activity.Current = act;

                try
                {
                    using var serviceScope = serviceProvider.CreateScope();
                    await context.WorkItem(serviceScope.ServiceProvider, ct);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "При выполнении фоновой задачи произошла ошибка");
                }
            });
}
