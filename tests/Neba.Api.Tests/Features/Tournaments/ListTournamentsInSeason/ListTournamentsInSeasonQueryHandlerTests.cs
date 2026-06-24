using Neba.Api.Database;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.ListTournamentsInSeason;
using Neba.Api.Storage;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Storage;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.ListTournamentsInSeason;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class ListTournamentsInSeasonQueryHandlerTests(AppDbContextFixture fixture)
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

    [Fact(DisplayName = "HandleAsync returns empty collection when no tournaments exist for season")]
    public async Task HandleAsync_ShouldReturnEmpty_WhenNoTournamentsExistForSeason()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Loose);
        var handler = new ListTournamentsInSeasonQueryHandler(_dbContext, fileStorageMock.Object);

        // Act
        var result = await handler.HandleAsync(
            new ListTournamentsInSeasonQuery { SeasonId = season.Id },
            ct);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync returns only tournaments for the specified season")]
    public async Task HandleAsync_ShouldReturnOnlyTournamentsForSpecifiedSeason()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var seasonA = SeasonFactory.Create();
        var seasonB = SeasonFactory.Create();
        await _dbContext.Seasons.AddRangeAsync([seasonA, seasonB], ct);

        var tournamentA = TournamentFactory.Create(name: "Season A Tournament", seasonId: seasonA.Id);
        var tournamentB = TournamentFactory.Create(name: "Season B Tournament", seasonId: seasonB.Id);
        await _dbContext.Tournaments.AddRangeAsync([tournamentA, tournamentB], ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Loose);
        var handler = new ListTournamentsInSeasonQueryHandler(_dbContext, fileStorageMock.Object);

        // Act
        var result = await handler.HandleAsync(
            new ListTournamentsInSeasonQuery { SeasonId = seasonA.Id }, ct);

        // Assert
        result.ShouldHaveSingleItem();
        result.Single().Name.ShouldBe("Season A Tournament");
    }

    [Fact(DisplayName = "HandleAsync returns tournament with correct fields when tournament exists")]
    public async Task HandleAsync_ShouldReturnTournament_WithCorrectFields()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create(description: "2025 Season");
        await _dbContext.Seasons.AddAsync(season, ct);

        var tournament = TournamentFactory.Create(
            name: "NEBA Singles 2025",
            startDate: new DateOnly(2025, 10, 4),
            endDate: new DateOnly(2025, 10, 5),
            statsEligible: true,
            seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Loose);
        var handler = new ListTournamentsInSeasonQueryHandler(_dbContext, fileStorageMock.Object);

        // Act
        var result = await handler.HandleAsync(
            new ListTournamentsInSeasonQuery { SeasonId = season.Id }, ct);

        // Assert
        result.ShouldHaveSingleItem();
        var dto = result.Single();
        dto.Id.ShouldBe(tournament.Id);
        dto.Name.ShouldBe("NEBA Singles 2025");
        dto.Season.Description.ShouldBe("2025 Season");
        dto.StatsEligible.ShouldBeTrue();
        dto.Winners.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync sets LogoUrl when tournament has a logo")]
    public async Task HandleAsync_ShouldSetLogoUrl_WhenTournamentHasLogo()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var logo = StoredFileFactory.Create(container: "logos", path: "tournaments/neba-singles.jpg");
        var tournament = TournamentFactory.Create(seasonId: season.Id, logo: logo);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        var expectedUri = new Uri("https://storage.example.com/logos/tournaments/neba-singles.jpg");
        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        fileStorageMock
            .Setup(s => s.GetBlobUri("logos", "tournaments/neba-singles.jpg"))
            .Returns(expectedUri);
        var handler = new ListTournamentsInSeasonQueryHandler(_dbContext, fileStorageMock.Object);

        // Act
        var result = await handler.HandleAsync(
            new ListTournamentsInSeasonQuery { SeasonId = season.Id }, ct);

        // Assert
        result.ShouldHaveSingleItem();
        result.Single().LogoUrl.ShouldBe(expectedUri);
    }

    [Fact(DisplayName = "HandleAsync returns oil patterns with round names when tournament has oil patterns")]
    public async Task HandleAsync_ShouldReturnOilPatternsWithRoundNames_WhenTournamentHasOilPatterns()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var oilPattern = OilPatternFactory.Create(name: "Chameleon", length: 37);
        await _dbContext.OilPatterns.AddAsync(oilPattern, ct);

        var tournament = TournamentFactory.Create(seasonId: season.Id);
        tournament.AddOilPattern(oilPattern.Id, TournamentRound.Qualifying, TournamentRound.MatchPlay);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Loose);
        var handler = new ListTournamentsInSeasonQueryHandler(_dbContext, fileStorageMock.Object);

        // Act
        var result = await handler.HandleAsync(
            new ListTournamentsInSeasonQuery { SeasonId = season.Id }, ct);

        // Assert
        result.ShouldHaveSingleItem();
        var pattern = result.Single().OilPatterns.Single();
        pattern.Name.ShouldBe("Chameleon");
        pattern.Length.ShouldBe(37);
        pattern.TournamentRounds.ShouldBe(["Qualifying", "Match Play"], ignoreOrder: true);
    }
}