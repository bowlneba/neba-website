using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Series.ListStageResults;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.ListStageResults;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.ListStageResults;

[UnitTest]
[Component("Series")]
public sealed class ListStageResultsEndpointTests
{
    private const string ValidSeriesId = "01000000000000000000000001";
    private const string ValidStageId = "01000000000000000000000002";

    private static ListStageResultsRequest Request()
        => new() { SeriesId = ValidSeriesId, StageId = ValidStageId };

    private static Mock<IQueryHandler<ListStageResultsQuery, ErrorOr<IReadOnlyList<StageResultSummaryDto>>>> Handler()
        => new(MockBehavior.Strict);

    private static ErrorOr<IReadOnlyList<StageResultSummaryDto>> Ok(IReadOnlyList<StageResultSummaryDto> items)
        => items.ToList();

    [Fact(DisplayName = "Configure should register a GET route at /series/{id}/stages/{stageId}/results that needs the Series.ManageAdvancement permission")]
    public void Configure_ShouldRegisterPermissionProtectedGetRoute()
    {
        // Arrange & Act
        var endpoint = Factory.Create<ListStageResultsEndpoint>(Handler().Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain("/series/{id}/stages/{stageId}/results");
        endpoint.Definition.PreBuiltUserPolicies!.ShouldContain("Permission:Series.ManageAdvancement");
        endpoint.Definition.PreBuiltUserPolicies!.Count.ShouldBe(1);
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should return OK with the result rows mapped")]
    public async Task HandleAsync_ShouldReturnOkWithMappedRows()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        IReadOnlyList<StageResultSummaryDto> rows = StageResultSummaryDtoFactory.Bogus(4, 102);
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<ListStageResultsQuery>(), ct)).ReturnsAsync(Ok(rows));
        var endpoint = Factory.Create<ListStageResultsEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(Request(), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "HandleAsync should map each row's ids, name and place, with the bowler id as a ULID string")]
    public async Task HandleAsync_ShouldMapEveryField()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var first = StageResultSummaryDtoFactory.Create(bowlerName: "First Bowler", place: 2);
        var second = StageResultSummaryDtoFactory.Create(bowlerName: "Second Bowler", place: 5);
        ListStageResultsQuery? captured = null;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<ListStageResultsQuery>(), ct))
            .Callback<ListStageResultsQuery, CancellationToken>((q, _) => captured = q)
            .ReturnsAsync(Ok([first, second]));
        var endpoint = Factory.Create<ListStageResultsEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(Request(), ct);

        // Assert
        captured.ShouldNotBeNull();
        captured.SeriesId.ShouldBe(new SeriesId(ValidSeriesId));
        captured.StageId.ShouldBe(new SeriesStageId(ValidStageId));
        var items = endpoint.Response.Items.ToList();
        items.Select(i => i.BowlerId).ShouldBe([first.BowlerId.Value.ToString(), second.BowlerId.Value.ToString()]);
        items.Select(i => i.BowlerName).ShouldBe(["First Bowler", "Second Bowler"]);
        items.Select(i => i.Place).ShouldBe([2, 5]);
    }

    [Fact(DisplayName = "HandleAsync should return OK with an empty collection when the tournament has no results")]
    public async Task HandleAsync_ShouldReturnOkWithEmptyCollection_WhenThereAreNoResults()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<ListStageResultsQuery>(), ct))
            .ReturnsAsync(Ok([]));
        var endpoint = Factory.Create<ListStageResultsEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(Request(), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Items.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should return 404 when the Series or the Stage does not exist")]
    public async Task HandleAsync_ShouldReturn404_WhenStageIsNotFound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<ListStageResultsQuery>(), ct))
            .ReturnsAsync(SeriesAdvancementErrors.StageNotFound(new SeriesStageId(ValidStageId)));
        var endpoint = Factory.Create<ListStageResultsEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(Request(), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }
}