using ErrorOr;

using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments;
using Neba.Api.Features.Tournaments.CheckTeamEligibility;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.HallOfFame;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.CheckTeamEligibility;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class CheckTeamEligibilityQueryHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private Season _season = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();
        _season = SeasonFactory.Create(startDate: new DateOnly(2010, 1, 1), endDate: new DateOnly(2030, 12, 31));
        await _dbContext.Seasons.AddAsync(_season, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private CheckTeamEligibilityQueryHandler CreateHandler()
        => new(_dbContext, new DefendingTeamsReader(_dbContext), new BowlerTitlesReader(_dbContext));

    private async Task<Tournament> SeedTournamentAsync(
        TournamentType type,
        int year,
        CancellationToken ct,
        Tournament? previous = null,
        TournamentStatus? status = null,
        IReadOnlyCollection<Bowler>? winners = null)
    {
        var tournament = TournamentFactory.Create(
            tournamentType: type,
            startDate: new DateOnly(year, 3, 7),
            endDate: new DateOnly(year, 3, 8),
            seasonId: _season.Id,
            status: status,
            previousTournament: previous is null
                ? null
                : PreviousTournamentFactsFactory.Create(previous.Id, type, previous.EndDate));

        foreach (var winner in winners ?? [])
        {
            tournament.AddResult(winner.Id, place: 1, prizeMoney: 100m, points: 10);
        }

        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        return tournament;
    }

    private async Task<Bowler[]> SeedBowlersAsync(int count, CancellationToken ct, DateOnly? dateOfBirth = null)
    {
        var bowlers = Enumerable.Range(0, count)
            .Select(_ => BowlerFactory.Create(dateOfBirth: dateOfBirth))
            .ToArray();
        await _dbContext.Bowlers.AddRangeAsync(bowlers, ct);
        await _dbContext.SaveChangesAsync(ct);
        return bowlers;
    }

    private static CheckTeamEligibilityQuery Query(TournamentId tournamentId, IEnumerable<Bowler> bowlers, int unknown = 0)
        => new()
        {
            TournamentId = tournamentId,
            BowlerIds = [.. bowlers.Select(bowler => bowler.Id)],
            UnknownBowlerCount = unknown
        };

    [Fact(DisplayName = "HandleAsync returns Tournament.NotFound when the tournament does not exist")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTournamentDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowlers = await SeedBowlersAsync(2, ct);

        // Act
        var result = await CreateHandler().HandleAsync(Query(TournamentId.New(), bowlers), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
        result.FirstError.Code.ShouldBe("Tournament.NotFound");
    }

    [Fact(DisplayName = "HandleAsync returns a validation error when the tournament is not a team event")]
    public async Task HandleAsync_ShouldReturnNotATeamTournament_WhenTournamentIsSingles()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(TournamentType.Singles, 2026, ct);
        var bowlers = await SeedBowlersAsync(1, ct);

        // Act
        var result = await CreateHandler().HandleAsync(Query(tournament.Id, bowlers), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
        result.FirstError.Code.ShouldBe("TeamEligibility.NotATeamTournament");
    }

    [Theory(DisplayName = "HandleAsync returns a validation error when the bowler count does not match the team size")]
    [InlineData(1, 0, TestDisplayName = "One bowler, none unknown")]
    [InlineData(2, 1, TestDisplayName = "Two bowlers and one unknown")]
    [InlineData(0, 1, TestDisplayName = "One unknown only")]
    public async Task HandleAsync_ShouldReturnTeamSizeMismatch_WhenCountDoesNotMatch(int known, int unknown)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(TournamentType.Doubles, 2026, ct);
        var bowlers = await SeedBowlersAsync(known, ct);

        // Act
        var result = await CreateHandler().HandleAsync(Query(tournament.Id, bowlers, unknown), ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
        result.FirstError.Code.ShouldBe("TeamEligibility.TeamSizeMismatch");
    }

    [Fact(DisplayName = "HandleAsync returns Bowler.NotFound when a bowler ID does not exist")]
    public async Task HandleAsync_ShouldReturnBowlerNotFound_WhenBowlerDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(TournamentType.Doubles, 2026, ct);
        var bowlers = await SeedBowlersAsync(1, ct);
        var query = new CheckTeamEligibilityQuery
        {
            TournamentId = tournament.Id,
            BowlerIds = [bowlers[0].Id, BowlerId.New()],
            UnknownBowlerCount = 0
        };

        // Act
        var result = await CreateHandler().HandleAsync(query, ct);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
        result.FirstError.Code.ShouldBe("Bowler.NotFound");
    }

    [Fact(DisplayName = "HandleAsync returns DefendingChampions and the source tournament when the team equals the previous winners")]
    public async Task HandleAsync_ShouldReturnDefendingChampions_WhenTeamIsThePreviousWinners()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var winners = await SeedBowlersAsync(2, ct);
        var previous = await SeedTournamentAsync(TournamentType.Doubles, 2025, ct, status: TournamentStatus.Completed, winners: winners);
        var current = await SeedTournamentAsync(TournamentType.Doubles, 2026, ct, previous: previous);

        // Act
        var result = await CreateHandler().HandleAsync(Query(current.Id, winners), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(TeamEligibilityOutcome.DefendingChampions);
        result.Value.Reasons.ShouldBeEmpty();
        result.Value.DefendingTeam.ShouldNotBeNull().SourceTournamentId.ShouldBe(previous.Id);
    }

    [Fact(DisplayName = "HandleAsync skips the defending check when one bowler is not in the system")]
    public async Task HandleAsync_ShouldSkipDefendingCheck_WhenABowlerIsUnknown()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var winners = await SeedBowlersAsync(2, ct);
        var previous = await SeedTournamentAsync(TournamentType.Doubles, 2025, ct, status: TournamentStatus.Completed, winners: winners);
        var current = await SeedTournamentAsync(TournamentType.Doubles, 2026, ct, previous: previous);

        // Act
        var result = await CreateHandler().HandleAsync(Query(current.Id, [winners[0]], unknown: 1), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldNotBe(TeamEligibilityOutcome.DefendingChampions);
        result.Value.DefendingTeam.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync applies the normal rules when only one bowler is a defending champion")]
    public async Task HandleAsync_ShouldApplyNormalRules_WhenOnlyOneBowlerIsADefender()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var winners = await SeedBowlersAsync(2, ct);
        var other = await SeedBowlersAsync(1, ct);
        var previous = await SeedTournamentAsync(TournamentType.Doubles, 2025, ct, status: TournamentStatus.Completed, winners: winners);
        var current = await SeedTournamentAsync(TournamentType.Doubles, 2026, ct, previous: previous);

        // Act
        var result = await CreateHandler().HandleAsync(Query(current.Id, [winners[0], other[0]]), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(TeamEligibilityOutcome.Eligible);
    }

    [Fact(DisplayName = "HandleAsync returns Ineligible when a doubles team has two champions from recorded and historical titles")]
    public async Task HandleAsync_ShouldReturnIneligible_WhenDoublesTeamHasTwoChampions()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var team = await SeedBowlersAsync(2, ct);

        var recorded = await SeedTournamentAsync(TournamentType.Singles, 2024, ct, status: TournamentStatus.Completed, winners: [team[0]]);
        var historical = await SeedTournamentAsync(TournamentType.Singles, 2022, ct);
        await _dbContext.HistoricalTournamentChampions.AddAsync(HistoricalTournamentChampionFactory.Create(team[1], historical), ct);
        await _dbContext.SaveChangesAsync(ct);
        recorded.ShouldNotBeNull();

        var current = await SeedTournamentAsync(TournamentType.Doubles, 2026, ct);

        // Act
        var result = await CreateHandler().HandleAsync(Query(current.Id, team), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(TeamEligibilityOutcome.Ineligible);
        result.Value.Reasons.Select(reason => reason.Code).ShouldBe(["Champions.TooMany"]);
    }

    [Fact(DisplayName = "HandleAsync counts only Superior Performance Hall of Fame categories")]
    public async Task HandleAsync_ShouldCountOnlySuperiorPerformance_ForHallOfFameLimit()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var team = await SeedBowlersAsync(5, ct);
        await _dbContext.HallOfFameInductions.AddRangeAsync(
            [
                HallOfFameInductionFactory.Create(bowlerId: team[0].Id, categories: [HallOfFameCategory.SuperiorPerformance]),
                HallOfFameInductionFactory.Create(bowlerId: team[1].Id, categories: [HallOfFameCategory.MeritoriousService]),
                HallOfFameInductionFactory.Create(bowlerId: team[2].Id, categories: [HallOfFameCategory.SuperiorPerformance, HallOfFameCategory.MeritoriousService])
            ],
            ct);
        await _dbContext.SaveChangesAsync(ct);
        var current = await SeedTournamentAsync(TournamentType.Baker, 2026, ct);

        // Act
        var result = await CreateHandler().HandleAsync(Query(current.Id, team), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(TeamEligibilityOutcome.Ineligible);
        result.Value.Reasons.Select(reason => reason.Code).ShouldBe(["HallOfFame.TooMany"]);
    }

    [Fact(DisplayName = "HandleAsync returns Ineligible with Age.NotVerified when an over/under team has an unknown bowler")]
    public async Task HandleAsync_ShouldReturnAgeNotVerified_WhenOverUnderTeamHasUnknownBowler()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var known = await SeedBowlersAsync(1, ct, dateOfBirth: new DateOnly(1960, 1, 1));
        var current = await SeedTournamentAsync(TournamentType.OverUnderFiftyDoubles, 2026, ct);

        // Act
        var result = await CreateHandler().HandleAsync(Query(current.Id, known, unknown: 1), ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(TeamEligibilityOutcome.Ineligible);
        result.Value.Reasons.Select(reason => reason.Code).ShouldBe(["Age.NotVerified"]);
    }
}
