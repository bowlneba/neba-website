using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.TestFactory.Series;

[UnitTest]
[Component("Series")]
public sealed class MakeStageOfficialFactoryTests
{
    [Fact(DisplayName = "MakeStageOfficialRequestFactory Create should return ULID ids and the valid Version with no arguments")]
    public void MakeStageOfficialRequestFactory_Create_ShouldReturnDefaults()
    {
        // Arrange & Act
        var request = MakeStageOfficialRequestFactory.Create();

        // Assert
        request.Id.Length.ShouldBe(26);
        request.StageId.Length.ShouldBe(26);
        request.Official.Version.ShouldBe(MakeStageOfficialInputFactory.ValidVersion);
    }

    [Fact(DisplayName = "MakeStageOfficialRequestFactory Create should use the given values")]
    public void MakeStageOfficialRequestFactory_Create_ShouldUseGivenValues()
    {
        // Arrange & Act
        var request = MakeStageOfficialRequestFactory.Create("series", "stage", MakeStageOfficialInputFactory.Create(9));

        // Assert
        request.Id.ShouldBe("series");
        request.StageId.ShouldBe("stage");
        request.Official.Version.ShouldBe(9);
    }

    [Fact(DisplayName = "MakeStageOfficialCommandFactory Create should return the valid Version with no arguments and use the given values")]
    public void MakeStageOfficialCommandFactory_Create_ShouldReturnDefaultsAndGivenValues()
    {
        // Arrange
        var seriesId = SeriesId.New();
        var stageId = SeriesStageId.New();

        // Act
        var defaults = MakeStageOfficialCommandFactory.Create();
        var given = MakeStageOfficialCommandFactory.Create(seriesId, stageId, 8);

        // Assert
        defaults.Version.ShouldBe(MakeStageOfficialCommandFactory.ValidVersion);
        given.SeriesId.ShouldBe(seriesId);
        given.StageId.ShouldBe(stageId);
        given.Version.ShouldBe(8);
    }

    [Fact(DisplayName = "MakeStageOfficial factories Bogus should return the requested count")]
    public void MakeStageOfficialFactories_Bogus_ShouldReturnRequestedCount()
    {
        // Arrange & Act
        var inputs = MakeStageOfficialInputFactory.Bogus(2, 41);
        var requests = MakeStageOfficialRequestFactory.Bogus(3, 42);
        var commands = MakeStageOfficialCommandFactory.Bogus(4, 43);

        // Assert
        inputs.Count.ShouldBe(2);
        requests.Count.ShouldBe(3);
        commands.Count.ShouldBe(4);
    }
}
