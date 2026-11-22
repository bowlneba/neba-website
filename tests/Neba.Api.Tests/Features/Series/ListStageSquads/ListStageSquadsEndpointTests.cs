using ErrorOr;

using FastEndpoints;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Series.ListStageSquads;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.ListStageSquads;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.ListStageSquads;

[UnitTest]
[Component("Series")]
public sealed class ListStageSquadsEndpointTests
{
    private const string ValidSeriesId = "01000000000000000000000001";
    private const string ValidStageId = "01000000000000000000000002";

    private static ListStageSquadsRequest Request()
        => new() { SeriesId = ValidSeriesId, StageId = ValidStageId };

    private static Mock<IQueryHandler<ListStageSquadsQuery, ErrorOr<IReadOnlyList<StageSquadSummaryDto>>>> Handler()
        => new(MockBehavior.Strict);

    private static ErrorOr<IReadOnlyList<StageSquadSummaryDto>> Ok(IReadOnlyList<StageSquadSummaryDto> items)
        => items.ToList();

    [Fact(DisplayName = "Configure should register a GET route at /series/{id}/stages/{stageId}/squads that needs the Series.EditSeries permission")]
    public void Configure_ShouldRegisterPermissionProtectedGetRoute()
    {
        // Arrange & Act
        var endpoint = Factory.Create<ListStageSquadsEndpoint>(Handler().Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain("/series/{id}/stages/{stageId}/squads");
        endpoint.Definition.PreBuiltUserPolicies!.ShouldContain("Permission:Series.EditSeries");
        endpoint.Definition.PreBuiltUserPolicies!.Count.ShouldBe(1);
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should return OK with the squads mapped")]
    public async Task HandleAsync_ShouldReturnOkWithMappedSquads()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        IReadOnlyList<StageSquadSummaryDto> squads = StageSquadSummaryDtoFactory.Bogus(3, 103);
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<ListStageSquadsQuery>(), ct)).ReturnsAsync(Ok(squads));
        var endpoint = Factory.Create<ListStageSquadsEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(Request(), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "HandleAsync should map each squad's fields, with the squad id as a ULID string and the local date and start time as Date and StartTime")]
    public async Task HandleAsync_ShouldMapEveryField()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var first = StageSquadSummaryDtoFactory.Create(
            localDate: new DateOnly(2025, 10, 4), localStartTime: new TimeOnly(22, 30), hasScores: true, isSelected: false);
        var second = StageSquadSummaryDtoFactory.Create(
            localDate: new DateOnly(2025, 10, 5), localStartTime: new TimeOnly(9, 15), hasScores: false, isSelected: true);
        ListStageSquadsQuery? captured = null;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<ListStageSquadsQuery>(), ct))
            .Callback<ListStageSquadsQuery, CancellationToken>((q, _) => captured = q)
            .ReturnsAsync(Ok([first, second]));
        var endpoint = Factory.Create<ListStageSquadsEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(Request(), ct);

        // Assert
        captured.ShouldNotBeNull();
        captured.SeriesId.ShouldBe(new SeriesId(ValidSeriesId));
        captured.StageId.ShouldBe(new SeriesStageId(ValidStageId));
        var items = endpoint.Response.Items.ToList();
        items.Select(i => i.SquadId).ShouldBe([first.SquadId.Value.ToString(), second.SquadId.Value.ToString()]);
        items.Select(i => i.Date).ShouldBe([new DateOnly(2025, 10, 4), new DateOnly(2025, 10, 5)]);
        items.Select(i => i.StartTime).ShouldBe([new TimeOnly(22, 30), new TimeOnly(9, 15)]);
        items.Select(i => i.HasScores).ShouldBe([true, false]);
        items.Select(i => i.IsSelected).ShouldBe([false, true]);
    }

    [Fact(DisplayName = "HandleAsync should return OK with an empty collection when the tournament has no squads")]
    public async Task HandleAsync_ShouldReturnOkWithEmptyCollection_WhenThereAreNoSquads()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<ListStageSquadsQuery>(), ct))
            .ReturnsAsync(Ok([]));
        var endpoint = Factory.Create<ListStageSquadsEndpoint>(handler.Object);

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
        handler.Setup(h => h.HandleAsync(It.IsAny<ListStageSquadsQuery>(), ct))
            .ReturnsAsync(SeriesAdvancementErrors.StageNotFound(new SeriesStageId(ValidStageId)));
        var endpoint = Factory.Create<ListStageSquadsEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(Request(), ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }
}