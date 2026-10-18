using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Legacy.Bowlers;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Legacy.Bowlers;

[UnitTest]
[Component("Legacy")]
public sealed class LegacyBowlerMappingTests
{
    [Fact(DisplayName = "MapGender should map 0 to Male")]
    public void MapGender_ShouldReturnMale_WhenZero()
    {
        // Arrange / Act
        var result = LegacyBowlerMapping.MapGender(0);

        // Assert
        result.ShouldBe(Gender.Male);
    }

    [Fact(DisplayName = "MapGender should map 1 to Female")]
    public void MapGender_ShouldReturnFemale_WhenOne()
    {
        // Arrange / Act
        var result = LegacyBowlerMapping.MapGender(1);

        // Assert
        result.ShouldBe(Gender.Female);
    }

    [Theory(DisplayName = "MapGender should return null for any other value")]
    [InlineData(-1, TestDisplayName = "Negative")]
    [InlineData(2, TestDisplayName = "Two")]
    [InlineData(99, TestDisplayName = "Large")]
    public void MapGender_ShouldReturnNull_WhenUnrecognized(int legacyGender)
    {
        // Arrange / Act
        var result = LegacyBowlerMapping.MapGender(legacyGender);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "MapDateOfBirth should convert a date and drop the time")]
    public void MapDateOfBirth_ShouldReturnDateOnly_WhenValuePresent()
    {
        // Arrange
        var legacy = new DateTime(1980, 5, 17, 13, 45, 0, DateTimeKind.Unspecified);

        // Act
        var result = LegacyBowlerMapping.MapDateOfBirth(legacy);

        // Assert
        result.ShouldBe(new DateOnly(1980, 5, 17));
    }

    [Fact(DisplayName = "MapDateOfBirth should return null when the legacy value is null")]
    public void MapDateOfBirth_ShouldReturnNull_WhenValueMissing()
    {
        // Arrange / Act
        var result = LegacyBowlerMapping.MapDateOfBirth(null);

        // Assert
        result.ShouldBeNull();
    }

    [Theory(DisplayName = "MapSuffix should match known suffixes ignoring case, spaces and a trailing period")]
    [InlineData("Jr", "Jr", TestDisplayName = "Jr without period")]
    [InlineData("Jr.", "Jr", TestDisplayName = "Jr with period")]
    [InlineData("JR.", "Jr", TestDisplayName = "Jr upper case")]
    [InlineData("  sr. ", "Sr", TestDisplayName = "Sr padded and lower case")]
    [InlineData("II", "II", TestDisplayName = "II")]
    [InlineData("iii", "III", TestDisplayName = "III lower case")]
    [InlineData("IV.", "IV", TestDisplayName = "IV with period")]
    [InlineData("V", "V", TestDisplayName = "V")]
    public void MapSuffix_ShouldReturnMatchingSuffix_WhenRecognized(string legacySuffix, string expectedName)
    {
        // Arrange / Act
        var result = LegacyBowlerMapping.MapSuffix(legacySuffix);

        // Assert
        result.ShouldBe(NameSuffix.FromName(expectedName));
    }

    [Theory(DisplayName = "MapSuffix should return null for blank or unknown values")]
    [InlineData(null, TestDisplayName = "Null")]
    [InlineData("", TestDisplayName = "Empty")]
    [InlineData("   ", TestDisplayName = "Whitespace")]
    [InlineData("Esq", TestDisplayName = "Unknown")]
    public void MapSuffix_ShouldReturnNull_WhenBlankOrUnknown(string? legacySuffix)
    {
        // Arrange / Act
        var result = LegacyBowlerMapping.MapSuffix(legacySuffix);

        // Assert
        result.ShouldBeNull();
    }
}
