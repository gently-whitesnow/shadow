using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Shadow.Agent.Models.Bo;

namespace Shadow.Agent.Parsers;

public sealed class JUnitParser : IResultParser
{
    private static ReadOnlySpan<byte> Suite => "<testsuite"u8;
    private static ReadOnlySpan<byte> Suites => "<testsuites"u8;

    public bool CanParse(ReadOnlySpan<byte> preview)
    {
        return preview.IndexOf(Suite) >= 0 || preview.IndexOf(Suites) >= 0;
    }

    public async Task<TestRunResult> ParseAsync(Stream content, CancellationToken ct = default)
    {
        var settings = new XmlReaderSettings
        {
            Async = true,
            IgnoreComments = true,
            IgnoreWhitespace = true,
            IgnoreProcessingInstructions = true
        };

        int total = 0, failures = 0, errors = 0, skipped = 0;

        using var reader = XmlReader.Create(content, settings);

        while (await reader.ReadAsync())
        {
            if (reader.NodeType != XmlNodeType.Element) continue;

            switch (reader.LocalName)
            {
                case "testsuite":
                    // чаще всего все суммы уже в атрибутах
                    total += GetInt(reader, "tests");
                    failures += GetInt(reader, "failures");
                    errors += GetInt(reader, "errors");
                    skipped += GetInt(reader, "skipped");
                    return new TestRunResult
                    {
                        Total = total,
                        Passed = total - failures - errors - skipped,
                        Failed = failures + errors,
                        Skipped = skipped
                    };

                case "testcase":
                    total++;

                    if (reader.IsEmptyElement) break;

                    // читаем вложенные элементы текущего testcase
                    while (await reader.ReadAsync())
                    {
                        if (reader.NodeType == XmlNodeType.EndElement &&
                            reader.LocalName == "testcase") break;

                        if (reader.NodeType != XmlNodeType.Element) continue;

                        switch (reader.LocalName)
                        {
                            case "failure": failures++; break;
                            case "error": errors++; break;
                            case "skipped": skipped++; break;
                        }
                    }
                    break;
            }
        }

        // если атрибутов не было
        if (total == 0) total = failures + errors + skipped;

        var failed = failures + errors;
        var passed = total - failed - skipped;

        return new TestRunResult
        {
            Total = total,
            Passed = passed,
            Failed = failed,
            Skipped = skipped
        };
    }

    private static int GetInt(XmlReader r, string attrName)
        => int.TryParse(r.GetAttribute(attrName), out var v) ? v : 0;
}