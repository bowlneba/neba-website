using Neba.Api.Features.Series;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesStageInputMapperTests
{
    [Fact(DisplayName = "ToLogo should return null for no logo and copy every field otherwise")]
    public void ToLogo_ShouldMapLogo()
    {
        // Arrange
        var input = SeriesLogoInputFactory.Create(container: "c", path: "p.png", contentType: "image/png", sizeInBytes: 7);

        // Act
        var logo = SeriesStageInputMapper.ToLogo(input);

        // Assert
        SeriesStageInputMapper.ToLogo(null).ShouldBeNull();
        logo!.Container.ShouldBe("c");
        logo.Path.ShouldBe("p.png");
        logo.ContentType.ShouldBe("image/png");
        logo.SizeInBytes.ShouldBe(7);
    }

    [Fact(DisplayName = "ToFinale should return null for no Finale and map the center only when one is given")]
    public void ToFinale_ShouldMapFinale()
    {
        // Arrange
        var withCenter = SeriesFinaleInputFactory.Create(finalistCount: 6, bowlingCenterId: "01948");
        var blankCenter = SeriesFinaleInputFactory.Create(bowlingCenterId: "  ");

        // Act
        var mapped = SeriesStageInputMapper.ToFinale(withCenter);
        var blank = SeriesStageInputMapper.ToFinale(blankCenter);

        // Assert
        SeriesStageInputMapper.ToFinale(null).ShouldBeNull();
        mapped!.FinalistCount.ShouldBe(6);
        mapped.Date.ShouldBe(SeriesFinaleInputFactory.ValidDate);
        mapped.StartTime.ShouldBe(SeriesFinaleInputFactory.ValidStartTime);
        mapped.BowlingCenterId!.Value.ShouldBe("01948");
        blank!.BowlingCenterId.ShouldBeNull();
    }

    [Fact(DisplayName = "ToDefinitions should map kind, ids, count and squad for each Stage in order")]
    public void ToDefinitions_ShouldMapStages()
    {
        // Arrange
        var stageId = SeriesStageId.New().Value.ToString();
        var tournamentId = new string('1', 26);
        IReadOnlyList<Neba.Api.Contracts.Series.CreateSeries.SeriesStageInput> inputs =
        [
            SeriesStageInputFactory.Create(id: stageId, kind: "Survivor", tournamentId: tournamentId, advancingCount: 20,
                squadDate: new DateOnly(2025, 7, 12), squadStartTime: new TimeOnly(9, 0)),
            SeriesStageInputFactory.Create(kind: "Placement", advancingCount: null),
        ];

        // Act
        var definitions = SeriesStageInputMapper.ToDefinitions(inputs);

        // Assert
        definitions.Count.ShouldBe(2);
        definitions[0].StageId.ShouldNotBeNull();
        definitions[0].StageId!.Value.Value.ToString().ShouldBe(stageId);
        definitions[0].Kind.ShouldBe(StageKind.Survivor);
        definitions[0].AdvancingCount.ShouldBe(20);
        definitions[0].SquadDate.ShouldBe(new DateOnly(2025, 7, 12));
        definitions[1].StageId.ShouldBeNull();
        definitions[1].Kind.ShouldBe(StageKind.Placement);
        definitions[1].AdvancingCount.ShouldBeNull();
    }
}