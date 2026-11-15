using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.HallOfFame.GetHallOfFamePointsDetail;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.HallOfFame.GetHallOfFamePointsDetail;

[UnitTest]
[Component("HallOfFame")]
public sealed class GetHallOfFamePointsDetailQueryTests
{
    private static readonly BowlerId Bowler = new("01JZ8K3M4N5P6Q7R8S9T0V1W2X");

    [Fact(DisplayName = "Expiry should be 14 days")]
    public void Expiry_ShouldBe14Days()
    {
        // Act
        var query = new GetHallOfFamePointsDetailQuery { BowlerId = Bowler };

        // Assert
        query.Expiry.ShouldBe(TimeSpan.FromDays(14));
    }

    [Fact(DisplayName = "Cache key should include the bowler id")]
    public void Cache_Key_ShouldIncludeBowlerId()
    {
        // Act
        var query = new GetHallOfFamePointsDetailQuery { BowlerId = Bowler };

        // Assert
        query.Cache.Key.ShouldBe($"neba:hall-of-fame:points:{Bowler}");
    }

    [Fact(DisplayName = "Cache tags should hold the points tag, the bowlers tag and the bowler tag")]
    public void Cache_Tags_ShouldHoldPointsAndBowlerTags()
    {
        // Act
        var query = new GetHallOfFamePointsDetailQuery { BowlerId = Bowler };

        // Assert
        query.Cache.Tags.ShouldBe(
            ["neba", "neba:hall-of-fame", "neba:hall-of-fame:points", "neba:bowlers", $"neba:bowlers:{Bowler}"],
            ignoreOrder: true);
    }
}