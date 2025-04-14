using Projects;
using WeatherApp.Infrastructure.Messaging;
using WeatherApp.Infrastructure.Outbox;

var builder = DistributedApplication.CreateBuilder(args);

// SQL database
var sql = builder.AddSqlServer("sql", port: 54782)
    .WithLifetime(ContainerLifetime.Persistent);

var db = sql.AddDatabase("WeatherAppDb", "WeatherAppDb");

builder.AddSqlProject<WeatherAppDb>("weatherAppDbSqlProj")
    .WithConfigureDacDeployOptions(options => 
    {
        options.BlockOnPossibleDataLoss = false;
    })
    .WithReference(db).WaitFor(db);

// ASB
//var serviceBus = builder.AddConnectionString("asb").Resource; // existing namespace
var serviceBus = builder.AddAzureServiceBus("asb");
var weatherAppModelingDataAcceptedQueue = serviceBus.AddServiceBusQueue(Queues.ModelingDataAcceptedIntegrationEvent.WithPrefix());
var weatherAppModelingDataRejectedQueue = serviceBus.AddServiceBusQueue(Queues.ModelingDataRejectedIntegrationEvent.WithPrefix());
var weatherAppModelUpdatedQueue = serviceBus.AddServiceBusQueue(Queues.ModelUpdatedIntegrationEvent.WithPrefix());
var weatherAppUserNotificationQueue = serviceBus.AddServiceBusQueue(Queues.UserNotificationEvent.WithPrefix());
serviceBus.RunAsEmulator();

// Aspire Experiments
var queryabletracecollector = builder.AddProject<QueryableTraceCollector>("queryabletracecollector")
    .WithExternalHttpEndpoints()
    .WithEnvironment("Aspire:ServiceDiscovery:AllowedSchemes", "http,https")
    .WithEnvironment("Aspire:ServiceDiscovery:DefaultScheme", "http")
    .WithEnvironment("Aspire:ServiceDiscovery:DefaultPort", "80")
    .WithEnvironment("Aspire:ServiceDiscovery:DefaultHost", "localhost")
    .WithEnvironment("Aspire:ServiceDiscovery:DefaultPath", "/traces");

// External Dummy Apps
var contributorPaymentsService = builder.AddProject<ContributorPaymentsService>("contributorpaymentsservice")
    .WithExternalHttpEndpoints();

var weatherModelingService = builder.AddProject<WeatherDataModelingSystem>("weathermodelingservice")
    .WithReference(serviceBus).WaitFor(serviceBus)
    .WithEnvironment($"{ServiceBusOutboundOptions.SectionName}__{nameof(ServiceBusOutboundOptions.Entities)}__{nameof(Queues.ModelingDataAcceptedIntegrationEvent)}", Queues.ModelingDataAcceptedIntegrationEvent)
    .WithEnvironment($"{ServiceBusOutboundOptions.SectionName}__{nameof(ServiceBusOutboundOptions.Entities)}__{nameof(Queues.ModelingDataRejectedIntegrationEvent)}", Queues.ModelingDataRejectedIntegrationEvent)
    .WithEnvironment($"{ServiceBusOutboundOptions.SectionName}__{nameof(ServiceBusOutboundOptions.Entities)}__{nameof(Queues.ModelUpdatedIntegrationEvent)}", Queues.ModelUpdatedIntegrationEvent)
    .WithExternalHttpEndpoints();

builder.AddProject<NotificationService>("notificationservice")
    .WithReference(serviceBus).WaitFor(serviceBus)
    .WithReference(queryabletracecollector)
    .WithEnvironment($"{ServiceBusInboundOptions.SectionName}__{nameof(ServiceBusInboundOptions.Entities)}__{nameof(Queues.UserNotificationEvent)}", Queues.UserNotificationEvent);

// WeatherApp components
builder.AddProject<WeatherApp_API>("api")
    .WithExternalHttpEndpoints()
    .WithReference(db).WaitFor(db)
    .WithReference(contributorPaymentsService)
    .WithReference(weatherModelingService)
    .WithReference(queryabletracecollector);

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
    .WithReference(queryabletracecollector);

builder.AddProject<WeatherApp_Outbox>("outbox")
    .WithReference(db).WaitFor(db)
    .WithReference(serviceBus).WaitFor(serviceBus)
    .WithEnvironment($"{nameof(OutboxProcessorOptions)}__{nameof(OutboxProcessorOptions.IntervalBetweenBatchesInSeconds)}", "15")
    .WithReference(queryabletracecollector);

builder.Build().Run();
