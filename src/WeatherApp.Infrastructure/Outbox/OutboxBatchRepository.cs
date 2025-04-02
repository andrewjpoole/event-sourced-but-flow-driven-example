using System.Data;
using Dapper;

namespace WeatherApp.Infrastructure.Outbox;

public class OutboxBatchRepository : IOutboxBatchRepository
{
    private readonly IDbConnection _dbConnection;

    public OutboxBatchRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<IEnumerable<OutboxBatchItem>> GetNextBatchAsync(int batchSize)
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

        return await _dbConnection.QueryAsync<OutboxBatchItem>(sql, new { BatchSize = batchSize });        
    }
}
