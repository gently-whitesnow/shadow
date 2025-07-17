using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Shadow.Agent.Models.DbModels;
using Shadow.Agent.Models.Dto;

namespace Shadow.Agent.DA.Postgres;

public sealed class ScopesDbClient : PostgresClient, IScopesDbClient
{
    public async Task<ScopeDbModel> CreateScopeAsync(ScopeDto dto)
    {
        await using var conn = await DataSource.OpenConnectionAsync();
        return await conn.QuerySingleAsync<ScopeDbModel>(
            "SELECT * FROM public.create_scope(@name, @messenger_channel_id, @messenger_notify_reason);",
            new { name = dto.Name, messenger_channel_id = dto.Messenger.ChannelId, messenger_notify_reason = dto.Messenger.NotifyReason }
        );
    }

    public async Task<ScopeDbModel> UpdateScopeAsync(ScopeDto dto)
    {
        await using var conn = await DataSource.OpenConnectionAsync();
        return await conn.QuerySingleAsync<ScopeDbModel>(
            "SELECT * FROM public.update_scope(@name, @messenger_channel_id, @messenger_notify_reason);",
            new { name = dto.Name, messenger_channel_id = dto.Messenger.ChannelId, messenger_notify_reason = dto.Messenger.NotifyReason }
        );
    }

    public async Task<ScopeDbModel[]> ListScopesAsync()
    {
        await using var conn = await DataSource.OpenConnectionAsync();
        return (await conn.QueryAsync<ScopeDbModel>("SELECT * FROM public.list_scopes();")).ToArray();
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