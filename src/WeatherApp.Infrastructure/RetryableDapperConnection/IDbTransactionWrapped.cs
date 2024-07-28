using System.Data;

namespace WeatherApp.Infrastructure.RetryableDapperConnection;

public interface IDbTransactionWrapped
{
    IRetryableConnection GetConnection();
    IDbTransaction ToIDbTransaction();
    void Commit();
    void Rollback();
}