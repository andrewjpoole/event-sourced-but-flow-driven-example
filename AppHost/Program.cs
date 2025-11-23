using Projects;
using WeatherApp.Infrastructure.Messaging;
using WeatherApp.Infrastructure.Outbox;

var builder = DistributedApplication.CreateBuilder(args);

// SQL database
var sql = builder.AddSqlServer("sql", port: 54782)
    .WithLifetime(ContainerLifetime.Persistent);

var db = sql.AddDatabase("WeatherAppDb", "WeatherAppDb");

builder.AddSqlProject<WeatherAppDb>("weatherAppDbSqlProj")
    .WithSkipWhenDeployed()
    .WithConfigureDacDeployOptions(options => 
    {
        options.BlockOnPossibleDataLoss = false;
    })
    .WithReference(db).WaitFor(db);

// ASB
// if (builder.ExecutionContext.IsPublishMode || builder.Environment.IsProduction()) {}
//var serviceBus = builder.AddConnectionString("asb").Resource; // existing namespace
var serviceBus = builder.AddAzureServiceBus("asb");
var weatherAppModelingDataAcceptedQueue = serviceBus.AddServiceBusQueue(Queues.ModelingDataAcceptedIntegrationEvent);
var weatherAppModelingDataRejectedQueue = serviceBus.AddServiceBusQueue(Queues.ModelingDataRejectedIntegrationEvent);
var weatherAppModelUpdatedQueue = serviceBus.AddServiceBusQueue(Queues.ModelUpdatedIntegrationEvent);
var weatherAppUserNotificationQueue = serviceBus.AddServiceBusQueue(Queues.UserNotificationEvent);
serviceBus.RunAsEmulator(
    x => x.WithLifetime(ContainerLifetime.Persistent));

//builder.AddAsbEmulatorUi("asb-ui", serviceBus);

// Queryable Trace Collector for integration test assertions against collected trace data
var queryableTraceCollectorApiKey = builder.Configuration["QueryableTraceCollectorApiKey"] ?? "123456789";
var queryabletracecollector = builder.AddQueryableTraceCollector("queryabletracecollector", queryableTraceCollectorApiKey)
    .WithExternalHttpEndpoints()
    .ExcludeFromManifest();

// External Dummy Apps
var contributorPaymentsService = builder.AddProject<ContributorPaymentsService>("contributorpaymentsservice")
    .WithExternalHttpEndpoints()
    .ExcludeFromManifest();

var weatherModelingService = builder.AddProject<WeatherDataModelingSystem>("weathermodelingservice")
    .WithReference(serviceBus).WaitFor(serviceBus)
    .WithEnvironment($"{ServiceBusOutboundOptions.SectionName}__{nameof(ServiceBusOutboundOptions.Entities)}__{nameof(Queues.ModelingDataAcceptedIntegrationEvent)}", Queues.ModelingDataAcceptedIntegrationEvent)
    .WithEnvironment($"{ServiceBusOutboundOptions.SectionName}__{nameof(ServiceBusOutboundOptions.Entities)}__{nameof(Queues.ModelingDataRejectedIntegrationEvent)}", Queues.ModelingDataRejectedIntegrationEvent)
    .WithEnvironment($"{ServiceBusOutboundOptions.SectionName}__{nameof(ServiceBusOutboundOptions.Entities)}__{nameof(Queues.ModelUpdatedIntegrationEvent)}", Queues.ModelUpdatedIntegrationEvent)
    .WithExternalHttpEndpoints()
    .ExcludeFromManifest();

builder.AddProject<NotificationService>("notificationservice")
    .WithReference(serviceBus).WaitFor(serviceBus)
    .WithReference(queryabletracecollector)
    .WithEnvironment("QueryableTraceCollectorApiKey", queryableTraceCollectorApiKey)
    .WithEnvironment($"{ServiceBusInboundOptions.SectionName}__{nameof(ServiceBusInboundOptions.Entities)}__{nameof(Queues.UserNotificationEvent)}", Queues.UserNotificationEvent)
    .ExcludeFromManifest();

// WeatherApp components
builder.AddProject<WeatherApp_API>("api")
    .WithExternalHttpEndpoints()
    .WithReference(db).WaitFor(db)
    .WithReference(contributorPaymentsService)
    .WithReference(weatherModelingService)
    .WithReference(queryabletracecollector)
    .WithEnvironment("QueryableTraceCollectorApiKey", queryableTraceCollectorApiKey);

builder.AddProject<WeatherApp_EventListener>("eventlistener")
    .WithReference(db).WaitFor(db)
    .WithReference(serviceBus).WaitFor(serviceBus)
    .WithEnvironment($"{ServiceBusInboundOptions.SectionName}__{nameof(ServiceBusInboundOptions.InitialBackoffInMs)}", "2000")
    .WithEnvironment($"{ServiceBusInboundOptions.SectionName}__{nameof(ServiceBusInboundOptions.MaxConcurrentCalls)}", "1")
    .WithEnvironment($"{ServiceBusInboundOptions.SectionName}__{nameof(ServiceBusInboundOptions.Entities)}__{nameof(Queues.ModelingDataAcceptedIntegrationEvent)}", Queues.ModelingDataAcceptedIntegrationEvent)
    .WithEnvironment($"{ServiceBusInboundOptions.SectionName}__{nameof(ServiceBusInboundOptions.Entities)}__{nameof(Queues.ModelingDataRejectedIntegrationEvent)}", Queues.ModelingDataRejectedIntegrationEvent)
    .WithEnvironment($"{ServiceBusInboundOptions.SectionName}__{nameof(ServiceBusInboundOptions.Entities)}__{nameof(Queues.ModelUpdatedIntegrationEvent)}", Queues.ModelUpdatedIntegrationEvent)
    .WithEnvironment($"{ServiceBusOutboundOptions.SectionName}__{nameof(ServiceBusOutboundOptions.Entities)}__{nameof(Queues.UserNotificationEvent)}", Queues.UserNotificationEvent)
    .WithReference(weatherModelingService)
    .WithReference(contributorPaymentsService)
    .WithReference(queryabletracecollector)
    .WithEnvironment("QueryableTraceCollectorApiKey", queryableTraceCollectorApiKey);

builder.AddProject<WeatherApp_Outbox>("outbox")
    .WithReference(db).WaitFor(db)
    .WithReference(serviceBus).WaitFor(serviceBus)
    .WithEnvironment($"{nameof(OutboxProcessorOptions)}__{nameof(OutboxProcessorOptions.IntervalBetweenBatchesInSeconds)}", "5")
    .WithReference(queryabletracecollector)
    .WithEnvironment("QueryableTraceCollectorApiKey", queryableTraceCollectorApiKey);

builder.Build().Run();
