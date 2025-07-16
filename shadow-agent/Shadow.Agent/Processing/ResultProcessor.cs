using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shadow.Agent.Parsers;

namespace Shadow.Agent.Processing;

public sealed class ResultProcessor
{
    private const int PreviewSize = 8 * 1024;
    private readonly IReadOnlyList<IResultParser> _parsers;
    private readonly ILogger<ResultProcessor> _logger;

    public ResultProcessor(IEnumerable<IResultParser> parsers, ILogger<ResultProcessor> logger)
    {
        _parsers = parsers.ToList();
        _logger = logger;
    }

    public async ValueTask<TestRunSummary> ProcessAsync(
        Stream content,
        CancellationToken ct = default)
    {

        byte[] rented = ArrayPool<byte>.Shared.Rent(PreviewSize);
        int read;
        try
        {
            read = await content.ReadAsync(rented.AsMemory(0, PreviewSize), ct);
        }
        finally
        {
            // Вернём позицию, только если можем.
            if (content.CanSeek) content.Position = 0;
            
        }

        var preview = rented.AsSpan(0, read);

        IResultParser? parser = null;
        foreach (var p in _parsers)
        {
            if (p.CanParse(preview))
            {
                parser = p;
                break;
            }
        }

        ArrayPool<byte>.Shared.Return(rented);

        if (parser is null)
        {
            const string msg = "No suitable parser found (supported: TRX, JUnit, …)";
            _logger.LogError(msg);
            throw new NotSupportedException(msg);
        }

        return await ParseWithAsync(parser, content, ct);
    }

    private async ValueTask<TestRunSummary> ParseWithAsync(
        IResultParser parser,
        Stream content,
        CancellationToken ct)
    {
        _logger.LogDebug("Using parser {Parser}", parser.GetType().Name);

        try
        {
            var summary = await parser.ParseAsync(content, ct);

            LogSummary(summary);
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed in {Parser}", parser.GetType().Name);
            throw;
        }
    }

    private void LogSummary(TestRunSummary s)
    {
        if (s.Failed > 0)
            _logger.LogWarning("❌ {Failed}/{Total} failed - {Skipped} skipped", s.Failed, s.Total, s.Skipped);
        else
            _logger.LogInformation("✔ {Passed}/{Total} passed - {Skipped} skipped", s.Passed, s.Total, s.Skipped);
    }
}