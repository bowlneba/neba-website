using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;
using Neba.Website.Server.Series;
using Neba.Website.Server.Services;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesInputMappingTests
{
    private static SeriesFormModel NewModel(params SeriesStageRowModel[] rows)
    {
        var model = new SeriesFormModel { Name = "  Spring Series  ", SeasonId = "season-1" };
        model.Stages.AddRange(rows);
        return model;
    }

    [Fact(DisplayName = "StageKinds.ToValue should map an unknown kind to 0 instead of throwing")]
    public void ToValue_ShouldReturnZero_WhenKindIsUnknown()
    {
        // Arrange
        const string kind = "Unknown";

        // Act
        var value = StageKinds.ToValue(kind);

        // Assert
        value.ShouldBe(0);
    }

    [Fact(DisplayName = "ToCreateInput should trim the name and keep row order")]
    public void ToCreateInput_ShouldKeepRowOrder()
    {
        // Arrange
        var model = NewModel(
            new SeriesStageRowModel { Kind = StageKinds.Qualifying, TournamentId = "t1" },
            new SeriesStageRowModel { Kind = StageKinds.Qualifying, TournamentId = "t2" },
            new SeriesStageRowModel { Kind = StageKinds.Qualifying, TournamentId = "t3" });

        // Act
        var input = SeriesInputMapping.ToCreateInput(model);

        // Assert
        input.Name.ShouldBe("Spring Series");
        input.SeasonId.ShouldBe("season-1");
        input.Stages.Select(s => s.TournamentId).ShouldBe(["t1", "t2", "t3"]);
    }

    [Fact(DisplayName = "ToEditInput should carry Stage ids and the logo")]
    public void ToEditInput_ShouldCarryStageIdsAndLogo()
    {
        // Arrange
        var logo = SeriesLogoInputFactory.Create();
        var model = NewModel(new SeriesStageRowModel { StageId = "stage-1", Kind = StageKinds.Qualifying, TournamentId = "t1" });
        model.Logo = logo;

        // Act
        var input = SeriesInputMapping.ToEditInput(model);

        // Assert
        input.Stages[0].Id.ShouldBe("stage-1");
        input.Logo.ShouldBe(logo);
    }

    [Fact(DisplayName = "ToCreateInput should send the squad only for Survivor rows")]
    public void ToCreateInput_ShouldStripSquadFromOtherKinds()
    {
        // Arrange
        var date = new DateOnly(2025, 7, 12);
        var time = new TimeOnly(9, 0);
        var model = NewModel(
            new SeriesStageRowModel { Kind = StageKinds.Qualifying, TournamentId = "t1", SquadDate = date, SquadStartTime = time },
            new SeriesStageRowModel { Kind = StageKinds.Survivor, TournamentId = "t2", SquadDate = date, SquadStartTime = time });

        // Act
        var input = SeriesInputMapping.ToCreateInput(model);

        // Assert
        input.Stages[0].SquadDate.ShouldBeNull();
        input.Stages[0].SquadStartTime.ShouldBeNull();
        input.Stages[1].SquadDate.ShouldBe(date);
        input.Stages[1].SquadStartTime.ShouldBe(time);
    }

    [Fact(DisplayName = "ToCreateInput should never send a count for a Placement row")]
    public void ToCreateInput_ShouldNotSendPlacementCount()
    {
        // Arrange
        var model = NewModel(new SeriesStageRowModel { Kind = StageKinds.Placement, TournamentId = "t1", AdvancingCount = 8 });

        // Act
        var input = SeriesInputMapping.ToCreateInput(model);

        // Assert
        input.Stages[0].AdvancingCount.ShouldBeNull();
    }

    [Fact(DisplayName = "ToCreateInput should leave the Finale null when the form has none")]
    public void ToCreateInput_ShouldKeepNullFinale()
    {
        // Arrange
        var model = NewModel();

        // Act
        var input = SeriesInputMapping.ToCreateInput(model);

        // Assert
        input.Finale.ShouldBeNull();
    }

    [Fact(DisplayName = "ToCreateInput should map the Finale, sending an empty count as 0 and an empty host as null")]
    public void ToCreateInput_ShouldMapFinale()
    {
        // Arrange
        var model = NewModel();
        model.Finale = new SeriesFinaleFormModel
        {
            FinalistCount = null,
            Date = new DateOnly(2025, 9, 27),
            StartTime = new TimeOnly(14, 0),
            BowlingCenterId = " ",
        };

        // Act
        var input = SeriesInputMapping.ToCreateInput(model);

        // Assert
        var finale = input.Finale.ShouldNotBeNull();
        finale.FinalistCount.ShouldBe(0);
        finale.Date.ShouldBe(new DateOnly(2025, 9, 27));
        finale.StartTime.ShouldBe(new TimeOnly(14, 0));
        finale.BowlingCenterId.ShouldBeNull();
    }

    [Fact(DisplayName = "FromDetail should keep Stage ids and fill the Finale")]
    public void FromDetail_ShouldKeepStageIdsAndFillFinale()
    {
        // Arrange
        var detail = SeriesDetailViewModelFactory.Reference2025();

        // Act
        var model = SeriesInputMapping.FromDetail(detail);

        // Assert
        model.Name.ShouldBe(detail.Name);
        model.SeasonId.ShouldBe(detail.SeasonId);
        model.Stages.Select(s => s.StageId).ShouldBe(detail.Stages.Select(s => s.Id));
        var finale = model.Finale.ShouldNotBeNull();
        finale.FinalistCount.ShouldBe(4);
        finale.Date.ShouldBe(new DateOnly(2025, 9, 27));
    }

    [Fact(DisplayName = "FromDetail should leave the Finale null when the Series has none")]
    public void FromDetail_ShouldLeaveFinaleNull()
    {
        // Arrange
        var detail = SeriesDetailViewModelFactory.Reference2025(withFinale: false);

        // Act
        var model = SeriesInputMapping.FromDetail(detail);

        // Assert
        model.Finale.ShouldBeNull();
    }

    [Theory(DisplayName = "ApplyFieldErrors should put Stage errors on the matching row")]
    [InlineData("series.Stages[1]")]
    [InlineData("series.Stages[1].AdvancingCount")]
    [InlineData("Series.Stages[1]")]
    public void ApplyFieldErrors_ShouldRouteStageErrorsToRow(string name)
    {
        // Arrange
        var model = NewModel(new SeriesStageRowModel(), new SeriesStageRowModel(), new SeriesStageRowModel());

        // Act
        var general = SeriesInputMapping.ApplyFieldErrors(model, [new ApiFieldError(name, "Bad row.")]);

        // Assert
        model.Stages[1].Errors.ShouldBe(["Bad row."]);
        model.Stages[0].Errors.ShouldBeEmpty();
        general.ShouldBeEmpty();
    }

    [Fact(DisplayName = "ApplyFieldErrors should put Finale errors on the Finale section")]
    public void ApplyFieldErrors_ShouldRouteFinaleErrors()
    {
        // Arrange
        var model = NewModel();
        model.Finale = new SeriesFinaleFormModel();

        // Act
        var general = SeriesInputMapping.ApplyFieldErrors(model, [new ApiFieldError("series.Finale.Date", "Finale date is wrong.")]);

        // Assert
        model.Finale.Errors.ShouldBe(["Finale date is wrong."]);
        general.ShouldBeEmpty();
    }

    [Fact(DisplayName = "ApplyFieldErrors should return Series-level errors for the banner")]
    public void ApplyFieldErrors_ShouldReturnGeneralErrors()
    {
        // Arrange
        var model = NewModel(new SeriesStageRowModel());

        // Act
        var general = SeriesInputMapping.ApplyFieldErrors(model, [new ApiFieldError("series", "Series is wrong.")]);

        // Assert
        general.ShouldBe(["Series is wrong."]);
    }

    [Fact(DisplayName = "ApplyFieldErrors should send an out-of-range row index to the banner")]
    public void ApplyFieldErrors_ShouldSendOutOfRangeToBanner()
    {
        // Arrange
        var model = NewModel(new SeriesStageRowModel());

        // Act
        var general = SeriesInputMapping.ApplyFieldErrors(model, [new ApiFieldError("series.Stages[5]", "Gone.")]);

        // Assert
        general.ShouldBe(["Gone."]);
        model.Stages[0].Errors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "ApplyFieldErrors should clear earlier errors first")]
    public void ApplyFieldErrors_ShouldClearEarlierErrors()
    {
        // Arrange
        var row = new SeriesStageRowModel();
        row.Errors.Add("old");
        var model = NewModel(row);
        model.Finale = new SeriesFinaleFormModel();
        model.Finale.Errors.Add("old");

        // Act
        SeriesInputMapping.ApplyFieldErrors(model, []);

        // Assert
        row.Errors.ShouldBeEmpty();
        model.Finale.Errors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "ApplyFieldErrors should send Finale errors to the banner when the form has no Finale")]
    public void ApplyFieldErrors_ShouldSendFinaleErrorToBanner_WhenNoFinale()
    {
        // Arrange
        var model = NewModel();

        // Act
        var general = SeriesInputMapping.ApplyFieldErrors(model, [new ApiFieldError("series.Finale.Date", "Nope.")]);

        // Assert
        general.ShouldBe(["Nope."]);
    }
}