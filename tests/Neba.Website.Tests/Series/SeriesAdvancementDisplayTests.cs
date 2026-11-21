using System.Globalization;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;
using Neba.Website.Server.Series;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesAdvancementDisplayTests
{
    // ---- Status classes and titles ----

    [Theory(DisplayName = "StatusPillClass should name the pill for each status and fall back to pending")]
    [InlineData("Official", "series-pill series-pill--official")]
    [InlineData("Computed", "series-pill series-pill--computed")]
    [InlineData("Pending", "series-pill series-pill--pending")]
    [InlineData("Unknown", "series-pill series-pill--pending")]
    public void StatusPillClass_ShouldMapStatus(string status, string expected)
    {
        // Arrange & Act
        var css = SeriesDisplay.StatusPillClass(status);

        // Assert
        css.ShouldBe(expected);
    }

    [Theory(DisplayName = "SegmentClass should name the strip segment for each status and fall back to the plain segment")]
    [InlineData("Official", "series-strip__seg series-strip__seg--official")]
    [InlineData("Computed", "series-strip__seg series-strip__seg--computed")]
    [InlineData("Pending", "series-strip__seg")]
    [InlineData("Unknown", "series-strip__seg")]
    public void SegmentClass_ShouldMapStatus(string status, string expected)
    {
        // Arrange & Act
        var css = SeriesDisplay.SegmentClass(status);

        // Assert
        css.ShouldBe(expected);
    }

    [Theory(DisplayName = "NodeClass should name the rail node for each status and add the cancelled suffix")]
    [InlineData("Official", false, "series-node series-node--official")]
    [InlineData("Computed", false, "series-node series-node--computed")]
    [InlineData("Pending", false, "series-node series-node--pending")]
    [InlineData("Unknown", false, "series-node series-node--pending")]
    [InlineData("Official", true, "series-node series-node--official series-node--cancelled")]
    [InlineData("Pending", true, "series-node series-node--pending series-node--cancelled")]
    [InlineData("Unknown", true, "series-node series-node--pending series-node--cancelled")]
    public void NodeClass_ShouldMapStatusAndCancelled(string status, bool isCancelled, string expected)
    {
        // Arrange & Act
        var css = SeriesDisplay.NodeClass(status, isCancelled);

        // Assert
        css.ShouldBe(expected);
    }

    [Fact(DisplayName = "SegmentTitle should name the Stage and its status")]
    public void SegmentTitle_ShouldNameStageAndStatus()
    {
        // Arrange & Act
        var title = SeriesDisplay.SegmentTitle(3, "Official");

        // Assert
        title.ShouldBe("Stage 3: Official");
    }

    [Fact(DisplayName = "StageChip should name the kind and the Stage")]
    public void StageChip_ShouldNameKindAndStage()
    {
        // Arrange & Act
        var chip = SeriesDisplay.StageChip("Qualifying", 3);

        // Assert
        chip.ShouldBe("Qualifying · Stage 3");
    }

    // ---- RailMeta and Headline ----

    [Fact(DisplayName = "RailMeta should include the team size and the team advancing label above one bowler")]
    public void RailMeta_ShouldIncludeTeamSize_ForTeams()
    {
        // Arrange
        var stage = SeriesStageViewModelFactory.Create(
            kind: "Qualifying", advancingCount: 2, teamSize: 3,
            tournamentStartDate: new DateOnly(2025, 7, 12), tournamentEndDate: new DateOnly(2025, 7, 13));

        // Act
        var meta = SeriesDisplay.RailMeta(stage, null);

        // Assert
        meta.ShouldBe("7/12 to 7/13 · 3-bowler teams · Top 2 teams (6 bowlers)");
    }

    [Fact(DisplayName = "RailMeta should leave out the team size for singles")]
    public void RailMeta_ShouldOmitTeamSize_ForSingles()
    {
        // Arrange
        var stage = SeriesStageViewModelFactory.Create(
            kind: "Qualifying", advancingCount: 8, teamSize: 1,
            tournamentStartDate: new DateOnly(2025, 7, 12), tournamentEndDate: new DateOnly(2025, 7, 12));

        // Act
        var meta = SeriesDisplay.RailMeta(stage, null);

        // Assert
        meta.ShouldBe("7/12 · Top 8");
    }

    [Fact(DisplayName = "RailMeta should use the Finale count for a Placement Stage")]
    public void RailMeta_ShouldUseFinaleCount_ForPlacement()
    {
        // Arrange
        var stage = SeriesStageViewModelFactory.Create(
            kind: "Placement", advancingCount: null, teamSize: 1,
            tournamentStartDate: new DateOnly(2025, 7, 12), tournamentEndDate: new DateOnly(2025, 7, 12));

        // Act
        var meta = SeriesDisplay.RailMeta(stage, 4);

        // Assert
        meta.ShouldBe("7/12 · Top 4 to the Finale");
    }

    [Fact(DisplayName = "Headline should be null while a Stage is Pending")]
    public void Headline_ShouldBeNull_WhenPending()
    {
        // Arrange
        var stage = StageAdvancementViewModelFactory.Create(status: "Pending");

        // Act
        var headline = SeriesDisplay.Headline(stage, 5);

        // Assert
        headline.ShouldBeNull();
    }

    [Fact(DisplayName = "Headline should count filled Qualifying slots in the plural")]
    public void Headline_ShouldCountSlots_Plural()
    {
        // Arrange
        var stage = StageAdvancementViewModelFactory.Create(kind: "Qualifying", status: "Computed", slotCount: 8, vacantSlotCount: 1);

        // Act
        var headline = SeriesDisplay.Headline(stage, 0);

        // Assert
        headline.ShouldBe("7 of 8 slots filled");
    }

    [Fact(DisplayName = "Headline should count filled Qualifying slots in the singular")]
    public void Headline_ShouldCountSlots_Singular()
    {
        // Arrange
        var stage = StageAdvancementViewModelFactory.Create(kind: "Qualifying", status: "Official", slotCount: 1, vacantSlotCount: 0);

        // Act
        var headline = SeriesDisplay.Headline(stage, 0);

        // Assert
        headline.ShouldBe("1 of 1 slot filled");
    }

    [Fact(DisplayName = "Headline should count advancing bowlers for a Survivor Stage")]
    public void Headline_ShouldCountAdvancing_ForSurvivor()
    {
        // Arrange
        var stage = StageAdvancementViewModelFactory.Create(kind: "Survivor", status: "Computed", outcomes:
        [
            SurvivorOutcomeViewModelFactory.Create(bowlerId: "b1", advanced: true),
            SurvivorOutcomeViewModelFactory.Create(bowlerId: "b2", advanced: false),
            SurvivorOutcomeViewModelFactory.Create(bowlerId: "b3", advanced: true),
        ]);

        // Act
        var headline = SeriesDisplay.Headline(stage, 0);

        // Assert
        headline.ShouldBe("2 advance");
    }

    [Fact(DisplayName = "Headline should count ranked bowlers for a Placement Stage")]
    public void Headline_ShouldCountRanked_ForPlacement()
    {
        // Arrange
        var stage = StageAdvancementViewModelFactory.Create(kind: "Placement", status: "Computed");

        // Act
        var headline = SeriesDisplay.Headline(stage, 20);

        // Assert
        headline.ShouldBe("20 ranked");
    }

    // ---- Due and Backfill labels, Progress ----

    [Fact(DisplayName = "OfficialDueLabel should show the time and date with about")]
    public void OfficialDueLabel_ShouldShowTimeAndDate()
    {
        // Arrange
        var local = new DateTimeOffset(2025, 7, 12, 15, 15, 0, TimeSpan.FromHours(-4));

        // Act
        var label = SeriesDisplay.OfficialDueLabel(local);

        // Assert
        label.ShouldBe("Becomes official about 3:15 PM on 7/12");
    }

    [Fact(DisplayName = "OfficialDueLabel should be null for a missing time")]
    public void OfficialDueLabel_ShouldBeNull_WhenMissing()
    {
        // Arrange & Act
        var label = SeriesDisplay.OfficialDueLabel(null);

        // Assert
        label.ShouldBeNull();
    }

    [Fact(DisplayName = "BackfillLabel should show the date")]
    public void BackfillLabel_ShouldShowDate()
    {
        // Arrange & Act
        var label = SeriesDisplay.BackfillLabel(new DateOnly(2025, 6, 14));

        // Assert
        label.ShouldBe("Backfill as of 6/14/2025");
    }

    [Fact(DisplayName = "BackfillLabel should be null for a missing date")]
    public void BackfillLabel_ShouldBeNull_WhenMissing()
    {
        // Arrange & Act
        var label = SeriesDisplay.BackfillLabel(null);

        // Assert
        label.ShouldBeNull();
    }

    [Fact(DisplayName = "Progress should count done and official Stages and say the Backfill has not run")]
    public void Progress_ShouldCountStages_WhenBackfillNotRun()
    {
        // Arrange
        IReadOnlyList<StageAdvancementViewModel> stages =
        [
            StageAdvancementViewModelFactory.Create(id: "a", order: 1, status: "Official"),
            StageAdvancementViewModelFactory.Create(id: "b", order: 2, status: "Computed"),
            StageAdvancementViewModelFactory.Create(id: "c", order: 3, status: "Pending"),
        ];

        // Act
        var line = SeriesDisplay.Progress(stages, null);

        // Assert
        line.Lead.ShouldBe("2 of 3 Stages");
        line.Detail.ShouldBe(" computed or official (1 official). Backfill has not run.");
    }

    [Fact(DisplayName = "Progress should show the Backfill date and a singular Stage")]
    public void Progress_ShouldShowBackfillDate_ForOneStage()
    {
        // Arrange
        IReadOnlyList<StageAdvancementViewModel> stages = [StageAdvancementViewModelFactory.Create(status: "Official")];

        // Act
        var line = SeriesDisplay.Progress(stages, new DateOnly(2025, 6, 14));

        // Assert
        line.Lead.ShouldBe("1 of 1 Stage");
        line.Detail.ShouldBe(" computed or official (1 official). Backfill ran as of 6/14/2025.");
    }

    [Fact(DisplayName = "Progress should use the plural for eleven Stages")]
    public void Progress_ShouldUsePlural_ForElevenStages()
    {
        // Arrange
        IReadOnlyList<StageAdvancementViewModel> stages =
        [
            .. Enumerable.Range(1, 4).Select(order => StageAdvancementViewModelFactory.Create(id: $"c{order}", order: order, status: "Computed")),
            .. Enumerable.Range(5, 7).Select(order => StageAdvancementViewModelFactory.Create(id: $"p{order}", order: order, status: "Pending")),
        ];

        // Act
        var line = SeriesDisplay.Progress(stages, null);

        // Assert
        line.Lead.ShouldBe("4 of 11 Stages");
        line.Detail.ShouldBe(" computed or official (0 official). Backfill has not run.");
    }

    // ---- Expand label and pending reason ----

    [Theory(DisplayName = "ExpandLabel should say Show or Hide with the Stage order")]
    [InlineData(true, "Hide output for Stage 3")]
    [InlineData(false, "Show output for Stage 3")]
    public void ExpandLabel_ShouldReflectState(bool isExpanded, string expected)
    {
        // Arrange & Act
        var label = SeriesDisplay.ExpandLabel(isExpanded, 3);

        // Assert
        label.ShouldBe(expected);
    }

    [Fact(DisplayName = "PendingReason should explain a cancelled tournament before anything else")]
    public void PendingReason_ShouldExplainCancelled_First()
    {
        // Arrange
        var stage = StageAdvancementViewModelFactory.Create(kind: "Survivor", tournamentStatus: "Cancelled", needsSquad: true);

        // Act
        var reason = SeriesDisplay.PendingReason(stage);

        // Assert
        reason.ShouldBe("This tournament was cancelled. Remove this Stage on the Edit page, then recompute from the Stage before it.");
    }

    [Fact(DisplayName = "PendingReason should ask for a squad when the Stage needs one")]
    public void PendingReason_ShouldAskForSquad_WhenNeeded()
    {
        // Arrange
        var stage = StageAdvancementViewModelFactory.Create(kind: "Placement", needsSquad: true);

        // Act
        var reason = SeriesDisplay.PendingReason(stage);

        // Assert
        reason.ShouldBe("Waiting for a squad. Pick the squad this round uses and the Stage computes.");
    }

    [Fact(DisplayName = "PendingReason should explain a Placement Stage")]
    public void PendingReason_ShouldExplainPlacement()
    {
        // Arrange
        var stage = StageAdvancementViewModelFactory.Create(kind: "Placement");

        // Act
        var reason = SeriesDisplay.PendingReason(stage);

        // Assert
        reason.ShouldBe("Positions appear when the Placement Round computes.");
    }

    [Theory(DisplayName = "PendingReason should give the default for a Qualifying or Survivor Stage")]
    [InlineData("Qualifying")]
    [InlineData("Survivor")]
    public void PendingReason_ShouldGiveDefault(string kind)
    {
        // Arrange
        var stage = StageAdvancementViewModelFactory.Create(kind: kind);

        // Act
        var reason = SeriesDisplay.PendingReason(stage);

        // Assert
        reason.ShouldBe("Not computed yet. This Stage computes when its tournament completes.");
    }

    // ---- Qualifying output ----

    [Fact(DisplayName = "SlotsLead should pluralize slots")]
    public void SlotsLead_ShouldPluralize()
    {
        // Arrange & Act
        var one = SeriesDisplay.SlotsLead(1);
        var many = SeriesDisplay.SlotsLead(8);

        // Assert
        one.ShouldBe("1 slot");
        many.ShouldBe("8 slots");
    }

    [Theory(DisplayName = "VacantLabel should say none, one or several vacant slots")]
    [InlineData(0, ", none vacant.")]
    [InlineData(1, ", 1 vacant slot. Vacant slots go to the Backfill.")]
    [InlineData(3, ", 3 vacant slots. Vacant slots go to the Backfill.")]
    public void VacantLabel_ShouldPluralize(int vacant, string expected)
    {
        // Arrange & Act
        var label = SeriesDisplay.VacantLabel(vacant);

        // Assert
        label.ShouldBe(expected);
    }

    [Theory(DisplayName = "EntriesHeading should mention the Backfill only for the last Qualifying Stage")]
    [InlineData(true, "Field entries from this Stage, then the Backfill")]
    [InlineData(false, "Field entries from this Stage")]
    public void EntriesHeading_ShouldReflectLastQualifying(bool isLastQualifying, string expected)
    {
        // Arrange & Act
        var heading = SeriesDisplay.EntriesHeading(isLastQualifying);

        // Assert
        heading.ShouldBe(expected);
    }

    [Theory(DisplayName = "OrdinalPlace should ordinalize a place and show an em dash for none")]
    [InlineData(null, "—")]
    [InlineData(1, "1st")]
    [InlineData(3, "3rd")]
    [InlineData(11, "11th")]
    [InlineData(12, "12th")]
    [InlineData(22, "22nd")]
    public void OrdinalPlace_ShouldOrdinalize(int? place, string expected)
    {
        // Arrange & Act
        var label = SeriesDisplay.OrdinalPlace(place);

        // Assert
        label.ShouldBe(expected);
    }

    [Theory(DisplayName = "ClearPlace should say the ordinal place and Backfill for none")]
    [InlineData(7, "7th place")]
    [InlineData(22, "22nd place")]
    [InlineData(null, "Backfill")]
    public void ClearPlace_ShouldDescribePlace(int? place, string expected)
    {
        // Arrange & Act
        var label = SeriesDisplay.ClearPlace(place);

        // Assert
        label.ShouldBe(expected);
    }

    [Fact(DisplayName = "EntrySource should say Added by admin for an override entry, even a Backfill-shaped one")]
    public void EntrySource_ShouldSayAdmin_ForOverride()
    {
        // Arrange
        var entry = FieldEntryViewModelFactory.Create(viaOverride: true);

        // Act
        var source = SeriesDisplay.EntrySource(entry);

        // Assert
        source.ShouldBe("Added by admin");
    }

    [Fact(DisplayName = "EntrySource should say Backfill for a Backfill entry")]
    public void EntrySource_ShouldSayBackfill()
    {
        // Arrange
        var entry = FieldEntryViewModelFactory.Create(isBackfill: true);

        // Act
        var source = SeriesDisplay.EntrySource(entry);

        // Assert
        source.ShouldBe("Backfill");
    }

    [Fact(DisplayName = "EntrySource should say Qualifier for a Stage entry")]
    public void EntrySource_ShouldSayQualifier()
    {
        // Arrange
        var entry = FieldEntryViewModelFactory.Create();

        // Act
        var source = SeriesDisplay.EntrySource(entry);

        // Assert
        source.ShouldBe("Qualifier");
    }

    [Fact(DisplayName = "SourceClass should name the admin class for an override entry")]
    public void SourceClass_ShouldNameAdmin_ForOverride()
    {
        // Arrange
        var entry = FieldEntryViewModelFactory.Create(viaOverride: true);

        // Act
        var css = SeriesDisplay.SourceClass(entry);

        // Assert
        css.ShouldBe("series-src--admin");
    }

    [Fact(DisplayName = "SourceClass should name the Backfill class for a Backfill entry")]
    public void SourceClass_ShouldNameBackfill()
    {
        // Arrange
        var entry = FieldEntryViewModelFactory.Create(isBackfill: true);

        // Act
        var css = SeriesDisplay.SourceClass(entry);

        // Assert
        css.ShouldBe("series-src--backfill");
    }

    [Fact(DisplayName = "SourceClass should be empty for a Qualifier entry")]
    public void SourceClass_ShouldBeEmpty_ForQualifier()
    {
        // Arrange
        var entry = FieldEntryViewModelFactory.Create();

        // Act
        var css = SeriesDisplay.SourceClass(entry);

        // Assert
        css.ShouldBeEmpty();
    }

    [Theory(DisplayName = "Points should show the number or an em dash")]
    [InlineData(120, "120")]
    [InlineData(1234, "1234")]
    [InlineData(null, "—")]
    public void Points_ShouldFormat(int? points, string expected)
    {
        // Arrange & Act
        var label = SeriesDisplay.Points(points);

        // Assert
        label.ShouldBe(expected);
    }

    [Theory(DisplayName = "BlockTotal should show the total or say there is no full block")]
    [InlineData(1200, "1200")]
    [InlineData(null, "No full block")]
    public void BlockTotal_ShouldFormat(int? total, string expected)
    {
        // Arrange & Act
        var label = SeriesDisplay.BlockTotal(total);

        // Assert
        label.ShouldBe(expected);
    }

    [Theory(DisplayName = "OverrideChange should name the change for each kind")]
    [InlineData("Add", "Add bowler")]
    [InlineData("Clear", "Clear slot")]
    public void OverrideChange_ShouldNameChange(string kind, string expected)
    {
        // Arrange & Act
        var label = SeriesDisplay.OverrideChange(kind);

        // Assert
        label.ShouldBe(expected);
    }

    [Theory(DisplayName = "OverrideStatus should say Applied or No longer applies")]
    [InlineData(true, "Applied")]
    [InlineData(false, "No longer applies")]
    public void OverrideStatus_ShouldReflectApplied(bool applied, string expected)
    {
        // Arrange & Act
        var label = SeriesDisplay.OverrideStatus(applied);

        // Assert
        label.ShouldBe(expected);
    }

    [Fact(DisplayName = "AddedDate should show month, day and year")]
    public void AddedDate_ShouldShowFullDate()
    {
        // Arrange
        var local = new DateTimeOffset(2025, 3, 23, 8, 0, 0, TimeSpan.FromHours(-4));

        // Act
        var label = SeriesDisplay.AddedDate(local);

        // Assert
        label.ShouldBe("3/23/2025");
    }

    // ---- Survivor and Placement output ----

    [Fact(DisplayName = "SurvivorSummary should count advancing, bowled and absent bowlers")]
    public void SurvivorSummary_ShouldCountBowlers()
    {
        // Arrange
        IReadOnlyList<SurvivorOutcomeViewModel> outcomes =
        [
            SurvivorOutcomeViewModelFactory.Create(bowlerId: "b1", blockTotal: 1200, advanced: true),
            SurvivorOutcomeViewModelFactory.Create(bowlerId: "b2", blockTotal: null, advanced: false),
            SurvivorOutcomeViewModelFactory.Create(bowlerId: "b3", blockTotal: 1100, advanced: true),
        ];

        // Act
        var line = SeriesDisplay.SurvivorSummary(outcomes);

        // Assert
        line.Lead.ShouldBe("2 advance");
        line.Detail.ShouldBe(" of 3 in the field. 2 bowled; 1 did not.");
    }

    [Fact(DisplayName = "SurvivorSummary should handle an empty field")]
    public void SurvivorSummary_ShouldHandleEmpty()
    {
        // Arrange & Act
        var line = SeriesDisplay.SurvivorSummary([]);

        // Assert
        line.Lead.ShouldBe("0 advance");
        line.Detail.ShouldBe(" of 0 in the field. 0 bowled; 0 did not.");
    }

    [Fact(DisplayName = "PlacementSummary should use the singular and say positions are final without a Finale")]
    public void PlacementSummary_ShouldUseSingular_WithoutFinale()
    {
        // Arrange & Act
        var line = SeriesDisplay.PlacementSummary(1, hasFinalists: false);

        // Assert
        line.Lead.ShouldBe("1 bowler ranked.");
        line.Detail.ShouldBe(" These positions are final.");
    }

    [Fact(DisplayName = "PlacementSummary should use the plural and say positions are provisional with a Finale")]
    public void PlacementSummary_ShouldUsePlural_WithFinale()
    {
        // Arrange & Act
        var line = SeriesDisplay.PlacementSummary(20, hasFinalists: true);

        // Assert
        line.Lead.ShouldBe("20 bowlers ranked.");
        line.Detail.ShouldBe(" Finalist positions are provisional until the Finale is recorded.");
    }

    // ---- Recompute ----

    [Fact(DisplayName = "RecomputeTitle should name the Stage")]
    public void RecomputeTitle_ShouldNameStage()
    {
        // Arrange & Act
        var title = SeriesDisplay.RecomputeTitle(3);

        // Assert
        title.ShouldBe("Recompute from Stage 3?");
    }

    [Theory(DisplayName = "StageWordList should join Stage numbers with commas and and")]
    [InlineData(new int[0], "")]
    [InlineData(new[] { 3 }, "3")]
    [InlineData(new[] { 3, 4 }, "3 and 4")]
    [InlineData(new[] { 1, 2, 3 }, "1, 2 and 3")]
    public void StageWordList_ShouldJoinOrders(int[] orders, string expected)
    {
        // Arrange & Act
        var list = SeriesDisplay.StageWordList(orders);

        // Assert
        list.ShouldBe(expected);
    }

    [Fact(DisplayName = "OfficialResetMessage should use the singular for one Stage")]
    public void OfficialResetMessage_ShouldUseSingular_ForOneStage()
    {
        // Arrange & Act
        var message = SeriesDisplay.OfficialResetMessage([3]);

        // Assert
        message.ShouldBe("Stage 3 is official. Recomputing resets it to Computed.");
    }

    [Fact(DisplayName = "OfficialResetMessage should use the plural for several Stages")]
    public void OfficialResetMessage_ShouldUsePlural_ForSeveralStages()
    {
        // Arrange & Act
        var message = SeriesDisplay.OfficialResetMessage([3, 4, 5]);

        // Assert
        message.ShouldBe("Stages 3, 4 and 5 are official. Recomputing resets them to Computed.");
    }

    [Fact(DisplayName = "RecomputedMessage should name the Stage")]
    public void RecomputedMessage_ShouldNameStage()
    {
        // Arrange & Act
        var message = SeriesDisplay.RecomputedMessage(3);

        // Assert
        message.ShouldBe("Recomputed from Stage 3.");
    }

    // ---- Overrides ----

    [Fact(DisplayName = "RemoveOverrideMessage should say the bowler leaves the field for an Add override")]
    public void RemoveOverrideMessage_ShouldSayLeaves_ForAdd()
    {
        // Arrange
        var item = StageOverrideViewModelFactory.Create(bowlerName: "Dave Smith", kind: "Add");

        // Act
        var message = SeriesDisplay.RemoveOverrideMessage(item, 2);

        // Assert
        message.ShouldBe("Dave Smith leaves the field for Stage 2.");
    }

    [Fact(DisplayName = "RemoveOverrideMessage should say the bowler takes the slot back for a Clear override")]
    public void RemoveOverrideMessage_ShouldSayTakesSlotBack_ForClear()
    {
        // Arrange
        var item = StageOverrideViewModelFactory.Create(bowlerName: "Dave Smith", kind: "Clear");

        // Act
        var message = SeriesDisplay.RemoveOverrideMessage(item, 2);

        // Assert
        message.ShouldBe("Dave Smith takes the slot back, and the Backfill no longer refills it.");
    }

    [Fact(DisplayName = "OverrideAddedMessage should name the bowler and the Stage")]
    public void OverrideAddedMessage_ShouldNameBowlerAndStage()
    {
        // Arrange & Act
        var message = SeriesDisplay.OverrideAddedMessage("Dave Smith", 2);

        // Assert
        message.ShouldBe("Dave Smith added to Stage 2.");
    }

    [Theory(DisplayName = "ConflictMessage should use the default for a null or blank description")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ConflictMessage_ShouldUseDefault_WhenBlank(string? description)
    {
        // Arrange & Act
        var message = SeriesDisplay.ConflictMessage(description);

        // Assert
        message.ShouldBe(SeriesDisplay.DefaultConflictMessage);
    }

    [Fact(DisplayName = "ConflictMessage should use the API text when it has some")]
    public void ConflictMessage_ShouldUseApiText()
    {
        // Arrange & Act
        var message = SeriesDisplay.ConflictMessage("Stage 2 is Official.");

        // Assert
        message.ShouldBe("Stage 2 is Official.");
    }

    [Theory(DisplayName = "OverrideTitle should name the dialog for each kind")]
    [InlineData("Add", "Add bowler")]
    [InlineData("Clear", "Clear slot")]
    public void OverrideTitle_ShouldNameDialog(string kind, string expected)
    {
        // Arrange & Act
        var title = SeriesDisplay.OverrideTitle(kind);

        // Assert
        title.ShouldBe(expected);
    }

    [Fact(DisplayName = "OverrideLead should explain an Add override")]
    public void OverrideLead_ShouldExplainAdd()
    {
        // Arrange & Act
        var lead = SeriesDisplay.OverrideLead("Add", 2, "Callahan's Bowl-O-Rama");

        // Assert
        lead.ShouldBe("Stage 2, Callahan's Bowl-O-Rama. Pick any bowler with a result in this tournament, whatever the place or membership. The field can then be larger than the target.");
    }

    [Fact(DisplayName = "OverrideLead should explain a Clear override")]
    public void OverrideLead_ShouldExplainClear()
    {
        // Arrange & Act
        var lead = SeriesDisplay.OverrideLead("Clear", 2, "Callahan's Bowl-O-Rama");

        // Assert
        lead.ShouldBe("Stage 2, Callahan's Bowl-O-Rama. The slot goes vacant and the Backfill fills it. This bowler can't come back through the Backfill.");
    }

    [Fact(DisplayName = "ResultOption should show the name and the ordinal place")]
    public void ResultOption_ShouldShowNameAndPlace()
    {
        // Arrange & Act
        var label = SeriesDisplay.ResultOption("Marcus Alvarez", 12);

        // Assert
        label.ShouldBe("Marcus Alvarez (12th)");
    }

    [Theory(DisplayName = "ReasonCount should show the length over the limit")]
    [InlineData(0, "0 / 500")]
    [InlineData(120, "120 / 500")]
    [InlineData(500, "500 / 500")]
    public void ReasonCount_ShouldShowLengthOverLimit(int length, string expected)
    {
        // Arrange & Act
        var label = SeriesDisplay.ReasonCount(length);

        // Assert
        label.ShouldBe(expected);
    }

    // ---- Squads ----

    [Fact(DisplayName = "SquadOption should show the start time and date")]
    public void SquadOption_ShouldShowTimeAndDate()
    {
        // Arrange & Act
        var label = SeriesDisplay.SquadOption(new DateOnly(2025, 7, 12), new TimeOnly(9, 30));

        // Assert
        label.ShouldBe("9:30 AM on 7/12");
    }

    [Theory(DisplayName = "SquadDetail should combine the score and current flags")]
    [InlineData(true, true, "Has scores · Current")]
    [InlineData(true, false, "Has scores")]
    [InlineData(false, true, "No scores yet · Current")]
    [InlineData(false, false, "No scores yet")]
    public void SquadDetail_ShouldCombineFlags(bool hasScores, bool isSelected, string expected)
    {
        // Arrange & Act
        var detail = SeriesDisplay.SquadDetail(hasScores, isSelected);

        // Assert
        detail.ShouldBe(expected);
    }

    [Theory(DisplayName = "SquadLead should name the expected squad and say whether to pick one")]
    [InlineData("9:00 AM squad on 7/12", false, "Stage 2, Callahan's Bowl-O-Rama. The Series expects a 9:00 AM squad on 7/12. Choose the squad this round uses.")]
    [InlineData("9:00 AM squad on 7/12", true, "Stage 2, Callahan's Bowl-O-Rama. The Series expects a 9:00 AM squad on 7/12. No squad matches it exactly, so pick the one this round uses.")]
    [InlineData(null, false, "Stage 2, Callahan's Bowl-O-Rama. The Series expects a squad. Choose the squad this round uses.")]
    [InlineData(null, true, "Stage 2, Callahan's Bowl-O-Rama. The Series expects a squad. No squad matches it exactly, so pick the one this round uses.")]
    public void SquadLead_ShouldDescribeExpectedSquad(string? expected, bool needsSquad, string lead)
    {
        // Arrange & Act
        var text = SeriesDisplay.SquadLead(2, "Callahan's Bowl-O-Rama", expected, needsSquad);

        // Assert
        text.ShouldBe(lead);
    }

    // ---- Locked editor ----

    [Fact(DisplayName = "LockedMessage should explain what is fixed")]
    public void LockedMessage_ShouldExplainFixedFields()
    {
        // Arrange & Act
        var message = SeriesDisplay.LockedMessage(false);

        // Assert
        message.ShouldBe("Processing has begun for this Series. Its Stages and whether it has a Finale are fixed. The name, the logo, and the Finale's date, time and center can still change.");
    }

    [Fact(DisplayName = "LockedMessage should add the removal note when a Stage can be removed")]
    public void LockedMessage_ShouldAddRemovalNote_WhenStageRemovable()
    {
        // Arrange & Act
        var message = SeriesDisplay.LockedMessage(true);

        // Assert
        message.ShouldBe("Processing has begun for this Series. Its Stages and whether it has a Finale are fixed. The name, the logo, and the Finale's date, time and center can still change."
            + " A Stage whose tournament was cancelled can still be removed. After you save, run Recompute from the Stage before it on the Series page.");
    }

    // ---- Constants ----

    [Fact(DisplayName = "The advancement messages should read as written")]
    public void Constants_ShouldReadAsWritten()
    {
        // Arrange & Act & Assert
        SeriesDisplay.DefaultConflictMessage.ShouldBe("This Series changed after you loaded it. Your change was not saved. Review the Stage and try again.");
        SeriesDisplay.AdvancementReloadFailed.ShouldBe("The Stage status could not refresh. Reload the page to see the latest.");
        SeriesDisplay.StaleNote.ShouldBe("An earlier Stage changed after this Stage was official. Its output has not changed. Recompute from here to refresh it.");
        SeriesDisplay.StaleHint.ShouldBe("An earlier Stage changed after this Stage was official.");
        SeriesDisplay.NoFieldEntries.ShouldBe("No bowlers in the field from this Stage.");
        SeriesDisplay.NoSquads.ShouldBe("No squads yet. They arrive when the desktop software sends this tournament's squads.");
        SeriesDisplay.SlotClearedMessage.ShouldBe("Slot cleared. The Backfill will refill it.");
    }

    // ---- Culture ----

    [Fact(DisplayName = "Advancement text should stay in English and US format under another current culture")]
    public void AdvancementText_ShouldIgnoreCurrentCulture()
    {
        // Arrange
        var original = CultureInfo.CurrentCulture;
        var originalUi = CultureInfo.CurrentUICulture;
        var stage = StageAdvancementViewModelFactory.Create(kind: "Qualifying", status: "Computed", slotCount: 8, vacantSlotCount: 1);

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");

            // Act
            var ordinal = SeriesDisplay.OrdinalPlace(22);
            var headline = SeriesDisplay.Headline(stage, 0);
            var due = SeriesDisplay.OfficialDueLabel(new DateTimeOffset(2025, 7, 12, 15, 15, 0, TimeSpan.FromHours(-4)));
            var points = SeriesDisplay.Points(12345);
            var option = SeriesDisplay.ResultOption("Dave Smith", 3);

            // Assert
            ordinal.ShouldBe("22nd");
            headline.ShouldBe("7 of 8 slots filled");
            due.ShouldBe("Becomes official about 3:15 PM on 7/12");
            points.ShouldBe("12345");
            option.ShouldBe("Dave Smith (3rd)");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
            CultureInfo.CurrentUICulture = originalUi;
        }
    }
}