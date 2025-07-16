using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shadow.Agent.Processing;
using Shadow.Agent.TaskQueue;

namespace Shadow.Agent.Services;

public sealed class TestResultsService(
        ILogger<TestResultsService> log,
        ResultProcessor processor,
        ITaskQueue queue)
{
    public ValueTask EnqueueForProcessingAsync(
        string runId, string filePath) =>
        queue.EnqueueAsync(async () =>
        {
            try
            {
                await using var fs = File.Open(
                        filePath, FileMode.Open, FileAccess.Read,
                        FileShare.None | FileShare.Delete);        // заблокируем от паралл. чтения

                await processor.ProcessAsync(fs);
                log.LogInformation("Run {RunId} processed OK", runId);
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
        }, log.LogError, "Run {RunId} failed", runId);
}