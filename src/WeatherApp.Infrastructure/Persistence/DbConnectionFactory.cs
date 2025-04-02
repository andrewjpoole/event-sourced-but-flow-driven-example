using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WeatherApp.Infrastructure.RetryableDapperConnection;

namespace WeatherApp.Infrastructure.Persistence;

public class DbConnectionFactory(ILogger logger, string connectionString) : IDbConnectionFactory
{
    public RetryableConnection Create()
    {
        var connection = new SqlConnection(connectionString);
        return new RetryableConnection(connection, logger);
    }
}

public static class DatabaseRegistrations
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("WeatherAppDb") ?? throw new InvalidOperationException("Connection string not found.");

        services.AddSingleton<IDbConnectionFactory>(sp => new DbConnectionFactory(sp.GetRequiredService<ILogger>(), connectionString));

        return services;
    }
}