using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shadow.Agent.Options;
using Shadow.Agent.Services;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;



namespace Shadow.Agent.Controllers;

[ApiController]
[Route("v1")]
public sealed class TestResultsController(
        ILogger<TestResultsController> logger,
        TestResultsService testResultsService,
        IOptions<AgentOptions> opt) : ControllerBase
{
    private readonly long _maxBytes = opt.Value.MaxReportMbLimit * 1024L * 1024L;
    private readonly string _tmpDir = opt.Value.TempDir;

    [HttpPost("test-results")]
    public async Task<IActionResult> Upload(CancellationToken ct = default)
    {
        if (Request.ContentLength > _maxBytes)
            return Problem(statusCode: 413, title: "Payload too large");

        Request.EnableBuffering();           // буфер + Seek
        Request.Body.Position = 0;           // гарантируем начало

        var runId = Request.Headers["X-Shadow-RunId"].FirstOrDefault()
                   ?? Guid.NewGuid().ToString("N");
        var tmpPath = Path.Combine(_tmpDir, $"{runId}.report");

        // стараемся сразу сохранить в файл и отдать 202
        await using (var fs = System.IO.File.Create(
                           tmpPath, 64 * 1024,
                           FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync(ct);
                var f = form.Files.FirstOrDefault()
                        ?? throw new InvalidDataException("multipart: file not found");
                await using var rs = f.OpenReadStream();
                await rs.CopyToAsync(fs, ct);
            }
            else
            {
                await Request.Body.CopyToAsync(fs, ct);
            }
        }

        logger.LogInformation("Run {RunId} saved to {File} ({Bytes} B)",
                           runId, tmpPath, Request.ContentLength);

        await testResultsService.EnqueueForProcessingAsync(runId, tmpPath);
        return Accepted(new { runId });
    }
}