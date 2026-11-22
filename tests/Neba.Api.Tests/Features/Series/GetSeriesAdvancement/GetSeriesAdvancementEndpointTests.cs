using ErrorOr;

using FastEndpoints;

using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.GetSeriesAdvancement;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.GetSeriesAdvancement;

[UnitTest]
[Component("Series")]
public sealed class GetSeriesAdvancementEndpointTests
{
    private const string ValidId = "01000000000000000000000001";

    private static Mock<IQueryHandler<GetSeriesAdvancementQuery, ErrorOr<SeriesAdvancementDto>>> Handler()
        => new(MockBehavior.Strict);

    [Fact(DisplayName = "Configure should register a GET route at /series/{id}/advancement that needs the Series.EditSeries permission")]
    public void Configure_ShouldRegisterPermissionProtectedGetRoute()
    {
        // Arrange & Act
        var endpoint = Factory.Create<GetSeriesAdvancementEndpoint>(Handler().Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain("/series/{id}/advancement");
        endpoint.Definition.PreBuiltUserPolicies!.ShouldContain("Permission:Series.EditSeries");
        endpoint.Definition.PreBuiltUserPolicies!.Count.ShouldBe(1);
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should return OK with the advancement mapped")]
    public async Task HandleAsync_ShouldReturnOkWithMappedAdvancement()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var dto = SeriesAdvancementDtoFactory.Bogus(1, 101).Single();
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<GetSeriesAdvancementQuery>(), ct)).ReturnsAsync(dto);
        var endpoint = Factory.Create<GetSeriesAdvancementEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(new GetSeriesAdvancementRequest { SeriesId = ValidId }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "HandleAsync should send the Series id from the route in the query")]
    public async Task HandleAsync_ShouldMapSeriesIdToQuery()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        GetSeriesAdvancementQuery? captured = null;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<GetSeriesAdvancementQuery>(), ct))
            .Callback<GetSeriesAdvancementQuery, CancellationToken>((q, _) => captured = q)
            .ReturnsAsync(SeriesAdvancementDtoFactory.Create());
        var endpoint = Factory.Create<GetSeriesAdvancementEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(new GetSeriesAdvancementRequest { SeriesId = ValidId }, ct);

        // Assert
        captured.ShouldNotBeNull();
        captured.SeriesId.ShouldBe(new SeriesId(ValidId));
    }

    [Fact(DisplayName = "HandleAsync should map every DTO field to the response, with ids as ULID strings")]
    public async Task HandleAsync_ShouldMapEveryField()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var stageId = SeriesStageId.New();
        var entry = FieldEntryDtoFactory.Create(bowlerName: "Entry Name", sourceStageId: stageId, place: 3, backfillPoints: 12, viaOverride: true);
        var backfill = FieldEntryDtoFactory.Create(bowlerName: "Backfill Name");
        var stageOverride = StageOverrideDtoFactory.Create(
            bowlerName: "Override Name", kind: "Clear", reason: "Moved.", createdBy: "admin-1",
            createdAt: new DateTimeOffset(2025, 4, 1, 8, 0, 0, TimeSpan.Zero), applied: false);
        var outcome = SurvivorOutcomeDtoFactory.Create(bowlerName: "Outcome Name", blockTotal: 1234, advanced: false);
        var position = SeriesPositionDtoFactory.Create(bowlerName: "Position Name", position: 4, bowled: false, isFinalist: true);
        var squadId = SquadId.New();
        var stage = StageAdvancementDtoFactory.Create(
            stageId: stageId, order: 2, kind: "Survivor", status: "Official", isStale: true, needsSquad: true,
            computedAt: new DateTimeOffset(2025, 3, 1, 12, 0, 0, TimeSpan.Zero),
            officialDueAt: new DateTimeOffset(2025, 3, 2, 12, 0, 0, TimeSpan.Zero),
            officialAt: new DateTimeOffset(2025, 3, 3, 12, 0, 0, TimeSpan.Zero),
            slotCount: 9, vacantSlotCount: 2, tournamentStatus: "Truncated", resolvedSquadId: squadId,
            entries: [entry, backfill], overrides: [stageOverride], survivorOutcomes: [outcome]);
        var dto = SeriesAdvancementDtoFactory.Create(
            version: 7, backfillAsOf: new DateOnly(2025, 6, 28), targetField: 11, stages: [stage], positions: [position]);
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<GetSeriesAdvancementQuery>(), ct)).ReturnsAsync(dto);
        var endpoint = Factory.Create<GetSeriesAdvancementEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(new GetSeriesAdvancementRequest { SeriesId = ValidId }, ct);

        // Assert
        var response = endpoint.Response;
        response.Version.ShouldBe(7);
        response.BackfillAsOf.ShouldBe(new DateOnly(2025, 6, 28));
        response.TargetField.ShouldBe(11);

        var mappedStage = response.Stages.Single();
        mappedStage.StageId.ShouldBe(stageId.Value.ToString());
        mappedStage.Order.ShouldBe(2);
        mappedStage.Kind.ShouldBe("Survivor");
        mappedStage.Status.ShouldBe("Official");
        mappedStage.IsStale.ShouldBeTrue();
        mappedStage.NeedsSquad.ShouldBeTrue();
        mappedStage.ComputedAt.ShouldBe(new DateTimeOffset(2025, 3, 1, 12, 0, 0, TimeSpan.Zero));
        mappedStage.OfficialDueAt.ShouldBe(new DateTimeOffset(2025, 3, 2, 12, 0, 0, TimeSpan.Zero));
        mappedStage.OfficialAt.ShouldBe(new DateTimeOffset(2025, 3, 3, 12, 0, 0, TimeSpan.Zero));
        mappedStage.SlotCount.ShouldBe(9);
        mappedStage.VacantSlotCount.ShouldBe(2);
        mappedStage.TournamentId.ShouldBe(stage.TournamentId.Value.ToString());
        mappedStage.TournamentStatus.ShouldBe("Truncated");
        mappedStage.ResolvedSquadId.ShouldBe(squadId.Value.ToString());

        mappedStage.Entries.Count.ShouldBe(2);
        var mappedEntry = mappedStage.Entries[0];
        mappedEntry.BowlerId.ShouldBe(entry.BowlerId.Value.ToString());
        mappedEntry.BowlerName.ShouldBe("Entry Name");
        mappedEntry.SourceStageId.ShouldBe(stageId.Value.ToString());
        mappedEntry.Place.ShouldBe(3);
        mappedEntry.BackfillPoints.ShouldBe(12);
        mappedEntry.ViaOverride.ShouldBeTrue();
        var mappedBackfill = mappedStage.Entries[1];
        mappedBackfill.SourceStageId.ShouldBeNull();
        mappedBackfill.Place.ShouldBeNull();
        mappedBackfill.BackfillPoints.ShouldBe(backfill.BackfillPoints);

        var mappedOverride = mappedStage.Overrides.Single();
        mappedOverride.OverrideId.ShouldBe(stageOverride.OverrideId.Value.ToString());
        mappedOverride.BowlerId.ShouldBe(stageOverride.BowlerId.Value.ToString());
        mappedOverride.BowlerName.ShouldBe("Override Name");
        mappedOverride.Kind.ShouldBe("Clear");
        mappedOverride.Reason.ShouldBe("Moved.");
        mappedOverride.CreatedBy.ShouldBe("admin-1");
        mappedOverride.CreatedAt.ShouldBe(new DateTimeOffset(2025, 4, 1, 8, 0, 0, TimeSpan.Zero));
        mappedOverride.Applied.ShouldBeFalse();

        var mappedOutcome = mappedStage.SurvivorOutcomes.Single();
        mappedOutcome.BowlerId.ShouldBe(outcome.BowlerId.Value.ToString());
        mappedOutcome.BowlerName.ShouldBe("Outcome Name");
        mappedOutcome.BlockTotal.ShouldBe(1234);
        mappedOutcome.Advanced.ShouldBeFalse();

        var mappedPosition = response.Positions.Single();
        mappedPosition.BowlerId.ShouldBe(position.BowlerId.Value.ToString());
        mappedPosition.BowlerName.ShouldBe("Position Name");
        mappedPosition.Position.ShouldBe(4);
        mappedPosition.Bowled.ShouldBeFalse();
        mappedPosition.IsFinalist.ShouldBe(true);
    }

    [Fact(DisplayName = "HandleAsync should map null optional values to null and keep empty lists")]
    public async Task HandleAsync_ShouldMapNullOptionalValuesToNull()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var dto = SeriesAdvancementDtoFactory.Create(stages: [StageAdvancementDtoFactory.Create()]);
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<GetSeriesAdvancementQuery>(), ct)).ReturnsAsync(dto);
        var endpoint = Factory.Create<GetSeriesAdvancementEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(new GetSeriesAdvancementRequest { SeriesId = ValidId }, ct);

        // Assert
        endpoint.Response.BackfillAsOf.ShouldBeNull();
        endpoint.Response.Positions.ShouldBeEmpty();
        var stage = endpoint.Response.Stages.Single();
        stage.ComputedAt.ShouldBeNull();
        stage.OfficialDueAt.ShouldBeNull();
        stage.OfficialAt.ShouldBeNull();
        stage.SlotCount.ShouldBeNull();
        stage.VacantSlotCount.ShouldBeNull();
        stage.ResolvedSquadId.ShouldBeNull();
        stage.Entries.ShouldBeEmpty();
        stage.Overrides.ShouldBeEmpty();
        stage.SurvivorOutcomes.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should return 404 when the Series does not exist")]
    public async Task HandleAsync_ShouldReturn404_WhenSeriesDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = Handler();
        handler.Setup(h => h.HandleAsync(It.IsAny<GetSeriesAdvancementQuery>(), ct)).ReturnsAsync(SeriesErrors.NotFound(new SeriesId(ValidId)));
        var endpoint = Factory.Create<GetSeriesAdvancementEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(new GetSeriesAdvancementRequest { SeriesId = ValidId }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }
}