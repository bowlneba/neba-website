using Neba.Api.Domain;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.Domain;

[UnitTest]
[Component("Tournaments.SideCut")]
public sealed class SideCutTests
{
    [Fact(DisplayName = "SideCut derives from AggregateRoot")]
    public void SideCut_ShouldDeriveFromAggregateRoot()
    {
        // Act
        var sideCut = SideCutFactory.Create();

        // Assert
        sideCut.ShouldBeOfType<SideCut>();
        sideCut.ShouldBeAssignableTo<AggregateRoot>();
    }

    [Fact(DisplayName = "AddCriteriaGroup returns a non-default SideCutCriteriaGroupId")]
    public void AddCriteriaGroup_ShouldReturnNewId()
    {
        // Arrange
        var sideCut = SideCutFactory.Create();

        // Act
        var result = sideCut.AddCriteriaGroup(LogicalOperator.And, sortOrder: 1);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldNotBe(default);
    }

    [Fact(DisplayName = "AddCriteriaGroup adds the group to CriteriaGroups")]
    public void AddCriteriaGroup_ShouldAddGroupToCollection()
    {
        // Arrange
        var sideCut = SideCutFactory.Create();

        // Act
        sideCut.AddCriteriaGroup(LogicalOperator.And, sortOrder: 1);

        // Assert
        sideCut.CriteriaGroups.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "AddCriteriaGroup sets the correct LogicalOperator on the new group")]
    public void AddCriteriaGroup_ShouldSetLogicalOperator()
    {
        // Arrange
        var sideCut = SideCutFactory.Create();

        // Act
        var result = sideCut.AddCriteriaGroup(LogicalOperator.Or, sortOrder: 1);

        // Assert
        sideCut.CriteriaGroups.Single(g => g.Id.Equals(result.Value)).LogicalOperator.ShouldBe(LogicalOperator.Or);
    }

    [Fact(DisplayName = "AddCriteriaGroup sets the correct SortOrder on the new group")]
    public void AddCriteriaGroup_ShouldSetSortOrder()
    {
        // Arrange
        var sideCut = SideCutFactory.Create();

        // Act
        var result = sideCut.AddCriteriaGroup(LogicalOperator.And, sortOrder: 5);

        // Assert
        sideCut.CriteriaGroups.Single(g => g.Id.Equals(result.Value)).SortOrder.ShouldBe(5);
    }

    [Fact(DisplayName = "AddCriteriaGroup returns error when sort order is already in use")]
    public void AddCriteriaGroup_ShouldReturnError_WhenSortOrderIsDuplicate()
    {
        // Arrange
        var sideCut = SideCutFactory.Create();
        sideCut.AddCriteriaGroup(LogicalOperator.And, sortOrder: 1);

        // Act
        var result = sideCut.AddCriteriaGroup(LogicalOperator.Or, sortOrder: 1);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("SideCut.DuplicateSortOrder");
    }

    [Fact(DisplayName = "AddCriteriaGroup allows two groups with different sort orders")]
    public void AddCriteriaGroup_ShouldAllowMultipleGroupsWithUniqueSortOrders()
    {
        // Arrange
        var sideCut = SideCutFactory.Create();

        // Act
        sideCut.AddCriteriaGroup(LogicalOperator.And, sortOrder: 1);
        sideCut.AddCriteriaGroup(LogicalOperator.Or, sortOrder: 2);

        // Assert
        sideCut.CriteriaGroups.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "AddCriteria adds age criteria to the group when minimumAge only is provided")]
    public void AddCriteria_Age_ShouldAddCriteria_WhenMinimumAgeProvided()
    {
        // Arrange
        var sideCut = SideCutFactory.Create();
        var groupId = sideCut.AddCriteriaGroup(LogicalOperator.And, sortOrder: 1).Value;

        // Act
        var result = sideCut.AddCriteria(groupId, minimumAge: 50, maximumAge: null);

        // Assert
        result.IsError.ShouldBeFalse();
        sideCut.CriteriaGroups.Single().Criteria.Single().MinimumAge.ShouldBe(50);
    }

    [Fact(DisplayName = "AddCriteria adds age criteria to the group when maximumAge only is provided")]
    public void AddCriteria_Age_ShouldAddCriteria_WhenMaximumAgeProvided()
    {
        // Arrange
        var sideCut = SideCutFactory.Create();
        var groupId = sideCut.AddCriteriaGroup(LogicalOperator.And, sortOrder: 1).Value;

        // Act
        var result = sideCut.AddCriteria(groupId, minimumAge: null, maximumAge: 17);

        // Assert
        result.IsError.ShouldBeFalse();
        sideCut.CriteriaGroups.Single().Criteria.Single().MaximumAge.ShouldBe(17);
    }

    [Fact(DisplayName = "AddCriteria replaces existing age criteria when called again on the same group")]
    public void AddCriteria_Age_ShouldReplaceExistingAgeCriteria()
    {
        // Arrange
        var sideCut = SideCutFactory.Create();
        var groupId = sideCut.AddCriteriaGroup(LogicalOperator.And, sortOrder: 1).Value;
        sideCut.AddCriteria(groupId, minimumAge: 50, maximumAge: null);

        // Act
        sideCut.AddCriteria(groupId, minimumAge: 60, maximumAge: 80);

        // Assert
        var group = sideCut.CriteriaGroups.Single();
        group.Criteria.Count.ShouldBe(1);
        group.Criteria.Single().MinimumAge.ShouldBe(60);
        group.Criteria.Single().MaximumAge.ShouldBe(80);
    }

    [Fact(DisplayName = "AddCriteria returns error when age criteria is invalid")]
    public void AddCriteria_Age_ShouldReturnError_WhenAgeRangeIsInvalid()
    {
        // Arrange
        var sideCut = SideCutFactory.Create();
        var groupId = sideCut.AddCriteriaGroup(LogicalOperator.And, sortOrder: 1).Value;

        // Act
        var result = sideCut.AddCriteria(groupId, minimumAge: 60, maximumAge: 50);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("SideCutCriteria.AgeRangeInvalid");
    }

    [Fact(DisplayName = "AddCriteria returns error when age criteria group is not found")]
    public void AddCriteria_Age_ShouldReturnError_WhenGroupNotFound()
    {
        // Arrange
        var sideCut = SideCutFactory.Create();
        var unknownGroupId = SideCutCriteriaGroupId.New();

        // Act
        var result = sideCut.AddCriteria(unknownGroupId, minimumAge: 50, maximumAge: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("SideCut.CriteriaGroupNotFound");
    }

    [Theory(DisplayName = "AddCriteria adds gender criteria to the group for each gender")]
    [MemberData(nameof(AllGenderValues))]
    public void AddCriteria_Gender_ShouldAddCriteria(string genderValue)
    {
        // Arrange
        var gender = Gender.FromValue(genderValue);
        var sideCut = SideCutFactory.Create();
        var groupId = sideCut.AddCriteriaGroup(LogicalOperator.And, sortOrder: 1).Value;

        // Act
        var result = sideCut.AddCriteria(groupId, gender);

        // Assert
        result.IsError.ShouldBeFalse();
        sideCut.CriteriaGroups.Single().Criteria.Single().GenderRequirement.ShouldBe(gender);
    }

    [Fact(DisplayName = "AddCriteria replaces existing gender criteria when called again on the same group")]
    public void AddCriteria_Gender_ShouldReplaceExistingGenderCriteria()
    {
        // Arrange
        var sideCut = SideCutFactory.Create();
        var groupId = sideCut.AddCriteriaGroup(LogicalOperator.And, sortOrder: 1).Value;
        sideCut.AddCriteria(groupId, Gender.Female);

        // Act
        sideCut.AddCriteria(groupId, Gender.Male);

        // Assert
        var group = sideCut.CriteriaGroups.Single();
        group.Criteria.Count.ShouldBe(1);
        group.Criteria.Single().GenderRequirement.ShouldBe(Gender.Male);
    }

    [Fact(DisplayName = "AddCriteria leaves age limits null on the added gender criteria")]
    public void AddCriteria_Gender_ShouldLeaveAgeLimitsNull()
    {
        // Arrange
        var sideCut = SideCutFactory.Create();
        var groupId = sideCut.AddCriteriaGroup(LogicalOperator.And, sortOrder: 1).Value;

        // Act
        sideCut.AddCriteria(groupId, Gender.Female);

        // Assert
        var criteria = sideCut.CriteriaGroups.Single().Criteria.Single();
        criteria.MinimumAge.ShouldBeNull();
        criteria.MaximumAge.ShouldBeNull();
    }

    [Fact(DisplayName = "AddCriteria returns error when gender criteria group is not found")]
    public void AddCriteria_Gender_ShouldReturnError_WhenGroupNotFound()
    {
        // Arrange
        var sideCut = SideCutFactory.Create();
        var unknownGroupId = SideCutCriteriaGroupId.New();

        // Act
        var result = sideCut.AddCriteria(unknownGroupId, Gender.Female);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("SideCut.CriteriaGroupNotFound");
    }

    [Fact(DisplayName = "AddCriteria allows one age and one gender criteria in the same group")]
    public void AddCriteria_ShouldAllowOneAgeAndOneGenderInSameGroup()
    {
        // Arrange
        var sideCut = SideCutFactory.Create();
        var groupId = sideCut.AddCriteriaGroup(LogicalOperator.And, sortOrder: 1).Value;

        // Act
        sideCut.AddCriteria(groupId, Gender.Female);
        sideCut.AddCriteria(groupId, minimumAge: 60, maximumAge: null);

        // Assert
        sideCut.CriteriaGroups.Single().Criteria.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "Create applies the provided criteria groups")]
    public void Create_ShouldApplyProvidedCriteriaGroups()
    {
        // Arrange
        var criteriaGroups = new[]
        {
            SideCutCriteriaGroupFactory.Create(
                logicalOperator: LogicalOperator.Or,
                sortOrder: 2,
                criteria:
                [
                    SideCutCriteriaFactory.CreateGenderRequirement(Gender.Female)
                ]),
            SideCutCriteriaGroupFactory.Create(
                logicalOperator: LogicalOperator.And,
                sortOrder: 1,
                criteria:
                [
                    SideCutCriteriaFactory.CreateAgeRequirement(minimumAge: 50)
                ])
        };

        // Act
        var sideCut = SideCutFactory.Create(criteriaGroups: criteriaGroups);

        // Assert
        sideCut.CriteriaGroups.Count.ShouldBe(2);
        sideCut.CriteriaGroups.OrderBy(group => group.SortOrder).Select(group => group.SortOrder).ShouldBe([1, 2]);
        sideCut.CriteriaGroups.Single(group => group.SortOrder == 1).Criteria.Single().MinimumAge.ShouldBe(50);
        sideCut.CriteriaGroups.Single(group => group.SortOrder == 2).Criteria.Single().GenderRequirement.ShouldBe(Gender.Female);
    }

    public static TheoryData<string> AllGenderValues() => [.. Gender.List.Select(static gender => gender.Value)];
}