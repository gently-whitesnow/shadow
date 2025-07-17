using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Shadow.Agent.Models.Bo;

namespace Shadow.Agent.Parsers;

public sealed class TrxParser : IResultParser
{
    private static ReadOnlySpan<byte> ZipMagic => [0x50, 0x4B, 0x03, 0x04];   // PK\003\004

    // Маркер происхождения файла
    private static ReadOnlySpan<byte> TeamTest => "TeamTest"u8;

    public bool CanParse(ReadOnlySpan<byte> preview)
    {
        if (preview.Length >= 4 && preview.StartsWith(ZipMagic))
            return true;

        return preview.IndexOf(TeamTest) >= 0;
    }

    public async Task<TestRunResult> ParseAsync(Stream content, CancellationToken ct = default)
    {
        // Прочитаем первые 4 байта, чтобы понять ZIP или XML.
        content.Position = 0;

        var buffer = new byte[4];
        int read = await ReadExactAsync(content, buffer, 0, 4, ct);
        if (read < 4)
            throw new InvalidDataException("Stream too short");
        content.Position = 0;

        var isZip = buffer[0] == 0x50 && buffer[1] == 0x4B && buffer[2] == 0x03 && buffer[3] == 0x04;

        return isZip
            ? await ParseZipAsync(content, ct)
            : await ParseXmlAsync(content, ct);
    }

    private static async Task<TestRunResult> ParseZipAsync(Stream zip, CancellationToken ct)
    {
        using var archive = new ZipArchive(zip, ZipArchiveMode.Read, leaveOpen: true);

        foreach (var entry in archive.Entries)
        {
            if (!entry.Name.EndsWith(".trx", StringComparison.OrdinalIgnoreCase)) continue;

            await using var entryStream = entry.Open();
            return await ParseXmlAsync(entryStream, ct);
        }

        throw new InvalidOperationException("TRX file not found in archive");
    }

    private static async Task<TestRunResult> ParseXmlAsync(Stream xml, CancellationToken ct)
    {
        var settings = new XmlReaderSettings
        {
            Async = true,
            IgnoreComments = true,
            IgnoreWhitespace = true,
            IgnoreProcessingInstructions = true
        };

        int total = 0, passed = 0, failed = 0, skipped = 0;
        string? projectName = null;

        using var reader = XmlReader.Create(xml, settings);
        while (await reader.ReadAsync())
        {
            if (reader.NodeType != XmlNodeType.Element) continue;

            // 1) Быстрый путь – Counters внутри ResultSummary
            if (reader.LocalName == "Counters")
            {
                total = GetAttrInt(reader, "total");
                passed = GetAttrInt(reader, "passed");
                failed = GetAttrInt(reader, "failed");
                skipped = GetAttrInt(reader, "inconclusive") + GetAttrInt(reader, "notExecuted");
                // Можно сразу выйти – всё найдено
                break;
            }

            // 2) Извлекаем ProjectName из первого UnitTest в TestDefinitions
            if (reader.LocalName == "UnitTest" && projectName == null)
            {
                projectName = await ExtractProjectNameAsync(reader, ct);
            }

            // 3) Извлекаем ProjectName из первого TestMethod как fallback
            if (reader.LocalName == "TestMethod" && projectName == null)
            {
                var codeBase = reader.GetAttribute("codeBase");
                var className = reader.GetAttribute("className");
                projectName = ExtractProjectNameFromAttributes(codeBase, className);
            }

            // 4) Медленный путь – перечисляем UnitTestResult
            if (reader.LocalName == "UnitTestResult")
            {
                total++;
                switch (reader.GetAttribute("outcome"))
                {
                    case "Passed": passed++; break;
                    case "Failed": failed++; break;
                    case "Inconclusive":
                    case "NotExecuted":
                    case "Ignored": skipped++; break;
                }
            }
        }

        if (total == 0) total = passed + failed + skipped;

        return new TestRunResult
        {
            Total = total,
            Passed = passed,
            Failed = failed,
            Skipped = skipped,
            ProjectName = projectName
        };
    }

    private static int GetAttrInt(XmlReader r, string attr)
        => int.TryParse(r.GetAttribute(attr), out var v) ? v : 0;

    private static async Task<int> ReadExactAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken ct)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(offset + totalRead, count - totalRead), ct);
            if (read == 0)
                break; // конец потока
            totalRead += read;
        }

        return totalRead;
    }

    private static async Task<string?> ExtractProjectNameAsync(XmlReader reader, CancellationToken ct)
    {
        // Сначала пробуем из storage атрибута UnitTest
        var storage = reader.GetAttribute("storage");
        
        if (!reader.IsEmptyElement)
        {
            // Читаем вложенные элементы в поисках TestMethod с codeBase
            using var subtreeReader = reader.ReadSubtree();
            while (await subtreeReader.ReadAsync())
            {
                if (subtreeReader.NodeType == XmlNodeType.Element && subtreeReader.LocalName == "TestMethod")
                {
                    var codeBase = subtreeReader.GetAttribute("codeBase");
                    var className = subtreeReader.GetAttribute("className");
                    return ExtractProjectNameFromAttributes(codeBase ?? storage, className);
                }
            }
        }

        // Fallback: используем storage атрибут
        return ExtractProjectNameFromAttributes(storage, null);
    }

    private static string? ExtractProjectNameFromAttributes(string? assemblyPath, string? className)
    {
        // Приоритет 1: извлекаем из пути к assembly
        if (!string.IsNullOrEmpty(assemblyPath))
        {
            var projectName = Path.GetFileNameWithoutExtension(assemblyPath);
            return projectName;
        }

        // Приоритет 2: извлекаем из className (корневой namespace)
        if (!string.IsNullOrEmpty(className))
        {
            var firstDotIndex = className.IndexOf('.');
            var rootNamespace = firstDotIndex > 0 ? className[..firstDotIndex] : className;
            return rootNamespace;
        }

        return null;
    }
}