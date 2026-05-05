using Neba.Domain.Bowlers;
using Neba.Domain.Tournaments;
using Neba.TestFactory.Attributes;

namespace Neba.Domain.Tests.Tournaments;

[UnitTest]
[Component("Tournaments.SideCutCriteriaGroup")]
public sealed class SideCutCriteriaGroupTests
{
    // ── Create ────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Create should return a group with the specified SideCutId")]
    public void Create_ShouldSetSideCutId()
    {
        // Arrange
        var sideCutId = SideCutId.New();

        // Act
        var result = SideCutCriteriaGroup.Create(sideCutId, LogicalOperator.And, sortOrder: 1);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.SideCutId.ShouldBe(sideCutId);
    }

    [Fact(DisplayName = "Create should return a group with the specified LogicalOperator")]
    public void Create_ShouldSetLogicalOperator()
    {
        // Act
        var result = SideCutCriteriaGroup.Create(SideCutId.New(), LogicalOperator.Or, sortOrder: 1);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.LogicalOperator.ShouldBe(LogicalOperator.Or);
    }

    [Fact(DisplayName = "Create should return a group with the specified SortOrder")]
    public void Create_ShouldSetSortOrder()
    {
        // Act
        var result = SideCutCriteriaGroup.Create(SideCutId.New(), LogicalOperator.And, sortOrder: 3);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.SortOrder.ShouldBe(3);
    }

    [Fact(DisplayName = "Create should return a group with an empty Criteria collection")]
    public void Create_ShouldReturnEmptyCriteria()
    {
        // Act
        var result = SideCutCriteriaGroup.Create(SideCutId.New(), LogicalOperator.And, sortOrder: 1);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Criteria.ShouldBeEmpty();
    }

    // ── AddCriteria(int?, int?) ───────────────────────────────────────────────

    [Fact(DisplayName = "AddCriteria should add age criteria to the group when minimumAge only is provided")]
    public void AddCriteria_Age_ShouldAddCriteria_WhenMinimumAgeProvided()
    {
        // Arrange
        var group = SideCutCriteriaGroup.Create(SideCutId.New(), LogicalOperator.And, sortOrder: 1).Value;

        // Act
        var result = group.AddCriteria(minimumAge: 50, maximumAge: null);

        // Assert
        result.IsError.ShouldBeFalse();
        group.Criteria.Count.ShouldBe(1);
        group.Criteria.Single().MinimumAge.ShouldBe(50);
    }

    [Fact(DisplayName = "AddCriteria should add age criteria to the group when maximumAge only is provided")]
    public void AddCriteria_Age_ShouldAddCriteria_WhenMaximumAgeProvided()
    {
        // Arrange
        var group = SideCutCriteriaGroup.Create(SideCutId.New(), LogicalOperator.And, sortOrder: 1).Value;

        // Act
        var result = group.AddCriteria(minimumAge: null, maximumAge: 17);

        // Assert
        result.IsError.ShouldBeFalse();
        group.Criteria.Count.ShouldBe(1);
        group.Criteria.Single().MaximumAge.ShouldBe(17);
    }

    [Fact(DisplayName = "AddCriteria should add age criteria to the group when both ages are provided")]
    public void AddCriteria_Age_ShouldAddCriteria_WhenBothAgesProvided()
    {
        // Arrange
        var group = SideCutCriteriaGroup.Create(SideCutId.New(), LogicalOperator.And, sortOrder: 1).Value;

        // Act
        var result = group.AddCriteria(minimumAge: 14, maximumAge: 17);

        // Assert
        result.IsError.ShouldBeFalse();
        group.Criteria.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "AddCriteria should accumulate multiple age criteria")]
    public void AddCriteria_Age_ShouldAccumulateCriteria()
    {
        // Arrange
        var group = SideCutCriteriaGroup.Create(SideCutId.New(), LogicalOperator.And, sortOrder: 1).Value;

        // Act
        group.AddCriteria(minimumAge: 50, maximumAge: null);
        group.AddCriteria(minimumAge: null, maximumAge: 17);

        // Assert
        group.Criteria.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "AddCriteria should return an error and not add criteria when minimumAge is negative")]
    public void AddCriteria_Age_ShouldReturnError_WhenMinimumAgeIsNegative()
    {
        // Arrange
        var group = SideCutCriteriaGroup.Create(SideCutId.New(), LogicalOperator.And, sortOrder: 1).Value;

        // Act
        var result = group.AddCriteria(minimumAge: -1, maximumAge: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("SideCutCriteria.MinimumAgeInvalid");
        group.Criteria.ShouldBeEmpty();
    }

    [Fact(DisplayName = "AddCriteria should return an error and not add criteria when maximumAge is negative")]
    public void AddCriteria_Age_ShouldReturnError_WhenMaximumAgeIsNegative()
    {
        // Arrange
        var group = SideCutCriteriaGroup.Create(SideCutId.New(), LogicalOperator.And, sortOrder: 1).Value;

        // Act
        var result = group.AddCriteria(minimumAge: null, maximumAge: -1);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("SideCutCriteria.MaximumAgeInvalid");
        group.Criteria.ShouldBeEmpty();
    }

    [Fact(DisplayName = "AddCriteria should return an error and not add criteria when minimumAge is greater than maximumAge")]
    public void AddCriteria_Age_ShouldReturnError_WhenMinimumAgeExceedsMaximumAge()
    {
        // Arrange
        var group = SideCutCriteriaGroup.Create(SideCutId.New(), LogicalOperator.And, sortOrder: 1).Value;

        // Act
        var result = group.AddCriteria(minimumAge: 60, maximumAge: 50);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("SideCutCriteria.AgeRangeInvalid");
        group.Criteria.ShouldBeEmpty();
    }

    // ── AddCriteria(Gender) ───────────────────────────────────────────────────

    [Theory(DisplayName = "AddCriteria should add gender criteria to the group for each gender")]
    [MemberData(nameof(AllGenders))]
    public void AddCriteria_Gender_ShouldAddCriteria(Gender gender)
    {
        // Arrange
        var group = SideCutCriteriaGroup.Create(SideCutId.New(), LogicalOperator.Or, sortOrder: 1).Value;

        // Act
        var result = group.AddCriteria(gender);

        // Assert
        result.IsError.ShouldBeFalse();
        group.Criteria.Count.ShouldBe(1);
        group.Criteria.Single().GenderRequirement.ShouldBe(gender);
    }

    [Fact(DisplayName = "AddCriteria should accumulate multiple gender criteria")]
    public void AddCriteria_Gender_ShouldAccumulateCriteria()
    {
        // Arrange
        var group = SideCutCriteriaGroup.Create(SideCutId.New(), LogicalOperator.Or, sortOrder: 1).Value;

        // Act
        group.AddCriteria(Gender.Female);
        group.AddCriteria(Gender.Male);

        // Assert
        group.Criteria.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "AddCriteria should leave MinimumAge and MaximumAge null on the added gender criteria")]
    public void AddCriteria_Gender_ShouldLeaveAgeLimitsNull()
    {
        // Arrange
        var group = SideCutCriteriaGroup.Create(SideCutId.New(), LogicalOperator.And, sortOrder: 1).Value;

        // Act
        group.AddCriteria(Gender.Female);

        // Assert
        var criteria = group.Criteria.Single();
        criteria.MinimumAge.ShouldBeNull();
        criteria.MaximumAge.ShouldBeNull();
    }

    public static TheoryData<Gender> AllGenders() => [.. Gender.List];
}
