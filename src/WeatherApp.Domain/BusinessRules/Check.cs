using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace WeatherApp.Domain.BusinessRules;

public static class Check
{
    public static void NotNull<T>(
        [NotNull] T? value,
        [CallerArgumentExpression(parameterName: nameof(value))] string? paramName = null)
    {
        if (value == null)
            throw new ArgumentNullException(paramName);
    }
}