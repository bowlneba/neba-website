using Neba.Api.Features.Tournaments;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments;

[UnitTest]
[Component("Tournaments")]
public sealed class TournamentSeriesDtoMappingTests
{
    [Fact(DisplayName = "ToResponse should copy every field and turn the id into its string")]
    public void ToResponse_ShouldCopyEveryField()
    {
        // Arrange
        var logo = new Uri("https://storage.example.com/series/logo.png");
        var dto = TournamentSeriesDtoFactory.Create(
            name: "VH", logoUrl: logo, kind: "Placement", advancingCount: null,
            squadDate: new DateOnly(2025, 7, 12), squadStartTime: new TimeOnly(9, 0),
            finaleCount: 4, finaleDate: new DateOnly(2025, 9, 27), finaleStartTime: new TimeOnly(14, 0));

        // Act
        var response = dto.ToResponse();

        // Assert
        response.SeriesId.ShouldBe(dto.SeriesId.Value.ToString());
        response.Name.ShouldBe("VH");
        response.LogoUrl.ShouldBe(logo);
        response.Kind.ShouldBe("Placement");
        response.AdvancingCount.ShouldBeNull();
        response.SquadDate.ShouldBe(new DateOnly(2025, 7, 12));
        response.SquadStartTime.ShouldBe(new TimeOnly(9, 0));
        response.FinaleCount.ShouldBe(4);
        response.FinaleDate.ShouldBe(new DateOnly(2025, 9, 27));
        response.FinaleStartTime.ShouldBe(new TimeOnly(14, 0));
    }

    [Fact(DisplayName = "SeasonTournamentDto ToResponse should map Series, and leave it null when there is none")]
    public void SeasonTournamentToResponse_ShouldMapSeries()
    {
        // Arrange
        var withSeries = SeasonTournamentDtoFactory.Create(series: TournamentSeriesDtoFactory.Create(name: "VH"));
        var without = SeasonTournamentDtoFactory.Create();

        // Act & Assert
        withSeries.ToResponse().Series!.Name.ShouldBe("VH");
        without.ToResponse().Series.ShouldBeNull();
    }
}