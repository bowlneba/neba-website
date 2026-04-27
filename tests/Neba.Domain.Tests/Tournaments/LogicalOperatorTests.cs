using Neba.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Domain.Tests.Tournaments;

[UnitTest]
[Component("Tournaments.LogicalOperator")]
public sealed class LogicalOperatorTests
{
    [Fact(DisplayName = "Should have 3 logical operators")]
    public void LogicalOperator_ShouldHave3Operators()
    {
        // Act
        var count = LogicalOperator.List.Count;

        // Assert
        count.ShouldBe(3);
    }

    [Theory(DisplayName = "Logical operator values should be correct")]
    [InlineData("And", "AND", TestDisplayName = "And should have value 'AND'")]
    [InlineData("Or", "OR", TestDisplayName = "Or should have value 'OR'")]
    [InlineData("Not", "NOT", TestDisplayName = "Not should have value 'NOT'")]
    public void LogicalOperator_ShouldHaveCorrectProperties(string expectedName, string expectedValue)
    {
        // Act
        var op = LogicalOperator.FromValue(expectedValue);

        // Assert
        op.Name.ShouldBe(expectedName);
        op.Value.ShouldBe(expectedValue);
    }
}