using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;
using Neba.Website.Server.Series;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesAdvancementMappingTests
{
    private static readonly IReadOnlyDictionary<DateTimeOffset, DateTimeOffset> NoLocalTimes
        = new Dictionary<DateTimeOffset, DateTimeOffset>();

    private static readonly AdvancementAccess ViewAndManage = new(CanView: true, CanEdit: true, CanManage: true, CanMakeOfficial: true);

    private const string OtherStageId = "01JBQ5Y7K3M9N2P4R6T8V0W1X6";

    // ---- SeriesAdvancementResponse.ToViewModel ----

    [Fact(DisplayName = "Advancement should order Stages by Order whatever the input order")]
    public void AdvancementToViewModel_ShouldOrderStagesByOrder()
    {
        // Arrange
        var response = SeriesAdvancementResponseFactory.Create(stages:
        [
            StageAdvancementResponseFactory.Create(stageId: "c", order: 3, kind: "Placement"),
            StageAdvancementResponseFactory.Create(stageId: "a", order: 1),
            StageAdvancementResponseFactory.Create(stageId: "b", order: 2, kind: "Survivor"),
        ]);

        // Act
        var model = response.ToViewModel(ViewAndManage, NoLocalTimes);

        // Assert
        model.Stages.Select(s => s.Id).ShouldBe(["a", "b", "c"]);
        model.Stages.Select(s => s.Order).ShouldBe([1, 2, 3]);
    }

    [Fact(DisplayName = "Advancement should order positions by Position whatever the input order")]
    public void AdvancementToViewModel_ShouldOrderPositionsByPosition()
    {
        // Arrange
        var response = SeriesAdvancementResponseFactory.Create(positions:
        [
            SeriesPositionResponseFactory.Create(bowlerId: "third", position: 3),
            SeriesPositionResponseFactory.Create(bowlerId: "first", position: 1),
            SeriesPositionResponseFactory.Create(bowlerId: "second", position: 2),
        ]);

        // Act
        var model = response.ToViewModel(ViewAndManage, NoLocalTimes);

        // Assert
        model.Positions.Select(p => p.BowlerId).ShouldBe(["first", "second", "third"]);
    }

    [Fact(DisplayName = "Advancement should carry the version, the Backfill date and the target field")]
    public void AdvancementToViewModel_ShouldCarryScalars()
    {
        // Arrange
        var backfillAsOf = new DateOnly(2025, 6, 14);
        var response = SeriesAdvancementResponseFactory.Create(version: 31, backfillAsOf: backfillAsOf, targetField: 24);

        // Act
        var model = response.ToViewModel(ViewAndManage, NoLocalTimes);

        // Assert
        model.Version.ShouldBe(31);
        model.BackfillAsOf.ShouldBe(backfillAsOf);
        model.TargetField.ShouldBe(24);
    }

    [Fact(DisplayName = "Advancement should keep a missing Backfill date null")]
    public void AdvancementToViewModel_ShouldKeepBackfillAsOfNull()
    {
        // Arrange
        var response = SeriesAdvancementResponseFactory.Create();

        // Act
        var model = response.ToViewModel(ViewAndManage, NoLocalTimes);

        // Assert
        model.BackfillAsOf.ShouldBeNull();
    }

    [Fact(DisplayName = "Advancement should map empty lists to empty lists")]
    public void AdvancementToViewModel_ShouldMapEmptyListsToEmpty()
    {
        // Arrange
        var response = SeriesAdvancementResponseFactory.Create(stages: [], positions: []);

        // Act
        var model = response.ToViewModel(ViewAndManage, NoLocalTimes);

        // Assert
        model.Stages.ShouldNotBeNull();
        model.Stages.ShouldBeEmpty();
        model.Positions.ShouldNotBeNull();
        model.Positions.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Advancement should map empty Stage lists to empty lists")]
    public void AdvancementToViewModel_ShouldMapEmptyStageListsToEmpty()
    {
        // Arrange
        var response = SeriesAdvancementResponseFactory.Create(stages:
        [
            StageAdvancementResponseFactory.Create(entries: [], overrides: [], survivorOutcomes: []),
        ]);

        // Act
        var stage = response.ToViewModel(ViewAndManage, NoLocalTimes).Stages.Single();

        // Assert
        stage.Entries.ShouldNotBeNull();
        stage.Entries.ShouldBeEmpty();
        stage.Overrides.ShouldNotBeNull();
        stage.Overrides.ShouldBeEmpty();
        stage.Outcomes.ShouldNotBeNull();
        stage.Outcomes.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Advancement should flag only the last Qualifying Stage")]
    public void AdvancementToViewModel_ShouldFlagLastQualifyingStage()
    {
        // Arrange
        var response = SeriesAdvancementResponseFactory.Create(stages:
        [
            StageAdvancementResponseFactory.Create(stageId: "placement", order: 4, kind: "Placement"),
            StageAdvancementResponseFactory.Create(stageId: "q2", order: 2),
            StageAdvancementResponseFactory.Create(stageId: "survivor", order: 3, kind: "Survivor"),
            StageAdvancementResponseFactory.Create(stageId: "q1", order: 1),
        ]);

        // Act
        var model = response.ToViewModel(ViewAndManage, NoLocalTimes);

        // Assert
        model.Stages.Where(s => s.IsLastQualifying).Select(s => s.Id).ShouldBe(["q2"]);
    }

    [Fact(DisplayName = "Advancement should flag no Stage as last Qualifying when there is no Qualifying Stage")]
    public void AdvancementToViewModel_ShouldFlagNoStage_WhenNoQualifyingStage()
    {
        // Arrange
        var response = SeriesAdvancementResponseFactory.Create(stages:
        [
            StageAdvancementResponseFactory.Create(stageId: "survivor", order: 1, kind: "Survivor"),
            StageAdvancementResponseFactory.Create(stageId: "placement", order: 2, kind: "Placement"),
        ]);

        // Act
        var model = response.ToViewModel(ViewAndManage, NoLocalTimes);

        // Assert
        model.Stages.ShouldAllBe(s => !s.IsLastQualifying);
    }

    [Theory(DisplayName = "Advancement should set CanRecompute from CanEdit")]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void AdvancementToViewModel_ShouldSetCanRecompute_FromCanView(bool canView, bool canManage)
    {
        // Arrange
        var response = SeriesAdvancementResponseFactory.Create(stages:
        [
            StageAdvancementResponseFactory.Create(order: 1),
            StageAdvancementResponseFactory.Create(stageId: OtherStageId, order: 2, kind: "Placement"),
        ]);

        // Act
        var model = response.ToViewModel(new AdvancementAccess(canView, canView, canManage, false), NoLocalTimes);

        // Assert
        model.Stages.ShouldAllBe(s => s.CanRecompute == canView);
    }

    [Theory(DisplayName = "Advancement should allow Pick squad only for a viewer on a Survivor Stage")]
    [InlineData(true, "Survivor", true)]
    [InlineData(false, "Survivor", false)]
    [InlineData(true, "Qualifying", false)]
    [InlineData(true, "Placement", false)]
    public void AdvancementToViewModel_ShouldSetCanPickSquad(bool canView, string kind, bool expected)
    {
        // Arrange
        var response = SeriesAdvancementResponseFactory.Create(stages: [StageAdvancementResponseFactory.Create(kind: kind)]);

        // Act
        var stage = response.ToViewModel(new AdvancementAccess(canView, canView, CanManage: false, CanMakeOfficial: false), NoLocalTimes).Stages.Single();

        // Assert
        stage.CanPickSquad.ShouldBe(expected);
    }

    [Theory(DisplayName = "Advancement should allow adding and removing overrides only for a manager on a Computed Qualifying Stage")]
    [InlineData(true, true, "Qualifying", "Computed", true)]
    [InlineData(true, false, "Qualifying", "Computed", false)]
    [InlineData(false, false, "Qualifying", "Computed", false)]
    [InlineData(true, true, "Qualifying", "Official", false)]
    [InlineData(true, true, "Qualifying", "Pending", false)]
    [InlineData(true, true, "Survivor", "Computed", false)]
    [InlineData(true, true, "Placement", "Computed", false)]
    public void AdvancementToViewModel_ShouldSetOverrideFlags(bool canView, bool canManage, string kind, string status, bool expected)
    {
        // Arrange
        var response = SeriesAdvancementResponseFactory.Create(stages: [StageAdvancementResponseFactory.Create(kind: kind, status: status)]);

        // Act
        var stage = response.ToViewModel(new AdvancementAccess(canView, canView, canManage, false), NoLocalTimes).Stages.Single();

        // Assert
        stage.CanAddOverride.ShouldBe(expected);
        stage.CanRemoveOverride.ShouldBe(expected);
    }

    [Fact(DisplayName = "Advancement should let a manager clear a Qualifier entry that this Stage produced")]
    public void AdvancementToViewModel_ShouldAllowClear_ForOwnQualifierEntry()
    {
        // Arrange
        var response = SeriesAdvancementResponseFactory.Create(stages:
        [
            StageAdvancementResponseFactory.Create(stageId: "q1", status: "Computed",
                entries: [FieldEntryResponseFactory.Create(sourceStageId: "q1")]),
        ]);

        // Act
        var entry = response.ToViewModel(ViewAndManage, NoLocalTimes).Stages.Single().Entries.Single();

        // Assert
        entry.CanClear.ShouldBeTrue();
    }

    [Fact(DisplayName = "Advancement should not let a manager clear a Backfill entry")]
    public void AdvancementToViewModel_ShouldNotAllowClear_ForBackfillEntry()
    {
        // Arrange
        var response = SeriesAdvancementResponseFactory.Create(stages:
        [
            StageAdvancementResponseFactory.Create(stageId: "q1", status: "Computed",
                entries: [FieldEntryResponseFactory.Create(sourceStageId: null)]),
        ]);

        // Act
        var entry = response.ToViewModel(ViewAndManage, NoLocalTimes).Stages.Single().Entries.Single();

        // Assert
        entry.CanClear.ShouldBeFalse();
        entry.IsBackfill.ShouldBeTrue();
    }

    [Fact(DisplayName = "Advancement should not let a manager clear an entry another Stage produced")]
    public void AdvancementToViewModel_ShouldNotAllowClear_ForEntryFromAnotherStage()
    {
        // Arrange
        var response = SeriesAdvancementResponseFactory.Create(stages:
        [
            StageAdvancementResponseFactory.Create(stageId: "q2", status: "Computed",
                entries: [FieldEntryResponseFactory.Create(sourceStageId: "q1")]),
        ]);

        // Act
        var entry = response.ToViewModel(ViewAndManage, NoLocalTimes).Stages.Single().Entries.Single();

        // Assert
        entry.CanClear.ShouldBeFalse();
        entry.IsBackfill.ShouldBeFalse();
    }

    [Fact(DisplayName = "Advancement should not let a manager clear an entry an override added")]
    public void AdvancementToViewModel_ShouldNotAllowClear_ForOverrideEntry()
    {
        // Arrange
        var response = SeriesAdvancementResponseFactory.Create(stages:
        [
            StageAdvancementResponseFactory.Create(stageId: "q1", status: "Computed",
                entries: [FieldEntryResponseFactory.Create(sourceStageId: "q1", viaOverride: true)]),
        ]);

        // Act
        var entry = response.ToViewModel(ViewAndManage, NoLocalTimes).Stages.Single().Entries.Single();

        // Assert
        entry.CanClear.ShouldBeFalse();
        entry.ViaOverride.ShouldBeTrue();
    }

    [Theory(DisplayName = "Advancement should not let anyone clear an entry without manage rights or on a Stage that is not Computed")]
    [InlineData(false, "Computed")]
    [InlineData(true, "Official")]
    [InlineData(true, "Pending")]
    public void AdvancementToViewModel_ShouldNotAllowClear_WhenStageOutputIsLocked(bool canManage, string status)
    {
        // Arrange
        var response = SeriesAdvancementResponseFactory.Create(stages:
        [
            StageAdvancementResponseFactory.Create(stageId: "q1", status: status,
                entries: [FieldEntryResponseFactory.Create(sourceStageId: "q1")]),
        ]);

        // Act
        var entry = response.ToViewModel(new AdvancementAccess(CanView: true, CanEdit: true, canManage, CanMakeOfficial: false), NoLocalTimes).Stages.Single().Entries.Single();

        // Assert
        entry.CanClear.ShouldBeFalse();
    }

    [Fact(DisplayName = "Advancement should map entry fields")]
    public void AdvancementToViewModel_ShouldMapEntryFields()
    {
        // Arrange
        var response = SeriesAdvancementResponseFactory.Create(stages:
        [
            StageAdvancementResponseFactory.Create(entries:
            [
                FieldEntryResponseFactory.Create(bowlerId: "b1", bowlerName: "Ann Lee", sourceStageId: null, backfillPoints: 77),
            ]),
        ]);

        // Act
        var entry = response.ToViewModel(ViewAndManage, NoLocalTimes).Stages.Single().Entries.Single();

        // Assert
        entry.BowlerId.ShouldBe("b1");
        entry.BowlerName.ShouldBe("Ann Lee");
        entry.SourceStageId.ShouldBeNull();
        entry.Place.ShouldBeNull();
        entry.BackfillPoints.ShouldBe(77);
        entry.ViaOverride.ShouldBeFalse();
    }

    [Theory(DisplayName = "Advancement should allow Mark official only when the viewer may and the API says the Stage is ready")]
    [InlineData(true, true, true, TestDisplayName = "viewer may and tournament is ready")]
    [InlineData(true, false, false, TestDisplayName = "viewer may but tournament is not ready")]
    [InlineData(false, true, false, TestDisplayName = "viewer may not but tournament is ready")]
    [InlineData(false, false, false, TestDisplayName = "viewer may not and tournament is not ready")]
    public void AdvancementToViewModel_ShouldSetCanMakeOfficial(bool canMakeOfficial, bool ready, bool expected)
    {
        // Arrange
        var response = SeriesAdvancementResponseFactory.Create(stages: [StageAdvancementResponseFactory.Create(canMakeOfficial: ready)]);
        var access = new AdvancementAccess(CanView: true, CanEdit: false, CanManage: false, CanMakeOfficial: canMakeOfficial);

        // Act
        var stage = response.ToViewModel(access, NoLocalTimes).Stages.Single();

        // Assert
        stage.CanMakeOfficial.ShouldBe(expected);
    }

    [Fact(DisplayName = "Advancement should not allow Recompute or Pick squad for a viewer who cannot edit the Series")]
    public void AdvancementToViewModel_ShouldNotAllowEditActions_WithoutCanEdit()
    {
        // Arrange
        var response = SeriesAdvancementResponseFactory.Create(stages: [StageAdvancementResponseFactory.Create(kind: "Survivor")]);
        var access = new AdvancementAccess(CanView: true, CanEdit: false, CanManage: false, CanMakeOfficial: true);

        // Act
        var stage = response.ToViewModel(access, NoLocalTimes).Stages.Single();

        // Assert
        stage.CanRecompute.ShouldBeFalse();
        stage.CanPickSquad.ShouldBeFalse();
    }

    [Fact(DisplayName = "Advancement should convert an override's created time through the local times")]
    public void AdvancementToViewModel_ShouldConvertOverrideCreatedAt()
    {
        // Arrange
        var utc = new DateTimeOffset(2025, 3, 25, 12, 0, 0, TimeSpan.Zero);
        var local = new DateTimeOffset(2025, 3, 25, 8, 0, 0, TimeSpan.FromHours(-4));
        var response = SeriesAdvancementResponseFactory.Create(stages:
        [
            StageAdvancementResponseFactory.Create(overrides:
            [
                StageOverrideResponseFactory.Create(overrideId: "o1", bowlerId: "b1", bowlerName: "Ann Lee", kind: "Clear",
                    reason: "Moved away.", createdAt: utc, applied: false),
            ]),
        ]);
        var localTimes = new Dictionary<DateTimeOffset, DateTimeOffset> { [utc] = local };

        // Act
        var item = response.ToViewModel(ViewAndManage, localTimes).Stages.Single().Overrides.Single();

        // Assert
        item.CreatedAt.Offset.ShouldBe(TimeSpan.FromHours(-4));
        item.CreatedAt.DateTime.ShouldBe(new DateTime(2025, 3, 25, 8, 0, 0, DateTimeKind.Unspecified));
        item.OverrideId.ShouldBe("o1");
        item.BowlerId.ShouldBe("b1");
        item.BowlerName.ShouldBe("Ann Lee");
        item.Kind.ShouldBe("Clear");
        item.Reason.ShouldBe("Moved away.");
        item.Applied.ShouldBeFalse();
    }

    [Fact(DisplayName = "Advancement should pass an override's UTC created time through when the local times lack it")]
    public void AdvancementToViewModel_ShouldPassOverrideCreatedAtThrough_WhenMissingFromLocalTimes()
    {
        // Arrange
        var utc = new DateTimeOffset(2025, 3, 25, 12, 0, 0, TimeSpan.Zero);
        var response = SeriesAdvancementResponseFactory.Create(stages:
        [
            StageAdvancementResponseFactory.Create(overrides: [StageOverrideResponseFactory.Create(createdAt: utc)]),
        ]);

        // Act
        var item = response.ToViewModel(ViewAndManage, NoLocalTimes).Stages.Single().Overrides.Single();

        // Assert
        item.CreatedAt.Offset.ShouldBe(TimeSpan.Zero);
        item.CreatedAt.DateTime.ShouldBe(new DateTime(2025, 3, 25, 12, 0, 0, DateTimeKind.Unspecified));
    }

    [Fact(DisplayName = "Advancement should map Stage fields")]
    public void AdvancementToViewModel_ShouldMapStageFields()
    {
        // Arrange
        var response = SeriesAdvancementResponseFactory.Create(stages:
        [
            StageAdvancementResponseFactory.Create(stageId: "s1", order: 2, kind: "Survivor", status: "Computed", isStale: true,
                needsSquad: true, tournamentId: "t1", tournamentStatus: "Cancelled", resolvedSquadId: "sq1"),
        ]);

        // Act
        var stage = response.ToViewModel(ViewAndManage, NoLocalTimes).Stages.Single();

        // Assert
        stage.Id.ShouldBe("s1");
        stage.Order.ShouldBe(2);
        stage.Kind.ShouldBe("Survivor");
        stage.Status.ShouldBe("Computed");
        stage.IsStale.ShouldBeTrue();
        stage.NeedsSquad.ShouldBeTrue();
        stage.TournamentId.ShouldBe("t1");
        stage.TournamentStatus.ShouldBe("Cancelled");
        stage.ResolvedSquadId.ShouldBe("sq1");
        stage.SlotCount.ShouldBeNull();
        stage.VacantSlotCount.ShouldBeNull();
    }

    [Fact(DisplayName = "Advancement should carry the slot counts of a Qualifying Stage")]
    public void AdvancementToViewModel_ShouldMapSlotCounts()
    {
        // Arrange
        var response = SeriesAdvancementResponseFactory.Create(stages:
        [
            StageAdvancementResponseFactory.Create(slotCount: 12, vacantSlotCount: 2),
        ]);

        // Act
        var stage = response.ToViewModel(ViewAndManage, NoLocalTimes).Stages.Single();

        // Assert
        stage.SlotCount.ShouldBe(12);
        stage.VacantSlotCount.ShouldBe(2);
    }

    [Fact(DisplayName = "Advancement should map survivor outcomes field by field and keep a null block total")]
    public void AdvancementToViewModel_ShouldMapSurvivorOutcomes()
    {
        // Arrange
        var response = SeriesAdvancementResponseFactory.Create(stages:
        [
            StageAdvancementResponseFactory.Create(kind: "Survivor", survivorOutcomes:
            [
                SurvivorOutcomeResponseFactory.Create(bowlerId: "b1", bowlerName: "Ann Lee", blockTotal: 1234, advanced: true),
                SurvivorOutcomeResponseFactory.Create(bowlerId: "b2", bowlerName: "Bo Chen", blockTotal: null, advanced: false),
            ]),
        ]);

        // Act
        var outcomes = response.ToViewModel(ViewAndManage, NoLocalTimes).Stages.Single().Outcomes;

        // Assert
        outcomes.Count.ShouldBe(2);
        outcomes[0].ShouldBe(new SurvivorOutcomeViewModel("b1", "Ann Lee", 1234, true));
        outcomes[1].ShouldBe(new SurvivorOutcomeViewModel("b2", "Bo Chen", null, false));
    }

    [Fact(DisplayName = "Advancement should map positions field by field and keep a null finalist flag")]
    public void AdvancementToViewModel_ShouldMapPositions()
    {
        // Arrange
        var response = SeriesAdvancementResponseFactory.Create(positions:
        [
            SeriesPositionResponseFactory.Create(bowlerId: "b1", bowlerName: "Ann Lee", position: 1, bowled: true, isFinalist: true),
            SeriesPositionResponseFactory.Create(bowlerId: "b2", bowlerName: "Bo Chen", position: 2, bowled: false, isFinalist: null),
        ]);

        // Act
        var positions = response.ToViewModel(ViewAndManage, NoLocalTimes).Positions;

        // Assert
        positions.Count.ShouldBe(2);
        positions[0].ShouldBe(new PositionViewModel("b1", "Ann Lee", 1, true, true));
        positions[1].ShouldBe(new PositionViewModel("b2", "Bo Chen", 2, false, null));
    }

    // ---- SeriesAdvancementResponse.Instants ----

    [Fact(DisplayName = "Instants should list each due time and override created time once")]
    public void Instants_ShouldListEachInstantOnce()
    {
        // Arrange
        var created = new DateTimeOffset(2025, 3, 25, 12, 0, 0, TimeSpan.Zero);
        var later = new DateTimeOffset(2025, 3, 26, 12, 0, 0, TimeSpan.Zero);
        var response = SeriesAdvancementResponseFactory.Create(stages:
        [
            StageAdvancementResponseFactory.Create(stageId: "a", order: 1, overrides:
            [
                StageOverrideResponseFactory.Create(overrideId: "o1", createdAt: created),
                StageOverrideResponseFactory.Create(overrideId: "o2", createdAt: created),
            ]),
            StageAdvancementResponseFactory.Create(stageId: "b", order: 2),
            StageAdvancementResponseFactory.Create(stageId: "c", order: 3, overrides: [StageOverrideResponseFactory.Create(overrideId: "o3", createdAt: later)]),
        ]);

        // Act
        var instants = response.Instants();

        // Assert
        instants.Count.ShouldBe(2);
        instants.ShouldBe([created, later], ignoreOrder: true);
    }

    [Fact(DisplayName = "Instants should be empty for a Stage without overrides")]
    public void Instants_ShouldSkipStagesWithoutOverrides()
    {
        // Arrange
        var response = SeriesAdvancementResponseFactory.Create(stages: [StageAdvancementResponseFactory.Create(overrides: [])]);

        // Act
        var instants = response.Instants();

        // Assert
        instants.ShouldBeEmpty();
    }

    // ---- Result and squad summaries ----

    [Fact(DisplayName = "A Stage result should map to a view model with an ordinal label")]
    public void StageResultToViewModel_ShouldBuildLabel()
    {
        // Arrange
        var response = StageResultSummaryResponseFactory.Create(bowlerId: "b1", bowlerName: "Marcus Alvarez", place: 12);

        // Act
        var model = response.ToViewModel();

        // Assert
        model.BowlerId.ShouldBe("b1");
        model.BowlerName.ShouldBe("Marcus Alvarez");
        model.Place.ShouldBe(12);
        model.Label.ShouldBe("Marcus Alvarez (12th)");
    }

    [Fact(DisplayName = "A Stage squad should map to a view model with a time label and a detail")]
    public void StageSquadToViewModel_ShouldBuildLabelAndDetail()
    {
        // Arrange
        var response = StageSquadSummaryResponseFactory.Create(
            squadId: "sq1", date: new DateOnly(2025, 7, 12), startTime: new TimeOnly(9, 30), hasScores: true, isSelected: true);

        // Act
        var model = response.ToViewModel();

        // Assert
        model.SquadId.ShouldBe("sq1");
        model.Label.ShouldBe("9:30 AM on 7/12");
        model.Detail.ShouldBe("Has scores · Current");
        model.HasScores.ShouldBeTrue();
        model.IsSelected.ShouldBeTrue();
    }

    [Fact(DisplayName = "A Stage squad should carry cleared flags")]
    public void StageSquadToViewModel_ShouldCarryClearedFlags()
    {
        // Arrange
        var response = StageSquadSummaryResponseFactory.Create(hasScores: false, isSelected: false);

        // Act
        var model = response.ToViewModel();

        // Assert
        model.Detail.ShouldBe("No scores yet");
        model.HasScores.ShouldBeFalse();
        model.IsSelected.ShouldBeFalse();
    }

    // ---- SeriesAdvancementViewModel members ----

    [Fact(DisplayName = "HasFinalists should be true when a position has a Finalist flag")]
    public void HasFinalists_ShouldBeTrue_WhenAnyPositionHasFlag()
    {
        // Arrange
        var model = SeriesAdvancementViewModelFactory.Create(positions:
        [
            PositionViewModelFactory.Create(position: 1, isFinalist: null),
            PositionViewModelFactory.Create(position: 2, isFinalist: false),
        ]);

        // Act
        var hasFinalists = model.HasFinalists;

        // Assert
        hasFinalists.ShouldBeTrue();
    }

    [Fact(DisplayName = "HasFinalists should be false when no position has a Finalist flag")]
    public void HasFinalists_ShouldBeFalse_WhenNoFlags()
    {
        // Arrange
        var model = SeriesAdvancementViewModelFactory.Create(positions: [PositionViewModelFactory.Create(isFinalist: null)]);

        // Act
        var hasFinalists = model.HasFinalists;

        // Assert
        hasFinalists.ShouldBeFalse();
    }

    [Fact(DisplayName = "HasFinalists should be false when there are no positions")]
    public void HasFinalists_ShouldBeFalse_WhenNoPositions()
    {
        // Arrange
        var model = SeriesAdvancementViewModelFactory.Create(positions: []);

        // Act
        var hasFinalists = model.HasFinalists;

        // Assert
        hasFinalists.ShouldBeFalse();
    }

    [Fact(DisplayName = "StageById should find a Stage by its id and return null for an unknown id")]
    public void StageById_ShouldFindStage()
    {
        // Arrange
        var first = StageAdvancementViewModelFactory.Create(id: "a", order: 1);
        var second = StageAdvancementViewModelFactory.Create(id: "b", order: 2);
        var model = SeriesAdvancementViewModelFactory.Create(stages: [first, second]);

        // Act
        var found = model.StageById("b");
        var missing = model.StageById("zzz");

        // Assert
        found.ShouldBe(second);
        missing.ShouldBeNull();
    }

    [Fact(DisplayName = "OfficialOrdersFrom should list only Official Stages at or after the order")]
    public void OfficialOrdersFrom_ShouldListOfficialStagesFromOrder()
    {
        // Arrange
        var model = SeriesAdvancementViewModelFactory.Create(stages:
        [
            StageAdvancementViewModelFactory.Create(id: "a", order: 1, status: "Official"),
            StageAdvancementViewModelFactory.Create(id: "b", order: 2, status: "Official"),
            StageAdvancementViewModelFactory.Create(id: "c", order: 3, status: "Computed"),
            StageAdvancementViewModelFactory.Create(id: "d", order: 4, status: "Official"),
            StageAdvancementViewModelFactory.Create(id: "e", order: 5, status: "Pending"),
        ]);

        // Act
        var orders = model.OfficialOrdersFrom(2);

        // Assert
        orders.ShouldBe([2, 4]);
    }

    [Fact(DisplayName = "OfficialOrdersFrom should be empty when no Stage after the order is Official")]
    public void OfficialOrdersFrom_ShouldBeEmpty_WhenNoneOfficial()
    {
        // Arrange
        var model = SeriesAdvancementViewModelFactory.Create(stages:
        [
            StageAdvancementViewModelFactory.Create(id: "a", order: 1, status: "Official"),
            StageAdvancementViewModelFactory.Create(id: "b", order: 2, status: "Computed"),
        ]);

        // Act
        var orders = model.OfficialOrdersFrom(2);

        // Assert
        orders.ShouldBeEmpty();
    }

    [Fact(DisplayName = "AddCandidates should drop bowlers already in a Stage's entries and bowlers with an override on that Stage")]
    public void AddCandidates_ShouldDropEntriesAndOverridesOnStage()
    {
        // Arrange
        var model = SeriesAdvancementViewModelFactory.Create(stages:
        [
            StageAdvancementViewModelFactory.Create(id: "a", order: 1,
                entries: [FieldEntryViewModelFactory.Create(bowlerId: "b1")],
                overrides: [StageOverrideViewModelFactory.Create(bowlerId: "b2")]),
            StageAdvancementViewModelFactory.Create(id: "b", order: 2,
                entries: [FieldEntryViewModelFactory.Create(bowlerId: "b3")],
                overrides: [StageOverrideViewModelFactory.Create(bowlerId: "b4", kind: "Clear")]),
        ]);
        var results = ResultsFor("b1", "b2", "b3", "b4", "b5");

        // Act
        var candidates = model.AddCandidates("a", results);

        // Assert
        candidates.Select(c => c.BowlerId).ShouldBe(["b4", "b5"]);
    }

    [Fact(DisplayName = "AddCandidates should drop only entry matches for an unknown Stage id")]
    public void AddCandidates_ShouldDropOnlyEntries_ForUnknownStage()
    {
        // Arrange
        var model = SeriesAdvancementViewModelFactory.Create(stages:
        [
            StageAdvancementViewModelFactory.Create(id: "a", order: 1,
                entries: [FieldEntryViewModelFactory.Create(bowlerId: "b1")],
                overrides: [StageOverrideViewModelFactory.Create(bowlerId: "b2")]),
        ]);
        var results = ResultsFor("b1", "b2", "b3");

        // Act
        var candidates = model.AddCandidates("unknown", results);

        // Assert
        candidates.Select(c => c.BowlerId).ShouldBe(["b2", "b3"]);
    }

    [Fact(DisplayName = "AddCandidates should return an empty list when every result is taken")]
    public void AddCandidates_ShouldBeEmpty_WhenAllTaken()
    {
        // Arrange
        var model = SeriesAdvancementViewModelFactory.Create(stages:
        [
            StageAdvancementViewModelFactory.Create(id: "a", entries: [FieldEntryViewModelFactory.Create(bowlerId: "b1")]),
        ]);

        // Act
        var candidates = model.AddCandidates("a", ResultsFor("b1"));

        // Assert
        candidates.ShouldBeEmpty();
    }

    // ---- Smaller members ----

    [Theory(DisplayName = "IsTournamentCancelled should be true only for a Cancelled tournament")]
    [InlineData("Cancelled", true)]
    [InlineData("Scheduled", false)]
    [InlineData("Completed", false)]
    public void IsTournamentCancelled_ShouldMatchStatus(string tournamentStatus, bool expected)
    {
        // Arrange
        var stage = StageAdvancementViewModelFactory.Create(tournamentStatus: tournamentStatus);

        // Act
        var cancelled = stage.IsTournamentCancelled;

        // Assert
        cancelled.ShouldBe(expected);
    }

    [Theory(DisplayName = "OverrideKinds.ToValue should map Add to 1, Clear to 2 and an unknown name to 0")]
    [InlineData("Add", 1)]
    [InlineData("Clear", 2)]
    [InlineData("Remove", 0)]
    [InlineData("", 0)]
    public void OverrideKindsToValue_ShouldMapKind(string kind, int expected)
    {
        // Arrange & Act
        var value = OverrideKinds.ToValue(kind);

        // Assert
        value.ShouldBe(expected);
    }

    [Fact(DisplayName = "The reason limit should be 500 characters")]
    public void ReasonMaxLength_ShouldBe500()
    {
        // Arrange & Act
        const int max = StageOverrideFormModel.ReasonMaxLength;

        // Assert
        max.ShouldBe(500);
    }

    private static IReadOnlyList<StageResultViewModel> ResultsFor(params string[] bowlerIds)
        => [.. bowlerIds.Select((id, index) => StageResultViewModelFactory.Create(bowlerId: id, place: index + 1))];
}