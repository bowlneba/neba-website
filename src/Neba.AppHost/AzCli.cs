using System.Diagnostics;

namespace Neba.AppHost;

/// <summary>
/// Runs Azure CLI commands.
/// </summary>
internal interface IAzCli
{
    /// <summary>
    /// Runs <c>az</c> with the arguments and returns its standard output.
    /// </summary>
    Task<string> RunAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken);
}

/// <summary>
/// Runs the <c>az</c> executable against one subscription.
/// </summary>
internal sealed class AzCli(string subscriptionId, string executable = "az") : IAzCli
{
    public async Task<string> RunAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        // az is found through PATH on purpose: its install location differs between a developer
        // machine and the GitHub runner.
#pragma warning disable S4036
        var startInfo = new ProcessStartInfo(executable)
#pragma warning restore S4036
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.ArgumentList.Add("--subscription");
        startInfo.ArgumentList.Add(subscriptionId);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start the Azure CLI (az).");

        // The reads take no token: killing the process closes the pipes and ends them.
        var output = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var error = process.StandardError.ReadToEndAsync(CancellationToken.None);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // The process exited before the kill; the cancellation still stands.
            }

            throw;
        }

        return process.ExitCode == 0
            ? await output
            : throw new InvalidOperationException(
                $"az {string.Join(' ', arguments)} failed with exit code {process.ExitCode}: {await error}");
    }
}