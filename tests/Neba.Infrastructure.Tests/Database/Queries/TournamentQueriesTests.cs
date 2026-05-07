using Neba.Domain.Bowlers;
using Neba.Domain.BowlingCenters;
using Neba.Domain.Contact;
using Neba.Domain.Geography;
using Neba.Domain.Seasons;
using Neba.Domain.Sponsors;
using Neba.Domain.Tournaments;
using Neba.Infrastructure.Database;
using Neba.Infrastructure.Database.Configurations;
using Neba.Infrastructure.Database.Queries;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.BowlingCenters;
using Neba.TestFactory.Contact;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Sponsors;
using Neba.TestFactory.Storage;
using Neba.TestFactory.Tournaments;

namespace Neba.Infrastructure.Tests.Database.Queries;

[IntegrationTest]
[Component("Tournaments")]
[Collection<PostgreSqlFixture>]
public sealed class TournamentQueriesTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _postgres;
    private readonly AppDbContext _dbContext;
    private readonly TournamentQueries _sut;

    public TournamentQueriesTests(PostgreSqlFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _postgres = fixture;
        _dbContext = fixture.CreateDbContext();
        _sut = new TournamentQueries(_dbContext);
    }

    public async ValueTask InitializeAsync()
        => await _postgres.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await _postgres.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact(DisplayName = "GetTournamentCountForSeasonAsync returns count for tournaments in requested season only")]
    public async Task GetTournamentCountForSeasonAsync_ShouldReturnCountForRequestedSeasonOnly()
    {
        // Arrange
        var requestedSeason = SeasonFactory.Create(description: "Requested Season");
        var otherSeason = SeasonFactory.Create(description: "Other Season");

        var requestedSeasonTournaments = new[]
        {
            TournamentFactory.Create(name: "Tournament A", seasonId: requestedSeason.Id),
            TournamentFactory.Create(name: "Tournament B", seasonId: requestedSeason.Id),
            TournamentFactory.Create(name: "Tournament C", seasonId: requestedSeason.Id)
        };

        var otherSeasonTournament = TournamentFactory.Create(
            name: "Other Tournament",
            seasonId: otherSeason.Id);

        await _dbContext.Seasons.AddRangeAsync(
            [requestedSeason, otherSeason],
            TestContext.Current.CancellationToken);
        await _dbContext.Tournaments.AddRangeAsync(requestedSeasonTournaments, TestContext.Current.CancellationToken);
        await _dbContext.Tournaments.AddAsync(otherSeasonTournament, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetTournamentCountForSeasonAsync(requestedSeason.Id, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(3);
    }

    [Fact(DisplayName = "GetTournamentCountForSeasonAsync returns zero when requested season has no tournaments")]
    public async Task GetTournamentCountForSeasonAsync_ShouldReturnZero_WhenRequestedSeasonHasNoTournaments()
    {
        // Arrange
        var requestedSeason = SeasonFactory.Create(description: "Requested Season");
        var otherSeason = SeasonFactory.Create(description: "Other Season");

        var tournamentsInOtherSeason = new[]
        {
            TournamentFactory.Create(name: "Other Tournament A", seasonId: otherSeason.Id),
            TournamentFactory.Create(name: "Other Tournament B", seasonId: otherSeason.Id)
        };

        await _dbContext.Seasons.AddRangeAsync(
            [requestedSeason, otherSeason],
            TestContext.Current.CancellationToken);
        await _dbContext.Tournaments.AddRangeAsync(tournamentsInOtherSeason, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetTournamentCountForSeasonAsync(requestedSeason.Id, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(0);
    }

    [Fact(DisplayName = "GetTournamentEntryCountsAsync returns counts for requested tournaments")]
    public async Task GetTournamentEntryCountsAsync_ShouldReturnCounts_WhenRequestedTournamentsHaveHistoricalEntries()
    {
        // Arrange
        var season = SeasonFactory.Create();
        var tournamentA = TournamentFactory.Create(name: "Tournament A", seasonId: season.Id);
        var tournamentB = TournamentFactory.Create(name: "Tournament B", seasonId: season.Id);

        await _dbContext.Seasons.AddAsync(season, TestContext.Current.CancellationToken);
        await _dbContext.Tournaments.AddRangeAsync([tournamentA, tournamentB], TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _dbContext.Add(HistoricalTournamentEntryFactory.Create(tournament: tournamentA, entries: 125));
        _dbContext.Add(HistoricalTournamentEntryFactory.Create(tournament: tournamentB, entries: 231));
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetTournamentEntryCountsAsync(
            [tournamentA.Id, tournamentB.Id],
            TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);
        result[tournamentA.Id].ShouldBe(125);
        result[tournamentB.Id].ShouldBe(231);
    }

    [Fact(DisplayName = "GetTournamentEntryCountsAsync returns zero for requested tournaments without historical entries")]
    public async Task GetTournamentEntryCountsAsync_ShouldReturnZero_WhenRequestedTournamentHasNoHistoricalEntry()
    {
        // Arrange
        var season = SeasonFactory.Create();
        var tournamentWithEntries = TournamentFactory.Create(name: "With Entries", seasonId: season.Id);
        var tournamentWithoutEntries = TournamentFactory.Create(name: "Without Entries", seasonId: season.Id);

        await _dbContext.Seasons.AddAsync(season, TestContext.Current.CancellationToken);
        await _dbContext.Tournaments.AddRangeAsync(
            [tournamentWithEntries, tournamentWithoutEntries],
            TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _dbContext.Add(HistoricalTournamentEntryFactory.Create(tournament: tournamentWithEntries, entries: 180));
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetTournamentEntryCountsAsync(
            [tournamentWithEntries.Id, tournamentWithoutEntries.Id],
            TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);
        result[tournamentWithEntries.Id].ShouldBe(180);
        result[tournamentWithoutEntries.Id].ShouldBe(0);
    }

    [Fact(DisplayName = "GetTournamentEntryCountsAsync does not return tournaments that were not requested")]
    public async Task GetTournamentEntryCountsAsync_ShouldReturnOnlyRequestedTournaments_WhenOtherTournamentsHaveHistoricalEntries()
    {
        // Arrange
        var season = SeasonFactory.Create();
        var requestedTournament = TournamentFactory.Create(name: "Requested", seasonId: season.Id);
        var otherTournament = TournamentFactory.Create(name: "Other", seasonId: season.Id);

        await _dbContext.Seasons.AddAsync(season, TestContext.Current.CancellationToken);
        await _dbContext.Tournaments.AddRangeAsync([requestedTournament, otherTournament], TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _dbContext.Add(HistoricalTournamentEntryFactory.Create(tournament: requestedTournament, entries: 90));
        _dbContext.Add(HistoricalTournamentEntryFactory.Create(tournament: otherTournament, entries: 333));
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetTournamentEntryCountsAsync(
            [requestedTournament.Id],
            TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(1);
        result.ShouldContainKey(requestedTournament.Id);
        result[requestedTournament.Id].ShouldBe(90);
        result.ShouldNotContainKey(otherTournament.Id);
    }

    [Fact(DisplayName = "GetTournamentsInSeasonAsync returns only tournaments for the requested season")]
    public async Task GetTournamentsInSeasonAsync_ReturnsOnlyTournamentsForRequestedSeason()
    {
        // Arrange
        var requestedSeason = SeasonFactory.Create(description: "Requested Season");
        var otherSeason = SeasonFactory.Create(description: "Other Season");

        var requestedTournaments = new[]
        {
            TournamentFactory.Create(name: "Tournament A", seasonId: requestedSeason.Id),
            TournamentFactory.Create(name: "Tournament B", seasonId: requestedSeason.Id),
        };
        var otherTournament = TournamentFactory.Create(name: "Other Tournament", seasonId: otherSeason.Id);

        await _dbContext.Seasons.AddRangeAsync([requestedSeason, otherSeason], TestContext.Current.CancellationToken);
        await _dbContext.Tournaments.AddRangeAsync(requestedTournaments, TestContext.Current.CancellationToken);
        await _dbContext.Tournaments.AddAsync(otherTournament, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetTournamentsInSeasonAsync(requestedSeason.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldAllBe(t => t.Season.Id == requestedSeason.Id);
        result.ShouldNotContain(t => t.Name == "Other Tournament");
    }

    [Fact(DisplayName = "GetTournamentsInSeasonAsync returns empty collection when season has no tournaments")]
    public async Task GetTournamentsInSeasonAsync_ReturnsEmptyCollection_WhenSeasonHasNoTournaments()
    {
        // Arrange
        var requestedSeason = SeasonFactory.Create(description: "Empty Season");
        var otherSeason = SeasonFactory.Create(description: "Other Season");
        var otherTournament = TournamentFactory.Create(name: "Other Tournament", seasonId: otherSeason.Id);

        await _dbContext.Seasons.AddRangeAsync([requestedSeason, otherSeason], TestContext.Current.CancellationToken);
        await _dbContext.Tournaments.AddAsync(otherTournament, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetTournamentsInSeasonAsync(requestedSeason.Id, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "GetTournamentsInSeasonAsync maps all fields for a fully populated tournament")]
    public async Task GetTournamentsInSeasonAsync_MapsFullyPopulatedTournament()
    {
        // Arrange
        var season = SeasonFactory.Create(
            id: new SeasonId("01000000000000000000000002"),
            description: "2026 Season",
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 12, 31));

        var bowlingCenter = BowlingCenterFactory.Create(
            certificationNumber: CertificationNumberFactory.Create("12345"),
            name: "Bowlero Hartford",
            status: BowlingCenterStatus.Open,
            address: AddressFactory.CreateUsAddress(
                street: "100 Strike Way",
                city: "Hartford",
                state: UsState.Connecticut,
                postalCode: "06103",
                coordinates: AddressFactory.ValidCoordinates));

        var tournament = TournamentFactory.Create(
            id: new TournamentId("01000000000000000000000001"),
            name: "NEBA Singles 2026",
            tournamentType: TournamentType.Singles,
            startDate: new DateOnly(2026, 3, 15),
            endDate: new DateOnly(2026, 3, 16),
            seasonId: season.Id,
            bowlingCenterId: bowlingCenter.CertificationNumber,
            entryFee: 75m,
            externalRegistrationUrl: new Uri("https://bowlneba.com/register/singles"),
            logo: StoredFileFactory.Create(container: "tournament-logos", path: "singles-2026.png"),
            patternLengthCategory: PatternLengthCategory.MediumPattern,
            patternRatioCategory: PatternRatioCategory.Challenge);

        var sponsor1 = SponsorFactory.Create(
            id: new SponsorId("01000000000000000000000005"),
            name: "Acme Corp",
            slug: "acme-corp");

        var sponsor2 = SponsorFactory.Create(
            id: new SponsorId("01000000000000000000000006"),
            name: "Bowling Unlimited",
            slug: "bowling-unlimited");

        var bowler1 = BowlerFactory.Create(
            id: new BowlerId("01000000000000000000000003"),
            name: NameFactory.Create(firstName: "Alice", lastName: "Smith"));

        var bowler2 = BowlerFactory.Create(
            id: new BowlerId("01000000000000000000000004"),
            name: NameFactory.Create(firstName: "Bob", lastName: "Jones"));

        var oilPattern = OilPatternFactory.Create(
            id: new OilPatternId("01000000000000000000000007"),
            name: "Dragon",
            length: 40,
            volume: 25.0m,
            leftRatio: 3.0m,
            rightRatio: 3.0m,
            kegelId: null);

        tournament.AddOilPattern(oilPattern.Id, TournamentRound.Qualifying, TournamentRound.StepLadder)
            .IsError.ShouldBeFalse();

        await _dbContext.BowlingCenters.AddAsync(bowlingCenter, TestContext.Current.CancellationToken);
        await _dbContext.Seasons.AddAsync(season, TestContext.Current.CancellationToken);
        await _dbContext.Sponsors.AddRangeAsync([sponsor1, sponsor2], TestContext.Current.CancellationToken);
        await _dbContext.Bowlers.AddRangeAsync([bowler1, bowler2], TestContext.Current.CancellationToken);
        await _dbContext.OilPatterns.AddAsync(oilPattern, TestContext.Current.CancellationToken);
        await _dbContext.Tournaments.AddAsync(tournament, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _dbContext.Add(HistoricalTournamentChampionFactory.Create(bowler: bowler1, tournament: tournament));
        _dbContext.Add(HistoricalTournamentChampionFactory.Create(bowler: bowler2, tournament: tournament));
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var tournamentDbId = _dbContext.Entry(tournament)
            .Property<int>(ShadowIdConfiguration.DefaultPropertyName).CurrentValue;

        _dbContext.ChangeTracker.Clear();

        var ts1 = _dbContext.Set<TournamentSponsor>().Add(new TournamentSponsor
        {
            SponsorId = sponsor1.Id,
            TitleSponsor = false,
            SponsorshipAmount = 1000m,
        });
        ts1.Property<int>(TournamentConfiguration.ForeignKeyName).CurrentValue = tournamentDbId;

        var ts2 = _dbContext.Set<TournamentSponsor>().Add(new TournamentSponsor
        {
            SponsorId = sponsor2.Id,
            TitleSponsor = false,
            SponsorshipAmount = 2500m,
        });
        ts2.Property<int>(TournamentConfiguration.ForeignKeyName).CurrentValue = tournamentDbId;

        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetTournamentsInSeasonAsync(season.Id, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldHaveSingleItem();
        await Verify(result.Single());
    }

    [Fact(DisplayName = "GetTournamentsInSeasonAsync maps all nullable fields as null when not populated")]
    public async Task GetTournamentsInSeasonAsync_MapsNullOptionalFields()
    {
        // Arrange
        var season = SeasonFactory.Create(
            id: new SeasonId("01000000000000000000000012"),
            description: "2026 Season",
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 12, 31));

        var tournament = TournamentFactory.Create(
            id: new TournamentId("01000000000000000000000011"),
            name: "NEBA Singles 2026",
            tournamentType: TournamentType.Singles,
            startDate: new DateOnly(2026, 3, 15),
            endDate: new DateOnly(2026, 3, 16),
            seasonId: season.Id,
            bowlingCenterId: null,
            externalRegistrationUrl: null,
            logo: null,
            patternLengthCategory: null,
            patternRatioCategory: null);

        await _dbContext.Seasons.AddAsync(season, TestContext.Current.CancellationToken);
        await _dbContext.Tournaments.AddAsync(tournament, TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetTournamentsInSeasonAsync(season.Id, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldHaveSingleItem();
        await Verify(result.Single());
    }

    [Fact(DisplayName = "GetTournamentsInSeasonAsync does not mix winners across tournaments")]
    public async Task GetTournamentsInSeasonAsync_DoesNotMixWinners_AcrossTournaments()
    {
        // Arrange
        var season = SeasonFactory.Create();
        var tournamentA = TournamentFactory.Create(name: "Tournament A", seasonId: season.Id);
        var tournamentB = TournamentFactory.Create(name: "Tournament B", seasonId: season.Id);
        var bowlerA = BowlerFactory.Create(name: NameFactory.Create(firstName: "Alice", lastName: "Smith"));
        var bowlerB = BowlerFactory.Create(name: NameFactory.Create(firstName: "Bob", lastName: "Jones"));

        await _dbContext.Seasons.AddAsync(season, TestContext.Current.CancellationToken);
        await _dbContext.Tournaments.AddRangeAsync([tournamentA, tournamentB], TestContext.Current.CancellationToken);
        await _dbContext.Bowlers.AddRangeAsync([bowlerA, bowlerB], TestContext.Current.CancellationToken);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _dbContext.Add(HistoricalTournamentChampionFactory.Create(bowler: bowlerA, tournament: tournamentA));
        _dbContext.Add(HistoricalTournamentChampionFactory.Create(bowler: bowlerB, tournament: tournamentB));
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetTournamentsInSeasonAsync(season.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);
        var dtoA = result.Single(t => t.Name == "Tournament A");
        var dtoB = result.Single(t => t.Name == "Tournament B");

        dtoA.Winners.ShouldHaveSingleItem();
        dtoA.Winners.Single().FirstName.ShouldBe("Alice");

        dtoB.Winners.ShouldHaveSingleItem();
        dtoB.Winners.Single().FirstName.ShouldBe("Bob");
    }
}