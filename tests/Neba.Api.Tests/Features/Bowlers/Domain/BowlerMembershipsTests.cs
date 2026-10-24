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

    [Fact(DisplayName = "AddLegacyMembership should add the record as stored without running eligibility rules")]
    public void AddLegacyMembership_ShouldAddAsStored_WhenValid()
    {
        // Arrange
        var bowler = BowlerFactory.Create();
        bowler.AddLegacyMembership(1, MembershipType.New, 25m, new DateOnly(2020, 1, 5), new DateOnly(2020, 12, 31));

        // Act - overlaps the first and ends earlier, which AddMembership would reject
        var result = bowler.AddLegacyMembership(2, MembershipType.NonMember, 15m, new DateOnly(2020, 6, 6), new DateOnly(2020, 6, 7));

        // Assert
        result.IsError.ShouldBeFalse();
        bowler.Memberships.Count.ShouldBe(2);
        bowler.Memberships.Single(m => m.LegacyId == 2).Cost.ShouldBe(15m);
    }

    [Fact(DisplayName = "AddLegacyMembership should reject a legacy id the bowler already has")]
    public void AddLegacyMembership_ShouldReturnError_WhenLegacyIdAlreadyAdded()
    {
        // Arrange
        var bowler = BowlerFactory.Create();
        bowler.AddLegacyMembership(1, MembershipType.New, 25m, new DateOnly(2020, 1, 5), new DateOnly(2020, 12, 31));

        // Act
        var result = bowler.AddLegacyMembership(1, MembershipType.Renewal, 25m, new DateOnly(2021, 1, 5), new DateOnly(2021, 12, 31));

        // Assert
        result.FirstError.Code.ShouldBe("BowlerMembership.LegacyIdAlreadyAdded");
        bowler.Memberships.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "AddLegacyMembership should return the structural error and add nothing")]
    public void AddLegacyMembership_ShouldReturnErrorAndAddNothing_WhenRecordInvalid()
    {
        // Arrange
        var bowler = BowlerFactory.Create();

        // Act
        var result = bowler.AddLegacyMembership(1, MembershipType.New, -1m, new DateOnly(2020, 1, 5), new DateOnly(2020, 12, 31));

        // Assert
        result.IsError.ShouldBeTrue();
        bowler.Memberships.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Legacy memberships should count as history for later purchases")]
    public void AddMembership_ShouldUseLegacyMemberships_WhenCheckingRules()
    {
        // Arrange
        var bowler = BowlerFactory.Create();
        bowler.AddLegacyMembership(1, MembershipType.New, 25m, new DateOnly(2024, 2, 1), new DateOnly(2024, 12, 31));

        // Act
        var newAgain = bowler.AddMembership(MembershipType.New, new DateOnly(2025, 2, 1));
        var renewal = bowler.AddMembership(MembershipType.Renewal, new DateOnly(2025, 2, 1));

        // Assert
        newAgain.FirstError.Code.ShouldBe("BowlerMembership.AlreadyHeld");
        renewal.IsError.ShouldBeFalse();
    }
}
