using ErrorOr;

using FastEndpoints;

using Neba.Api.Features.Tournaments.GetTournament;
using Neba.Api.Messaging;
using Neba.Application.Tournaments.GetTournament;
using Neba.Domain.Tournaments;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.GetTournament;

[UnitTest]
[Component("Tournaments")]
public sealed class GetTournamentEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with mapped tournament detail when query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedTournamentDetail_WhenQuerySucceeds()
    {
        // Arrange
        var dto = TournamentDetailDtoFactory.Bogus(count: 1, seed: 42).Single();
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<GetTournamentQuery, ErrorOr<TournamentDetailDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetTournamentQuery>(), cancellationToken))
            .ReturnsAsync(dto);

        var endpoint = Factory.Create<GetTournamentEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(
            new GetTournamentRequest { TournamentId = dto.Id.Value.ToString() },
            cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Id.ShouldBe(dto.Id.Value.ToString());
        endpoint.Response.Name.ShouldBe(dto.Name);
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "HandleAsync should return 404 Not Found when tournament does not exist")]
    public async Task HandleAsync_ShouldReturn404_WhenTournamentDoesNotExist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<GetTournamentQuery, ErrorOr<TournamentDetailDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetTournamentQuery>(), cancellationToken))
            .ReturnsAsync(Error.NotFound());

        var endpoint = Factory.Create<GetTournamentEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(
            new GetTournamentRequest { TournamentId = TournamentId.New().Value.ToString() },
            cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact(DisplayName = "HandleAsync should return 500 and add all error messages to validation failures when query returns non-NotFound errors")]
    public async Task HandleAsync_ShouldReturn500WithAllErrors_WhenQueryReturnsNonNotFoundErrors()
    {
        // Arrange — two errors to exercise the foreach loop body
        const string firstErrorMessage = "An unexpected error occurred.";
        const string secondErrorMessage = "A secondary failure occurred.";
        var cancellationToken = TestContext.Current.CancellationToken;

        List<Error> errors =
        [
            Error.Unexpected(description: firstErrorMessage),
            Error.Failure(description: secondErrorMessage),
        ];

        var queryHandlerMock = new Mock<IQueryHandler<GetTournamentQuery, ErrorOr<TournamentDetailDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetTournamentQuery>(), cancellationToken))
            .ReturnsAsync(errors);

        var endpoint = Factory.Create<GetTournamentEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(
            new GetTournamentRequest { TournamentId = TournamentId.New().Value.ToString() },
            cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(500);
        endpoint.ValidationFailures.ShouldContain(f => f.ErrorMessage == firstErrorMessage);
        endpoint.ValidationFailures.ShouldContain(f => f.ErrorMessage == secondErrorMessage);
    }

    [Fact(DisplayName = "Configure should register anonymous GET route under /tournaments")]
    public void Configure_ShouldRegisterAnonymousGetRoute_UnderTournamentsPath()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<GetTournamentQuery, ErrorOr<TournamentDetailDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<GetTournamentEndpoint>(queryHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("tournaments"), "should be under the /tournaments path");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }
}