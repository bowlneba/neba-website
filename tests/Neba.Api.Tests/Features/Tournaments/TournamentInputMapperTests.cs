using Neba.Api.Features.Tournaments;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments;

[UnitTest]
[Component("Tournaments")]
public sealed class TournamentInputMapperTests
{
    [Theory(DisplayName = "ToPreviousTournamentId returns null for a missing or blank id")]
    [InlineData(null, TestDisplayName = "Null")]
    [InlineData("", TestDisplayName = "Empty")]
    [InlineData("   ", TestDisplayName = "Whitespace")]
    public void ToPreviousTournamentId_ShouldReturnNull_WhenBlank(string? value)
    {
        // Arrange & Act
        var id = TournamentInputMapper.ToPreviousTournamentId(value);

        // Assert
        id.ShouldBeNull();
    }

    [Fact(DisplayName = "ToPreviousTournamentId parses a valid id")]
    public void ToPreviousTournamentId_ShouldParseId_WhenValid()
    {
        // Arrange
        var expected = TournamentFactory.Create().Id;

        // Act
        var id = TournamentInputMapper.ToPreviousTournamentId(expected.ToString());

        // Assert
        id.ShouldBe(expected);
    }
}