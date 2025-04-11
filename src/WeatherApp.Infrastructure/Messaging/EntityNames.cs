namespace WeatherApp.Infrastructure.Messaging;

public static partial class EntityNames
{
    // Any constants defined here will be placed inside an ImmutableDictionary in a partial class by a source generator.
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

    public static string GetEntityNameFromTypeName(Type type)
    {
        if (EntityNameDictionary.TryGetValue(type.Name, out var entityName))        
            return entityName;        

        throw new ArgumentException($"Unknown type name: {type.Name}");
    }

    public static string GetTypeNameFromEntityName(string entityName)
    {
        var typeName = EntityNameDictionary.FirstOrDefault(kvp => kvp.Value == entityName).Key;

        if (typeName is not null)        
            return typeName;        

        throw new ArgumentException($"Unknown entity name: {entityName}");
    }    
}