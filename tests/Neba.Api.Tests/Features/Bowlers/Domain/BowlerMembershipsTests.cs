using Neba.Api.Contracts.Memberships;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;

namespace Neba.Api.Tests.Features.Bowlers.Domain;

[UnitTest]
[Component("Memberships")]
public sealed class BowlerMembershipsTests
{
    private static readonly DateOnly Start = new(2025, 3, 15);

    [Fact(DisplayName = "AddMembership should add the membership when the bowler is eligible")]
    public void AddMembership_ShouldAddMembership_WhenEligible()
    {
        // Arrange
        var bowler = BowlerFactory.Create();

        // Act
        var result = bowler.AddMembership(MembershipType.New, Start);

        // Assert
        result.IsError.ShouldBeFalse();
        var membership = bowler.Memberships.ShouldHaveSingleItem();
        membership.Type.ShouldBe(MembershipType.New);
        membership.EndDate.ShouldBe(new DateOnly(2025, 12, 31));
    }

    [Fact(DisplayName = "AddMembership should return the error and add nothing when a rule is broken")]
    public void AddMembership_ShouldReturnErrorAndAddNothing_WhenIneligible()
    {
        // Arrange
        var bowler = BowlerFactory.Create();
        bowler.AddMembership(MembershipType.New, Start);

        // Act
        var result = bowler.AddMembership(MembershipType.NonMember, Start.AddDays(30));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("BowlerMembership.ExpiresBeforeCurrent");
        bowler.Memberships.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "AddMembership should check the rules against memberships already added")]
    public void AddMembership_ShouldUseExistingMemberships_WhenCheckingRules()
    {
        // Arrange
        var bowler = BowlerFactory.Create();
        bowler.AddMembership(MembershipType.NonMember, Start);

        // Act
        var temporaryAgain = bowler.AddMembership(MembershipType.NonMember, Start);
        var yearly = bowler.AddMembership(MembershipType.New, Start.AddDays(1));

        // Assert
        temporaryAgain.FirstError.Code.ShouldBe("BowlerMembership.TemporaryMembershipActive");
        yearly.IsError.ShouldBeFalse();
        bowler.Memberships.Count.ShouldBe(2);
    }
}
