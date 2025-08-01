using Dapper;
using WeatherApp.Infrastructure.Persistence;
using WeatherApp.Infrastructure.RetryableDapperConnection;
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using System.Text.Json;

namespace WeatherApp.Infrastructure.Outbox;

public class OutboxRepository(IDbConnectionFactory dbConnectionFactory) : IOutboxRepository
{
    private readonly IDbConnectionFactory dbConnectionFactory = dbConnectionFactory;

    private static readonly ActivitySource Activity = new(nameof(OutboxRepository));
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    public async Task<long> Add(OutboxItem outboxItem, IDbTransactionWrapped? transaction = null)
    {
        const string sql = @"
            INSERT INTO [dbo].[OutboxItems] ([AssociatedId], [TypeName], [SerialisedData], [MessagingEntityName], [SerialisedTelemetry], [Created])
            VALUES (@AssociatedId, @TypeName, @SerialisedData, @MessagingEntityName, @SerialisedTelemetry, @Created);
            SELECT CAST(SCOPE_IDENTITY() as BIGINT);";

        var connection = transaction == null ? 
            dbConnectionFactory.Create() : 
            transaction.GetConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@AssociatedId", outboxItem.AssociatedId);
        parameters.Add("@TypeName", outboxItem.TypeName);
        parameters.Add("@SerialisedData", outboxItem.SerialisedData);
        parameters.Add("@MessagingEntityName", outboxItem.MessagingEntityName);
        parameters.Add("@Created", outboxItem.Created);

        using (var activity = Activity.StartActivity("Outbox Item Insertion", ActivityKind.Producer))
        {
            Dictionary<string, string> telemetryDictionary = new();
            if(activity != null)
                Propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), telemetryDictionary, (carrier, key, value) => 
                {
                    carrier.Add(key, value);
                });

            parameters.Add("@SerialisedTelemetry", JsonSerializer.Serialize(telemetryDictionary));

            var insertedOutboxItemId = await connection.QuerySingleOrDefault<int>(sql, parameters, transaction);
            
            activity?.SetTag("outbox-item.TypeName", outboxItem.TypeName);
            activity?.SetTag("outbox-item.Id", insertedOutboxItemId);

            return insertedOutboxItemId;
        }
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

        using (var activity = Activity.StartActivity("Outbox Item Sent Status Insertion", ActivityKind.Producer))
        {
            activity?.SetTag("outbox-item-sent-status.OutboxItemId", outboxSentStatusUpdate.OutboxItemId);
            activity?.SetTag("outbox-item-sent-status.Status", outboxSentStatusUpdate.Status.ToString());

            await connection.ExecuteAsync(sql, parameters, transaction);
        }
    }    
}
