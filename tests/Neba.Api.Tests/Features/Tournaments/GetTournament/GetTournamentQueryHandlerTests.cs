using Microsoft.Extensions.Time.Testing;

using Neba.Api.Database;
using Neba.Api.Features.News.Domain;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.GetTournament;
using Neba.Api.Storage;
using Neba.TestFactory;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.News;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Sponsors;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.GetTournament;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class GetTournamentQueryHandlerTests(AppDbContextFixture fixture)
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

    [Fact(DisplayName = "HandleAsync returns TournamentNotFound when tournament does not exist")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTournamentDoesNotExist()
    {
        // Arrange
        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = new GetTournamentQueryHandler(_dbContext, fileStorageMock.Object, TimeProvider.System);

        // Act
        var result = await handler.HandleAsync(
            new GetTournamentQuery { Id = TournamentId.New(), CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true },
            TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.NotFound");
    }

    [Fact(DisplayName = "HandleAsync returns tournament detail with correct fields when tournament exists")]
    public async Task HandleAsync_ShouldReturnTournamentDetail_WithCorrectFields_WhenTournamentExists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create(description: "2025 Season");
        await _dbContext.Seasons.AddAsync(season, ct);

        var tournament = TournamentFactory.Create(
            name: "NEBA Singles 2025",
            tournamentType: TournamentType.Singles,
            startDate: new DateOnly(2025, 10, 4),
            endDate: new DateOnly(2025, 10, 5),
            statsEligible: true,
            seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = new GetTournamentQueryHandler(_dbContext, fileStorageMock.Object, TimeProvider.System);

        // Act
        var result = await handler.HandleAsync(
            new GetTournamentQuery { Id = tournament.Id, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Id.ShouldBe(tournament.Id);
        result.Value.Name.ShouldBe("NEBA Singles 2025");
        result.Value.Season.ShouldBe("2025 Season");
        result.Value.StatsEligible.ShouldBeTrue();
        result.Value.Winners.ShouldBeEmpty();
        result.Value.Results.ShouldBeEmpty();
        result.Value.EntryCount.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync sets LogoUrl when tournament has a logo")]
    public async Task HandleAsync_ShouldSetLogoUrl_WhenTournamentHasLogo()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var logo = Neba.TestFactory.Storage.StoredFileFactory.Create(
            container: "logos",
            path: "tournaments/neba-singles.jpg");
        var tournament = TournamentFactory.Create(seasonId: season.Id, logo: logo);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        var expectedUri = new Uri("https://storage.example.com/logos/tournaments/neba-singles.jpg");
        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        fileStorageMock
            .Setup(s => s.GetBlobUri("logos", "tournaments/neba-singles.jpg"))
            .Returns(expectedUri);
        var handler = new GetTournamentQueryHandler(_dbContext, fileStorageMock.Object, TimeProvider.System);

        // Act
        var result = await handler.HandleAsync(
            new GetTournamentQuery { Id = tournament.Id, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.LogoUrl.ShouldBe(expectedUri);
    }

    [Fact(DisplayName = "HandleAsync includes raw logo fields when caller has the tournament management permission")]
    public async Task HandleAsync_ShouldIncludeRawLogoFields_WhenCallerHasManagementPermission()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var logo = Neba.TestFactory.Storage.StoredFileFactory.Create(
            container: "logos", path: "tournaments/neba-singles.jpg", contentType: "image/jpeg", sizeInBytes: 4096);
        var tournament = TournamentFactory.Create(seasonId: season.Id, logo: logo);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        fileStorageMock.Setup(s => s.GetBlobUri("logos", "tournaments/neba-singles.jpg")).Returns(new Uri("https://storage.example.com/logo.jpg"));
        var handler = new GetTournamentQueryHandler(_dbContext, fileStorageMock.Object, TimeProvider.System);

        // Act
        var result = await handler.HandleAsync(
            new GetTournamentQuery { Id = tournament.Id, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.LogoContainer.ShouldBe("logos");
        result.Value.LogoPath.ShouldBe("tournaments/neba-singles.jpg");
        result.Value.LogoContentType.ShouldBe("image/jpeg");
        result.Value.LogoSizeInBytes.ShouldBe(4096);
    }

    [Fact(DisplayName = "HandleAsync omits raw logo fields when caller lacks the tournament management permission")]
    public async Task HandleAsync_ShouldOmitRawLogoFields_WhenCallerLacksManagementPermission()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var logo = Neba.TestFactory.Storage.StoredFileFactory.Create(
            container: "logos", path: "tournaments/neba-singles.jpg");
        var tournament = TournamentFactory.Create(seasonId: season.Id, logo: logo);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        fileStorageMock.Setup(s => s.GetBlobUri("logos", "tournaments/neba-singles.jpg")).Returns(new Uri("https://storage.example.com/logo.jpg"));
        var handler = new GetTournamentQueryHandler(_dbContext, fileStorageMock.Object, TimeProvider.System);

        // Act
        var result = await handler.HandleAsync(
            new GetTournamentQuery { Id = tournament.Id, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = false }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.LogoContainer.ShouldBeNull();
        result.Value.LogoPath.ShouldBeNull();
        result.Value.LogoContentType.ShouldBeNull();
        result.Value.LogoSizeInBytes.ShouldBeNull();
        result.Value.LogoUrl.ShouldNotBeNull();
    }

    [Fact(DisplayName = "HandleAsync includes the bowling center's certification number when caller has the tournament management permission")]
    public async Task HandleAsync_ShouldIncludeBowlingCenterCertificationNumber_WhenCallerHasManagementPermission()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var bowlingCenter = Neba.TestFactory.BowlingCenters.BowlingCenterFactory.Create();
        await _dbContext.BowlingCenters.AddAsync(bowlingCenter, ct);

        var tournament = TournamentFactory.Create(bowlingCenterId: bowlingCenter.CertificationNumber, seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = new GetTournamentQueryHandler(_dbContext, fileStorageMock.Object, TimeProvider.System);

        // Act
        var result = await handler.HandleAsync(
            new GetTournamentQuery { Id = tournament.Id, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.BowlingCenter.ShouldNotBeNull();
        result.Value.BowlingCenter.CertificationNumber.ShouldBe(bowlingCenter.CertificationNumber.Value);
    }

    [Fact(DisplayName = "HandleAsync omits the bowling center's certification number when caller lacks the tournament management permission")]
    public async Task HandleAsync_ShouldOmitBowlingCenterCertificationNumber_WhenCallerLacksManagementPermission()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var bowlingCenter = Neba.TestFactory.BowlingCenters.BowlingCenterFactory.Create();
        await _dbContext.BowlingCenters.AddAsync(bowlingCenter, ct);

        var tournament = TournamentFactory.Create(bowlingCenterId: bowlingCenter.CertificationNumber, seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = new GetTournamentQueryHandler(_dbContext, fileStorageMock.Object, TimeProvider.System);

        // Act
        var result = await handler.HandleAsync(
            new GetTournamentQuery { Id = tournament.Id, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = false }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.BowlingCenter.ShouldNotBeNull();
        result.Value.BowlingCenter.CertificationNumber.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync returns published articles with title and slug when tournament has articles")]
    public async Task HandleAsync_ShouldReturnPublishedArticles_WhenTournamentHasArticles()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var tournament = TournamentFactory.Create(seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        var published = ArticleFactory.Create(
            title: "NEBA Singles Recap",
            slug: "neba-singles-recap",
            publicationStatus: PublicationStatus.Published,
            tournamentId: tournament.Id);
        var draft = ArticleFactory.Create(
            slug: "draft-article",
            publicationStatus: PublicationStatus.Draft,
            tournamentId: tournament.Id);
        await _dbContext.Articles.AddRangeAsync([published, draft], ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = new GetTournamentQueryHandler(_dbContext, fileStorageMock.Object, TimeProvider.System);

        // Act
        var result = await handler.HandleAsync(
            new GetTournamentQuery { Id = tournament.Id, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Articles.ShouldHaveSingleItem();
        var article = result.Value.Articles.Single();
        article.Title.ShouldBe("NEBA Singles Recap");
        article.Slug.ShouldBe("neba-singles-recap");
    }

    [Fact(DisplayName = "HandleAsync excludes draft articles when tournament has only draft articles")]
    public async Task HandleAsync_ShouldExcludeDraftArticles_WhenTournamentHasOnlyDraftArticles()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var tournament = TournamentFactory.Create(seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        var draft = ArticleFactory.Create(
            publicationStatus: PublicationStatus.Draft,
            tournamentId: tournament.Id);
        await _dbContext.Articles.AddAsync(draft, ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = new GetTournamentQueryHandler(_dbContext, fileStorageMock.Object, TimeProvider.System);

        // Act
        var result = await handler.HandleAsync(
            new GetTournamentQuery { Id = tournament.Id, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Articles.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync returns oil patterns with round names when tournament has oil patterns")]
    public async Task HandleAsync_ShouldReturnOilPatternsWithRoundNames_WhenTournamentHasOilPatterns()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var oilPattern = OilPatternFactory.Create(name: "Dragon", length: 40);
        await _dbContext.OilPatterns.AddAsync(oilPattern, ct);

        var tournament = TournamentFactory.Create(seasonId: season.Id);
        tournament.AddOilPattern(oilPattern.Id, TournamentRound.Qualifying, TournamentRound.StepLadder);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = new GetTournamentQueryHandler(_dbContext, fileStorageMock.Object, TimeProvider.System);

        // Act
        var result = await handler.HandleAsync(
            new GetTournamentQuery { Id = tournament.Id, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.OilPatterns.ShouldHaveSingleItem();
        var pattern = result.Value.OilPatterns.Single();
        pattern.Name.ShouldBe("Dragon");
        pattern.Length.ShouldBe(40);
        pattern.TournamentRounds.ShouldBe(["Qualifying", "Step Ladder"], ignoreOrder: true);
    }

    [Fact(DisplayName = "HandleAsync maps sponsor ID, title sponsor flag, and sponsorship amount when tournament has a sponsor")]
    public async Task HandleAsync_ShouldMapSponsorIdTitleSponsorAndSponsorshipAmount_WhenTournamentHasSponsor()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var sponsor = SponsorFactory.Create();
        await _dbContext.Sponsors.AddAsync(sponsor, ct);

        var tournament = TournamentFactory.Create(seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        tournament.AddSponsor(sponsor.Id, titleSponsor: true, sponsorshipAmount: 1234.56m);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = new GetTournamentQueryHandler(_dbContext, fileStorageMock.Object, TimeProvider.System);

        // Act
        var result = await handler.HandleAsync(
            new GetTournamentQuery { Id = tournament.Id, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Sponsors.ShouldHaveSingleItem();
        var mappedSponsor = result.Value.Sponsors.Single();
        mappedSponsor.SponsorId.ShouldBe(sponsor.Id);
        mappedSponsor.TitleSponsor.ShouldBeTrue();
        mappedSponsor.SponsorshipAmount.ShouldBe(1234.56m);
    }

    [Fact(DisplayName = "HandleAsync orders sponsors with the title sponsor first, then alphabetically by name")]
    public async Task HandleAsync_ShouldOrderSponsors_WithTitleSponsorFirstThenAlphabeticalByName()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var zetaSponsor = SponsorFactory.Create(name: "Zeta Bowling", slug: "zeta-bowling");
        var alphaSponsor = SponsorFactory.Create(name: "Alpha Lanes", slug: "alpha-lanes");
        var titleSponsor = SponsorFactory.Create(name: "Mid Title Sponsor", slug: "mid-title-sponsor");
        await _dbContext.Sponsors.AddRangeAsync([zetaSponsor, alphaSponsor, titleSponsor], ct);

        var tournament = TournamentFactory.Create(seasonId: season.Id);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        tournament.AddSponsor(zetaSponsor.Id, titleSponsor: false, sponsorshipAmount: 100m);
        tournament.AddSponsor(alphaSponsor.Id, titleSponsor: false, sponsorshipAmount: 100m);
        tournament.AddSponsor(titleSponsor.Id, titleSponsor: true, sponsorshipAmount: 5000m);
        await _dbContext.SaveChangesAsync(ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = new GetTournamentQueryHandler(_dbContext, fileStorageMock.Object, TimeProvider.System);

        // Act
        var result = await handler.HandleAsync(
            new GetTournamentQuery { Id = tournament.Id, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Sponsors.Select(s => s.Name).ShouldBe(["Mid Title Sponsor", "Alpha Lanes", "Zeta Bowling"]);
    }

    private async Task<Tournament> SeedTournamentWithOilPatternAsync(DateTimeOffset? oilPatternRevealDateTime, CancellationToken ct)
    {
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var oilPattern = OilPatternFactory.Create(name: "Dragon", volume: 22.5m, leftRatio: 4.0m, rightRatio: 6.0m);
        await _dbContext.OilPatterns.AddAsync(oilPattern, ct);

        var tournament = TournamentFactory.Create(seasonId: season.Id, oilPatternRevealDateTime: oilPatternRevealDateTime);
        tournament.AddOilPattern(oilPattern.Id, TournamentRound.Qualifying);
        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        return tournament;
    }

    [Fact(DisplayName = "HandleAsync returns reduced oil pattern info and no reveal date when caller is anonymous and reveal date is in the future")]
    public async Task HandleAsync_ShouldReturnReducedOilPatternInfoAndNoRevealDate_WhenCallerIsAnonymousAndRevealDateIsInFuture()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var now = DateTimeOffset.UtcNow.TruncateToMicroseconds();
        var revealAt = now.AddDays(5);
        var tournament = await SeedTournamentWithOilPatternAsync(revealAt, ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = new GetTournamentQueryHandler(_dbContext, fileStorageMock.Object, new FakeTimeProvider(now));

        // Act
        var result = await handler.HandleAsync(
            new GetTournamentQuery { Id = tournament.Id, CallerIsAuthenticated = false, CallerHasTournamentManagementPermission = false }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.OilPatterns.ShouldBeEmpty();
        result.Value.OilPatternRevealDateTime.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync returns reduced oil pattern info but the reveal date when caller is authenticated without the management permission and reveal date is in the future")]
    public async Task HandleAsync_ShouldReturnReducedOilPatternInfoButRevealDate_WhenCallerIsAuthenticatedWithoutManagementPermissionAndRevealDateIsInFuture()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var now = DateTimeOffset.UtcNow.TruncateToMicroseconds();
        var revealAt = now.AddDays(5);
        var tournament = await SeedTournamentWithOilPatternAsync(revealAt, ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = new GetTournamentQueryHandler(_dbContext, fileStorageMock.Object, new FakeTimeProvider(now));

        // Act
        var result = await handler.HandleAsync(
            new GetTournamentQuery { Id = tournament.Id, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = false }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.OilPatterns.ShouldBeEmpty();
        result.Value.OilPatternRevealDateTime.ShouldBe(revealAt);
    }

    [Fact(DisplayName = "HandleAsync returns full oil pattern info when caller has the management permission, even before the reveal date")]
    public async Task HandleAsync_ShouldReturnFullOilPatternInfo_WhenCallerHasManagementPermissionBeforeRevealDate()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var now = DateTimeOffset.UtcNow.TruncateToMicroseconds();
        var revealAt = now.AddDays(5);
        var tournament = await SeedTournamentWithOilPatternAsync(revealAt, ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = new GetTournamentQueryHandler(_dbContext, fileStorageMock.Object, new FakeTimeProvider(now));

        // Act
        var result = await handler.HandleAsync(
            new GetTournamentQuery { Id = tournament.Id, CallerIsAuthenticated = true, CallerHasTournamentManagementPermission = true }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.OilPatterns.ShouldHaveSingleItem();
        var pattern = result.Value.OilPatterns.Single();
        pattern.Volume.ShouldBe(22.5m);
        pattern.LeftRatio.ShouldBe(4.0m);
        pattern.RightRatio.ShouldBe(6.0m);
        result.Value.OilPatternRevealDateTime.ShouldBe(revealAt);
    }

    [Fact(DisplayName = "HandleAsync returns full oil pattern info when the reveal date has passed, regardless of caller")]
    public async Task HandleAsync_ShouldReturnFullOilPatternInfo_WhenRevealDateHasPassed()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var now = DateTimeOffset.UtcNow.TruncateToMicroseconds();
        var tournament = await SeedTournamentWithOilPatternAsync(now.AddDays(-1), ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = new GetTournamentQueryHandler(_dbContext, fileStorageMock.Object, new FakeTimeProvider(now));

        // Act
        var result = await handler.HandleAsync(
            new GetTournamentQuery { Id = tournament.Id, CallerIsAuthenticated = false, CallerHasTournamentManagementPermission = false }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.OilPatterns.ShouldHaveSingleItem();
        result.Value.OilPatternRevealDateTime.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync returns full oil pattern info when caller is anonymous and no reveal date is set")]
    public async Task HandleAsync_ShouldReturnFullOilPatternInfo_WhenCallerIsAnonymousAndNoRevealDateIsSet()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentWithOilPatternAsync(null, ct);

        var fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        var handler = new GetTournamentQueryHandler(_dbContext, fileStorageMock.Object, TimeProvider.System);

        // Act
        var result = await handler.HandleAsync(
            new GetTournamentQuery { Id = tournament.Id, CallerIsAuthenticated = false, CallerHasTournamentManagementPermission = false }, ct);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.OilPatterns.ShouldHaveSingleItem();
        result.Value.OilPatternRevealDateTime.ShouldBeNull();
    }
}