using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Shadow.Agent.DA.Postgres;
using Shadow.Agent.Models.DbModels;
using Shadow.Agent.Models.Dto;
using Shadow.Agent.Options;

namespace Shadow.Agent.IntegrationTests;

public class TestRunsEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TestRunsEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task PostTestResults_WithValidRequest_ReturnsAccepted()
    {
        // Arrange
        var xmlContent = """
            <?xml version="1.0" encoding="utf-8"?>
            <TestRun id="12345" xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
              <ResultSummary outcome="Completed">
                <Counters total="5" executed="5" passed="4" failed="1" />
              </ResultSummary>
            </TestRun>
            """;

        var content = new StringContent(xmlContent, Encoding.UTF8, "application/xml");

        // Act
        var response = await _client.PostAsync("/v1/test-results", content);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(responseContent);
    }

    [Fact]
    public async Task PostTestResults_WithCustomHeaders_ReturnsAcceptedWithCorrectMetadata()
    {
        // Arrange
        var xmlContent = """
            <?xml version="1.0" encoding="utf-8"?>
            <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
              <ResultSummary><Counters total="1" passed="1" failed="0"/></ResultSummary>
            </TestRun>
            """;

        var content = new StringContent(xmlContent, Encoding.UTF8, "application/xml");
        
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/test-results")
        {
            Content = content
        };
        request.Headers.Add("Shadow-RunId", "test-run-123");
        request.Headers.Add("Shadow-Scope", "my-project");
        request.Headers.Add("Shadow-Branch", "main");
        request.Headers.Add("Shadow-Commit", "abc123def");
        request.Headers.Add("Shadow-MachineName", "test-machine");
        request.Headers.Add("Shadow-OsPlatform", "linux");
        request.Headers.Add("Shadow-ProcessorCount", "8");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("test-run-123", responseContent);
        Assert.Contains("my-project", responseContent);
        Assert.Contains("main", responseContent);
        Assert.Contains("abc123def", responseContent);
        Assert.Contains("test-machine", responseContent);
        Assert.Contains("linux", responseContent);
        Assert.Contains("8", responseContent);
    }

    [Fact]
    public async Task PostTestResults_WithEmptyContent_ReturnsAccepted()
    {
        // Arrange
        var content = new StringContent("", Encoding.UTF8, "application/xml");

        // Act
        var response = await _client.PostAsync("/v1/test-results", content);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }
}

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Удаляем реальный IScopesDbClient
            var scopesDbClientDescriptor = services.Single(d => d.ServiceType == typeof(IScopesDbClient));
            services.Remove(scopesDbClientDescriptor);

            // Добавляем мок IScopesDbClient
            var mockScopesClient = new Mock<IScopesDbClient>();
            mockScopesClient.Setup(x => x.GetScopeAsync(It.IsAny<string>()))
                .ReturnsAsync(new ScopeDbModel 
                { 
                    Name = "test-scope", 
                    MessengerChannelId = "test-channel",
                    MessengerNotifyReason = 0
                });
            
            mockScopesClient.Setup(x => x.CreateScopeAsync(It.IsAny<ScopeDto>()))
                .ReturnsAsync(new ScopeDbModel 
                { 
                    Name = "new-scope", 
                    MessengerChannelId = "new-channel",
                    MessengerNotifyReason = 1
                });

            mockScopesClient.Setup(x => x.UpdateScopeAsync(It.IsAny<ScopeDto>()))
                .ReturnsAsync(new ScopeDbModel 
                { 
                    Name = "updated-scope", 
                    MessengerChannelId = "updated-channel",
                    MessengerNotifyReason = 2
                });

            services.AddSingleton(mockScopesClient.Object);

                         // Настраиваем DefaultOptions для тестов
             services.AddSingleton<IOptions<DefaultOptions>>(
                 new OptionsWrapper<DefaultOptions>(new DefaultOptions { DefaultChannelId = "default-test-channel" }));
        });
    }
} 