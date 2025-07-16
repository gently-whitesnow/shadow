using Example.DA.DbClients;
using Example.DA.HttpClients;
using Example.Models;

namespace Example.BO;

public class ExampleService(IAnotherServiceHttpClient anotherServiceHttpClient, IEntityPostgresDbClient entityPostgresDbClient)
{
    public async Task<(Result, EntityDbModel?)> SaveEntity(EntityDto entityDto)
    {
        using var response = await anotherServiceHttpClient.CheckNameAsync(entityDto.Name);
        if (!response.IsSuccessStatusCode)
        {
            return (Result.BadName, null);
        }

        var entityDbModel = await entityPostgresDbClient.SaveEntityAsync(entityDto.Name);
        return (Result.Success, entityDbModel);
    }
}