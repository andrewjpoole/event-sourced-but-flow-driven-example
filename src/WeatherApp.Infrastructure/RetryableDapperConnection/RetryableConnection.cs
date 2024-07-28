using System.Collections.Immutable;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Logging;
using Polly;

namespace WeatherApp.Infrastructure.RetryableDapperConnection;

public class RetryableConnection :IRetryableConnection
{
    private readonly IDbConnection connection;
    private readonly ILogger logger;
    private readonly AsyncPolicy sqlRetryPolicy;
    private readonly Random jitter = new();
    
    public RetryableConnection(IDbConnection connection, ILogger logger)
    {
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
        this.logger = logger;

        sqlRetryPolicy = Policy
            .Handle<DbException>(x => transientSqlErrors.ContainsKey(x.ErrorNumber))
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(50, retryAttempt)) + TimeSpan.FromMilliseconds(jitter.Next(0, 200)),
                (ex, timeSpan, retryAttempt, context) =>
                {
                    var retryableException = (DbException)ex;
                    logger.LogWarning($"A transient Sql error occured, {retryAttempt} attempt, #{retryableException.ErrorNumber}", retryableException.SqlException);
                    connection.Close();
                });
    }

    public async Task<IEnumerable<dynamic>> QueryAsync(string sql, DynamicParameters parameters, IDbTransactionWrapped? transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
        await InternalExecute(c => c.QueryAsync<IEnumerable<dynamic>>(sql, parameters, transaction?.ToIDbTransaction(), commandTimeout, commandType));

    public async Task<int> ExecuteAsync(string sql, DynamicParameters parameters, IDbTransactionWrapped? transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
        await InternalExecute(c => c.ExecuteAsync(sql, parameters, transaction?.ToIDbTransaction(), commandTimeout, commandType));

    public async Task ExecuteAsync(Func<IDbConnection, Task<bool>> action) => 
        await InternalExecute(async con => await action(con));

    public async Task<IEnumerable<T>> Query<T>(string sql, DynamicParameters parameters, IDbTransactionWrapped? transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
        await InternalExecute(c => c.QueryAsync<T>(sql, parameters, transaction?.ToIDbTransaction(), commandTimeout, commandType));

    public async Task<IEnumerable<dynamic>> Query(string sql, DynamicParameters parameters, IDbTransactionWrapped? transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
        await InternalExecute(c => c.QueryAsync(sql, parameters, transaction?.ToIDbTransaction(), commandTimeout, commandType));

    public async Task<T> QuerySingleOrDefault<T>(string sql, DynamicParameters parameters, IDbTransactionWrapped? transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
        await InternalExecute(c => c.QuerySingleOrDefaultAsync<T>(sql, parameters, transaction?.ToIDbTransaction(), commandTimeout, commandType));

    public async Task<dynamic> QuerySingleOrDefault(string sql, DynamicParameters parameters, IDbTransactionWrapped? transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
        await InternalExecute(c => c.QuerySingleOrDefaultAsync(sql, parameters, transaction?.ToIDbTransaction(), commandTimeout, commandType));

    public IDbTransactionWrapped BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.RepeatableRead)
    {
        OpenConnectionIfNotOpen();
        return new DbTransactionWrapped(connection.BeginTransaction(isolationLevel), logger);
    }

    private async Task<T> InternalExecute<T>(Func<IDbConnection, Task<T>> query)
    {
        var result = await sqlRetryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                OpenConnectionIfNotOpen();
                return await query(connection);
            }
            catch (SqlException e)
            {
                throw new DbException(e);
            }
        });
        return result;
    }

    private void OpenConnectionIfNotOpen()
    {
        if(connection.State != ConnectionState.Open)
            connection.Open();
    }

    private readonly ImmutableDictionary<int, string> transientSqlErrors = new Dictionary<int, string>
    {
         {1204,     "The instance of the SQL Server Database Engine cannot obtain a LOCK resource at this time. Rerun your statement when there are fewer active users. Ask the database administrator to check the lock and memory configuration for this instance, or to check for long-running transactions."}
        ,{1205,     "Transaction (Process ID) was deadlocked on resources with another process and has been chosen as the deadlock victim. Rerun the transaction."}
        ,{1222,     "Lock request time out period exceeded."}
        ,{49918,    "Cannot process request. Not enough resources to process request."}
        ,{49919,    "Cannot process create or update request. Too many create or update operations in progress for subscription."}
        ,{49920,    "Cannot process request. Too many operations in progress for subscription."}
        ,{4060,     "Cannot open database requested by the login. The login failed."}
        ,{4221,     "Login to read-secondary failed due to long wait on 'HADR_DATABASE_WAIT_FOR_TRANSITION_TO_VERSIONING'. The replica is not available for login because row versions are missing for transactions that were in-flight when the replica was recycled. The issue can be resolved by rolling back or committing the active transactions on the primary replica. Occurrences of this condition can be minimized by avoiding long write transactions on the primary."}
        ,{40143,    "The service has encountered an error processing your request. Please try again."}
        ,{40613,    "Database '%.*ls' on server '%.*ls' is not currently available. Please retry the connection later. If the problem persists, contact customer support, and provide them the session tracing ID of '%.*ls'."}
        ,{40501,    "The service is currently busy. Retry the request after 10 seconds. Incident ID: %ls. Code: %d."}
        ,{40540,    "The service has encountered an error processing your request. Please try again."}
        ,{40197,    "The service has encountered an error processing your request. Please try again. Error code %d."}
        ,{10929,    "Resource ID: %d. The %s minimum guarantee is %d, maximum limit is %d and the current usage for the database is %d. However, the server is currently too busy to support requests greater than %d for this database. For more information, see http://go.microsoft.com/fwlink/?LinkId=267637. Otherwise, please try again later."}
        ,{10928,    "Resource ID: %d. The %s limit for the database is %d and has been reached. For more information, see http://go.microsoft.com/fwlink/?LinkId=267637.|"}
        ,{10060,    "An error has occurred while establishing a connection to the server. When connecting to SQL Server, this failure may be caused by the fact that under the default settings SQL Server does not allow remote connections. (provider: TCP Provider, error: 0 - A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond.) (Microsoft SQL Server, Error: 10060)"}
        ,{10054,    "The data value for one or more columns overflowed the type used by the provider."}
        ,{10053,    "Could not convert the data value due to reasons other than sign mismatch or overflow."}
        ,{997,      "A connection was successfully established with the server, but then an error occurred during the login process. (provider: Named Pipes Provider, error: 0 - Overlapped I/O operation is in progress)"}
        ,{233,      "A connection was successfully established with the server, but then an error occurred during the login process. (provider: Shared Memory Provider, error: 0 - No process is on the other end of the pipe.) (Microsoft SQL Server, Error: 233)"}

    }.ToImmutableDictionary();

    public void Dispose()
    {
        connection.Dispose();
    }
}