using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Shadow.Agent.Processing;

namespace Shadow.Agent.Parsers;

public interface IResultParser
{
    /// <summary>
    /// Проверяет, может ли парсер обработать данный контент
    /// </summary>
    bool CanParse(ReadOnlySpan<byte> content);
    
    /// <summary>
    /// Парсит содержимое и возвращает суммарную информацию о тестах
    /// </summary>
    Task<TestRunSummary> ParseAsync(Stream content, CancellationToken ct = default);
} 