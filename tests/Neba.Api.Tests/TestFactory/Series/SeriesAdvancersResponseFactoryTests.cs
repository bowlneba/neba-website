using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;

namespace Neba.Api.Tests.TestFactory.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesAdvancersResponseFactoryTests
{
    [Fact(DisplayName = "SeriesAdvancersResponseFactory Create should return one Stage with one qualifier with no arguments")]
    public void SeriesAdvancersResponseFactory_Create_ShouldReturnDefaults()
    {
        // Arrange & Act
        var response = SeriesAdvancersResponseFactory.Create();

        // Assert
        var stage = response.Stages.Single();
        stage.StageId.ShouldBe(StageAdvancersResponseFactory.ValidStageId);
        stage.Order.ShouldBe(StageAdvancersResponseFactory.ValidOrder);
        stage.Kind.ShouldBe(StageAdvancersResponseFactory.ValidKind);
        var advancer = stage.Advancers.Single();
        advancer.BowlerId.ShouldBe(AdvancerResponseFactory.ValidBowlerId);
        advancer.BowlerName.ShouldBe(AdvancerResponseFactory.ValidBowlerName);
        advancer.Place.ShouldBe(AdvancerResponseFactory.ValidPlace);
        advancer.BlockTotal.ShouldBeNull();
        advancer.IsBackfill.ShouldBeFalse();
    }

    [Fact(DisplayName = "SeriesAdvancersResponseFactory Create should use the given values")]
    public void SeriesAdvancersResponseFactory_Create_ShouldUseGivenValues()
    {
        // Arrange
        var advancer = AdvancerResponseFactory.Create("bowler", "Pat Plain", place: null, blockTotal: 1500, isBackfill: true);
        var stage = StageAdvancersResponseFactory.Create("stage", 3, "Survivor", [advancer]);

        // Act
        var response = SeriesAdvancersResponseFactory.Create([stage]);

        // Assert
        var mapped = response.Stages.Single();
        mapped.StageId.ShouldBe("stage");
        mapped.Order.ShouldBe(3);
        mapped.Kind.ShouldBe("Survivor");
        var mappedAdvancer = mapped.Advancers.Single();
        mappedAdvancer.BowlerId.ShouldBe("bowler");
        mappedAdvancer.BowlerName.ShouldBe("Pat Plain");
        mappedAdvancer.Place.ShouldBeNull();
        mappedAdvancer.BlockTotal.ShouldBe(1500);
        mappedAdvancer.IsBackfill.ShouldBeTrue();
    }

    [Fact(DisplayName = "SeriesAdvancersResponseFactory Bogus should return the requested count of Stages, ordered from 1")]
    public void SeriesAdvancersResponseFactory_Bogus_ShouldReturnRequestedCount()
    {
        // Arrange & Act
        var responses = SeriesAdvancersResponseFactory.Bogus(2, 31);
        var stages = StageAdvancersResponseFactory.Bogus(3, 32);
        var advancers = AdvancerResponseFactory.Bogus(4, 33);

        // Assert
        responses.Count.ShouldBe(2);
        responses.ShouldAllBe(r => r.Stages.Count == 2);
        stages.Select(s => s.Order).ShouldBe([1, 2, 3]);
        stages.ShouldAllBe(s => s.Advancers.Count > 0);
        advancers.Select(a => a.Place).ShouldBe([1, 2, 3, 4]);
    }
}
