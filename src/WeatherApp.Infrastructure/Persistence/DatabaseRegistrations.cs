using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WeatherApp.Infrastructure.Persistence;

public static class DatabaseRegistrations
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("WeatherAppDb") ?? throw new InvalidOperationException("Connection string not found.");

        services.AddSingleton<IDbConnectionFactory>(sp => new DbConnectionFactory(sp.GetRequiredService<ILogger<DbConnectionFactory>>(), connectionString));

        return services;
    }
}
