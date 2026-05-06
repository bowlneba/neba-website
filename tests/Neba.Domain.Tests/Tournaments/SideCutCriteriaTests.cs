using Neba.Domain.Bowlers;
using Neba.Domain.Tournaments;
using Neba.TestFactory.Attributes;

namespace Neba.Domain.Tests.Tournaments;

[UnitTest]
[Component("Tournaments.SideCutCriteria")]
public sealed class SideCutCriteriaTests
{
    // ── CreateAgeRequirement ──────────────────────────────────────────────────

    [Fact(DisplayName = "CreateAgeRequirement returns SideCutCriteria with MinimumAge set when only minimumAge is provided")]
    public void CreateAgeRequirement_ShouldSetMinimumAge_WhenOnlyMinimumAgeProvided()
    {
        var result = SideCutCriteria.CreateAgeRequirement(minimumAge: 50, maximumAge: null);

        result.IsError.ShouldBeFalse();
        result.Value.MinimumAge.ShouldBe(50);
    }

    [Fact(DisplayName = "CreateAgeRequirement returns SideCutCriteria with MaximumAge null when only minimumAge is provided")]
    public void CreateAgeRequirement_ShouldLeaveMaximumAgeNull_WhenOnlyMinimumAgeProvided()
    {
        var result = SideCutCriteria.CreateAgeRequirement(minimumAge: 50, maximumAge: null);

        result.IsError.ShouldBeFalse();
        result.Value.MaximumAge.ShouldBeNull();
    }

    [Fact(DisplayName = "CreateAgeRequirement returns SideCutCriteria with MaximumAge set when only maximumAge is provided")]
    public void CreateAgeRequirement_ShouldSetMaximumAge_WhenOnlyMaximumAgeProvided()
    {
        var result = SideCutCriteria.CreateAgeRequirement(minimumAge: null, maximumAge: 17);

        result.IsError.ShouldBeFalse();
        result.Value.MaximumAge.ShouldBe(17);
    }

    [Fact(DisplayName = "CreateAgeRequirement returns SideCutCriteria with MinimumAge null when only maximumAge is provided")]
    public void CreateAgeRequirement_ShouldLeaveMinimumAgeNull_WhenOnlyMaximumAgeProvided()
    {
        var result = SideCutCriteria.CreateAgeRequirement(minimumAge: null, maximumAge: 17);

        result.IsError.ShouldBeFalse();
        result.Value.MinimumAge.ShouldBeNull();
    }

    [Fact(DisplayName = "CreateAgeRequirement returns SideCutCriteria with both MinimumAge and MaximumAge set when both are provided")]
    public void CreateAgeRequirement_ShouldSetBothAges_WhenBothProvided()
    {
        var result = SideCutCriteria.CreateAgeRequirement(minimumAge: 14, maximumAge: 17);

        result.IsError.ShouldBeFalse();
        result.Value.MinimumAge.ShouldBe(14);
        result.Value.MaximumAge.ShouldBe(17);
    }

    [Fact(DisplayName = "CreateAgeRequirement returns SideCutCriteria when minimumAge equals maximumAge")]
    public void CreateAgeRequirement_ShouldReturnSuccess_WhenMinimumAgeEqualsMaximumAge()
    {
        var result = SideCutCriteria.CreateAgeRequirement(minimumAge: 18, maximumAge: 18);

        result.IsError.ShouldBeFalse();
        result.Value.MinimumAge.ShouldBe(18);
        result.Value.MaximumAge.ShouldBe(18);
    }

    [Fact(DisplayName = "CreateAgeRequirement returns SideCutCriteria when minimumAge is zero")]
    public void CreateAgeRequirement_ShouldReturnSuccess_WhenMinimumAgeIsZero()
    {
        var result = SideCutCriteria.CreateAgeRequirement(minimumAge: 0, maximumAge: null);

        result.IsError.ShouldBeFalse();
        result.Value.MinimumAge.ShouldBe(0);
    }

    [Fact(DisplayName = "CreateAgeRequirement returns SideCutCriteria.MinimumAgeInvalid when minimumAge is negative")]
    public void CreateAgeRequirement_ShouldReturnError_WhenMinimumAgeIsNegative()
    {
        var result = SideCutCriteria.CreateAgeRequirement(minimumAge: -1, maximumAge: null);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("SideCutCriteria.MinimumAgeInvalid");
    }

    [Fact(DisplayName = "CreateAgeRequirement returns SideCutCriteria.MaximumAgeInvalid when maximumAge is negative")]
    public void CreateAgeRequirement_ShouldReturnError_WhenMaximumAgeIsNegative()
    {
        var result = SideCutCriteria.CreateAgeRequirement(minimumAge: null, maximumAge: -1);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("SideCutCriteria.MaximumAgeInvalid");
    }

    [Fact(DisplayName = "CreateAgeRequirement returns SideCutCriteria.AgeRangeInvalid when minimumAge is greater than maximumAge")]
    public void CreateAgeRequirement_ShouldReturnError_WhenMinimumAgeIsGreaterThanMaximumAge()
    {
        var result = SideCutCriteria.CreateAgeRequirement(minimumAge: 60, maximumAge: 50);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("SideCutCriteria.AgeRangeInvalid");
    }

    [Fact(DisplayName = "CreateAgeRequirement returns SideCutCriteria.BothAgesRequired when both minimumAge and maximumAge are null")]
    public void CreateAgeRequirement_ShouldReturnError_WhenBothAgesAreNull()
    {
        var result = SideCutCriteria.CreateAgeRequirement(null, null);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("SideCutCriteria.BothAgesRequired");
    }

    // ── CreateGenderRequirement ───────────────────────────────────────────────

    [Theory(DisplayName = "CreateGenderRequirement returns SideCutCriteria with GenderRequirement set")]
    [MemberData(nameof(AllGenders))]
    public void CreateGenderRequirement_ShouldSetGenderRequirement(Gender gender)
    {
        var result = SideCutCriteria.CreateGenderRequirement(gender);

        result.IsError.ShouldBeFalse();
        result.Value.GenderRequirement.ShouldBe(gender);
    }

    [Fact(DisplayName = "CreateGenderRequirement returns SideCutCriteria with MinimumAge null")]
    public void CreateGenderRequirement_ShouldLeaveMinimumAgeNull()
    {
        var result = SideCutCriteria.CreateGenderRequirement(Gender.Female);

        result.IsError.ShouldBeFalse();
        result.Value.MinimumAge.ShouldBeNull();
    }

    [Fact(DisplayName = "CreateGenderRequirement returns SideCutCriteria with MaximumAge null")]
    public void CreateGenderRequirement_ShouldLeaveMaximumAgeNull()
    {
        var result = SideCutCriteria.CreateGenderRequirement(Gender.Female);

        result.IsError.ShouldBeFalse();
        result.Value.MaximumAge.ShouldBeNull();
    }

#nullable disable
    [Fact(DisplayName = "CreateGenderRequirement throws ArgumentNullException when gender is null")]
    public void CreateGenderRequirement_ShouldThrow_WhenGenderIsNull()
    {
        Action act = () => SideCutCriteria.CreateGenderRequirement(null);

        act.ShouldThrow<ArgumentNullException>();
    }
#nullable enable

    public static TheoryData<Gender> AllGenders() => [.. Gender.List];
}