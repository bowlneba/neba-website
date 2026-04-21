using Neba.TestFactory.Attributes;
using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.Website.Tests.Tournaments.Schedule;

[UnitTest]
[Component("Website.Tournaments.Schedule.TournamentEnumExtensions")]
public sealed class TournamentEnumExtensionsTests
{
    [Theory(DisplayName = "Should return expected display labels for eligibility values")]
    [InlineData(TournamentEligibility.Open, "Open")]
    [InlineData(TournamentEligibility.Senior50Plus, "Senior 50+")]
    [InlineData(TournamentEligibility.Women, "Women")]
    [InlineData(TournamentEligibility.NonChampions, "Non-Champions")]
    public void ToDisplayStringEligibility_ShouldReturnExpectedLabel_WhenKnownValueProvided(
        TournamentEligibility eligibility,
        string expected)
    {
        var result = eligibility.ToDisplayString();

        result.ShouldBe(expected);
    }

    [Fact(DisplayName = "Should fall back to enum ToString for unmapped eligibility values")]
    public void ToDisplayStringEligibility_ShouldFallbackToEnumText_WhenUnknownValueProvided()
    {
        var result = ((TournamentEligibility)999).ToDisplayString();

        result.ShouldBe("999");
    }

    [Theory(DisplayName = "Should return expected display labels for tournament type values")]
    [InlineData(TournamentType.Singles, "Singles")]
    [InlineData(TournamentType.Doubles, "Doubles")]
    [InlineData(TournamentType.Trios, "Trios")]
    [InlineData(TournamentType.Team, "Team")]
    [InlineData(TournamentType.Senior, "Senior")]
    [InlineData(TournamentType.Women, "Women")]
    [InlineData(TournamentType.SpecialEvent, "Special Event")]
    public void ToDisplayStringType_ShouldReturnExpectedLabel_WhenKnownValueProvided(
        TournamentType type,
        string expected)
    {
        var result = type.ToDisplayString();

        result.ShouldBe(expected);
    }

    [Fact(DisplayName = "Should fall back to enum ToString for unmapped tournament type values")]
    public void ToDisplayStringType_ShouldFallbackToEnumText_WhenUnknownValueProvided()
    {
        var result = ((TournamentType)999).ToDisplayString();

        result.ShouldBe("999");
    }
}