using Neba.Api.Caching;
using Neba.Api.Features.Seasons.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Stats.GetSeasonStats;

[UnitTest]
[Component("Stats")]
public sealed class GetSeasonStatsQueryTests
{
    [Fact(DisplayName = "BowlerSeasonStats cache key should follow neba:stats:seasons:{seasonId}:bowlers format")]
    public void BowlerSeasonStats_Key_ShouldFollowExpectedFormat()
    {
        // Arrange
        var seasonId = new SeasonId("01000000000000000000000001");

        // Act
        var descriptor = CacheDescriptors.Stats.BowlerSeasonStats(seasonId);

        // Assert
        descriptor.Key.ShouldBe($"neba:stats:seasons:{seasonId}:bowlers");
    }

    [Fact(DisplayName = "BowlerSeasonStats cache key should be specific to the season")]
    public void BowlerSeasonStats_Key_ShouldBeSpecificToSeason()
    {
        // Act
        var descriptor1 = CacheDescriptors.Stats.BowlerSeasonStats(new SeasonId("01000000000000000000000001"));
        var descriptor2 = CacheDescriptors.Stats.BowlerSeasonStats(new SeasonId("01000000000000000000000002"));

        // Assert
        descriptor1.Key.ShouldNotBe(descriptor2.Key);
    }

    [Fact(DisplayName = "BowlerSeasonStats cache tags should contain neba")]
    public void BowlerSeasonStats_Tags_ShouldContainNebaTag()
    {
        // Act
        var descriptor = CacheDescriptors.Stats.BowlerSeasonStats(SeasonId.New());

        // Assert
        descriptor.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "BowlerSeasonStats cache tags should contain neba:stats")]
    public void BowlerSeasonStats_Tags_ShouldContainStatsTag()
    {
        // Act
        var descriptor = CacheDescriptors.Stats.BowlerSeasonStats(SeasonId.New());

        // Assert
        descriptor.Tags.ShouldContain("neba:stats");
    }

    [Fact(DisplayName = "BowlerSeasonStats cache tags should contain neba:stats:seasons")]
    public void BowlerSeasonStats_Tags_ShouldContainSeasonsTag()
    {
        // Act
        var descriptor = CacheDescriptors.Stats.BowlerSeasonStats(SeasonId.New());

        // Assert
        descriptor.Tags.ShouldContain("neba:stats:seasons");
    }

    [Fact(DisplayName = "BowlerSeasonStats cache tags should contain season-specific tag")]
    public void BowlerSeasonStats_Tags_ShouldContainSeasonSpecificTag()
    {
        // Arrange
        var seasonId = SeasonId.New();

        // Act
        var descriptor = CacheDescriptors.Stats.BowlerSeasonStats(seasonId);

        // Assert
        descriptor.Tags.ShouldContain($"neba:stats:seasons:{seasonId}");
    }

    [Fact(DisplayName = "BowlerSeasonStats cache tags should contain exactly 4 tags")]
    public void BowlerSeasonStats_Tags_ShouldContainExactly4Tags()
    {
        // Act
        var descriptor = CacheDescriptors.Stats.BowlerSeasonStats(SeasonId.New());

        // Assert
        descriptor.Tags.Count.ShouldBe(4);
    }
}
