using Neba.Application.Caching;
using Neba.Domain.Seasons;
using Neba.TestFactory.Attributes;

namespace Neba.Application.Tests.Stats;

[UnitTest]
[Component("Stats")]
public sealed class StatsCacheDescriptorsTests
{
    [Fact(DisplayName = "ListSeasonsWithStats key should be neba:stats:seasons:list")]
    public void ListSeasonsWithStats_Key_ShouldBeExpectedValue()
    {
        CacheDescriptors.Stats.ListSeasonsWithStats.Key.ShouldBe("neba:stats:seasons:list");
    }

    [Fact(DisplayName = "ListSeasonsWithStats tags should contain neba")]
    public void ListSeasonsWithStats_Tags_ShouldContainNebaTag()
    {
        CacheDescriptors.Stats.ListSeasonsWithStats.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "ListSeasonsWithStats tags should contain neba:stats")]
    public void ListSeasonsWithStats_Tags_ShouldContainStatsTag()
    {
        CacheDescriptors.Stats.ListSeasonsWithStats.Tags.ShouldContain("neba:stats");
    }

    [Fact(DisplayName = "ListSeasonsWithStats tags should contain neba:stats:seasons")]
    public void ListSeasonsWithStats_Tags_ShouldContainSeasonsTag()
    {
        CacheDescriptors.Stats.ListSeasonsWithStats.Tags.ShouldContain("neba:stats:seasons");
    }

    [Fact(DisplayName = "ListSeasonsWithStats tags should contain exactly 3 tags")]
    public void ListSeasonsWithStats_Tags_ShouldContainExactly3Tags()
    {
        CacheDescriptors.Stats.ListSeasonsWithStats.Tags.Count.ShouldBe(3);
    }

    [Fact(DisplayName = "BowlerSeasonStats key should follow neba:stats:seasons:{id}:bowlers format")]
    public void BowlerSeasonStats_Key_ShouldFollowExpectedFormat()
    {
        var seasonId = SeasonId.New();

        var descriptor = CacheDescriptors.Stats.BowlerSeasonStats(seasonId);

        descriptor.Key.ShouldBe($"neba:stats:seasons:{seasonId}:bowlers");
    }

    [Fact(DisplayName = "BowlerSeasonStats key should be specific to the season")]
    public void BowlerSeasonStats_Key_ShouldBeSpecificToSeason()
    {
        var season1 = SeasonId.New();
        var season2 = SeasonId.New();

        CacheDescriptors.Stats.BowlerSeasonStats(season1).Key
            .ShouldNotBe(CacheDescriptors.Stats.BowlerSeasonStats(season2).Key);
    }

    [Fact(DisplayName = "BowlerSeasonStats tags should contain neba")]
    public void BowlerSeasonStats_Tags_ShouldContainNebaTag()
    {
        var descriptor = CacheDescriptors.Stats.BowlerSeasonStats(SeasonId.New());

        descriptor.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "BowlerSeasonStats tags should contain neba:stats")]
    public void BowlerSeasonStats_Tags_ShouldContainStatsTag()
    {
        var descriptor = CacheDescriptors.Stats.BowlerSeasonStats(SeasonId.New());

        descriptor.Tags.ShouldContain("neba:stats");
    }

    [Fact(DisplayName = "BowlerSeasonStats tags should contain neba:stats:seasons")]
    public void BowlerSeasonStats_Tags_ShouldContainSeasonsTag()
    {
        var descriptor = CacheDescriptors.Stats.BowlerSeasonStats(SeasonId.New());

        descriptor.Tags.ShouldContain("neba:stats:seasons");
    }

    [Fact(DisplayName = "BowlerSeasonStats tags should contain season-specific tag")]
    public void BowlerSeasonStats_Tags_ShouldContainSeasonSpecificTag()
    {
        var seasonId = SeasonId.New();

        var descriptor = CacheDescriptors.Stats.BowlerSeasonStats(seasonId);

        descriptor.Tags.ShouldContain($"neba:stats:seasons:{seasonId}");
    }

    [Fact(DisplayName = "BowlerSeasonStats tags should contain exactly 4 tags")]
    public void BowlerSeasonStats_Tags_ShouldContainExactly4Tags()
    {
        var descriptor = CacheDescriptors.Stats.BowlerSeasonStats(SeasonId.New());

        descriptor.Tags.Count.ShouldBe(4);
    }
}