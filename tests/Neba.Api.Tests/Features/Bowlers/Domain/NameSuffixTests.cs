using Neba.Api.Features.Bowlers.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Bowlers.Domain;

[UnitTest]
[Component("Bowlers.NameSuffix")]
public sealed class NameSuffixTests
{
    [Fact(DisplayName = "Should have 6 name suffixes")]
    public void NameSuffix_ShouldHave6Suffixes()
    {
        // Act
        var count = NameSuffix.List.Count;

        // Assert
        count.ShouldBe(6);
    }

    [Theory(DisplayName = "Name suffix values should be correct")]
    [InlineData("Jr", "Jr.", TestDisplayName = "Jr value should be 'Jr.'")]
    [InlineData("Sr", "Sr.", TestDisplayName = "Sr value should be 'Sr.'")]
    [InlineData("II", "II", TestDisplayName = "II value should be 'II'")]
    [InlineData("III", "III", TestDisplayName = "III value should be 'III'")]
    [InlineData("IV", "IV", TestDisplayName = "IV value should be 'IV'")]
    [InlineData("V", "V", TestDisplayName = "V value should be 'V'")]
    public void NameSuffix_ShouldHaveCorrectProperties(string expectedName, string expectedValue)
    {
        // Act
        var suffix = NameSuffix.FromName(expectedName);

        // Assert
        suffix.Name.ShouldBe(expectedName);
        suffix.Value.ShouldBe(expectedValue);
    }
}