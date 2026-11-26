using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.ListEligibleBowlers;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.ListEligibleBowlers;

[UnitTest]
[Component("Tournaments")]
public sealed class ListEligibleBowlersQueryTests
{
    [Fact(DisplayName = "Expiry should be 1 day")]
    public void Expiry_ShouldBe1Day()
    {
        // Arrange
        var query = new ListEligibleBowlersQuery { Id = TournamentId.New() };

        // Act
        var expiry = query.Expiry;

        // Assert
        expiry.ShouldBe(TimeSpan.FromDays(1));
    }

    [Fact(DisplayName = "Cache key should be neba:tournaments:{id}:eligible-bowlers with no caller scope")]
    public void Cache_Key_ShouldFollowExpectedFormat()
    {
        // Arrange
        var query = new ListEligibleBowlersQuery { Id = TournamentId.New() };

        // Act
        var key = query.Cache.Key;

        // Assert
        key.ShouldBe($"neba:tournaments:{query.Id}:eligible-bowlers");
    }

    [Fact(DisplayName = "Cache key should be specific to the tournament")]
    public void Cache_Key_ShouldBeSpecificToTournament()
    {
        // Arrange
        var first = new ListEligibleBowlersQuery { Id = TournamentId.New() };
        var second = new ListEligibleBowlersQuery { Id = TournamentId.New() };

        // Act
        var firstKey = first.Cache.Key;
        var secondKey = second.Cache.Key;

        // Assert
        firstKey.ShouldNotBe(secondKey);
    }

    [Fact(DisplayName = "Cache tags should contain neba, neba:tournaments, the tournament tag and the eligible-bowlers tag")]
    public void Cache_Tags_ShouldContainExpectedTags()
    {
        // Arrange
        var query = new ListEligibleBowlersQuery { Id = TournamentId.New() };

        // Act
        var tags = query.Cache.Tags;

        // Assert
        tags.ShouldContain("neba");
        tags.ShouldContain("neba:tournaments");
        tags.ShouldContain($"neba:tournaments:{query.Id}");
        tags.ShouldContain("neba:tournaments:eligible-bowlers");
    }

    [Fact(DisplayName = "Cache tags should contain exactly 4 tags")]
    public void Cache_Tags_ShouldContainExactly4Tags()
    {
        // Arrange
        var query = new ListEligibleBowlersQuery { Id = TournamentId.New() };

        // Act
        var tags = query.Cache.Tags;

        // Assert
        tags.Count.ShouldBe(4);
    }
}
