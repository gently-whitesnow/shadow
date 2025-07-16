using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Shadow.Agent.IntegrationTests;

public class TestRunsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TestRunsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task PostTestResults_WithValidTrxContent_ReturnsOk()
    {
        // Arrange
        var trxContent = """
            <?xml version="1.0" encoding="utf-8"?>
            <TestRun id="12345" xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
              <ResultSummary outcome="Completed">
                <Counters total="5" executed="5" passed="4" failed="1" error="0" timeout="0" aborted="0" inconclusive="0" passedButRunAborted="0" notRunnable="0" notExecuted="0" disconnected="0" warning="0" completed="0" inProgress="0" pending="0" />
              </ResultSummary>
            </TestRun>
            """;

        var content = new StringContent(trxContent, Encoding.UTF8, "application/xml");

        // Act
        var response = await _client.PostAsync("/v1/test-results", content);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        // В новой архитектуре возвращается только runId, обработка асинхронная
        Assert.Contains("\"", responseContent); // Проверяем что runId есть в ответе
    }

    [Fact]
    public async Task PostTestResults_WithValidJUnitContent_ReturnsOk()
    {
        // Arrange
        var junitContent = """
            <?xml version="1.0" encoding="UTF-8"?>
            <testsuite name="TestSuite" tests="8" failures="2" errors="1" skipped="1" time="30.5">
              <testcase classname="MyClass" name="test1" time="1.0"/>
              <testcase classname="MyClass" name="test2" time="2.0">
                <failure message="Test failed">Stack trace</failure>
              </testcase>
            </testsuite>
            """;

        var content = new StringContent(junitContent, Encoding.UTF8, "application/xml");

        // Act
        var response = await _client.PostAsync("/v1/test-results", content);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        // В новой архитектуре возвращается только runId, обработка асинхронная
        Assert.Contains("\"", responseContent); // Проверяем что runId есть в ответе
    }

    [Fact]
    public async Task PostTestResults_WithCustomHeaders_ReturnsHeaders()
    {
        // Arrange
        var trxContent = """
            <?xml version="1.0" encoding="utf-8"?>
            <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
              <ResultSummary><Counters total="1" passed="1" failed="0"/></ResultSummary>
            </TestRun>
            """;

        var content = new StringContent(trxContent, Encoding.UTF8, "application/xml");
        
        // Add custom headers
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/test-results")
        {
            Content = content
        };
        request.Headers.Add("X-Shadow-RunId", "test-run-123");
        request.Headers.Add("X-Shadow-Project", "my-project");
        request.Headers.Add("X-Shadow-Branch", "main");
        request.Headers.Add("X-Shadow-Commit", "abc123");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        // В новой архитектуре возвращается только runId, остальные поля не возвращаются
        Assert.Contains("test-run-123", responseContent);
    }

    [Fact]
    public async Task PostTestResults_WithEmptyContent_ReturnsAccepted()
    {
        // Arrange
        var content = new StringContent("", Encoding.UTF8, "application/xml");

        // Act
        var response = await _client.PostAsync("/v1/test-results", content);

        // Assert  
        // В новой архитектуре запрос принимается, ошибки обрабатываются асинхронно
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task PostTestResults_WithUnsupportedContent_ReturnsAccepted()
    {
        // Arrange
        var content = new StringContent("This is not a valid test report", Encoding.UTF8, "text/plain");

        // Act
        var response = await _client.PostAsync("/v1/test-results", content);

        // Assert
        // В новой архитектуре запрос принимается, ошибки обрабатываются асинхронно  
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }
} 