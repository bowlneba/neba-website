using System.Diagnostics.CodeAnalysis;

using Neba.Api.BackgroundJobs;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.News;

using Newtonsoft.Json;

namespace Neba.Api.Tests.Features.News.AnnounceArticleToDiscord;

[UnitTest]
[Component("News")]
public sealed class AnnounceArticleToDiscordJobTests
{
    // Mirrors Hangfire's UseRecommendedSerializerSettings(), without mutating its process-wide global state.
    [SuppressMessage("Security", "CA2326:Do not use TypeNameHandling values other than None", Justification = "Mirrors Hangfire's own serializer settings; test data is trusted.")]
    [SuppressMessage("Security", "CA2327:Do not use insecure JsonSerializerSettings", Justification = "Mirrors Hangfire's own serializer settings; test data is trusted.")]
    private static readonly JsonSerializerSettings HangfireSettings = new()
    {
        TypeNameHandling = TypeNameHandling.Auto,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
    };

    [Fact(DisplayName = "JobName names the job and the article")]
    public void JobName_ShouldNameJobAndArticle()
    {
        // Arrange
        var job = AnnounceArticleToDiscordJobFactory.Create();

        // Act
        var jobName = job.JobName;

        // Assert
        jobName.ShouldBe($"AnnounceArticleToDiscordJob: article {job.ArticleId.Value}");
    }

    [Fact(DisplayName = "The job is marked [NoAutomaticRetry]")]
    public void Job_ShouldBeMarkedNoAutomaticRetry()
    {
        // Arrange & Act
        var attribute = Attribute.GetCustomAttribute(
            typeof(Neba.Api.Features.News.AnnounceArticleToDiscord.AnnounceArticleToDiscordJob),
            typeof(NoAutomaticRetryAttribute));

        // Assert
        attribute.ShouldNotBeNull();
    }

    [SuppressMessage("Security", "CA2326:Do not use TypeNameHandling values other than None", Justification = "Mirrors Hangfire's own serializer settings; test data is trusted.")]
    [SuppressMessage("Security", "CA2327:Do not use insecure JsonSerializerSettings", Justification = "Mirrors Hangfire's own serializer settings; test data is trusted.")]
    [Fact(DisplayName = "The job round-trips through Hangfire serialization")]
    public void Job_ShouldRoundTripThroughHangfireSerialization()
    {
        // Arrange
        var job = AnnounceArticleToDiscordJobFactory.Create(expectedPublishDateUtc: new DateTimeOffset(2026, 10, 27, 11, 0, 0, 123, TimeSpan.Zero));

        // Act
        var json = JsonConvert.SerializeObject(job, HangfireSettings);
        var result = JsonConvert.DeserializeObject<Neba.Api.Features.News.AnnounceArticleToDiscord.AnnounceArticleToDiscordJob>(json, HangfireSettings);

        // Assert
        result.ShouldBe(job);
    }
}