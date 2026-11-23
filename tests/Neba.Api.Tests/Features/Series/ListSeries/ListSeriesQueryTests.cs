using Neba.Api.Caching;
using Neba.Api.Features.Series.ListSeries;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.ListSeries;

[UnitTest]
[Component("Series")]
public sealed class ListSeriesQueryTests
{
    [Fact(DisplayName = "Cache should carry the tournaments tag, so every Series command clears it")]
    public void Cache_ShouldCarryTournamentsTag()
    {
        // Arrange
        var query = new ListSeriesQuery();

        // Act
        var descriptor = query.Cache;

        // Assert
        descriptor.Key.ShouldBe(CacheDescriptors.Series.List.Key);
        descriptor.Tags.ShouldContain(CacheDescriptors.Tournaments.Tag);
    }

    [Fact(DisplayName = "Expiry should be a positive span of at most a day, because the next Stage depends on today's date")]
    public void Expiry_ShouldBeShort()
    {
        // Arrange
        var query = new ListSeriesQuery();

        // Act
        var expiry = query.Expiry;

        // Assert
        expiry.ShouldBeGreaterThan(TimeSpan.Zero);
        expiry.ShouldBeLessThanOrEqualTo(TimeSpan.FromDays(1));
    }
}