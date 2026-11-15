using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.HallOfFame.Domain;

[UnitTest]
[Component("HallOfFame")]
public sealed class HallOfFamePointRulesTests
{
    private static readonly DateOnly Modern = new(2019, 3, 10);

    public static TheoryData<string, int> TitlePoints => new()
    {
        { TournamentType.Singles.Name, 6 },
        { TournamentType.TournamentOfChampions.Name, 6 },
        { TournamentType.Doubles.Name, 6 },
        { TournamentType.Invitational.Name, 6 },
        { TournamentType.Masters.Name, 6 },
        { TournamentType.OverUnderFiftyDoubles.Name, 3 },
        { TournamentType.OverUnderFortyDoubles.Name, 3 },
        { TournamentType.NonChampions.Name, 2 },
        { TournamentType.Senior.Name, 2 },
        { TournamentType.SeniorAndWomen.Name, 2 },
        { TournamentType.Trios.Name, 2 },
        { TournamentType.HighRoller.Name, 3 },
        { TournamentType.OverForty.Name, 2 },
        { TournamentType.FortyToFortyNine.Name, 2 }
    };

    public static TheoryData<string> ZeroPointTitles =>
    [
        TournamentType.Women.Name,
        TournamentType.Youth.Name,
        TournamentType.Eliminator.Name,
        TournamentType.Baker.Name
    ];

    public static TheoryData<string, int> AwardPoints => new()
    {
        { BowlerOfTheYearCategory.Open.Name, 12 },
        { BowlerOfTheYearCategory.Senior.Name, 2 },
        { BowlerOfTheYearCategory.SuperSenior.Name, 2 }
    };

    public static TheoryData<string> ZeroPointAwards =>
    [
        BowlerOfTheYearCategory.Youth.Name,
        BowlerOfTheYearCategory.Rookie.Name,
        BowlerOfTheYearCategory.Woman.Name
    ];

    [Fact(DisplayName = "Every tournament type has an entry in the point table")]
    public void IsMapped_ShouldBeTrue_ForEveryTournamentType()
    {
        // Arrange
        var unmapped = TournamentType.List.Where(type => !HallOfFamePointRules.IsMapped(type)).Select(type => type.Name).ToList();

        // Act & Assert
        unmapped.ShouldBeEmpty("a new tournament type needs a Hall of Fame point decision");
    }

    [Fact(DisplayName = "Every Bowler of the Year category has an entry in the point table")]
    public void IsMapped_ShouldBeTrue_ForEveryAwardCategory()
    {
        // Arrange
        var unmapped = BowlerOfTheYearCategory.List.Where(category => !HallOfFamePointRules.IsMapped(category)).Select(category => category.Name).ToList();

        // Act & Assert
        unmapped.ShouldBeEmpty("a new award category needs a Hall of Fame point decision");
    }

    [Theory(DisplayName = "A title earns the points from the page's table")]
    [MemberData(nameof(TitlePoints))]
    public void ForTitle_ShouldReturnPoints_ForEachTitleType(string typeName, int points)
    {
        // Act
        var rule = HallOfFamePointRules.ForTitle(TournamentType.FromName(typeName), Modern);

        // Assert
        rule.ShouldNotBeNull();
        rule.Points.ShouldBe(points);
    }

    [Theory(DisplayName = "A zero-point title returns no rule")]
    [MemberData(nameof(ZeroPointTitles))]
    public void ForTitle_ShouldReturnNull_ForZeroPointTypes(string typeName)
    {
        // Act
        var rule = HallOfFamePointRules.ForTitle(TournamentType.FromName(typeName), Modern);

        // Assert
        rule.ShouldBeNull();
    }

    [Theory(DisplayName = "A Bowler of the Year award earns the points from the page's table")]
    [MemberData(nameof(AwardPoints))]
    public void ForAward_ShouldReturnPoints_ForEachCategory(string categoryName, int points)
    {
        // Act
        var rule = HallOfFamePointRules.ForAward(BowlerOfTheYearCategory.FromName(categoryName));

        // Assert
        rule.ShouldNotBeNull();
        rule.Points.ShouldBe(points);
    }

    [Theory(DisplayName = "A Youth, Rookie or Woman Bowler of the Year award earns no points")]
    [MemberData(nameof(ZeroPointAwards))]
    public void ForAward_ShouldReturnNull_ForZeroPointCategories(string categoryName)
    {
        // Act
        var rule = HallOfFamePointRules.ForAward(BowlerOfTheYearCategory.FromName(categoryName));

        // Assert
        rule.ShouldBeNull();
    }

    [Fact(DisplayName = "Singles in 1990 earns 12 points")]
    public void ForTitle_ShouldReturn12_ForSinglesBefore1991()
    {
        // Act
        var rule = HallOfFamePointRules.ForTitle(TournamentType.Singles, new DateOnly(1990, 12, 31));

        // Assert
        rule.ShouldNotBeNull();
        rule.Points.ShouldBe(12);
    }

    [Fact(DisplayName = "Singles in 1991 earns 6 points")]
    public void ForTitle_ShouldReturn6_ForSinglesIn1991()
    {
        // Act
        var rule = HallOfFamePointRules.ForTitle(TournamentType.Singles, new DateOnly(1991, 1, 1));

        // Assert
        rule.ShouldNotBeNull();
        rule.Points.ShouldBe(6);
    }

    [Fact(DisplayName = "The point table has no duplicate label")]
    public void All_ShouldHaveNoDuplicateLabel()
    {
        // Act
        var duplicates = HallOfFamePointRules.All.GroupBy(rule => rule.Label).Where(group => group.Count() > 1).ToList();

        // Assert
        duplicates.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Every rule a title or award can return is in the point table")]
    public void All_ShouldContainEveryRuleReturned()
    {
        // Arrange
        var returned = TournamentType.List.Select(type => HallOfFamePointRules.ForTitle(type, Modern))
            .Append(HallOfFamePointRules.ForTitle(TournamentType.Singles, new DateOnly(1990, 1, 1)))
            .Concat(BowlerOfTheYearCategory.List.Select(HallOfFamePointRules.ForAward))
            .OfType<HallOfFamePointRule>()
            .Distinct();

        // Act & Assert
        HallOfFamePointRules.All.ShouldBe(HallOfFamePointRules.All.Intersect(returned), ignoreOrder: true);
    }

    [Theory(DisplayName = "Meeting all three criteria needs every one at its threshold")]
    [InlineData(36, 3, 10, true, TestDisplayName = "All exactly at the threshold")]
    [InlineData(35, 3, 10, false, TestDisplayName = "One point short")]
    [InlineData(36, 2, 10, false, TestDisplayName = "One season short")]
    [InlineData(36, 3, 9, false, TestDisplayName = "One membership year short")]
    [InlineData(80, 9, 30, true, TestDisplayName = "Well over every threshold")]
    public void MeetsAllCriteria_ShouldRequireEveryThreshold(int points, int seasons, int years, bool expected)
    {
        // Act
        var result = HallOfFamePointRules.MeetsAllCriteria(points, seasons, years);

        // Assert
        result.ShouldBe(expected);
    }
}