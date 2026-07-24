using Microsoft.Extensions.Time.Testing;

using Neba.Api.Database;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.ListTournamentsInSeason;
using Neba.Api.Storage;
using Neba.TestFactory;
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

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = new ListTournamentsInSeasonQueryHandler(_dbContext, fileStorageMock.Object, TimeProvider.System);

        // Act
        var result = await handler.HandleAsync(
            new ListTournamentsInSeasonQuery { SeasonId = season.Id, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true },
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

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = new ListTournamentsInSeasonQueryHandler(_dbContext, fileStorageMock.Object, TimeProvider.System);

        // Act
        var result = await handler.HandleAsync(
            new ListTournamentsInSeasonQuery { SeasonId = seasonA.Id, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true }, ct);

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

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = new ListTournamentsInSeasonQueryHandler(_dbContext, fileStorageMock.Object, TimeProvider.System);

        // Act
        var result = await handler.HandleAsync(
            new ListTournamentsInSeasonQuery { SeasonId = season.Id, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true }, ct);

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
        var handler = new ListTournamentsInSeasonQueryHandler(_dbContext, fileStorageMock.Object, TimeProvider.System);

        // Act
        var result = await handler.HandleAsync(
            new ListTournamentsInSeasonQuery { SeasonId = season.Id, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true }, ct);

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

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = new ListTournamentsInSeasonQueryHandler(_dbContext, fileStorageMock.Object, TimeProvider.System);

        // Act
        var result = await handler.HandleAsync(
            new ListTournamentsInSeasonQuery { SeasonId = season.Id, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true }, ct);

        // Assert
        result.ShouldHaveSingleItem();
        var pattern = result.Single().OilPatterns.Single();
        pattern.Name.ShouldBe("Chameleon");
        pattern.Length.ShouldBe(37);
        pattern.TournamentRounds.ShouldBe(["Qualifying", "Match Play"], ignoreOrder: true);
    }

    private async Task<Season> SeedSeasonWithTournamentsAsync(DateTimeOffset? revealedTournamentRevealDate, DateTimeOffset? gatedTournamentRevealDate, CancellationToken ct)
    {
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var revealedOilPattern = OilPatternFactory.Create(name: "Revealed Pattern", volume: 20m, leftRatio: 3m, rightRatio: 3m);
        var gatedOilPattern = OilPatternFactory.Create(name: "Gated Pattern", volume: 22.5m, leftRatio: 4.0m, rightRatio: 6.0m);
        await _dbContext.OilPatterns.AddRangeAsync([revealedOilPattern, gatedOilPattern], ct);

        var revealedTournament = TournamentFactory.Create(name: "Revealed Tournament", seasonId: season.Id, oilPatternRevealDateTime: revealedTournamentRevealDate);
        revealedTournament.AddOilPattern(revealedOilPattern.Id, TournamentRound.Qualifying);

        var gatedTournament = TournamentFactory.Create(name: "Gated Tournament", seasonId: season.Id, oilPatternRevealDateTime: gatedTournamentRevealDate);
        gatedTournament.AddOilPattern(gatedOilPattern.Id, TournamentRound.Qualifying);

        await _dbContext.Tournaments.AddRangeAsync([revealedTournament, gatedTournament], ct);
        await _dbContext.SaveChangesAsync(ct);

        return season;
    }

    [Fact(DisplayName = "HandleAsync returns reduced oil pattern info and no reveal date per-row when caller is anonymous")]
    public async Task HandleAsync_ShouldReturnReducedOilPatternInfoAndNoRevealDatePerRow_WhenCallerIsAnonymous()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var now = DateTimeOffset.UtcNow.TruncateToMicroseconds();
        var season = await SeedSeasonWithTournamentsAsync(now.AddDays(-1), now.AddDays(5), ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = new ListTournamentsInSeasonQueryHandler(_dbContext, fileStorageMock.Object, new FakeTimeProvider(now));

        // Act
        var result = await handler.HandleAsync(
            new ListTournamentsInSeasonQuery { SeasonId = season.Id, CallerIsAuthenticated = false, CallerHasTournamentManagementPermission = false }, ct);

        // Assert
        var revealed = result.Single(t => t.Name == "Revealed Tournament");
        revealed.OilPatterns.ShouldHaveSingleItem();
        revealed.OilPatternRevealDateTime.ShouldBeNull();

        var gated = result.Single(t => t.Name == "Gated Tournament");
        gated.OilPatterns.ShouldBeEmpty();
        gated.OilPatternRevealDateTime.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync returns reduced oil pattern info but the reveal date when caller is authenticated without the management permission")]
    public async Task HandleAsync_ShouldReturnReducedOilPatternInfoButRevealDate_WhenCallerIsAuthenticatedWithoutManagementPermission()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var now = DateTimeOffset.UtcNow.TruncateToMicroseconds();
        var gatedRevealAt = now.AddDays(5);
        var season = await SeedSeasonWithTournamentsAsync(now.AddDays(-1), gatedRevealAt, ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = new ListTournamentsInSeasonQueryHandler(_dbContext, fileStorageMock.Object, new FakeTimeProvider(now));

        // Act
        var result = await handler.HandleAsync(
            new ListTournamentsInSeasonQuery { SeasonId = season.Id, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = false }, ct);

        // Assert
        var gated = result.Single(t => t.Name == "Gated Tournament");
        gated.OilPatterns.ShouldBeEmpty();
        gated.OilPatternRevealDateTime.ShouldBe(gatedRevealAt);
    }

    [Fact(DisplayName = "HandleAsync returns full oil pattern info when caller has the management permission, even before the reveal date")]
    public async Task HandleAsync_ShouldReturnFullOilPatternInfo_WhenCallerHasManagementPermissionBeforeRevealDate()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var now = DateTimeOffset.UtcNow.TruncateToMicroseconds();
        var gatedRevealAt = now.AddDays(5);
        var season = await SeedSeasonWithTournamentsAsync(now.AddDays(-1), gatedRevealAt, ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = new ListTournamentsInSeasonQueryHandler(_dbContext, fileStorageMock.Object, new FakeTimeProvider(now));

        // Act
        var result = await handler.HandleAsync(
            new ListTournamentsInSeasonQuery { SeasonId = season.Id, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true }, ct);

        // Assert
        var gated = result.Single(t => t.Name == "Gated Tournament");
        gated.OilPatterns.ShouldHaveSingleItem();
        gated.OilPatternRevealDateTime.ShouldBe(gatedRevealAt);
    }

    [Fact(DisplayName = "HandleAsync returns full oil pattern info when the reveal date has passed, regardless of caller")]
    public async Task HandleAsync_ShouldReturnFullOilPatternInfo_WhenRevealDateHasPassed()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var now = DateTimeOffset.UtcNow.TruncateToMicroseconds();
        var season = await SeedSeasonWithTournamentsAsync(now.AddDays(-5), now.AddDays(-1), ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = new ListTournamentsInSeasonQueryHandler(_dbContext, fileStorageMock.Object, new FakeTimeProvider(now));

        // Act
        var result = await handler.HandleAsync(
            new ListTournamentsInSeasonQuery { SeasonId = season.Id, CallerIsAuthenticated = false, CallerHasTournamentManagementPermission = false }, ct);

        // Assert
        var gated = result.Single(t => t.Name == "Gated Tournament");
        gated.OilPatterns.ShouldHaveSingleItem();
        gated.OilPatternRevealDateTime.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync returns full oil pattern info when caller is anonymous and no reveal date is set")]
    public async Task HandleAsync_ShouldReturnFullOilPatternInfo_WhenCallerIsAnonymousAndNoRevealDateIsSet()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = await SeedSeasonWithTournamentsAsync(null, null, ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = new ListTournamentsInSeasonQueryHandler(_dbContext, fileStorageMock.Object, TimeProvider.System);

        // Act
        var result = await handler.HandleAsync(
            new ListTournamentsInSeasonQuery { SeasonId = season.Id, CallerIsAuthenticated = false, CallerHasTournamentManagementPermission = false }, ct);

        // Assert
        result.ShouldAllBe(t => t.OilPatterns.Count == 1);
        result.ShouldAllBe(t => t.OilPatternRevealDateTime == null);
    }
}