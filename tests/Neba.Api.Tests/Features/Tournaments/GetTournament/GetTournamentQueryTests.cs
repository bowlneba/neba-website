using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.GetTournament;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.GetTournament;

[UnitTest]
[Component("Tournaments")]
public sealed class GetTournamentDetailQueryTests
{
    [Fact(DisplayName = "Expiry should be 5 days")]
    public void Expiry_ShouldBe5Days()
    {
        // Act
        var query = new GetTournamentQuery { Id = TournamentId.New() };

        // Assert
        query.Expiry.ShouldBe(TimeSpan.FromDays(5));
    }

    [Fact(DisplayName = "Cache key should follow neba:tournaments:{id} format")]
    public void Cache_Key_ShouldFollowExpectedFormat()
    {
        // Arrange
        var id = TournamentId.New();

        // Act
        var query = new GetTournamentQuery { Id = id };

        // Assert
        query.Cache.Key.ShouldBe($"neba:tournaments:{id}");
    }

    [Fact(DisplayName = "Cache key should be specific to the tournament")]
    public void Cache_Key_ShouldBeSpecificToTournament()
    {
        // Act
        var query1 = new GetTournamentQuery { Id = TournamentId.New() };
        var query2 = new GetTournamentQuery { Id = TournamentId.New() };

        // Assert
        query1.Cache.Key.ShouldNotBe(query2.Cache.Key);
    }

    [Fact(DisplayName = "Cache tags should contain neba")]
    public void Cache_Tags_ShouldContainNebaTag()
    {
        // Act
        var query = new GetTournamentQuery { Id = TournamentId.New() };

        // Assert
        query.Cache.Tags.ShouldContain("neba");
    }

    [Fact(DisplayName = "Cache tags should contain neba:tournaments")]
    public void Cache_Tags_ShouldContainTournamentsTag()
    {
        // Act
        var query = new GetTournamentQuery { Id = TournamentId.New() };

        // Assert
        query.Cache.Tags.ShouldContain("neba:tournaments");
    }

    [Fact(DisplayName = "Cache tags should contain tournament-specific tag")]
    public void Cache_Tags_ShouldContainTournamentSpecificTag()
    {
        // Arrange
        var id = TournamentId.New();

        // Act
        var query = new GetTournamentQuery { Id = id };

        // Assert
        query.Cache.Tags.ShouldContain($"neba:tournaments:{id}");
    }

    [Fact(DisplayName = "Cache tags should contain exactly 3 tags")]
    public void Cache_Tags_ShouldContainExactly3Tags()
    {
        // Act
        var query = new GetTournamentQuery { Id = TournamentId.New() };

        // Assert
        query.Cache.Tags.Count.ShouldBe(3);
    }
}
