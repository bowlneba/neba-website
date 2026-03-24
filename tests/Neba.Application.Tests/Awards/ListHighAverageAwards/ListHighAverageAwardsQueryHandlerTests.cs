using Neba.Application.Awards;
using Neba.Application.Awards.ListHighAverageAwards;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Awards;

namespace Neba.Application.Tests.Awards.ListHighAverageAwards;

[UnitTest]
[Component("Awards")]
public sealed class ListHighAverageAwardsQueryHandlerTests
{
    private readonly Mock<IAwardQueries> _awardQueriesMock;

    private readonly ListHighAverageAwardsQueryHandler _queryHandler;

    public ListHighAverageAwardsQueryHandlerTests()
    {
        _awardQueriesMock = new Mock<IAwardQueries>(MockBehavior.Strict);

        _queryHandler = new ListHighAverageAwardsQueryHandler(_awardQueriesMock.Object);
    }

    [Fact(DisplayName = "HandleAsync should return expected awards")]
    public async Task HandleAsync_ShouldReturnExpectedAwards()
    {
        // Arrange
        var expectedAwards = HighAverageAwardDtoFactory.Bogus(3);
        _awardQueriesMock.Setup(x => x.GetAllHighAverageAwardsAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(expectedAwards);

        var query = new ListHighAverageAwardsQuery();

        // Act
        var result = await _queryHandler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(expectedAwards);
    }
}
