using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using WeatherApp.Domain.Logging;
using WeatherApp.Infrastructure.RetryableDapperConnection;

namespace WeatherApp.Infrastructure.Persistence;

public class DbConnectionFactory(ILogger<DbConnectionFactory> logger, string connectionString) : IDbConnectionFactory
{
    public RetryableConnection Create()
    {
        try
        {
            var connection = new SqlConnection(connectionString);
            return new RetryableConnection(connection, logger);
        }
        catch (Exception ex)
        {
            logger.LogFailedToConectToDatabase(ex);
            throw;
        }
        
    }
}
