using ErrorOr;

using FastEndpoints;

using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.ListEligibleBowlers;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.ListEligibleBowlers;

[UnitTest]
[Component("Tournaments")]
public sealed class ListEligibleBowlersEndpointTests
{
    private static Mock<IQueryHandler<ListEligibleBowlersQuery, ErrorOr<EligibleBowlersDto>>> HandlerReturning(
        ErrorOr<EligibleBowlersDto> result,
        CancellationToken ct,
        Action<ListEligibleBowlersQuery>? capture = null)
    {
        var mock = new Mock<IQueryHandler<ListEligibleBowlersQuery, ErrorOr<EligibleBowlersDto>>>(MockBehavior.Strict);
        mock.Setup(handler => handler.HandleAsync(It.IsAny<ListEligibleBowlersQuery>(), ct))
            .Callback<ListEligibleBowlersQuery, CancellationToken>((query, _) => capture?.Invoke(query))
            .ReturnsAsync(result);
        return mock;
    }

    [Fact(DisplayName = "HandleAsync should return OK with the mapped bowlers when the query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedBowlers_WhenQuerySucceeds()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var dto = EligibleBowlersDtoFactory.Create(bowlers: EligibleBowlerSummaryDtoFactory.Bogus(3, 2201));
        var endpoint = Factory.Create<ListEligibleBowlersEndpoint>(HandlerReturning(dto, ct).Object);

        // Act
        await endpoint.HandleAsync(new ListEligibleBowlersRequest { TournamentId = TournamentId.New().Value.ToString() }, ct);

        // Assert
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "HandleAsync should pass the route tournament ID to the query")]
    public async Task HandleAsync_ShouldBuildQueryFromRoute()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournamentId = TournamentId.New();
        ListEligibleBowlersQuery? captured = null;
        var handler = HandlerReturning(
            EligibleBowlersDtoFactory.Create(bowlers: EligibleBowlerSummaryDtoFactory.Bogus(1, 2202)), ct, query => captured = query);
        var endpoint = Factory.Create<ListEligibleBowlersEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(new ListEligibleBowlersRequest { TournamentId = tournamentId.Value.ToString() }, ct);

        // Assert
        captured.ShouldNotBeNull();
        captured.Id.ShouldBe(tournamentId);
    }

    [Fact(DisplayName = "HandleAsync should map a bowler without a USBC ID to a null USBC ID")]
    public async Task HandleAsync_ShouldMapNullUsbcId()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var dto = EligibleBowlersDtoFactory.Create(
            bowlers: [EligibleBowlerSummaryDtoFactory.Create(name: "Pat O'Brien", usbcId: null)]);
        var endpoint = Factory.Create<ListEligibleBowlersEndpoint>(HandlerReturning(dto, ct).Object);

        // Act
        await endpoint.HandleAsync(new ListEligibleBowlersRequest { TournamentId = TournamentId.New().Value.ToString() }, ct);

        // Assert
        var item = endpoint.Response.Bowlers.ShouldHaveSingleItem();
        item.Name.ShouldBe("Pat O'Brien");
        item.UsbcId.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should return 200 with null requirements when the tournament has no list")]
    public async Task HandleAsync_ShouldReturnNullRequirements_WhenListDoesNotApply()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var endpoint = Factory.Create<ListEligibleBowlersEndpoint>(
            HandlerReturning(EligibleBowlersDtoFactory.CreateNotApplicable(), ct).Object);

        // Act
        await endpoint.HandleAsync(new ListEligibleBowlersRequest { TournamentId = TournamentId.New().Value.ToString() }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Requirements.ShouldBeNull();
        endpoint.Response.Bowlers.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should return 404 when the tournament does not exist")]
    public async Task HandleAsync_ShouldReturn404_WhenTournamentDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var endpoint = Factory.Create<ListEligibleBowlersEndpoint>(HandlerReturning(Error.NotFound(), ct).Object);

        // Act
        await endpoint.HandleAsync(new ListEligibleBowlersRequest { TournamentId = TournamentId.New().Value.ToString() }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact(DisplayName = "HandleAsync should return 500 when the query returns a non-NotFound error")]
    public async Task HandleAsync_ShouldReturn500_WhenQueryReturnsOtherError()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var endpoint = Factory.Create<ListEligibleBowlersEndpoint>(
            HandlerReturning(Error.Unexpected(description: "An unexpected error occurred."), ct).Object);

        // Act
        await endpoint.HandleAsync(new ListEligibleBowlersRequest { TournamentId = TournamentId.New().Value.ToString() }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(500);
    }

    [Fact(DisplayName = "Configure should register an anonymous GET route ending in eligible-bowlers")]
    public void Configure_ShouldRegisterAnonymousGetRoute()
    {
        // Arrange
        var handler = new Mock<IQueryHandler<ListEligibleBowlersQuery, ErrorOr<EligibleBowlersDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<ListEligibleBowlersEndpoint>(handler.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain(route => route.EndsWith("/eligible-bowlers", StringComparison.Ordinal));
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }
}
