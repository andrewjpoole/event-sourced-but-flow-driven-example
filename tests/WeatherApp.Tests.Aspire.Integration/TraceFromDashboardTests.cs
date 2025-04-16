using Aspire.Dashboard;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace WeatherApp.Tests.Aspire.Integration;

// Code taken from Aspire Issue https://github.com/dotnet/aspire/issues/2897#issuecomment-2596407400 credit to Martynas Pocius
// it does sucessfully allow access to the private _app field of the DashboardWebApplication class
// and allows us to get the IServiceProvider from it.
// However the TelemetryRepository does not actually collect any data in the context of the tests, so it didn't help.
//
// An alternative approach would be to use the InMemoryExporter from OpenTelemetry
// https://github.com/open-telemetry/opentelemetry-dotnet/blob/0c26ce2b3b909223672fe40d22e1f15750dabe0a/src/OpenTelemetry.Exporter.InMemory/README.md
// this could be registered in the apps via ServiceDefaults but we would still need to aggregate the data in order to assert on it.
// 
// Sticking with the QueryableTraceCollector project resource as it's good enough to demonstrate the concept.

public static class ServiceCollectionExtensions
{
    public static TService GetService<TService>(this IServiceProvider provider, Type implementationType) where TService : class
    {
        if (provider is null)
            throw new ArgumentNullException(nameof(provider));

        if (implementationType is null)
            throw new ArgumentNullException(nameof(implementationType));

        var service = provider.GetServices<TService>()
                              .FirstOrDefault(s => s.GetType() == implementationType);

        if (service == null)
            throw new InvalidOperationException($"No service for type '{typeof(TService)}' and implementation '{implementationType}' has been registered.");

        return service;
    }
}
public static class DashboardWebApplicationExtensions
{
    public static IServiceProvider GetAppServices(this DashboardWebApplication dashboardWebApplication)
    {
        // Use reflection to get the private _app field
        FieldInfo appField = typeof(DashboardWebApplication).GetField("_app", BindingFlags.NonPublic | BindingFlags.Instance) ??
            throw new InvalidOperationException("The DashboardWebApplication class does not contain a private field named '_app'.");
        
        // Get the _app instance from the dashboardWebApplication instance
        WebApplication appInstance = appField.GetValue(dashboardWebApplication) as WebApplication ??
            throw new InvalidOperationException("Unable to retrieve the WebApplication instance from the DashboardWebApplication instance.");
        
        // Return the IServiceProvider from the WebApplication instance
        return appInstance.Services;
    }
}
public static class IDistributedApplicationTestingBuilderExtensions
{
    public static IServiceCollection AddDashboardWebApplication(this IServiceCollection services)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });

        var myServiceConfigurator = new ServiceConfigurator();
        var logger = loggerFactory.CreateLogger<DashboardWebApplication>();
        services.AddTransient(sp => logger);
        services.AddTransient<Action<IServiceCollection>>(sp => myServiceConfigurator.Configure);
        services.AddHostedService<DashboardWebApplication>();
        return services;
    }

    public static IServiceProvider GetDashboardWebApplication(this IServiceProvider services)
    {
        var dashboardWebApplication = (DashboardWebApplication)services.GetService<IHostedService>(typeof(DashboardWebApplication));
        var serviceProvider = dashboardWebApplication.GetAppServices();
        return serviceProvider;
    }
}

public class ServiceConfigurator
{
    public void Configure(IServiceCollection services)
    {
    }
}