using Neba.Api.Caching;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.GetSeries;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.GetSeries;

[UnitTest]
[Component("Series")]
public sealed class GetSeriesQueryTests
{
    [Fact(DisplayName = "Cache should carry the tournaments and bowling centers tags")]
    public void Cache_ShouldCarryTournamentsAndBowlingCentersTags()
    {
        // Arrange
        var id = SeriesId.New();
        var query = new GetSeriesQuery { Id = id };

        // Act
        var descriptor = query.Cache;

        // Assert
        descriptor.Key.ShouldBe(CacheDescriptors.Series.Detail(id).Key);
        descriptor.Tags.ShouldContain(CacheDescriptors.Tournaments.Tag);
        descriptor.Tags.ShouldContain("neba:bowling-centers");
    }

    [Fact(DisplayName = "Cache key should differ for each Series")]
    public void Cache_ShouldUseDistinctKeyPerSeries()
    {
        // Arrange
        var first = new GetSeriesQuery { Id = SeriesId.New() };
        var second = new GetSeriesQuery { Id = SeriesId.New() };

        // Act
        var firstKey = first.Cache.Key;
        var secondKey = second.Cache.Key;

        // Assert
        firstKey.ShouldNotBe(secondKey);
    }

    [Fact(DisplayName = "Expiry should be a positive span")]
    public void Expiry_ShouldBePositive()
    {
        // Arrange
        var query = new GetSeriesQuery { Id = SeriesId.New() };

        // Act
        var expiry = query.Expiry;

        // Assert
        expiry.ShouldBeGreaterThan(TimeSpan.Zero);
    }
}
