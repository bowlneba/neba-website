using Aspire.Hosting.Pipelines;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Neba.AppHost;

/// <summary>
/// Runs the two migration Container App Jobs during <c>aspire deploy</c> and waits for them to
/// succeed. It replaces the "Run database migrations" shell step in cd.yml.
/// </summary>
internal static partial class MigrationPipelineStep
{
    internal const string StepName = "run-migrations";

    // The resource names of the two migration jobs. AppHost.cs uses the same constants for the
    // resources, so a rename cannot leave the pipeline step pointing at a job that does not exist.
    internal const string AppMigrationsJob = "api-migrations";
    internal const string SecurityMigrationsJob = "api-security-migrations";

    internal static IReadOnlyList<string> JobNames { get; } = [AppMigrationsJob, SecurityMigrationsJob];

    // Aspire names the deploy step of a job resource "provision-<resource>-containerapp".
    internal static IReadOnlyList<string> JobDeployStepNames { get; } =
        [.. JobNames.Select(name => $"provision-{name}-containerapp")];

    // Matches the old shell loop: 60 polls, 10 seconds apart.
    internal const int MaxPolls = 60;
    internal static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

    internal static Task RunAsync(PipelineStepContext context)
    {
        var configuration = context.Services.GetRequiredService<IConfiguration>();
        var (resourceGroup, subscriptionId) = ReadTarget(configuration);

        return RunAsync(
            new AzCli(subscriptionId),
            resourceGroup,
            JobNames,
            context.Logger,
            PollInterval,
            context.CancellationToken);
    }

    internal static async Task RunAsync(
        IAzCli az,
        string resourceGroup,
        IReadOnlyList<string> jobNames,
        ILogger logger,
        TimeSpan pollInterval,
        CancellationToken cancellationToken)
    {
        foreach (var jobName in jobNames)
        {
            await RunJobAsync(az, jobName, resourceGroup, logger, pollInterval, cancellationToken);
        }
    }

    // The subscription is required: without it az uses its default, which on a runner with more
    // than one subscription can differ from the one the deploy targets.
    internal static (string ResourceGroup, string SubscriptionId) ReadTarget(IConfiguration configuration)
    {
        var resourceGroup = configuration["Azure:ResourceGroup"];
        if (string.IsNullOrWhiteSpace(resourceGroup))
        {
            throw new InvalidOperationException("Azure__ResourceGroup is not set; cannot find the migration jobs.");
        }

        var subscriptionId = configuration["Azure:SubscriptionId"];
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            throw new InvalidOperationException("Azure__SubscriptionId is not set; cannot pick the subscription for the migration jobs.");
        }

        return (resourceGroup, subscriptionId);
    }

    private static async Task RunJobAsync(
        IAzCli az,
        string jobName,
        string resourceGroup,
        ILogger logger,
        TimeSpan pollInterval,
        CancellationToken cancellationToken)
    {
        LogStarting(logger, jobName);

        var executionName = (await az.RunAsync(
            ["containerapp", "job", "start", "--name", jobName, "--resource-group", resourceGroup, "--query", "name", "-o", "tsv"],
            cancellationToken)).Trim();

        if (executionName.Length == 0)
        {
            throw new InvalidOperationException($"Starting job '{jobName}' returned no execution name.");
        }

        LogWaiting(logger, executionName);

        var status = "Running";
        var polls = 0;
        while (status is "Running" or "Processing")
        {
            if (polls++ >= MaxPolls)
            {
                throw new TimeoutException($"Timed out waiting for job '{jobName}' execution '{executionName}' to complete.");
            }

            await Task.Delay(pollInterval, cancellationToken);

            status = (await az.RunAsync(
                [
                    "containerapp", "job", "execution", "show",
                    "--name", jobName,
                    "--resource-group", resourceGroup,
                    "--job-execution-name", executionName,
                    "--query", "properties.status", "-o", "tsv"
                ],
                cancellationToken)).Trim();
        }

        if (!string.Equals(status, "Succeeded", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Job '{jobName}' execution '{executionName}' finished with status '{status}'.");
        }

        LogCompleted(logger, jobName);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting job execution: {JobName}")]
    private static partial void LogStarting(ILogger logger, string jobName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Waiting for job execution '{ExecutionName}' to complete...")]
    private static partial void LogWaiting(ILogger logger, string executionName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Job '{JobName}' completed successfully.")]
    private static partial void LogCompleted(ILogger logger, string jobName);
}