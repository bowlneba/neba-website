using Neba.Application.Seasons;
using Neba.Application.Seasons.ListSeasons;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Seasons;

namespace Neba.Application.Tests.Seasons.ListSeasons;

[UnitTest]
[Component("Seasons")]
public sealed class ListSeasonsQueryHandlerTests
{
    private readonly Mock<ISeasonQueries> _seasonQueriesMock;
    private readonly ListSeasonsQueryHandler _handler;

    public ListSeasonsQueryHandlerTests()
    {
        _seasonQueriesMock = new Mock<ISeasonQueries>(MockBehavior.Strict);

        _handler = new ListSeasonsQueryHandler(_seasonQueriesMock.Object);
    }

    [Fact(DisplayName = "Should return all seasons returned by queries")]
    public async Task HandleAsync_ShouldReturnAllSeasons()
    {
        // Arrange
        var seasons = SeasonDtoFactory.Bogus(5);
        var query = new ListSeasonsQuery();

        _seasonQueriesMock
            .Setup(q => q.GetAllAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(seasons);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(seasons);
    }

    [Fact(DisplayName = "Should return 5 seasons when queries returns 5")]
    public async Task HandleAsync_ShouldReturn5Seasons_WhenQueriesReturns5()
    {
        // Arrange
        var seasons = SeasonDtoFactory.Bogus(5);
        var query = new ListSeasonsQuery();

        _seasonQueriesMock
            .Setup(q => q.GetAllAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(seasons);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(5);
    }

    [Fact(DisplayName = "Should return empty collection when queries returns no seasons")]
    public async Task HandleAsync_ShouldReturnEmptyCollection_WhenQueriesReturnsNone()
    {
        // Arrange
        IReadOnlyCollection<SeasonDto> empty = [];
        var query = new ListSeasonsQuery();

        _seasonQueriesMock
            .Setup(q => q.GetAllAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(empty);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }
}