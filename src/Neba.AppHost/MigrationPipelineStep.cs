using System.Diagnostics;

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

    // Matches the old shell loop: 60 polls, 10 seconds apart.
    private const int MaxPolls = 60;
    private static readonly TimeSpan s_pollInterval = TimeSpan.FromSeconds(10);

    internal static async Task RunAsync(PipelineStepContext context, IReadOnlyList<string> jobNames)
    {
        var configuration = context.Services.GetRequiredService<IConfiguration>();
        var logger = context.Logger;

        // Opt-in on purpose: any run that deploys api or web (a hotfix, a retry of one app)
        // passes through this step, and only the full CD run may touch the schema.
        if (!string.Equals(configuration["Migrations:Run"], "true", StringComparison.OrdinalIgnoreCase))
        {
            LogSkipped(logger);
            return;
        }

        var resourceGroup = configuration["Azure:ResourceGroup"];
        if (string.IsNullOrWhiteSpace(resourceGroup))
        {
            throw new InvalidOperationException("Azure__ResourceGroup is not set; cannot find the migration jobs.");
        }

        var subscriptionId = configuration["Azure:SubscriptionId"];

        foreach (var jobName in jobNames)
        {
            await RunJobAsync(jobName, resourceGroup, subscriptionId, logger, context.CancellationToken);
        }
    }

    private static async Task RunJobAsync(
        string jobName,
        string resourceGroup,
        string? subscriptionId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        LogStarting(logger, jobName);

        var executionName = (await RunAzAsync(
            ["containerapp", "job", "start", "--name", jobName, "--resource-group", resourceGroup, "--query", "name", "-o", "tsv"],
            subscriptionId,
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

            await Task.Delay(s_pollInterval, cancellationToken);

            status = (await RunAzAsync(
                [
                    "containerapp", "job", "execution", "show",
                    "--name", jobName,
                    "--resource-group", resourceGroup,
                    "--job-execution-name", executionName,
                    "--query", "properties.status", "-o", "tsv"
                ],
                subscriptionId,
                cancellationToken)).Trim();
        }

        if (!string.Equals(status, "Succeeded", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Job '{jobName}' execution '{executionName}' finished with status '{status}'.");
        }

        LogCompleted(logger, jobName);
    }

    private static async Task<string> RunAzAsync(
        IReadOnlyList<string> arguments,
        string? subscriptionId,
        CancellationToken cancellationToken)
    {
        // az is found through PATH on purpose: its install location differs between a developer
        // machine and the GitHub runner.
#pragma warning disable S4036
        var startInfo = new ProcessStartInfo("az")
#pragma warning restore S4036
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (!string.IsNullOrWhiteSpace(subscriptionId))
        {
            startInfo.ArgumentList.Add("--subscription");
            startInfo.ArgumentList.Add(subscriptionId);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start the Azure CLI (az).");

        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"az {string.Join(' ', arguments.Take(4))} failed with exit code {process.ExitCode}: {await error}");
        }

        return await output;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Skipping database migrations: set Migrations__Run=true to run them.")]
    private static partial void LogSkipped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting job execution: {JobName}")]
    private static partial void LogStarting(ILogger logger, string jobName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Waiting for job execution '{ExecutionName}' to complete...")]
    private static partial void LogWaiting(ILogger logger, string executionName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Job '{JobName}' completed successfully.")]
    private static partial void LogCompleted(ILogger logger, string jobName);
}
