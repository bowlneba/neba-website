using Neba.Api.Caching;
using Neba.Api.Features.Series.ListSeriesFinales;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series.ListSeriesFinales;

[UnitTest]
[Component("Series")]
public sealed class ListSeriesFinalesQueryTests
{
    [Fact(DisplayName = "Cache should carry the tournaments tag, so every Series command clears it")]
    public void Cache_ShouldCarryTournamentsTag()
    {
        // Arrange
        var query = new ListSeriesFinalesQuery();

        // Act
        var descriptor = query.Cache;

        // Assert
        descriptor.Key.ShouldBe(CacheDescriptors.Series.ListFinales.Key);
        descriptor.Tags.ShouldContain(CacheDescriptors.Tournaments.Tag);
    }

    [Fact(DisplayName = "Expiry should be a positive span")]
    public void Expiry_ShouldBePositive()
    {
        // Arrange
        var query = new ListSeriesFinalesQuery();

        // Act
        var expiry = query.Expiry;

        // Assert
        expiry.ShouldBeGreaterThan(TimeSpan.Zero);
    }
}