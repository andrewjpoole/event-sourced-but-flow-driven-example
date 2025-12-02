using Projects;

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