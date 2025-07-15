using Example.Models;

namespace Example.DA.DbClients;

public interface IEntityPostgresDbClient
{
    Task<EntityDbModel> SaveEntityAsync(string name);
}