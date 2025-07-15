using System.Net;
using Example.DA.HttpClients;
using Example.IntegrationTests;
using Example.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Testcontainers.PostgreSql;

public class ExampleWebAppFactory
    : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _db;

    public ExampleWebAppFactory(PostgresContainerFixture fixture) => _db = fixture.Container;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 1. Подменяем конфигурацию БД
        builder.ConfigureAppConfiguration((ctx, cfg) =>
        {
            cfg.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string,string>(
                    $"{nameof(PostgresOptions)}:ConnectionString",
                    _db.GetConnectionString())
            });
        });

        // 2. Подменяем нужные сервисы
        builder.ConfigureTestServices(services =>
        {
            // а) убираем «боевой» клиент
            services.RemoveAll<IAnotherServiceHttpClient>();

            // б) добавляем мок
            var mock = new Mock<IAnotherServiceHttpClient>();
            mock.Setup(x => x.CheckNameAsync(It.IsAny<string>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
            services.AddSingleton(mock.Object);
        });
    }
}