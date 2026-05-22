using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Seasons.Domain;

[UnitTest]
[Component("Awards.BowlerOfTheYearAward")]
public sealed class BowlerOfTheYearAwardTests
{
    // ── CreateOpen ────────────────────────────────────────────────────────────

    [Fact(DisplayName = "CreateOpen should return an error when bowler ID is empty")]
    public void CreateOpen_ShouldReturnError_WhenBowlerIdIsEmpty()
    {
        // Act
        var result = BowlerOfTheYearAward.CreateOpen(BowlerId.Empty);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(BowlerOfTheYearAwardErrors.BowlerIdRequired);
    }

    [Fact(DisplayName = "CreateOpen should return an Open award when bowler ID is valid")]
    public void CreateOpen_ShouldReturnAward_WhenBowlerIdIsValid()
    {
        // Arrange
        var bowlerId = BowlerId.New();

        // Act
        var result = BowlerOfTheYearAward.CreateOpen(bowlerId);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.BowlerId.ShouldBe(bowlerId);
        result.Value.Category.ShouldBe(BowlerOfTheYearCategory.Open);
    }

    // ── CreateWoman ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "CreateWoman should return an error when bowler ID is empty")]
    public void CreateWoman_ShouldReturnError_WhenBowlerIdIsEmpty()
    {
        // Act
        var result = BowlerOfTheYearAward.CreateWoman(BowlerId.Empty, Gender.Female);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(BowlerOfTheYearAwardErrors.BowlerIdRequired);
    }

    [Fact(DisplayName = "CreateWoman should return an error when gender is not female")]
    public void CreateWoman_ShouldReturnError_WhenGenderIsNotFemale()
    {
        // Act
        var result = BowlerOfTheYearAward.CreateWoman(BowlerId.New(), Gender.Male);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(BowlerOfTheYearAwardErrors.NotFemale);
    }

    [Fact(DisplayName = "CreateWoman should return a Woman award when bowler is female")]
    public void CreateWoman_ShouldReturnAward_WhenBowlerIsFemale()
    {
        // Arrange
        var bowlerId = BowlerId.New();

        // Act
        var result = BowlerOfTheYearAward.CreateWoman(bowlerId, Gender.Female);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.BowlerId.ShouldBe(bowlerId);
        result.Value.Category.ShouldBe(BowlerOfTheYearCategory.Woman);
    }

    // ── CreateSenior ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "CreateSenior should return an error when bowler ID is empty")]
    public void CreateSenior_ShouldReturnError_WhenBowlerIdIsEmpty()
    {
        // Act
        var result = BowlerOfTheYearAward.CreateSenior(BowlerId.Empty, age: 55);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(BowlerOfTheYearAwardErrors.BowlerIdRequired);
    }

    [Fact(DisplayName = "CreateSenior should return an error when age is below 50")]
    public void CreateSenior_ShouldReturnError_WhenAgeIsBelow50()
    {
        // Act
        var result = BowlerOfTheYearAward.CreateSenior(BowlerId.New(), age: 49);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(BowlerOfTheYearAwardErrors.InsufficientAgeForSenior);
    }

    [Fact(DisplayName = "CreateSenior should return a Senior award when age is exactly 50")]
    public void CreateSenior_ShouldReturnAward_WhenAgeIsExactly50()
    {
        // Arrange
        var bowlerId = BowlerId.New();

        // Act
        var result = BowlerOfTheYearAward.CreateSenior(bowlerId, age: 50);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.BowlerId.ShouldBe(bowlerId);
        result.Value.Category.ShouldBe(BowlerOfTheYearCategory.Senior);
    }

    // ── CreateSuperSenior ─────────────────────────────────────────────────────

    [Fact(DisplayName = "CreateSuperSenior should return an error when bowler ID is empty")]
    public void CreateSuperSenior_ShouldReturnError_WhenBowlerIdIsEmpty()
    {
        // Act
        var result = BowlerOfTheYearAward.CreateSuperSenior(BowlerId.Empty, age: 65);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(BowlerOfTheYearAwardErrors.BowlerIdRequired);
    }

    [Fact(DisplayName = "CreateSuperSenior should return an error when age is below 60")]
    public void CreateSuperSenior_ShouldReturnError_WhenAgeIsBelow60()
    {
        // Act
        var result = BowlerOfTheYearAward.CreateSuperSenior(BowlerId.New(), age: 59);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(BowlerOfTheYearAwardErrors.InsufficientAgeForSuperSenior);
    }

    [Fact(DisplayName = "CreateSuperSenior should return a SuperSenior award when age is exactly 60")]
    public void CreateSuperSenior_ShouldReturnAward_WhenAgeIsExactly60()
    {
        // Arrange
        var bowlerId = BowlerId.New();

        // Act
        var result = BowlerOfTheYearAward.CreateSuperSenior(bowlerId, age: 60);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.BowlerId.ShouldBe(bowlerId);
        result.Value.Category.ShouldBe(BowlerOfTheYearCategory.SuperSenior);
    }

    // ── CreateRookie ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "CreateRookie should return an error when bowler ID is empty")]
    public void CreateRookie_ShouldReturnError_WhenBowlerIdIsEmpty()
    {
        // Act
        var result = BowlerOfTheYearAward.CreateRookie(BowlerId.Empty, isRookie: true);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(BowlerOfTheYearAwardErrors.BowlerIdRequired);
    }

    [Fact(DisplayName = "CreateRookie should return an error when bowler is not a rookie")]
    public void CreateRookie_ShouldReturnError_WhenBowlerIsNotARookie()
    {
        // Act
        var result = BowlerOfTheYearAward.CreateRookie(BowlerId.New(), isRookie: false);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(BowlerOfTheYearAwardErrors.NotARookie);
    }

    [Fact(DisplayName = "CreateRookie should return a Rookie award when bowler is a new member")]
    public void CreateRookie_ShouldReturnAward_WhenBowlerIsARookie()
    {
        // Arrange
        var bowlerId = BowlerId.New();

        // Act
        var result = BowlerOfTheYearAward.CreateRookie(bowlerId, isRookie: true);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.BowlerId.ShouldBe(bowlerId);
        result.Value.Category.ShouldBe(BowlerOfTheYearCategory.Rookie);
    }

    // ── CreateYouth ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "CreateYouth should return an error when bowler ID is empty")]
    public void CreateYouth_ShouldReturnError_WhenBowlerIdIsEmpty()
    {
        // Act
        var result = BowlerOfTheYearAward.CreateYouth(BowlerId.Empty, age: 16);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(BowlerOfTheYearAwardErrors.BowlerIdRequired);
    }

    [Fact(DisplayName = "CreateYouth should return an error when age is 18 or older")]
    public void CreateYouth_ShouldReturnError_WhenAgeIs18OrOlder()
    {
        // Act
        var result = BowlerOfTheYearAward.CreateYouth(BowlerId.New(), age: 18);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(BowlerOfTheYearAwardErrors.AgeExceedsYouthLimit);
    }

    [Fact(DisplayName = "CreateYouth should return a Youth award when age is exactly 17")]
    public void CreateYouth_ShouldReturnAward_WhenAgeIsExactly17()
    {
        // Arrange
        var bowlerId = BowlerId.New();

        // Act
        var result = BowlerOfTheYearAward.CreateYouth(bowlerId, age: 17);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.BowlerId.ShouldBe(bowlerId);
        result.Value.Category.ShouldBe(BowlerOfTheYearCategory.Youth);
    }
}
