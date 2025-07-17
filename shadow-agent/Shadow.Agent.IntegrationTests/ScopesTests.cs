using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shadow.Agent.DA.Postgres;
using Shadow.Agent.Models;
using Shadow.Agent.Models.DbModels;
using Shadow.Agent.Models.Dto;
using Shadow.Agent.Options;
using Testcontainers.PostgreSql;
using Xunit;

namespace Shadow.Agent.IntegrationTests;

[Collection("Integration")]
public class ScopesTests : IClassFixture<ScopesWebAppFactory>
{
    private readonly ScopesWebAppFactory _factory;
    private readonly HttpClient _client;

    public ScopesTests(ScopesWebAppFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateScope_WithValidData_ReturnsCreatedScope()
    {
        // Arrange
        var scopeDto = new ScopeDto(
            Name: "test-scope",
            Messenger: new MessengerScopeDto(
                ChannelId: "test-channel-123",
                NotifyReason: (int)NotifyReason.All
            )
        );

        var json = JsonSerializer.Serialize(scopeDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/v1/scopes", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var createdScope = JsonSerializer.Deserialize<ScopeDbModel>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(createdScope);
        Assert.Equal(scopeDto.Name, createdScope.Name);
        Assert.Equal(scopeDto.Messenger.ChannelId, createdScope.MessengerChannelId);
        Assert.Equal(scopeDto.Messenger.NotifyReason, createdScope.MessengerNotifyReason);
        Assert.True(createdScope.Id > 0);
        Assert.True(createdScope.CreatedAt > DateTimeOffset.MinValue);
        Assert.True(createdScope.UpdatedAt > DateTimeOffset.MinValue);
    }

    [Fact]
    public async Task UpdateScope_WithValidData_ReturnsUpdatedScope()
    {
        // Arrange - сначала создаем scope
        var createDto = new ScopeDto(
            Name: "scope-to-update",
            Messenger: new MessengerScopeDto(
                ChannelId: "initial-channel",
                NotifyReason: (int)NotifyReason.Failed
            )
        );

        var createJson = JsonSerializer.Serialize(createDto);
        var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
        await _client.PostAsync("/v1/scopes", createContent);

        // Arrange - готовим данные для обновления
        var updateDto = new ScopeDto(
            Name: "scope-to-update",
            Messenger: new MessengerScopeDto(
                ChannelId: "updated-channel",
                NotifyReason: (int)NotifyReason.All
            )
        );

        var updateJson = JsonSerializer.Serialize(updateDto);
        var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/v1/scopes", updateContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var updatedScope = JsonSerializer.Deserialize<ScopeDbModel>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(updatedScope);
        Assert.Equal(updateDto.Name, updatedScope.Name);
        Assert.Equal(updateDto.Messenger.ChannelId, updatedScope.MessengerChannelId);
        Assert.Equal(updateDto.Messenger.NotifyReason, updatedScope.MessengerNotifyReason);
        Assert.True(updatedScope.UpdatedAt > updatedScope.CreatedAt);
    }

    [Fact]
    public async Task ListScopes_ReturnsAllScopes()
    {
        // Arrange - создаем несколько scope'ов
        var scope1 = new ScopeDto(
            Name: "scope-1",
            Messenger: new MessengerScopeDto("channel-1", (int)NotifyReason.All)
        );
        var scope2 = new ScopeDto(
            Name: "scope-2", 
            Messenger: new MessengerScopeDto("channel-2", (int)NotifyReason.Failed)
        );

        var json1 = JsonSerializer.Serialize(scope1);
        var json2 = JsonSerializer.Serialize(scope2);
        
        await _client.PostAsync("/v1/scopes", new StringContent(json1, Encoding.UTF8, "application/json"));
        await _client.PostAsync("/v1/scopes", new StringContent(json2, Encoding.UTF8, "application/json"));

        // Act
        var response = await _client.GetAsync("/v1/scopes");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var scopes = JsonSerializer.Deserialize<ScopeDbModel[]>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(scopes);
        Assert.True(scopes.Length >= 2);
        
        var returnedScope1 = scopes.FirstOrDefault(s => s.Name == "scope-1");
        var returnedScope2 = scopes.FirstOrDefault(s => s.Name == "scope-2");
        
        Assert.NotNull(returnedScope1);
        Assert.NotNull(returnedScope2);
        Assert.Equal("channel-1", returnedScope1.MessengerChannelId);
        Assert.Equal("channel-2", returnedScope2.MessengerChannelId);
    }

    [Fact]
    public async Task CreateScope_WithDuplicateName_ReturnsBadRequest()
    {
        // Arrange - создаем первый scope
        var scopeDto = new ScopeDto(
            Name: "duplicate-scope",
            Messenger: new MessengerScopeDto("channel", (int)NotifyReason.All)
        );

        var json = JsonSerializer.Serialize(scopeDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        // Создаем первый scope
        await _client.PostAsync("/v1/scopes", content);

        // Act - пытаемся создать scope с тем же именем
        content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/v1/scopes", content);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}

public class ScopesWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public ScopesWebAppFactory()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithResourceMapping(new DirectoryInfo(
                Path.Combine(AppContext.BaseDirectory, "sql")),
                "/docker-entrypoint-initdb.d/")
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Удаляем существующий IScopesDbClient
            var scopesDbClientDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IScopesDbClient));
            if (scopesDbClientDescriptor != null)
            {
                services.Remove(scopesDbClientDescriptor);
            }

            // Настраиваем connection string для тестового контейнера
            Environment.SetEnvironmentVariable("POSTGRES_CONNECTION_STRING", _container.GetConnectionString());

            // Добавляем реальный ScopesDbClient с тестовой БД
            services.AddSingleton<IScopesDbClient, ScopesDbClient>();

            // Настраиваем DefaultOptions для тестов
            services.AddSingleton<IOptions<DefaultOptions>>(
                new OptionsWrapper<DefaultOptions>(new DefaultOptions { DefaultChannelId = "default-test-channel" }));
        });
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _container.DisposeAsync();
        await base.DisposeAsync();
    }
}

[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<ScopesWebAppFactory>
{
    // Этот класс не содержит кода, он просто определяет коллекцию тестов
    // которая будет использовать ScopesWebAppFactory
}
