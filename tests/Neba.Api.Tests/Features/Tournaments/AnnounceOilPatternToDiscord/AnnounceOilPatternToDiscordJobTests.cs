using System.Diagnostics.CodeAnalysis;

using Neba.Api.BackgroundJobs;
using Neba.Api.Features.Tournaments.AnnounceOilPatternToDiscord;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

using Newtonsoft.Json;

namespace Neba.Api.Tests.Features.Tournaments.AnnounceOilPatternToDiscord;

[UnitTest]
[Component("Tournaments")]
public sealed class AnnounceOilPatternToDiscordJobTests
{
    // Mirrors Hangfire's UseRecommendedSerializerSettings(), without mutating its process-wide global state.
    [SuppressMessage("Security", "CA2326:Do not use TypeNameHandling values other than None", Justification = "Mirrors Hangfire's own serializer settings; test data is trusted.")]
    [SuppressMessage("Security", "CA2327:Do not use insecure JsonSerializerSettings", Justification = "Mirrors Hangfire's own serializer settings; test data is trusted.")]
    private static readonly JsonSerializerSettings HangfireSettings = new()
    {
        TypeNameHandling = TypeNameHandling.Auto,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
    };

    [Fact(DisplayName = "JobName names the job and the tournament")]
    public void JobName_ShouldNameJobAndTournament()
    {
        // Arrange
        var job = AnnounceOilPatternToDiscordJobFactory.Create();

        // Act
        var jobName = job.JobName;

        // Assert
        jobName.ShouldBe($"AnnounceOilPatternToDiscordJob: tournament {job.TournamentId}");
    }

    [Fact(DisplayName = "The job is marked [NoAutomaticRetry]")]
    public void Job_ShouldBeMarkedNoAutomaticRetry()
    {
        // Arrange & Act
        var attribute = Attribute.GetCustomAttribute(typeof(AnnounceOilPatternToDiscordJob), typeof(NoAutomaticRetryAttribute));

        // Assert
        attribute.ShouldNotBeNull();
    }

    [SuppressMessage("Security", "CA2326:Do not use TypeNameHandling values other than None", Justification = "Mirrors Hangfire's own serializer settings; test data is trusted.")]
    [SuppressMessage("Security", "CA2327:Do not use insecure JsonSerializerSettings", Justification = "Mirrors Hangfire's own serializer settings; test data is trusted.")]
    [Fact(DisplayName = "The job round-trips through Hangfire serialization")]
    public void Job_ShouldRoundTripThroughHangfireSerialization()
    {
        // Arrange
        var job = AnnounceOilPatternToDiscordJobFactory.Create(
            expectedRevealDateTime: new DateTimeOffset(2026, 11, 1, 12, 0, 0, 123, TimeSpan.Zero));

        // Act
        var json = JsonConvert.SerializeObject(job, HangfireSettings);
        var result = JsonConvert.DeserializeObject<AnnounceOilPatternToDiscordJob>(json, HangfireSettings);

        // Assert
        result.ShouldBe(job);
    }
}
