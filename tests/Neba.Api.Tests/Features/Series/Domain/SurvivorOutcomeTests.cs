using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.Domain;

[UnitTest]
[Component("Series")]
public sealed class SurvivorOutcomeTests
{
    [Fact(DisplayName = "Create should return an advancing outcome with a block total")]
    public void Create_ShouldReturnOutcome_WhenAdvancedWithTotal()
    {
        // Arrange
        var bowlerId = BowlerId.New();

        // Act
        var result = SurvivorOutcome.Create(bowlerId, 1250, true);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.BowlerId.ShouldBe(bowlerId);
        result.Value.BlockTotal.ShouldBe(1250);
        result.Value.Advanced.ShouldBeTrue();
    }

    [Fact(DisplayName = "Create should return an eliminated outcome without a block total")]
    public void Create_ShouldReturnOutcome_WhenNotAdvancedAndNoTotal()
    {
        // Arrange & Act
        var result = SurvivorOutcome.Create(BowlerId.New(), null, false);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.BlockTotal.ShouldBeNull();
        result.Value.Advanced.ShouldBeFalse();
    }

    [Theory(DisplayName = "Create should accept a block total of zero")]
    [InlineData(true)]
    [InlineData(false)]
    public void Create_ShouldAcceptZeroTotal(bool advanced)
    {
        // Arrange & Act
        var result = SurvivorOutcome.Create(BowlerId.New(), 0, advanced);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.BlockTotal.ShouldBe(0);
    }

    [Theory(DisplayName = "Create should return OutcomeInvalid for a negative total or an advancing bowler with no total")]
    [InlineData(-1, false)]
    [InlineData(-1, true)]
    [InlineData(null, true)]
    public void Create_ShouldReturnOutcomeInvalid_WhenOutcomeIsMalformed(int? total, bool advanced)
    {
        // Arrange & Act
        var result = SurvivorOutcome.Create(BowlerId.New(), total, advanced);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Series.SurvivorOutcome.Invalid");
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact(DisplayName = "Update should change the total and the flag")]
    public void Update_ShouldChangeTotalAndFlag()
    {
        // Arrange
        var outcome = SurvivorOutcomeFactory.Create(blockTotal: 1000, advanced: true);

        // Act
        var result = outcome.Update(900, false);

        // Assert
        result.IsError.ShouldBeFalse();
        outcome.BlockTotal.ShouldBe(900);
        outcome.Advanced.ShouldBeFalse();
    }

    [Fact(DisplayName = "Update should return OutcomeInvalid and change nothing when the new values break the invariant")]
    public void Update_ShouldReturnOutcomeInvalid_AndChangeNothing()
    {
        // Arrange
        var outcome = SurvivorOutcomeFactory.Create(blockTotal: 1000, advanced: true);

        // Act
        var result = outcome.Update(null, true);

        // Assert
        result.FirstError.Code.ShouldBe("Series.SurvivorOutcome.Invalid");
        outcome.BlockTotal.ShouldBe(1000);
        outcome.Advanced.ShouldBeTrue();
    }
}