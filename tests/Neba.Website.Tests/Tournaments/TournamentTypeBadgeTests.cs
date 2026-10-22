using Bunit;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Tournaments;

namespace Neba.Website.Tests.Tournaments;

[UnitTest]
[Component("Website.Tournaments.TournamentTypeBadge")]
public sealed class TournamentTypeBadgeTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render the tournament type name with the type-pill class")]
    public void Render_ShouldRenderTypeNameInPill()
    {
        // Arrange & Act
        var cut = _ctx.Render<TournamentTypeBadge>(p => p
            .Add(x => x.TournamentType, "Doubles"));

        // Assert
        var pill = cut.Find("span.type-pill");
        pill.TextContent.Trim().ShouldBe("Doubles");
        pill.ClassList.ShouldContain("type-doubles");
    }

    [Theory(DisplayName = "ModifierFor should map tournament type names to a style modifier, ignoring case")]
    [InlineData("Singles", "singles")]
    [InlineData("Non-Champions", "singles")]
    [InlineData("High Roller", "singles")]
    [InlineData("Over 40", "singles")]
    [InlineData("40 - 49", "singles")]
    [InlineData("Eliminator", "singles")]
    [InlineData("Something Unrecognised", "singles")]
    [InlineData("Doubles", "doubles")]
    [InlineData("Over/Under 50 Doubles", "doubles")]
    [InlineData("Over/Under 40 Doubles", "doubles")]
    [InlineData("Trios", "trios")]
    [InlineData("Senior", "senior")]
    [InlineData("Senior / Women", "senior")]
    [InlineData("Women", "women")]
    [InlineData("Youth", "youth")]
    [InlineData("Baker", "baker")]
    [InlineData("Tournament of Champions", "major")]
    [InlineData("Invitational", "major")]
    [InlineData("Masters", "major")]
    [InlineData("DOUBLES", "doubles")]
    [InlineData("masters", "major")]
    [InlineData("bAkEr", "baker")]
    public void ModifierFor_ShouldReturnExpectedModifier(string tournamentType, string expected)
    {
        // Arrange & Act
        var result = TournamentTypeBadge.ModifierFor(tournamentType);

        // Assert
        result.ShouldBe(expected);
    }
}