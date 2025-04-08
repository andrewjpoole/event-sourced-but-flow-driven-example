using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Sql database
var sql = builder.AddSqlServer("sql", port: 54782)
    .WithLifetime(ContainerLifetime.Persistent);

var overridenConnectionsString = builder.AddConnectionString("sqloverriden").Resource;
var db = sql.AddDatabase("WeatherAppDb", "WeatherAppDb");

builder.AddSqlProject<WeatherAppDb>("weatherAppDbSqlProj")
    .WithConfigureDacDeployOptions(options => 
    {
        options.BlockOnPossibleDataLoss = false;
    })
    .WithReference(db)
    .WaitFor(db);

// ASB
//var serviceBus = builder.AddConnectionString("asb").Resource; // existing namespace
var serviceBus = builder.AddAzureServiceBus("asb");
var weatherAppModelingDataAcceptedQueue = serviceBus.AddServiceBusQueue(Queues.ModelingDataAcceptedIntegrationEvent.WithPrefix());
var weatherAppModelingDataRejectedQueue = serviceBus.AddServiceBusQueue(Queues.ModelingDataRejectedIntegrationEvent.WithPrefix());
var weatherAppModelUpdatedQueue = serviceBus.AddServiceBusQueue(Queues.ModelUpdatedIntegrationEvent.WithPrefix());
var weatherAppUserNotificationQueue = serviceBus.AddServiceBusQueue(Queues.UserNotificationEvent.WithPrefix());
serviceBus.RunAsEmulator();

// External Dummy Apps
var contributorPaymentsService = builder.AddProject<ContributorPaymentsService>("contributorpaymentsservice")
    .WithExternalHttpEndpoints();

var weatherModelingService = builder.AddProject<WeatherDataModelingSystem>("weathermodelingservice")
    .WithReference(serviceBus)
    .WithEnvironment("ServiceBusSettings__FullyQualifiedNamespace", "sbusdev001coachcandor")
    .WithEnvironment($"ServiceBus__Outbound__Names__{nameof(Queues.ModelingDataAcceptedIntegrationEvent)}", Queues.ModelingDataAcceptedIntegrationEvent)
    .WithEnvironment($"ServiceBus__Outbound__Names__{nameof(Queues.ModelingDataRejectedIntegrationEvent)}", Queues.ModelingDataRejectedIntegrationEvent)
    .WithEnvironment($"ServiceBus__Outbound__Names__{nameof(Queues.ModelUpdatedIntegrationEvent)}", Queues.ModelUpdatedIntegrationEvent)
    .WithExternalHttpEndpoints();

builder.AddProject<NotificationService>("notificationservice")
    .WithReference(serviceBus)
    .WithEnvironment($"ServiceBus__Inbound__Names__{nameof(Queues.UserNotificationEvent)}", Queues.UserNotificationEvent);

// WeatherApp components
builder.AddProject<WeatherApp_API>("api")
    .WithExternalHttpEndpoints()
    .WithReference(db).WaitFor(db)
    .WithReference(contributorPaymentsService)
    .WithReference(weatherModelingService)
    .WithEnvironment("WeatherModelingServiceOptions__MaxRetryCount", "3")
    .WithEnvironment("ContributorPaymentServiceOptions__MaxRetryCount", "3");

builder.AddProject<WeatherApp_EventListener>("eventlistener")
    .WithReference(db).WaitFor(db)
    .WithReference(serviceBus)
    .WithEnvironment("ServiceBus__Inbound__InitialBackoffInMs", "2000")
    .WithEnvironment("ServiceBus__Inbound__MaxConcurrentCalls", "1")
    .WithEnvironment($"ServiceBus__Inbound__Names__{nameof(Queues.ModelingDataAcceptedIntegrationEvent)}", Queues.ModelingDataAcceptedIntegrationEvent)
    .WithEnvironment($"ServiceBus__Inbound__Names__{nameof(Queues.ModelingDataRejectedIntegrationEvent)}", Queues.ModelingDataRejectedIntegrationEvent)
    .WithEnvironment($"ServiceBus__Inbound__Names__{nameof(Queues.ModelUpdatedIntegrationEvent)}", Queues.ModelUpdatedIntegrationEvent)
    .WithEnvironment($"ServiceBus__Outbound__Names__{nameof(Queues.UserNotificationEvent)}", Queues.UserNotificationEvent)
    .WithReference(weatherModelingService)
    .WithEnvironment("WeatherModelingServiceOptions__MaxRetryCount", "3")
    .WithReference(contributorPaymentsService)
    .WithEnvironment("ContributorPaymentServiceOptions__MaxRetryCount", "3");

builder.AddProject<WeatherApp_Outbox>("outbox")
    .WithReference(db).WaitFor(db)
    .WithReference(serviceBus);

builder.Build().Run();


public static class Queues
{
    public const string ModelingDataAcceptedIntegrationEvent = "weatherapp-modeling-data-accepted";
    public const string ModelingDataRejectedIntegrationEvent = "weatherapp-modeling-data-rejected";
    public const string ModelUpdatedIntegrationEvent = "weatherapp-model-updated";
    public const string UserNotificationEvent = "weatherapp-user-notification";

#if DEBUG
    public static string Prefix = $"{Environment.MachineName}-";
#else
    public static string Prefix = string.Empty;
#endif

    public static string WithPrefix(this string queueName) => $"{Prefix}{queueName}";
}