using Microsoft.Extensions.DependencyInjection;
using WeatherApp.Infrastructure.MessageBus;

namespace WeatherApp.Infrastructure.Outbox;

public static class OutboxRegistrations
{
    public static IServiceCollection AddOutboxDispatcherService(this IServiceCollection services)
    {
        services.AddSingleton<IOutboxBatchRepository, OutboxBatchRepository>();
        services.AddSingleton<IUniversalMessageSender, UniversalMessageSender>();
        services.AddHostedService<OutboxDispatcherHostedService>();

        return services;
    }
}
