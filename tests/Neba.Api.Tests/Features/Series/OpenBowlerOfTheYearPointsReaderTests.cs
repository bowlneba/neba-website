using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

namespace Neba.Api.Tests.Features.Series;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class OpenBowlerOfTheYearPointsReaderTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private static readonly DateOnly AsOf = new(2025, 12, 31);

    private readonly AppDbContext _dbContext = fixture.CreateDbContext();

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private Task<Tournament> AddAsync(
        Season season,
        string name,
        DateOnly start,
        TournamentStatus status,
        IReadOnlyCollection<TournamentResult>? results = null,
        DateOnly? end = null,
        TournamentType? type = null,
        bool statsEligible = true)
        => AdvancementReaderSeed.AddTournamentAsync(
            _dbContext, season, name, start, end, TestContext.Current.CancellationToken,
            status: status, results: results, type: type, statsEligible: statsEligible);

    [Fact(DisplayName = "ReadAsync should sum points per bowler over Completed and Truncated tournaments")]
    public async Task ReadAsync_ShouldSumPointsPerBowler()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var bowlers = await AdvancementReaderSeed.AddBowlersAsync(_dbContext, 2, ct);
        await AddAsync(season, "Completed", new DateOnly(2025, 3, 1), TournamentStatus.Completed,
            [AdvancementReaderSeed.Result(bowlers[0], 1, 100), AdvancementReaderSeed.Result(bowlers[1], 2, 80)]);
        await AddAsync(season, "Truncated", new DateOnly(2025, 4, 5), TournamentStatus.Truncated,
            [AdvancementReaderSeed.Result(bowlers[0], 1, 50)]);

        // Act
        var points = await OpenBowlerOfTheYearPointsReader.ReadAsync(_dbContext, season.Id, AsOf, ct);

        // Assert
        points.IsReady.ShouldBeTrue();
        points.Points.Select(row => (row.BowlerId, row.Points))
            .ShouldBe([(bowlers[0].Id, 150), (bowlers[1].Id, 80)]);
    }

    [Theory(DisplayName = "ReadAsync should include a tournament ending on the as-of date and exclude one ending a day later")]
    [InlineData(0, true)]
    [InlineData(-1, false)]
    public async Task ReadAsync_ShouldApplyEndDateBoundary(int asOfOffsetDays, bool expectedIncluded)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var bowlers = await AdvancementReaderSeed.AddBowlersAsync(_dbContext, 1, ct);
        var end = new DateOnly(2025, 6, 15);
        await AddAsync(season, "Ends on the line", end.AddDays(-1), TournamentStatus.Completed,
            [AdvancementReaderSeed.Result(bowlers[0], 1, 70)], end);

        // Act
        var points = await OpenBowlerOfTheYearPointsReader.ReadAsync(_dbContext, season.Id, end.AddDays(asOfOffsetDays), ct);

        // Assert
        points.IsReady.ShouldBeTrue();
        points.Points.Count.ShouldBe(expectedIncluded ? 1 : 0);
        if (expectedIncluded)
        {
            points.Points[0].Points.ShouldBe(70);
        }
    }

    [Fact(DisplayName = "ReadAsync should skip tournaments that are not stats eligible, in another season or cancelled, and bowlers with zero points")]
    public async Task ReadAsync_ShouldSkipExcludedTournamentsAndZeroPointBowlers()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var otherSeason = await SeriesSeed.AddSeasonAsync(_dbContext, 2024, ct);
        var bowlers = await AdvancementReaderSeed.AddBowlersAsync(_dbContext, 4, ct);
        await AddAsync(season, "Counts", new DateOnly(2025, 3, 1), TournamentStatus.Completed,
            [AdvancementReaderSeed.Result(bowlers[0], 1, 100), AdvancementReaderSeed.Result(bowlers[1], 2, 0)]);
        await AddAsync(season, "Not eligible", new DateOnly(2025, 4, 5), TournamentStatus.Completed,
            [AdvancementReaderSeed.Result(bowlers[2], 1, 500)], statsEligible: false);
        await AddAsync(otherSeason, "Last year", new DateOnly(2024, 4, 5), TournamentStatus.Completed,
            [AdvancementReaderSeed.Result(bowlers[3], 1, 900)]);
        await AddAsync(season, "Cancelled", new DateOnly(2025, 5, 3), TournamentStatus.Cancelled);

        // Act
        var points = await OpenBowlerOfTheYearPointsReader.ReadAsync(_dbContext, season.Id, AsOf, ct);

        // Assert
        points.IsReady.ShouldBeTrue();
        points.Points.Select(row => (row.BowlerId, row.Points)).ShouldBe([(bowlers[0].Id, 100)]);
    }

    [Fact(DisplayName = "ReadAsync should return no points and be ready when the season has no tournaments")]
    public async Task ReadAsync_ShouldBeReadyAndEmpty_WhenSeasonHasNoTournaments()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);

        // Act
        var points = await OpenBowlerOfTheYearPointsReader.ReadAsync(_dbContext, season.Id, AsOf, ct);

        // Assert
        points.IsReady.ShouldBeTrue();
        points.Points.ShouldBeEmpty();
    }

    [Fact(DisplayName = "ReadAsync should order bowlers by points, highest first")]
    public async Task ReadAsync_ShouldOrderByPointsDescending()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var bowlers = await AdvancementReaderSeed.AddBowlersAsync(_dbContext, 3, ct);
        await AddAsync(season, "One", new DateOnly(2025, 3, 1), TournamentStatus.Completed,
            [
                AdvancementReaderSeed.Result(bowlers[0], 3, 20),
                AdvancementReaderSeed.Result(bowlers[1], 1, 90),
                AdvancementReaderSeed.Result(bowlers[2], 2, 55),
            ]);

        // Act
        var points = await OpenBowlerOfTheYearPointsReader.ReadAsync(_dbContext, season.Id, AsOf, ct);

        // Assert
        points.Points.Select(row => row.BowlerId).ShouldBe([bowlers[1].Id, bowlers[2].Id, bowlers[0].Id]);
        points.Points.Select(row => row.Points).ShouldBe([90, 55, 20]);
    }

    [Theory(DisplayName = "ReadAsync should not be ready while a stats-eligible tournament ending on or before the date is unfinished or has no results")]
    [InlineData("Scheduled")]
    [InlineData("Completed")]
    [InlineData("Truncated")]
    public async Task ReadAsync_ShouldNotBeReady_WhenEligibleTournamentIsIncomplete(string statusName)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var status = TournamentStatus.FromName(statusName);
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        await AddAsync(season, "Incomplete", new DateOnly(2025, 6, 1), status);

        // Act
        var points = await OpenBowlerOfTheYearPointsReader.ReadAsync(_dbContext, season.Id, AsOf, ct);

        // Assert
        points.IsReady.ShouldBeFalse();
        points.Points.ShouldBeEmpty();
    }

    [Fact(DisplayName = "ReadAsync should not be ready when a Scheduled tournament ends exactly on the as-of date")]
    public async Task ReadAsync_ShouldNotBeReady_WhenScheduledTournamentEndsOnAsOfDate()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        await AddAsync(season, "Today", new DateOnly(2025, 6, 14), TournamentStatus.Scheduled, end: new DateOnly(2025, 6, 15));

        // Act
        var points = await OpenBowlerOfTheYearPointsReader.ReadAsync(_dbContext, season.Id, new DateOnly(2025, 6, 15), ct);

        // Assert
        points.IsReady.ShouldBeFalse();
    }

    [Fact(DisplayName = "ReadAsync should stay ready when the only unfinished tournaments end after the date, are not stats eligible, are cancelled or belong to another season")]
    public async Task ReadAsync_ShouldBeReady_WhenUnfinishedTournamentsDoNotApply()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var otherSeason = await SeriesSeed.AddSeasonAsync(_dbContext, 2024, ct);
        var asOf = new DateOnly(2025, 6, 15);
        await AddAsync(season, "Later", asOf.AddDays(1), TournamentStatus.Scheduled);
        await AddAsync(season, "Not eligible", new DateOnly(2025, 5, 1), TournamentStatus.Scheduled, statsEligible: false);
        await AddAsync(season, "Cancelled", new DateOnly(2025, 5, 8), TournamentStatus.Cancelled);
        await AddAsync(otherSeason, "Last year", new DateOnly(2024, 5, 1), TournamentStatus.Scheduled);

        // Act
        var points = await OpenBowlerOfTheYearPointsReader.ReadAsync(_dbContext, season.Id, asOf, ct);

        // Assert
        points.IsReady.ShouldBeTrue();
    }

    [Fact(DisplayName = "ReadAsync should drop the Tournament of Champions points of the single-day Non-Champions winner and keep the rest")]
    public async Task ReadAsync_ShouldApplyDoubleDipExclusion()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var bowlers = await AdvancementReaderSeed.AddBowlersAsync(_dbContext, 3, ct);
        var winner = bowlers[0];
        var runnerUp = bowlers[1];
        var other = bowlers[2];
        await AddAsync(season, "Non-Champions", new DateOnly(2025, 5, 3), TournamentStatus.Completed,
            [
                AdvancementReaderSeed.Result(winner, 1, 30),
                AdvancementReaderSeed.Result(runnerUp, 2, 25),
            ],
            type: TournamentType.NonChampions);
        await AddAsync(season, "Tournament of Champions", new DateOnly(2025, 10, 4), TournamentStatus.Completed,
            [
                AdvancementReaderSeed.Result(winner, 1, 20),
                AdvancementReaderSeed.Result(runnerUp, 2, 18),
                AdvancementReaderSeed.Result(other, 3, 15),
            ],
            type: TournamentType.TournamentOfChampions);

        // Act
        var points = await OpenBowlerOfTheYearPointsReader.ReadAsync(_dbContext, season.Id, AsOf, ct);

        // Assert
        points.Points.Select(row => (row.BowlerId, row.Points))
            .ShouldBe([(winner.Id, 30), (runnerUp.Id, 43), (other.Id, 15)], ignoreOrder: true);
    }

    [Theory(DisplayName = "ReadAsync should apply no double-dip exclusion unless there is exactly one single-day Non-Champions and one Tournament of Champions")]
    [InlineData(2, 1, false, 80)]
    [InlineData(1, 2, false, 70)]
    [InlineData(0, 1, false, 20)]
    [InlineData(1, 0, false, 30)]
    [InlineData(1, 1, true, 50)]
    public async Task ReadAsync_ShouldNotExclude_WhenDoubleDipDoesNotApply(
        int nonChampionsCount, int championsCount, bool nonChampionsMultiDay, int expectedPoints)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var bowlers = await AdvancementReaderSeed.AddBowlersAsync(_dbContext, 1, ct);
        var winner = bowlers[0];

        for (var index = 0; index < nonChampionsCount; index++)
        {
            var start = new DateOnly(2025, 3, 1).AddDays(index * 7);
            await AddAsync(season, $"Non-Champions {index}", start, TournamentStatus.Completed,
                [AdvancementReaderSeed.Result(winner, 1, 30)],
                end: nonChampionsMultiDay ? start.AddDays(1) : null,
                type: TournamentType.NonChampions);
        }

        for (var index = 0; index < championsCount; index++)
        {
            await AddAsync(season, $"Tournament of Champions {index}", new DateOnly(2025, 10, 4).AddDays(index * 7), TournamentStatus.Completed,
                [AdvancementReaderSeed.Result(winner, 1, 20)],
                type: TournamentType.TournamentOfChampions);
        }

        // Act
        var points = await OpenBowlerOfTheYearPointsReader.ReadAsync(_dbContext, season.Id, AsOf, ct);

        // Assert
        points.Points.Single().Points.ShouldBe(expectedPoints);
    }

    // Parity. LegacySeasonStatsCalculator.ComputeAwardPoints (not run here) gives a bowler's Bowler of the Year points as the
    // sum of main-cut Points over stats-eligible tournaments, minus the Tournament of Champions row of the Non-Champions
    // winner, plus a flat bonus per side-cut final. This fixture is a season whose totals were worked out by hand from that
    // rule, with no side cuts. A true parity run still needs the legacy tables for a finished season, run through the legacy
    // calculator and compared with these readers' totals; the side-cut bonus cannot match until it exists on website data.
    [Fact(DisplayName = "ReadAsync should match hand-computed legacy Bowler of the Year totals for a full season")]
    public async Task ReadAsync_ShouldMatchGoldenSeasonTotals()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeriesSeed.AddSeasonAsync(_dbContext, 2025, ct);
        var bowlers = await AdvancementReaderSeed.AddBowlersAsync(_dbContext, 5, ct);
        var (a, b, c, d, e) = (bowlers[0], bowlers[1], bowlers[2], bowlers[3], bowlers[4]);

        await AddAsync(season, "Singles", new DateOnly(2025, 3, 1), TournamentStatus.Completed,
            [
                AdvancementReaderSeed.Result(a, 1, 50), AdvancementReaderSeed.Result(b, 2, 40), AdvancementReaderSeed.Result(c, 3, 30),
                AdvancementReaderSeed.Result(d, 4, 20), AdvancementReaderSeed.Result(e, 5, 10),
            ],
            end: new DateOnly(2025, 3, 2));
        await AddAsync(season, "Doubles", new DateOnly(2025, 4, 5), TournamentStatus.Truncated,
            [
                AdvancementReaderSeed.Result(b, 1, 60), AdvancementReaderSeed.Result(a, 2, 45), AdvancementReaderSeed.Result(d, 3, 30),
                AdvancementReaderSeed.Result(c, 4, 20), AdvancementReaderSeed.Result(e, 5, 0),
            ],
            end: new DateOnly(2025, 4, 6),
            type: TournamentType.Doubles);
        await AddAsync(season, "Non-Champions", new DateOnly(2025, 5, 3), TournamentStatus.Completed,
            [
                AdvancementReaderSeed.Result(c, 1, 30), AdvancementReaderSeed.Result(a, 2, 25),
                AdvancementReaderSeed.Result(b, 3, 20), AdvancementReaderSeed.Result(d, 4, 15),
            ],
            type: TournamentType.NonChampions);
        await AddAsync(season, "Not eligible", new DateOnly(2025, 6, 7), TournamentStatus.Completed,
            [AdvancementReaderSeed.Result(e, 1, 500)],
            statsEligible: false);
        await AddAsync(season, "Cancelled", new DateOnly(2025, 8, 9), TournamentStatus.Cancelled);
        await AddAsync(season, "Tournament of Champions", new DateOnly(2025, 10, 4), TournamentStatus.Completed,
            [
                AdvancementReaderSeed.Result(c, 1, 20), AdvancementReaderSeed.Result(a, 2, 18),
                AdvancementReaderSeed.Result(b, 3, 16), AdvancementReaderSeed.Result(e, 4, 14),
            ],
            type: TournamentType.TournamentOfChampions);

        // Act
        var points = await OpenBowlerOfTheYearPointsReader.ReadAsync(_dbContext, season.Id, AsOf, ct);

        // Assert
        points.IsReady.ShouldBeTrue();
        points.Points.Select(row => (row.BowlerId, row.Points)).ShouldBe(
        [
            (a.Id, 138), // 50 + 45 + 25 + 18
            (b.Id, 136), // 40 + 60 + 20 + 16
            (c.Id, 80),  // 30 + 20 + 30; the Tournament of Champions 20 is the double dip
            (d.Id, 65),  // 20 + 30 + 15
            (e.Id, 24),  // 10 + 0 + 14; the 500 is in a tournament that is not stats eligible
        ]);
    }
}