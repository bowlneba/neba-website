using System.Reflection;

using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Neba.ServiceDefaults.Telemetry;

/// <summary>
/// Extension methods that add application and process properties to every log record.
/// </summary>
public static class LogEnrichmentConfiguration
{
    private const string UnknownVersion = "unknown";

    extension<TBuilder>(TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        /// <summary>
        /// Adds the application enricher (application name, environment name, build version)
        /// and the process enricher (process id) to every log record.
        /// </summary>
        /// <returns>
        /// The builder for chaining.
        /// </returns>
        internal TBuilder ConfigureLogEnrichment()
        {
            var environmentName = builder.Environment.EnvironmentName;
            var applicationName = builder.Environment.ApplicationName;
            var buildVersion = GetBuildVersion();

            builder.Services.AddApplicationMetadata(metadata =>
            {
                metadata.ApplicationName = applicationName;
                metadata.EnvironmentName = environmentName;
                metadata.BuildVersion = buildVersion;
            });

            builder.Services.AddApplicationLogEnricher(options =>
            {
                options.ApplicationName = true;
                options.EnvironmentName = true;
                options.BuildVersion = true;
            });

            builder.Services.AddProcessLogEnricher();

            // Enrichers only run when the logging pipeline is wrapped by ExtendedLogger.
            builder.Logging.EnableEnrichment();

            return builder;
        }
    }

    // Informational version is "<version>+<commit sha>" when SourceLink is on; keep the
    // sha because it ties a log line to a build.
    private static string GetBuildVersion()
        => (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly())
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? UnknownVersion;
}
