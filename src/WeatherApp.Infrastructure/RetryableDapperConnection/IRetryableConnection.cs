using System.Data;
using Dapper;

namespace WeatherApp.Infrastructure.RetryableDapperConnection;

public interface IRetryableConnection : IDisposable
{
    Task<IEnumerable<dynamic>> QueryAsync(string sql, DynamicParameters parameters, IDbTransactionWrapped? transaction = null, int? commandTimeout = null, CommandType? commandType = null);
    Task<int> ExecuteAsync(string sql, DynamicParameters parameters, IDbTransactionWrapped? transaction = null, int? commandTimeout = null, CommandType? commandType = null);
    Task ExecuteAsync(Func<IDbConnection, Task<bool>> action);
    Task<IEnumerable<T>> Query<T>(string sql, DynamicParameters parameters, IDbTransactionWrapped? transaction = null, int? commandTimeout = null, CommandType? commandType = null);
    Task<IEnumerable<dynamic>> Query(string sql, DynamicParameters parameters, IDbTransactionWrapped? transaction = null, int? commandTimeout = null, CommandType? commandType = null);
    Task<T?> QuerySingleOrDefault<T>(string sql, DynamicParameters parameters, IDbTransactionWrapped? transaction = null, int? commandTimeout = null, CommandType? commandType = null);
    Task<dynamic?> QuerySingleOrDefault(string sql, DynamicParameters parameters, IDbTransactionWrapped? transaction = null, int? commandTimeout = null, CommandType? commandType = null);

    IDbTransactionWrapped BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.RepeatableRead);
}