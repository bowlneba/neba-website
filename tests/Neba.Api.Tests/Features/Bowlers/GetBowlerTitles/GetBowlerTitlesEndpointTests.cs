using ErrorOr;

using FastEndpoints;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Bowlers.GetBowlerTitles;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;

namespace Neba.Api.Tests.Features.Bowlers.GetBowlerTitles;

[UnitTest]
[Component("Bowlers")]
public sealed class GetBowlerTitlesEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with mapped bowler titles when query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedBowlerTitles_WhenQuerySucceeds()
    {
        // Arrange
        var dto = BowlerTitlesDtoFactory.Bogus(count: 1, seed: 101).Single();
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<GetBowlerTitlesQuery, ErrorOr<BowlerTitlesDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetBowlerTitlesQuery>(), cancellationToken))
            .ReturnsAsync(dto);

        var endpoint = Factory.Create<GetBowlerTitlesEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new GetBowlerTitlesRequest { BowlerId = BowlerId.New().Value.ToString() }, cancellationToken);

        // Assert
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "HandleAsync should return 404 Not Found when bowler does not exist")]
    public async Task HandleAsync_ShouldReturn404_WhenBowlerDoesNotExist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<GetBowlerTitlesQuery, ErrorOr<BowlerTitlesDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetBowlerTitlesQuery>(), cancellationToken))
            .ReturnsAsync(Error.NotFound());

        var endpoint = Factory.Create<GetBowlerTitlesEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new GetBowlerTitlesRequest { BowlerId = BowlerId.New().Value.ToString() }, cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact(DisplayName = "HandleAsync should return 500 and add all error messages when query returns non-NotFound errors")]
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

        var queryHandlerMock = new Mock<IQueryHandler<GetBowlerTitlesQuery, ErrorOr<BowlerTitlesDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetBowlerTitlesQuery>(), cancellationToken))
            .ReturnsAsync(errors);

        var endpoint = Factory.Create<GetBowlerTitlesEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new GetBowlerTitlesRequest { BowlerId = BowlerId.New().Value.ToString() }, cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(500);
    }

    [Fact(DisplayName = "Configure should register anonymous GET route under /bowlers")]
    public void Configure_ShouldRegisterAnonymousGetRoute_UnderBowlersPath()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<GetBowlerTitlesQuery, ErrorOr<BowlerTitlesDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<GetBowlerTitlesEndpoint>(queryHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("bowlers"), "should be under the /bowlers path");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }
}