using System;
using Microsoft.Extensions.DependencyInjection;
using Shadow.Agent.DA;
using Shadow.Agent.DA.Mattermost;

namespace Shadow.Agent.Extensions.cs;

public static class MattermostExtensions
{
    private static readonly string? MattermostBaseUrl = Environment.GetEnvironmentVariable(MattermostConstants.MattermostBaseUrlKey);
    private static readonly string? MattermostBotAccessTokenName = Environment.GetEnvironmentVariable(MattermostConstants.MattermostBotAccessTokenKey);
    public static IServiceCollection AddMattermostNotifications(this IServiceCollection services)
    {
        if (string.IsNullOrEmpty(MattermostBaseUrl) || string.IsNullOrEmpty(MattermostBotAccessTokenName))
            return services;
        
        services.AddHttpClient(MattermostConstants.MattermostHttpClientKey, client =>
        {
            client.BaseAddress = new Uri(MattermostBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        
        services.AddSingleton<IMessengerClient, MattermostClient>();
        
        return services;
    }
}