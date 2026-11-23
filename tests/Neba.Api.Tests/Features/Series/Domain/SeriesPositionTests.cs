using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.Features.Series.Domain;

[UnitTest]
[Component("Series")]
public sealed class SeriesPositionTests
{
    [Theory(DisplayName = "Create should return a position with the finalist flag as given, including none")]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public void Create_ShouldReturnPosition_WhenInputIsValid(bool? isFinalist)
    {
        // Arrange
        var bowlerId = BowlerId.New();

        // Act
        var result = SeriesPosition.Create(bowlerId, 5, true, isFinalist);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.BowlerId.ShouldBe(bowlerId);
        result.Value.Position.ShouldBe(5);
        result.Value.Bowled.ShouldBeTrue();
        result.Value.IsFinalist.ShouldBe(isFinalist);
    }

    [Theory(DisplayName = "Create should return PositionInvalid for a position of zero or less")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldReturnPositionInvalid_WhenPositionIsNotPositive(int position)
    {
        // Arrange & Act
        var result = SeriesPosition.Create(BowlerId.New(), position, true, null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Series.Position.Invalid");
        result.FirstError.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact(DisplayName = "Update should change the position, bowled flag and finalist flag")]
    public void Update_ShouldChangeAllFields()
    {
        // Arrange
        var position = SeriesPositionFactory.Create(position: 1, bowled: true, isFinalist: null);

        // Act
        var result = position.Update(7, false, true);

        // Assert
        result.IsError.ShouldBeFalse();
        position.Position.ShouldBe(7);
        position.Bowled.ShouldBeFalse();
        position.IsFinalist.ShouldBe(true);
    }

    [Fact(DisplayName = "Update should return PositionInvalid and change nothing when the position is not positive")]
    public void Update_ShouldReturnPositionInvalid_AndChangeNothing()
    {
        // Arrange
        var position = SeriesPositionFactory.Create(position: 3, bowled: true, isFinalist: true);

        // Act
        var result = position.Update(0, false, false);

        // Assert
        result.FirstError.Code.ShouldBe("Series.Position.Invalid");
        position.Position.ShouldBe(3);
        position.Bowled.ShouldBeTrue();
        position.IsFinalist.ShouldBe(true);
    }
}