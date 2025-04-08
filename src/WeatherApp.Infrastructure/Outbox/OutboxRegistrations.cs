using Microsoft.Extensions.DependencyInjection;
using WeatherApp.Infrastructure.MessageBus;

namespace WeatherApp.Infrastructure.Outbox;

public static class OutboxRegistrations
{
    public static IServiceCollection AddOutboxDispatcherService(this IServiceCollection services)
    {
        services.AddSingleton<IOutboxRepository, OutboxRepository>();
        services.AddSingleton<IOutboxBatchRepository, OutboxBatchRepository>();
        services.AddSingleton<IUniversalMessageSender, UniversalMessageSender>();
        services.AddHostedService<OutboxDispatcherHostedService>();

        return services;
    }

    public static IServiceCollection AddOutboxServices(this IServiceCollection services)
    {
        services
            .AddSingleton<IOutboxItemFactory, OutboxItemFactory>()
            .AddSingleton<IOutboxRepository, OutboxRepository>();

        return services;
    }
}
