using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.AppHost;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.AppHost;

[UnitTest]
[Component("AppHost.MigrationPipelineStep")]
public sealed class MigrationPipelineStepTests
{
    private const string ResourceGroup = "rg-test";
    private const string Execution = "api-migrations-abc123";

    private static readonly string[] s_jobs = ["api-migrations", "api-security-migrations"];

    private static bool IsStart(IReadOnlyList<string> args) => args.Contains("start");

    private static bool IsStartOf(IReadOnlyList<string> args, string job)
        => args.Contains("start") && args.Contains(job);

    private static bool IsShow(IReadOnlyList<string> args) => args.Contains("show");

    [Fact(DisplayName = "RunAsync should start each job and finish when the execution succeeds")]
    public async Task RunAsync_ShouldStartEachJob_WhenExecutionsSucceed()
    {
        // Arrange
        var az = new Mock<IAzCli>(MockBehavior.Strict);
        az.Setup(x => x.RunAsync(It.Is<IReadOnlyList<string>>(a => IsStart(a)), It.IsAny<CancellationToken>()))
            .ReturnsAsync($"{Execution}\n");
        az.SetupSequence(x => x.RunAsync(It.Is<IReadOnlyList<string>>(a => IsShow(a)), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Running\n")
            .ReturnsAsync("Succeeded\n")
            .ReturnsAsync("Processing\n")
            .ReturnsAsync("Succeeded\n");

        // Act
        await MigrationPipelineStep.RunAsync(az.Object, ResourceGroup, s_jobs, NullLogger.Instance, TimeSpan.Zero, TestContext.Current.CancellationToken);

        // Assert
        foreach (var job in s_jobs)
        {
            az.Verify(
                x => x.RunAsync(It.Is<IReadOnlyList<string>>(a => IsStartOf(a, job) && a.Contains(ResourceGroup)), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        az.Verify(x => x.RunAsync(It.Is<IReadOnlyList<string>>(a => IsShow(a)), It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [Fact(DisplayName = "RunAsync should throw naming the job and status when the execution does not succeed")]
    public async Task RunAsync_ShouldThrow_WhenExecutionFails()
    {
        // Arrange
        var az = new Mock<IAzCli>(MockBehavior.Strict);
        az.Setup(x => x.RunAsync(It.Is<IReadOnlyList<string>>(a => IsStart(a)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Execution);
        az.Setup(x => x.RunAsync(It.Is<IReadOnlyList<string>>(a => IsShow(a)), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Failed");

        // Act
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            MigrationPipelineStep.RunAsync(az.Object, ResourceGroup, s_jobs, NullLogger.Instance, TimeSpan.Zero, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldContain("api-migrations");
        exception.Message.ShouldContain(Execution);
        exception.Message.ShouldContain("Failed");
        az.Verify(
            x => x.RunAsync(It.Is<IReadOnlyList<string>>(a => IsStartOf(a, "api-security-migrations")), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "RunAsync should throw a TimeoutException when the execution never leaves Running")]
    public async Task RunAsync_ShouldThrowTimeout_WhenExecutionStaysRunning()
    {
        // Arrange
        var az = new Mock<IAzCli>(MockBehavior.Strict);
        az.Setup(x => x.RunAsync(It.Is<IReadOnlyList<string>>(a => IsStart(a)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Execution);
        az.Setup(x => x.RunAsync(It.Is<IReadOnlyList<string>>(a => IsShow(a)), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Running");

        // Act
        var exception = await Should.ThrowAsync<TimeoutException>(() =>
            MigrationPipelineStep.RunAsync(az.Object, ResourceGroup, s_jobs, NullLogger.Instance, TimeSpan.Zero, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldContain("api-migrations");
        az.Verify(
            x => x.RunAsync(It.Is<IReadOnlyList<string>>(a => IsShow(a)), It.IsAny<CancellationToken>()),
            Times.Exactly(MigrationPipelineStep.MaxPolls));
    }

    [Fact(DisplayName = "RunAsync should throw when starting a job returns no execution name")]
    public async Task RunAsync_ShouldThrow_WhenExecutionNameIsEmpty()
    {
        // Arrange
        var az = new Mock<IAzCli>(MockBehavior.Strict);
        az.Setup(x => x.RunAsync(It.Is<IReadOnlyList<string>>(a => IsStart(a)), It.IsAny<CancellationToken>()))
            .ReturnsAsync("  \n");

        // Act
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            MigrationPipelineStep.RunAsync(az.Object, ResourceGroup, s_jobs, NullLogger.Instance, TimeSpan.Zero, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldContain("returned no execution name");
        exception.Message.ShouldContain("api-migrations");
    }

    [Fact(DisplayName = "RunAsync should stop when the token is cancelled")]
    public async Task RunAsync_ShouldThrowCancelled_WhenTokenIsCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var az = new Mock<IAzCli>(MockBehavior.Strict);
        az.Setup(x => x.RunAsync(It.Is<IReadOnlyList<string>>(a => IsStart(a)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Execution);

        // Act
        var act = () => MigrationPipelineStep.RunAsync(az.Object, ResourceGroup, s_jobs, NullLogger.Instance, TimeSpan.FromSeconds(1), cts.Token);

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(act);
    }

    [Fact(DisplayName = "ReadTarget should return the resource group and subscription from configuration")]
    public void ReadTarget_ShouldReturnValues_WhenBothAreSet()
    {
        // Arrange
        var configuration = Configuration(("Azure:ResourceGroup", ResourceGroup), ("Azure:SubscriptionId", "sub-1"));

        // Act
        var (resourceGroup, subscriptionId) = MigrationPipelineStep.ReadTarget(configuration);

        // Assert
        resourceGroup.ShouldBe(ResourceGroup);
        subscriptionId.ShouldBe("sub-1");
    }

    [Theory(DisplayName = "ReadTarget should throw naming the missing setting")]
    [InlineData(null, "sub-1", "Azure__ResourceGroup")]
    [InlineData(" ", "sub-1", "Azure__ResourceGroup")]
    [InlineData(ResourceGroup, null, "Azure__SubscriptionId")]
    [InlineData(ResourceGroup, "", "Azure__SubscriptionId")]
    public void ReadTarget_ShouldThrow_WhenASettingIsMissing(string? resourceGroup, string? subscriptionId, string expected)
    {
        // Arrange
        var configuration = Configuration(("Azure:ResourceGroup", resourceGroup), ("Azure:SubscriptionId", subscriptionId));

        // Act
        var act = () => { _ = MigrationPipelineStep.ReadTarget(configuration); };

        // Assert
        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain(expected);
    }

    [Fact(DisplayName = "JobDeployStepNames should name the Aspire deploy step of each migration job")]
    public void JobDeployStepNames_ShouldDeriveFromJobNames()
    {
        // Arrange & Act
        var steps = MigrationPipelineStep.JobDeployStepNames;

        // Assert
        steps.ShouldBe(["provision-api-migrations-containerapp", "provision-api-security-migrations-containerapp"]);
    }

    private static IConfiguration Configuration(params (string Key, string? Value)[] values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();
}