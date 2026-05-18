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
        var seasonId = new SeasonId("01000000000000000000000001");

        var descriptor = CacheDescriptors.Stats.BowlerSeasonStats(seasonId);

        descriptor.Key.ShouldBe($"neba:stats:seasons:{seasonId}:bowlers");
    }

    [Fact(DisplayName = "BowlerSeasonStats cache key should be specific to the season")]
    public void BowlerSeasonStats_Key_ShouldBeSpecificToSeason()
    {
        var descriptor1 = CacheDescriptors.Stats.BowlerSeasonStats(new SeasonId("01000000000000000000000001"));
        var descriptor2 = CacheDescriptors.Stats.BowlerSeasonStats(new SeasonId("01000000000000000000000002"));

        descriptor1.Key.ShouldNotBe(descriptor2.Key);
    }

    [Fact(DisplayName = "BowlerSeasonStats cache tags should contain neba")]
    public void BowlerSeasonStats_Tags_ShouldContainNebaTag()
    {
        var descriptor = CacheDescriptors.Stats.BowlerSeasonStats(SeasonId.New());

        descriptor.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "BowlerSeasonStats cache tags should contain neba:stats")]
    public void BowlerSeasonStats_Tags_ShouldContainStatsTag()
    {
        var descriptor = CacheDescriptors.Stats.BowlerSeasonStats(SeasonId.New());

        descriptor.Tags.ShouldContain("neba:stats");
    }

    [Fact(DisplayName = "BowlerSeasonStats cache tags should contain neba:stats:seasons")]
    public void BowlerSeasonStats_Tags_ShouldContainSeasonsTag()
    {
        var descriptor = CacheDescriptors.Stats.BowlerSeasonStats(SeasonId.New());

        descriptor.Tags.ShouldContain("neba:stats:seasons");
    }

    [Fact(DisplayName = "BowlerSeasonStats cache tags should contain season-specific tag")]
    public void BowlerSeasonStats_Tags_ShouldContainSeasonSpecificTag()
    {
        var seasonId = SeasonId.New();

        var descriptor = CacheDescriptors.Stats.BowlerSeasonStats(seasonId);

        descriptor.Tags.ShouldContain($"neba:stats:seasons:{seasonId}");
    }

    [Fact(DisplayName = "BowlerSeasonStats cache tags should contain exactly 4 tags")]
    public void BowlerSeasonStats_Tags_ShouldContainExactly4Tags()
    {
        var descriptor = CacheDescriptors.Stats.BowlerSeasonStats(SeasonId.New());

        descriptor.Tags.Count.ShouldBe(4);
    }
}
