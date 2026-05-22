using Neba.Api.Features.Bowlers.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.Domain;

[UnitTest]
[Component("Tournaments.SideCutCriteriaGroup")]
public sealed class SideCutCriteriaGroupTests
{
    [Fact(DisplayName = "AddCriteria replaces existing age criteria when called again on the same group")]
    public void AddCriteria_ShouldReplaceExistingAgeCriteria_WhenAgeCriteriaAlreadyExists()
    {
        // Arrange
        var group = SideCutCriteriaGroupFactory.Create(
            criteria:
            [
                SideCutCriteriaFactory.CreateAgeRequirement(minimumAge: 50)
            ]);

        // Act
        var result = group.AddCriteria(minimumAge: 60, maximumAge: 80);

        // Assert
        result.IsError.ShouldBeFalse();
        group.Criteria.Count.ShouldBe(1);
        group.Criteria.Single().MinimumAge.ShouldBe(60);
        group.Criteria.Single().MaximumAge.ShouldBe(80);
    }

    [Fact(DisplayName = "AddCriteria returns SideCutCriteria.BothAgesRequired when both age bounds are null")]
    public void AddCriteria_ShouldReturnError_WhenBothAgeBoundsAreNull()
    {
        // Arrange
        var group = SideCutCriteriaGroupFactory.Create();

        // Act
        var result = group.AddCriteria(minimumAge: null, maximumAge: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("SideCutCriteria.BothAgesRequired");
    }

    [Fact(DisplayName = "AddCriteria replaces existing gender criteria when called again on the same group")]
    public void AddCriteria_ShouldReplaceExistingGenderCriteria_WhenGenderCriteriaAlreadyExists()
    {
        // Arrange
        var group = SideCutCriteriaGroupFactory.Create(
            criteria:
            [
                SideCutCriteriaFactory.CreateGenderRequirement(Gender.Female)
            ]);

        // Act
        var result = group.AddCriteria(Gender.Male);

        // Assert
        result.IsError.ShouldBeFalse();
        group.Criteria.Count.ShouldBe(1);
        group.Criteria.Single().GenderRequirement.ShouldBe(Gender.Male);
    }

    [Fact(DisplayName = "AddCriteria allows one age and one gender criteria in the same group")]
    public void AddCriteria_ShouldAllowOneAgeAndOneGenderInSameGroup()
    {
        // Arrange
        var group = SideCutCriteriaGroupFactory.Create();

        // Act
        group.AddCriteria(Gender.Female);
        group.AddCriteria(minimumAge: 60, maximumAge: null);

        // Assert
        group.Criteria.Count.ShouldBe(2);
    }
}
