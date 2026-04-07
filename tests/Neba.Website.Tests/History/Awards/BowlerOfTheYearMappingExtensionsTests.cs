using Neba.TestFactory.Attributes;
using Neba.TestFactory.Awards;
using Neba.Website.Server.History.Awards;

namespace Neba.Website.Tests.History.Awards;

[UnitTest]
[Component("Website.History.Awards.BowlerOfTheYearMappingExtensions")]
public sealed class BowlerOfTheYearMappingExtensionsTests
{
    [Fact(DisplayName = "Maps all fields from response to view model")]
    public async Task ToViewModels_ShouldMapAllFields()
    {
        var responses = BowlerOfTheYearAwardResponseFactory.Bogus(3, seed: 1);

        var viewModels = responses.ToViewModels();

        await Verify(viewModels);
    }

    [Fact(DisplayName = "Groups awards from the same season into one view model")]
    public void ToViewModels_ShouldGroupBySeason_WhenMultipleAwardsInSameSeason()
    {
        var open = BowlerOfTheYearAwardResponseFactory.Create(season: "2024 Season", bowlerName: "Alice", category: "Open");
        var senior = BowlerOfTheYearAwardResponseFactory.Create(season: "2024 Season", bowlerName: "Bob", category: "Senior");

        var result = new[] { open, senior }.ToViewModels();

        result.ShouldHaveSingleItem();
        result[0].WinnersByCategory.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "Produces one view model per distinct season")]
    public void ToViewModels_ShouldProduceOneEntryPerSeason_WhenMultipleSeasons()
    {
        var award2024 = BowlerOfTheYearAwardResponseFactory.Create(season: "2024 Season");
        var award2023 = BowlerOfTheYearAwardResponseFactory.Create(season: "2023 Season");

        var result = new[] { award2024, award2023 }.ToViewModels();

        result.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "Maps Open category to Bowler of the Year display label")]
    public void ToViewModels_ShouldMapOpenToBotyDisplayLabel()
    {
        var award = BowlerOfTheYearAwardResponseFactory.Create(bowlerName: "Jane Smith", category: "Open");

        var result = new[] { award }.ToViewModels();

        var entry = result[0].WinnersByCategory.Single();
        entry.Key.ShouldBe("Bowler of the Year");
        entry.Value.ShouldBe("Jane Smith");
    }

    [Theory(DisplayName = "Passes non-Open category names through unchanged")]
    [InlineData("Senior")]
    [InlineData("Super Senior")]
    [InlineData("Woman")]
    [InlineData("Rookie")]
    [InlineData("Youth")]
    public void ToViewModels_ShouldPassNonOpenCategoriesThrough(string category)
    {
        var award = BowlerOfTheYearAwardResponseFactory.Create(bowlerName: "Jane Smith", category: category);

        var result = new[] { award }.ToViewModels();

        result[0].WinnersByCategory.Single().Key.ShouldBe(category);
    }

    [Fact(DisplayName = "Orders seasons descending so newest appears first")]
    public void ToViewModels_ShouldOrderSeasonsDescending()
    {
        var award2022 = BowlerOfTheYearAwardResponseFactory.Create(season: "2022 Season");
        var award2024 = BowlerOfTheYearAwardResponseFactory.Create(season: "2024 Season");
        var award2023 = BowlerOfTheYearAwardResponseFactory.Create(season: "2023 Season");

        var result = new[] { award2022, award2024, award2023 }.ToViewModels();

        result[0].Season.ShouldBe("2024 Season");
        result[1].Season.ShouldBe("2023 Season");
        result[2].Season.ShouldBe("2022 Season");
    }

    [Fact(DisplayName = "Orders Open before Senior within a season")]
    public void ToViewModels_ShouldOrderOpenBeforeSenior_WithinSeason()
    {
        var senior = BowlerOfTheYearAwardResponseFactory.Create(season: "2024 Season", bowlerName: "Bob", category: "Senior");
        var open = BowlerOfTheYearAwardResponseFactory.Create(season: "2024 Season", bowlerName: "Alice", category: "Open");

        var result = new[] { senior, open }.ToViewModels();

        result[0].WinnersByCategory[0].Key.ShouldBe("Bowler of the Year");
        result[0].WinnersByCategory[1].Key.ShouldBe("Senior");
    }

    [Fact(DisplayName = "Returns empty list when input is empty")]
    public void ToViewModels_ShouldReturnEmpty_WhenInputIsEmpty()
    {
        var result = Array.Empty<Neba.Api.Contracts.Awards.BowlerOfTheYearAwardResponse>().ToViewModels();

        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Maps season string directly from response")]
    public void ToViewModels_ShouldMapSeasonFromResponse()
    {
        var award = BowlerOfTheYearAwardResponseFactory.Create(season: "2020 - 2021 Season");

        var result = new[] { award }.ToViewModels();

        result[0].Season.ShouldBe("2020 - 2021 Season");
    }
}