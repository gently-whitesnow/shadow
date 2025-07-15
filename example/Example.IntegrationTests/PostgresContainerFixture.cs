using Testcontainers.PostgreSql;
using Xunit;

namespace Example.IntegrationTests;

public class PostgresContainerFixture : IAsyncLifetime
{
    public readonly PostgreSqlContainer Container =
        new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithResourceMapping(new DirectoryInfo(
                    Path.Combine(AppContext.BaseDirectory, "sql")),
                "/docker-entrypoint-initdb.d/")
            // если хотим не ронять контейнер после всех тестов и оставить до следующего dotnet test
            // .WithReuse(true)
            .Build();

    public Task InitializeAsync() => Container.StartAsync();
    public Task DisposeAsync()    => Container.DisposeAsync().AsTask();
}