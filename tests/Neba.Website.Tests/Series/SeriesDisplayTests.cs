using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;
using Neba.Website.Server.Series;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesDisplayTests
{
    [Theory(DisplayName = "Advancing should label each Stage kind from its count and team size")]
    [InlineData("Qualifying", 8, 1, null, "Top 8", null)]
    [InlineData("Qualifying", 2, 3, null, "Top 2 teams", "(6 bowlers)")]
    [InlineData("Survivor", 40, 1, null, "40 advance", null)]
    [InlineData("Placement", null, 1, 4, "Top 4 to the Finale", null)]
    [InlineData("Placement", null, 1, null, "Final standings", "(no Finale)")]
    public void Advancing_ShouldLabelStage(string kind, int? count, int teamSize, int? finaleCount, string primary, string? secondary)
    {
        // Arrange & Act
        var label = SeriesDisplay.Advancing(kind, count, teamSize, finaleCount);

        // Assert
        label.Primary.ShouldBe(primary);
        label.Secondary.ShouldBe(secondary);
    }

    [Fact(DisplayName = "Advancing should show a dash when a Qualifying Stage has no count")]
    public void Advancing_ShouldShowDash_WhenCountMissing()
    {
        // Arrange & Act
        var label = SeriesDisplay.Advancing("Qualifying", null, 1);

        // Assert
        label.Primary.ShouldBe("—");
    }

    [Fact(DisplayName = "TeamHint should show the team size and bowler count for a Qualifying team row")]
    public void TeamHint_ShouldShowBowlers_ForTeamRow()
    {
        // Arrange & Act
        var hint = SeriesDisplay.TeamHint("Qualifying", 2, 3);

        // Assert
        hint.ShouldNotBeNull();
        hint.Primary.ShouldBe("Teams of 3");
        hint.Secondary.ShouldBe("(6 bowlers)");
    }

    [Fact(DisplayName = "TeamHint should leave out the bowler count when the count is empty")]
    public void TeamHint_ShouldOmitBowlers_WhenCountEmpty()
    {
        // Arrange & Act
        var hint = SeriesDisplay.TeamHint("Qualifying", null, 3);

        // Assert
        hint.ShouldNotBeNull();
        hint.Secondary.ShouldBeNull();
    }

    [Theory(DisplayName = "TeamHint should be null for singles and for other kinds")]
    [InlineData("Qualifying", 1)]
    [InlineData("Survivor", 3)]
    public void TeamHint_ShouldBeNull(string kind, int teamSize)
    {
        // Arrange & Act
        var hint = SeriesDisplay.TeamHint(kind, 8, teamSize);

        // Assert
        hint.ShouldBeNull();
    }

    [Theory(DisplayName = "Initials should take the first letters of the first two words that start with a letter")]
    [InlineData("2025 Vincent Hall Memorial Series", "VH")]
    [InlineData("Spring Open", "SO")]
    [InlineData("Solo", "S")]
    public void Initials_ShouldSkipLeadingYear(string name, string expected)
    {
        // Arrange & Act
        var initials = SeriesDisplay.Initials(name);

        // Assert
        initials.ShouldBe(expected);
    }

    [Fact(DisplayName = "FinaleWhen should read like 'Sat 9/27 at 2:00 PM'")]
    public void FinaleWhen_ShouldFormatDateAndTime()
    {
        // Arrange & Act
        var when = SeriesDisplay.FinaleWhen(new DateOnly(2025, 9, 27), new TimeOnly(14, 0));

        // Assert
        when.ShouldBe("Sat 9/27 at 2:00 PM");
    }

    [Theory(DisplayName = "Host should join the parts that exist and be null with no center")]
    [InlineData("Callahan's Bowl-O-Rama", "Seekonk", "MA", "Callahan's Bowl-O-Rama, Seekonk, MA")]
    [InlineData("Callahan's Bowl-O-Rama", null, null, "Callahan's Bowl-O-Rama")]
    [InlineData(null, "Seekonk", "MA", null)]
    public void Host_ShouldJoinParts(string? name, string? city, string? state, string? expected)
    {
        // Arrange & Act
        var host = SeriesDisplay.Host(name, city, state);

        // Assert
        host.ShouldBe(expected);
    }

    [Fact(DisplayName = "FinaleName should add 'Finale' to the Series name")]
    public void FinaleName_ShouldAppendFinale()
    {
        // Arrange & Act
        var name = SeriesDisplay.FinaleName("Vincent Hall Memorial Series");

        // Assert
        name.ShouldBe("Vincent Hall Memorial Series Finale");
    }

    [Fact(DisplayName = "FinaleCaption should name the Placement Round")]
    public void FinaleCaption_ShouldNamePlacementRound()
    {
        // Arrange & Act
        var caption = SeriesDisplay.FinaleCaption(4);

        // Assert
        caption.ShouldBe("Top 4 of the Placement Round");
    }

    [Fact(DisplayName = "Squad should read like '9:00 AM squad on 7/12'")]
    public void Squad_ShouldFormat()
    {
        // Arrange & Act
        var squad = SeriesDisplay.Squad(new DateOnly(2025, 7, 12), new TimeOnly(9, 0));

        // Assert
        squad.ShouldBe("9:00 AM squad on 7/12");
    }

    [Theory(DisplayName = "Squad should be null when the date or time is missing")]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void Squad_ShouldBeNull_WhenPartMissing(bool hasDate, bool hasTime)
    {
        // Arrange
        DateOnly? date = hasDate ? new DateOnly(2025, 7, 12) : null;
        TimeOnly? time = hasTime ? new TimeOnly(9, 0) : null;

        // Act
        var squad = SeriesDisplay.Squad(date, time);

        // Assert
        squad.ShouldBeNull();
    }

    [Fact(DisplayName = "TournamentDates should show one date for a one-day tournament")]
    public void TournamentDates_ShouldShowOneDate_ForOneDay()
    {
        // Arrange & Act
        var dates = SeriesDisplay.TournamentDates(new DateOnly(2025, 2, 21), new DateOnly(2025, 2, 21));

        // Assert
        dates.ShouldBe("2/21");
    }

    [Fact(DisplayName = "TournamentDates should show a range for a multi-day tournament")]
    public void TournamentDates_ShouldShowRange_ForMultipleDays()
    {
        // Arrange & Act
        var dates = SeriesDisplay.TournamentDates(new DateOnly(2025, 1, 24), new DateOnly(2025, 1, 25));

        // Assert
        dates.ShouldBe("1/24 to 1/25");
    }

    [Fact(DisplayName = "SeasonRange should read like '1/1/2025 to 12/31/2025'")]
    public void SeasonRange_ShouldFormat()
    {
        // Arrange & Act
        var range = SeriesDisplay.SeasonRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));

        // Assert
        range.ShouldBe("1/1/2025 to 12/31/2025");
    }

    [Fact(DisplayName = "QualifierCeiling should be 62 for the 2025 configuration")]
    public void QualifierCeiling_ShouldBe62_For2025()
    {
        // Arrange
        var detail = SeriesDetailViewModelFactory.Reference2025();

        // Act
        var ceiling = SeriesDisplay.QualifierCeiling(detail.Stages);

        // Assert
        ceiling.ShouldBe(62);
    }

    [Fact(DisplayName = "FieldSteps should list 62, 40, 20 and 4 for the 2025 configuration")]
    public void FieldSteps_ShouldListFullField_For2025()
    {
        // Arrange
        var detail = SeriesDetailViewModelFactory.Reference2025();

        // Act
        var steps = SeriesDisplay.FieldSteps(detail.Stages, detail.Finale);

        // Assert
        steps.Select(s => s.Count).ShouldBe([62, 40, 20, 4]);
        steps[0].IsCeiling.ShouldBeTrue();
        steps.Skip(1).ShouldAllBe(s => !s.IsCeiling);
        steps[0].Label.ShouldBe("Qualifiers");
        steps[0].Detail.ShouldBe("8 events, top finishers");
    }

    [Fact(DisplayName = "FieldSteps should read the Finale date and time")]
    public void FieldSteps_ShouldReadFinaleWhen()
    {
        // Arrange
        var detail = SeriesDetailViewModelFactory.Reference2025();

        // Act
        var steps = SeriesDisplay.FieldSteps(detail.Stages, detail.Finale);

        // Assert
        steps[^1].Label.ShouldBe("Finale");
        steps[^1].Detail.ShouldBe("Sat 9/27 at 2:00 PM");
    }

    [Fact(DisplayName = "FieldSteps should have no Finale step when the Series has no Finale")]
    public void FieldSteps_ShouldOmitFinale_WhenNoFinale()
    {
        // Arrange
        var detail = SeriesDetailViewModelFactory.Reference2025(withFinale: false);

        // Act
        var steps = SeriesDisplay.FieldSteps(detail.Stages, detail.Finale);

        // Assert
        steps.Select(s => s.Count).ShouldBe([62, 40, 20]);
    }

    [Fact(DisplayName = "FieldSteps should return only the Qualifying step when there are no other Stages")]
    public void FieldSteps_ShouldReturnOnlyQualifying_WhenNothingElse()
    {
        // Arrange
        IReadOnlyList<Website.Server.Series.SeriesStageViewModel> stages = [SeriesStageViewModelFactory.Create(advancingCount: 8)];

        // Act
        var steps = SeriesDisplay.FieldSteps(stages, null);

        // Assert
        steps.Count.ShouldBe(1);
        steps[0].Kind.ShouldBe("Qualifying");
    }

    [Theory(DisplayName = "BarWidthStyle should scale to the largest step and never go below 8 percent")]
    [InlineData(40, 62, "width:64%")]
    [InlineData(4, 62, "width:8%")]
    [InlineData(62, 62, "width:100%")]
    [InlineData(0, 0, "width:8%")]
    public void BarWidthStyle_ShouldScaleWithFloor(int count, int max, string expected)
    {
        // Arrange & Act
        var style = SeriesDisplay.BarWidthStyle(count, max);

        // Assert
        style.ShouldBe(expected);
    }

    [Theory(DisplayName = "KindClass should map each kind to its style class")]
    [InlineData("Qualifying", "kind-qualifying")]
    [InlineData("Survivor", "kind-survivor")]
    [InlineData("Placement", "kind-placement")]
    public void KindClass_ShouldMapKind(string kind, string expected)
    {
        // Arrange & Act
        var cssClass = SeriesDisplay.KindClass(kind);

        // Assert
        cssClass.ShouldBe(expected);
    }

    [Theory(DisplayName = "RowsNeedChanges should use the singular for one row and the plural for more")]
    [InlineData(1, "Changes are still needed in 1 tournament before this Series can be saved.", TestDisplayName = "one tournament")]
    [InlineData(2, "Changes are still needed in 2 tournaments before this Series can be saved.", TestDisplayName = "two tournaments")]
    public void RowsNeedChanges_ShouldAgreeWithCount(int count, string expected)
    {
        // Arrange

        // Act
        var text = SeriesDisplay.RowsNeedChanges(count);

        // Assert
        text.ShouldBe(expected);
    }

    [Theory(DisplayName = "GroupHeading and GroupNote should describe each kind")]
    [InlineData("Qualifying", "Qualifying events", "Top finishers enter the Series")]
    [InlineData("Survivor", "Survivor rounds", "Cut by the designated squad score")]
    [InlineData("Placement", "Placement round", "Ranked by tournament position")]
    public void Group_ShouldDescribeKind(string kind, string heading, string note)
    {
        // Arrange & Act & Assert
        SeriesDisplay.GroupHeading(kind).ShouldBe(heading);
        SeriesDisplay.GroupNote(kind).ShouldBe(note);
    }

    [Fact(DisplayName = "TournamentOption should read like 'Sat 7/12 · Name'")]
    public void TournamentOption_ShouldFormat()
    {
        // Arrange & Act
        var label = SeriesDisplay.TournamentOption(new DateOnly(2025, 7, 12), "Callahan's Bowl-O-Rama");

        // Assert
        label.ShouldBe("Sat 7/12 · Callahan's Bowl-O-Rama");
    }

    [Theory(DisplayName = "RoundLabel should append 'Round' to the kind")]
    [InlineData("Qualifying", "Qualifying Round")]
    [InlineData("Survivor", "Survivor Round")]
    [InlineData("Placement", "Placement Round")]
    public void RoundLabel_ShouldAppendRound(string kind, string expected)
    {
        // Arrange & Act
        var label = SeriesDisplay.RoundLabel(kind);

        // Assert
        label.ShouldBe(expected);
    }

    [Theory(DisplayName = "CalloutRule should say what each round decides")]
    [InlineData("Qualifying", 8, 1, false, null, false, "Top 8 finishers enter the Series field.", null)]
    [InlineData("Qualifying", 2, 3, false, null, false, "Top 2 teams enter the Series field.", "(6 bowlers)")]
    [InlineData("Survivor", 40, 1, true, null, false, "40 bowlers advance.", "Series squad: 9:00 AM on 7/12.")]
    [InlineData("Survivor", 40, 1, false, null, false, "40 bowlers advance.", null)]
    [InlineData("Placement", null, 1, false, 4, true, "Final standings. The top 4 go to the Finale.", "Finale: Sat 9/27 at 2:00 PM.")]
    [InlineData("Placement", null, 1, false, 4, false, "Final standings. The top 4 go to the Finale.", null)]
    [InlineData("Placement", null, 1, false, null, false, "Final standings (no Finale).", null)]
    public void CalloutRule_ShouldDescribeRound(
        string kind, int? count, int teamSize, bool hasSquad, int? finaleCount, bool hasFinaleTime,
        string primary, string? secondary)
    {
        // Arrange
        DateOnly? squadDate = hasSquad ? new DateOnly(2025, 7, 12) : null;
        TimeOnly? squadTime = hasSquad ? new TimeOnly(9, 0) : null;
        DateOnly? finaleDate = finaleCount is not null ? new DateOnly(2025, 9, 27) : null;
        TimeOnly? finaleTime = hasFinaleTime ? new TimeOnly(14, 0) : null;

        // Act
        var rule = SeriesDisplay.CalloutRule(
            kind, count, teamSize, new SquadSlot(squadDate, squadTime), new FinaleSlot(finaleCount, finaleDate, finaleTime));

        // Assert
        rule.Primary.ShouldBe(primary);
        rule.Secondary.ShouldBe(secondary);
    }

    [Fact(DisplayName = "CalloutRule should fall back when a Qualifying Stage has no count")]
    public void CalloutRule_ShouldFallBack_WhenNoCount()
    {
        // Arrange & Act
        var rule = SeriesDisplay.CalloutRule("Qualifying", null, 1, new SquadSlot(null, null), new FinaleSlot(null, null, null));

        // Assert
        rule.Primary.ShouldBe("Part of the Series.");
        rule.Secondary.ShouldBeNull();
    }
}