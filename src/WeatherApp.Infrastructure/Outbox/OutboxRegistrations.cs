using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using WeatherApp.Infrastructure.Messaging;

namespace WeatherApp.Infrastructure.Outbox;

public static class OutboxRegistrations
{
    public static IServiceCollection AddOutboxDispatcherService(this IServiceCollection services, IConfiguration config)
    {
        var options = config.GetSection(nameof(OutboxProcessorOptions)).Get<OutboxProcessorOptions>() ??
                      throw new Exception($"A {nameof(OutboxProcessorOptions)} config section is required.");

        var outboxProcessorOptions = Options.Create(options);
        services.AddSingleton(outboxProcessorOptions);

        services.AddSingleton<IOutboxRepository, OutboxRepository>();
        services.AddSingleton<IOutboxBatchRepository, OutboxBatchRepository>();
        services.AddSingleton<IUniversalMessageSender, UniversalMessageSender>();
        services.AddHostedService<OutboxDispatcherHostedService>();

    // Add retention hosted service and default retention options
    var retentionOptions = config.GetSection(nameof(OutboxRetentionOptions)).Get<OutboxRetentionOptions>() ?? new OutboxRetentionOptions();
    services.AddSingleton(Options.Create(retentionOptions));
    services.AddHostedService<OutboxRetentionHostedService>();

        return services;
    }

    public static IServiceCollection AddOutboxServices(this IServiceCollection services)
    {
        services
            .AddSingleton<IOutboxItemFactory, OutboxItemFactory>()
            .AddSingleton<IOutboxRepository, OutboxRepository>();

        // Default retention options registration (can be overridden in AddOutboxDispatcherService)
        services.TryAddSingleton(Options.Create(new OutboxRetentionOptions()));

        return services;
    }
}
