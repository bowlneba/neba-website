using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Website.Tests.Tournaments.Schedule;

[UnitTest]
[Component("Website.Tournaments.Schedule.SeasonViewModel")]
public sealed class SeasonViewModelTests
{
    [Fact(DisplayName = "Should derive label as year string for single-year season")]
    public void Label_ShouldBeYearString_WhenStartAndEndYearMatch()
    {
        var vm = SeasonViewModelFactory.Create(
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 12, 31));

        vm.Label.ShouldBe("2026");
    }

    [Fact(DisplayName = "Should derive label as YYYY-YY for multi-year season")]
    public void Label_ShouldUseHyphenFormat_WhenStartAndEndYearDiffer()
    {
        var vm = SeasonViewModelFactory.Create(
            startDate: new DateOnly(2020, 1, 1),
            endDate: new DateOnly(2021, 12, 31));

        vm.Label.ShouldBe("2020-21");
    }

    [Fact(DisplayName = "Should zero-pad two-digit suffix in multi-year label")]
    public void Label_ShouldZeroPadSuffix_WhenEndYearModuloIsLessThanTen()
    {
        var vm = SeasonViewModelFactory.Create(
            startDate: new DateOnly(2008, 1, 1),
            endDate: new DateOnly(2009, 12, 31));

        vm.Label.ShouldBe("2008-09");
    }

    [Fact(DisplayName = "Should not flag single-year season as merged")]
    public void IsMergedSeason_ShouldBeFalse_WhenStartAndEndYearMatch()
    {
        var vm = SeasonViewModelFactory.Create(
            startDate: new DateOnly(2026, 1, 1),
            endDate: new DateOnly(2026, 12, 31));

        vm.IsMergedSeason.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should flag multi-year season as merged")]
    public void IsMergedSeason_ShouldBeTrue_WhenStartAndEndYearDiffer()
    {
        var vm = SeasonViewModelFactory.Create(
            startDate: new DateOnly(2020, 1, 1),
            endDate: new DateOnly(2021, 12, 31));

        vm.IsMergedSeason.ShouldBeTrue();
    }

    [Theory(DisplayName = "Should match year within season date bounds")]
    [InlineData(2026, 2026, 2026, true, TestDisplayName = "exact single-year match")]
    [InlineData(2020, 2020, 2021, true, TestDisplayName = "start year of merged season")]
    [InlineData(2021, 2020, 2021, true, TestDisplayName = "end year of merged season")]
    [InlineData(2019, 2020, 2021, false, TestDisplayName = "year before merged range")]
    [InlineData(2022, 2020, 2021, false, TestDisplayName = "year after merged range")]
    [InlineData(2025, 2026, 2026, false, TestDisplayName = "year before single-year season")]
    [InlineData(2027, 2026, 2026, false, TestDisplayName = "year after single-year season")]
    public void ContainsYear_ShouldReturnExpectedResult_WhenYearChecked(
        int year, int startYear, int endYear, bool expected)
    {
        var vm = SeasonViewModelFactory.Create(
            startDate: new DateOnly(startYear, 1, 1),
            endDate: new DateOnly(endYear, 12, 31));

        vm.ContainsYear(year).ShouldBe(expected);
    }

    [Fact(DisplayName = "Should treat start year boundary as inclusive")]
    public void ContainsYear_ShouldBeTrue_WhenYearEqualsStartYear()
    {
        var vm = SeasonViewModelFactory.Create(
            startDate: new DateOnly(2024, 1, 1),
            endDate: new DateOnly(2024, 12, 31));

        vm.ContainsYear(2024).ShouldBeTrue();
        vm.ContainsYear(2023).ShouldBeFalse();
    }

    [Fact(DisplayName = "Should treat end year boundary as inclusive")]
    public void ContainsYear_ShouldBeTrue_WhenYearEqualsEndYear()
    {
        var vm = SeasonViewModelFactory.Create(
            startDate: new DateOnly(2020, 1, 1),
            endDate: new DateOnly(2021, 12, 31));

        vm.ContainsYear(2021).ShouldBeTrue();
        vm.ContainsYear(2022).ShouldBeFalse();
    }
}