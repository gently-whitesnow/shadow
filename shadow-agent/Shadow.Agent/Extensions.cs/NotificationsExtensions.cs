using Microsoft.Extensions.DependencyInjection;
using Shadow.Agent.Services;

namespace Shadow.Agent.Extensions.cs;

public static class NotificationsExtensions
{
    public static IServiceCollection AddNotifications(this IServiceCollection services)
    {
        services.AddSingleton<IResultConsumer, NotificationsService>();
        services.AddMattermostNotifications();
        // todo any messenger
        
        return services;
    }
}