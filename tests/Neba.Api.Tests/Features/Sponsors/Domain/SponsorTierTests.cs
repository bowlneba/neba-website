using Neba.Api.Features.Sponsors.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Sponsors.Domain;

[UnitTest]
[Component("Sponsors.SponsorTier")]
public sealed class SponsorTierTests
{
    [Fact(DisplayName = "Should have 3 sponsor tiers")]
    public void SponsorTier_ShouldHave3Tiers()
    {
        // Act
        var count = SponsorTier.List.Count;

        // Assert
        count.ShouldBe(3);
    }

    [Theory(DisplayName = "Sponsor tier properties should be correct")]
    [InlineData("Title Sponsor", 1, TestDisplayName = "Title Sponsor should have value 1")]
    [InlineData("Premier", 2, TestDisplayName = "Premier should have value 2")]
    [InlineData("Standard", 3, TestDisplayName = "Standard should have value 3")]
    public void SponsorTier_ShouldHaveCorrectProperties(string tierName, int expectedValue)
    {
        // Act
        var tier = SponsorTier.FromValue(expectedValue);

        // Assert
        tier.Name.ShouldBe(tierName);
        tier.Value.ShouldBe(expectedValue);
    }
}