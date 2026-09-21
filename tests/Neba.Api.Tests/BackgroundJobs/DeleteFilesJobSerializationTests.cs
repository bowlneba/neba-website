using System.Diagnostics.CodeAnalysis;

using Neba.Api.Features.News.DeleteArticle;
using Neba.Api.Features.Sponsors.EditSponsor;
using Neba.Api.Features.Tournaments.EditTournament;
using Neba.TestFactory.Attributes;

using Newtonsoft.Json;

using ArticleFileReference = Neba.Api.Features.News.DeleteArticle.StoredFileReference;
using SponsorFileReference = Neba.Api.Features.Sponsors.EditSponsor.StoredFileReference;

namespace Neba.Api.Tests.BackgroundJobs;

[UnitTest]
[Component("BackgroundJobs")]
public sealed class DeleteFilesJobSerializationTests
{
    [SuppressMessage("Security", "CA2326:Do not use TypeNameHandling values other than None", Justification = "Mirrors Hangfire's own serializer settings; test data is trusted.")]
    [SuppressMessage("Security", "CA2327:Do not use insecure JsonSerializerSettings", Justification = "Mirrors Hangfire's own serializer settings; test data is trusted.")]
    // Mirrors Hangfire's UseRecommendedSerializerSettings(), without mutating its process-wide global state.
    //
    // Hangfire.Common.SerializationHelper does not replace this. Its Internal/TypedInternal options use
    // different settings and round-trip these jobs even without the fix, so they cannot catch the bug.
    // Its User option (the one Hangfire uses for job arguments) does catch it, but only after
    // UseRecommendedSerializerSettings() and SetDataCompatibilityLevel(...) change global Hangfire state
    // that cannot be reset (the setter is internal) and that other tests in the run would see.
    private static readonly JsonSerializerSettings HangfireSettings = new()
    {
        TypeNameHandling = TypeNameHandling.Auto,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
    };

    [Fact(DisplayName = "DeleteTournamentFilesJob should round-trip through Hangfire serialization with a single-item collection expression")]
    public void DeleteTournamentFilesJob_ShouldRoundTrip_WhenFilesIsSingleItemCollectionExpression()
    {
        // Arrange
        var job = new DeleteTournamentFilesJob
        {
            Files = [new TournamentFileReference { Container = "tournaments", Path = "logo.png" }]
        };

        // Act
        var result = RoundTrip(job);

        // Assert
        result.Files.ShouldHaveSingleItem().Path.ShouldBe("logo.png");
    }

    [Fact(DisplayName = "DeleteArticleFilesJob should round-trip through Hangfire serialization with a single-item collection expression")]
    public void DeleteArticleFilesJob_ShouldRoundTrip_WhenFilesIsSingleItemCollectionExpression()
    {
        // Arrange
        var job = new DeleteArticleFilesJob
        {
            Files = [new ArticleFileReference { Container = "articles", Path = "header.png" }]
        };

        // Act
        var result = RoundTrip(job);

        // Assert
        result.Files.ShouldHaveSingleItem().Path.ShouldBe("header.png");
    }

    [Fact(DisplayName = "DeleteSponsorFilesJob should round-trip through Hangfire serialization with a single-item collection expression")]
    public void DeleteSponsorFilesJob_ShouldRoundTrip_WhenFilesIsSingleItemCollectionExpression()
    {
        // Arrange
        var job = new DeleteSponsorFilesJob
        {
            Files = [new SponsorFileReference { Container = "sponsors", Path = "logo.png" }]
        };

        // Act
        var result = RoundTrip(job);

        // Assert
        result.Files.ShouldHaveSingleItem().Path.ShouldBe("logo.png");
    }

    private static T RoundTrip<T>(T job)
    {
        var json = JsonConvert.SerializeObject(job, HangfireSettings);

        return JsonConvert.DeserializeObject<T>(json, HangfireSettings)!;
    }
}