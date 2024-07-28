using System.Data.SqlClient;
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