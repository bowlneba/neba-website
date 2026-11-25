using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.TestFactory.Series;

[UnitTest]
[Component("Series")]
public sealed class AlreadyAdvancedResponseFactoryTests
{
    [Fact(DisplayName = "AlreadyAdvancedResponseFactory Create should return the defaults with no arguments")]
    public void AlreadyAdvancedResponseFactory_Create_ShouldReturnDefaults()
    {
        // Arrange

        // Act
        var response = AlreadyAdvancedResponseFactory.Create();

        // Assert
        response.BowlerId.ShouldBe(AlreadyAdvancedResponseFactory.ValidBowlerId);
        response.BowlerName.ShouldBe(AlreadyAdvancedResponseFactory.ValidBowlerName);
        response.Place.ShouldBe(AlreadyAdvancedResponseFactory.ValidPlace);
        response.EarlierStageId.ShouldBe(AlreadyAdvancedResponseFactory.ValidEarlierStageId);
        response.EarlierStageOrder.ShouldBe(AlreadyAdvancedResponseFactory.ValidEarlierStageOrder);
    }

    [Fact(DisplayName = "AlreadyAdvancedResponseFactory Create should use the given values")]
    public void AlreadyAdvancedResponseFactory_Create_ShouldUseGivenValues()
    {
        // Arrange

        // Act
        var response = AlreadyAdvancedResponseFactory.Create("bowler", "Pat Plain", 4, "stage", 3);

        // Assert
        response.BowlerId.ShouldBe("bowler");
        response.BowlerName.ShouldBe("Pat Plain");
        response.Place.ShouldBe(4);
        response.EarlierStageId.ShouldBe("stage");
        response.EarlierStageOrder.ShouldBe(3);
    }

    [Fact(DisplayName = "AlreadyAdvancedResponseFactory Bogus should return the requested count with ranked places")]
    public void AlreadyAdvancedResponseFactory_Bogus_ShouldReturnRankedCollection()
    {
        // Arrange

        // Act
        var responses = AlreadyAdvancedResponseFactory.Bogus(3);

        // Assert
        responses.Count.ShouldBe(3);
        responses.Select(r => r.Place).ShouldBe([1, 2, 3]);
    }

    [Fact(DisplayName = "AlreadyAdvancedResponseFactory Bogus should return the same data for the same seed")]
    public void AlreadyAdvancedResponseFactory_Bogus_ShouldBeRepeatable_WhenSeeded()
    {
        // Arrange
        const int seed = 8841;

        // Act
        var first = AlreadyAdvancedResponseFactory.Bogus(2, seed);
        var second = AlreadyAdvancedResponseFactory.Bogus(2, seed);

        // Assert
        first.ShouldBe(second);
    }
}
