using Dapper;
using Example.BO;
using Example.Models;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Example.IntegrationTests;

[Collection("Integration")]                               // ссылка на фикстуру если хотим переиспользовать контейнер для всех тестов
public class CreateEntityTests : IClassFixture<ExampleWebAppFactory>
{
    private readonly ExampleService _sut;                 // system-under-test

    public CreateEntityTests(
        ExampleWebAppFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        _sut = scope.ServiceProvider.GetRequiredService<ExampleService>();
    }

    [Fact]
    public async Task SaveEntity_Should_Persist()
    {
        // Arrange
        var dto = new EntityDto { Name = "IntegrationTestEntity" };

        var now = DateTime.UtcNow;
        // Act
        var (result, model) = await _sut.SaveEntity(dto);

        // Assert
        Assert.Equal(Result.Success, result);
        Assert.NotNull(model);
        Assert.Equal(dto.Name, model.Name);
        Assert.Equal(1, model.Id);
        Assert.True(model.CreatedAt > now);
    }
}
