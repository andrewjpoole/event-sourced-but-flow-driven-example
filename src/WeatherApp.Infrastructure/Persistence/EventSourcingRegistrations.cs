using Microsoft.Extensions.DependencyInjection;
using WeatherApp.Domain.ServiceDefinitions;

namespace WeatherApp.Infrastructure.Persistence;

public static class EventSourcingRegistrations
{
    public static IServiceCollection AddEventSourcing(this IServiceCollection services)
    {        
        services
            .AddSingleton<IDbQueryProvider, DbQueryProvider>()
            .AddSingleton<IEventPersistenceService, EventPersistenceService>()
            .AddSingleton<IEventRepository, EventRepositorySql>();

        return services;
    }
}