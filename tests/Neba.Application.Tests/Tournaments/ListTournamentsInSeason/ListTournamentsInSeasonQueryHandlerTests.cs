using Neba.Application.Sponsors;
using Neba.Application.Storage;
using Neba.Application.Tournaments;
using Neba.Application.Tournaments.ListTournamentsInSeason;
using Neba.Domain.Seasons;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Sponsors;
using Neba.TestFactory.Tournaments;

namespace Neba.Application.Tests.Tournaments.ListTournamentsInSeason;

[UnitTest]
[Component("Tournaments")]
public sealed class ListTournamentsInSeasonQueryHandlerTests
{
    private readonly Mock<ITournamentQueries> _tournamentQueriesMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly ListTournamentsInSeasonQueryHandler _handler;

    public ListTournamentsInSeasonQueryHandlerTests()
    {
        _tournamentQueriesMock = new Mock<ITournamentQueries>(MockBehavior.Strict);
        _fileStorageServiceMock = new Mock<IFileStorageService>(MockBehavior.Strict);

        _handler = new ListTournamentsInSeasonQueryHandler(
            _tournamentQueriesMock.Object,
            _fileStorageServiceMock.Object);
    }

    [Fact(DisplayName = "Should return all tournaments returned by queries")]
    public async Task HandleAsync_ShouldReturnAllTournaments()
    {
        // Arrange
        IReadOnlyCollection<SeasonTournamentDto> tournaments = SeasonTournamentDtoFactory.Bogus(3, seed: 42);
        var seasonId = SeasonId.New();
        var query = new ListTournamentsInSeasonQuery { SeasonId = seasonId };

        _tournamentQueriesMock
            .Setup(q => q.GetTournamentsInSeasonAsync(seasonId, TestContext.Current.CancellationToken))
            .ReturnsAsync(tournaments);

        _fileStorageServiceMock
            .Setup(s => s.GetBlobUri(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string container, string path) => new Uri($"https://storage.example.com/{container}/{path}"));

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert — compare count, IDs, and resolved URLs (not full record equality since sponsor
        // collections produce new array instances that break reference equality inside records)
        result.Count.ShouldBe(tournaments.Count);
        result.Select(t => t.Id).ShouldBe(tournaments.Select(t => t.Id));

        // Tournament logos
        foreach (var (actual, original) in result.Zip(tournaments))
        {
            var expectedLogoUrl = original.LogoContainer is not null && original.LogoPath is not null
                ? new Uri($"https://storage.example.com/{original.LogoContainer}/{original.LogoPath}")
                : original.LogoUrl;
            actual.LogoUrl.ShouldBe(expectedLogoUrl);

            // Sponsor logos
            actual.Sponsors.Count.ShouldBe(original.Sponsors.Count);
            foreach (var (actualSponsor, originalSponsor) in actual.Sponsors.Zip(original.Sponsors))
            {
                var expectedSponsorLogoUrl = originalSponsor.LogoContainer is not null && originalSponsor.LogoPath is not null
                    ? new Uri($"https://storage.example.com/{originalSponsor.LogoContainer}/{originalSponsor.LogoPath}")
                    : originalSponsor.LogoUrl;
                actualSponsor.LogoUrl.ShouldBe(expectedSponsorLogoUrl);
            }
        }
    }

    [Fact(DisplayName = "Should return 3 tournaments when queries returns 3")]
    public async Task HandleAsync_ShouldReturn3Tournaments_WhenQueriesReturns3()
    {
        // Arrange
        var seasonId = SeasonId.New();
        var tournaments = SeasonTournamentDtoFactory.Bogus(3);
        var query = new ListTournamentsInSeasonQuery { SeasonId = seasonId };

        _tournamentQueriesMock
            .Setup(q => q.GetTournamentsInSeasonAsync(seasonId, TestContext.Current.CancellationToken))
            .ReturnsAsync(tournaments);

        _fileStorageServiceMock
            .Setup(s => s.GetBlobUri(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string container, string path) => new Uri($"https://storage.example.com/{container}/{path}"));

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(3);
    }

    [Fact(DisplayName = "Should return empty collection when queries returns no tournaments")]
    public async Task HandleAsync_ShouldReturnEmptyCollection_WhenQueriesReturnsNone()
    {
        // Arrange
        var seasonId = SeasonId.New();
        IReadOnlyCollection<SeasonTournamentDto> empty = [];
        var query = new ListTournamentsInSeasonQuery { SeasonId = seasonId };

        _tournamentQueriesMock
            .Setup(q => q.GetTournamentsInSeasonAsync(seasonId, TestContext.Current.CancellationToken))
            .ReturnsAsync(empty);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should resolve logo URL when tournament has both container and path")]
    public async Task HandleAsync_ShouldResolveLogoUrl_WhenTournamentHasBothContainerAndPath()
    {
        // Arrange
        var seasonId = SeasonId.New();
        const string logoContainer = "tournament-logos";
        const string logoPath = "spring-open/logo.png";
        var expectedLogoUrl = new Uri($"https://storage.example.com/{logoContainer}/{logoPath}");
        var tournament = SeasonTournamentDtoFactory.Create(logoUrl: null, logoContainer: logoContainer, logoPath: logoPath);
        var query = new ListTournamentsInSeasonQuery { SeasonId = seasonId };

        _tournamentQueriesMock
            .Setup(q => q.GetTournamentsInSeasonAsync(seasonId, TestContext.Current.CancellationToken))
            .ReturnsAsync([tournament]);

        _fileStorageServiceMock
            .Setup(s => s.GetBlobUri(logoContainer, logoPath))
            .Returns(expectedLogoUrl);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.Single().LogoUrl.ShouldBe(expectedLogoUrl);
    }

    [Fact(DisplayName = "Should keep logo URL null when tournament has no logo container or path")]
    public async Task HandleAsync_ShouldKeepLogoUrlNull_WhenTournamentHasNoLogoContainerOrPath()
    {
        // Arrange
        var seasonId = SeasonId.New();
        var tournament = SeasonTournamentDtoFactory.Create(logoUrl: null, logoContainer: null, logoPath: null);
        var query = new ListTournamentsInSeasonQuery { SeasonId = seasonId };

        _tournamentQueriesMock
            .Setup(q => q.GetTournamentsInSeasonAsync(seasonId, TestContext.Current.CancellationToken))
            .ReturnsAsync([tournament]);

        // Canary: if the && guard is mutated to ||, GetBlobUri(null, null) is called.
        // Returning a URL here lets the assertion catch the mutation rather than relying on MockException.
        _fileStorageServiceMock
            .Setup(s => s.GetBlobUri(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new Uri("https://unexpected.example.com/logo.png"));

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.Single().LogoUrl.ShouldBeNull();
    }

    [Fact(DisplayName = "Should not set logo URL when tournament has container but no path")]
    public async Task HandleAsync_ShouldNotSetLogoUrl_WhenTournamentHasContainerButNoPath()
    {
        // Arrange — LogoContainer set but LogoPath null.
        // The && guard means no URL should be resolved. The || mutation would call
        // GetBlobUri(container, null) — the assertion below catches the mutation.
        var seasonId = SeasonId.New();
        var tournament = SeasonTournamentDtoFactory.Create(logoUrl: null, logoContainer: "tournament-logos", logoPath: null);
        var query = new ListTournamentsInSeasonQuery { SeasonId = seasonId };

        _tournamentQueriesMock
            .Setup(q => q.GetTournamentsInSeasonAsync(seasonId, TestContext.Current.CancellationToken))
            .ReturnsAsync([tournament]);

        // Canary: if || mutation fires, GetBlobUri("tournament-logos", null) is called.
        _fileStorageServiceMock
            .Setup(s => s.GetBlobUri(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new Uri("https://unexpected.example.com/logo.png"));

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.Single().LogoUrl.ShouldBeNull();
    }

    [Fact(DisplayName = "Should resolve sponsor logo URLs when sponsors have both container and path")]
    public async Task HandleAsync_ShouldResolveSponsorLogoUrls_WhenSponsorsHaveBothContainerAndPath()
    {
        // Arrange
        var seasonId = SeasonId.New();
        const string container = "sponsor-logos";
        const string path = "acme/logo.png";
        var expectedLogoUrl = new Uri($"https://storage.example.com/{container}/{path}");
        var sponsor = SponsorSummaryDtoFactory.Create(logoUrl: null, logoContainer: container, logoPath: path);
        var tournament = SeasonTournamentDtoFactory.Create(
sponsors: [sponsor], logoContainer: null, logoPath: null);
        var query = new ListTournamentsInSeasonQuery { SeasonId = seasonId };

        _tournamentQueriesMock
            .Setup(q => q.GetTournamentsInSeasonAsync(seasonId, TestContext.Current.CancellationToken))
            .ReturnsAsync([tournament]);

        _fileStorageServiceMock
            .Setup(s => s.GetBlobUri(container, path))
            .Returns(expectedLogoUrl);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.Single().Sponsors.Single().LogoUrl.ShouldBe(expectedLogoUrl);
    }

    [Fact(DisplayName = "Should keep sponsor logo URL null when sponsor has no logo container")]
    public async Task HandleAsync_ShouldKeepSponsorLogoUrlNull_WhenSponsorHasNoLogoContainer()
    {
        // Arrange
        // Canary: if && guard mutated to ||, GetBlobUri(null, path) is called.
        // Construct directly — the factory uses ?? defaults so null can't be passed through it.
        var seasonId = SeasonId.New();
        var sponsor = new SponsorSummaryDto { Name = "Acme Corp", Slug = "acme-corp", LogoContainer = null, LogoPath = "acme/logo.png" };
        var tournament = SeasonTournamentDtoFactory.Create(
sponsors: [sponsor], logoContainer: null, logoPath: null);
        var query = new ListTournamentsInSeasonQuery { SeasonId = seasonId };

        _tournamentQueriesMock
            .Setup(q => q.GetTournamentsInSeasonAsync(seasonId, TestContext.Current.CancellationToken))
            .ReturnsAsync([tournament]);

        _fileStorageServiceMock
            .Setup(s => s.GetBlobUri(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new Uri("https://unexpected.example.com/logo.png"));

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.Single().Sponsors.Single().LogoUrl.ShouldBeNull();
    }
}