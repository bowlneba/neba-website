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

    [Fact(DisplayName = "Create should use the given id")]
    public void Create_ShouldUseGivenId_WhenIdProvided()
    {
        // Arrange
        var id = BowlerMembershipId.New();

        // Act
        var result = BowlerMembership.Create(MembershipType.New, new DateOnly(2025, 3, 15), [], id);

        // Assert
        result.Value.Id.ShouldBe(id);
    }

    [Fact(DisplayName = "CreateFromLegacy should copy cost and dates exactly as stored")]
    public void CreateFromLegacy_ShouldCopyStoredValues_WhenValid()
    {
        // Arrange
        var start = new DateOnly(2019, 11, 3);
        var end = new DateOnly(2021, 1, 15);

        // Act
        var result = BowlerMembership.CreateFromLegacy(4321, MembershipType.Renewal, 20.00m, start, end);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.LegacyId.ShouldBe(4321);
        result.Value.Type.ShouldBe(MembershipType.Renewal);
        result.Value.Cost.ShouldBe(20.00m);
        result.Value.StartDate.ShouldBe(start);
        result.Value.EndDate.ShouldBe(end);
    }

    [Theory(DisplayName = "CreateFromLegacy should copy records for inactive types")]
    [InlineData("RESERVATION", TestDisplayName = "Reservation is copied")]
    [InlineData("COURTESY", TestDisplayName = "Courtesy is copied")]
    public void CreateFromLegacy_ShouldCopy_WhenTypeInactive(string code)
    {
        // Arrange
        var type = MembershipType.FromValue(code);

        // Act
        var result = BowlerMembership.CreateFromLegacy(77, type, 0m, new DateOnly(2021, 5, 1), new DateOnly(2021, 5, 2));

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Type.ShouldBe(type);
    }

    [Theory(DisplayName = "CreateFromLegacy should reject a legacy id that is not positive")]
    [InlineData(0, TestDisplayName = "Zero is rejected")]
    [InlineData(-5, TestDisplayName = "A negative id is rejected")]
    public void CreateFromLegacy_ShouldReturnError_WhenLegacyIdNotPositive(int legacyId)
    {
        // Arrange & Act
        var result = BowlerMembership.CreateFromLegacy(legacyId, MembershipType.New, 25m, new DateOnly(2021, 1, 1), new DateOnly(2021, 12, 31));

        // Assert
        result.FirstError.ShouldBe(BowlerMembershipErrors.InvalidLegacyId);
    }

    [Fact(DisplayName = "CreateFromLegacy should reject a negative cost")]
    public void CreateFromLegacy_ShouldReturnError_WhenCostNegative()
    {
        // Arrange & Act
        var result = BowlerMembership.CreateFromLegacy(1, MembershipType.New, -0.01m, new DateOnly(2021, 1, 1), new DateOnly(2021, 12, 31));

        // Assert
        result.FirstError.ShouldBe(BowlerMembershipErrors.NegativeCost);
    }

    [Fact(DisplayName = "CreateFromLegacy should reject an end date before the start date")]
    public void CreateFromLegacy_ShouldReturnError_WhenEndBeforeStart()
    {
        // Arrange & Act
        var result = BowlerMembership.CreateFromLegacy(1, MembershipType.New, 25m, new DateOnly(2021, 12, 31), new DateOnly(2021, 1, 1));

        // Assert
        result.FirstError.Code.ShouldBe("BowlerMembership.EndDateBeforeStartDate");
    }

    [Fact(DisplayName = "CreateFromLegacy should accept an end date equal to the start date")]
    public void CreateFromLegacy_ShouldAccept_WhenEndEqualsStart()
    {
        // Arrange
        var day = new DateOnly(2021, 6, 5);

        // Act
        var result = BowlerMembership.CreateFromLegacy(1, MembershipType.NonMember, 15m, day, day);

        // Assert
        result.IsError.ShouldBeFalse();
    }

    [Fact(DisplayName = "Factory should give the same ids for the same seed")]
    public void Factory_ShouldBeDeterministic_WhenSeedRepeated()
    {
        // Arrange & Act
        var first = BowlerMembershipFactory.Bogus(5, seed: 11);
        var second = BowlerMembershipFactory.Bogus(5, seed: 11);

        // Assert
        first.Select(m => m.Id).ShouldBe(second.Select(m => m.Id));
        first.Select(m => m.StartDate).ShouldBe(second.Select(m => m.StartDate));
    }
}
