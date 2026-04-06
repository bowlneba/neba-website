using Neba.Application.Sponsors;
using Neba.Application.Sponsors.ListActiveSponsors;
using Neba.Application.Storage;
using Neba.Domain.Sponsors;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Sponsors;

namespace Neba.Application.Tests.Sponsors.ListActiveSponsors;

[UnitTest]
[Component("Sponsors")]
public sealed class ListActiveSponsorsQueryHandlerTests
{
    private readonly Mock<ISponsorQueries> _sponsorQueriesMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly ListActiveSponsorsQueryHandler _handler;

    public ListActiveSponsorsQueryHandlerTests()
    {
        _sponsorQueriesMock = new Mock<ISponsorQueries>(MockBehavior.Strict);
        _fileStorageServiceMock = new Mock<IFileStorageService>(MockBehavior.Strict);

        _handler = new ListActiveSponsorsQueryHandler(
            _sponsorQueriesMock.Object,
            _fileStorageServiceMock.Object);
    }

    [Fact(DisplayName = "Should return all active sponsors returned by queries")]
    public async Task HandleAsync_ShouldReturnAllActiveSponsors()
    {
        // Arrange
        IReadOnlyCollection<SponsorSummaryDto> sponsors = SponsorSummaryDtoFactory.Bogus(5, seed: 42);
        var query = new ListActiveSponsorsQuery();

        _sponsorQueriesMock
            .Setup(q => q.GetActiveSponsorsAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(sponsors);

        _fileStorageServiceMock
            .Setup(s => s.GetBlobUri(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string container, string path) => new Uri($"https://storage.example.com/{container}/{path}"));

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(sponsors);
        result.ShouldAllBe(s => s.LogoUri != null);
    }

    [Fact(DisplayName = "Should return 5 active sponsors when queries returns 5")]
    public async Task HandleAsync_ShouldReturn5ActiveSponsors_WhenQueriesReturns5()
    {
        // Arrange
        IReadOnlyCollection<SponsorSummaryDto> sponsors = SponsorSummaryDtoFactory.Bogus(5);
        var query = new ListActiveSponsorsQuery();

        _sponsorQueriesMock
            .Setup(q => q.GetActiveSponsorsAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(sponsors);

        _fileStorageServiceMock
            .Setup(s => s.GetBlobUri(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string container, string path) => new Uri($"https://storage.example.com/{container}/{path}"));

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(5);
        result.ShouldAllBe(s => s.LogoUri != null);
    }

    [Fact(DisplayName = "Should return empty collection when queries returns no active sponsors")]
    public async Task HandleAsync_ShouldReturnEmptyCollection_WhenQueriesReturnsNone()
    {
        // Arrange
        IReadOnlyCollection<SponsorSummaryDto> empty = [];
        var query = new ListActiveSponsorsQuery();

        _sponsorQueriesMock
            .Setup(q => q.GetActiveSponsorsAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(empty);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should keep LogoUri null when sponsors do not have logo container and path")]
    public async Task HandleAsync_ShouldKeepLogoUriNull_WhenSponsorsDoNotHaveLogoDetails()
    {
        // Arrange
        IReadOnlyCollection<SponsorSummaryDto> sponsors =
        [
            new SponsorSummaryDto
            {
                Name = "Sponsor One",
                Slug = "sponsor-one",
                LogoContainer = null,
                LogoPath = null,
                IsCurrentSponsor = true,
                Priority = 1,
                Tier = SponsorTier.Standard,
                Category = SponsorCategory.Technology
            },
            new SponsorSummaryDto
            {
                Name = "Sponsor Two",
                Slug = "sponsor-two",
                LogoContainer = null,
                LogoPath = null,
                IsCurrentSponsor = true,
                Priority = 2,
                Tier = SponsorTier.Standard,
                Category = SponsorCategory.Technology
            }
        ];
        var query = new ListActiveSponsorsQuery();

        _sponsorQueriesMock
            .Setup(q => q.GetActiveSponsorsAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(sponsors);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldAllBe(s => s.LogoUri == null);
    }

    [Fact(DisplayName = "Should return sponsors with logo uri when queries returns sponsors")]
    public async Task HandleAsync_ShouldReturnSponsorsWithLogoUri_WhenQueriesReturnsSponsors()
    {
        // Arrange
        IReadOnlyCollection<SponsorSummaryDto> sponsors = SponsorSummaryDtoFactory.Bogus(5, seed: 42);
        var query = new ListActiveSponsorsQuery();

        _sponsorQueriesMock
            .Setup(q => q.GetActiveSponsorsAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(sponsors);

        _fileStorageServiceMock
            .Setup(s => s.GetBlobUri(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string container, string path) => new Uri($"https://storage.example.com/{container}/{path}"));

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        foreach (var sponsor in result)
        {
            sponsor.LogoUri.ShouldNotBeNull();
            sponsor.LogoUri.IsAbsoluteUri.ShouldBeTrue();
        }
    }
}
