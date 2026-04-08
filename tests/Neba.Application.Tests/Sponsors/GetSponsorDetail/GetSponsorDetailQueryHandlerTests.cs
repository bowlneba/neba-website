using ErrorOr;

using Neba.Application.Sponsors;
using Neba.Application.Sponsors.GetSponsorDetail;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Sponsors;

namespace Neba.Application.Tests.Sponsors.GetSponsorDetail;

[UnitTest]
[Component("Sponsors")]
public sealed class GetSponsorDetailQueryHandlerTests
{
    private readonly Mock<ISponsorQueries> _sponsorQueriesMock;
    private readonly GetSponsorDetailQueryHandler _handler;

    public GetSponsorDetailQueryHandlerTests()
    {
        _sponsorQueriesMock = new Mock<ISponsorQueries>(MockBehavior.Strict);

        _handler = new GetSponsorDetailQueryHandler(_sponsorQueriesMock.Object);
    }

    [Fact(DisplayName = "HandleAsync should return sponsor when sponsor exists")]
    public async Task HandleAsync_ShouldReturnSponsor_WhenSponsorExists()
    {
        // Arrange
        var sponsor = SponsorDetailDtoFactory.Create(slug: "acme");
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

    [Fact(DisplayName = "HandleAsync should return not found error when sponsor does not exist")]
    public async Task HandleAsync_ShouldReturnNotFoundError_WhenSponsorDoesNotExist()
    {
        // Arrange
        var query = new GetSponsorDetailQuery { Slug = "missing-sponsor" };

        _sponsorQueriesMock
            .Setup(q => q.GetSponsorAsync(query.Slug, TestContext.Current.CancellationToken))
            .ReturnsAsync((SponsorDetailDto?)null);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Sponsor.NotFound");
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
        var metadata = result.FirstError.Metadata;
        metadata.ShouldNotBeNull();
        metadata.ShouldContainKey("slug");
        metadata["slug"]?.ShouldBe(query.Slug);
    }

    [Fact(DisplayName = "HandleAsync should pass cancellation token to sponsor queries")]
    public async Task HandleAsync_ShouldPassCancellationToken_ToSponsorQueries()
    {
        // Arrange
        var sponsor = SponsorDetailDtoFactory.Create(slug: "acme");
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