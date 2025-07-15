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
            // если хотим переиспользовать контейнер для всех тестов
            // .WithReuse(true)
            .Build();

    public Task InitializeAsync() => Container.StartAsync();
    public Task DisposeAsync()    => Container.DisposeAsync().AsTask();
}