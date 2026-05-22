using Neba.Api.Caching;
using Neba.Api.Features.Bowlers.Domain;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Bowlers.GetBowlerTitles;

[UnitTest]
[Component("Bowlers")]
public sealed class GetBowlerTitlesQueryTests
{
    [Fact(DisplayName = "Titles cache key should follow neba:bowlers:{bowlerId}:titles format")]
    public void Titles_Key_ShouldFollowExpectedFormat()
    {
        var bowlerId = new BowlerId("01000000000000000000000001");

        var descriptor = CacheDescriptors.Bowlers.Titles(bowlerId);

        descriptor.Key.ShouldBe($"neba:bowlers:{bowlerId}:titles");
    }

    [Fact(DisplayName = "Titles cache key should be specific to the bowler")]
    public void Titles_Key_ShouldBeSpecificToBowler()
    {
        var descriptor1 = CacheDescriptors.Bowlers.Titles(new BowlerId("01000000000000000000000001"));
        var descriptor2 = CacheDescriptors.Bowlers.Titles(new BowlerId("01000000000000000000000002"));

        descriptor1.Key.ShouldNotBe(descriptor2.Key);
    }

    [Fact(DisplayName = "Titles cache tags should contain neba")]
    public void Titles_Tags_ShouldContainNebaTag()
    {
        var descriptor = CacheDescriptors.Bowlers.Titles(BowlerId.New());

        descriptor.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "Titles cache tags should contain neba:bowlers")]
    public void Titles_Tags_ShouldContainBowlersTag()
    {
        var descriptor = CacheDescriptors.Bowlers.Titles(BowlerId.New());

        descriptor.Tags.ShouldContain("neba:bowlers");
    }

    [Fact(DisplayName = "Titles cache tags should contain bowler-specific tag")]
    public void Titles_Tags_ShouldContainBowlerSpecificTag()
    {
        var bowlerId = BowlerId.New();

        var descriptor = CacheDescriptors.Bowlers.Titles(bowlerId);

        descriptor.Tags.ShouldContain($"neba:bowlers:{bowlerId}");
    }

    [Fact(DisplayName = "Titles cache tags should contain exactly 3 tags")]
    public void Titles_Tags_ShouldContainExactly3Tags()
    {
        var descriptor = CacheDescriptors.Bowlers.Titles(BowlerId.New());

        descriptor.Tags.Count.ShouldBe(3);
    }
}
