using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace WeatherApp.Tests.TUnit.AppHostFactories;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMockLogger(this IServiceCollection services, Mock<ILogger> mockLogger)
    {
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
        services.AddSingleton(loggerFactory.Object);

        return services;
    }
}
