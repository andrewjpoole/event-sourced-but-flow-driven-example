using WeatherApp.SourceGenerators;

namespace WeatherApp.Infrastructure.Messaging;

[AutoGenerateImmutableDictionyFromConstants]
public static partial class EntityNames
{
    // Any constants defined here will be placed inside an ImmutableDictionary in a partial class by a source generator.
    public const string ModelingDataAcceptedIntegrationEvent = "weatherapp-modeling-data-accepted";
    public const string ModelingDataRejectedIntegrationEvent = "weatherapp-modeling-data-rejected";
    public const string ModelUpdatedIntegrationEvent = "weatherapp-model-updated";
    public const string UserNotificationEvent = "weatherapp-user-notification";

    public static string GetEntityNameFromTypeName(Type type)
    {
        if (ConstantsDictionary.TryGetValue(type.Name, out var entityName))        
            return entityName;        

        throw new ArgumentException($"Unknown type name: {type.Name}");
    }

    public static string GetTypeNameFromEntityName(string entityName)
    {
        var typeName = ConstantsDictionary.FirstOrDefault(kvp => kvp.Value == entityName).Key;

        if (typeName is not null)        
            return typeName;        

        throw new ArgumentException($"Unknown entity name: {entityName}");
    }    
}
