using System.Data;
using Dapper;

namespace WeatherApp.Infrastructure.Outbox;

public class OutboxRepository : IOutboxRepository
{
    private readonly IDbConnection _dbConnection;

    public OutboxRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<long> Add(OutboxItem outboxItem, IDbTransaction? transaction = null)
    {
        const string sql = @"
            INSERT INTO [dbo].[OutboxItems] ([TypeName], [SerialisedData], [MessagingEntityName], [Created])
            VALUES (@TypeName, @SerialisedData, @MessagingEntityName, @Created);
            SELECT CAST(SCOPE_IDENTITY() as BIGINT);";

        return await _dbConnection.ExecuteScalarAsync<long>(sql, outboxItem, transaction);
    }

    public async Task<long> AddScheduled(OutboxItem outboxItem, DateTimeOffset retryAfter)
    {
        _dbConnection.Open();
        using var transaction = _dbConnection.BeginTransaction();

        var id = await Add(outboxItem, transaction);
        await AddSentStatus(OutboxSentStatusUpdate.CreateScheduled(id, retryAfter), transaction);

        transaction.Commit();

        return id;
    }

    public async Task<long> AddSentStatus(OutboxSentStatusUpdate outboxSentStatusUpdate, IDbTransaction? transaction = null)
    {
        const string sql = @"
            INSERT INTO [dbo].[OutboxItemStatus] ([OutboxItemId], [Status], [NotBefore], [Created])
            VALUES (@OutboxItemId, @Status, @NotBefore, @Created);
            SELECT CAST(SCOPE_IDENTITY() as BIGINT);";

        return await _dbConnection.ExecuteScalarAsync<long>(sql, outboxSentStatusUpdate, transaction);
    }
}
