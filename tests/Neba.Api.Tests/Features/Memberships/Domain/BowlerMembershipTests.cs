using Neba.Api.Contracts.Memberships;
using Neba.Api.Features.Memberships.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Memberships;

namespace Neba.Api.Tests.Features.Memberships.Domain;

[UnitTest]
[Component("Memberships")]
public sealed class BowlerMembershipTests
{
    [Fact(DisplayName = "IsValidOn should be true on the end date")]
    public void IsValidOn_ShouldBeTrue_WhenDateIsEndDate()
    {
        // Arrange
        var membership = BowlerMembershipFactory.Create();

        // Act
        var result = membership.IsValidOn(membership.EndDate);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "IsValidOn should be true before the end date")]
    public void IsValidOn_ShouldBeTrue_WhenDateIsBeforeEndDate()
    {
        // Arrange
        var membership = BowlerMembershipFactory.Create();

        // Act
        var result = membership.IsValidOn(membership.StartDate);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "IsValidOn should be false the day after the end date")]
    public void IsValidOn_ShouldBeFalse_WhenDateIsAfterEndDate()
    {
        // Arrange
        var membership = BowlerMembershipFactory.Create();

        // Act
        var result = membership.IsValidOn(membership.EndDate.AddDays(1));

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "Create should snapshot the type's cost, start date and calculated end date")]
    public void Create_ShouldSetFieldsFromType_WhenEligible()
    {
        // Arrange
        var type = MembershipType.NonMember;
        var start = new DateOnly(2025, 6, 7);

        // Act
        var result = BowlerMembership.Create(type, start, []);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Type.ShouldBe(type);
        result.Value.Cost.ShouldBe(type.Cost);
        result.Value.StartDate.ShouldBe(start);
        result.Value.EndDate.ShouldBe(new DateOnly(2025, 6, 8));
        result.Value.LegacyId.ShouldBeNull();
        result.Value.Id.ShouldNotBe(default);
    }

    [Fact(DisplayName = "Create should return the first broken rule")]
    public void Create_ShouldReturnError_WhenIneligible()
    {
        // Arrange
        var type = MembershipType.Reservation;

        // Act
        var result = BowlerMembership.Create(type, new DateOnly(2025, 6, 7), []);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("BowlerMembership.TypeInactive");
    }

    [Fact(DisplayName = "Factory should build a valid membership for a type that requires a prior membership")]
    public void Factory_ShouldBuildMembership_WhenTypeRequiresPriorMembership()
    {
        // Arrange & Act
        var membership = BowlerMembershipFactory.Create(MembershipType.Renewal);

        // Assert
        membership.Type.ShouldBe(MembershipType.Renewal);
    }
}
