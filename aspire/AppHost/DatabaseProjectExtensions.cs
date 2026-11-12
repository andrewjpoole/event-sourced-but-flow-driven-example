using Aspire.Hosting.ApplicationModel;

namespace AppHost;

public static class DatabaseProjectExtensions
{
    /// <summary>
    /// Adds the database project to the app host by pointing at its built .dacpac.
    /// </summary>
    /// <remarks>
    /// Used instead of <c>AddSqlProject&lt;T&gt;()</c> because the CommunityToolkit's in-process MSBuild evaluation of the
    /// sqlproj fails with "Microsoft.Common.props was not found" under the .NET 10 SDK. The dacpac is produced by the
    /// AppHost's ProjectReference to the sqlproj, so it is always built before the app host runs.
    /// </remarks>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The resource name.</param>
    /// <param name="databaseProjectDirectory">Directory of the sqlproj, relative to the AppHost directory.</param>
    /// <param name="dacpacFileName">The dacpac file name, e.g. WeatherAppDb.dacpac.</param>
    public static IResourceBuilder<SqlProjectResource> AddSqlProjectFromDacpac(
        this IDistributedApplicationBuilder builder,
        string name,
        string databaseProjectDirectory,
        string dacpacFileName)
    {
#if DEBUG
        const string buildConfiguration = "Debug";
#else
        const string buildConfiguration = "Release";
#endif
        var dacpacPath = Path.GetFullPath(
            Path.Combine(builder.AppHostDirectory, databaseProjectDirectory, "bin", buildConfiguration, dacpacFileName));

        return builder.AddSqlProject(name).WithDacpac(dacpacPath);
    }
}
