using Neba.Api.Database;
using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class StageTournamentFactsReaderTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact(DisplayName = "ReadAsync should return facts for the requested tournaments and name the other Series that holds one")]
    public async Task ReadAsync_ShouldReturnFacts_AndOtherSeriesName()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var held = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct, name: "Holder");
        var free = await SeriesSeed.AddTournamentAsync(_dbContext, season, "Free", new DateOnly(2025, 11, 1), new DateOnly(2025, 11, 2), ct);
        var heldTournamentId = held.Stages.First().TournamentId;

        // Act
        var facts = await StageTournamentFactsReader.ReadAsync(_dbContext, [heldTournamentId, free.Id], null, ct);

        // Assert
        facts.Count.ShouldBe(2);
        facts[heldTournamentId].OtherSeriesName.ShouldBe("Holder");
        facts[free.Id].OtherSeriesName.ShouldBeNull();
        facts[free.Id].TournamentName.ShouldBe("Free");
        facts[free.Id].SeasonId.ShouldBe(season.Id);
        facts[free.Id].StartDate.ShouldBe(new DateOnly(2025, 11, 1));
        facts[free.Id].EndDate.ShouldBe(new DateOnly(2025, 11, 2));
    }

    [Fact(DisplayName = "ReadAsync should not report the excluded Series' own tournaments as held by another Series")]
    public async Task ReadAsync_ShouldIgnoreExcludedSeries()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var own = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct, name: "Own");
        var ids = own.Stages.Select(s => s.TournamentId).ToList();

        // Act
        var facts = await StageTournamentFactsReader.ReadAsync(_dbContext, ids, own.Id, ct);

        // Assert
        facts.Values.ShouldAllBe(f => f.OtherSeriesName == null);
    }

    [Fact(DisplayName = "ReadAsync should omit tournaments that do not exist and return nothing for an empty list")]
    public async Task ReadAsync_ShouldOmitMissingTournaments()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        var missing = await StageTournamentFactsReader.ReadAsync(_dbContext, [TournamentId.New()], null, ct);
        var empty = await StageTournamentFactsReader.ReadAsync(_dbContext, [], null, ct);

        // Assert
        missing.ShouldBeEmpty();
        empty.ShouldBeEmpty();
    }
}