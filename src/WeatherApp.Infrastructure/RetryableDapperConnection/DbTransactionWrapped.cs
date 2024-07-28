using System.Data;
using Microsoft.Extensions.Logging;

namespace WeatherApp.Infrastructure.RetryableDapperConnection;

public class DbTransactionWrapped(IDbTransaction transaction, ILogger logger) : IDbTransactionWrapped
{
    public IRetryableConnection GetConnection() => transaction.Connection != null ? new RetryableConnection(transaction.Connection, logger): throw new Exception("Transaction Connection is null.");
    public IDbTransaction ToIDbTransaction() => transaction;
    public void Commit() => transaction.Commit();
    public void Rollback() => transaction.Rollback();
    public void Dispose() => transaction.Dispose();
}