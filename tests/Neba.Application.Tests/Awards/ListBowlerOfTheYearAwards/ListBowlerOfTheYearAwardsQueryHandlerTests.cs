using Neba.Application.Awards;
using Neba.Application.Awards.ListBowlerOfTheYearAwards;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Awards;

namespace Neba.Application.Tests.Awards.ListBowlerOfTheYearAwards;

[UnitTest]
[Component("Awards")]
public sealed class ListBowlerOfTheYearAwardsQueryHandlerTests
{
    private readonly Mock<IAwardQueries> _awardQueriesMock;

    private readonly ListBowlerOfTheYearAwardsQueryHandler _queryHandler;

    public ListBowlerOfTheYearAwardsQueryHandlerTests()
    {
        _awardQueriesMock = new Mock<IAwardQueries>(MockBehavior.Strict);

        _queryHandler = new ListBowlerOfTheYearAwardsQueryHandler(_awardQueriesMock.Object);
    }

    [Fact(DisplayName = "HandleAsync should return expected awards")]
    public async Task HandleAsync_ShouldReturnExpectedAwards()
    {
        // Arrange
        var expectedAwards = BowlerOfTheYearAwardDtoFactory.Bogus(3);
        _awardQueriesMock.Setup(x => x.GetAllBowlerOfTheYearAwardsAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(expectedAwards);

        var query = new ListBowlerOfTheYearAwardsQuery();

        // Act
        var result = await _queryHandler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(expectedAwards);
    }
}