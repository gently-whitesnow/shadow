using System.Threading.Tasks;
using Shadow.Agent.Models.DbModels;
using Shadow.Agent.Models.Dto;

namespace Shadow.Agent.DA.Postgres;

public interface IScopesDbClient
{
    Task<ScopeDbModel> CreateScopeAsync(ScopeDto dto);
    Task<ScopeDbModel> UpdateScopeAsync(ScopeDto dto);
    Task<ScopeDbModel?> GetScopeAsync(string scopeName);
    Task<ScopeDbModel[]> ListScopesAsync();
}