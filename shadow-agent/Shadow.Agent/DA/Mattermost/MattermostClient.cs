using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shadow.Agent.DA.Mattermost.HttpModels;

namespace Shadow.Agent.DA.Mattermost;

public class MattermostClient(IHttpClientFactory httpClientFactory, ILogger<MattermostClient> logger) : IMessengerClient
{
    private const string PostMessageUrl = "/api/v4/posts";
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(MattermostConstants.MattermostHttpClientKey);

    private readonly string? _botAccessToken =
        Environment.GetEnvironmentVariable(MattermostConstants.MattermostBotAccessTokenKey)
        ?? throw new ApplicationException($"Не задан токен mattermost, env=[{MattermostConstants.MattermostBotAccessTokenKey}]");

    private JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public async Task SendMessageAsync(string channelId, string text)
    {
        var messageRequest = new CreateMattermostMessageRequest
        {
            ChannelId = channelId,
            Message = text
        };

        var request = new HttpRequestMessage(HttpMethod.Post, PostMessageUrl)
        {
            Content = JsonContent.Create(messageRequest, options: _jsonSerializerOptions)
        };

        request.Headers.Add("Authorization", $"Bearer {_botAccessToken}");

        try
        {
            using var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
                return;
            var responseError = await response.Content.ReadFromJsonAsync<MattermostMessageResponse>(_jsonSerializerOptions);
            logger.LogError("Error sending message to mattermost, channelId=[{channelId}], text=[{text}], responseError=[{responseError}]", channelId, text, responseError);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending message to mattermost, channelId=[{channelId}], text=[{text}]", channelId, text);
        }
    }
}