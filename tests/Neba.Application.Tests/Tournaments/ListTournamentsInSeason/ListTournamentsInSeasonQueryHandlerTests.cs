using Neba.Application.Messaging;
using Neba.Application.Tournaments;
using Neba.Application.Tournaments.ListTournamentsInSeason;
using Neba.Domain.Seasons;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

namespace Neba.Application.Tests.Tournaments.ListTournamentsInSeason;

[UnitTest]
[Component("Tournaments")]
public sealed class ListTournamentsInSeasonQueryHandlerTests
{
    private readonly Mock<ITournamentQueries> _tournamentQueriesMock;
    private readonly ListTournamentsInSeasonQueryHandler _handler;

    public ListTournamentsInSeasonQueryHandlerTests()
    {
        _tournamentQueriesMock = new Mock<ITournamentQueries>(MockBehavior.Strict);

        _handler = new ListTournamentsInSeasonQueryHandler(_tournamentQueriesMock.Object);
    }

    [Fact(DisplayName = "Should return all tournaments returned by queries")]
    public async Task HandleAsync_ShouldReturnAllTournaments()
    {
        // Arrange
        var seasonId = SeasonId.New();
        var tournaments = SeasonTournamentDtoFactory.Bogus(3);
        var query = new ListTournamentsInSeasonQuery { SeasonId = seasonId };

        _tournamentQueriesMock
            .Setup(q => q.GetTournamentsInSeasonAsync(seasonId, TestContext.Current.CancellationToken))
            .ReturnsAsync(tournaments);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(tournaments);
    }

    [Fact(DisplayName = "Should return 3 tournaments when queries returns 3")]
    public async Task HandleAsync_ShouldReturn3Tournaments_WhenQueriesReturns3()
    {
        // Arrange
        var seasonId = SeasonId.New();
        var tournaments = SeasonTournamentDtoFactory.Bogus(3);
        var query = new ListTournamentsInSeasonQuery { SeasonId = seasonId };

        _tournamentQueriesMock
            .Setup(q => q.GetTournamentsInSeasonAsync(seasonId, TestContext.Current.CancellationToken))
            .ReturnsAsync(tournaments);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(3);
    }

    [Fact(DisplayName = "Should return empty collection when queries returns no tournaments")]
    public async Task HandleAsync_ShouldReturnEmptyCollection_WhenQueriesReturnsNone()
    {
        // Arrange
        var seasonId = SeasonId.New();
        IReadOnlyCollection<SeasonTournamentDto> empty = [];
        var query = new ListTournamentsInSeasonQuery { SeasonId = seasonId };

        _tournamentQueriesMock
            .Setup(q => q.GetTournamentsInSeasonAsync(seasonId, TestContext.Current.CancellationToken))
            .ReturnsAsync(empty);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }
}
