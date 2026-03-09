using Neba.Application.BowlingCenters;
using Neba.Application.BowlingCenters.ListBowlingCenters;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;

namespace Neba.Application.Tests.BowlingCenters.ListBowlingCenters;

[UnitTest]
[Component("BowlingCenters")]
public sealed class ListBowlingCentersQueryHandlerTests
{
    private readonly Mock<IBowlingCenterQueries> _bowlingCenterQueriesMock;
    private readonly ListBowlingCentersQueryHandler _handler;

    public ListBowlingCentersQueryHandlerTests()
    {
        _bowlingCenterQueriesMock = new Mock<IBowlingCenterQueries>(MockBehavior.Strict);

        _handler = new ListBowlingCentersQueryHandler(_bowlingCenterQueriesMock.Object);
    }

    [Fact(DisplayName = "Should return all bowling centers returned by queries")]
    public async Task HandleAsync_ShouldReturnAllBowlingCenters()
    {
        // Arrange
        var bowlingCenters = BowlingCenterSummaryDtoFactory.Bogus(5, seed: 42);
        var query = new ListBowlingCentersQuery();

        _bowlingCenterQueriesMock
            .Setup(q => q.GetAllAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(bowlingCenters);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(bowlingCenters);
    }

    [Fact(DisplayName = "Should return 5 bowling centers when queries returns 5")]
    public async Task HandleAsync_ShouldReturn5BowlingCenters_WhenQueriesReturns5()
    {
        // Arrange
        var bowlingCenters = BowlingCenterSummaryDtoFactory.Bogus(5);
        var query = new ListBowlingCentersQuery();

        _bowlingCenterQueriesMock
            .Setup(q => q.GetAllAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(bowlingCenters);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(5);
    }

    [Fact(DisplayName = "Should return empty collection when queries returns no bowling centers")]
    public async Task HandleAsync_ShouldReturnEmptyCollection_WhenQueriesReturnsNone()
    {
        // Arrange
        IReadOnlyCollection<BowlingCenterSummaryDto> empty = [];
        var query = new ListBowlingCentersQuery();

        _bowlingCenterQueriesMock
            .Setup(q => q.GetAllAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(empty);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }
}