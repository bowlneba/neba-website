using ErrorOr;

using Neba.Application.Sponsors;
using Neba.Application.Sponsors.GetSponsorDetail;
using Neba.Application.Storage;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Sponsors;

namespace Neba.Application.Tests.Sponsors.GetSponsorDetail;

[UnitTest]
[Component("Sponsors")]
public sealed class GetSponsorDetailQueryHandlerTests
{
    private readonly Mock<ISponsorQueries> _sponsorQueriesMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly GetSponsorDetailQueryHandler _handler;

    public GetSponsorDetailQueryHandlerTests()
    {
        _sponsorQueriesMock = new Mock<ISponsorQueries>(MockBehavior.Strict);
        _fileStorageServiceMock = new Mock<IFileStorageService>(MockBehavior.Strict);

        _handler = new GetSponsorDetailQueryHandler(_sponsorQueriesMock.Object, _fileStorageServiceMock.Object);
    }

    [Fact(DisplayName = "HandleAsync should return sponsor when sponsor exists")]
    public async Task HandleAsync_ShouldReturnSponsor_WhenSponsorExists()
    {
        // Arrange
        var sponsor = SponsorDetailDtoFactory.Create(slug: "acme", logoContainer: null, logoPath: null);
        var query = new GetSponsorDetailQuery { Slug = "acme" };

        _sponsorQueriesMock
            .Setup(q => q.GetSponsorAsync(query.Slug, TestContext.Current.CancellationToken))
            .ReturnsAsync(sponsor);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(sponsor);
    }

    [Fact(DisplayName = "HandleAsync should resolve logo URL when sponsor has a logo")]
    public async Task HandleAsync_ShouldResolveLogoUrl_WhenSponsorHasLogo()
    {
        // Arrange
        var sponsor = SponsorDetailDtoFactory.Create(slug: "acme", logoContainer: "sponsors", logoPath: "logos/acme.png");
        var query = new GetSponsorDetailQuery { Slug = "acme" };
        var expectedUrl = new Uri("https://cdn.example.com/sponsors/logos/acme.png");

        _sponsorQueriesMock
            .Setup(q => q.GetSponsorAsync(query.Slug, TestContext.Current.CancellationToken))
            .ReturnsAsync(sponsor);

        _fileStorageServiceMock
            .Setup(s => s.GetBlobUri("sponsors", "logos/acme.png"))
            .Returns(expectedUrl);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.LogoUrl.ShouldBe(expectedUrl);
    }

    [Fact(DisplayName = "HandleAsync should not set logo URL when sponsor has no logo")]
    public async Task HandleAsync_ShouldNotSetLogoUrl_WhenSponsorHasNoLogo()
    {
        // Arrange
        var sponsor = SponsorDetailDtoFactory.Create(slug: "acme", logoContainer: null, logoPath: null);
        var query = new GetSponsorDetailQuery { Slug = "acme" };

        _sponsorQueriesMock
            .Setup(q => q.GetSponsorAsync(query.Slug, TestContext.Current.CancellationToken))
            .ReturnsAsync(sponsor);

        // Canary: if Conditional(true) mutation fires, GetBlobUri(null, null) is called.
        // Returning a URL here lets the assertion below catch the mutation rather than relying on MockException.
        _fileStorageServiceMock
            .Setup(s => s.GetBlobUri(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new Uri("https://unexpected.example.com/logo.png"));

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.LogoUrl.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should return not found error when sponsor does not exist")]
    public async Task HandleAsync_ShouldReturnNotFoundError_WhenSponsorDoesNotExist()
    {
        // Arrange
        var query = new GetSponsorDetailQuery { Slug = "missing-sponsor" };

        _sponsorQueriesMock
            .Setup(q => q.GetSponsorAsync(query.Slug, TestContext.Current.CancellationToken))
            .ReturnsAsync((SponsorDetailDto?)null);

        // Act
        ErrorOr<SponsorDetailDto> result = default;
        await Should.NotThrowAsync(async () => { result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken); });

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Sponsor.NotFound");
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
        var metadata = result.FirstError.Metadata;
        metadata.ShouldNotBeNull();
        metadata.ShouldContainKey("slug");
        metadata["slug"]?.ShouldBe(query.Slug);
    }

    [Fact(DisplayName = "HandleAsync should not set logo URL when sponsor has container but no path")]
    public async Task HandleAsync_ShouldNotSetLogoUrl_WhenSponsorHasContainerButNoPath()
    {
        // Arrange — only LogoContainer set; LogoPath null.
        // The && guard means this should NOT resolve a URL. The || mutation would call
        // GetBlobUri(container, null) — MockBehavior.Strict would throw, killing the mutation.
        var sponsor = SponsorDetailDtoFactory.Create(slug: "acme", logoContainer: "sponsors", logoPath: null);
        var query = new GetSponsorDetailQuery { Slug = "acme" };

        _sponsorQueriesMock
            .Setup(q => q.GetSponsorAsync(query.Slug, TestContext.Current.CancellationToken))
            .ReturnsAsync(sponsor);

        // Canary: if || mutation fires, GetBlobUri("sponsors", null) is called.
        // Returning a URL here lets the assertion below catch the mutation rather than relying on MockException.
        _fileStorageServiceMock
            .Setup(s => s.GetBlobUri(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new Uri("https://unexpected.example.com/logo.png"));

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.LogoUrl.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should pass cancellation token to sponsor queries")]
    public async Task HandleAsync_ShouldPassCancellationToken_ToSponsorQueries()
    {
        // Arrange
        var sponsor = SponsorDetailDtoFactory.Create(slug: "acme", logoContainer: null, logoPath: null);
        var query = new GetSponsorDetailQuery { Slug = "acme" };
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _sponsorQueriesMock
            .Setup(q => q.GetSponsorAsync(query.Slug, cancellationToken))
            .ReturnsAsync(sponsor);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(sponsor);
    }
}