using System.Data;
using Dapper;
using WeatherApp.Infrastructure.Persistence;
using WeatherApp.Infrastructure.RetryableDapperConnection;

namespace WeatherApp.Infrastructure.Outbox;

public class OutboxRepository : IOutboxRepository
{
    private readonly IDbConnectionFactory dbConnectionFactory;

    public OutboxRepository(IDbConnectionFactory dbConnectionFactory)
    {
        this.dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<long> Add(OutboxItem outboxItem, IDbTransactionWrapped? transaction = null)
    {
        const string sql = @"
            INSERT INTO [dbo].[OutboxItems] ([TypeName], [SerialisedData], [MessagingEntityName], [Created])
            VALUES (@TypeName, @SerialisedData, @MessagingEntityName, @Created);
            SELECT CAST(SCOPE_IDENTITY() as BIGINT);";

        var connection = transaction == null ? 
            dbConnectionFactory.Create() : 
            transaction.GetConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@TypeName", outboxItem.TypeName);
        parameters.Add("@SerialisedData", outboxItem.SerialisedData);
        parameters.Add("@MessagingEntityName", outboxItem.MessagingEntityName);
        parameters.Add("@Created", outboxItem.Created);

        return await connection.ExecuteAsync(sql, parameters, transaction);
    }

    public async Task AddScheduled(OutboxItem outboxItem, DateTimeOffset retryAfter)
    {
        using var connection = dbConnectionFactory.Create();
        var transaction = connection.BeginTransaction();

        var id = await Add(outboxItem, transaction);
        await AddSentStatus(OutboxSentStatusUpdate.CreateScheduled(id, retryAfter), transaction);

        transaction.Commit();
    }

    public async Task AddSentStatus(OutboxSentStatusUpdate outboxSentStatusUpdate, IDbTransactionWrapped? transaction = null)
    {
        const string sql = @"
            INSERT INTO [dbo].[OutboxItemStatus] ([OutboxItemId], [Status], [NotBefore], [Created])
            VALUES (@OutboxItemId, @Status, @NotBefore, @Created);
            SELECT CAST(SCOPE_IDENTITY() as BIGINT);";

        var connection = transaction == null ? 
            dbConnectionFactory.Create() : 
            transaction.GetConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@OutboxItemId", outboxSentStatusUpdate.OutboxItemId);
        parameters.Add("@Status", outboxSentStatusUpdate.Status);
        parameters.Add("@NotBefore", outboxSentStatusUpdate.NotBefore);
        parameters.Add("@Created", outboxSentStatusUpdate.Created);

        await connection.ExecuteAsync(sql, parameters, transaction);
    }
}
