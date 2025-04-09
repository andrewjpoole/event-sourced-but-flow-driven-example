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
            INSERT INTO [dbo].[OutboxItems] ([TypeName], [SerialisedData], [MessagingEntityName], [SerialisedTelemetry], [Created])
            VALUES (@TypeName, @SerialisedData, @MessagingEntityName, @SerialisedTelemetry, @Created);
            SELECT CAST(SCOPE_IDENTITY() as BIGINT);";

        var connection = transaction == null ? 
            dbConnectionFactory.Create() : 
            transaction.GetConnection();

        var parameters = new DynamicParameters();
        parameters.Add("@TypeName", outboxItem.TypeName);
        parameters.Add("@SerialisedData", outboxItem.SerialisedData);
        parameters.Add("@MessagingEntityName", outboxItem.MessagingEntityName);
        parameters.Add("@Created", outboxItem.Created);

        using var activity = Activity.StartActivity("Outbox Item Insertion", ActivityKind.Producer);

        KeyValuePair<string, string> telemetry = default;
        if(activity != null)
            Propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), telemetry, (carrier, key, value) => 
            {
                telemetry = new KeyValuePair<string, string>(key, value);
            });

        activity?.SetTag("messaging.system", "outbox");

        parameters.Add("@SerialisedTelemetry", JsonSerializer.Serialize(telemetry));

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
