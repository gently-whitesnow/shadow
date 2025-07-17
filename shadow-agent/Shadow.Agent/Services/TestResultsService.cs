using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shadow.Agent.Models.Bo;
using Shadow.Agent.Processing;
using Shadow.Agent.TaskQueue;

namespace Shadow.Agent.Services;

public sealed class TestResultsService(
        ILogger<TestResultsService> log,
        ResultProcessor processor,
        ITaskQueue queue,
        ResultConsumerProvider resultConsumerProvider,
        ScopesService scopesService)
{
    public ValueTask EnqueueForProcessingAsync(
        TestRunMeta meta, string filePath) =>
        queue.EnqueueAsync(async () =>
        {
            try
            {
                await using var fs = File.Open(
                        filePath, FileMode.Open, FileAccess.Read,
                        FileShare.None | FileShare.Delete);        // заблокируем от паралл. чтения


                var resultTask = processor.ProcessAsync(fs);
                var scopeTask = scopesService.GetScopeAsync(meta.Scope);

                await resultTask;
                await scopeTask;

                log.LogInformation("Run {@meta} processed OK", meta);
                await resultConsumerProvider.SendResultAsync(scopeTask.Result, meta, resultTask.Result);
            }
            finally
            {
                // удаляем если парсер упал
                try { File.Delete(filePath); }
                catch (Exception)
                {
                    throw;
                }
            }
        }, log.LogError, "Run {@meta} failed", meta);
}