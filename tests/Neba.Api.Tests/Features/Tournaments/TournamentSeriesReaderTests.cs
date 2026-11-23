using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Storage;
using Neba.Api.Tests.Features.Series;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Series;
using Neba.TestFactory.Storage;

namespace Neba.Api.Tests.Features.Tournaments;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class TournamentSeriesReaderTests(AppDbContextFixture fixture)
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

    private TournamentSeriesReader CreateReader(IFileStorageService? storage = null)
        => new(_dbContext, storage ?? new Mock<IFileStorageService>(MockBehavior.Strict).Object);

    [Fact(DisplayName = "LoadAsync should return nothing, without a query, for an empty id list")]
    public async Task LoadAsync_ShouldReturnEmpty_ForEmptyIdList()
    {
        // Arrange & Act
        var result = await CreateReader().LoadAsync([], TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "LoadAsync should return the Series id, name, kind, count and squad for each Stage tournament, and omit tournaments in no Series")]
    public async Task LoadAsync_ShouldReturnSeriesDetailsPerStage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var series = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct, name: "VH");
        var loner = await SeriesSeed.AddTournamentAsync(_dbContext, season, "Loner", new DateOnly(2025, 11, 1), null, ct);
        var ids = series.Stages.Select(s => s.TournamentId).Append(loner.Id).ToList();

        // Act
        var result = await CreateReader().LoadAsync(ids, ct);

        // Assert
        result.ContainsKey(loner.Id).ShouldBeFalse();
        var byKind = series.Stages.ToDictionary(s => s.Kind.Name, s => result[s.TournamentId]);
        byKind.Values.ShouldAllBe(dto => dto.SeriesId == series.Id && dto.Name == "VH" && dto.LogoUrl == null);
        byKind["Qualifying"].AdvancingCount.ShouldBe(8);
        byKind["Qualifying"].SquadDate.ShouldBeNull();
        byKind["Survivor"].AdvancingCount.ShouldBe(20);
        byKind["Survivor"].SquadStartTime.ShouldBe(new TimeOnly(9, 0));
        byKind["Placement"].AdvancingCount.ShouldBeNull();
    }

    [Fact(DisplayName = "LoadAsync should return only the requested tournaments")]
    public async Task LoadAsync_ShouldReturnOnlyRequestedTournaments()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var series = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct);
        var wanted = series.Stages.First().TournamentId;

        // Act
        var result = await CreateReader().LoadAsync([wanted], ct);

        // Assert
        result.Keys.ShouldBe([wanted]);
    }

    [Fact(DisplayName = "LoadAsync should return the logo URL only for a Series that has a logo")]
    public async Task LoadAsync_ShouldReturnLogoUrl_OnlyWhenSeriesHasLogo()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var with = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct, name: "With", logo: StoredFileFactory.Create(container: "series", path: "logo.png"));
        var without = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 6, 1), ct, name: "Without");

        var uri = new Uri("https://storage.example.com/series/logo.png");
        var storage = new Mock<IFileStorageService>(MockBehavior.Strict);
        storage.Setup(s => s.GetBlobUri("series", "logo.png")).Returns(uri);

        // Act
        var result = await CreateReader(storage.Object).LoadAsync(
            [with.Stages.First().TournamentId, without.Stages.First().TournamentId], ct);

        // Assert
        result[with.Stages.First().TournamentId].LogoUrl.ShouldBe(uri);
        result[without.Stages.First().TournamentId].LogoUrl.ShouldBeNull();
    }

    [Fact(DisplayName = "LoadAsync should give the Finale fields to a Placement Stage only, and only when the Series has a Finale")]
    public async Task LoadAsync_ShouldGiveFinaleFields_ToPlacementStageOnly()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var finale = SeriesFinaleDefinitionFactory.Create(finalistCount: 4, date: new DateOnly(2025, 12, 1), startTime: new TimeOnly(14, 0));
        var withFinale = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct, name: "With", finale: finale);
        var noFinale = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 6, 1), ct, name: "No");

        var ids = withFinale.Stages.Concat(noFinale.Stages).Select(s => s.TournamentId).ToList();

        // Act
        var result = await CreateReader().LoadAsync(ids, ct);

        // Assert
        static TournamentId Of(Neba.Api.Features.Series.Domain.Series s, StageKind kind) => s.Stages.Single(x => x.Kind == kind).TournamentId;

        var placement = result[Of(withFinale, StageKind.Placement)];
        placement.FinaleCount.ShouldBe(4);
        placement.FinaleDate.ShouldBe(new DateOnly(2025, 12, 1));
        placement.FinaleStartTime.ShouldBe(new TimeOnly(14, 0));

        foreach (var kind in new[] { StageKind.Qualifying, StageKind.Survivor })
        {
            var dto = result[Of(withFinale, kind)];
            dto.FinaleCount.ShouldBeNull();
            dto.FinaleDate.ShouldBeNull();
            dto.FinaleStartTime.ShouldBeNull();
        }

        var noFinalePlacement = result[Of(noFinale, StageKind.Placement)];
        noFinalePlacement.FinaleCount.ShouldBeNull();
        noFinalePlacement.FinaleDate.ShouldBeNull();
    }

    [Fact(DisplayName = "LoadAsync should still return the Series for a cancelled tournament")]
    public async Task LoadAsync_ShouldReturnSeries_ForCancelledTournament()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var series = await SeriesSeed.AddSeriesAsync(_dbContext, season, new DateOnly(2025, 2, 1), ct);
        var tournamentId = series.Stages.First().TournamentId;

        var tournament = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(_dbContext.Tournaments, t => t.Id == tournamentId, ct);
        tournament.CancelTournament().IsError.ShouldBeFalse();
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await CreateReader().LoadAsync([tournamentId], ct);

        // Assert
        result[tournamentId].SeriesId.ShouldBe(series.Id);
    }
}