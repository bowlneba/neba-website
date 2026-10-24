using System.Text.RegularExpressions;

using Ardalis.SmartEnum;

using Neba.Api.Contracts.Memberships;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Contracts.Memberships;

[UnitTest]
[Component("Memberships")]
public sealed partial class MembershipTypeTests
{
    private sealed record Seed(
        string Code, int LegacyId, string Name, MembershipKind Kind, decimal Cost, int? LengthInDays,
        RepeatPolicyKind RepeatKind, int? RepeatDays, bool RequiresPrior, bool QualifiesAsPrior, bool Active);

    private static readonly Seed[] Seeds =
    [
        new("NEW", 1, "New Member", MembershipKind.Yearly, 25.00m, null, RepeatPolicyKind.OncePerLifetime, null, false, true, true),
        new("RENEWAL", 2, "Renewal", MembershipKind.Yearly, 25.00m, null, RepeatPolicyKind.Unlimited, null, true, true, true),
        new("MILITARY", 3, "Military Service", MembershipKind.Yearly, 0.00m, null, RepeatPolicyKind.Unlimited, null, false, true, true),
        new("HONORARY", 4, "Honorary Member", MembershipKind.Yearly, 0.00m, null, RepeatPolicyKind.Unlimited, null, false, true, true),
        new("NON_MEMBER", 5, "Non-Member", MembershipKind.Temporary, 15.00m, 2, RepeatPolicyKind.Unlimited, null, false, false, true),
        new("GUEST", 6, "Guest", MembershipKind.Temporary, 0.00m, 2, RepeatPolicyKind.MinimumDaysSinceLast, 730, false, false, true),
        new("RESERVATION", 7, "Reservation", MembershipKind.Temporary, 0.00m, 2, RepeatPolicyKind.Unlimited, null, false, false, false),
        new("COURTESY", 9, "Courtesy", MembershipKind.Yearly, 0.00m, null, RepeatPolicyKind.Unlimited, null, true, true, false),
        new("TIME_HONORED", 10, "Time Honored Member", MembershipKind.Yearly, 0.00m, null, RepeatPolicyKind.Unlimited, null, true, true, true),
    ];

    public static TheoryData<string> Codes => [.. Seeds.Select(s => s.Code)];

    [GeneratedRegex("^[A-Z]+(_[A-Z]+)*$")]
    private static partial Regex UpperSnakeCase();

    [Fact(DisplayName = "List contains exactly the seeded types")]
    public void List_ShouldContainExactlySeededCodes()
    {
        // Arrange & Act
        var codes = MembershipType.List.Select(t => t.Value);

        // Assert
        codes.ShouldBe(Seeds.Select(s => s.Code), ignoreOrder: true);
    }

    [Theory(DisplayName = "Seeded type matches the seed table")]
    [MemberData(nameof(Codes))]
    public void SeededType_ShouldMatchSeedTable(string code)
    {
        // Arrange
        var seed = Seeds.Single(s => s.Code == code);

        // Act
        var type = MembershipType.FromValue(code);

        // Assert
        type.Value.ShouldBe(seed.Code);
        type.LegacyId.ShouldBe(seed.LegacyId);
        type.Name.ShouldBe(seed.Name);
        type.Kind.ShouldBe(seed.Kind);
        type.Cost.ShouldBe(seed.Cost);
        type.LengthInDays.ShouldBe(seed.LengthInDays);
        type.RepeatPolicy.Kind.ShouldBe(seed.RepeatKind);
        type.RepeatPolicy.Days.ShouldBe(seed.RepeatDays);
        type.RequiresPriorMembership.ShouldBe(seed.RequiresPrior);
        type.QualifiesAsPriorMembership.ShouldBe(seed.QualifiesAsPrior);
        type.Active.ShouldBe(seed.Active);
    }

    [Theory(DisplayName = "FromLegacyId resolves every seeded type")]
    [MemberData(nameof(Codes))]
    public void FromLegacyId_ShouldResolveSeededType(string code)
    {
        // Arrange
        var seed = Seeds.Single(s => s.Code == code);

        // Act
        var type = MembershipType.FromLegacyId(seed.LegacyId);

        // Assert
        type.Value.ShouldBe(code);
    }

    [Theory(DisplayName = "FromLegacyId throws and TryFromLegacyId returns false for unknown ids")]
    [InlineData(8)]
    [InlineData(99)]
    [InlineData(0)]
    public void FromLegacyId_ShouldFail_WhenUnknown(int legacyId)
    {
        // Arrange & Act
        var act = () => MembershipType.FromLegacyId(legacyId);
        var found = MembershipType.TryFromLegacyId(legacyId, out var type);

        // Assert
        Should.Throw<SmartEnumNotFoundException>(act);
        found.ShouldBeFalse();
        type.ShouldBeNull();
    }

    [Theory(DisplayName = "FromValue is case-insensitive")]
    [InlineData("guest")]
    [InlineData("GUEST")]
    [InlineData("Guest")]
    public void FromValue_ShouldReturnSameInstance_RegardlessOfCase(string code)
    {
        // Arrange & Act
        var type = MembershipType.FromValue(code);

        // Assert
        type.ShouldBeSameAs(MembershipType.Guest);
    }

    [Fact(DisplayName = "FromValue throws and TryFromValue returns false for an unknown code")]
    public void FromValue_ShouldFail_WhenUnknown()
    {
        // Arrange & Act
        var act = () => MembershipType.FromValue("SYSTEM_IMPORT");
        var found = MembershipType.TryFromValue("SYSTEM_IMPORT", out var type);

        // Assert
        Should.Throw<SmartEnumNotFoundException>(act);
        found.ShouldBeFalse();
        type.ShouldBeNull();
    }

    [Fact(DisplayName = "Codes are unique, UPPER_SNAKE_CASE and within the length limit")]
    public void Codes_ShouldBeUniqueUpperSnakeCaseAndShort()
    {
        // Arrange & Act
        var codes = MembershipType.List.Select(t => t.Value).ToList();

        // Assert
        codes.Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldBe(codes.Count);
        foreach (var code in codes)
        {
            UpperSnakeCase().IsMatch(code).ShouldBeTrue(code);
            code.Length.ShouldBeLessThanOrEqualTo(MembershipType.MaxCodeLength);
        }
    }

    [Fact(DisplayName = "Names are non-empty and unique")]
    public void Names_ShouldBeNonEmptyAndUnique()
    {
        // Arrange & Act
        var names = MembershipType.List.Select(t => t.Name).ToList();

        // Assert
        names.ShouldAllBe(n => !string.IsNullOrWhiteSpace(n));
        names.Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldBe(names.Count);
    }

    [Fact(DisplayName = "Legacy ids are unique")]
    public void LegacyIds_ShouldBeUnique()
    {
        // Arrange & Act
        var ids = MembershipType.List.Select(t => t.LegacyId).ToList();

        // Assert
        ids.Distinct().Count().ShouldBe(ids.Count);
    }

    [Fact(DisplayName = "Only temporary types have a length, and it is at least 1")]
    public void LengthInDays_ShouldBeSetOnlyForTemporaryTypes()
    {
        // Arrange & Act
        var types = MembershipType.List;

        // Assert
        foreach (var type in types)
        {
            if (type.Kind == MembershipKind.Temporary)
            {
                type.LengthInDays.ShouldNotBeNull();
                type.LengthInDays.Value.ShouldBeGreaterThanOrEqualTo(1);
            }
            else
            {
                type.LengthInDays.ShouldBeNull();
            }
        }
    }
}
