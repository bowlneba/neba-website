using Neba.Api.Contracts.Memberships;
using Neba.Api.Database;
using Neba.Api.Features.HallOfFame;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.HallOfFame;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.HallOfFame;

[IntegrationTest]
[Component("HallOfFame")]
[Collection<AppDbContextFixture>]
public sealed class HallOfFamePointsReaderTests(AppDbContextFixture fixture)
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

    private HallOfFamePointsReader CreateReader() => new(_dbContext);

    [Fact(DisplayName = "LoadTitleWinsAsync returns titles from historical champions and recorded results")]
    public async Task LoadTitleWinsAsync_ShouldReturnBothSources()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        var season = SeasonFactory.Create();
        await _dbContext.Bowlers.AddAsync(bowler, ct);
        await _dbContext.Seasons.AddAsync(season, ct);

        var historicalTournament = TournamentFactory.Create(
            tournamentType: TournamentType.Doubles,
            startDate: new DateOnly(2015, 5, 1),
            endDate: new DateOnly(2015, 5, 2),
            seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(historicalTournament, ct);
        await _dbContext.HistoricalTournamentChampions.AddAsync(
            HistoricalTournamentChampionFactory.Create(bowler, historicalTournament), ct);

        var recordedTournament = TournamentFactory.Create(
            tournamentType: TournamentType.Singles,
            startDate: new DateOnly(2025, 3, 1),
            endDate: new DateOnly(2025, 3, 2),
            seasonId: season.Id);
        recordedTournament.CompleteTournament(entryCount: 100);
        recordedTournament.AddResult(bowler.Id, place: 1, prizeMoney: 500m, points: 50);
        await _dbContext.Tournaments.AddAsync(recordedTournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var titles = await CreateReader().LoadTitleWinsAsync(null, ct);

        // Assert
        titles.Count.ShouldBe(2);
        titles.ShouldAllBe(title => title.BowlerId == bowler.Id && title.SeasonId == season.Id);
        titles.Select(title => title.TournamentType).ShouldBe([TournamentType.Doubles, TournamentType.Singles], ignoreOrder: true);
    }

    [Fact(DisplayName = "LoadTitleWinsAsync skips second place and tournaments that are not title eligible")]
    public async Task LoadTitleWinsAsync_ShouldSkipNonTitles()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        var other = BowlerFactory.Create();
        await _dbContext.Bowlers.AddRangeAsync([bowler, other], ct);
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var eligible = TournamentFactory.Create(seasonId: season.Id);
        eligible.CompleteTournament(entryCount: 100);
        eligible.AddResult(other.Id, place: 1, prizeMoney: 500m, points: 50);
        eligible.AddResult(bowler.Id, place: 2, prizeMoney: 250m, points: 30);

        var notEligible = TournamentFactory.Create(seasonId: season.Id);
        notEligible.AddResult(bowler.Id, place: 1, prizeMoney: 1000m, points: 100);
        await _dbContext.Tournaments.AddRangeAsync([eligible, notEligible], ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var titles = await CreateReader().LoadTitleWinsAsync(null, ct);

        // Assert
        titles.ShouldHaveSingleItem().BowlerId.ShouldBe(other.Id);
    }

    [Fact(DisplayName = "LoadTitleWinsAsync narrows to one bowler when given an id")]
    public async Task LoadTitleWinsAsync_ShouldNarrowToBowler()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        var other = BowlerFactory.Create();
        await _dbContext.Bowlers.AddRangeAsync([bowler, other], ct);
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var tournament = TournamentFactory.Create(seasonId: season.Id);
        tournament.CompleteTournament(entryCount: 100);
        tournament.AddResult(bowler.Id, place: 1, prizeMoney: 500m, points: 50);
        await _dbContext.Tournaments.AddAsync(tournament, ct);

        var historical = TournamentFactory.Create(seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(historical, ct);
        await _dbContext.HistoricalTournamentChampions.AddAsync(
            HistoricalTournamentChampionFactory.Create(other, historical), ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var titles = await CreateReader().LoadTitleWinsAsync(bowler.Id, ct);

        // Assert
        titles.ShouldHaveSingleItem().BowlerId.ShouldBe(bowler.Id);
    }

    [Fact(DisplayName = "LoadAwardWinsAsync returns each Bowler of the Year award with the season end date")]
    public async Task LoadAwardWinsAsync_ShouldReturnAwards()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        var other = BowlerFactory.Create();
        await _dbContext.Bowlers.AddRangeAsync([bowler, other], ct);
        var season = SeasonFactory.Create(startDate: new DateOnly(2024, 1, 1), endDate: new DateOnly(2024, 12, 31), complete: true);
        season.AddBowlerOfTheYearWinner(bowler.Id);
        season.AddRookieBowlerOfTheYearWinner(other.Id, isRookie: true);
        await _dbContext.Seasons.AddAsync(season, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var all = await CreateReader().LoadAwardWinsAsync(null, ct);
        var one = await CreateReader().LoadAwardWinsAsync(bowler.Id, ct);

        // Assert
        all.Count.ShouldBe(2);
        all.ShouldAllBe(award => award.SeasonId == season.Id && award.SeasonEndDate == new DateOnly(2024, 12, 31));
        var open = one.ShouldHaveSingleItem();
        open.Category.ShouldBe(BowlerOfTheYearCategory.Open);
        open.BowlerId.ShouldBe(bowler.Id);
    }

    [Fact(DisplayName = "LoadMembershipsAsync and LoadNamesAsync translate the strongly typed id Contains and return only the asked bowlers, with only counting membership types")]
    public async Task LoadMembershipsAndNames_ShouldReturnOnlyAskedBowlers()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create(name: NameFactory.Create("Jane", "Doe"));
        var other = BowlerFactory.Create(name: NameFactory.Create("John", "Roe"));
        bowler.AddLegacyMembership(1, MembershipType.New, 25m, new DateOnly(2020, 1, 1), new DateOnly(2020, 12, 31)).IsError.ShouldBeFalse();
        bowler.AddLegacyMembership(2, MembershipType.Guest, 0m, new DateOnly(2021, 5, 1), new DateOnly(2021, 5, 2)).IsError.ShouldBeFalse();
        other.AddLegacyMembership(3, MembershipType.Renewal, 25m, new DateOnly(2022, 1, 1), new DateOnly(2022, 12, 31)).IsError.ShouldBeFalse();
        await _dbContext.Bowlers.AddRangeAsync([bowler, other], ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var memberships = await CreateReader().LoadMembershipsAsync([bowler.Id], ct);
        var names = await CreateReader().LoadNamesAsync([bowler.Id], ct);

        // Assert
        var membership = memberships.ShouldHaveSingleItem();
        membership.BowlerId.ShouldBe(bowler.Id);
        membership.Type.ShouldBe(MembershipType.New);
        names.Count.ShouldBe(1);
        names[bowler.Id].ShouldBe("Jane Doe");
    }

    [Fact(DisplayName = "LoadInductedCategoriesAsync returns the categories per bowler")]
    public async Task LoadInductedCategoriesAsync_ShouldReturnCategoriesPerBowler()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        var other = BowlerFactory.Create();
        await _dbContext.Bowlers.AddRangeAsync([bowler, other], ct);
        await _dbContext.HallOfFameInductions.AddRangeAsync(
        [
            HallOfFameInductionFactory.Create(year: 2018, bowlerId: bowler.Id, categories: [HallOfFameCategory.MeritoriousService]),
            HallOfFameInductionFactory.Create(year: 2022, bowlerId: bowler.Id, categories: [HallOfFameCategory.FriendOfNeba]),
            HallOfFameInductionFactory.Create(year: 2020, bowlerId: other.Id, categories: [HallOfFameCategory.SuperiorPerformance])
        ], ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var inducted = await CreateReader().LoadInductedCategoriesAsync(null, ct);

        // Assert
        inducted.Count.ShouldBe(2);
        inducted[bowler.Id].ShouldBe([HallOfFameCategory.MeritoriousService, HallOfFameCategory.FriendOfNeba], ignoreOrder: true);
        inducted[other.Id].ShouldBe([HallOfFameCategory.SuperiorPerformance]);
    }

    [Fact(DisplayName = "LoadInductedCategoriesAsync returns only the asked bowler when one is given")]
    public async Task LoadInductedCategoriesAsync_ShouldReturnOnlyAskedBowler_WhenBowlerGiven()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        var other = BowlerFactory.Create();
        await _dbContext.Bowlers.AddRangeAsync([bowler, other], ct);
        await _dbContext.HallOfFameInductions.AddRangeAsync(
        [
            HallOfFameInductionFactory.Create(year: 2018, bowlerId: bowler.Id, categories: [HallOfFameCategory.MeritoriousService]),
            HallOfFameInductionFactory.Create(year: 2020, bowlerId: other.Id, categories: [HallOfFameCategory.SuperiorPerformance])
        ], ct);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var inducted = await CreateReader().LoadInductedCategoriesAsync(bowler.Id, ct);

        // Assert
        inducted.Count.ShouldBe(1);
        inducted[bowler.Id].ShouldBe([HallOfFameCategory.MeritoriousService]);
    }
}