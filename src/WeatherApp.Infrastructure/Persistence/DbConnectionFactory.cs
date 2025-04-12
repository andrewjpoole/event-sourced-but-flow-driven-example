using System.ComponentModel;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WeatherApp.Domain.Logging;
using WeatherApp.Infrastructure.RetryableDapperConnection;

namespace WeatherApp.Infrastructure.Persistence;

public class DbConnectionFactory(
    ILogger<DbConnectionFactory> logger, 
    IServiceScopeFactory serviceScopeFactory    
    ) : IDbConnectionFactory
{
    public IRetryableConnection Create()
    {
        try
        {

            var scope = serviceScopeFactory.CreateScope();
            
            var sqlConnection = scope.ServiceProvider.GetService<SqlConnection>() ?? 
                throw new InvalidOperationException("SqlConnection not found in service provider.");
            
            return new RetryableConnection(sqlConnection, logger);
        }
        catch (Exception ex)
        {
            logger.LogFailedToConectToDatabase(ex);
            throw;
        }
        
    }
}
