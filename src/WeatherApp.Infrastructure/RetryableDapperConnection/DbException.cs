using Microsoft.Data.SqlClient;

namespace WeatherApp.Infrastructure.RetryableDapperConnection;

public class DbException(int errorNumber) : Exception
{
    public int ErrorNumber { get; } = errorNumber;
    public SqlException? SqlException { get; }

    public DbException(SqlException sqlException) : this(sqlException.Number)
    {
        SqlException = sqlException;
    }
}