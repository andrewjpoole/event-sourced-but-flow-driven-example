# ServiceCollectionExtensions template

Purpose: provide a small helper for registering a mocked logger factory into the test host's DI container. Adapt only if the target app resolves generic or typed loggers differently.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace {Namespace}.Tests.TUnit.AppHostFactories;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMockLogger(this IServiceCollection services, Mock<ILogger> mockLogger)
    {
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(mockLogger.Object);

        services.AddSingleton(loggerFactory.Object);
        return services;
    }
}
```
