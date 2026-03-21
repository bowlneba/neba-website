using Neba.Domain.Bowlers;

namespace Neba.Domain.Tests.Bowlers;

public sealed class GenderTests
{
    [Fact(DisplayName = "Gender should have 2 defined values")]
    public void Gender_ShouldHaveTwoDefinedValues()
    {
        // Arrange & Act
        var values = Gender.List;

        // Assert
        values.Count.ShouldBe(2);
    }

    [Theory(DisplayName = "Gender should have correct name and value for all values")]
    [InlineData("Male", "M", TestDisplayName = "Male gender maps to 'M'")]
    [InlineData("Female", "F", TestDisplayName = "Female gender maps to 'F'")]
    public void Gender_ShouldHaveCorrectNameAndValue(string expectedName, string expectedValue)
    {
        // Arrange & Act
        var gender = Gender.List.FirstOrDefault(g => g.Name == expectedName);

        // Assert
        gender.ShouldNotBeNull();
        gender.Value.ShouldBe(expectedValue);
    }
}