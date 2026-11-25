using ErrorOr;

using FastEndpoints;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.GetSeriesAdvancers;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.GetSeriesAdvancers;

[UnitTest]
[Component("Series")]
public sealed class GetSeriesAdvancersEndpointTests
{
    private const string ValidId = "01000000000000000000000001";

    private static Mock<IQueryHandler<GetSeriesAdvancersQuery, ErrorOr<SeriesAdvancersDto>>> Handler()
        => new(MockBehavior.Strict);

    [Fact(DisplayName = "Configure should register an anonymous GET route at /series/{id}/advancers")]
    public void Configure_ShouldRegisterAnonymousGetRoute()
    {
        // Arrange

        // Act
        var endpoint = Factory.Create<GetSeriesAdvancersEndpoint>(Handler().Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain("/series/{id}/advancers");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeNull();
        endpoint.Definition.AnonymousVerbs.ShouldContain("GET");
    }

    [Fact(DisplayName = "HandleAsync should map every DTO field to the response, with ids as ULID strings")]
    public async Task HandleAsync_ShouldMapEveryField()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var stageId = SeriesStageId.New();
        var bowlerId = BowlerId.New();
        var backfillId = BowlerId.New();
        var dto = new SeriesAdvancersDto(
        [
            new StageAdvancersDto(stageId, 2, "Qualifying",
            [
                new AdvancerDto(bowlerId, "Dave Smith", 3, 1234, false, false, 1),
                new AdvancerDto(backfillId, "Pat Plain", null, null, true, true, null),
            ]),
        ]);
        GetSeriesAdvancersQuery? captured = null;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<GetSeriesAdvancersQuery>(), ct))
            .Callback<GetSeriesAdvancersQuery, CancellationToken>((q, _) => captured = q)
            .ReturnsAsync(dto);
        var endpoint = Factory.Create<GetSeriesAdvancersEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(new GetSeriesAdvancersRequest { SeriesId = ValidId }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        captured.ShouldNotBeNull();
        captured.SeriesId.ShouldBe(new SeriesId(ValidId));

        var stage = endpoint.Response.Stages.Single();
        stage.StageId.ShouldBe(stageId.Value.ToString());
        stage.Order.ShouldBe(2);
        stage.Kind.ShouldBe("Qualifying");
        stage.Advancers.Count.ShouldBe(2);
        stage.Advancers[0].BowlerId.ShouldBe(bowlerId.Value.ToString());
        stage.Advancers[0].BowlerName.ShouldBe("Dave Smith");
        stage.Advancers[0].Place.ShouldBe(3);
        stage.Advancers[0].BlockTotal.ShouldBe(1234);
        stage.Advancers[0].IsBackfill.ShouldBeFalse();
        stage.Advancers[1].Place.ShouldBeNull();
        stage.Advancers[1].BlockTotal.ShouldBeNull();
        stage.Advancers[0].Advanced.ShouldBeFalse();
        stage.Advancers[0].AlreadyAdvancedFromOrder.ShouldBe(1);
        stage.Advancers[1].IsBackfill.ShouldBeTrue();
        stage.Advancers[1].Advanced.ShouldBeTrue();
        stage.Advancers[1].AlreadyAdvancedFromOrder.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should return the public response shape for a Qualifying, Survivor and Placement Stage")]
    public async Task HandleAsync_ShouldReturnOkWithMappedAdvancers()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var dto = new SeriesAdvancersDto(
        [
            new StageAdvancersDto(new SeriesStageId("01000000000000000000000011"), 1, "Qualifying",
            [
                new AdvancerDto(new BowlerId("01000000000000000000000021"), "Dave Smith", 1, null, false, true, null),
                new AdvancerDto(new BowlerId("01000000000000000000000025"), "Ann Early", 3, null, false, false, 1),
                new AdvancerDto(new BowlerId("01000000000000000000000022"), "Pat Plain", null, null, true, true, null) { BackfillPoints = 80 },
            ]),
            new StageAdvancersDto(new SeriesStageId("01000000000000000000000012"), 2, "Survivor",
            [
                new AdvancerDto(new BowlerId("01000000000000000000000023"), "Kyle Wilks", null, 1500, false, true, null),
            ]),
            new StageAdvancersDto(new SeriesStageId("01000000000000000000000013"), 3, "Placement",
            [
                new AdvancerDto(new BowlerId("01000000000000000000000024"), "Lee Park", 2, null, false, true, null)
                {
                    Position = 1,
                    Bowled = true,
                    IsFinalist = true,
                },
            ]),
        ]);
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<GetSeriesAdvancersQuery>(), ct)).ReturnsAsync(dto);
        var endpoint = Factory.Create<GetSeriesAdvancersEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(new GetSeriesAdvancersRequest { SeriesId = ValidId }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "HandleAsync should return an empty Stage list when no Stage is settled")]
    public async Task HandleAsync_ShouldReturnEmptyStages_WhenNoneSettled()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<GetSeriesAdvancersQuery>(), ct)).ReturnsAsync(new SeriesAdvancersDto([]));
        var endpoint = Factory.Create<GetSeriesAdvancersEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(new GetSeriesAdvancersRequest { SeriesId = ValidId }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Stages.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should return 404 when the Series does not exist")]
    public async Task HandleAsync_ShouldReturn404_WhenSeriesDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<GetSeriesAdvancersQuery>(), ct)).ReturnsAsync(SeriesErrors.NotFound(new SeriesId(ValidId)));
        var endpoint = Factory.Create<GetSeriesAdvancersEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(new GetSeriesAdvancersRequest { SeriesId = ValidId }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }
}