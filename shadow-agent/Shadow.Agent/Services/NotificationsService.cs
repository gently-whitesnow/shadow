using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Shadow.Agent.DA;
using Shadow.Agent.Models;
using Shadow.Agent.Models.Bo;
using Shadow.Agent.Models.DbModels;

namespace Shadow.Agent.Services;

public sealed class NotificationsService(
    IEnumerable<IMessengerClient> clients) : IResultConsumer
{
    public async Task SendResultAsync(ScopeDbModel scope, TestRunMeta meta, TestRunResult result)
    {
        if (scope.MessengerNotifyReason == (int)NotifyReason.None)
            return;
        
        if (scope.MessengerNotifyReason == (int)NotifyReason.Failed && result.Failed == 0)
            return;

        var text = GetMessage(meta, result);
        foreach (var client in clients)
        {
            await client.SendMessageAsync(scope.MessengerChannelId, text);
        }
    }

    private static string GetMessage(TestRunMeta meta, TestRunResult result)
    {
        // 1. вычисляем продолжительность
        var started = DateTimeOffset.Parse(meta.StartUtc ?? "", null, DateTimeStyles.AssumeUniversal);
        var finished = DateTimeOffset.Parse(meta.FinishUtc ?? "", null, DateTimeStyles.AssumeUniversal);
        var duration = finished - started;
        string dur = duration.TotalSeconds < 1
            ? $"{duration.TotalMilliseconds:N0} ms"
            : $"{duration.TotalSeconds:N0} s";
        // 2. готовим короткий SHA и CPU-count
        string shortSha = string.Join("", (meta.Commit ?? string.Empty).Take(7));
        string cpuInfo = int.TryParse(meta.ProcessorCount, out var cpu) ? $"{cpu} CPU" : meta.ProcessorCount;

        // 3. собираем сообщение
        return
    $"""
**{result.ProjectName}**
🧪 {result.Passed} / {result.Total} ✓ ❌ {result.Failed} ⚠️ {result.Skipped}
👤 {meta.OsUser} · {meta.Scope}  💻 {meta.MachineName} ({meta.OsPlatform?.Split('-').FirstOrDefault() ?? meta.OsPlatform}, {cpuInfo})
🌿 {meta.Branch} @ {shortSha} ⏱ {dur} ({started:HH:mm:ss}→{finished:HH:mm:ss} UTC)
🔗 run {meta.RunId}
""";
    }
}