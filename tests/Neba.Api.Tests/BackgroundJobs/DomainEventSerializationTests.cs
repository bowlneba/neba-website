using System.Diagnostics.CodeAnalysis;

using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Stats.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;

using Newtonsoft.Json;

namespace Neba.Api.Tests.BackgroundJobs;

[UnitTest]
[Component("BackgroundJobs")]
public sealed class DomainEventSerializationTests
{
    [SuppressMessage("Security", "CA2326:Do not use TypeNameHandling values other than None", Justification = "Mirrors Hangfire's own serializer settings; test data is trusted.")]
    [SuppressMessage("Security", "CA2327:Do not use insecure JsonSerializerSettings", Justification = "Mirrors Hangfire's own serializer settings; test data is trusted.")]
    // Mirrors Hangfire's UseRecommendedSerializerSettings(), without mutating its process-wide global state.
    private static readonly JsonSerializerSettings HangfireSettings = new()
    {
        TypeNameHandling = TypeNameHandling.Auto,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
    };

    [Fact(DisplayName = "TournamentResultsRecorded should round-trip through Hangfire serialization")]
    public void TournamentResultsRecorded_ShouldRoundTrip()
    {
        // Arrange
        var domainEvent = new TournamentResultsRecorded { TournamentId = TournamentId.New() };

        // Act
        var result = RoundTrip(domainEvent);

        // Assert
        result.ShouldBe(domainEvent);
    }

    [Fact(DisplayName = "TournamentSquadsChanged should round-trip through Hangfire serialization")]
    public void TournamentSquadsChanged_ShouldRoundTrip()
    {
        // Arrange
        var domainEvent = new TournamentSquadsChanged { TournamentId = TournamentId.New() };

        // Act
        var result = RoundTrip(domainEvent);

        // Assert
        result.ShouldBe(domainEvent);
    }

    [Fact(DisplayName = "SquadScoresReplaced should round-trip through Hangfire serialization")]
    public void SquadScoresReplaced_ShouldRoundTrip()
    {
        // Arrange
        var domainEvent = new SquadScoresReplaced { TournamentId = TournamentId.New(), SquadId = SquadId.New() };

        // Act
        var result = RoundTrip(domainEvent);

        // Assert
        result.ShouldBe(domainEvent);
        result.SquadId.ShouldNotBe(SquadId.Empty);
    }

    [Fact(DisplayName = "SeasonStatsGenerated should round-trip through Hangfire serialization")]
    public void SeasonStatsGenerated_ShouldRoundTrip()
    {
        // Arrange
        var domainEvent = new SeasonStatsGenerated { SeasonId = SeasonId.New() };

        // Act
        var result = RoundTrip(domainEvent);

        // Assert
        result.ShouldBe(domainEvent);
    }

    private static T RoundTrip<T>(T value)
    {
        var json = JsonConvert.SerializeObject(value, HangfireSettings);
        return JsonConvert.DeserializeObject<T>(json, HangfireSettings)!;
    }
}