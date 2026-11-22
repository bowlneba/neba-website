using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.Domain;

[UnitTest]
[Component("Series")]
public sealed class StageOverrideTests
{
    [Fact(DisplayName = "Create should return an applied override carrying the kind, bowler, reason, creator and time")]
    public void Create_ShouldReturnAppliedOverride_WhenInputIsValid()
    {
        // Arrange
        var bowlerId = BowlerId.New();

        // Act
        var result = StageOverride.Create(
            OverrideKind.Clear, bowlerId, StageOverrideFactory.ValidReason, StageOverrideFactory.ValidCreatedBy, StageOverrideFactory.ValidCreatedAt);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Kind.ShouldBe(OverrideKind.Clear);
        result.Value.BowlerId.ShouldBe(bowlerId);
        result.Value.Reason.ShouldBe(StageOverrideFactory.ValidReason);
        result.Value.CreatedBy.ShouldBe(StageOverrideFactory.ValidCreatedBy);
        result.Value.CreatedAt.ShouldBe(StageOverrideFactory.ValidCreatedAt);
        result.Value.Applied.ShouldBeTrue();
    }

    [Fact(DisplayName = "Create should give each override its own ID")]
    public void Create_ShouldGiveEachOverrideItsOwnId()
    {
        // Arrange & Act
        var first = StageOverrideFactory.Create();
        var second = StageOverrideFactory.Create();

        // Assert
        first.Id.ShouldNotBe(second.Id);
    }

    [Fact(DisplayName = "Create should trim the reason")]
    public void Create_ShouldTrimReason()
    {
        // Arrange & Act
        var created = StageOverrideFactory.Create(reason: "  Withdrew.  ");

        // Assert
        created.Reason.ShouldBe("Withdrew.");
    }

    [Theory(DisplayName = "Create should return ReasonRequired for an empty or blank reason")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Create_ShouldReturnReasonRequired_WhenReasonIsBlank(string reason)
    {
        // Arrange & Act
        var result = StageOverride.Create(
            OverrideKind.Add, BowlerId.New(), reason, StageOverrideFactory.ValidCreatedBy, StageOverrideFactory.ValidCreatedAt);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Series.Override.Reason.Required");
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
    }

#nullable disable
    [Fact(DisplayName = "Create should return ReasonRequired for a null reason")]
    public void Create_ShouldReturnReasonRequired_WhenReasonIsNull()
    {
        // Arrange & Act
        var result = StageOverride.Create(
            OverrideKind.Add, BowlerId.New(), null, StageOverrideFactory.ValidCreatedBy, StageOverrideFactory.ValidCreatedAt);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Override.Reason.Required");
    }

    [Fact(DisplayName = "Create should throw when the kind is null")]
    public void Create_ShouldThrow_WhenKindIsNull()
    {
        // Arrange & Act
        static void Act() => _ = StageOverride.Create(
            null, BowlerId.New(), StageOverrideFactory.ValidReason, StageOverrideFactory.ValidCreatedBy, StageOverrideFactory.ValidCreatedAt);

        // Assert
        Should.Throw<ArgumentNullException>(Act);
    }
#nullable enable

    [Fact(DisplayName = "Create should return ReasonTooLong for a reason over 500 characters")]
    public void Create_ShouldReturnReasonTooLong_WhenReasonIsOver500Characters()
    {
        // Arrange & Act
        var result = StageOverride.Create(
            OverrideKind.Add, BowlerId.New(), new string('r', 501), StageOverrideFactory.ValidCreatedBy, StageOverrideFactory.ValidCreatedAt);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Series.Override.Reason.TooLong");
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
        result.FirstError.Description.ShouldContain("500");
    }

    [Theory(DisplayName = "Create should accept a reason of 1 or 500 characters")]
    [InlineData(1)]
    [InlineData(500)]
    public void Create_ShouldAcceptReason_WhenLengthIsAtTheLimits(int length)
    {
        // Arrange & Act
        var created = StageOverrideFactory.Create(reason: new string('r', length));

        // Assert
        created.Reason.Length.ShouldBe(length);
    }
}