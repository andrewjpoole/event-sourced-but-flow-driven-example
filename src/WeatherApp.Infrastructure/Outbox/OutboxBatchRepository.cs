using Dapper;
using WeatherApp.Infrastructure.Persistence;
using WeatherApp.Infrastructure.RetryableDapperConnection;

namespace WeatherApp.Infrastructure.Outbox;

public class OutboxBatchRepository : IOutboxBatchRepository
{    
    private readonly IDbConnectionFactory dbConnectionFactory;

    public OutboxBatchRepository(IDbConnectionFactory dbConnectionFactory)
    {
        this.dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<IEnumerable<OutboxBatchItem>> GetNextBatchAsync(int batchSize, IDbTransactionWrapped dbTransactionWrapped)
    {
        const string sql = @"
            WITH LatestStatus AS
            (
                SELECT
                    OIS.[OutboxItemId],
                    OIS.[Status],
                    OIS.[NotBefore],
                    ROW_NUMBER() OVER (PARTITION BY OIS.[OutboxItemId] ORDER BY OIS.[Created] DESC) AS RowNum
                FROM [dbo].[OutboxItemStatus] OIS
            )
            SELECT TOP (@BatchSize) 
                OI.[Id],
                OI.[TypeName],
                OI.[SerialisedData],
                OI.[MessagingEntityName],
                OI.[Created],
                CAST(LS.[Status] AS INT) AS Status,                
                LS.[NotBefore]
            FROM [dbo].[OutboxItems] OI WITH (UPDLOCK, READPAST)
            LEFT JOIN LatestStatus LS
                ON OI.[Id] = LS.[OutboxItemId] AND LS.RowNum = 1
            WHERE
                LS.[OutboxItemId] IS NULL -- No status record exists yet
                OR (LS.[Status] = 2 AND LS.[NotBefore] < GETUTCDATE()) -- TransientFailure and NotBefore has passed        
                OR (LS.[Status] = 3 AND LS.[NotBefore] < GETUTCDATE()) -- Scheduled and NotBefore has passed        
            ORDER BY OI.[Created];";

        var parameters = new DynamicParameters();
        parameters.Add("BatchSize", batchSize);

        return await dbTransactionWrapped.GetConnection().Query<OutboxBatchItem>(sql, parameters, dbTransactionWrapped);        
    }
}
