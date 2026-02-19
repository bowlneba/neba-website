using Azure.Monitor.OpenTelemetry.AspNetCore;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Neba.ServiceDefaults.HealthChecks;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Neba.ServiceDefaults.Telemetry;

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S2325 // Private types or members should be static

/// <summary>
/// Extension methods for configuring telemetry in the application.
/// </summary>
public static class TelemetryConfiguration
{
    extension<TBuilder>(TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        /// <summary>
        /// Configures OpenTelemetry for the application, including logging, metrics, and tracing.
        /// </summary>
        /// <returns>The builder for chaining.</returns>
        internal TBuilder ConfigureOpenTelemetry()
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });

            builder.Services.AddOpenTelemetry()
                .WithMetrics(metrics => metrics
                    .AddMeter("Neba.*")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation())
                .WithTracing(tracing =>
                {
                    tracing
                        .AddSource(builder.Environment.ApplicationName)
                        .AddSource("Neba.*") // Custom application meters
                                             //.AddSource("Azure.Storage.Blobs") // Azure SDK traces // Uncomment to enable Azure Storage Blob SDK tracing
                        .AddAspNetCoreInstrumentation(tracing =>
                            // Exclude health check requests from tracing
                            tracing.Filter = context =>
                                !context.Request.Path.StartsWithSegments(HealthCheckConfiguration.HealthEndpointPath, StringComparison.OrdinalIgnoreCase)
                                && !context.Request.Path.StartsWithSegments(HealthCheckConfiguration.AlivenessEndpointPath, StringComparison.OrdinalIgnoreCase)
                        )
                        .AddHttpClientInstrumentation();

                    // Filter out Hangfire internal traces to reduce noise and costs in production monitoring
                    tracing.AddProcessor(new HangfireTraceFilterProcessor());
                });

            builder.AddOpenTelemetryExporters();

            return builder;
        }

        private TBuilder AddOpenTelemetryExporters()
        {
            var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

            if (useOtlpExporter)
            {
                builder.Services.AddOpenTelemetry().UseOtlpExporter();
            }

            // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
            if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
            {
                builder.Services.AddOpenTelemetry()
                   .UseAzureMonitor();
            }

            return builder;
        }
    }
}