using Neba.Application.Sponsors;
using Neba.Application.Sponsors.ListActiveSponsors;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Sponsors;

namespace Neba.Application.Tests.Sponsors.ListActiveSponsors;

[UnitTest]
[Component("Sponsors")]
public sealed class ListActiveSponsorsQueryHandlerTests
{
    private readonly Mock<ISponsorQueries> _sponsorQueriesMock;
    private readonly ListActiveSponsorsQueryHandler _handler;

    public ListActiveSponsorsQueryHandlerTests()
    {
        _sponsorQueriesMock = new Mock<ISponsorQueries>(MockBehavior.Strict);

        _handler = new ListActiveSponsorsQueryHandler(_sponsorQueriesMock.Object);
    }

    [Fact(DisplayName = "Should return all active sponsors returned by queries")]
    public async Task HandleAsync_ShouldReturnAllActiveSponsors()
    {
        // Arrange
        IReadOnlyCollection<SponsorSummaryDto> sponsors = SponsorSummaryDtoFactory.Bogus(5);
        var query = new ListActiveSponsorsQuery();

        _sponsorQueriesMock
            .Setup(q => q.GetActiveSponsorsAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(sponsors);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(sponsors);
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

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(5);
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
}
