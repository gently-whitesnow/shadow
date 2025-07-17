using Microsoft.AspNetCore.Mvc;
using Shadow.Agent.Models.DbModels;
using Shadow.Agent.Models.Dto;
using Shadow.Agent.Services;
using System.Threading.Tasks;

namespace Shadow.Agent.Controllers;

[ApiController]
[Route("v1/scopes")]
public class ScopesController(ScopesService scopesService) : ControllerBase
{

    [HttpGet]
    public  Task<ScopeDbModel[]> ListScopesAsync()
    {
        return scopesService.ListScopesAsync();
    }

    [HttpPost]
    public Task<ScopeDbModel> CreateScopeAsync(ScopeDto scope)
    {
        return scopesService.CreateScopeAsync(scope);
    }

    [HttpPut]
    public Task<ScopeDbModel> UpdateScopeAsync(ScopeDto scope)
    {
        return scopesService.UpdateScopeAsync(scope);
    }
}