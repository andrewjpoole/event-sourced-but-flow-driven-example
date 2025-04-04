namespace WeatherApp.Tests.Framework.ServiceBus;

public static class DebugCompilationSymbol
{
#if DEBUG
    private const bool IsPresent = true;
#else
    private const bool IsPresent = false;
#endif
}