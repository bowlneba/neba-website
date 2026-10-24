using Neba.Api.Contracts.Memberships;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Contracts.Memberships;

[UnitTest]
[Component("Memberships")]
public sealed class RepeatPolicyTests
{
    [Fact(DisplayName = "FromLegacyFrequency maps 0 to Unlimited")]
    public void FromLegacyFrequency_ShouldReturnUnlimited_WhenZero()
    {
        // Arrange & Act
        var policy = RepeatPolicy.FromLegacyFrequency(0);

        // Assert
        policy.Kind.ShouldBe(RepeatPolicyKind.Unlimited);
        policy.Days.ShouldBeNull();
    }

    [Fact(DisplayName = "FromLegacyFrequency maps -1 to OncePerLifetime")]
    public void FromLegacyFrequency_ShouldReturnOncePerLifetime_WhenMinusOne()
    {
        // Arrange & Act
        var policy = RepeatPolicy.FromLegacyFrequency(-1);

        // Assert
        policy.Kind.ShouldBe(RepeatPolicyKind.OncePerLifetime);
        policy.Days.ShouldBeNull();
    }

    [Fact(DisplayName = "FromLegacyFrequency maps 730 to MinimumDaysSinceLast with 730 days")]
    public void FromLegacyFrequency_ShouldReturnMinimumDaysSinceLast_WhenPositive()
    {
        // Arrange & Act
        var policy = RepeatPolicy.FromLegacyFrequency(730);

        // Assert
        policy.Kind.ShouldBe(RepeatPolicyKind.MinimumDaysSinceLast);
        policy.Days.ShouldBe(730);
    }

    [Theory(DisplayName = "FromLegacyFrequency throws for negatives other than -1")]
    [InlineData(-2)]
    [InlineData(-730)]
    public void FromLegacyFrequency_ShouldThrow_WhenOtherNegative(int frequency)
    {
        // Arrange & Act
        var act = () => RepeatPolicy.FromLegacyFrequency(frequency);

        // Assert
        Should.Throw<ArgumentOutOfRangeException>(act);
    }

    [Theory(DisplayName = "MinimumDaysSinceLast throws when days is not positive")]
    [InlineData(0)]
    [InlineData(-1)]
    public void MinimumDaysSinceLast_ShouldThrow_WhenNotPositive(int days)
    {
        // Arrange & Act
        var act = () => RepeatPolicy.MinimumDaysSinceLast(days);

        // Assert
        Should.Throw<ArgumentOutOfRangeException>(act);
    }

    [Fact(DisplayName = "Policies with the same kind and days are equal")]
    public void Equality_ShouldHold_WhenKindAndDaysMatch()
    {
        // Arrange & Act
        var first = RepeatPolicy.MinimumDaysSinceLast(30);
        var second = RepeatPolicy.MinimumDaysSinceLast(30);

        // Assert
        first.ShouldBe(second);
    }
}