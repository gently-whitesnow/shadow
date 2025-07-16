using System.Net;
using Example.BO;
using Example.DA.DbClients;
using Example.DA.HttpClients;
using Example.Models;
using Moq;
using Xunit;

namespace Example.UnitTests;

/// <summary>
/// Пример юнит-тестов для демонстрации настройки
/// </summary>
public class ExampleServiceTests
{
    [Fact]
    public async Task SaveEntity_ShouldReturnSuccessWithModel_WhenHttpClientReturnsOk()
    {
        // Arrange
        var mockHttpClient = new Mock<IAnotherServiceHttpClient>();
        var mockDbClient = new Mock<IEntityPostgresDbClient>();
        
        var entityDto = new EntityDto { Name = "TestEntity" };
        var expectedDbModel = new EntityDbModel 
        { 
            Id = 1, 
            Name = "TestEntity", 
            CreatedAt = DateTime.UtcNow 
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        
        mockHttpClient
            .Setup(x => x.CheckNameAsync(entityDto.Name))
            .ReturnsAsync(httpResponse);
            
        mockDbClient
            .Setup(x => x.SaveEntityAsync(entityDto.Name))
            .ReturnsAsync(expectedDbModel);

        var service = new ExampleService(mockHttpClient.Object, mockDbClient.Object);

        // Act
        var (result, model) = await service.SaveEntity(entityDto);

        // Assert
        Assert.Equal(Result.Success, result);
        Assert.NotNull(model);
        Assert.Equal(expectedDbModel.Id, model.Id);
        Assert.Equal(expectedDbModel.Name, model.Name);
        
        mockHttpClient.Verify(x => x.CheckNameAsync(entityDto.Name), Times.Once);
        mockDbClient.Verify(x => x.SaveEntityAsync(entityDto.Name), Times.Once);
        
        // Cleanup
        httpResponse.Dispose();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task SaveEntity_ShouldReturnBadNameWithNullModel_WhenHttpClientReturnsError(
        HttpStatusCode httpStatusCode)
    {
        // Arrange
        var mockHttpClient = new Mock<IAnotherServiceHttpClient>();
        var mockDbClient = new Mock<IEntityPostgresDbClient>();
        
        var entityDto = new EntityDto { Name = "TestEntity" };

        var httpResponse = new HttpResponseMessage(httpStatusCode);
        
        mockHttpClient
            .Setup(x => x.CheckNameAsync(entityDto.Name))
            .ReturnsAsync(httpResponse);

        var service = new ExampleService(mockHttpClient.Object, mockDbClient.Object);

        // Act
        var (result, model) = await service.SaveEntity(entityDto);

        // Assert
        Assert.Equal(Result.BadName, result);
        Assert.Null(model);
        
        mockHttpClient.Verify(x => x.CheckNameAsync(entityDto.Name), Times.Once);
        mockDbClient.Verify(x => x.SaveEntityAsync(It.IsAny<string>()), Times.Never);
        
        // Cleanup     
        httpResponse.Dispose();  
    }


} 