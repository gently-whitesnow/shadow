using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Shadow.Agent.DA.Postgres;
using Shadow.Agent.Models.DbModels;
using Shadow.Agent.Models.Dto;
using Shadow.Agent.Options;


namespace Shadow.Agent.Services;

public class ScopesService(IOptions<DefaultOptions> defaultOptions, ScopesDbClient scopesDbClient)
{
    public async Task<ScopeDbModel> GetScopeAsync(string scopeName)
    {
        var scope = await scopesDbClient.GetScopeAsync(scopeName);
        if (scope == null)
        {
            scope = new ScopeDbModel
            {
                Name = "default",
                MessengerChannelId = defaultOptions.Value.DefaultChannelId
            };
        }
        return scope;
    }

    public Task<ScopeDbModel> CreateScopeAsync(ScopeDto scope)
    {
        return scopesDbClient.CreateScopeAsync(scope);
    }

    public Task<ScopeDbModel> UpdateScopeAsync(ScopeDto scope)
    {
        return scopesDbClient.UpdateScopeAsync(scope);
    }
}