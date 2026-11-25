using Neba.Api.Caching;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.GetSeriesAdvancers;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.GetSeriesAdvancers;

[UnitTest]
[Component("Series")]
public sealed class GetSeriesAdvancersQueryTests
{
    [Fact(DisplayName = "Cache should carry the tournaments and Series tags")]
    public void Cache_ShouldCarryTournamentsAndSeriesTags()
    {
        // Arrange
        var id = SeriesId.New();
        var query = new GetSeriesAdvancersQuery { SeriesId = id };

        // Act
        var descriptor = query.Cache;

        // Assert
        descriptor.Key.ShouldBe(CacheDescriptors.Series.Advancers(id).Key);
        descriptor.Tags.ShouldContain(CacheDescriptors.Tournaments.Tag);
        descriptor.Tags.ShouldContain(CacheDescriptors.Series.Tag);
    }

    [Fact(DisplayName = "Cache key should differ for each Series and from the detail key")]
    public void Cache_ShouldUseDistinctKeyPerSeries()
    {
        // Arrange
        var id = SeriesId.New();
        var first = new GetSeriesAdvancersQuery { SeriesId = id };
        var second = new GetSeriesAdvancersQuery { SeriesId = SeriesId.New() };

        // Act
        var firstKey = first.Cache.Key;
        var secondKey = second.Cache.Key;

        // Assert
        firstKey.ShouldNotBe(secondKey);
        firstKey.ShouldNotBe(CacheDescriptors.Series.Detail(id).Key);
    }

    [Fact(DisplayName = "Expiry should be a positive span")]
    public void Expiry_ShouldBePositive()
    {
        // Arrange
        var query = new GetSeriesAdvancersQuery { SeriesId = SeriesId.New() };

        // Act
        var expiry = query.Expiry;

        // Assert
        expiry.ShouldBeGreaterThan(TimeSpan.Zero);
    }
}
