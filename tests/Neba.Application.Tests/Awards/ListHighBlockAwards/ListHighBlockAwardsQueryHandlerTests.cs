using Neba.Application.Awards;
using Neba.Application.Awards.ListHighBlockAwards;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Awards;

namespace Neba.Application.Tests.Awards.ListHighBlockAwards;

[UnitTest]
[Component("Awards")]
public sealed class ListHighBlockAwardsQueryHandlerTests
{
    private readonly Mock<IAwardQueries> _awardQueriesMock;

    private readonly ListHighBlockAwardsQueryHandler _queryHandler;

    public ListHighBlockAwardsQueryHandlerTests()
    {
        _awardQueriesMock = new Mock<IAwardQueries>(MockBehavior.Strict);

        _queryHandler = new ListHighBlockAwardsQueryHandler(_awardQueriesMock.Object);
    }

    [Fact(DisplayName = "HandleAsync should return expected awards")]
    public async Task HandleAsync_ShouldReturnExpectedAwards()
    {
        // Arrange
        var expectedAwards = HighBlockAwardDtoFactory.Bogus(3);
        _awardQueriesMock.Setup(x => x.GetAllHighBlockAwardsAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(expectedAwards);

        var query = new ListHighBlockAwardsQuery();

        // Act
        var result = await _queryHandler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(expectedAwards);
    }
}