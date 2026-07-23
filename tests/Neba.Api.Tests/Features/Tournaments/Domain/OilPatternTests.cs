using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.Domain;

[UnitTest]
[Component("Tournaments.OilPattern")]
public sealed class OilPatternTests
{
    #nullable disable
    [Theory(DisplayName = "Create returns OilPattern.Name.Required when name is null, empty, or whitespace")]
    [InlineData(null, TestDisplayName = "name is null")]
    [InlineData("", TestDisplayName = "name is empty")]
    [InlineData("   ", TestDisplayName = "name is whitespace")]
    public void Create_ShouldReturnError_WhenNameIsNullOrWhiteSpace(string name)
    {
        // Act
        var result = OilPattern.Create(
            name,
            OilPatternFactory.ValidLength,
            OilPatternFactory.ValidVolume,
            OilPatternFactory.ValidLeftRatio,
            OilPatternFactory.ValidRightRatio);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("OilPattern.Name.Required");
    }
    #nullable enable

    [Theory(DisplayName = "Create returns OilPattern.Length.MustBePositive when length is zero or negative")]
    [InlineData(0, TestDisplayName = "length is zero")]
    [InlineData(-1, TestDisplayName = "length is negative")]
    public void Create_ShouldReturnError_WhenLengthIsNotPositive(int length)
    {
        // Act
        var result = OilPattern.Create(
            OilPatternFactory.ValidName,
            length,
            OilPatternFactory.ValidVolume,
            OilPatternFactory.ValidLeftRatio,
            OilPatternFactory.ValidRightRatio);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("OilPattern.Length.MustBePositive");
    }

    [Theory(DisplayName = "Create returns OilPattern.Volume.MustBePositive when volume is zero or negative")]
    [InlineData(0, TestDisplayName = "volume is zero")]
    [InlineData(-1, TestDisplayName = "volume is negative")]
    public void Create_ShouldReturnError_WhenVolumeIsNotPositive(decimal volume)
    {
        // Act
        var result = OilPattern.Create(
            OilPatternFactory.ValidName,
            OilPatternFactory.ValidLength,
            volume,
            OilPatternFactory.ValidLeftRatio,
            OilPatternFactory.ValidRightRatio);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("OilPattern.Volume.MustBePositive");
    }

    [Fact(DisplayName = "Create returns OilPattern with the specified properties when inputs are valid")]
    public void Create_ShouldReturnOilPattern_WhenInputsAreValid()
    {
        // Act
        var result = OilPattern.Create(
            OilPatternFactory.ValidName,
            OilPatternFactory.ValidLength,
            OilPatternFactory.ValidVolume,
            OilPatternFactory.ValidLeftRatio,
            OilPatternFactory.ValidRightRatio);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Name.ShouldBe(OilPatternFactory.ValidName);
        result.Value.Length.ShouldBe(OilPatternFactory.ValidLength);
        result.Value.Volume.ShouldBe(OilPatternFactory.ValidVolume);
        result.Value.LeftRatio.ShouldBe(OilPatternFactory.ValidLeftRatio);
        result.Value.RightRatio.ShouldBe(OilPatternFactory.ValidRightRatio);
    }

    [Fact(DisplayName = "Create assigns a new non-empty OilPatternId when inputs are valid")]
    public void Create_ShouldAssignNewId_WhenInputsAreValid()
    {
        // Act
        var result = OilPattern.Create(
            OilPatternFactory.ValidName,
            OilPatternFactory.ValidLength,
            OilPatternFactory.ValidVolume,
            OilPatternFactory.ValidLeftRatio,
            OilPatternFactory.ValidRightRatio);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Id.ShouldNotBe(default);
    }

    [Fact(DisplayName = "Create leaves KegelId null when not provided")]
    public void Create_ShouldLeaveKegelIdNull_WhenNotProvided()
    {
        // Act
        var result = OilPattern.Create(
            OilPatternFactory.ValidName,
            OilPatternFactory.ValidLength,
            OilPatternFactory.ValidVolume,
            OilPatternFactory.ValidLeftRatio,
            OilPatternFactory.ValidRightRatio);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.KegelId.ShouldBeNull();
    }

    [Fact(DisplayName = "Create sets KegelId when provided")]
    public void Create_ShouldSetKegelId_WhenProvided()
    {
        // Arrange
        var kegelId = Guid.NewGuid();

        // Act
        var result = OilPattern.Create(
            OilPatternFactory.ValidName,
            OilPatternFactory.ValidLength,
            OilPatternFactory.ValidVolume,
            OilPatternFactory.ValidLeftRatio,
            OilPatternFactory.ValidRightRatio,
            kegelId);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.KegelId.ShouldBe(kegelId);
    }

    [Theory(DisplayName = "LengthCategory should reflect the pattern length")]
    [InlineData(37, "Short", TestDisplayName = "length 37 is Short")]
    [InlineData(40, "Medium", TestDisplayName = "length 40 is Medium")]
    [InlineData(43, "Long", TestDisplayName = "length 43 is Long")]
    public void LengthCategory_ShouldReflectPatternLength(int length, string expectedCategoryName)
    {
        // Arrange
        var oilPattern = OilPatternFactory.Create(length: length);

        // Act
        var category = oilPattern.LengthCategory;

        // Assert
        category.Name.ShouldBe(expectedCategoryName);
    }

    [Theory(DisplayName = "RatioCategory should reflect the larger of LeftRatio and RightRatio")]
    [InlineData(3.0, 3.0, "Sport", TestDisplayName = "equal ratios of 3.0 are Sport")]
    [InlineData(6.0, 3.0, "Challenge", TestDisplayName = "left ratio of 6.0 is Challenge")]
    [InlineData(3.0, 6.0, "Challenge", TestDisplayName = "right ratio of 6.0 is Challenge")]
    [InlineData(1.0, 9.0, "Recreation", TestDisplayName = "right ratio of 9.0 is Recreation")]
    public void RatioCategory_ShouldReflectMaxRatio(decimal leftRatio, decimal rightRatio, string expectedCategoryName)
    {
        // Arrange
        var oilPattern = OilPatternFactory.Create(leftRatio: leftRatio, rightRatio: rightRatio);

        // Act
        var category = oilPattern.RatioCategory;

        // Assert
        category.Name.ShouldBe(expectedCategoryName);
    }
}