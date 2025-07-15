using Dapper;
using Example.Models;
using Example.Options;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Example.DA.DbClients;

public class EntityPostgresDbClient(IOptions<PostgresOptions> postgresOptions) : IEntityPostgresDbClient
{
    private readonly NpgsqlDataSource _dataSource = NpgsqlDataSource.Create(postgresOptions.Value.ConnectionString);
    public async Task<EntityDbModel> SaveEntityAsync(string name)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();

        return await connection.QuerySingleAsync<EntityDbModel>(
            "SELECT * FROM public.create_entity(@name);",
            new {
                name = name
            }
        );
    }
}