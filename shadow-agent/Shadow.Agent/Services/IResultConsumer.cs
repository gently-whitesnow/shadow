using System.Threading.Tasks;
using Shadow.Agent.Models.Bo;
using Shadow.Agent.Models.DbModels;

namespace Shadow.Agent.Services;

public interface IResultConsumer
{
    Task SendResultAsync(ScopeDbModel scope, TestRunMeta meta, TestRunResult result);
}