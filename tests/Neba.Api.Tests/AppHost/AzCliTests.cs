using Neba.AppHost;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.AppHost;

[UnitTest]
[Component("AppHost.AzCli")]
public sealed class AzCliTests : IDisposable
{
    private const string SubscriptionId = "sub-123";

    private readonly string _directory = Directory.CreateTempSubdirectory("azcli-tests").FullName;

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private string WriteScript(string body)
    {
        var path = Path.Combine(_directory, "fake-az.sh");
        File.WriteAllText(path, $"#!/bin/sh\n{body}\n");
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        return path;
    }

    [Fact(DisplayName = "RunAsync should pass the arguments and subscription and return standard output")]
    public async Task RunAsync_ShouldReturnOutput_WhenCommandSucceeds()
    {
        // Arrange
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Uses a shell script as the fake az.");
        var az = new AzCli(SubscriptionId, WriteScript("echo \"$@\""));

        // Act
        var output = await az.RunAsync(["group", "list"], TestContext.Current.CancellationToken);

        // Assert
        output.Trim().ShouldBe($"group list --subscription {SubscriptionId}");
    }

    [Fact(DisplayName = "RunAsync should throw with the exit code and error text when the command fails")]
    public async Task RunAsync_ShouldThrow_WhenCommandFails()
    {
        // Arrange
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Uses a shell script as the fake az.");
        var az = new AzCli(SubscriptionId, WriteScript("echo boom >&2\nexit 3"));

        // Act
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            az.RunAsync(["group", "list"], TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldContain("exit code 3");
        exception.Message.ShouldContain("boom");
        exception.Message.ShouldContain("az group list");
    }

    [Fact(DisplayName = "RunAsync should kill the process and rethrow when cancelled")]
    public async Task RunAsync_ShouldThrowOperationCanceled_WhenCancelled()
    {
        // Arrange
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Uses a shell script as the fake az.");
        var az = new AzCli(SubscriptionId, WriteScript("sleep 30"));
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(300));

        // Act
        var act = () => az.RunAsync(["group", "list"], cts.Token);

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(act);
    }

    [Fact(DisplayName = "RunAsync should throw when the executable cannot be started")]
    public async Task RunAsync_ShouldThrow_WhenExecutableIsMissing()
    {
        // Arrange
        var az = new AzCli(SubscriptionId, Path.Combine(_directory, "does-not-exist"));

        // Act
        var act = () => az.RunAsync(["group", "list"], TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<System.ComponentModel.Win32Exception>(act);
    }
}
