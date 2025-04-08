using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WeatherApp.Infrastructure.Persistence;

public static class DatabaseRegistrations
{
    public static IServiceCollection AddDatabaseConnectionFactory(this IServiceCollection services)
    {        
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

        return services;
    }
}
