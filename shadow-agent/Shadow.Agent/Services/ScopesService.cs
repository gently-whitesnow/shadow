using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Shadow.Agent.DA.Postgres;
using Shadow.Agent.Models;
using Shadow.Agent.Models.DbModels;
using Shadow.Agent.Models.Dto;
using Shadow.Agent.Options;


namespace Shadow.Agent.Services;

public class ScopesService(IOptions<DefaultOptions> defaultOptions, IScopesDbClient scopesDbClient)
{
    public async Task<ScopeDbModel> GetScopeAsync(string? scopeName)
    {
        if (string.IsNullOrEmpty(scopeName))
        {
            return GetDefaultScope(defaultOptions.Value.DefaultChannelId);
        }

        var scope = await scopesDbClient.GetScopeAsync(scopeName);
        if (scope == null)
        {
            scope = GetDefaultScope(defaultOptions.Value.DefaultChannelId);
        }
        return scope;
    }

    public Task<ScopeDbModel[]> ListScopesAsync()
    {
        return scopesDbClient.ListScopesAsync();
    }

    public Task<ScopeDbModel> CreateScopeAsync(ScopeDto scope)
    {
        return scopesDbClient.CreateScopeAsync(scope);
    }

    public Task<ScopeDbModel> UpdateScopeAsync(ScopeDto scope)
    {
        return scopesDbClient.UpdateScopeAsync(scope);
    }

    private static ScopeDbModel GetDefaultScope(string defaultChannelId)
    {
        return new ScopeDbModel
        {
            Name = "default",
            MessengerChannelId = defaultChannelId,
            MessengerNotifyReason = (int)NotifyReason.All
        };
    }
}