// using Microsoft.Extensions.DependencyInjection;
// using Polly;
// using WeatherApp.Application.Services;
// using WeatherApp.Infrastructure.ModelingService;

// namespace WeatherApp.Infrastructure.ApiClients.NotificationService;

// public static class NotificationsServiceRegistrations
// {
//     public static IServiceCollection AddNotificationsService(this IServiceCollection services, NotificationsServiceOptions options)
//     {
//         var typeOfClientInterface = typeof(Notifications.NotificationService);
//         var nameOfClientInterface = typeOfClientInterface.FullName ?? typeOfClientInterface.Name;
//         services.AddHttpClient(nameOfClientInterface, client =>
//             {
//                 if (string.IsNullOrWhiteSpace(options.BaseUrl))
//                     throw new Exception($"{nameof(options.BaseUrl)} is required on {nameof(NotificationsServiceOptions)} section in config.");

//                 client.BaseAddress = new Uri(options.BaseUrl);
//                 client.DefaultRequestHeaders.Add(options.ApiManagerSubscriptionKeyHeader, options.SubscriptionKey);
//             })
//             .AddTransientHttpErrorPolicy(policy =>
//                 policy.WaitAndRetryAsync(options.MaxRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

//         services.AddSingleton<IWeatherModelingService, WeatherModelingService>();

//         return services;
//     }
// }