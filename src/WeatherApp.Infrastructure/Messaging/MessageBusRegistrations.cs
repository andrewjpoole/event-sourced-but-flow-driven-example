using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WeatherApp.Application.Services;

namespace WeatherApp.Infrastructure.Messaging;

public static class MessageBusRegistrations
{
    public static IServiceCollection AddHostedServiceBusEventListener<T, THandler>(this IServiceCollection services)
        where T : class
        where THandler : IEventHandler<T>
    {
        services.AddSingleton(typeof(IEventHandler<T>), typeof(THandler));
        services.AddHostedService<ServiceBusEventListener<T>>();

        return services;
    }

    public static IServiceCollection AddMessageSender<T>(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IMessageSender<T>, MessageSender<T>>();

        return services;
    }

    public static IServiceCollection AddUniversalMessageSender(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IUniversalMessageSender, UniversalMessageSender>();

        return services;
    }

    public static IServiceCollection AddServiceBusOutboundEntityOptions(this IServiceCollection services, IConfiguration configuration)
    {               
        var serviceBusOutboundEntityOptions = configuration.GetSection(ServiceBusOutboundOptions.RuntimeSectionName).Get<ServiceBusOutboundOptions>() ??
                                   throw new Exception($"A {nameof(ServiceBusOutboundOptions)} config section is required.");

        var outboundServiceBusOptions = Options.Create(serviceBusOutboundEntityOptions);
        services.AddSingleton(outboundServiceBusOptions);

        return services;        
    }

    public static IServiceCollection AddServiceBusInboundQueueHandlerOptions(this IServiceCollection services, IConfiguration configuration)
    {
        var queueHandlerOptions = configuration.GetSection(ServiceBusInboundOptions.RuntimeSectionName).Get<ServiceBusInboundOptions>() ??
                                  throw new Exception($"A {nameof(ServiceBusInboundOptions)} config section is required.");

        var inboundServiceBusOptions = Options.Create(queueHandlerOptions);
        services.AddSingleton(inboundServiceBusOptions);

        return services;
    }
}