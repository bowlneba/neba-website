using Neba.Api.Features.HallOfFame.ListHallOfFamePoints;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.HallOfFame.ListHallOfFamePoints;

[UnitTest]
[Component("HallOfFame")]
public sealed class ListHallOfFamePointsQueryTests
{
    [Fact(DisplayName = "Expiry should be 14 days")]
    public void Expiry_ShouldBe14Days()
    {
        // Act
        var query = new ListHallOfFamePointsQuery();

        // Assert
        query.Expiry.ShouldBe(TimeSpan.FromDays(14));
    }

    [Fact(DisplayName = "Cache key should be neba:hall-of-fame:points:list")]
    public void Cache_Key_ShouldBeExpectedValue()
    {
        // Act
        var query = new ListHallOfFamePointsQuery();

        // Assert
        query.Cache.Key.ShouldBe("neba:hall-of-fame:points:list");
    }

    [Fact(DisplayName = "Cache tags should be neba, neba:hall-of-fame, the points tag and the bowlers tag")]
    public void Cache_Tags_ShouldBeExpected()
    {
        // Act
        var query = new ListHallOfFamePointsQuery();

        // Assert
        query.Cache.Tags.ShouldBe(["neba", "neba:hall-of-fame", "neba:hall-of-fame:points", "neba:bowlers"], ignoreOrder: true);
    }
}