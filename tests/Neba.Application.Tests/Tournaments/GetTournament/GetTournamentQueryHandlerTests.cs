using ErrorOr;

using Neba.Application.Tournaments;
using Neba.Application.Tournaments.GetTournament;
using Neba.Domain.Tournaments;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Application.Tests.Tournaments.GetTournament;

[UnitTest]
[Component("Tournaments")]
public sealed class GetTournamentQueryHandlerTests
{
    private readonly Mock<ITournamentQueries> _tournamentQueriesMock;
    private readonly GetTournamentQueryHandler _handler;

    public GetTournamentQueryHandlerTests()
    {
        _tournamentQueriesMock = new Mock<ITournamentQueries>(MockBehavior.Strict);
        _handler = new GetTournamentQueryHandler(_tournamentQueriesMock.Object);
    }

    [Fact(DisplayName = "HandleAsync should return tournament detail when tournament exists")]
    public async Task HandleAsync_ShouldReturnTournamentDetail_WhenTournamentExists()
    {
        // Arrange
        var dto = TournamentDetailDtoFactory.Create();
        var query = new GetTournamentQuery { Id = dto.Id };

        _tournamentQueriesMock
            .Setup(q => q.GetTournamentDetailAsync(query.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(dto);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(dto);
    }

    [Fact(DisplayName = "HandleAsync should return not found error when tournament does not exist")]
    public async Task HandleAsync_ShouldReturnNotFoundError_WhenTournamentDoesNotExist()
    {
        // Arrange
        var id = TournamentId.New();
        var query = new GetTournamentQuery { Id = id };

        _tournamentQueriesMock
            .Setup(q => q.GetTournamentDetailAsync(query.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync((TournamentDetailDto?)null);

        // Act
        var result = await _handler.HandleAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.NotFound");
        result.FirstError.Type.ShouldBe(ErrorType.NotFound);
        var metadata = result.FirstError.Metadata;
        metadata.ShouldNotBeNull();
        metadata.ShouldContainKey("TournamentId");
        metadata["TournamentId"]?.ShouldBe(id.ToString());
    }

    [Fact(DisplayName = "HandleAsync should pass cancellation token to tournament queries")]
    public async Task HandleAsync_ShouldPassCancellationToken_ToTournamentQueries()
    {
        // Arrange
        var dto = TournamentDetailDtoFactory.Create();
        var query = new GetTournamentQuery { Id = dto.Id };
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _tournamentQueriesMock
            .Setup(q => q.GetTournamentDetailAsync(query.Id, cancellationToken))
            .ReturnsAsync(dto);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(dto);
    }
}
