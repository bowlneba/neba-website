using Neba.Api.Caching;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Bowlers.GetBowlerTitles;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Bowlers.GetBowlerTitles;

[UnitTest]
[Component("Bowlers")]
public sealed class GetBowlerTitlesQueryTests
{
    [Fact(DisplayName = "Titles cache key should follow neba:bowlers:{bowlerId}:titles format")]
    public void Titles_Key_ShouldFollowExpectedFormat()
    {
        // Arrange
        var bowlerId = new BowlerId("01000000000000000000000001");

        // Act
        var descriptor = CacheDescriptors.Bowlers.Titles(bowlerId);

        // Assert
        descriptor.Key.ShouldBe($"neba:bowlers:{bowlerId}:titles");
    }

    [Fact(DisplayName = "Titles cache key should be specific to the bowler")]
    public void Titles_Key_ShouldBeSpecificToBowler()
    {
        // Act
        var descriptor1 = CacheDescriptors.Bowlers.Titles(new BowlerId("01000000000000000000000001"));
        var descriptor2 = CacheDescriptors.Bowlers.Titles(new BowlerId("01000000000000000000000002"));

        // Assert
        descriptor1.Key.ShouldNotBe(descriptor2.Key);
    }

    [Fact(DisplayName = "Titles cache tags should contain neba")]
    public void Titles_Tags_ShouldContainNebaTag()
    {
        // Act
        var descriptor = CacheDescriptors.Bowlers.Titles(BowlerId.New());

        // Assert
        descriptor.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "Titles cache tags should contain neba:bowlers")]
    public void Titles_Tags_ShouldContainBowlersTag()
    {
        // Act
        var descriptor = CacheDescriptors.Bowlers.Titles(BowlerId.New());

        // Assert
        descriptor.Tags.ShouldContain("neba:bowlers");
    }

    [Fact(DisplayName = "Titles cache tags should contain bowler-specific tag")]
    public void Titles_Tags_ShouldContainBowlerSpecificTag()
    {
        // Arrange
        var bowlerId = BowlerId.New();

        // Act
        var descriptor = CacheDescriptors.Bowlers.Titles(bowlerId);

        // Assert
        descriptor.Tags.ShouldContain($"neba:bowlers:{bowlerId}");
    }

    [Fact(DisplayName = "Titles cache tags should contain exactly 3 tags")]
    public void Titles_Tags_ShouldContainExactly3Tags()
    {
        // Act
        var descriptor = CacheDescriptors.Bowlers.Titles(BowlerId.New());

        // Assert
        descriptor.Tags.Count.ShouldBe(3);
    }

    // ── GetBowlerTitlesQuery properties ───────────────────────────────────

    [Fact(DisplayName = "Query Cache property should return descriptor keyed to the bowler")]
    public void Query_Cache_ShouldReturnDescriptorKeyedToBowler()
    {
        // Arrange
        var bowlerId = new BowlerId("01000000000000000000000001");
        var query = new GetBowlerTitlesQuery { BowlerId = bowlerId };

        // Act
        var descriptor = query.Cache;

        // Assert
        descriptor.Key.ShouldBe($"neba:bowlers:{bowlerId}:titles");
    }

    [Fact(DisplayName = "Query Expiry should be 7 days")]
    public void Query_Expiry_ShouldBe7Days()
    {
        // Arrange
        var query = new GetBowlerTitlesQuery { BowlerId = BowlerId.New() };

        // Act
        var expiry = query.Expiry;

        // Assert
        expiry.ShouldBe(TimeSpan.FromDays(7));
    }
}