using System.Threading.Tasks;
using Dapper;
using Shadow.Agent.Models.DbModels;
using Shadow.Agent.Models.Dto;

namespace Shadow.Agent.DA.Postgres;

public sealed class ScopesDbClient : PostgresClient
{
    public async Task<ScopeDbModel> CreateScopeAsync(ScopeDto dto)
    {
        await using var conn = await DataSource.OpenConnectionAsync();
        return await conn.QuerySingleAsync<ScopeDbModel>(
            "SELECT * FROM public.create_scope(@name, @messenger_channel_id, @notify_reason);",
            new { name = dto.Name, messengerChannelId = dto.Messenger.ChannelId, notifyReason = dto.Messenger.NotifyReason }
        );
    }

    public async Task<ScopeDbModel> UpdateScopeAsync(ScopeDto dto)
    {
        await using var conn = await DataSource.OpenConnectionAsync();
        return await conn.QuerySingleAsync<ScopeDbModel>(
            "SELECT * FROM public.update_scope(@name, @messenger_channel_id, @notify_reason);",
            new { name = dto.Name, messengerChannelId = dto.Messenger.ChannelId, notifyReason = dto.Messenger.NotifyReason }
        );
    }

    public async Task<ScopeDbModel?> GetScopeAsync(string scopeName)
    {
        await using var conn = await DataSource.OpenConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<ScopeDbModel>(
            "SELECT * FROM public.get_scope(@name);",
            new { name = scopeName }
        );
    }
}