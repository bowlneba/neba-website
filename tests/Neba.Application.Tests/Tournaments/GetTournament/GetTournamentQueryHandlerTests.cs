using ErrorOr;

using Neba.Application.Sponsors;
using Neba.Application.Storage;
using Neba.Application.Tournaments;
using Neba.Application.Tournaments.GetTournament;
using Neba.Domain.Tournaments;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Sponsors;
using Neba.TestFactory.Tournaments;

namespace Neba.Application.Tests.Tournaments.GetTournament;

[UnitTest]
[Component("Tournaments")]
public sealed class GetTournamentQueryHandlerTests
{
    private readonly Mock<ITournamentQueries> _tournamentQueriesMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly GetTournamentQueryHandler _handler;

    public GetTournamentQueryHandlerTests()
    {
        _tournamentQueriesMock = new Mock<ITournamentQueries>(MockBehavior.Strict);
        _fileStorageServiceMock = new Mock<IFileStorageService>(MockBehavior.Strict);
        _handler = new GetTournamentQueryHandler(
            _tournamentQueriesMock.Object,
            _fileStorageServiceMock.Object);
    }

    [Fact(DisplayName = "HandleAsync should return tournament detail when tournament exists")]
    public async Task HandleAsync_ShouldReturnTournamentDetail_WhenTournamentExists()
    {
        // Arrange — Create() defaults logoContainer/logoPath to null, so no logo URL resolution occurs.
        var dto = TournamentDetailDtoFactory.Create();
        var query = new GetTournamentQuery { Id = dto.Id };

        _tournamentQueriesMock
            .Setup(q => q.GetTournamentDetailAsync(query.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(dto);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(dto);
    }

    [Fact(DisplayName = "HandleAsync should return not found error when tournament does not exist")]
    public async Task HandleAsync_ShouldReturnNotFoundError_WhenTournamentDoesNotExist()
    {
        // Arrange
        var id = TournamentId.New();
        var query = new GetTournamentQuery { Id = id };

        _tournamentQueriesMock
            .Setup(q => q.GetTournamentDetailAsync(query.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync((TournamentDetailDto?)null);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.NotFound");
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
        var metadata = result.FirstError.Metadata;
        metadata.ShouldNotBeNull();
        metadata.ShouldContainKey("TournamentId");
        metadata["TournamentId"]?.ShouldBe(id.ToString());
    }

    [Fact(DisplayName = "HandleAsync should pass cancellation token to tournament queries")]
    public async Task HandleAsync_ShouldPassCancellationToken_ToTournamentQueries()
    {
        // Arrange — Create() defaults logoContainer/logoPath to null, so no logo URL resolution occurs.
        var dto = TournamentDetailDtoFactory.Create();
        var query = new GetTournamentQuery { Id = dto.Id };
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _tournamentQueriesMock
            .Setup(q => q.GetTournamentDetailAsync(query.Id, cancellationToken))
            .ReturnsAsync(dto);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(dto);
    }

    [Fact(DisplayName = "HandleAsync should resolve logo URL when tournament has both container and path")]
    public async Task HandleAsync_ShouldResolveLogoUrl_WhenTournamentHasBothContainerAndPath()
    {
        // Arrange
        const string logoContainer = "tournament-logos";
        const string logoPath = "spring-open/logo.png";
        var expectedLogoUrl = new Uri($"https://storage.example.com/{logoContainer}/{logoPath}");
        var dto = TournamentDetailDtoFactory.Create(logoContainer: logoContainer, logoPath: logoPath, logoUrl: null);
        var query = new GetTournamentQuery { Id = dto.Id };

        _tournamentQueriesMock
            .Setup(q => q.GetTournamentDetailAsync(query.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(dto);

        _fileStorageServiceMock
            .Setup(s => s.GetBlobUri(logoContainer, logoPath))
            .Returns(expectedLogoUrl);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.LogoUrl.ShouldBe(expectedLogoUrl);
    }

    [Fact(DisplayName = "HandleAsync should keep logo URL null when tournament has no logo container")]
    public async Task HandleAsync_ShouldKeepLogoUrlNull_WhenTournamentHasNoLogoContainer()
    {
        // Arrange
        // Canary: if the && guard is mutated to ||, GetBlobUri(null, path) is called.
        // Returning a URL here lets the assertion catch the mutation rather than relying on MockException.
        var dto = TournamentDetailDtoFactory.Create(logoContainer: null, logoPath: "spring-open/logo.png", logoUrl: null);
        var query = new GetTournamentQuery { Id = dto.Id };

        _tournamentQueriesMock
            .Setup(q => q.GetTournamentDetailAsync(query.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(dto);

        _fileStorageServiceMock
            .Setup(s => s.GetBlobUri(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new Uri("https://unexpected.example.com/logo.png"));

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.LogoUrl.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should not set logo URL when tournament has container but no path")]
    public async Task HandleAsync_ShouldNotSetLogoUrl_WhenTournamentHasContainerButNoPath()
    {
        // Arrange — LogoContainer set but LogoPath null.
        // The && guard means no URL should be resolved. The || mutation would call
        // GetBlobUri(container, null) — the assertion below catches the mutation.
        var dto = TournamentDetailDtoFactory.Create(logoContainer: "tournament-logos", logoPath: null, logoUrl: null);
        var query = new GetTournamentQuery { Id = dto.Id };

        _tournamentQueriesMock
            .Setup(q => q.GetTournamentDetailAsync(query.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(dto);

        // Canary: if || mutation fires, GetBlobUri("tournament-logos", null) is called.
        _fileStorageServiceMock
            .Setup(s => s.GetBlobUri(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new Uri("https://unexpected.example.com/logo.png"));

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.LogoUrl.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should resolve sponsor logo URLs when sponsors have both container and path")]
    public async Task HandleAsync_ShouldResolveSponsorLogoUrls_WhenSponsorsHaveBothContainerAndPath()
    {
        // Arrange
        const string container = "sponsor-logos";
        const string path = "acme/logo.png";
        var expectedLogoUrl = new Uri($"https://storage.example.com/{container}/{path}");
        var sponsor = SponsorSummaryDtoFactory.Create(logoContainer: container, logoPath: path, logoUrl: null);
        var dto = TournamentDetailDtoFactory.Create(logoContainer: null, logoPath: null, sponsors: [sponsor]);
        var query = new GetTournamentQuery { Id = dto.Id };

        _tournamentQueriesMock
            .Setup(q => q.GetTournamentDetailAsync(query.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(dto);

        _fileStorageServiceMock
            .Setup(s => s.GetBlobUri(container, path))
            .Returns(expectedLogoUrl);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Sponsors.Single().LogoUrl.ShouldBe(expectedLogoUrl);
    }

    [Fact(DisplayName = "HandleAsync should keep sponsor logo URL null when sponsor has no logo container")]
    public async Task HandleAsync_ShouldKeepSponsorLogoUrlNull_WhenSponsorHasNoLogoContainer()
    {
        // Arrange
        // Canary: if && guard mutated to ||, GetBlobUri(null, path) is called.
        // Construct directly — the factory uses ?? defaults so null can't be passed through it.
        var sponsor = new SponsorSummaryDto { Name = "Acme Corp", Slug = "acme-corp", LogoContainer = null, LogoPath = "acme/logo.png" };
        var dto = TournamentDetailDtoFactory.Create(logoContainer: null, logoPath: null, sponsors: [sponsor]);
        var query = new GetTournamentQuery { Id = dto.Id };

        _tournamentQueriesMock
            .Setup(q => q.GetTournamentDetailAsync(query.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(dto);

        _fileStorageServiceMock
            .Setup(s => s.GetBlobUri(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new Uri("https://unexpected.example.com/logo.png"));

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Sponsors.Single().LogoUrl.ShouldBeNull();
    }
}