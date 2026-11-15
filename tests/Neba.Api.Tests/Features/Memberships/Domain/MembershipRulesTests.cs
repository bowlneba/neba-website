using Neba.Api.Contracts.Memberships;
using Neba.Api.Features.Memberships.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Memberships.Domain;

[UnitTest]
[Component("Memberships")]
public sealed class MembershipRulesTests
{
    private static readonly DateOnly Start = new(2025, 3, 15);

    private static MembershipSnapshot Held(MembershipType type, DateOnly start)
        => new(type, start, type.CalculateEndDate(start));

    private static ErrorOr.ErrorOr<ErrorOr.Success> Check(
        MembershipType type,
        DateOnly start,
        params MembershipSnapshot[] history)
        => MembershipRules.CheckEligibility(type, start, type.CalculateEndDate(start), history);

    [Theory(DisplayName = "CalculateEndDate should follow the yearly rollover")]
    [InlineData(2025, 10, 31, 2025, TestDisplayName = "Oct 31 ends Dec 31 of the same year")]
    [InlineData(2025, 11, 1, 2026, TestDisplayName = "Nov 1 ends Dec 31 of next year")]
    [InlineData(2025, 12, 31, 2026, TestDisplayName = "Dec 31 ends Dec 31 of next year")]
    [InlineData(2025, 1, 1, 2025, TestDisplayName = "Jan 1 ends Dec 31 of the same year")]
    public void CalculateEndDate_ShouldRollOver_ForYearlyTypes(int year, int month, int day, int expectedYear)
    {
        // Arrange
        var start = new DateOnly(year, month, day);

        // Act
        var end = MembershipType.New.CalculateEndDate(start);

        // Assert
        end.ShouldBe(new DateOnly(expectedYear, 12, 31));
    }

    [Fact(DisplayName = "CalculateEndDate should count the start day for temporary types")]
    public void CalculateEndDate_ShouldIncludeStartDay_ForTemporaryTypes()
    {
        // Arrange
        var saturday = new DateOnly(2025, 6, 7);

        // Act
        var end = MembershipType.NonMember.CalculateEndDate(saturday);

        // Assert
        end.ShouldBe(new DateOnly(2025, 6, 8));
    }

    [Fact(DisplayName = "Should fail for an inactive type")]
    public void CheckEligibility_ShouldFail_WhenTypeInactive()
    {
        // Arrange & Act
        var result = Check(MembershipType.Courtesy, Start, Held(MembershipType.New, Start.AddYears(-2)));

        // Assert
        result.FirstError.Code.ShouldBe("BowlerMembership.TypeInactive");
    }

    [Theory(DisplayName = "Should fail a type that requires a prior membership when the bowler has no qualifying one")]
    [InlineData(null, TestDisplayName = "No history")]
    [InlineData("NON_MEMBER", TestDisplayName = "Only non-member history")]
    [InlineData("GUEST", TestDisplayName = "Only guest history")]
    public void CheckEligibility_ShouldFail_WhenNoQualifyingPriorMembership(string? heldCode)
    {
        // Arrange
        MembershipSnapshot[] history = heldCode is null
            ? []
            : [Held(MembershipType.FromValue(heldCode), Start.AddYears(-3))];

        // Act
        var result = Check(MembershipType.Renewal, Start, history);

        // Assert
        result.FirstError.Code.ShouldBe("BowlerMembership.RequiresPriorMembership");
    }

    [Fact(DisplayName = "Should pass a type that requires a prior membership when the bowler has a qualifying one")]
    public void CheckEligibility_ShouldPass_WhenQualifyingPriorMembership()
    {
        // Arrange & Act
        var result = Check(MembershipType.Renewal, Start, Held(MembershipType.New, Start.AddYears(-2)));

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should fail a once-per-lifetime type the bowler already held")]
    public void CheckEligibility_ShouldFail_WhenOncePerLifetimeAlreadyHeld()
    {
        // Arrange & Act
        var result = Check(MembershipType.New, Start, Held(MembershipType.New, Start.AddYears(-5)));

        // Assert
        result.FirstError.Code.ShouldBe("BowlerMembership.AlreadyHeld");
    }

    [Fact(DisplayName = "Should pass a once-per-lifetime type when the bowler held only other types")]
    public void CheckEligibility_ShouldPass_WhenOncePerLifetimeHeldOnlyOtherTypes()
    {
        // Arrange & Act
        var result = Check(MembershipType.New, Start, Held(MembershipType.NonMember, Start.AddYears(-5)));

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should pass a minimum-days type at exactly the required days since the last start")]
    public void CheckEligibility_ShouldPass_WhenExactlyMinimumDaysSinceLast()
    {
        // Arrange
        var lastStart = Start.AddDays(-730);

        // Act
        var result = Check(MembershipType.Guest, Start, Held(MembershipType.NonMember, lastStart));

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should fail a minimum-days type one day short of the required days")]
    public void CheckEligibility_ShouldFail_WhenOneDayShortOfMinimumDays()
    {
        // Arrange
        var lastStart = Start.AddDays(-729);

        // Act
        var result = Check(MembershipType.Guest, Start, Held(MembershipType.NonMember, lastStart));

        // Assert
        result.FirstError.Code.ShouldBe("BowlerMembership.TooSoonSinceLast");
    }

    [Fact(DisplayName = "Should measure minimum days from the most recent start of any type")]
    public void CheckEligibility_ShouldUseLatestStartOfAnyType_WhenMinimumDays()
    {
        // Arrange
        var old = Held(MembershipType.New, Start.AddYears(-10));
        var recent = Held(MembershipType.NonMember, Start.AddDays(-100));

        // Act
        var result = Check(MembershipType.Guest, Start, old, recent);

        // Assert
        result.FirstError.Code.ShouldBe("BowlerMembership.TooSoonSinceLast");
    }

    [Fact(DisplayName = "Should fail a temporary type while another temporary membership is valid")]
    public void CheckEligibility_ShouldFail_WhenTemporaryMembershipActive()
    {
        // Arrange
        var saturday = new DateOnly(2025, 6, 7);
        var held = Held(MembershipType.NonMember, saturday);

        // Act
        var sameDay = Check(MembershipType.NonMember, saturday, held);
        var lastDay = Check(MembershipType.NonMember, saturday.AddDays(1), held);

        // Assert
        sameDay.FirstError.Code.ShouldBe("BowlerMembership.TemporaryMembershipActive");
        lastDay.FirstError.Code.ShouldBe("BowlerMembership.TemporaryMembershipActive");
    }

    [Fact(DisplayName = "Should pass a temporary type once the earlier temporary membership has ended")]
    public void CheckEligibility_ShouldPass_WhenTemporaryMembershipEnded()
    {
        // Arrange
        var saturday = new DateOnly(2025, 6, 7);

        // Act
        var result = Check(MembershipType.NonMember, saturday.AddDays(2), Held(MembershipType.NonMember, saturday));

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should pass a yearly type bought while a temporary membership is valid")]
    public void CheckEligibility_ShouldPass_WhenYearlyBoughtDuringTemporary()
    {
        // Arrange
        var saturday = new DateOnly(2025, 6, 7);

        // Act
        var result = Check(MembershipType.New, saturday.AddDays(1), Held(MembershipType.NonMember, saturday));

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should fail a temporary type bought while a yearly membership is valid")]
    public void CheckEligibility_ShouldFail_WhenTemporaryBoughtDuringYearly()
    {
        // Arrange
        var held = Held(MembershipType.New, new DateOnly(2025, 1, 10));

        // Act
        var result = Check(MembershipType.NonMember, new DateOnly(2025, 6, 7), held);

        // Assert
        result.FirstError.Code.ShouldBe("BowlerMembership.ExpiresBeforeCurrent");
    }

    [Fact(DisplayName = "Should fail a second yearly type bought before Nov 1 of the same year")]
    public void CheckEligibility_ShouldFail_WhenYearlyBoughtBeforeRolloverDuringYearly()
    {
        // Arrange
        var held = Held(MembershipType.New, new DateOnly(2025, 1, 10));

        // Act
        var result = Check(MembershipType.Renewal, new DateOnly(2025, 10, 31), held);

        // Assert
        result.FirstError.Code.ShouldBe("BowlerMembership.ExpiresBeforeCurrent");
    }

    [Fact(DisplayName = "Should pass a yearly renewal bought on Nov 1 while a yearly membership is valid")]
    public void CheckEligibility_ShouldPass_WhenRenewalBoughtOnRollover()
    {
        // Arrange
        var held = Held(MembershipType.New, new DateOnly(2025, 1, 10));

        // Act
        var result = Check(MembershipType.Renewal, new DateOnly(2025, 11, 1), held);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should ignore memberships that ended before the start date")]
    public void CheckEligibility_ShouldIgnoreEndedMemberships()
    {
        // Arrange
        var held = Held(MembershipType.New, new DateOnly(2024, 1, 10));

        // Act
        var result = Check(MembershipType.Renewal, new DateOnly(2025, 1, 2), held);

        // Assert
        result.IsError.ShouldBeFalse();
    }
}