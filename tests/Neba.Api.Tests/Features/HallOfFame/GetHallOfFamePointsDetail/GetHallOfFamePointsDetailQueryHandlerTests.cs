using Neba.Api.Contracts.Memberships;
using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.HallOfFame;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Features.HallOfFame.GetHallOfFamePointsDetail;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.HallOfFame;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.HallOfFame.GetHallOfFamePointsDetail;

[IntegrationTest]
[Component("HallOfFame")]
[Collection<AppDbContextFixture>]
public sealed class GetHallOfFamePointsDetailQueryHandlerTests(AppDbContextFixture fixture)
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

    private GetHallOfFamePointsDetailQueryHandler CreateHandler()
        => new(new HallOfFamePointsReader(_dbContext), new HallOfFamePointsCalculator());

    private async Task AddTitleAsync(Bowler bowler, TournamentType type, int year)
    {
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create(
            description: $"Season {year}",
            startDate: new DateOnly(year, 1, 1),
            endDate: new DateOnly(year, 12, 31));
        await _dbContext.Seasons.AddAsync(season, ct);

        var tournament = TournamentFactory.Create(
            tournamentType: type,
            startDate: new DateOnly(year, 5, 1),
            endDate: new DateOnly(year, 5, 2),
            seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.HistoricalTournamentChampions.AddAsync(
            HistoricalTournamentChampionFactory.Create(bowler, tournament), ct);
    }

    [Fact(DisplayName = "HandleAsync returns NotFound when the bowler does not exist")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenBowlerDoesNotExist()
    {
        // Arrange
        var query = new GetHallOfFamePointsDetailQuery { BowlerId = BowlerId.New() };

        // Act
        var result = await CreateHandler().HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Bowler.NotFound");
    }

    [Theory(DisplayName = "HandleAsync returns NotFound, not an error or exception, for a 26-character ID that is not a ULID")]
    [InlineData("!!JZ8K3M4N5P6Q7R8S9T0V1W2X", TestDisplayName = "invalid characters")]
    [InlineData("UUUUUUUUUUUUUUUUUUUUUUUUUU", TestDisplayName = "characters outside the ULID alphabet")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenIdIsNotAUlid(string notAUlid)
    {
        // Arrange
        // The validator checks length only, and Ulid parsing accepts any 26 characters, so such an ID reaches the handler.
        var query = new GetHallOfFamePointsDetailQuery { BowlerId = new BowlerId(notAUlid) };

        // Act
        var result = await CreateHandler().HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Bowler.NotFound");
    }

    [Fact(DisplayName = "HandleAsync returns the name, an empty breakdown and total 0 for a bowler with no points")]
    public async Task HandleAsync_ShouldReturnEmptyBreakdown_WhenBowlerHasNoPoints()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create(name: NameFactory.Create("Jane", "Doe"));
        await _dbContext.Bowlers.AddAsync(bowler, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await CreateHandler().HandleAsync(new GetHallOfFamePointsDetailQuery { BowlerId = bowler.Id }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.BowlerName.ShouldBe("Jane Doe");
        result.Value.TotalPoints.ShouldBe(0);
        result.Value.SeasonsWithPoints.ShouldBe(0);
        result.Value.Breakdown.ShouldBeEmpty();
        result.Value.MembershipYears.ShouldBeEmpty();
        result.Value.MembershipHistoryComplete.ShouldBeTrue();
        result.Value.InductedCategories.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync returns points and categories for an inducted bowler")]
    public async Task HandleAsync_ShouldReturnPointsAndCategories_ForInducteeBowler()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create(name: NameFactory.Create("Ray", "Dunleavy"));
        await _dbContext.Bowlers.AddAsync(bowler, ct);
        await AddTitleAsync(bowler, TournamentType.Singles, 2010);
        await _dbContext.HallOfFameInductions.AddAsync(
            HallOfFameInductionFactory.Create(
                bowlerId: bowler.Id,
                categories: [HallOfFameCategory.SuperiorPerformance, HallOfFameCategory.MeritoriousService]), ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await CreateHandler().HandleAsync(new GetHallOfFamePointsDetailQuery { BowlerId = bowler.Id }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.TotalPoints.ShouldBe(6);
        result.Value.InductedCategories.ShouldBe(["Superior Performance", "Meritorious Service"], ignoreOrder: true);
    }

    [Fact(DisplayName = "HandleAsync returns the breakdown and membership years with the type name, oldest first")]
    public async Task HandleAsync_ShouldReturnBreakdownAndMembershipYears_WithTypeName()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create(name: NameFactory.Create("Jane", "Doe"));
        bowler.AddLegacyMembership(1, MembershipType.Renewal, 25m, new DateOnly(2021, 1, 1), new DateOnly(2021, 12, 31)).IsError.ShouldBeFalse();
        bowler.AddLegacyMembership(2, MembershipType.TimeHonored, 0m, new DateOnly(2022, 1, 1), new DateOnly(2022, 12, 31)).IsError.ShouldBeFalse();
        bowler.AddLegacyMembership(3, MembershipType.Guest, 0m, new DateOnly(2023, 5, 1), new DateOnly(2023, 5, 2)).IsError.ShouldBeFalse();
        await _dbContext.Bowlers.AddAsync(bowler, ct);
        await AddTitleAsync(bowler, TournamentType.Doubles, 2020);
        await AddTitleAsync(bowler, TournamentType.Senior, 2018);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await CreateHandler().HandleAsync(new GetHallOfFamePointsDetailQuery { BowlerId = bowler.Id }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        var detail = result.Value;
        detail.TotalPoints.ShouldBe(8);
        detail.SeasonsWithPoints.ShouldBe(2);
        detail.Breakdown.Select(entry => (entry.Source, entry.Date.Year, entry.Points))
            .ShouldBe([("Senior Title", 2018, 2), ("Doubles Title", 2020, 6)]);
        detail.Breakdown.Select(entry => entry.RunningTotal).ShouldBe([2, 8]);
        detail.Breakdown.ShouldAllBe(entry => !entry.ReachedThreshold);
        detail.MembershipYears.Select(year => (year.Year, year.MembershipType))
            .ShouldBe([(2021, "Renewal"), (2022, "Time Honored Member")]);
        detail.MembershipHistoryComplete.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync flags the title that took an inducted bowler past the threshold and keeps the later ones unflagged")]
    public async Task HandleAsync_ShouldFlagThresholdEntry_ForInducteeWithPointsAfterwards()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);
        for (var year = 2005; year < 2011; year++)
        {
            await AddTitleAsync(bowler, TournamentType.Singles, year); // six 6-point titles: the sixth reaches 36
        }

        await AddTitleAsync(bowler, TournamentType.Doubles, 2015);     // earned after the threshold
        await _dbContext.HallOfFameInductions.AddAsync(
            HallOfFameInductionFactory.Create(bowlerId: bowler.Id, categories: [HallOfFameCategory.SuperiorPerformance]), ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await CreateHandler().HandleAsync(new GetHallOfFamePointsDetailQuery { BowlerId = bowler.Id }, ct);

        // Assert
        var breakdown = result.Value.Breakdown.ToList();
        breakdown.Count.ShouldBe(7);
        breakdown.Select(entry => entry.ReachedThreshold).ShouldBe([false, false, false, false, false, true, false]);
        breakdown[5].RunningTotal.ShouldBe(36);
        breakdown[6].RunningTotal.ShouldBe(42);
    }

    [Fact(DisplayName = "HandleAsync counts only the asked bowler's points")]
    public async Task HandleAsync_ShouldCountOnlyAskedBowler()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        var other = BowlerFactory.Create();
        await _dbContext.Bowlers.AddRangeAsync([bowler, other], ct);
        await AddTitleAsync(bowler, TournamentType.Singles, 2010);
        await AddTitleAsync(other, TournamentType.Singles, 2011);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var result = await CreateHandler().HandleAsync(new GetHallOfFamePointsDetailQuery { BowlerId = bowler.Id }, ct);

        // Assert
        result.Value.TotalPoints.ShouldBe(6);
        result.Value.Breakdown.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "HandleAsync records a detail cache miss after a successful load")]
    public async Task HandleAsync_ShouldRecordCacheMiss_WhenLoaded()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);
        await _dbContext.SaveChangesAsync(ct);
        using var recorder = new CacheMissRecorder();

        // Act
        await CreateHandler().HandleAsync(new GetHallOfFamePointsDetailQuery { BowlerId = bowler.Id }, ct);

        // Assert
        recorder.Queries.ShouldContain("detail");
    }

    [Fact(DisplayName = "HandleAsync records no cache miss when the bowler does not exist")]
    public async Task HandleAsync_ShouldNotRecordCacheMiss_WhenBowlerDoesNotExist()
    {
        // Arrange
        using var recorder = new CacheMissRecorder();
        var query = new GetHallOfFamePointsDetailQuery { BowlerId = BowlerId.New() };

        // Act
        await CreateHandler().HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        recorder.Queries.ShouldBeEmpty();
    }
}